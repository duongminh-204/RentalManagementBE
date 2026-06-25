namespace Backend.Services;

public static class PackageFeatureHelper
{
    public static string JoinFeatureLines(IEnumerable<string>? features) =>
        string.Join('\n', (features ?? [])
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim()));

    public static List<string> SplitFeatureLines(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
