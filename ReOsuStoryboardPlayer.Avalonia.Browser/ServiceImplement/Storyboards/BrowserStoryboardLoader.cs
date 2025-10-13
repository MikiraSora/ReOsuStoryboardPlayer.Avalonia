using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.Browser.ViewModels.Dialogs;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Core.Base;
using ReOsuStoryboardPlayer.Core.Parser;
using ReOsuStoryboardPlayer.Core.Parser.Reader;
using ReOsuStoryboardPlayer.Core.Parser.Stream;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards;

[RegisterInjectable(typeof(IStoryboardLoader), ServiceLifetime.Singleton)]
public class BrowserStoryboardLoader(
    ILogger<BrowserStoryboardLoader> logger,
    IDialogManager dialogManager,
    IParameterManager parameterManager)
    : IStoryboardLoader
{
    private readonly IParameterManager parameterManager = parameterManager;

    public async ValueTask<IStoryboardInstance> OpenLoaderDialog()
    {
        var openDialog = await dialogManager.ShowDialog<BrowserOpenStoryboardDialogViewModel>();
        return openDialog.SelectedStoryboardInstance;
    }

    public async ValueTask<IStoryboardInstance> OpenLoaderFromZipFileBytes(byte[] zipFileBytes)
    {
        var fsRoot = BrowserFileSystemBuilder.LoadFromZipFileBytes(zipFileBytes);
        return await LoadStoryboardInstance(fsRoot);
    }

    public async ValueTask<IStoryboardInstance> OpenLoaderFromLocalFileSystem(
        LocalFileSystemInterop.JSDirectory jsDirRoot)
    {
        var fsRoot = BrowserFileSystemBuilder.LoadFromLocalFileSystem(jsDirRoot);
        return await LoadStoryboardInstance(fsRoot);
    }

    private void dumpDirectory(StringBuilder sb, ISimpleDirectory fsRoot, int tabCount = 0)
    {
        if (fsRoot == null)
            return;

        var indent = new string(' ', tabCount * 4);
        sb.AppendLine($"{indent}[DIR] {fsRoot.DirectoryName}");
        sb.AppendLine();

        // 打印所有文件
        if (fsRoot.ChildFiles?.Length > 0)
        {
            foreach (var file in fsRoot.ChildFiles)
                sb.AppendLine($"{indent}  - {file.FileName} ({file.FileLength} bytes)");
            sb.AppendLine();
        }

        // 递归打印子目录
        if (fsRoot.ChildDictionaries != null)
            foreach (var subDir in fsRoot.ChildDictionaries)
                dumpDirectory(sb, subDir, tabCount + 1);
    }

    private async ValueTask<IStoryboardInstance> LoadStoryboardInstance(ISimpleDirectory fsRoot)
    {
        logger.LogInformationEx($"fsRoot loaded: {fsRoot}");

        var sb = new StringBuilder();
        dumpDirectory(sb, fsRoot);
        logger.LogDebugEx(sb.ToString());

        var instance = BrowserStoryboardInstance.CreateInstance();
        instance.StoryboardFileSystemRootDirectory = fsRoot;
        logger.LogInformationEx($"instance loaded: {instance}.");

        instance.InfoEx = await BeatmapFolderInfoEx.Parse(fsRoot, string.Empty, parameterManager.Parameters);
        if (instance.Info == null)
        {
            logger?.LogErrorEx("can't create BeatmapFolderInfo");
            return default;
        }

        logger.LogInformationEx($"instance.Info.osb_file_path: {instance.Info.osb_file_path}");
        logger.LogInformationEx($"instance.Info.folder_path: {instance.Info.folder_path}");
        logger.LogInformationEx($"instance.Info.IsWidescreenStoryboard: {instance.Info.IsWidescreenStoryboard}");
        foreach (var pair in instance.Info.DifficultFiles)
            logger.LogInformationEx($"instance.Info.DifficultFiles[{pair.Key}]: {pair.Value}");
        logger.LogInformationEx($"instance.InfoEx.audio_file_path: {instance.InfoEx.audio_file_path}");
        logger.LogInformationEx($"instance.InfoEx.osu_file_path: {instance.InfoEx.osu_file_path}");

        instance.StoryboardInfo = await ParseStoryboardInfo(fsRoot, instance.InfoEx);
        logger.LogInformationEx($"instance.StoryboardInfo.Title: {instance.StoryboardInfo.Title}");
        logger.LogInformationEx($"instance.StoryboardInfo.Artist: {instance.StoryboardInfo.Artist}");
        logger.LogInformationEx($"instance.StoryboardInfo.Creator: {instance.StoryboardInfo.Creator}");
        logger.LogInformationEx($"instance.StoryboardInfo.Source: {instance.StoryboardInfo.Source}");
        logger.LogInformationEx($"instance.StoryboardInfo.DifficultyName: {instance.StoryboardInfo.DifficultyName}");
        logger.LogInformationEx($"instance.StoryboardInfo.BeatmapId: {instance.StoryboardInfo.BeatmapId}");
        logger.LogInformationEx($"instance.StoryboardInfo.BeatmapSetId: {instance.StoryboardInfo.BeatmapSetId}");

        List<StoryboardObject> temp_objs_list = new(), parse_osb_Storyboard_objs = new();

        //get objs from osu file
        var parse_osu_Storyboard_objs = string.IsNullOrWhiteSpace(instance.InfoEx.osu_file_path)
            ? new List<StoryboardObject>()
            : await StoryboardParserHelper.GetStoryboardObjects(fsRoot, instance.InfoEx.osu_file_path);
        AdjustZ(parse_osu_Storyboard_objs);

        if (!string.IsNullOrWhiteSpace(instance.InfoEx.osb_file_path) &&
            BrowserSimpleIO.ExistFile(fsRoot, instance.InfoEx.osb_file_path))
        {
            parse_osb_Storyboard_objs =
                await StoryboardParserHelper.GetStoryboardObjects(fsRoot, instance.InfoEx.osb_file_path);
            AdjustZ(parse_osb_Storyboard_objs);
        }

        logger.LogInformationEx(".osb/.osu storyboard loaded.");

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
        logger.LogInformationEx($"loaded {instance.ObjectList.Count} storybaord objects totally.");

        instance.Resource = await BuildStoryboardResource(fsRoot, instance.ObjectList, string.Empty);
        logger.LogInformationEx("BuildStoryboardResource() successfully");

        return instance;
    }

    private async Task<StoryboardInfo> ParseStoryboardInfo(ISimpleDirectory fsRoot, BeatmapFolderInfoEx instanceInfoEx)
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
        if (BrowserSimpleIO.ExistFile(fsRoot, osuFilePath))
            try
            {
                using var fs = await BrowserSimpleIO.OpenRead(fsRoot, osuFilePath);
                var osuReader = new OsuFileReader(fs);
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

    private async ValueTask<IStoryboardResource> BuildStoryboardResource(ISimpleDirectory fsRoot,
        IEnumerable<StoryboardObject> storyboardObjectList, string folder_path)
    {
        Dictionary<string, BrowserSpriteResource> CacheDrawSpriteInstanceMap = new();

        var resource = new BrowserStoryboardResource();

        foreach (var obj in storyboardObjectList)
            switch (obj)
            {
                case StoryboardBackgroundObject background:
                    var group = await _get(obj.ImageFilePath.ToLower());

                    if (group != null)
                        background.AdjustScale(group.Image.Height);
                    else
                        logger.LogWarningEx($"not found image:{obj.ImageFilePath}");

                    break;

                case StoryboardAnimation animation:
                    List<BrowserSpriteResource> list = new();

                    for (var index = 0; index < animation.FrameCount; index++)
                    {
                        var path = animation.FrameBaseImagePath + index + animation.FrameFileExtension;
                        if (await _get(path) is not BrowserSpriteResource group2)
                        {
                            logger.LogWarningEx($"not found image:{path}");
                            continue;
                        }

                        list.Add(group2);
                    }

                    break;

                default:
                    var group3 = await _get(obj.ImageFilePath.ToLower());
                    if (group3 is null)
                        logger.LogWarningEx($"not found image:{obj.ImageFilePath}");
                    break;
            }

        resource.PinSpriteInstanceGroups(CacheDrawSpriteInstanceMap);

        return resource;

        async Task<BrowserSpriteResource> _get(string image_name)
        {
            var fix_image = image_name;
            //for Flex
            if (string.IsNullOrWhiteSpace(Path.GetExtension(fix_image)))
                fix_image += ".png";

            if (CacheDrawSpriteInstanceMap.TryGetValue(image_name, out var group))
                return group;

            //load
            var file_path = Path.Combine(folder_path, fix_image);

            var tex = await _load_tex(file_path);
            if (tex == null)
            {
                file_path = Path.Combine( /*PlayerSetting.UserSkinPath ?? */string.Empty, fix_image);

                tex = await _load_tex(file_path);
                if (tex == null)
                    if (!image_name.EndsWith("-0") && await _get(image_name + "-0") is BrowserSpriteResource group2)
                        return group2;
            }

            if (tex != null)
            {
                group = CacheDrawSpriteInstanceMap[image_name] =
                    new BrowserSpriteResource(fix_image, tex);
                logger.LogDebugEx($"Created Storyboard sprite instance from image file :{fix_image}");
            }

            return group;
        }

        async Task<SKImage> _load_tex(string file_path)
        {
            try
            {
                if (!BrowserSimpleIO.ExistFile(fsRoot, file_path))
                    return null;
                using var fs = await BrowserSimpleIO.OpenRead(fsRoot, file_path);
                return SKImage.FromEncodedData(fs);
            }
            catch (Exception e)
            {
                logger.LogWarningEx($"Load texture \"{file_path}\" failed : {e.Message}");
                return null;
            }
        }
    }
}