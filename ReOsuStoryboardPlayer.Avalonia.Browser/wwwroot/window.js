if (!globalThis.WindowInterop) {
    globalThis.WindowInterop = (() => {
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

        function openURL(url) {
            globalThis.open(url);
        }

        return {
            exitFullScreen, requestFullScreen, isFullScreen, openURL
        };
    })();

    console.log("window.js initialized");
}

