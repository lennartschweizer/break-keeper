using System.Text.Json;

namespace FocusEnforcer.Core;

public interface ISessionStore
{
    void Save(SessionSnapshot snapshot);
    SessionSnapshot? Load();
}

public interface IAuditLog
{
    void AppendEmergencyExit(DateTimeOffset timestampUtc, string presetName, TimeSpan remainingBreak);
}

public sealed class JsonSessionStore : ISessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

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

        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        string json = JsonSerializer.Serialize(snapshot, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    public SessionSnapshot? Load()
    {
        if (!File.Exists(_filePath))
            return null;

        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<SessionSnapshot>(json, JsonOptions);
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
