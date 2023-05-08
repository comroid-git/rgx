#if DEBUG
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace rgx;

public class Tests
{
    public enum RunType
    {
        Match,
        Replace,
        Split
    }

    public static string[] Run(RunType type, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, params string[] input)
    {
        var @in = new StringReader(string.Join("\n", input));
        var @out = new StringWriter();

        Run(type, pattern, @in, @out);
        return @out.ToString().Split("\r?\n");
    }

    public static void Run(RunType type, string pattern, TextReader input, TextWriter output)
    {
        var bakIn = Console.In;
        var bakOut = Console.Out;

        try
        {
            Console.SetIn(input);
            Console.SetOut(output);

            RGX.Main("-" + type.ToString()[0], pattern);
        }
        finally
        {
            Console.SetIn(bakIn);
            Console.SetOut(bakOut);
        }
    }

    public void ScreenExample()
    {
        const string input =
            @"There are screens on:
        468266.yourprocess      (Detached)
        467698.voip     (Detached)
        467687.voip     (Detached)
        467676.gameserver       (Detached)
        467665.gameserver       (Detached)
        467654.gameserver       (Detached)
        467643.gameserver       (Detached)
9 Sockets in /run/screens/S-kaleidox.";
        var result = Run(RunType.Match, @"\s*(\d+)\.(yourprocess)\s+\((\w+)\)", input);


        Assert.That(result.Length, Is.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(input.Split("\r?\n")));
    }

    public void SimpleArrayParse()
    {
        var result = Run(RunType.Split, @"[\[\"",\s\]]+", @"[ 0,1, 2 ,3, 4] ");
        for (var i = 0; i < result.Length; i++)
            Assert.That(int.Parse(result[i]), Is.EqualTo(i));
    }
}
#endif