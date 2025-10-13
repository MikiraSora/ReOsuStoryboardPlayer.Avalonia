if (!globalThis.WebAudioInterop) {
    globalThis.WebAudioInterop = (() => {
        const players = new Map();

        function createPlayer(id) {
            if (players.has(id)) return;
            players.set(id, {
                context: new AudioContext(),
                buffer: null,
                source: null,
                gainNode: null,
                startTime: 0,
                offset: 0,
                duration: 0,
                volume: 1,
                onEnd: undefined,
                playing: false
            });

        }

        function hello() {
            console.log('Hello WebAudio333!');
        }

        async function loadFromBase64(id, base64) {
            const player = players.get(id);
            if (!player) return;

            const binary = atob(base64);
            const arrayBuffer = new ArrayBuffer(binary.length);
            const view = new Uint8Array(arrayBuffer);
            for (let i = 0; i < binary.length; i++) {
                view[i] = binary.charCodeAt(i);
            }

            player.buffer = await player.context.decodeAudioData(arrayBuffer.slice(0));
            player.duration = player.buffer.duration;
        }

        function _createSource(player) {
            const source = player.context.createBufferSource();
            source.buffer = player.buffer;

            if (!player.gainNode) {
                //make sure gainNode is created
                player.gainNode = player.context.createGain();
                player.gainNode.gain.value = player.volume;
                player.gainNode.connect(player.context.destination);
            }

            source.connect(player.gainNode);
            source.onended = () => {
                if (player.onEnd) {
                    player.onEnd();
                }
            };
            return source;
        }

        function play(id) {
            const player = players.get(id);
            if (!player || !player.buffer) return;

            if (player.playing) return; // already playing

            player.source = _createSource(player);
            player.id = id;
            player.startTime = player.context.currentTime - player.offset;
            player.source.start(0, player.offset);
            player.playing = true;
            player.onEnd = () => {
                player.playing = false;
                // 调用 C# 导出的回调
                if (globalThis.AudioPlayer_OnPlaybackEnded)
                    globalThis.AudioPlayer_OnPlaybackEnded(player.id);
            };
        }

        function pause(id) {
            const player = players.get(id);
            if (!player?.playing) return;

            stop(id, true);
        }

        function stop(id, keepOffset = false) {
            const player = players.get(id);
            if (!player?.source) return;

            try {
                player.source.stop();
            } catch {
            }
            player.source.disconnect();
            if (!keepOffset) player.offset = 0;
            else player.offset = player.context.currentTime - player.startTime;
            player.playing = false;
        }

        function jumpToTime(id, seconds, isPauseAfterJumped) {
            const player = players.get(id);
            if (!player?.buffer) return;

            //这里先清空onended避免误触发
            player.onEnd = undefined;
            // 停止当前播放
            stop(id);
            player.offset = Math.min(seconds, player.duration);

            // 重新创建 source 并开始播放
            player.source = _createSource(player);
            player.id = id;
            player.startTime = player.context.currentTime - player.offset;
            player.source.start(0, player.offset);
            player.playing = true;

            if (isPauseAfterJumped) {
                pause(id);
            }
        }

        function getCurrentTime(id) {
            const player = players.get(id);
            if (!player) return 0;

            if (player.playing)
                return player.context.currentTime - player.startTime;
            else
                return player.offset;
        }

        function getDuration(id) {
            return players.get(id)?.duration || 0;
        }

        function dispose(id) {
            const player = players.get(id);
            stop(id);
            player.gainNode.disconnect();
            player.gainNode = null;
            players.delete(id);
        }

        function setVolume(id, volume) {
            const player = players.get(id);
            player.volume = Math.max(0, Math.min(1, volume));
            player.gainNode.gain.value = player.volume;
        }

        function getVolume(id) {
            const player = players.get(id);
            return player.volume;
        }

        return {
            createPlayer,
            loadFromBase64,
            play,
            pause,
            stop,
            jumpToTime,
            getCurrentTime,
            getDuration,
            dispose,
            hello,
            setVolume,
            getVolume
        };
    })();

    console.log("webAudio.js initialized");
    console.log(globalThis.WebAudioInterop);
}