using System;
using System.Web;
using Injectio.Attributes;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Browser.Utils;
using ReOsuStoryboardPlayer.Avalonia.Services.Parameters;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;

namespace ReOsuStoryboardPlayer.Avalonia.Browser.ServiceImplement.Parameters;

[RegisterSingleton<IParameterManager>]
public class BrowserParameterManager : IParameterManager
{
    private readonly ILogger<BrowserParameters> logger;

    public BrowserParameterManager(ILogger<BrowserParameters> logger)
    {
        this.logger = logger;
        var parameters = new BrowserParameters();

        try
        {
            var href = MiscInterop.GetHref();
            logger.LogInformationEx($"href: {href}");

            if (string.IsNullOrEmpty(href))
                return;

            var uri = new Uri(href);

            logger.LogInformationEx($"queryString: {uri.Query}");
            var query = HttpUtility.ParseQueryString(uri.Query);

            foreach (var key in query.AllKeys)
            {
                var value = query[key];
                logger.LogDebugEx($"Key: {key}, Value: {value}");

                if (string.IsNullOrWhiteSpace(key))
                {
                    var splitValues = (value ?? string.Empty).Split(',');
                    logger.LogDebugEx($"splited values: {string.Join(" | ", splitValues)}");
                    parameters.FreeArgs.AddRange(splitValues);
                }
                else
                {
                    parameters.Args[key] = value ?? "";
                    parameters.SimpleArgs.Add(key);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogErrorEx(e, "Failed to parse browser query string: " + e.Message);
        }

        Parameters = parameters;
    }

    public IParameters Parameters { get; }
}