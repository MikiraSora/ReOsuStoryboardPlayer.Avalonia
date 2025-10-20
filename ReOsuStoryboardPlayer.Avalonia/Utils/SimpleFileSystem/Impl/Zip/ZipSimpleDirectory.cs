using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.Zip;

public class ZipSimpleDirectory : ISimpleDirectory
{
    private readonly Dictionary<string, ZipSimpleDirectory> _dirs = new();
    private readonly Dictionary<string, ZipSimpleFile> _files = new();

    public ZipSimpleDirectory(ISimpleDirectory parent, string name)
    {
        ParentDictionary = parent;
        DirectoryName = name;
    }

    public ISimpleDirectory ParentDictionary { get; }

    public ISimpleDirectory[] ChildDictionaries => _dirs.Values.ToArray<ISimpleDirectory>();

    public ISimpleFile[] ChildFiles => _files.Values.ToArray<ISimpleFile>();
    public string FullPath => Path.Combine(ParentDictionary?.FullPath ?? string.Empty, DirectoryName);

    public string DirectoryName { get; }

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

    public void Dispose()
    {
        //nothing to do
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

    public void AddDirectory(ZipSimpleDirectory dir)
    {
        _dirs[dir.DirectoryName] = dir;
    }

    public void AddFile(ZipSimpleFile file)
    {
        _files[file.FileName] = file;
    }

    /// <summary>
    ///     从 zip 字节加载虚拟文件系统
    /// </summary>
    public static async Task<ISimpleDirectory> LoadFromZipFileBytes(byte[] bytes)
    {
        var root = new ZipSimpleDirectory(null, "");

        using var ms = new MemoryStream(bytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            var pathParts = entry.FullName.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length == 0) continue;

            var current = root;
            for (var i = 0; i < pathParts.Length - 1; i++)
            {
                var dirName = pathParts[i];
                if (!current._dirs.TryGetValue(dirName, out var next))
                {
                    next = new ZipSimpleDirectory(current, dirName);
                    current.AddDirectory(next);
                }

                current = next;
            }

            // 如果是文件
            if (!entry.FullName.EndsWith("/"))
            {
                using var es = entry.Open();
                using var msEntry = new MemoryStream();
                await es.CopyToAsync(msEntry);
                var data = msEntry.ToArray();

                var fileName = pathParts[^1];
                var file = new ZipSimpleFile(current, fileName, data);
                current.AddFile(file);
            }
        }

        return root;
    }

    public override string ToString()
    {
        return
            $"Directory: {FullPath}, ChildDirsCount: {ChildDictionaries.Length}, ChildFilesCount: {ChildFiles.Length}";
    }
}