using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.Utils;

public partial class LocalFileSystemInterop
{
    [JSImport("globalThis.LocalFileSystemInterop.userPickFile")]
    [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
    private static partial Task<object> PickFileInternal();

    public static async Task<byte[]> PickFile()
    {
        var ret = await PickFileInternal();
        return ret as byte[];
    }

    [JSImport("globalThis.LocalFileSystemInterop.userPickDirectory")]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    private static partial Task<string> PickDirectoryInternal();

    public static async Task<JSDirectory> PickDirectory()
    {
        var jsonContent = await PickDirectoryInternal();
        return JsonSerializer.Deserialize<JSDirectory>(jsonContent);
    }

    [JSImport("globalThis.LocalFileSystemInterop.readFileAllBytes")]
    [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
    private static partial Task<object> ReadFileAllBytesInternal(string fileHandle);

    public static async Task<byte[]> ReadFileAllBytes(string fileHandle)
    {
        var ret = await ReadFileAllBytesInternal(fileHandle);
        return ret as byte[];
    }

    [JSImport("globalThis.LocalFileSystemInterop.disposeFileHandle")]
    public static partial void DisposeFileHandle(string fileHandle);

    public record JSFile(
        string FileName,
        int FileLength,
        string fileHandle
    );

    public record JSDirectory(
        string DirectoryName,
        JSDirectory[] ChildDictionaries,
        JSFile[] ChildFiles
    );
}