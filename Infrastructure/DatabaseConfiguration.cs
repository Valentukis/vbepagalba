namespace VbeSort.Infrastructure;

public enum DatabaseProvider
{
    PostgreSql,
    Sqlite
}

public sealed record DatabaseSettings(DatabaseProvider Provider, string ConnectionString);

/// <summary>
/// Nustato, kuria duomenu baze naudoti. Pagrindinis variantas – PostgreSQL (Supabase),
/// bet lokaliam darbui ir testams palaikomas ir SQLite (connection string "Data Source=...").
/// </summary>
public static class DatabaseConfiguration
{
    public static DatabaseSettings Resolve(IConfiguration configuration)
    {
        var raw = Environment.GetEnvironmentVariable("DATABASE_URL")
                  ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                  ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Nenustatyta duomenų bazės prisijungimo eilutė.");

        var normalized = Normalize(raw);
        return new DatabaseSettings(DetectProvider(normalized), normalized);
    }

    public static DatabaseProvider DetectProvider(string connectionString)
    {
        var cs = connectionString.TrimStart();
        return cs.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
               || cs.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase)
               || cs.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase)
            ? DatabaseProvider.Sqlite
            : DatabaseProvider.PostgreSql;
    }

    public static string Normalize(string raw)
    {
        var connStr = EnvFileLoader.StripQuotes(raw.Trim());

        if (connStr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            connStr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return ConvertPostgresUrl(connStr);
        }
        return connStr;
    }

    /// <summary>postgresql://user:pass@host:port/db → Npgsql (ADO.NET) formatas.</summary>
    public static string ConvertPostgresUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = string.IsNullOrEmpty(uri.AbsolutePath.TrimStart('/'))
            ? "postgres"
            : uri.AbsolutePath.TrimStart('/');
        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
