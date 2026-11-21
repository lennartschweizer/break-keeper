using Core;

namespace Core.Tests;

public sealed class BreakKeyFilterTests
{
    [Theory]
    [InlineData(BreakKeyFilter.VkLWin, false, false)]
    [InlineData(BreakKeyFilter.VkRWin, false, false)]
    [InlineData(BreakKeyFilter.VkTab, true, false)]
    [InlineData(BreakKeyFilter.VkEscape, true, false)]
    [InlineData(BreakKeyFilter.VkF4, true, false)]
    [InlineData(BreakKeyFilter.VkEscape, false, true)]
    public void ShouldBlock_ReturnsTrue_ForSystemEscapeShortcuts(int vkCode, bool altDown, bool ctrlDown)
    {
        Assert.True(BreakKeyFilter.ShouldBlock(vkCode, altDown, ctrlDown));
    }

    [Theory]
    [InlineData(BreakKeyFilter.VkTab, false, false)]
    [InlineData(BreakKeyFilter.VkEscape, false, false)]
    [InlineData(BreakKeyFilter.VkF4, false, false)]
    [InlineData(0x41, false, false)] // 'A'
    [InlineData(0x32, false, false)] // '2'
    [InlineData(0x08, false, false)] // Backspace
    public void ShouldBlock_ReturnsFalse_ForNormalTypingAndNonSystemKeys(int vkCode, bool altDown, bool ctrlDown)
    {
        Assert.False(BreakKeyFilter.ShouldBlock(vkCode, altDown, ctrlDown));
    }
}
