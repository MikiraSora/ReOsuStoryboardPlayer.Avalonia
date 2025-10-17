using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Skia;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Services.Render;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;
using ReOsuStoryboardPlayer.Core.Base;
using ReOsuStoryboardPlayer.Core.Parser;
using ReOsuStoryboardPlayer.Core.Parser.Reader;
using ReOsuStoryboardPlayer.Core.Parser.Stream;
using SkiaSharp;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;

[RegisterSingleton<StoryboardLoader>]
public class StoryboardLoader
{
    private readonly ILogger<StoryboardLoader> logger;
    private readonly IParameterManager parameterManager;
    private readonly IRenderManager renderManager;

    public StoryboardLoader(ILogger<StoryboardLoader> logger, IParameterManager parameterManager,
        IRenderManager renderManager)
    {
        this.logger = logger;
        this.parameterManager = parameterManager;
        this.renderManager = renderManager;
    }

    public async Task<StoryboardInstance> LoadStoryboard(ISimpleDirectory fsRoot)
    {
        logger.LogInformationEx($"fsRoot loaded: {fsRoot}");

        var sb = new StringBuilder();
        dumpDirectory(sb, fsRoot);
        logger.LogDebugEx(sb.ToString());

        var info = await BeatmapFolderInfoEx.Parse(fsRoot, string.Empty, parameterManager.Parameters);
        if (info == null)
        {
            logger?.LogErrorEx("can't create BeatmapFolderInfo");
            return default;
        }

        logger.LogInformationEx($"info.osb_file_path: {info.osb_file_path}");
        logger.LogInformationEx($"info.folder_path: {info.folder_path}");
        logger.LogInformationEx($"info.IsWidescreenStoryboard: {info.IsWidescreenStoryboard}");
        foreach (var pair in info.DifficultFiles)
            logger.LogInformationEx($"info.DifficultFiles[{pair.Key}]: {pair.Value}");
        logger.LogInformationEx($"infoEx.audio_file_path: {info.audio_file_path}");
        logger.LogInformationEx($"infoEx.osu_file_path: {info.osu_file_path}");

        var storyboardInfo = await ParseStoryboardInfo(fsRoot, info);
        logger.LogInformationEx($"storyboardInfo.Title: {storyboardInfo.Title}");
        logger.LogInformationEx($"storyboardInfo.Artist: {storyboardInfo.Artist}");
        logger.LogInformationEx($"storyboardInfo.Creator: {storyboardInfo.Creator}");
        logger.LogInformationEx($"storyboardInfo.Source: {storyboardInfo.Source}");
        logger.LogInformationEx($"storyboardInfo.DifficultyName: {storyboardInfo.DifficultyName}");
        logger.LogInformationEx($"storyboardInfo.BeatmapId: {storyboardInfo.BeatmapId}");
        logger.LogInformationEx($"storyboardInfo.BeatmapSetId: {storyboardInfo.BeatmapSetId}");

        List<StoryboardObject> temp_objs_list, parse_osb_Storyboard_objs = new();

        //get objs from osu file
        var parse_osu_Storyboard_objs = string.IsNullOrWhiteSpace(info.osu_file_path)
            ? new List<StoryboardObject>()
            : await StoryboardParserHelper.GetStoryboardObjects(fsRoot, info.osu_file_path);
        AdjustZ(parse_osu_Storyboard_objs);

        if (!string.IsNullOrWhiteSpace(info.osb_file_path) &&
            SimpleIO.ExistFile(fsRoot, info.osb_file_path))
        {
            parse_osb_Storyboard_objs =
                await StoryboardParserHelper.GetStoryboardObjects(fsRoot, info.osb_file_path);
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

        var objectList = temp_objs_list;
        logger.LogInformationEx($"loaded {objectList.Count} storybaord objects totally.");

        var resource = await BuildStoryboardResource(fsRoot, objectList, string.Empty);

        logger.LogInformationEx("rebuild skImage as gpu texture resources at render context...");
        renderManager.InvokeInRender(drawingContext =>
        {
            var apiLeaseFeature = drawingContext.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (apiLeaseFeature == null)
            {
                logger.LogWarningEx("rebuild resource failed: TryGetFeature<ISkiaSharpApiLeaseFeature>() is null.");
                return;
            }

            using var apiLease = apiLeaseFeature.Lease();
            var grContext = apiLease.SkSurface?.Context as GRContext;
            if (grContext == null)
            {
                logger.LogWarningEx("rebuild resource failed: grContext is null.");
                return;
            }

            foreach (var resourceKey in objectList.Select(x => x.ImageFilePath).Distinct())
            {
                var sprite = resource.GetSprite(resourceKey);
                if (sprite == null)
                {
                    logger.LogWarningEx($"rebuild resource failed: sprite is null, key: {resourceKey}");
                    continue;
                }
                var img = sprite.Image;
                if (img.IsTextureBacked)
                    continue;
                var textureImg = img.ToTextureImage(grContext);
                sprite.Image = textureImg;
                logger.LogInformationEx($"rebuild image as texture: {resourceKey}");
                img.Dispose();
            }
            logger.LogInformationEx($"rebuild skimage resources done");
        });

        logger.LogInformationEx("BuildStoryboardResource() successfully");

        return new StoryboardInstance(fsRoot, storyboardInfo, info, objectList, resource);
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
        if (SimpleIO.ExistFile(fsRoot, osuFilePath))
            try
            {
                using var fs = await SimpleIO.OpenRead(fsRoot, osuFilePath);
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

    private async ValueTask<StoryboardResource> BuildStoryboardResource(ISimpleDirectory fsRoot,
        IEnumerable<StoryboardObject> storyboardObjectList, string folder_path)
    {
        Dictionary<string, SpriteResource> CacheDrawSpriteInstanceMap = new();

        var resource = new StoryboardResource();

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
                    for (var index = 0; index < animation.FrameCount; index++)
                    {
                        var path = animation.FrameBaseImagePath + index + animation.FrameFileExtension;
                        if (await _get(path) is not SpriteResource)
                            logger.LogWarningEx($"not found image:{path}");
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

        async Task<SpriteResource> _get(string image_name)
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
                    if (!image_name.EndsWith("-0") && await _get(image_name + "-0") is SpriteResource group2)
                        return group2;
            }

            if (tex != null)
            {
                group = CacheDrawSpriteInstanceMap[image_name] =
                    new SpriteResource(fix_image, tex);
                logger.LogDebugEx($"Created Storyboard sprite instance from image file :{fix_image}");
            }

            return group;
        }

        async Task<SKImage> _load_tex(string file_path)
        {
            try
            {
                if (!SimpleIO.ExistFile(fsRoot, file_path))
                    return null;
                using var fs = await SimpleIO.OpenRead(fsRoot, file_path);
                var img = SKImage.FromEncodedData(fs);
                return img;
            }
            catch (Exception e)
            {
                logger.LogWarningEx($"Load texture \"{file_path}\" failed : {e.Message}");
                return null;
            }
        }
    }
}