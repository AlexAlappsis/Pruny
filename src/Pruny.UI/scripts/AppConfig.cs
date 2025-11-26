using Godot;
using System.Text.Json;

namespace Pruny.UI;

public class AppConfig
{
    private static AppConfig? _instance;
    private const string ConfigFilePath = "user://pruny/config.json";

    public static AppConfig Instance => _instance ??= new AppConfig();

    private AppConfig()
    {
        LoadFromFile();
        LoadFromEnvironment();
    }

    public string PrUnPlannerApiUrl { get; set; } = "https://rest.fnar.net";
    public string PrUnPlannerApiKey { get; set; } = "placeholder-api-key";

    public string GameDataPath { get; set; } = "res://data/game-data.json";
    public string WorkspacesPath { get; set; } = "user://pruny/data/workspaces";
    public string MarketCachePath { get; set; } = "user://pruny/data/market-cache";

    public TimeSpan ApiTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int ApiMaxRetries { get; set; } = 3;

    private void LoadFromFile()
    {
        try
        {
            if (!Godot.FileAccess.FileExists(ConfigFilePath))
            {
                GD.Print("AppConfig: No config file found, using defaults");
                return;
            }

            var json = FileIOManager.LoadTextFile(ConfigFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                GD.Print("AppConfig: Config file is empty, using defaults");
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var config = JsonSerializer.Deserialize<AppConfigData>(json, options);

            if (config != null)
            {
                PrUnPlannerApiUrl = config.PrUnPlannerApiUrl ?? PrUnPlannerApiUrl;
                PrUnPlannerApiKey = config.PrUnPlannerApiKey ?? PrUnPlannerApiKey;
                GameDataPath = config.GameDataPath ?? GameDataPath;
                WorkspacesPath = config.WorkspacesPath ?? WorkspacesPath;
                MarketCachePath = config.MarketCachePath ?? MarketCachePath;
                ApiTimeout = TimeSpan.FromSeconds(config.ApiTimeoutSeconds);
                ApiMaxRetries = config.ApiMaxRetries;

                GD.Print("AppConfig: Settings loaded from file");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"AppConfig: Failed to load config file - {ex.Message}");
        }
    }

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

    public void Save()
    {
        try
        {
            var config = new AppConfigData
            {
                PrUnPlannerApiUrl = PrUnPlannerApiUrl,
                PrUnPlannerApiKey = PrUnPlannerApiKey,
                GameDataPath = GameDataPath,
                WorkspacesPath = WorkspacesPath,
                MarketCachePath = MarketCachePath,
                ApiTimeoutSeconds = (int)ApiTimeout.TotalSeconds,
                ApiMaxRetries = ApiMaxRetries
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(config, options);

            FileIOManager.SaveTextFile(ConfigFilePath, json);
            GD.Print("AppConfig: Settings saved to file");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"AppConfig: Failed to save config file - {ex.Message}");
            throw;
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

public class AppConfigData
{
    public string? PrUnPlannerApiUrl { get; set; }
    public string? PrUnPlannerApiKey { get; set; }
    public string? GameDataPath { get; set; }
    public string? WorkspacesPath { get; set; }
    public string? MarketCachePath { get; set; }
    public int ApiTimeoutSeconds { get; set; } = 30;
    public int ApiMaxRetries { get; set; } = 3;
}
