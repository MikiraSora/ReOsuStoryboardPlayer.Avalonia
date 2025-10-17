if (!globalThis.MiscInterop) {
    globalThis.MiscInterop = (() => {
        function getHref() {
            return window.location.href;
        }

        return {
            getHref
        };
    })();

    console.log("misc.js initialized");
}