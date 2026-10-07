using SipoDeck.Core.Actions;

namespace SipoDeck.Core.Tests;

public class KeyShortcutTests
{
    [Fact]
    public void TryParse_CtrlShiftM()
    {
        Assert.True(KeyShortcut.TryParse("Ctrl+Shift+M", out var s, out var error));
        Assert.Null(error);
        Assert.Equal(new KeyShortcut(true, false, true, false, "M"), s);
    }

    [Fact]
    public void TryParse_SingleFunctionKey()
    {
        Assert.True(KeyShortcut.TryParse("F5", out var s, out _));
        Assert.Equal(new KeyShortcut(false, false, false, false, "F5"), s);
    }

    [Fact]
    public void TryParse_WinUp()
    {
        Assert.True(KeyShortcut.TryParse("Win+Up", out var s, out _));
        Assert.Equal(new KeyShortcut(false, false, false, true, "Up"), s);
    }

    [Theory]
    [InlineData("alt+enter")]
    [InlineData("ALT+ENTER")]
    [InlineData("Alt+Enter")]
    [InlineData(" alt + enter ")]
    public void TryParse_IsCaseInsensitive_AndNormalizes(string text)
    {
        Assert.True(KeyShortcut.TryParse(text, out var s, out _));
        Assert.Equal(new KeyShortcut(false, true, false, false, "Enter"), s);
    }

    [Theory]
    [InlineData("control+windows+f24", "Ctrl+Win+F24")]
    [InlineData("ctrl+a", "Ctrl+A")]
    [InlineData("shift+esc", "Shift+Esc")]
    [InlineData("1", "1")]
    public void TryParse_Aliases_AndToString(string text, string expected)
    {
        Assert.True(KeyShortcut.TryParse(text, out var s, out _));
        Assert.Equal(expected, s.ToString());
    }

    [Fact]
    public void ToString_UsesFixedModifierOrder()
        => Assert.Equal("Ctrl+Alt+Shift+Win+Delete", new KeyShortcut(true, true, true, true, "Delete").ToString());

    [Fact]
    public void ToString_RoundTripsThroughTryParse()
    {
        var original = new KeyShortcut(true, false, true, false, "PageDown");
        Assert.True(KeyShortcut.TryParse(original.ToString(), out var parsed, out _));
        Assert.Equal(original, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+")]
    [InlineData("+A")]
    [InlineData("A+B")]
    [InlineData("Ctrl+Foo")]
    [InlineData("Ctrl")]
    [InlineData("Ctrl+Shift")]
    [InlineData("F0")]
    [InlineData("F25")]
    [InlineData("Ab")]
    public void TryParse_Invalid_ReturnsFalseWithError(string? text)
    {
        Assert.False(KeyShortcut.TryParse(text, out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
