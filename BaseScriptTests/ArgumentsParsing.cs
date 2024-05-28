using PBScriptBase;
using System.Collections.Generic;
using System.Text.Json;

namespace ModuleTests;

/// <summary>
/// Rules
/// Args splitter is ;
/// Value separator is =
/// KVP is trimmed before being added
/// Tab char is not trimmed
/// Escape char is \
/// Quotes are not special characters. (for c# maybe. not for these arguments)
/// Example: BayId=Jello;Mode=Open;
/// </summary>
public class ArgumentsParsing
{
    [Fact]
    public void SimpleTest()
    {
        string input = "BayId=Jello;Mode=Open;";
        Dictionary<string, string?> expected = new()
        {
            { "BayId", "Jello" },
            { "Mode", "Open" },
        };
        var actual = SEProgramBase.ParseArguments(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }

    [Fact]
    public void TrimTest()
    {
        string input = "BayId = Jello; Mode =Open ;";
        Dictionary<string, string?> expected = new()
        {
            { "BayId", "Jello" },
            { "Mode", "Open" },
        };
        var actual = SEProgramBase.ParseArguments(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }

    [Fact]
    public void TabTrimTest()
    {
        string input = "\tBayId=Jello\t;Mo\tde=\tOpen\t;";
        Dictionary<string, string?> expected = new()
        {
            { "BayId", "Jello" },
            { "Mo\tde", "Open" },
        };
        var actual = SEProgramBase.ParseArguments(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }

    [Fact]
    public void PartialTest()
    {
        string input = "BayId;=Open;";
        Dictionary<string, string?> expected = new()
        {
            { "BayId", null },
        };
        var actual = SEProgramBase.ParseArguments(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }

    [Fact]
    public void ExtrasTest()
    {
        string input = "Bay=Id=Jello;Mo;de=O;pen;";
        Dictionary<string, string?> expected = new()
        {
            { "Bay", "IdJello" },
            { "Mo", null },
            { "de", "O" },
            { "pen", null },
        };
        var actual = SEProgramBase.ParseArguments(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }

    [Fact]
    public void EscapeTest()
    {
        string input = "BayId=Jel\\=lo;Mo\\;de=Open;";
        Dictionary<string, string?> expected = new()
        {
            { "BayId", "Jel=lo" },
            { "Mo;de", "Open" },
        };
        var actual = SEProgramBase.ParseArguments(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }
}