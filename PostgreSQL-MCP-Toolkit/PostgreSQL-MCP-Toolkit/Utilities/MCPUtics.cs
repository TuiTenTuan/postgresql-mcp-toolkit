using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public class MCPUtics
{
    private static MCPUtics? instance;
    public static MCPUtics Instance
    {
        get { if (instance == null) instance = new MCPUtics(); return instance; }
        private set { instance = value; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly Regex IdentifierRegex = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private MCPUtics() { }

    public bool TryParseTableIdentifier(string input, out string schema, out string table, out string error)
    {
        schema = "public";
        table = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Table name must not be empty.";
            return false;
        }

        var trimmed = input.Trim();
        var parts = trimmed.Split('.', StringSplitOptions.None);
        if (parts.Length is < 1 or > 2)
        {
            error = "Table name must be 'table' or 'schema.table' using valid PostgreSQL identifiers.";
            return false;
        }

        if (parts.Length == 2)
        {
            schema = parts[0];
            table = parts[1];
        }
        else
        {
            table = parts[0];
        }

        if (!IsValidIdentifier(schema) || !IsValidIdentifier(table))
        {
            error = "Schema and table names must be unquoted identifiers matching [A-Za-z_][A-Za-z0-9_]*. " +
                    "Quoted/mixed-case identifiers are not supported via this tool input.";
            return false;
        }

        return true;
    }

    private bool IsValidIdentifier(string value) => !string.IsNullOrEmpty(value) && IdentifierRegex.IsMatch(value);

    public object? ConvertValue(object value) => value switch
    {
        DateTime dt => dt.ToString("O"),
        DateTimeOffset dto => dto.ToString("O"),
        DateOnly d => d.ToString("O"),
        TimeOnly t => t.ToString("O"),
        TimeSpan ts => ts.ToString(),
        Guid g => g.ToString(),
        byte[] bytes => Convert.ToBase64String(bytes),
        _ => value
    };

    public string FormatError(Exception ex)
    {
        var message = new StringBuilder(ex.Message);
        if (ex.InnerException is not null)
        {
            message.Append(" | ").Append(ex.InnerException.Message);
        }

        return JsonSerializer.Serialize(new { error = message.ToString() }, JsonOptions);
    }

    public string SerializeOutput(object obj) => JsonSerializer.Serialize(obj, JsonOptions);
}