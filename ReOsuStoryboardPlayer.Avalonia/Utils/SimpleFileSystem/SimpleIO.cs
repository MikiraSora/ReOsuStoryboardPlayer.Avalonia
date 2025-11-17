using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;

public static class SimpleIO
{
    public static bool ExistDirectory(ISimpleDirectory root, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;

        var dir = FindDirectory(root, path);
        return dir != null;
    }

    public static bool ExistFile(ISimpleDirectory root, string path)
    {
        var dir = FindFile(root, path);
        return dir != null;
    }

    public static Task<Stream> OpenRead(ISimpleDirectory root, string path)
    {
        var file = FindFile(root, path);
        return file is null ? throw new FileNotFoundException($"File not found: {path}") : file.OpenRead();
    }

    public static ValueTask<string[]> ReadAllLines(ISimpleDirectory root, string path)
    {
        var file = FindFile(root, path);
        return file is null ? throw new FileNotFoundException($"File not found: {path}") : file.ReadAllLines();
    }

    public static ISimpleFile[] GetFiles(ISimpleDirectory root, string path, string searchPattern = "*")
    {
        var dir = FindDirectory(root, path);
        if (dir == null)
            return [];

        var regex = WildcardToRegex(searchPattern);

        return [.. dir.ChildFiles.Where(f => regex.IsMatch(f.FileName))];
    }

    public static string[] GetFilePaths(ISimpleDirectory root, string path, string searchPattern = "*")
    {
        var dir = FindDirectory(root, path);
        if (dir == null)
            return [];

        var regex = WildcardToRegex(searchPattern);

        return [.. dir.ChildFiles
            .Where(f => regex.IsMatch(f.FileName))
            .Select(f => f.FullPath)];
    }

    private static readonly char[] separator = ['/', '\\'];

    public static ISimpleFile FindFile(ISimpleDirectory root, string path)
    {
        var parts = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        var dirPath = string.Join('/', parts.Take(parts.Length - 1));
        var fileName = parts.LastOrDefault();

        var dir = FindDirectory(root, dirPath);
        if (dir == null)
            return default;

        var file = dir.ChildFiles.FirstOrDefault(f => f.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        return file;
    }

    public static ISimpleDirectory FindDirectory(ISimpleDirectory root, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return root;

        var parts = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        var current = root;

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            if (part == ".")
                continue;

            if (part == "..")
            {
                current = current.ParentDictionary;
                continue;
            }

            var next = current.ChildDictionaries.FirstOrDefault(d =>
                d.DirectoryName.Equals(part, StringComparison.OrdinalIgnoreCase));

            current = next;
        }

        return current;
    }

    private static Regex WildcardToRegex(string pattern)
    {
        var sb = new StringBuilder("^");
        foreach (var c in pattern)
            sb.Append(c switch
            {
                '*' => ".*",
                '?' => ".",
                _ => Regex.Escape(c.ToString())
            });
        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}