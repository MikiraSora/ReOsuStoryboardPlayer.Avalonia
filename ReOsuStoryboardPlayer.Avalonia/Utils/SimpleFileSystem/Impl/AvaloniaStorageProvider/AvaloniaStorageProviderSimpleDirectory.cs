using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.AvaloniaStorageProvider;

public class AvaloniaStorageProviderSimpleDirectory : ISimpleDirectory
{
    private readonly Dictionary<string, AvaloniaStorageProviderSimpleDirectory> _dirs = new();
    private readonly Dictionary<string, AvaloniaStorageProviderSimpleFile> _files = new();

    public AvaloniaStorageProviderSimpleDirectory(ISimpleDirectory parent, string name)
    {
        ParentDictionary = parent;
        DirectoryName = name;
    }

    public ISimpleDirectory ParentDictionary { get; }

    public ISimpleDirectory[] ChildDictionaries => _dirs.Values.ToArray<ISimpleDirectory>();

    public ISimpleFile[] ChildFiles => _files.Values.ToArray<ISimpleFile>();
    public string FullPath => Path.Combine(ParentDictionary?.FullPath ?? string.Empty, DirectoryName);

    public string DirectoryName { get; set; }

    public bool ExistsDirectory(string dirName)
    {
        return _dirs.ContainsKey(dirName);
    }

    public bool ExistsFile(string fileName)
    {
        return _files.ContainsKey(fileName);
    }

    public ISimpleFile[] GetFiles(string searchPattern = "*")
    {
        var regex = WildcardToRegex(searchPattern);

        var results = new List<ISimpleFile>();
        foreach (var kv in _files)
        {
            var fileName = kv.Key;
            if (regex.IsMatch(fileName))
                results.Add(kv.Value);
        }

        return results.ToArray();
    }

    private static Regex WildcardToRegex(string pattern)
    {
        var sb = new StringBuilder();
        sb.Append("^");
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            switch (c)
            {
                case '*':
                    sb.Append(".*");
                    break;
                case '?':
                    sb.Append(".");
                    break;
                default:
                    sb.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }

        sb.Append("$");
        return new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public void AddDirectory(AvaloniaStorageProviderSimpleDirectory dir)
    {
        _dirs[dir.DirectoryName] = dir;
    }

    public void AddFile(AvaloniaStorageProviderSimpleFile file)
    {
        _files[file.FileName] = file;
    }

    public override string ToString()
    {
        return
            $"Directory: {FullPath}, ChildDirsCount: {ChildDictionaries.Length}, ChildFilesCount: {ChildFiles.Length}";
    }
}