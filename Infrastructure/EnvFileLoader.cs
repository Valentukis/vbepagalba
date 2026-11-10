namespace VbeSort.Infrastructure;

/// <summary>
/// Minimalus .env failo skaitytuvas. Kiekviena eilute formatu RAKTAS=REIKSME tampa proceso aplinkos kintamuoju.
/// Komentarai (#) ir tuscios eilutes ignoruojamos, kabutes aplink reiksme nuimamos.
/// </summary>
public static class EnvFileLoader
{
    public static int Load(string path)
    {
        if (!File.Exists(path)) return 0;

        var loaded = 0;
        foreach (var line in File.ReadAllLines(path))
        {
            var parsed = ParseLine(line);
            if (parsed is null) continue;

            Environment.SetEnvironmentVariable(parsed.Value.Key, parsed.Value.Value);
            loaded++;
        }
        return loaded;
    }

    public static KeyValuePair<string, string>? ParseLine(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) return null;

        var parts = trimmed.Split('=', 2);
        if (parts.Length != 2) return null;

        var key = parts[0].Trim();
        if (key.Length == 0) return null;

        return new KeyValuePair<string, string>(key, StripQuotes(parts[1].Trim()));
    }

    public static string StripQuotes(string value)
    {
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1].Trim();
        }
        return value;
    }
}
