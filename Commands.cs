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

    [Option('m', "unmatched", Required = false, Default = IncludeMode.Skip, HelpText = "What to do with unmatched inputs during output (default: Skip)", MetaValue = "Prepend, Skip or Append")]
    public IncludeMode unmatched { get; set; }
    [Option('t', "untreated", Required = false, Default = IncludeMode.Skip, HelpText = "What to do with untreated but matched inputs during output (default: Skip)", MetaValue = "Prepend, Skip or Append")]
    public IncludeMode untreated { get; set; }

    public enum IncludeMode
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
    [Value(1, Required = false, Default = null, MetaName = "expander", HelpText = "Expander input string; uses [$1, $2, ..] variables for groups", MetaValue = "<string> or <file>")]
    public string? expander { get; set; }

    public string pattern { get; set; }
    public IEnumerable<RegexOptions> flags { get; set; }
    public string? input { get; set; }
    public string? output { get; set; }
    public ICmd.IncludeMode unmatched { get; set; }
    public ICmd.IncludeMode untreated { get; set; }
}

[Verb("-S", HelpText = "Split input using RegExp and write results to output")]
internal class Split : ICmd
{
    public string pattern { get; set; }
    public IEnumerable<RegexOptions> flags { get; set; }
    public string? input { get; set; }
    public string? output { get; set; }
    public ICmd.IncludeMode unmatched { get; set; }
    public ICmd.IncludeMode untreated { get; set; }
}

// effectively grep using defaults
[Verb("-C", HelpText = "Cut matches out and write results to output")]
internal class Cut : ICmd
{
    public string pattern { get; set; }
    public IEnumerable<RegexOptions> flags { get; set; }
    public string? input { get; set; }
    public string? output { get; set; }
    public ICmd.IncludeMode unmatched { get; set; }
    public ICmd.IncludeMode untreated { get; set; }

    [Option('a', "invert", Required = false, Default = false, HelpText = "When set, instead of writing all occurrences of pattern; write the remainder after cutting all occurrences")]
    public bool invert { get; set; }
}
