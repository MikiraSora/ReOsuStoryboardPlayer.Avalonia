using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Injectio.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Services.Dialog;
using ReOsuStoryboardPlayer.Avalonia.Services.Persistences;
using ReOsuStoryboardPlayer.Avalonia.Utils;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Android.ServiceImplement.Persistences;

[RegisterSingleton<IPersistence>]
public class AndroidPersistence : IPersistence
{
    private readonly ILogger<AndroidPersistence> logger;
    private readonly IServiceProvider serviceProvider;

    public AndroidPersistence(ILogger<AndroidPersistence> logger, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    private string GetFilePath<T>()
    {
        // 使用应用专属文件夹
        var context = global::Android.App.Application.Context;
        var dir = Path.Combine(context.FilesDir.AbsolutePath, "config"); // /data/user/0/com.example.myapp/files
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, typeof(T).Name + ".json");
    }

    public async Task<T> Load<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        JsonTypeInfo<T> jsonTypeInfo) where T : new()
    {
        var path = GetFilePath<T>();
        if (!File.Exists(path))
            return serviceProvider.Resolve<T>();

        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync(fs, jsonTypeInfo) ?? serviceProvider.Resolve<T>();
        }
        catch (Exception ex)
        {
            logger.LogErrorEx(ex, $"Load error: {ex}");
            return serviceProvider.Resolve<T>();
        }
    }

    public async Task Save<T>(T obj, JsonTypeInfo<T> jsonTypeInfo)
    {
        var path = GetFilePath<T>();
        try
        {
            await using var fs = File.OpenWrite(path);
            await JsonSerializer.SerializeAsync(fs, obj, jsonTypeInfo);
            await fs.FlushAsync();
        }
        catch (Exception ex)
        {
            logger.LogErrorEx(ex, $"Save error: {ex}");
        }
    }
}