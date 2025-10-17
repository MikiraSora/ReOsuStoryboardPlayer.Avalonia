using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;
using ReOsuStoryboardPlayer.Core.Base;
using ReOsuStoryboardPlayer.Core.Optimzer;
using ReOsuStoryboardPlayer.Core.Optimzer.DefaultOptimzer;
using ReOsuStoryboardPlayer.Core.Parser.Collection;
using ReOsuStoryboardPlayer.Core.Parser.Reader;
using ReOsuStoryboardPlayer.Core.Parser.Stream;
using ReOsuStoryboardPlayer.Core.Serialization;
using ReOsuStoryboardPlayer.Core.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.Utils;

public static class StoryboardParserHelper
{
    private static bool optimzer_add;

    public static async Task<List<StoryboardObject>> GetStoryboardObjects(ISimpleDirectory fsRoot, string file_path)
    {
        using (StopwatchRun.Count($"Parse&Optimze Storyboard Objects/Commands from {file_path}"))
        {
            if (Path.GetExtension(file_path).ToLower() == ".osbin")
                return await GetStoryboardObjectsFromOsbin(fsRoot, file_path);
            return await GetStoryboardObjectsFromOsb(fsRoot, file_path);
        }
    }

    private static async Task<List<StoryboardObject>> GetStoryboardObjectsFromOsbin(ISimpleDirectory fsRoot,
        string path)
    {
        using (var stream = await SimpleIO.OpenRead(fsRoot, path))
        {
            return StoryboardBinaryFormatter.Deserialize(stream).ToList();
        }
    }

    private static async Task<List<StoryboardObject>> GetStoryboardObjectsFromOsb(ISimpleDirectory fsRoot, string path)
    {
        using var stream = await SimpleIO.OpenRead(fsRoot, path);
        var reader = new OsuFileReader(stream);

        var collection = new VariableCollection(new VariableReader(reader).EnumValues());

        var er = new EventReader(reader, collection);

        var StoryboardReader = new StoryboardReader(er);

        List<StoryboardObject> list;

        list = StoryboardReader.EnumValues().ToList();
        list.RemoveAll(c => c == null);

        foreach (var obj in list)
            obj.CalculateAndApplyBaseFrameTime();

        InitOptimzerManager();

        StoryboardOptimzerManager.Optimze( /*PlayerSetting.StoryboardObjectOptimzeLevel*/2857, list);

        return list;
    }

    private static void InitOptimzerManager()
    {
        if (!optimzer_add)
            return;
/*
        try
        {
            var base_type = typeof(OptimzerBase);
            var need_load_optimzer = AppDomain.CurrentDomain.GetAssemblies()
                .Select(x => x.GetTypes())
                .SelectMany(l => l)
                .Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(base_type)).Select(x =>
                {
                    try
                    {
                        return Activator.CreateInstance(x);
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Can't load optimzer \"{x.Name}\" :" + e.Message);
                        return null;
                    }
                }).OfType<OptimzerBase>();

            foreach (var optimzer in need_load_optimzer)
        }
        catch (Exception e)
        {
            Log.Warn($"InitOptimzerManager() throw exception: {e.Message}");
        }
*/
        StoryboardOptimzerManager.AddOptimzer(new RuntimeOptimzer());
        StoryboardOptimzerManager.AddOptimzer(new ParserStaticOptimzer());
        StoryboardOptimzerManager.AddOptimzer(new ConflictCommandRecoverOptimzer());
        optimzer_add = true;
    }
}