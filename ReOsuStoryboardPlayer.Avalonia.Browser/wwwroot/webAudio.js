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
                leadIn: 0,
                offset: 0,
                duration: 0,
                volume: 1,
                onEnd: undefined,
                playing: false
            });
        }

        async function loadFromBase64(id, base64, leadIn = 0) {
            const player = players.get(id);
            if (!player) return;

            console.log("loadFromBase64() start");
            const binary = atob(base64);
            const arrayBuffer = new ArrayBuffer(binary.length);
            const view = new Uint8Array(arrayBuffer);
            for (let i = 0; i < binary.length; i++) {
                view[i] = binary.charCodeAt(i);
            }

            const decodedBuffer = await player.context.decodeAudioData(arrayBuffer.slice(0));

            const totalLength = decodedBuffer.length + Math.floor(decodedBuffer.sampleRate * leadIn);
            const channels = decodedBuffer.numberOfChannels;
            const newBuffer = player.context.createBuffer(channels, totalLength, decodedBuffer.sampleRate);

            for (let ch = 0; ch < channels; ch++) {
                const newData = newBuffer.getChannelData(ch);
                const srcData = decodedBuffer.getChannelData(ch);
                newData.set(srcData, Math.floor(decodedBuffer.sampleRate * leadIn));
            }

            player.buffer = newBuffer;
            player.duration = newBuffer.duration;
            player.offset = 0;
            player.leadIn = leadIn;

            console.log(`loadFromBase64() done, duration=${player.duration.toFixed(2)}, leadIn=${player.leadIn}s`);
        }

        function _createSource(player) {
            const source = player.context.createBufferSource();
            source.buffer = player.buffer;

            if (!player.gainNode) {
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

            if (player.playing) return;

            player.source = _createSource(player);
            player.id = id;
            player.startTime = player.context.currentTime - player.offset;
            player.source.start(0, player.offset);
            player.playing = true;
            player.onEnd = () => {
                player.playing = false;
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

            player.onEnd = null;
            try {
                player.source.stop();
            } catch {
            }
            player.source.disconnect();
            if (!keepOffset)
                player.offset = 0;
            else
                player.offset = player.context.currentTime - player.startTime;
            player.playing = false;
            console.log(`stop() keepOffset=${keepOffset} player.offset = ${player.offset}`)
        }

        function jumpToTime(id, seconds, isPauseAfterJumped) {
            const player = players.get(id);
            if (!player?.buffer) return;

            stop(id);
            player.offset = Math.min(seconds + player.leadIn, player.duration);

            player.source = _createSource(player);
            player.id = id;
            player.startTime = player.context.currentTime - player.offset;
            console.log(`stop() player.context.currentTime=${player.context.currentTime} player.offset=${player.offset} player.startTime = ${player.startTime}`)
            player.source.start(0, player.offset);
            player.playing = true;

            if (isPauseAfterJumped)
                pause(id);

        }

        function getCurrentTime(id) {
            const player = players.get(id);
            if (!player) return 0;
            const time = player.playing
                ? player.context.currentTime - player.startTime
                : player.offset;
            //console.log(`getCurrentTime() player.playing=${player.playing} time=${time}`)
            return Math.max(0, time);
        }

        function getDuration(id) {
            return players.get(id)?.duration || 0;
        }

        function dispose(id) {
            const player = players.get(id);
            stop(id);
            if (player?.gainNode) {
                player.gainNode.disconnect();
                player.gainNode = null;
            }
            players.delete(id);
        }

        function setVolume(id, volume) {
            const player = players.get(id);
            player.volume = Math.max(0, Math.min(1, volume));
            if (player.gainNode)
                player.gainNode.gain.value = player.volume;
        }

        function getVolume(id) {
            const player = players.get(id);
            return player?.volume ?? 1;
        }

        function hello() {
            console.log('Hello WebAudio!');
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
}