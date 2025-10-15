using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Networks.DefaultImpl;

[Injectio.Attributes.RegisterSingleton<IApplicationHttpFactory>]
internal class DefaultApplicationHttpFactory : IApplicationHttpFactory
{
    private HttpClient client;
    private readonly HttpClientHandler handler = new();

    public DefaultApplicationHttpFactory()
    {
        client = CreateClient();
    }

    public void ResetAll()
    {
        client = CreateClient();
    }

    public CookieContainer Cookies
    {
        get => handler.CookieContainer;
        set => handler.CookieContainer = value;
    }

    public async ValueTask<HttpResponseMessage> SendAsync(string url,
        Action<HttpRequestMessage> customizeRequestCallback = default, CancellationToken cancellation = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        customizeRequestCallback?.Invoke(req);

        var beginTime = DateTime.Now;
        try
        {
            var resp = await client.SendAsync(req, cancellation);
            return resp;
        }
        finally
        {
            var endTime = DateTime.Now;
            var duration = endTime - beginTime;
        }
    }

    private HttpClient CreateClient()
    {
        var http = new HttpClient(handler);
        return http;
    }
}