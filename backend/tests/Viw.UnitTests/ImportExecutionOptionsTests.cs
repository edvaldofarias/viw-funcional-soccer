using Viw.Application.Imports;

namespace Viw.UnitTests;

public sealed class ImportExecutionOptionsTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(86400, true)]
    [InlineData(86401, false)]
    public void Poll_interval_must_stay_within_operational_limits(int seconds, bool expected)
    {
        // Arrange
        var interval = seconds;

        // Act
        var valid = ImportExecutionOptions.IsValidInterval(interval);

        // Assert
        valid.ShouldBe(expected);
    }
}
