using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using CommandLine;

namespace rgx;

public static class RGX
{
    public static void Main(params string[] args)
    {
        new Parser(cfg =>
            {
                cfg.CaseSensitive = false;
                cfg.CaseInsensitiveEnumValues = true;
                cfg.HelpWriter = Console.Out;
                cfg.IgnoreUnknownArguments = true;
                cfg.AutoHelp = true;
                cfg.AutoVersion = true;
                cfg.ParsingCulture = CultureInfo.InvariantCulture;
                cfg.EnableDashDash = false;
                cfg.MaximumDisplayWidth = Console.WindowWidth;
            }).ParseArguments<MatchCmd, ExpandCmd, SplitCmd, CutCmd>(args)
            .WithParsed(Run<MatchCmd>(Match))
            .WithParsed(Run<ExpandCmd>(Expand))
            .WithParsed(Run<SplitCmd>(Split))
            .WithParsed(Run<CutCmd>(Cut))
            .WithNotParsed(Error);
    }

    #region Command Methods

    private static IEnumerable<string> Match(MatchCmd cmd, string line, Match match)
    {
        yield return match.ToString();
    }

    private static IEnumerable<string> Expand(ExpandCmd cmd, string line, Match match)
    {
        var replacement = File.Exists(cmd.expander) ? File.ReadAllText(cmd.expander) : cmd.expander;
        yield return match.Result(replacement);
    }

    private static IEnumerable<string> Split(SplitCmd cmd, string line, Match match)
    {
        do
        {
            yield return match.ToString();
        } while ((match = match.NextMatch()) is { Success: true });
    }

    private static IEnumerable<string> Cut(CutCmd cmd, string line, Match match)
    {
        if (cmd.invert)
            do
            {
                yield return match.ToString();
            } while ((match = match.NextMatch()) is { Success: true });
        else
        {
            var matches = new List<Match>();
            do
            {
                matches.Add(match);
            } while ((match = match.NextMatch()) is { Success: true });

            var lastEnd = 0;
            foreach (var each in matches)
                line = line.Substring(lastEnd, lastEnd += each.Length);
            yield return line;
        }
    }

    private static void Error(IEnumerable<Error> errors)
    {
        foreach (var error in errors)
            Debug.WriteLine(error);
    }

    #endregion

    #region Utility Methods

    private static (Regex pattern, TextReader input, TextWriter output) Prepare(ICmd cmd)
    {
        if (cmd.flags.Contains(RegexOptions.Multiline))
            Console.Error.WriteLine("Warning: The multiline flag is not supported since we're only ever parsing line by line");
        return (new Regex(File.Exists(cmd.pattern) ? File.ReadAllText(cmd.pattern) : cmd.pattern, (RegexOptions)cmd.flags.Aggregate(0, (x, y) => x | (int)y)),
            cmd.input is not null and not ""
                ? File.Exists(cmd.input)
                    ? new StreamReader(new FileStream(cmd.input, FileMode.Open, FileAccess.Read))
                    : new StringReader(cmd.input)
                : Console.In,
            cmd.output is not null and not "" && File.Exists(cmd.output)
                ? new StreamWriter(new FileStream(cmd.output, FileMode.Truncate, FileAccess.Write))
                : Console.Out);
    }

    private static Action<CMD> Run<CMD>(Func<CMD, string, Match, IEnumerable<string>> handler) where CMD : ICmd
    {
        return cmd =>
        {
            var (pattern, input, output) = Prepare(cmd);

            while (input.ReadLine() is { } line)
            {
                var match = pattern.Match(line);

                if (!match.Success && cmd.unmatched == ICmd.IncludeMode.Prepend)
                    output.WriteLine(line);
                else if (match.Success)
                {
                    if (cmd.untreated == ICmd.IncludeMode.Prepend)
                        output.WriteLine(line);
                    foreach (var str in handler(cmd, line, match))
                        output.WriteLine(str);
                    if (cmd.untreated == ICmd.IncludeMode.Append)
                        output.WriteLine(line);
                }
                else if (cmd.unmatched == ICmd.IncludeMode.Append)
                    output.WriteLine(line);
            }

            foreach (var res in new IDisposable[] { input, output })
                res.Dispose();
        };
    }

    #endregion
}
