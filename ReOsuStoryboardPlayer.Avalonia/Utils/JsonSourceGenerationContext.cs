using System.Text.Json.Serialization;
using ReOsuStoryboardPlayer.Avalonia.Models;

namespace ReOsuStoryboardPlayer.Avalonia.Utils;

[JsonSerializable(typeof(StoryboardPlayerSetting))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
}