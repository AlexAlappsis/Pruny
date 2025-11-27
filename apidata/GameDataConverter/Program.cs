using System.Globalization;
using System.Text;
using System.Text.Json;

var dataDirectory = args.Length > 0
    ? Path.GetFullPath(args[0])
    : GetDefaultDataDirectory();

var outputPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.Combine(dataDirectory, "game-data.full.json");

if (!Directory.Exists(dataDirectory))
{
    Console.Error.WriteLine($"Data directory not found: {dataDirectory}");
    return 1;
}

var materialPath = Path.Combine(dataDirectory, "material.csv");
var buildingsPath = Path.Combine(dataDirectory, "buildings.txt");
var workforcePath = Path.Combine(dataDirectory, "building_workforce.txt");
var recipesPath = Path.Combine(dataDirectory, "building_recipies.txt");

var warnings = new List<string>();

var materials = LoadMaterials(materialPath, warnings);
var buildings = LoadBuildings(buildingsPath, workforcePath, warnings);
var recipes = LoadRecipes(recipesPath, materials, buildings, warnings);
var workforceTypes = BuildWorkforceTypes(buildings);

var gameData = new GameDataOutput
{
    Materials = materials,
    Buildings = buildings,
    Recipes = recipes,
    WorkforceTypes = workforceTypes,
    Version = "apidata-import",
    LoadedAt = DateTimeOffset.UtcNow
};

var json = JsonSerializer.Serialize(gameData, new JsonSerializerOptions
{
    WriteIndented = true
});

File.WriteAllText(outputPath, json);

Console.WriteLine($"Wrote {outputPath}");
Console.WriteLine($"  Materials: {materials.Count}");
Console.WriteLine($"  Buildings: {buildings.Count}");
Console.WriteLine($"  Recipes  : {recipes.Count}");
Console.WriteLine($"  Workforce types: {workforceTypes.Count}");

if (warnings.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Warnings:");
    foreach (var warning in warnings)
    {
        Console.WriteLine($"- {warning}");
    }
}

return 0;

static string GetDefaultDataDirectory()
{
    // AppContext.BaseDirectory -> .../apidata/GameDataConverter/bin/Debug/net9.0/
    var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    if (File.Exists(Path.Combine(candidate, "material.csv")))
    {
        return candidate;
    }

    // Fallback to current directory.
    return Directory.GetCurrentDirectory();
}

static Dictionary<string, MaterialOutput> LoadMaterials(string path, List<string> warnings)
{
    var rows = CsvReader.Read(path);
    var result = new Dictionary<string, MaterialOutput>(StringComparer.OrdinalIgnoreCase);

    foreach (var row in rows)
    {
        if (!row.TryGetValue("TICKER", out var ticker) || string.IsNullOrWhiteSpace(ticker))
        {
            warnings.Add("Material row missing TICKER");
            continue;
        }

        var name = ToDisplayName(row.GetValueOrDefault("NAME") ?? ticker);
        var material = new MaterialOutput
        {
            Id = ticker,
            Name = name,
            Ticker = ticker,
            CategoryId = row.GetValueOrDefault("CATEGORYID"),
            CategoryName = row.GetValueOrDefault("CATEGORYNAME"),
            Weight = ParseDecimal(row.GetValueOrDefault("WEIGHT")),
            Volume = ParseDecimal(row.GetValueOrDefault("VOLUME"))
        };

        if (!result.TryAdd(ticker, material))
        {
            warnings.Add($"Duplicate material ticker: {ticker}");
        }
    }

    return result;
}

static Dictionary<string, BuildingOutput> LoadBuildings(string buildingsPath, string workforcePath, List<string> warnings)
{
    var rows = CsvReader.Read(buildingsPath);
    var workforceByBuilding = LoadWorkforce(workforcePath, warnings);

    var result = new Dictionary<string, BuildingOutput>(StringComparer.OrdinalIgnoreCase);
    foreach (var row in rows)
    {
        if (!row.TryGetValue("Ticker", out var ticker) || string.IsNullOrWhiteSpace(ticker))
        {
            warnings.Add("Building row missing Ticker");
            continue;
        }

        var name = ToDisplayName(row.GetValueOrDefault("Name") ?? ticker);

        var building = new BuildingOutput
        {
            Id = ticker,
            Name = name,
            Area = ParseDecimal(row.GetValueOrDefault("Area")),
            Expertise = row.GetValueOrDefault("Expertise"),
            DefaultWorkforce = workforceByBuilding.GetValueOrDefault(ticker) ?? new List<WorkforceRequirementOutput>()
        };

        if (!result.TryAdd(ticker, building))
        {
            warnings.Add($"Duplicate building ticker: {ticker}");
        }
    }

    return result;
}

