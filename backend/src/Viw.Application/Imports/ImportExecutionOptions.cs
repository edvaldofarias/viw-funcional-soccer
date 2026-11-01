namespace Viw.Application.Imports;

public sealed class ImportExecutionOptions
{
    public const string SectionName = "Imports";
    public bool Enabled { get; set; }
    public bool RunOnce { get; set; }
    public int PollIntervalSeconds { get; set; } = 60;

    public static bool IsValidInterval(int seconds) => seconds is >= 5 and <= 86400;
}
