using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaWebView;
using EscapePod.ViewModels;
using EscapePod.Views;

namespace EscapePod;

public class App : Application
{
    private PodcastService? _podcastService;
    private MainWindowViewModel? _mainWindowViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _podcastService = new PodcastService();
            _mainWindowViewModel = new MainWindowViewModel(_podcastService);

            desktop.ShutdownRequested += OnShutdownRequested;
            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainWindowViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        e.Cancel = true;

        try
        {
            await _podcastService.SaveToDisk(_mainWindowViewModel.Podcasts);
        }
        catch (Exception)
        {
            // TODO: log the error
        }
        finally
        {
            if (ApplicationLifetime is IControlledApplicationLifetime controlledLifetime)
            {
                controlledLifetime.Shutdown();
            }
        }
    }

    public override void RegisterServices()
    {
        base.RegisterServices();

        AvaloniaWebViewBuilder.Initialize(null);
    }
}
