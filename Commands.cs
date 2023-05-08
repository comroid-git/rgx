using System.Text.RegularExpressions;
using CommandLine;

namespace rgx;

internal interface ICmd
{
    [Value(0, Required = true, HelpText = "Pattern to use (in-shell PCRE syntax)", MetaName = "<string> or <file>")]
    public string pattern { get; set; }

    [Option('f', "flags", Separator = ',', Required = false, HelpText = "Flags for the Pattern", MetaValue = "(see: dotnet RegexOptions)")]
    public IEnumerable<RegexOptions> flags { get; set; }

    [Option('i', "input", Required = false, Default = null, HelpText = "An input source, uses stdin if unspecified", MetaValue = "<string> or <file>")]
    public string? input { get; set; }

    [Option('o', "output", Required = false, Default = null, HelpText = "An output target, uses stdout if unspecified", MetaValue = "<file>")]
    public string? output { get; set; }

    [Option('u', "unmatched", Required = false, Default = UnmatchedOutputMode.Skip, HelpText = "What to do with unmatched inputs during output (default: Skip)", MetaValue = "Prepend, Skip or Append")]
    public UnmatchedOutputMode unmatched { get; set; }

    public enum UnmatchedOutputMode
    {
        
        Skip = default,
        S = default,
        
        Prepend = 1,
        P = 1,
        
        Append = 2,
        A = 2
    }
}

[Verb("-M", true, new[] { "-R" }, HelpText = "Match or Replace input using RegExp and write results to output")]
internal class MatchAndReplace : ICmd
{
    [Value(1, Required = false, Default = null, MetaName = "replacement", HelpText = "Replacement input string", MetaValue = "<string> or <file>")]
    public string? replacement { get; set; }

    public string pattern { get; set; }
    public IEnumerable<RegexOptions> flags { get; set; }
    public string? input { get; set; }
    public string? output { get; set; }
    public ICmd.UnmatchedOutputMode unmatched { get; set; }
}

[Verb("-S", HelpText = "Split input using RegExp and write results to output")]
internal class Split : ICmd
{
    public string pattern { get; set; }
    public IEnumerable<RegexOptions> flags { get; set; }
    public string? input { get; set; }
    public string? output { get; set; }
    public ICmd.UnmatchedOutputMode unmatched { get; set; }
}

// effectively grep using defaults
[Verb("-C", HelpText = "Cut matches out and write results to output")]
internal class Cut : ICmd
{
    public string pattern { get; set; }
    public IEnumerable<RegexOptions> flags { get; set; }
    public string? input { get; set; }
    public string? output { get; set; }
    public ICmd.UnmatchedOutputMode unmatched { get; set; }

    [Option('m', "mode", Required = false, Default = OutputMode.Extract, HelpText = "What to print", MetaValue = "")]
    public OutputMode outputMode { get; set; }
    public enum OutputMode
    {
        Extract = default,
        E = default,
        
        Remaining = 1,
        Rem = 1,
        R = 1
    }
}
