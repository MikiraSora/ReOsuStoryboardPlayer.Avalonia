using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.AvaloniaStorageProvider;

public class AvaloniaStorageProviderSimpleFile : ISimpleFile, IDisposable
{
    private IStorageFile file;

    public AvaloniaStorageProviderSimpleFile(ISimpleDirectory parent, string fileName, int fileLength,
        IStorageFile file)
    {
        this.file = file;
        FileLength = fileLength;
        ParentDictionary = parent;
        FileName = fileName;
    }

    public void Dispose()
    {
        file?.Dispose();
        file = null;
    }

    public ISimpleDirectory ParentDictionary { get; }
    public string FullPath => Path.Combine(ParentDictionary?.FullPath ?? string.Empty, FileName);

    public string FileName { get; }

    public int FileLength { get; }

    public async Task<byte[]> ReadAllBytes()
    {
#if DEBUG
        if (file == null)
            throw new ObjectDisposedException($"AvaloniaStorageProviderSimpleFile {FullPath} is disposed.");
#endif
        if (file == null)
            return [];
        using var fs = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await fs.CopyToAsync(ms);
        return ms.ToArray();
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