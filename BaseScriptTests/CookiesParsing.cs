using PBScriptBase;
using System.Collections.Generic;
using System.Text.Json;

namespace ModuleTests;

public class CookiesParsing
{
    [Fact]
    public void SimpleTest()
    {
        string input = "[Apple:Book]Comment[Cat:Dog]";
        Dictionary<string, string?> expected = new()
        {
            { "Apple", "Book" },
            { "Cat", "Dog" },
        };
        var actual = SEProgramBase.ParseCookies(input);

        Assert.Equal(
             JsonSerializer.Serialize(expected),
             JsonSerializer.Serialize(actual));
    }

    [Fact]
    public void TrimTest()
    {
        string input = " [ Apple:Book ] Comment [Cat : Dog]";
        Dictionary<string, string?> expected = new()
        {
            { "Apple", "Book" },
            { "Cat", "Dog" },
        };
        var actual = SEProgramBase.ParseCookies(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }

    [Fact]
    public void SpecialsTest()
    {
        string input = " [ App\r\nle:Book\r\n ] Comment \r\n[Ca t :\r\n D og]";
        Dictionary<string, string?> expected = new()
        {
            { "App\r\nle", "Book" },
            { "Ca t", "D og" },
        };
        var actual = SEProgramBase.ParseCookies(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }

    [Fact]
    public void ExtrasTest()
    {
        string input = "[Ap[ple:Boo]kC]omment[Ca]t:Do[g::::]";
        Dictionary<string, string?> expected = new()
        {
            { "Apple", "Boo" },
            { "Ca" , null },
            { "g" , null },
        };
        var actual = SEProgramBase.ParseCookies(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }

    [Fact]
    public void EscapeTest()
    {
        string input = "[Ap\\[ple:Boo\\]kC]omment[Ca\\]t\\:::D[o\\[g::]";
        Dictionary<string, string?> expected = new()
        {
            { "Ap[ple", "Boo]kC" },
            { "Ca]t:" , "Do[g" },
        };
        var actual = SEProgramBase.ParseCookies(input);

        Assert.Equal(
            JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(actual));
    }
}