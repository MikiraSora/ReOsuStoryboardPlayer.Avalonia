if (!globalThis.FullScreenInterop) {
    globalThis.FullScreenInterop = (() => {
        function isFullScreen() {
            return document.fullscreen;
        }

        function requestFullScreen() {
            document.documentElement.requestFullscreen();
        }

        function exitFullScreen() {
            if (document.fullscreenElement) {
                document.exitFullscreen();
            }
        }

        return {
            exitFullScreen, requestFullScreen, isFullScreen
        };
    })();

    console.log("fullscreen.js initialized");
}

