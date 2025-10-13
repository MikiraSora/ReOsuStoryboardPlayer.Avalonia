using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    private static readonly Regex regex = new(@"\[(.*)\]\.osu", RegexOptions.IgnoreCase);

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

        var info = ParseForBrowser(fsRoot, folder_path);

        info.osu_file_path = folder_path;

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
            using var fs = await BrowserSimpleIO.OpenRead(fsRoot, info.osu_file_path);
            info.reader = new OsuFileReader(fs);
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

        if (string.IsNullOrWhiteSpace(info.osu_file_path) || !BrowserSimpleIO.ExistFile(fsRoot, info.osu_file_path))
            Log.Warn("No .osu load");

        if (string.IsNullOrWhiteSpace(info.audio_file_path) || !BrowserSimpleIO.ExistFile(fsRoot, info.audio_file_path))
            throw new Exception("Audio file not found.");

        return info;
    }

    private static BeatmapFolderInfoEx ParseForBrowser(ISimpleDirectory fsRoot, string folder_path)
    {
        if (!BrowserSimpleIO.ExistDirectory(fsRoot, folder_path))
            throw new Exception($"\"{folder_path}\" not a folder!");

        var info = new BeatmapFolderInfoEx();

        foreach (var osu_file in TryGetAnyFiles(".osu"))
        {
            var match = regex.Match(osu_file);

            if (!match.Success)
                continue;

            info.DifficultFiles[match.Groups[1].Value] = osu_file;
        }

        info.osb_file_path = TryGetAnyFiles(".osb").FirstOrDefault();

        info.folder_path = folder_path;

        if (!(info.DifficultFiles.All(x => _check(x.Value)) || _check(info.osb_file_path)))
            throw new Exception("missing files such as .osu/.osb and audio file which is registered in .osu");

        return info;

        bool _check(string file_path)
        {
            return !string.IsNullOrWhiteSpace(file_path) && BrowserSimpleIO.ExistFile(fsRoot, file_path);
        }

        IEnumerable<string> TryGetAnyFiles(string extend_name)
        {
            return BrowserSimpleIO.GetFilePaths(fsRoot, folder_path, "*" + extend_name);
        }
    }
}