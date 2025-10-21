using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ReOsuStoryboardPlayer.Avalonia.Android.Utils.Logging;

/// <summary>
/// 输出日志到 Android Logcat
/// </summary>
public class LogcatLogger : ILogger
{
    private readonly string _category;

    public LogcatLogger(string category)
    {
        _category = "ReOsuStoryboardPlayer.Avalonia." + category;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
            return;

        // 附带异常信息
        if (exception != null)
            message += "\n" + exception;

        // 映射到 Android.Util.Log
        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                global::Android.Util.Log.Debug(_category, message);
                break;
            case LogLevel.Information:
                global::Android.Util.Log.Info(_category, message);
                break;
            case LogLevel.Warning:
                global::Android.Util.Log.Warn(_category, message);
                break;
            case LogLevel.Error:
                global::Android.Util.Log.Error(_category, message);
                break;
            case LogLevel.Critical:
                global::Android.Util.Log.Wtf(_category, message);
                break;
            default:
                global::Android.Util.Log.Info(_category, message);
                break;
        }
    }
}
