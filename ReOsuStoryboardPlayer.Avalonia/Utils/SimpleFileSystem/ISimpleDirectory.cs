using System;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;

public interface ISimpleDirectory : IDisposable
{
    ISimpleDirectory ParentDictionary { get; }

    ISimpleDirectory[] ChildDictionaries { get; }
    ISimpleFile[] ChildFiles { get; }

    string FullPath { get; }

    /// <summary>
    ///     likes "MyFolderA"
    /// </summary>
    string DirectoryName { get; }

    bool ExistsDirectory(string dirName);
    bool ExistsFile(string fileName);

    ISimpleFile[] GetFiles(string pattern);
}