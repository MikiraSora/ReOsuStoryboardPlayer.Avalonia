using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem.Impl.AvaloniaStorageProvider;

public class AvaloniaStorageProviderFileSystemBuilder
{
    public static async Task<ISimpleDirectory> LoadFromAvaloniaStorageFolder(IStorageFolder rootStorageFolder)
    {
        async Task<AvaloniaStorageProviderSimpleDirectory> buildDir(ISimpleDirectory parent, IStorageFolder folder)
        {
            var jsfsDir = new AvaloniaStorageProviderSimpleDirectory(parent, folder.Name);
            await foreach (var item in folder.GetItemsAsync())
                switch (item)
                {
                    case IStorageFile childFile:
                        var prop = await childFile.GetBasicPropertiesAsync();
                        var childJsfsFile =
                            new AvaloniaStorageProviderSimpleFile(jsfsDir, childFile.Name, (long) (prop.Size ?? 0), childFile);
                        jsfsDir.AddFile(childJsfsFile);
                        break;
                    case IStorageFolder childFolder:
                        var childJsfsDir = await buildDir(jsfsDir, childFolder);
                        jsfsDir.AddDirectory(childJsfsDir);
                        break;
                }

            return jsfsDir;
        }

        var jsfsDirRoot = await buildDir(null, rootStorageFolder);
        jsfsDirRoot.DirectoryName = string.Empty;
        return jsfsDirRoot;
    }
}