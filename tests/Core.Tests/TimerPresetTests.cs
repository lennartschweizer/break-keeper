using Core;

namespace Core.Tests;

public sealed class TimerPresetTests
{
    [Theory]
    [InlineData("1", 1)]
    [InlineData("45", 45)]
    [InlineData("999", 999)]
    [InlineData(" 25 ", 25)]
    public void TryParseCustomMinutes_AcceptsValidBounds(string input, int expected)
    {
        bool ok = TimerPreset.TryParseCustomMinutes(input, out int minutes);

        Assert.True(ok);
        Assert.Equal(expected, minutes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("1000")]
    [InlineData("-5")]
    [InlineData("12a")]
    [InlineData("abc")]
    public void TryParseCustomMinutes_RejectsInvalidOrOutOfRange(string? input)
    {
        bool ok = TimerPreset.TryParseCustomMinutes(input, out int minutes);

        Assert.False(ok);
        Assert.Equal(0, minutes);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("25", true)]
    [InlineData("999", true)]
    [InlineData("0", false)]
    [InlineData("05", false)]
    [InlineData("1000", false)]
    [InlineData("12a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void CanAcceptDurationInput_MatchesExpected(string? candidate, bool expected)
    {
        Assert.Equal(expected, TimerPreset.CanAcceptDurationInput(candidate));
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("123", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("12a", false)]
    [InlineData("-1", false)]
    [InlineData("1 2", false)]
    public void IsDigitsOnly_MatchesExpected(string? input, bool expected)
    {
        Assert.Equal(expected, TimerPreset.IsDigitsOnly(input));
    }

    [Fact]
    public void DurationPreferencesStore_RoundTripsValidValues_AndRejectsOutOfRange()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"breakkeeper-prefs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            string path = Path.Combine(tempDir, "last-durations.json");
            var store = new JsonDurationPreferencesStore(path);

            Assert.Null(store.Load());

            store.Save(new DurationPreferences(90, 15));
            DurationPreferences? loaded = store.Load();
            Assert.NotNull(loaded);
            Assert.Equal(90, loaded.FocusMinutes);
            Assert.Equal(15, loaded.BreakMinutes);

            store.Save(new DurationPreferences(0, 10));
            Assert.Null(store.Load());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
