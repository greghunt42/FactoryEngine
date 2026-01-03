using System.IO;
using System.Text.Json;

namespace FactoryPlatformer;

public static class HighScoreStorage
{
    private sealed record HighScoreModel(int Value);

    public static int Load(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                return 0;
            }

            var json = File.ReadAllText(fullPath);
            var model = JsonSerializer.Deserialize<HighScoreModel>(json);
            return model?.Value ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public static void Save(string path, int value)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(new HighScoreModel(value), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(fullPath, json);
        }
        catch
        {
            // Swallow errors so gameplay is unaffected.
        }
    }
}
