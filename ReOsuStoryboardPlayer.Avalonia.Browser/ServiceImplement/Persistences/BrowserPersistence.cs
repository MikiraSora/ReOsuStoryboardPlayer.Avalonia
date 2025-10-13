using System.Threading.Tasks;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Persistences;

[RegisterInjectable(typeof(IPersistence))]
public class BrowserPersistence : IPersistence
{
    public Task Save<T>(T obj)
    {
        //todo
        return Task.CompletedTask;
    }

    public Task<T> Load<T>() where T : new()
    {
        //todo
        return Task.FromResult(new T());
    }
}