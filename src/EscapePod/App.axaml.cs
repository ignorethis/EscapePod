using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EscapePod.ViewModels;
using EscapePod.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EscapePod;

public class App : Application
{
    private IHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowVm = _host.Services.GetRequiredService<MainWindowViewModel>();

            desktop.ShutdownRequested += OnShutdownRequested;
            desktop.MainWindow = new MainWindow() { DataContext = mainWindowVm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        e.Cancel = true;

        try
        {
            if (_host != null)
            {
                var podcastService = _host.Services.GetRequiredService<PodcastService>();
                var mainWindowViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();

                await podcastService.SaveToDisk(mainWindowViewModel.Podcasts);

                await _host.StopAsync();
                _host.Dispose();
            }
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

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName.Default, c =>
        {
            c.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:122.0) Gecko/20100101 Firefox/122.0");
            c.Timeout = TimeSpan.FromMinutes(10);
        });
        services.AddTransient<IPodcastService, PodcastService>();
        services.AddTransient<MainWindowViewModel>();
    }
}
