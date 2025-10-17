using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem.Impl.JSFileSystem;

public class JsfsSimpleFile : ISimpleFile
{
    private readonly string fileHandle;

    public JsfsSimpleFile(ISimpleDirectory parent, string fileName, int fileLength, string fileHandle)
    {
        FileLength = fileLength;
        this.fileHandle = fileHandle;
        ParentDictionary = parent;
        FileName = fileName;
    }

    public ISimpleDirectory ParentDictionary { get; }
    public string FullPath => Path.Combine(ParentDictionary?.FullPath ?? string.Empty, FileName);

    public string FileName { get; }

    public int FileLength { get; }

    public Task<byte[]> ReadAllBytes()
    {
        return LocalFileSystemInterop.ReadFileAllBytes(fileHandle);
    }

    public async Task<string[]> ReadAllLines()
    {
        var text = Encoding.UTF8.GetString(await ReadAllBytes());
        var lines = text.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
        return lines;
    }

    public override string ToString()
    {
        return $"File: {FullPath}, Length: {FileLength}";
    }
}