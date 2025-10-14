if (!globalThis.LocalStorageInterop) {
    globalThis.LocalStorageInterop = (() => {
        function load(key) {
            try {
                const value = localStorage.getItem(key);
                return Promise.resolve(value);
            } catch (error) {
                console.error(`Error loading from localStorage for key '${key}':`, error);
                return Promise.resolve(null);
            }
        }

        function save(key, value) {
            try {
                localStorage.setItem(key, value);
                return Promise.resolve();
            } catch (error) {
                console.error(`Error saving to localStorage for key '${key}':`, error);
                return Promise.resolve(null);
            }
        }

        return {
            save, load
        };
    })
    ();

    console.log('localStorage.js initialized');
}