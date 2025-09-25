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
using System;

namespace RenameBooks;

public partial class App : Application
{
    public IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {

        ServiceCollection services = new ServiceCollection();

        services.AddSingleton<IFileNameSanitizer, FileNameSanitizer>();
        services.AddSingleton<IRenamerStrategy, Fb2RenamerStrategy>();
        services.AddSingleton<RenamerStrategyFactory>();
        services.AddSingleton<FileRenamerService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<MainViewModel>();

        ServiceProvider = services.BuildServiceProvider();


        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {

            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
        }



        base.OnFrameworkInitializationCompleted();
    }
}
