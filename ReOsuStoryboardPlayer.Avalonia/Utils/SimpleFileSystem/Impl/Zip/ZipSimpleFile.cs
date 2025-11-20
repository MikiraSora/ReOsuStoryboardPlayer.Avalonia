using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.Zip;

public class ZipSimpleFile : ISimpleFile
{
    private readonly ZipArchiveEntry _entry;
    private WeakReference<byte[]> _data; 

    public ZipSimpleFile(ISimpleDirectory parent, string fileName, ZipArchiveEntry entry)
    {
        ParentDictionary = parent;
        FileName = fileName;
        _entry = entry;
    }

    public ISimpleDirectory ParentDictionary { get; }
    public string FullPath => Path.Combine(ParentDictionary?.FullPath ?? string.Empty, FileName);

    public string FileName { get; }

    public long FileLength => _entry.Length;

    private static readonly string[] separator = ["\r\n", "\n"];

    public async ValueTask<byte[]> ReadAllBytes()
    {
        if (_data != null && _data.TryGetTarget(out var target))
        {
            return target;
        }
        var buffer = new byte[FileLength];
        _data = new(buffer);
        using var stream = await _entry.OpenAsync();
        await stream.ReadExactlyAsync(buffer);
        return buffer;
    }

    public async Task<Stream> OpenRead()
    {
        return new SeekableStream(await _entry.OpenAsync(),FileLength); 
    }

    public async ValueTask<string[]> ReadAllLines()
    {
        var text = Encoding.UTF8.GetString(await ReadAllBytes());
        var lines = text.Split(separator, StringSplitOptions.None);
        return lines;
    }

    public override string ToString()
    {
        return $"File: {FullPath}, Length: {FileLength}";
    }

    public void Dispose()
    {
        //nothing to do
    }
}