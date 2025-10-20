using System;
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

    int FileLength { get; }

    Task<string[]> ReadAllLines();
    Task<byte[]> ReadAllBytes();
}