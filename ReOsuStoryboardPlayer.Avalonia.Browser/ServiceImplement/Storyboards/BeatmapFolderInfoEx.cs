using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards.FileSystem;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Core.Parser;
using ReOsuStoryboardPlayer.Core.Parser.Reader;
using ReOsuStoryboardPlayer.Core.Parser.Stream;
using ReOsuStoryboardPlayer.Core.Utils;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Storyboards;

public class BeatmapFolderInfoEx : BeatmapFolderInfo
{
    protected BeatmapFolderInfoEx()
    {
    }

    public string osu_file_path { get; private set; }

    public string audio_file_path { get; protected set; }

    public static async Task<BeatmapFolderInfoEx> Parse(ISimpleDirectory fsRoot, string folder_path, IParameters args)
    {
        var explicitly_osu_diff_name = "";

        if (args != null && args.TryGetArg("diff", out var diff_name))
            explicitly_osu_diff_name = diff_name;

        var info = Parse<BeatmapFolderInfoEx>(folder_path);

        if (!string.IsNullOrWhiteSpace(explicitly_osu_diff_name))
        {
            var index = -1;
            index = int.TryParse(explicitly_osu_diff_name, out index) ? index : -1;

            if (index > 0)
                info.osu_file_path = info.DifficultFiles.OrderBy(x => x.Key).FirstOrDefault().Value;
            else
                info.osu_file_path = info.DifficultFiles.Where(x => x.Key.Contains(explicitly_osu_diff_name))
                    .OrderBy(x => x.Key.Length).FirstOrDefault().Value;
        }
        else
        {
            //优先选std铺面的.一些图其他模式谱面会有阻挡 53925 fripSide - Hesitation Snow
            foreach (var x in info.DifficultFiles)
            {
                var lines = await BrowserSimpleIO.ReadAllLines(fsRoot, x.Value);

                foreach (var line in lines)
                    if (line.StartsWith("Mode"))
                        try
                        {
                            var mode = line.Split(':').Last().ToInt();

                            if (mode == 0)
                                info.osu_file_path = x.Value;
                        }
                        catch
                        {
                        }
            }

            if (!BrowserSimpleIO.ExistFile(fsRoot, info.osu_file_path))
                info.osu_file_path = info.DifficultFiles.FirstOrDefault().Value;
        }

        if (!string.IsNullOrWhiteSpace(info.osu_file_path) && BrowserSimpleIO.ExistFile(fsRoot, info.osu_file_path))
        {
            info.reader = new OsuFileReader(info.osu_file_path);
            var section = new SectionReader(Section.General, info.reader);

            info.audio_file_path = Path.Combine(folder_path, section.ReadProperty("AudioFilename"));
            Log.User($"audio file path={info.audio_file_path}");

            var wideMatch = section.ReadProperty("WidescreenStoryboard");

            if (!string.IsNullOrWhiteSpace(wideMatch))
                info.IsWidescreenStoryboard = wideMatch.ToInt() == 1;
        }

        if (string.IsNullOrWhiteSpace(info.osu_file_path) || !BrowserSimpleIO.ExistFile(fsRoot, info.osu_file_path))
            info.audio_file_path = BrowserSimpleIO.GetFiles(fsRoot, info.folder_path, "*.mp3")
                .OrderByDescending(x => x.FileLength)
                .FirstOrDefault()
                ?.FullPath;

        if (string.IsNullOrWhiteSpace(info.osu_file_path) || !File.Exists(info.osu_file_path))
            Log.Warn("No .osu load");

        if (string.IsNullOrWhiteSpace(info.audio_file_path) || !File.Exists(info.audio_file_path))
            throw new Exception("Audio file not found.");

        return info;
    }
}