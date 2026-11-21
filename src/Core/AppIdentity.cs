namespace Core;

public static class AppIdentity
{
    public const string DisplayName = "Break Keeper";
    public const string ProductId = "BreakKeeper";
    public const string BreakWindowTitle = $"{DisplayName} — Break";
    private static readonly string[] LegacyProductIds = ["BreakEnforcer", "FocusEnforcer"];

    public static string GetAppDataDirectory()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string currentDir = Path.Combine(localAppData, ProductId);

        Directory.CreateDirectory(currentDir);
        foreach (string legacyId in LegacyProductIds)
        {
            string legacyDir = Path.Combine(localAppData, legacyId);
            MigrateFileIfMissing(legacyDir, currentDir, "session.json");
            MigrateFileIfMissing(legacyDir, currentDir, "audit.log");
        }
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
