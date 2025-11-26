using Godot;

namespace Pruny.UI;

public class AppConfig
{
    private static AppConfig? _instance;

    public static AppConfig Instance => _instance ??= new AppConfig();

    private AppConfig()
    {
        LoadFromEnvironment();
    }

    public string PrUnPlannerApiUrl { get; set; } = "https://rest.fnar.net";
    public string PrUnPlannerApiKey { get; set; } = "placeholder-api-key";

    public string GameDataPath { get; set; } = "user://pruny/data/game-data.json";
    public string WorkspacesPath { get; set; } = "user://pruny/data/workspaces";
    public string MarketCachePath { get; set; } = "user://pruny/data/market-cache";

    public TimeSpan ApiTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int ApiMaxRetries { get; set; } = 3;

    private void LoadFromEnvironment()
    {
        var apiKey = OS.GetEnvironment("PRUNPLANNER_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            PrUnPlannerApiKey = apiKey;
        }

        var apiUrl = OS.GetEnvironment("PRUNPLANNER_API_URL");
        if (!string.IsNullOrWhiteSpace(apiUrl))
        {
            PrUnPlannerApiUrl = apiUrl;
        }
    }

    public void EnsureDirectoriesExist()
    {
        EnsureDirectoryExists(GameDataPath);
        EnsureDirectoryExists(WorkspacesPath);
        EnsureDirectoryExists(MarketCachePath);
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var dirPath = filePath.GetBaseDir();
        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(dirPath);
            GD.Print($"Created directory: {dirPath}");
        }
    }
}
