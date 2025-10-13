using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem.Impl.JSFileSystem;
using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem.Impl.Zip;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem;

public static class BrowserFileSystemBuilder
{
    public static ISimpleDirectory LoadFromZipFileBytes(byte[] zipFileBytes)
    {
        var root = ZipSimpleDirectory.LoadFromZipFileBytes(zipFileBytes);
        return root;
    }

    public static ISimpleDirectory LoadFromLocalFileSystem(LocalFileSystemInterop.JSDirectory jsDirRoot)
    {
        JsfsSimpleDirectory buildDir(ISimpleDirectory parent, LocalFileSystemInterop.JSDirectory jsDir)
        {
            var jsfsDir = new JsfsSimpleDirectory(parent, jsDir.DirectoryName);
            foreach (var childJsDir in jsDir.ChildDictionaries)
            {
                var childJsfsDir = buildDir(jsfsDir, childJsDir);
                jsfsDir.AddDirectory(childJsfsDir);
            }

            foreach (var childJsFile in jsDir.ChildFiles)
            {
                var childJsfsFile = new JsfsSimpleFile(jsfsDir, childJsFile.FileName, childJsFile.FileLength,
                    childJsFile.fileHandle);
                jsfsDir.AddFile(childJsfsFile);
            }

            return jsfsDir;
        }

        var jsfsDirRoot = buildDir(null, jsDirRoot with {DirectoryName = ""});
        return jsfsDirRoot;
    }
}