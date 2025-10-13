using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem;

public interface ISimpleFile
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