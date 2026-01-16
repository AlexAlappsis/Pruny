using Photino.Blazor;
using Microsoft.Extensions.DependencyInjection;
using Pruny.App;
using System;

class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.RootComponents.Add<App>("app");

        // Register services
        appBuilder.Services.AddLogging();
        appBuilder.Services.AddSingleton<Pruny.App.Services.AppConfig>(Pruny.App.Services.AppConfig.Instance);
        appBuilder.Services.AddSingleton<Pruny.App.Services.BlazorSessionManager>();

        var app = appBuilder.Build();

        // Initialize SessionManager
        var sessionManager = app.Services.GetRequiredService<Pruny.App.Services.BlazorSessionManager>();
        await sessionManager.InitializeAsync();
        
        app.MainWindow
            .SetTitle("Pruny - Production Calculator")
            .SetUseOsDefaultSize(false)
            .SetSize(1280, 800);
            // .SetCenterScreen() removed as it caused build error

        app.Run();
    }
}
