using System.Globalization;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Simulator.Commands;

internal sealed class CommandArguments
{
    internal List<string> Positionals { get; } = [];
    private readonly Dictionary<string, int> _options = new(StringComparer.Ordinal);

    internal static CommandArguments Parse(IEnumerable<string> arguments, params string[] optionNames)
    {
        var result = new CommandArguments();
        using var values = arguments.GetEnumerator();
        while (values.MoveNext())
        {
            string value = values.Current;
            if (!value.StartsWith("--", StringComparison.Ordinal))
            {
                result.Positionals.Add(value);
                continue;
            }
            if (!optionNames.Contains(value, StringComparer.Ordinal))
                throw new ArgumentException($"Unknown option '{value}'.");
            if (result._options.ContainsKey(value)) throw new ArgumentException($"Option '{value}' was repeated.");
            if (!values.MoveNext()) throw new ArgumentException($"Option '{value}' requires a positive integer.");
            result._options.Add(value, PositiveInteger(values.Current, value));
        }
        return result;
    }

    internal int Option(string name, int fallback) => _options.GetValueOrDefault(name, fallback);

    internal void RequireMaximumPositionals(int maximum, string usage)
    {
        if (Positionals.Count > maximum) throw new ArgumentException($"Use {usage}.");
    }

    internal static int Integer(string value, string name)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            throw new ArgumentException($"{name} must be an integer.");
        return parsed;
    }

    internal static int PositiveInteger(string value, string name)
    {
        int parsed = Integer(value, name);
        if (parsed <= 0) throw new ArgumentOutOfRangeException(name, $"{name} must be positive.");
        return parsed;
    }
}

internal static class GamePackageFiles
{
    internal static GameDefinition Load(string? path, string sampleName) =>
        GamePackageLoader.Load(File.ReadAllText(PathFor(path, sampleName)));

    internal static string PathFor(string? path, string sampleName) =>
        path ?? Path.Combine(AppContext.BaseDirectory, "Samples", sampleName);
}
