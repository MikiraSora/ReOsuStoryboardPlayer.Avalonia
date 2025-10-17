using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.Zip;

public class ZipSimpleFile : ISimpleFile
{
    private readonly byte[] _data;

    public ZipSimpleFile(ISimpleDirectory parent, string fileName, byte[] data)
    {
        ParentDictionary = parent;
        FileName = fileName;
        _data = data;
    }

    public ISimpleDirectory ParentDictionary { get; }
    public string FullPath => Path.Combine(ParentDictionary?.FullPath ?? string.Empty, FileName);

    public string FileName { get; }

    public int FileLength => _data.Length;

    public Task<byte[]> ReadAllBytes()
    {
        return Task.FromResult(_data);
    }

    public Task<string[]> ReadAllLines()
    {
        var text = Encoding.UTF8.GetString(_data);
        var lines = text.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
        return Task.FromResult(lines);
    }

    public override string ToString()
    {
        return $"File: {FullPath}, Length: {FileLength}";
    }
}