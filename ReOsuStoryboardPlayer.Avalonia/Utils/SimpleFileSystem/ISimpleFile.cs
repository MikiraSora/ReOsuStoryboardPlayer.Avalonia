using System;
using System.IO;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;

public interface ISimpleFile : IDisposable
{
    ISimpleDirectory ParentDictionary { get; }

    string FullPath { get; }

    /// <summary>
    ///     likes "myFile.txt"
    /// </summary>
    string FileName { get; }

    long FileLength { get; }

    ValueTask<string[]> ReadAllLines();
    ValueTask<byte[]> ReadAllBytes();
    Task<Stream> OpenRead();
}