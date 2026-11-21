namespace BreakEnforcer.Core;

public static class BreakKeyFilter
{
    public const int VkTab = 0x09;
    public const int VkEscape = 0x1B;
    public const int VkLWin = 0x5B;
    public const int VkRWin = 0x5C;
    public const int VkF4 = 0x73;

    public static bool ShouldBlock(int vkCode, bool altDown, bool ctrlDown)
    {
        if (vkCode == VkLWin || vkCode == VkRWin)
            return true;

        if (altDown && (vkCode == VkTab || vkCode == VkEscape || vkCode == VkF4))
            return true;

        if (ctrlDown && vkCode == VkEscape)
            return true;

        return false;
    }
}
