using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Desktop.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Core.Base;
using ReOsuStoryboardPlayer.Core.Parser;
using ReOsuStoryboardPlayer.Core.Parser.Reader;
using ReOsuStoryboardPlayer.Core.Parser.Stream;
using ReOsuStoryboardPlayer.Core.Utils;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Storyboards;

[RegisterSingleton<IStoryboardLoader>]
public class DesktopStoryboardLoader(ILogger<DesktopStoryboardLoader> logger, IParameterManager parameterManager)
    : IStoryboardLoader
{
    private readonly IParameterManager parameterManager = parameterManager;

    public async ValueTask<IStoryboardInstance> OpenLoaderDialog()
    {
        var toplevel = (App.Current as App).TopLevel;
        var folder = (await toplevel.StorageProvider?.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Select Beatmap Folder"
        })).FirstOrDefault();

        return await LoadStoryboardInstanceFromDisk(default);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    public static string BrowseFolder(string description)
    {
        // 使用COM组件实现文件夹选择
        var shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType == null) return null;

        dynamic shell = Activator.CreateInstance(shellType);
        var window = GetActiveWindow();
        var folder = shell.BrowseForFolder(window, description, 0);

        if (folder != null)
        {
            var folderItems = folder.Items();
            if (folderItems != null)
                return folderItems.Item().Path;
        }

        return null;
    }

    private async ValueTask<IStoryboardInstance> LoadStoryboardInstanceFromDisk(string selectDirPath)
    {
        logger?.LogInformationEx($"selectDirPath: {selectDirPath}");
        var instance = DesktopStoryboardInstance.CreateInstance();

        instance.InfoEx = BeatmapFolderInfoEx.Parse(selectDirPath, parameterManager.Parameters);
        if (instance.Info == null)
        {
            logger?.LogErrorEx("can't create BeatmapFolderInfo");
            return default;
        }

        instance.StoryboardInfo = ParseStoryboardInfo(instance.InfoEx);

        List<StoryboardObject> temp_objs_list = new(), parse_osb_Storyboard_objs = new();

        //get objs from osu file
        var parse_osu_Storyboard_objs = string.IsNullOrWhiteSpace(instance.InfoEx.osu_file_path)
            ? new List<StoryboardObject>()
            : StoryboardParserHelper.GetStoryboardObjects(instance.InfoEx.osu_file_path);
        AdjustZ(parse_osu_Storyboard_objs);

        if (!string.IsNullOrWhiteSpace(instance.InfoEx.osb_file_path) && File.Exists(instance.InfoEx.osb_file_path))
        {
            parse_osb_Storyboard_objs = StoryboardParserHelper.GetStoryboardObjects(instance.InfoEx.osb_file_path);
            AdjustZ(parse_osb_Storyboard_objs);
        }

        temp_objs_list = CombineStoryboardObjects(parse_osb_Storyboard_objs, parse_osu_Storyboard_objs);

        void AdjustZ(List<StoryboardObject> list)
        {
            list.Sort((a, b) => (int) (a.FileLine - b.FileLine));
        }

        List<StoryboardObject> CombineStoryboardObjects(List<StoryboardObject> osb_list,
            List<StoryboardObject> osu_list)
        {
            var result = new List<StoryboardObject>();

            Add(Layer.Background);
            Add(Layer.Fail);
            Add(Layer.Pass);
            Add(Layer.Foreground);
            Add(Layer.Overlay);

            var z = 0;
            foreach (var obj in result)
                obj.Z = z++;

            return result;

            void Add(Layer layout)
            {
                result.AddRange(osu_list.Where(x => x.layer == layout)); //先加osu
                result.AddRange(osb_list.Where(x => x.layer == layout).Select(x =>
                {
                    x.FromOsbFile = true;
                    return x;
                })); //后加osb覆盖
            }
        }

        instance.ObjectList = temp_objs_list;

        instance.Resource = await BuildStoryboardResource(instance.ObjectList, selectDirPath);

        return instance;
    }

    private StoryboardInfo ParseStoryboardInfo(BeatmapFolderInfoEx instanceInfoEx)
    {
        string stringOr(params string[] stringList)
        {
            foreach (var str in stringList)
                if (!string.IsNullOrWhiteSpace(str))
                    return str;
            return string.Empty;
        }

        var storyboardInfo = new StoryboardInfo();
        storyboardInfo.Title = "<n/a>";
        storyboardInfo.Artist = "<n/a>";
        storyboardInfo.DifficultyName = "<n/a>";
        storyboardInfo.Creator = "<n/a>";
        storyboardInfo.Source = "<n/a>";
        storyboardInfo.BeatmapId = -1;
        storyboardInfo.BeatmapSetId = -1;

        var osuFilePath = instanceInfoEx.osu_file_path;
        if (File.Exists(osuFilePath))
            try
            {
                var osuReader = new OsuFileReader(osuFilePath);
                var sectionReader = new SectionReader(Section.Metadata, osuReader);

                string readProp(string str)
                {
                    return sectionReader.ReadProperty(str);
                }

                storyboardInfo.Title = stringOr(readProp("TitleUnicode"), readProp("Title"), "n/a");
                storyboardInfo.Artist = stringOr(readProp("ArtistUnicode"), readProp("Artist"), "n/a");
                storyboardInfo.Creator = stringOr(readProp("Creator"), "n/a");
                storyboardInfo.DifficultyName = stringOr(readProp("Version"), "n/a");
                storyboardInfo.Source = stringOr(readProp("Source"), "n/a");
                storyboardInfo.BeatmapId = int.Parse(stringOr(readProp("BeatmapID"), "-1"));
                storyboardInfo.BeatmapSetId = int.Parse(stringOr(readProp("BeatmapSetID"), "-1"));
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, $"can't open/parse osu file: {osuFilePath}, message: {e.Message}");
            }

        return storyboardInfo;
    }

    private ValueTask<IStoryboardResource> BuildStoryboardResource(
        IEnumerable<StoryboardObject> storyboardObjectList, string folder_path)
    {
        Dictionary<string, DesktopSpriteResource> CacheDrawSpriteInstanceMap = new();

        var resource = new DesktopStoryboardResource();

        foreach (var obj in storyboardObjectList)
        {
            DesktopSpriteResource group;
            switch (obj)
            {
                case StoryboardBackgroundObject background:
                    if (!_get(obj.ImageFilePath.ToLower(), out group))
                        Log.Warn($"not found image:{obj.ImageFilePath}");

                    if (group != null)
                        background.AdjustScale(group.Image.Height);

                    break;

                case StoryboardAnimation animation:
                    List<DesktopSpriteResource> list = new();

                    for (var index = 0; index < animation.FrameCount; index++)
                    {
                        var path = animation.FrameBaseImagePath + index + animation.FrameFileExtension;
                        if (!_get(path, out group))
                        {
                            Log.Warn($"not found image:{path}");
                            continue;
                        }

                        list.Add(group);
                    }

                    break;

                default:
                    if (!_get(obj.ImageFilePath.ToLower(), out group))
                        Log.Warn($"not found image:{obj.ImageFilePath}");
                    break;
            }
        }

        resource.PinSpriteInstanceGroups(CacheDrawSpriteInstanceMap);

        return ValueTask.FromResult<IStoryboardResource>(resource);

        bool _get(string image_name, out DesktopSpriteResource group)
        {
            var fix_image = image_name;
            //for Flex
            if (string.IsNullOrWhiteSpace(Path.GetExtension(fix_image)))
                fix_image += ".png";

            if (CacheDrawSpriteInstanceMap.TryGetValue(image_name, out group))
                return true;

            //load
            var file_path = Path.Combine(folder_path, fix_image);

            if (!_load_tex(file_path, out var tex))
            {
                file_path = Path.Combine( /*PlayerSetting.UserSkinPath ?? */string.Empty, fix_image);

                if (!_load_tex(file_path, out tex))
                    if (!image_name.EndsWith("-0") && _get(image_name + "-0", out group))
                        return true;
            }

            if (tex != null)
            {
                group = CacheDrawSpriteInstanceMap[image_name] =
                    new DesktopSpriteResource(fix_image, tex);
                Log.Debug($"Created Storyboard sprite instance from image file :{fix_image}");
            }

            return group != null;
        }

        bool _load_tex(string file_path, out SKImage texture)
        {
            texture = null;

            try
            {
                if (!File.Exists(file_path))
                    return false;
                texture = SKImage.FromEncodedData(file_path);
            }
            catch (Exception e)
            {
                Log.Warn($"Load texture \"{file_path}\" failed : {e.Message}");
                texture = null;
            }

            return texture != null;
        }
    }
}