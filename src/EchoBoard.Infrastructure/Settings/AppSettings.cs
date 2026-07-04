namespace EchoBoard.Infrastructure.Settings;

public sealed record AppSettings
{
    public required string AppDataDirectory { get; init; }

    public required string LogDirectory { get; init; }

    public required string DatabasePath { get; init; }

    public string DatabaseConnectionString => $"Data Source={DatabasePath}";

    public static AppSettings CreateDefault()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EchoBoard");

        return new AppSettings
        {
            AppDataDirectory = appDataDirectory,
            LogDirectory = Path.Combine(appDataDirectory, "logs"),
            DatabasePath = Path.Combine(appDataDirectory, "echoboard.db")
        };
    }
}
