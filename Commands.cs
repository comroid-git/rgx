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
}

[Verb("-M", true, new[] { "-R" }, HelpText = "Match or Replace input using RegExp and write results to output")]
internal class MatchAndReplace : ICmd
{
    [Option('d', "default", Required = false, Default = null, HelpText = "Write the untreated input if it did not match")]
    public bool useDefault { get; set; }

    [Value(1, Required = false, Default = null, MetaName = "replacement", HelpText = "Replacement input string", MetaValue = "<string> or <file>")]
    public string? replacement { get; set; }

    public string pattern { get; set; }
    public IEnumerable<RegexOptions> flags { get; set; }
    public string? input { get; set; }
    public string? output { get; set; }
}

[Verb("-S", HelpText = "Split input using RegExp and write results to output")]
internal class Split : ICmd
{
    public string pattern { get; set; }
    public IEnumerable<RegexOptions> flags { get; set; }
    public string? input { get; set; }
    public string? output { get; set; }

    [Option('m', "mode", Required = false, Default = OutputMode.Join, HelpText = "What to do with the inputs during output", MetaValue = "Prepend, Join or Append")]
    public OutputMode mode { get; set; }

    public enum OutputMode
    {
        Prepend = 1,
        P = 1,
        
        Join = 2,
        J = 2,
        
        Append = 3,
        A = 3
    }
}