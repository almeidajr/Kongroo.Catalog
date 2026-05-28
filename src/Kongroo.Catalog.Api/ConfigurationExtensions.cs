namespace Kongroo.Catalog.Api;

public static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public string GetRequiredConnectionString(string name) =>
            configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is not configured.");
    }
}

