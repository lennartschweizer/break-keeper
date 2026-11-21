namespace Core;

public enum TimerPhase
{
    Idle,
    Focus,
    PreWarning,
    Break,
    BreakCompleted
}

public sealed record TimerPreset(
    string Name,
    TimeSpan FocusDuration,
    TimeSpan BreakDuration,
    TimeSpan PreWarningDuration,
    TimeSpan ExtensionDuration,
    int MaxExtensions = 1)
{
    public static TimerPreset Deep50 { get; } = new(
        "50/10",
        TimeSpan.FromMinutes(50),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        MaxExtensions: 1);

    public static TimerPreset Starter25 { get; } = new(
        "25/5",
        TimeSpan.FromMinutes(25),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        MaxExtensions: 1);

    public const int MinCustomMinutes = 1;
    public const int MaxCustomMinutes = 999;

    public static bool CanAcceptDurationInput(string? candidate)
    {
        if (string.IsNullOrEmpty(candidate))
            return false;

        return candidate.Length <= 3
            && IsDigitsOnly(candidate)
            && candidate[0] != '0';
    }

    public static bool TryParseCustomMinutes(string? text, out int minutes)
    {
        minutes = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string trimmed = text.Trim();
        if (!IsDigitsOnly(trimmed))
            return false;

        if (int.TryParse(trimmed, out int parsed)
            && parsed >= MinCustomMinutes
            && parsed <= MaxCustomMinutes)
        {
            minutes = parsed;
            return true;
        }

        return false;
    }

    public static bool IsDigitsOnly(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (char c in text)
        {
            if (c < '0' || c > '9')
                return false;
        }

        return true;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Preset name must not be empty.", nameof(Name));
        if (FocusDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(FocusDuration), "Focus duration must be positive.");
        if (BreakDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(BreakDuration), "Break duration must be positive.");
        if (PreWarningDuration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(PreWarningDuration), "Pre-warning duration cannot be negative.");
        if (ExtensionDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ExtensionDuration), "Extension duration must be positive.");
        if (MaxExtensions < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxExtensions), "Max extensions cannot be negative.");
    }
}

public sealed record SessionSnapshot(
    TimerPhase Phase,
    TimerPreset ActivePreset,
    string? EntryStep,
    DateTimeOffset? PhaseEndUtc,
    int ExtensionsUsed,
    string? EmergencyExitPhrase);
