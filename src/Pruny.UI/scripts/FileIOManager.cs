using Godot;

namespace Pruny.UI;

public static class FileIOManager
{
    public static string? LoadTextFile(string path)
    {
        try
        {
            if (!Godot.FileAccess.FileExists(path))
            {
                GD.PrintErr($"FileIOManager: File not found - {path}");
                return null;
            }

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            var error = Godot.FileAccess.GetOpenError();

            if (error != Error.Ok)
            {
                GD.PrintErr($"FileIOManager: Failed to open file - {path}, Error: {error}");
                return null;
            }

            var content = file.GetAsText();
            GD.Print($"FileIOManager: Loaded file - {path} ({content.Length} characters)");
            return content;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"FileIOManager: Exception loading file - {path}");
            GD.PrintErr($"  {ex.Message}");
            return null;
        }
    }

    public static void SaveTextFile(string path, string content)
    {
        try
        {
            var dirPath = path.GetBaseDir();
            if (!DirAccess.DirExistsAbsolute(dirPath))
            {
                DirAccess.MakeDirRecursiveAbsolute(dirPath);
                GD.Print($"FileIOManager: Created directory - {dirPath}");
            }

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            var error = Godot.FileAccess.GetOpenError();

            if (error != Error.Ok)
            {
                throw new IOException($"Failed to open file for writing: {path}, Error: {error}");
            }

            file.StoreString(content);
            GD.Print($"FileIOManager: Saved file - {path} ({content.Length} characters)");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"FileIOManager: Exception saving file - {path}");
            GD.PrintErr($"  {ex.Message}");
            throw;
        }
    }

    public static bool FileExists(string path)
    {
        return Godot.FileAccess.FileExists(path);
    }

    public static string[] ListFiles(string directory, string pattern = "*")
    {
        try
        {
            if (!DirAccess.DirExistsAbsolute(directory))
            {
                GD.Print($"FileIOManager: Directory does not exist - {directory}");
                return Array.Empty<string>();
            }

            using var dir = DirAccess.Open(directory);
            var error = DirAccess.GetOpenError();

            if (error != Error.Ok)
            {
                GD.PrintErr($"FileIOManager: Failed to open directory - {directory}, Error: {error}");
                return Array.Empty<string>();
            }

            var files = new List<string>();
            dir.ListDirBegin();

            while (true)
            {
                var fileName = dir.GetNext();
                if (string.IsNullOrEmpty(fileName))
                    break;

                if (dir.CurrentIsDir())
                    continue;

                if (pattern == "*" || fileName.Match(pattern))
                {
                    files.Add(fileName);
                }
            }

            dir.ListDirEnd();

            GD.Print($"FileIOManager: Found {files.Count} files in {directory}");
            return files.ToArray();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"FileIOManager: Exception listing files in {directory}");
            GD.PrintErr($"  {ex.Message}");
            return Array.Empty<string>();
        }
    }
}
