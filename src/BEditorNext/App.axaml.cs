using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using BEditorNext.Operations;
using BEditorNext.Services;
using BEditorNext.ViewModels;
using BEditorNext.Views;

using Reactive.Bindings;

namespace BEditorNext
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void RegisterServices()
        {
            base.RegisterServices();
            ServiceLocator.Current.BindToSelfSingleton<ProjectService>();
            RenderOperations.RegisterAll();
            UIDispatcherScheduler.Initialize();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
