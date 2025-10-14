if (!globalThis.LocalFileSystemInterop) {
    globalThis.LocalFileSystemInterop = (() => {
        const __fileHandleMap = new Map();
        let __nextHandleId = 1;

        async function userPickFile() {
            const handle = await window.showOpenFilePicker({
                multiple: false,
                types: [
                    {
                        description: ".osz/.zip Files",
                        accept: {
                            "application/zip": [".zip", ".osz"]
                        }
                    },
                ],
                excludeAcceptAllOption: false,
            });

            const file = await handle[0].getFile();
            const buffer = await file.arrayBuffer();
            return new Uint8Array(buffer);
        }

        async function userPickDirectory() {
            const dirHandle = await window.showDirectoryPicker({
                mode: 'read',
            });
            const root = await buildDirectoryInfo(dirHandle);
            return JSON.stringify(root);
        }

        async function buildDirectoryInfo(dirHandle) {
            const dir = {
                DirectoryName: dirHandle.name,
                ChildDictionaries: [],
                ChildFiles: []
            };

            for await (const [name, handle] of dirHandle.entries()) {
                if (handle.kind === "file") {
                    const id = `fh_${__nextHandleId++}`;
                    __fileHandleMap.set(id, handle);

                    const file = await handle.getFile();
                    dir.ChildFiles.push({
                        FileName: name,
                        FileLength: file.size,
                        fileHandle: id
                    });
                } else if (handle.kind === "directory") {
                    dir.ChildDictionaries.push(await buildDirectoryInfo(handle));
                }
            }

            return dir;
        }

        async function readFileAllBytes(fileHandle) {
            const handle = __fileHandleMap.get(fileHandle);
            if (!handle) {
                console.error(`[readFileAllBytes] Invalid handle: ${fileHandle}`);
                return new Uint8Array();
            }

            const file = await handle.getFile();
            const buffer = await file.arrayBuffer();
            return new Uint8Array(buffer);
        }

        function disposeFileHandle(fileHandle) {
            __fileHandleMap.delete(fileHandle);
        }

        return {readFileAllBytes, disposeFileHandle, userPickDirectory, userPickFile}
    })();

    console.log("localFileSystem.js initialized");
}