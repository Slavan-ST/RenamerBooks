using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RenameBooks.Factories;
using RenameBooks.Interfaces;
using RenameBooks.Services;
using RenameBooks.Strategies;
using RenameBooks.Utils;
using RenameBooks.ViewModels;
using RenameBooks.Views;

namespace RenameBooks;

public partial class App : Application
{
    public ServiceCollection services = new ServiceCollection();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }


        services.AddSingleton<IFileNameSanitizer, FileNameSanitizer>();
        services.AddSingleton<IRenamerStrategy, Fb2RenamerStrategy>();
        services.AddSingleton<RenamerStrategyFactory>();
        services.AddSingleton<FileRenamerService>();


        base.OnFrameworkInitializationCompleted();
    }
}
