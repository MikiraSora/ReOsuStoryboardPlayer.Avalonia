using Microsoft.Extensions.Logging;

namespace ReOsuStoryboardPlayer.Avalonia.Android.Utils.Logging;

/// <summary>
/// 提供 LogcatLogger 的 LoggerProvider
/// </summary>
public class LogcatLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new LogcatLogger(categoryName);
    }

    public void Dispose()
    {
        // 无需释放资源
    }
}

/// <summary>
/// 扩展方法方便注册
/// </summary>
public static class LogcatLoggerExtensions
{
    public static ILoggingBuilder AddLogcat(this ILoggingBuilder builder)
    {
        builder.AddProvider(new LogcatLoggerProvider());
        return builder;
    }
}