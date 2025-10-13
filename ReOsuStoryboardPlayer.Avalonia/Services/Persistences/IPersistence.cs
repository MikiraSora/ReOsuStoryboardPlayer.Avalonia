using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Persistences;

public interface IPersistence
{
    Task Save<T>(T obj);
    Task<T> Load<T>() where T : new();
}