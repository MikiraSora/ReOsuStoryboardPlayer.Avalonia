using Injectio.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Utils;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Persistences;

[RegisterSingleton<IPersistence>]
public partial class DesktopPersistence : IPersistence
{
    private readonly Dictionary<string, object> cacheObj = new();
    private readonly IDialogManager dialogManager;
    private readonly Lock locker = new();
    private readonly ILogger<DesktopPersistence> logger;
    private readonly IServiceProvider provider;
    private readonly string savePath;

    private readonly JsonSerializerOptions serializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private Dictionary<string, string> settingMap;

    public DesktopPersistence(IServiceProvider provider, ILogger<DesktopPersistence> logger,
        IDialogManager dialogManager)
    {
        this.provider = provider;
        this.logger = logger;
        this.dialogManager = dialogManager;
        savePath = Path.Combine(Path.GetDirectoryName(System.AppContext.BaseDirectory) ?? string.Empty,
            "setting.json");
    }

    public async Task Save<T>(T obj, JsonTypeInfo<T> typeInfo)
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return;
#endif

        await Task.Run(() =>
        {
            lock (locker)
            {
                var key = GetKey<T>();

                settingMap[key] = JsonSerializer.Serialize(obj, typeInfo);
                var content = JsonSerializer.Serialize(settingMap,DictionaryJsonSerializerContext.Default.DictionaryStringString);

                File.WriteAllText(savePath, content);
            }
        });
    }

    public Task<T> Load<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(JsonTypeInfo<T> typeInfo) where T : new()
    {
        return Task.Run(() =>
        {
            lock (locker)
            {
                return LoadInternal<T>(typeInfo);
            }
        });
    }

    private T LoadInternal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(JsonTypeInfo<T> jsonTypeInfo)
    {
        var key = GetKey<T>();

        if (cacheObj.TryGetValue(key, out var obj))
        {
            logger.LogDebugEx($"return cached {typeof(T).Name} object, hash = {obj.GetHashCode()}");
            return (T) obj;
        }
        if (settingMap is null)
        {
            if (File.Exists(savePath))
            {
                var content = File.ReadAllText(savePath);
                if (string.IsNullOrWhiteSpace(content))
                    settingMap = new Dictionary<string, string>();
                else
                    try
                    {
                        settingMap = JsonSerializer.Deserialize<Dictionary<string, string>>(content, DictionaryJsonSerializerContext.Default.DictionaryStringString);
                    }
                    catch (Exception e)
                    {
                        logger.LogErrorEx(e, $"Can't load setting.json : {e.Message}");
                        Task.Run(async () =>
                        {
                            await dialogManager.ShowMessageDialog($"无法加载应用配置文件setting.json:{e.Message}",
                                DialogMessageType.Error);
                            Environment.Exit(-1);
                        }).Wait();
                    }
            }
            else
            {
                settingMap = new Dictionary<string, string>();
            }
        }

        T cw = default;
        if (settingMap.TryGetValue(key, out var jsonContent))
        {
            cw = JsonSerializer.Deserialize<T>(jsonContent,jsonTypeInfo);
            logger.LogDebugEx($"create new {typeof(T).Name} object from setting.json, hash = {cw.GetHashCode()}");
        }
        else
        {
            cw = ActivatorUtilities.CreateInstance<T>(provider);
            logger.LogDebugEx(
                $"create new {typeof(T).Name} object from ActivatorUtilities.CreateInstance(), hash = {cw.GetHashCode()}");
        }

        cacheObj[key] = cw;
        return cw;
    }

    private static string GetKey<T>()
    {
        return typeof(T).FullName;
    }

    [JsonSerializable(typeof(Dictionary<string, string>))]
    internal partial class DictionaryJsonSerializerContext : JsonSerializerContext
    {

    }
}