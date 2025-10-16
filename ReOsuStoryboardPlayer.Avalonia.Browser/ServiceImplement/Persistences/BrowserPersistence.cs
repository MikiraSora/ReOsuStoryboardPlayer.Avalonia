using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Persistences;

[RegisterSingleton<IPersistence>]
public class BrowserPersistence : IPersistence
{
    private const string persistenceStoreKey = "__browserPersistence";
    private readonly Dictionary<string, object> cacheObj = new();
    private readonly ILogger<BrowserPersistence> logger;
    private readonly IServiceProvider provider;

    private Dictionary<string, string> settingMap;

    public BrowserPersistence(IServiceProvider provider, ILogger<BrowserPersistence> logger)
    {
        this.provider = provider;
        this.logger = logger;
    }

    public Task<T> Load<T>(JsonTypeInfo<T> jsonTypeInfo) where T : new()
    {
        return LoadInternal(jsonTypeInfo);
    }

    public async Task Save<T>(T obj, JsonTypeInfo<T> jsonTypeInfo)
    {
        var key = GetKey<T>();

        settingMap[key] = JsonSerializer.Serialize(obj, jsonTypeInfo);
        var content = JsonSerializer.Serialize(settingMap, JsonSourceGenerationContext.Default.DictionaryStringString);

        // Use localStorage for browser persistence
        await SetLocalStorage(persistenceStoreKey, content);
    }

    private async Task<T> LoadInternal<T>(JsonTypeInfo<T> jsonTypeInfo)
    {
        var key = GetKey<T>();

        if (cacheObj.TryGetValue(key, out var obj))
        {
            logger.LogDebugEx($"return cached {typeof(T).Name} object, hash = {obj.GetHashCode()}");
            return (T) obj;
        }

        if (settingMap is null)
        {
            var content = await GetLocalStorage(persistenceStoreKey);
            if (string.IsNullOrWhiteSpace(content))
                settingMap = new Dictionary<string, string>();
            else
                try
                {
                    settingMap = JsonSerializer.Deserialize(content,
                        JsonSourceGenerationContext.Default.DictionaryStringString);
                }
                catch (Exception e)
                {
                    logger.LogErrorEx(e, $"Can't load browser settings : {e.Message}");
                    settingMap = new Dictionary<string, string>();
                }
        }
        else
        {
            settingMap = new Dictionary<string, string>();
        }

        T cw;
        if (settingMap.TryGetValue(key, out var jsonContent))
        {
            cw = JsonSerializer.Deserialize(jsonContent, jsonTypeInfo);
            logger.LogDebugEx($"create new {typeof(T).Name} object from browser storage, hash = {cw.GetHashCode()}");
        }
        else
        {
            cw = provider.Resolve<T>();
            logger.LogDebugEx(
                $"create new {typeof(T).Name} object from ActivatorUtilities.CreateInstance(), hash = {cw.GetHashCode()}");
        }

        cacheObj[key] = cw;
        return cw;
    }

    private string GetKey<T>()
    {
        return typeof(T).FullName;
    }

    /* todo
    private async ValueTask SetLocalStorage(string key, string value)
    {
        var storageProvider = (App.Current as App)?.TopLevel?.StorageProvider;

        using var file = await storageProvider.OpenFileBookmarkAsync(key);
        using var fs = await file.OpenWriteAsync();
        using var writer = new StreamWriter(fs, Encoding.UTF8);

        await writer.WriteAsync(value);
        logger.LogDebugEx($"setting from storage {storageProvider.GetType().FullName}: {key} = {value}");
    }

    private async ValueTask<string> GetLocalStorage(string key)
    {
        var storageProvider = (App.Current as App)?.TopLevel?.StorageProvider;

        using var file = await storageProvider.OpenFileBookmarkAsync(key);
        if (file is null)
            return default;

        using var ms = new MemoryStream();
        using var fs = await file.OpenReadAsync();
        if (fs is null)
            return default;

        await fs.CopyToAsync(ms);

        logger.LogDebugEx($"getting from storage {storageProvider.GetType().FullName}: {key}");
        return Encoding.UTF8.GetString(ms.ToArray());
    }
    */

    private async ValueTask SetLocalStorage(string key, string value)
    {
        await LocalStorageInterop.Save(key, value);
        logger.LogDebugEx($"setting from storage {key} = {value}");
    }

    private async ValueTask<string> GetLocalStorage(string key)
    {
        var value = await LocalStorageInterop.Load(key);

        logger.LogDebugEx($"getting from storage {key} = {value}");
        return value;
    }
}