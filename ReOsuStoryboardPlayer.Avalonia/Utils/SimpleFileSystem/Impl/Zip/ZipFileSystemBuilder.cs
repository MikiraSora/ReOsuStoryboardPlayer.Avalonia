using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.Zip;

public class ZipFileSystemBuilder
{
    public static async Task<ISimpleDirectory> LoadFromZipFileBytes(byte[] zipFileBytes)
    {
        var root = await ZipSimpleDirectory.LoadFromZipFileBytes(zipFileBytes);
        return root;
    }
}