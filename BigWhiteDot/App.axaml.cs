using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace BigWhiteDot
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();

                // Don’t shut down when the main window closes:
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown; // This ensures calling Window.Close() won’t kill the process until you explicitly call desktop.Shutdown()
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}