static Dictionary<string, List<WorkforceRequirementOutput>> LoadWorkforce(string path, List<string> warnings)
{
    var rows = CsvReader.Read(path);
    var result = new Dictionary<string, List<WorkforceRequirementOutput>>(StringComparer.OrdinalIgnoreCase);

    foreach (var row in rows)
    {
        var building = row.GetValueOrDefault("Building");
        var level = row.GetValueOrDefault("Level");
        var capacity = row.GetValueOrDefault("Capacity");

        if (string.IsNullOrWhiteSpace(building) || string.IsNullOrWhiteSpace(level) || string.IsNullOrWhiteSpace(capacity))
        {
            warnings.Add("Workforce row missing Building/Level/Capacity");
            continue;
        }

        if (!int.TryParse(capacity, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
        {
            warnings.Add($"Invalid workforce capacity '{capacity}' for {building}/{level}");
            continue;
        }

        var list = result.GetValueOrDefault(building);
        if (list == null)
        {
            list = new List<WorkforceRequirementOutput>();
            result[building] = list;
        }

        list.Add(new WorkforceRequirementOutput
        {
            WorkforceType = level,
            Count = count
        });
    }

    return result;
}

static Dictionary<string, RecipeOutput> LoadRecipes(
    string path,
    Dictionary<string, MaterialOutput> materials,
    Dictionary<string, BuildingOutput> buildings,
    List<string> warnings)
{
    var rows = CsvReader.Read(path);
    var result = new Dictionary<string, RecipeOutput>(StringComparer.OrdinalIgnoreCase);

    foreach (var row in rows)
    {
        var key = row.GetValueOrDefault("Key");
        var buildingId = row.GetValueOrDefault("Building");
        var durationSeconds = ParseDecimal(row.GetValueOrDefault("Duration"));

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(buildingId) || durationSeconds == null)
        {
            warnings.Add("Recipe row missing Key/Building/Duration");
            continue;
        }

        if (!buildings.ContainsKey(buildingId))
        {
            warnings.Add($"Recipe references unknown building: {buildingId} (Key={key})");
        }

        var parts = key.Split(':', 2);
        var formula = parts.Length == 2 ? parts[1] : key;
        var ioParts = formula.Split("=>", StringSplitOptions.TrimEntries);
        if (ioParts.Length != 2)
        {
            warnings.Add($"Could not parse recipe formula: {key}");
            continue;
        }

        var inputs = ParseRecipeItems(ioParts[0], materials, warnings, key, "input");
        var outputs = ParseRecipeItems(ioParts[1], materials, warnings, key, "output");

        var id = SanitizeId(key);
        var recipe = new RecipeOutput
        {
            Id = id,
            BuildingId = buildingId,
            DurationMinutes = durationSeconds.Value / 60m,
            Inputs = inputs,
            Outputs = outputs,
            Name = $"{buildingId} => {string.Join("+", outputs.Select(o => o.MaterialId))}"
        };

        if (!result.TryAdd(id, recipe))
        {
            warnings.Add($"Duplicate recipe id: {id}");
        }
    }

    return result;
}

static List<RecipeItemOutput> ParseRecipeItems(
    string part,
    Dictionary<string, MaterialOutput> materials,
    List<string> warnings,
    string recipeKey,
    string label)
{
    var items = new List<RecipeItemOutput>();
    if (string.IsNullOrWhiteSpace(part))
        return items;

    var tokens = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var token in tokens)
    {
        var xIndex = token.IndexOf('x');
        if (xIndex <= 0 || xIndex == token.Length - 1)
        {
            warnings.Add($"Could not parse {label} token '{token}' in recipe {recipeKey}");
            continue;
        }

        var quantityText = token[..xIndex];
        var materialId = token[(xIndex + 1)..];

        if (!decimal.TryParse(quantityText, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
        {
            warnings.Add($"Invalid quantity '{quantityText}' for material {materialId} in recipe {recipeKey}");
            continue;
        }

        if (!materials.ContainsKey(materialId))
        {
            warnings.Add($"Recipe {recipeKey} references unknown material: {materialId}");
        }

        items.Add(new RecipeItemOutput
        {
            MaterialId = materialId,
            Quantity = quantity
        });
    }

    return items;
}

static Dictionary<string, WorkforceTypeOutput> BuildWorkforceTypes(Dictionary<string, BuildingOutput> buildings)
{
    var set = new Dictionary<string, WorkforceTypeOutput>(StringComparer.OrdinalIgnoreCase);

    foreach (var building in buildings.Values)
    {
        foreach (var worker in building.DefaultWorkforce)
        {
            if (set.ContainsKey(worker.WorkforceType))
                continue;

            set[worker.WorkforceType] = new WorkforceTypeOutput
            {
                Id = worker.WorkforceType,
                Name = worker.WorkforceType
            };
        }
    }

    return set;
}

static decimal? ParseDecimal(string? value)
{
    if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
    {
        return result;
    }

    return null;
}

static string SanitizeId(string key)
{
    var sanitized = key.Replace("=>", "-to-", StringComparison.Ordinal);
    sanitized = sanitized.Replace(":", "-", StringComparison.Ordinal);
    sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
    return sanitized;
}

static string ToDisplayName(string raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return raw;

    var sb = new StringBuilder();
    char? prev = null;

    for (var i = 0; i < raw.Length; i++)
    {
        var c = raw[i];
        var next = i + 1 < raw.Length ? raw[i + 1] : (char?)null;

        var isBoundary =
            (prev.HasValue && char.IsLower(prev.Value) && char.IsUpper(c)) ||
            (prev.HasValue && char.IsLetter(prev.Value) && char.IsDigit(c)) ||
            (prev.HasValue && char.IsDigit(prev.Value) && char.IsLetter(c)) ||
            (prev.HasValue && char.IsUpper(prev.Value) && char.IsUpper(c) && next.HasValue && char.IsLower(next.Value));

        if (isBoundary)
            sb.Append(' ');

        sb.Append(i == 0 ? char.ToUpperInvariant(c) : c);
        prev = c;
    }

    return sb.ToString().Trim();
}

internal static class CsvReader
{
    public static IEnumerable<Dictionary<string, string>> Read(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"CSV file not found: {path}");

        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (headerLine == null)
            yield break;

        var headers = ParseLine(headerLine);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseLine(line);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headers.Count && i < values.Count; i++)
            {
                row[headers[i]] = values[i];
            }

            yield return row;
        }
    }

    private static List<string> ParseLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}

