using ReOsuStoryboardPlayer.Avalonia.Services.Storyboards;
using ReOsuStoryboardPlayer.Avalonia.Utils.SimpleFileSystem;
using System;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Plaform;

public interface IPlatform
{
    public bool SupportMultiThread { get; }
}