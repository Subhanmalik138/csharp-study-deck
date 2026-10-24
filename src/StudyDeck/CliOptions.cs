namespace StudyDeck;

/// <summary>Minimal option parser: positionals plus --name value,
/// --name=value, and -x value forms. No external dependency for a
/// surface this small.</summary>
public sealed class CliOptions
{
    public List<string> Positionals { get; } = new();
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);

    public string? Get(params string[] names)
    {
        foreach (var n in names)
            if (_values.TryGetValue(n, out var v)) return v;
        return null;
    }

    public bool Has(params string[] names) => names.Any(n => _flags.Contains(n) || _values.ContainsKey(n));

    public static CliOptions Parse(List<string> args)
    {
        var opts = new CliOptions();
        for (var i = 0; i < args.Count; i++)
        {
            var a = args[i];
            if (a.StartsWith("--"))
            {
                var body = a[2..];
                var eq = body.IndexOf('=');
                if (eq >= 0)
                {
                    opts._values[body[..eq]] = body[(eq + 1)..];
                }
                else if (i + 1 < args.Count && !args[i + 1].StartsWith("-"))
                {
                    opts._values[body] = args[++i];
                }
                else
                {
                    opts._flags.Add(body);
                }
            }
            else if (a.StartsWith('-') && a.Length == 2)
            {
                var key = a[1..];
                if (i + 1 < args.Count && !args[i + 1].StartsWith("-"))
                    opts._values[key] = args[++i];
                else
                    opts._flags.Add(key);
            }
            else
            {
                opts.Positionals.Add(a);
            }
        }
        return opts;
    }
}
