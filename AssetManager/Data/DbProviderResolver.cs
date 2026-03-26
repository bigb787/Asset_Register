using System.Text.RegularExpressions;

namespace AssetManager.Data;

public enum DbProviderKind
{
    Sqlite,
    Npgsql,
    SqlServer,
}

public static class DbProviderResolver
{
    public static DbProviderKind Resolve(string? providerConfig, string? connectionString)
    {
        if (!string.IsNullOrWhiteSpace(providerConfig))
        {
            if (providerConfig.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                return DbProviderKind.SqlServer;
            if (providerConfig.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                providerConfig.Equals("Npgsql", StringComparison.OrdinalIgnoreCase))
                return DbProviderKind.Npgsql;
            if (providerConfig.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                return DbProviderKind.Sqlite;
        }

        var cs = connectionString ?? string.Empty;
        if (LikelySqlite(cs))
            return DbProviderKind.Sqlite;
        if (cs.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            return DbProviderKind.Npgsql;
        if (cs.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
            cs.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(cs, @"Database\s*=", RegexOptions.IgnoreCase))
            return DbProviderKind.SqlServer;

        return DbProviderKind.Sqlite;
    }

    private static bool LikelySqlite(string cs) =>
        cs.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) &&
        (cs.Contains(".db", StringComparison.OrdinalIgnoreCase) ||
         cs.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase));
}
