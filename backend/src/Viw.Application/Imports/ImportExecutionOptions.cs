namespace Viw.Application.Imports;

public sealed class ImportExecutionOptions
{
    public const string SectionName = "Imports";
    public bool Enabled { get; set; } = true;
    public bool RunOnce { get; set; }
    public bool ValidateSourceOnly { get; set; }
    public int PollIntervalSeconds { get; set; } = 3600;
    public int LeaseMinutes { get; set; } = 10;
    public static bool IsValidInterval(int seconds) => seconds is >= 5 and <= 86400;
}
