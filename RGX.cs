using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using CommandLine;
using comroid.common;
using comroid.common.csapi;

namespace rgx;

public static class RGX
{
    private static readonly Log log = new(typeof(RGX));

    static RGX()
    {
        ILog.Detail = DetailLevel.None;
    }
    
    public static void
#if TEST
        Exec
#else
        Main
#endif
        (params string[] args)
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
                cfg.MaximumDisplayWidth = log.RunWithExceptionLogger(() => Console.WindowWidth, "Could not get Console Width", _=>1024,LogLevel.Debug);
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
        var matches = new List<Match>();
        do
        {
            matches.Add(match);
        } while ((match = match.NextMatch()) is { Success: true });

        var off = 0;
        for (var i = 0; i < matches.Count; i++)
        {
            var each = matches[i];
            var next = matches.Count>i+1?matches[i+1]:null;
            if (next == null)
                break;
            var l = each.Index + each.Length;
            var r = (next?.Index ?? each.Index) - l;
            if (l + r > line.Length)
                break;
            yield return line.Substring(l, r);
        }
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
            log.At(LogLevel.Debug, error);
    }

    #endregion

    #region Utility Methods

    private static (Regex pattern, TextReader input, TextWriter output) Prepare(ICmd cmd)
    {
        if (cmd.flags.Contains(RegexOptions.Multiline))
            Console.Error.WriteLine("Warning: The multiline flag is not supported since we're only ever parsing line by line");
        return (new Regex(
                Streamable.Get(cmd.pattern).AsString(),
                cmd.flags.Aggregate((RegexOptions)0, (x, y) => x | y)),
            Streamable.Get(cmd.input).AsReader(),
            Streamable.Get(cmd.output).AsWriter());
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
