namespace Budge.Web;

internal static class LocalEnvironment
{
    public static void Load()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("BUDGE_LOAD_DOTENV"), "0", StringComparison.Ordinal))
        {
            return;
        }

        var path = FindEnvFile();
        if (path is null)
        {
            return;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var split = trimmed.IndexOf('=');
            if (split <= 0)
            {
                continue;
            }

            var key = trimmed[..split].Trim();
            var value = trimmed[(split + 1)..].Trim();
            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                value = value[1..^1];
            }

            if (key.Length == 0 || Environment.GetEnvironmentVariable(key) is not null)
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string? FindEnvFile()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var depth = 0; directory is not null && depth < 5; depth++)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
