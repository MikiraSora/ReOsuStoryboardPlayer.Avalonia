using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.Utils;

[JsonSerializable(typeof(LocalFileSystemInterop.JSDirectory))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{
}