using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.ViewModels.Pages;

namespace ReOsuStoryboardPlayer.Avalonia.Services.Navigation;

[Injectio.Attributes.RegisterSingleton<IPageNavigationManager>]
public partial class DefaultPageNavigationManager : ObservableObject, IPageNavigationManager
{
    private readonly ILogger<DefaultPageNavigationManager> logger;
    private readonly ViewModelFactory viewModelFactory;
    private readonly Stack<PageViewModelBase> pageViewModelStack = new();

    [ObservableProperty]
    private PageViewModelBase currentPageViewModel;

    public DefaultPageNavigationManager(ILogger<DefaultPageNavigationManager> logger,ViewModelFactory viewModelFactory)
    {
        this.logger = logger;
        this.viewModelFactory = viewModelFactory;
    }

    public async Task<T> SetPage<T>(bool cleanNavigationStack = false)
        where T : PageViewModelBase
    {
        var page = viewModelFactory.CreateViewModel<T>();
        await SetPage(page, cleanNavigationStack);
        return page;
    }

    public Task SetPage(PageViewModelBase pageViewModel, bool cleanNavigationStack = false)
    {
        ArgumentNullException.ThrowIfNull(pageViewModel);

        if (cleanNavigationStack)
            pageViewModelStack.Clear();

        pageViewModelStack.Clear();
        pageViewModelStack.Push(pageViewModel);
        CurrentPageViewModel = pageViewModel;

        logger.LogInformationEx(
            $"set page {pageViewModel.Title}({pageViewModel.GetType().FullName}), cleanNavigationStack: {cleanNavigationStack}");
        return Task.CompletedTask;
    }

    public async Task<T> PushPage<T>() where T : PageViewModelBase
    {
        var page = viewModelFactory.CreateViewModel<T>();
        await PushPage(page);
        return page;
    }

    public Task PushPage(PageViewModelBase pageViewModel)
    {
        pageViewModelStack.Push(pageViewModel);
        CurrentPageViewModel = pageViewModel;

        logger.LogInformationEx($"push page {pageViewModel.Title}({pageViewModel.GetType().FullName})");
        return Task.CompletedTask;
    }

    public Task<bool> TryPopPage()
    {
        if (pageViewModelStack.Count <= 1)
        {
            logger.LogWarningEx("Cannot pop page when stack count <= 1");
            return Task.FromResult(false);
        }

        var current = pageViewModelStack.Pop();

        var previous = pageViewModelStack.Peek();
        CurrentPageViewModel = previous;

        logger.LogInformationEx(
            $"pop page {current.Title}({current.GetType().FullName}), current page {CurrentPageViewModel.Title}({CurrentPageViewModel.GetType().FullName})");
        return Task.FromResult(true);
    }

    public Task<bool> TryPopToPage<T>() where T : PageViewModelBase
    {
        var target = pageViewModelStack.LastOrDefault(x => x is T);
        if (target == null)
        {
            logger.LogWarningEx($"request page type:{typeof(T).FullName} not found");
            return Task.FromResult(false);
        }

        while (pageViewModelStack.Peek() != target)
        {
            var popped = pageViewModelStack.Pop();
            logger.LogInformationEx($"pop page {popped.Title}({popped.GetType().FullName})");
        }

        CurrentPageViewModel = target;

        logger.LogInformationEx(
            $"current page {CurrentPageViewModel.Title}({CurrentPageViewModel.GetType().FullName})");
        return Task.FromResult(true);
    }
}