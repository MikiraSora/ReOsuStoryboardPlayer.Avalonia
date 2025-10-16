using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.Logging;
using ReOsuStoryboardPlayer.Avalonia.Utils;
using ReOsuStoryboardPlayer.Avalonia.Utils.MethodExtensions;
using ReOsuStoryboardPlayer.Avalonia.ViewModels;

namespace ReOsuStoryboardPlayer.Avalonia;

internal class ViewLocator : IDataTemplate
{
    private readonly ILogger<ViewLocator> logger;
    private readonly ViewFactory viewFactory;

    public ViewLocator(ILogger<ViewLocator> logger, ViewFactory viewFactory)
    {
        this.logger = logger;
        this.viewFactory = viewFactory;
    }

    public Control Build(object d)
    {
        if (d is not ViewModelBase viewModel)
        {
            logger.LogErrorEx($"viewModel is null: {new Exception().StackTrace}");
            return null;
        }

        logger.LogDebugEx($"viewModel fullName: {viewModel.GetType().FullName}");
        var viewTypeName = GetViewTypeName(viewModel.GetType());
        logger.LogDebugEx($"viewTypeName: {viewTypeName}");

        //create new view
        var view = viewFactory.CreateView(viewTypeName);
        if (view == null)
        {
            var msg = $"<resolve type object {viewTypeName} failed; model type:{viewModel.GetType().FullName}>";
#if DEBUG
            throw new Exception(msg);
#else
				return new TextBlock { Text = msg };
#endif
        }

        view.Loaded += (a, aa) => { viewModel.OnViewAfterLoaded(view); };
        view.Unloaded += (a, aa) =>
        {
            viewModel.OnViewBeforeUnload(view);
            view.DataContext = null;
        };

        view.DataContext = viewModel;
        return view;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Match(object data)
    {
        return data is ViewModelBase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Create(object viewModel)
    {
        return Build(viewModel);
    }

    private string GetViewTypeName(Type viewModelType)
    {
        if (viewModelType is null)
            return null;
        var name = string.Join(".", viewModelType.FullName.Split(".").Select(x =>
        {
            if (x == "ViewModels")
                return "Views";
            if (x.Length > "ViewModel".Length && x.EndsWith("ViewModel"))
                return x.Substring(0, x.Length - "Model".Length);
            return x;
        }));
        return name;
    }
}