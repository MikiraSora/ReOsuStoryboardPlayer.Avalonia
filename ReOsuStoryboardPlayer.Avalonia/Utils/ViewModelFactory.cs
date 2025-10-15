using Microsoft.Extensions.DependencyInjection;
using ReOsuStoryboardPlayer.Avalonia.Utils.Injections;
using ReOsuStoryboardPlayer.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Utils
{
    [Injectio.Attributes.RegisterSingleton<ViewModelFactory>]
    public class ViewModelFactory
    {
        private readonly IServiceProvider serviceProvider;

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public T CreateViewModel<T>() where T : ViewModelBase
        {
            return (T)CreateViewModel(typeof(T));
        }

        public ViewModelBase CreateViewModel(Type viewModelType)
        {
            return (ViewModelBase)ActivatorUtilities.CreateInstance(serviceProvider, viewModelType);
        }
    }
}
