using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CommandLine;
using comroid.common;
using comroid.common.csapi;
using rgx.antlr;
using Parser = CommandLine.Parser;

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
            }).ParseArguments<MatchCmd, ExpandCmd, SplitCmd, CutCmd, GroupsCmd, ReverseCmd>(args)
            .WithParsed(Run<MatchCmd>(Match))
            .WithParsed(Run<ExpandCmd>(Expand))
            .WithParsed(Run<SplitCmd>(Split, false))
            .WithParsed(Run<CutCmd>(Cut, false))
            .WithParsed(Run<GroupsCmd>(Groups))
            .WithParsed<ReverseCmd>(Reverse)
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

    private static IEnumerable<string> Groups(GroupsCmd cmd, string line, Match match)
    {
        var groups = match.Groups;
        foreach (var key in groups.Keys)
            yield return $"{key}: {groups[key]}";
    }

    private static void Reverse(ReverseCmd cmd)
    {
        foreach (var _ in Enumerable.Range(0, cmd.amount!.Value))
        {
            var pattern = Streamable.Get(cmd.pattern).AsString();
            var stream = new AntlrInputStream(pattern);
            var lexer = new RegExpLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new RegExpParser(tokens) { ErrorHandler = new BailErrorStrategy() };
            var reverser = new RegExpReverser();
            var atom = parser.atom();
            var result = reverser.Visit(atom);
            Console.WriteLine(result);
        }
    }

    private static void Error(IEnumerable<Error> errors)
    {
        foreach (var error in errors)
            log.At(LogLevel.Debug, error);
    }

    #endregion

    #region Utility Methods

    private static Action<CMD> Run<CMD>(Func<CMD, string, Match, IEnumerable<string>> handler, bool auto = true) where CMD : ICmd
    {
        return cmd =>
        {
            if (cmd.flags.Contains(RegexOptions.Multiline))
                Console.Error.WriteLine("Warning: The multiline flag is not supported since we're only ever parsing line by line");
            var regexOptions = cmd.flags.Aggregate((RegexOptions)0, (x, y) => x | y);
            Regex BuildRegex(Streamable from) => new(from.AsString(), regexOptions);
            var pattern = Streamable.Get(cmd.pattern).Use(BuildRegex).NonNull();
            var input = Streamable.Get(cmd.input).OrStdIO().AsReader();
            var output = Streamable.Get(cmd.output).OrStdIO().AsWriter();
            var start = Streamable.Get(cmd.start).Use(BuildRegex);
            var stop = Streamable.Get(cmd.stop).Use(BuildRegex);
            bool started = start == null, stopped = false;

            while (!stopped && input.ReadLine() is { } line)
            {
                if (!started && start != null)
                    started = start.IsMatch(line);
                else if (started && stop != null)
                    stopped = stop.IsMatch(line);
                else
                {
                    var match = pattern.Match(line);
                    var success = match.Success;

                    do
                    {
                        if (!success && cmd.unmatched == ICmd.IncludeMode.Prepend)
                            output.WriteLine(line);
                        else if (success)
                        {
                            if (cmd.untreated == ICmd.IncludeMode.Prepend)
                                output.WriteLine(line);
                            foreach (var str in handler(cmd, line, match!))
                                output.WriteLine(str);
                            if (cmd.untreated == ICmd.IncludeMode.Append)
                                output.WriteLine(line);
                        }
                        else if (cmd.unmatched == ICmd.IncludeMode.Append)
                            output.WriteLine(line);
                        match = match?.NextMatch();
                    } while (auto && (success = match?.Success ?? false));
                }
            }

            foreach (var res in new IDisposable[] { input, output })
                res.Dispose();
        };
    }

    #endregion
}

internal class RegExpReverser : IParseTreeListener
{
}
