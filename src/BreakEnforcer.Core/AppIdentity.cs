namespace BreakEnforcer.Core;

public static class AppIdentity
{
    public const string DisplayName = "Break Enforcer";
    public const string ProductId = "BreakEnforcer";
    public const string LegacyProductId = "FocusEnforcer";
    public const string BreakWindowTitle = $"{DisplayName} — Break";

    public static string GetAppDataDirectory()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string currentDir = Path.Combine(localAppData, ProductId);
        string legacyDir = Path.Combine(localAppData, LegacyProductId);

        Directory.CreateDirectory(currentDir);
        MigrateFileIfMissing(legacyDir, currentDir, "session.json");
        MigrateFileIfMissing(legacyDir, currentDir, "audit.log");
        return currentDir;
    }

    private static void MigrateFileIfMissing(string sourceDir, string targetDir, string fileName)
    {
        string sourcePath = Path.Combine(sourceDir, fileName);
        string targetPath = Path.Combine(targetDir, fileName);
        if (File.Exists(sourcePath) && !File.Exists(targetPath))
            File.Copy(sourcePath, targetPath);
    }
}
