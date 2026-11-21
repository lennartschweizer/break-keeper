using System.Text.Json;

namespace Core;

public interface ISessionStore
{
    void Save(SessionSnapshot snapshot);
    SessionSnapshot? Load();
}

public interface IAuditLog
{
    void AppendEmergencyExit(DateTimeOffset timestampUtc, string presetName, TimeSpan remainingBreak);
}

public sealed record DurationPreferences(int FocusMinutes, int BreakMinutes);

public interface IDurationPreferencesStore
{
    void Save(DurationPreferences preferences);
    DurationPreferences? Load();
}

public sealed class JsonSessionStore : ISessionStore
{
    private readonly string _filePath;

    public JsonSessionStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Session file path must not be empty.", nameof(filePath));
        _filePath = filePath;
    }

    public void Save(SessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        AtomicJsonFile.Write(_filePath, snapshot);
    }

    public SessionSnapshot? Load() => AtomicJsonFile.Read<SessionSnapshot>(_filePath);
}

public sealed class JsonDurationPreferencesStore : IDurationPreferencesStore
{
    private readonly string _filePath;

    public JsonDurationPreferencesStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Preferences file path must not be empty.", nameof(filePath));
        _filePath = filePath;
    }

    public void Save(DurationPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        AtomicJsonFile.Write(_filePath, preferences);
    }

    public DurationPreferences? Load()
    {
        DurationPreferences? loaded = AtomicJsonFile.Read<DurationPreferences>(_filePath);
        if (loaded is null)
            return null;

        bool validFocus = loaded.FocusMinutes >= TimerPreset.MinCustomMinutes && loaded.FocusMinutes <= TimerPreset.MaxCustomMinutes;
        bool validBreak = loaded.BreakMinutes >= TimerPreset.MinCustomMinutes && loaded.BreakMinutes <= TimerPreset.MaxCustomMinutes;
        return validFocus && validBreak ? loaded : null;
    }
}

internal static class AtomicJsonFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Write<T>(string filePath, T value)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        string json = JsonSerializer.Serialize(value, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, filePath, overwrite: true);
    }

    public static T? Read<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath))
            return null;

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}

public sealed class FileAuditLog : IAuditLog
{
    private readonly string _filePath;

    public FileAuditLog(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Audit log path must not be empty.", nameof(filePath));
        _filePath = filePath;
    }

    public void AppendEmergencyExit(DateTimeOffset timestampUtc, string presetName, TimeSpan remainingBreak)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string line = $"{timestampUtc:O} | EMERGENCY_EXIT | preset={presetName} | remaining={remainingBreak:c}{Environment.NewLine}";
        File.AppendAllText(_filePath, line);
    }
}
