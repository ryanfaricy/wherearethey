namespace WhereAreThey.Helpers;

public static class ConfigurationHelper
{
    public static string? GetConnectionString(IConfiguration configuration)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrEmpty(databaseUrl))
        {
            return configuration.GetConnectionString("DefaultConnection");
        }

        if (databaseUrl.StartsWith("postgres://") || databaseUrl.StartsWith("postgresql://"))
        {
            try
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : "";
                return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing DATABASE_URL URI: {ex}");
            }
        }

        return databaseUrl;
    }
}
