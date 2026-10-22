public class EnvConfigUti
{
    private static EnvConfigUti? instance;

    public static EnvConfigUti Instance
    {
        get { if (instance == null) instance = new EnvConfigUti(); return instance; }
        private set { instance = value; }
    }

    private EnvConfigUti() { }

    public string GetRequiredEnvironmentConfig(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required environment variable '{name}' is missing or empty.");
        }
        return value;
    }

    public string GetEnvironmentConfig(string name)
    {
        string value = string.Empty;

        try
        {
            value = Environment.GetEnvironmentVariable(name) ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving environment variable '{name}': {ex.Message}");
        }

        return value;
    }
}