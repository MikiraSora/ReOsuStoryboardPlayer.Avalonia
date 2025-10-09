using System.Text.Json;

namespace ReOsuStoryboardPlayer.Avalonia.Utils;

public static class JsonHelper
{
    public static T CopyNew<T>(T obj)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj));
    }
}