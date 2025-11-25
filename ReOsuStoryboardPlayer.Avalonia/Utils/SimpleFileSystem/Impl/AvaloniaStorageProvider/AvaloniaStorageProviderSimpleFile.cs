using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.AvaloniaStorageProvider;

public class AvaloniaStorageProviderSimpleFile : ISimpleFile, IDisposable
{
    private IStorageFile file;
    private WeakReference<byte[]> data;

    public AvaloniaStorageProviderSimpleFile(ISimpleDirectory parent, string fileName, long fileLength,
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

    public long FileLength { get; }

    private static readonly string[] separator = new[] {"\r\n", "\n"};

    public async ValueTask<byte[]> ReadAllBytes()
    {
#if DEBUG
        ObjectDisposedException.ThrowIf(file == null, $"AvaloniaStorageProviderSimpleFile {FullPath} is disposed.");
#endif
        if (file == null)
            return [];
        if (data !=null && data.TryGetTarget(out var target))
        {
            return target;
        }
        byte[] buffer = new byte[FileLength];
        data = new(buffer);
        using var fs = await file.OpenReadAsync();
        await fs.ReadExactlyAsync(buffer);
        return buffer;
    }

    public async ValueTask<string[]> ReadAllLines()
    {
        var text = Encoding.UTF8.GetString(await ReadAllBytes());
        var lines = text.Split(separator, StringSplitOptions.None);
        return lines;
    }

    public async Task<Stream> OpenRead()
    {
        return new SeekableStream(await file.OpenReadAsync(),FileLength);
    }

    public override string ToString()
    {
        return $"File: {FullPath}, Length: {FileLength}";
    }
}