internal class GameDataOutput
{
    public required Dictionary<string, MaterialOutput> Materials { get; init; }
    public required Dictionary<string, RecipeOutput> Recipes { get; init; }
    public required Dictionary<string, BuildingOutput> Buildings { get; init; }
    public Dictionary<string, WorkforceTypeOutput> WorkforceTypes { get; init; } = new();
    public string? Version { get; init; }
    public DateTimeOffset LoadedAt { get; init; }
}

internal class MaterialOutput
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Ticker { get; init; }
    public string? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public decimal? Weight { get; init; }
    public decimal? Volume { get; init; }
}

internal class RecipeOutput
{
    public required string Id { get; init; }
    public required string BuildingId { get; init; }
    public required List<RecipeItemOutput> Inputs { get; init; }
    public required List<RecipeItemOutput> Outputs { get; init; }
    public required decimal DurationMinutes { get; init; }
    public string? Name { get; init; }
}

internal class RecipeItemOutput
{
    public required string MaterialId { get; init; }
    public required decimal Quantity { get; init; }
}

internal class BuildingOutput
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public List<WorkforceRequirementOutput> DefaultWorkforce { get; init; } = new();
    public decimal? Area { get; init; }
    public string? Expertise { get; init; }
}

internal class WorkforceRequirementOutput
{
    public required string WorkforceType { get; init; }
    public required int Count { get; init; }
}

internal class WorkforceTypeOutput
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}
