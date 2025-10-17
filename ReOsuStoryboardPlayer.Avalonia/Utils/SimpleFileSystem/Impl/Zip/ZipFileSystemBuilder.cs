namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.Zip;

public class ZipFileSystemBuilder
{
    public static ISimpleDirectory LoadFromZipFileBytes(byte[] zipFileBytes)
    {
        var root = ZipSimpleDirectory.LoadFromZipFileBytes(zipFileBytes);
        return root;
    }
}