using System;
using System.IO;
using System.Text.Json;


namespace Pruny.App.Services;

public class AppConfig
{
    private static AppConfig? _instance;
    private static readonly string AppDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pruny");
    private static readonly string ConfigFilePath = Path.Combine(AppDataRoot, "config.json");

    public static AppConfig Instance => _instance ??= new AppConfig();

    private AppConfig()
    {
        // Set defaults relative to AppData
        GameDataPath = Path.Combine(AppDataRoot, "data", "game-data.json");
        WorkspacesPath = Path.Combine(AppDataRoot, "data", "workspaces");
        MarketCachePath = Path.Combine(AppDataRoot, "data", "market-cache");

        EnsureDirectoriesExist();
        LoadFromFile();
        LoadFromEnvironment();
    }

    public string PrUnPlannerApiUrl { get; set; } = "https://rest.fnar.net";
    public string PrUnPlannerApiKey { get; set; } = "placeholder-api-key";

    public string GameDataPath { get; set; }
    public string WorkspacesPath { get; set; }
    public string MarketCachePath { get; set; }

    public string? LastUsedWorkspace { get; set; }

    public TimeSpan ApiTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int ApiMaxRetries { get; set; } = 3;

    private void LoadFromFile()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                Console.WriteLine("AppConfig: No config file found, using defaults");
                return;
            }

            var json = File.ReadAllText(ConfigFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
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
                LastUsedWorkspace = config.LastUsedWorkspace;
                ApiTimeout = TimeSpan.FromSeconds(config.ApiTimeoutSeconds);
                ApiMaxRetries = config.ApiMaxRetries;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AppConfig: Failed to load config file - {ex.Message}");
        }
    }

    private void LoadFromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("PRUNPLANNER_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            PrUnPlannerApiKey = apiKey;
        }

        var apiUrl = Environment.GetEnvironmentVariable("PRUNPLANNER_API_URL");
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
                LastUsedWorkspace = LastUsedWorkspace,
                ApiTimeoutSeconds = (int)ApiTimeout.TotalSeconds,
                ApiMaxRetries = ApiMaxRetries
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(config, options);

            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AppConfig: Failed to save config file - {ex.Message}");
        }
    }

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(AppDataRoot);
        if (!string.IsNullOrEmpty(GameDataPath)) Directory.CreateDirectory(Path.GetDirectoryName(GameDataPath)!);
        if (!string.IsNullOrEmpty(WorkspacesPath)) Directory.CreateDirectory(WorkspacesPath);
        if (!string.IsNullOrEmpty(MarketCachePath)) Directory.CreateDirectory(MarketCachePath);
    }
}

public class AppConfigData
{
    public string? PrUnPlannerApiUrl { get; set; }
    public string? PrUnPlannerApiKey { get; set; }
    public string? GameDataPath { get; set; }
    public string? WorkspacesPath { get; set; }
    public string? MarketCachePath { get; set; }
    public string? LastUsedWorkspace { get; set; }
    public int ApiTimeoutSeconds { get; set; } = 30;
    public int ApiMaxRetries { get; set; } = 3;
}
