using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Persistences;

public interface IPersistence
{
    Task Save<T>(T obj, JsonTypeInfo<T> jsonTypeInfo);
    Task<T> Load<T>(JsonTypeInfo<T> jsonTypeInfo) where T : new();
}