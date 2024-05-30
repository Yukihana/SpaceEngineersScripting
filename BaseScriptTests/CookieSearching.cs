using PBScriptBase;

namespace BaseScriptTests;

public class CookieSearching
{
    [Fact]
    public void JustCookieKey()
    {
        string input = "[SomeText]\n[SomeText2:SomeValue]";
        string search = "SomeText";
        string? expected = null;
        Assert.True(SEProgramBase.SearchCookie(input, search, out string actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CookieWithValue()
    {
        string input = "[SomeText]\n[SomeText2:SomeValue]";
        string search = "SomeText2";
        string expected = "SomeValue";
        Assert.True(SEProgramBase.SearchCookie(input, search, out string actual));
        Assert.Equal(expected, actual);
    }
}