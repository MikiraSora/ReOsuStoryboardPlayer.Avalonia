using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem.Impl.Zip;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem;

public static class BrowserFileSystemBuilder
{
    public static ISimpleDirectory LoadFromZipFileBytes(byte[] zipFileBytes)
    {
        var root = ZipSimpleDirectory.LoadFromZipFileBytes(zipFileBytes);
        return root;
    }
    
    public static ISimpleDirectory LoadFromDisk(byte[] bytes)
    {
        return default;
    }
}