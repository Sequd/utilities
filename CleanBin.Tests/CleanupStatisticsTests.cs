using CleanBin;
using FluentAssertions;
using Xunit;

namespace CleanBin.Tests
{
    public class CleanupStatisticsTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var statistics = new CleanupStatistics();

            // Assert
            statistics.TotalProcessedFolders.Should().Be(0);
            statistics.DeletedFolders.Should().Be(0);
            statistics.SkippedFolders.Should().Be(0);
            statistics.Errors.Should().Be(0);
            statistics.TotalExecutionTimeMs.Should().Be(0);
            statistics.LastCleanupTime.Should().BeNull();
        }

        [Fact]
        public void Reset_ShouldResetAllPropertiesToDefaultValues()
        {
            // Arrange
            var statistics = new CleanupStatistics
            {
                TotalProcessedFolders = 10,
                DeletedFolders = 5,
                SkippedFolders = 3,
                Errors = 2,
                TotalExecutionTimeMs = 1000,
                LastCleanupTime = DateTime.UtcNow
            };

            // Act
            statistics.Reset();

            // Assert
            statistics.TotalProcessedFolders.Should().Be(0);
            statistics.DeletedFolders.Should().Be(0);
            statistics.SkippedFolders.Should().Be(0);
            statistics.Errors.Should().Be(0);
            statistics.TotalExecutionTimeMs.Should().Be(0);
            statistics.LastCleanupTime.Should().BeNull();
        }

        [Fact]
        public void ToString_WithDefaultValues_ShouldReturnFormattedString()
        {
            // Arrange
            var statistics = new CleanupStatistics();

            // Act
            var result = statistics.ToString();

            // Assert
            result.Should().Contain("Статистика очистки:");
            result.Should().Contain("- Обработано папок: 0");
            result.Should().Contain("- Удалено папок: 0");
            result.Should().Contain("- Пропущено папок: 0");
            result.Should().Contain("- Ошибок: 0");
            result.Should().Contain("- Время выполнения: 0 мс");
            result.Should().Contain("- Последняя очистка: Не выполнялась");
        }

        [Fact]
        public void ToString_WithValues_ShouldReturnFormattedString()
        {
            // Arrange
            var statistics = new CleanupStatistics
            {
                TotalProcessedFolders = 10,
                DeletedFolders = 5,
                SkippedFolders = 3,
                Errors = 2,
                TotalExecutionTimeMs = 1500,
                LastCleanupTime = new DateTime(2024, 1, 15, 10, 30, 45)
            };

            // Act
            var result = statistics.ToString();

            // Assert
            result.Should().Contain("Статистика очистки:");
            result.Should().Contain("- Обработано папок: 10");
            result.Should().Contain("- Удалено папок: 5");
            result.Should().Contain("- Пропущено папок: 3");
            result.Should().Contain("- Ошибок: 2");
            result.Should().Contain("- Время выполнения: 1500 мс");
            result.Should().Contain("- Последняя очистка: 2024-01-15 10:30:45");
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var statistics = new CleanupStatistics();
            var testTime = DateTime.UtcNow;

            // Act
            statistics.TotalProcessedFolders = 100;
            statistics.DeletedFolders = 50;
            statistics.SkippedFolders = 25;
            statistics.Errors = 5;
            statistics.TotalExecutionTimeMs = 5000;
            statistics.LastCleanupTime = testTime;

            // Assert
            statistics.TotalProcessedFolders.Should().Be(100);
            statistics.DeletedFolders.Should().Be(50);
            statistics.SkippedFolders.Should().Be(25);
            statistics.Errors.Should().Be(5);
            statistics.TotalExecutionTimeMs.Should().Be(5000);
            statistics.LastCleanupTime.Should().Be(testTime);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0)]
        [InlineData(1, 1, 0, 0, 100)]
        [InlineData(10, 5, 3, 2, 1500)]
        [InlineData(100, 50, 25, 5, 10000)]
        public void ToString_WithVariousValues_ShouldContainCorrectNumbers(
            int totalProcessed, int deleted, int skipped, int errors, long executionTime)
        {
            // Arrange
            var statistics = new CleanupStatistics
            {
                TotalProcessedFolders = totalProcessed,
                DeletedFolders = deleted,
                SkippedFolders = skipped,
                Errors = errors,
                TotalExecutionTimeMs = executionTime,
                LastCleanupTime = DateTime.UtcNow
            };

            // Act
            var result = statistics.ToString();

            // Assert
            result.Should().Contain($"- Обработано папок: {totalProcessed}");
            result.Should().Contain($"- Удалено папок: {deleted}");
            result.Should().Contain($"- Пропущено папок: {skipped}");
            result.Should().Contain($"- Ошибок: {errors}");
            result.Should().Contain($"- Время выполнения: {executionTime} мс");
        }

        [Fact]
        public void LastCleanupTime_WithNullValue_ShouldShowNotPerformed()
        {
            // Arrange
            var statistics = new CleanupStatistics
            {
                LastCleanupTime = null
            };

            // Act
            var result = statistics.ToString();

            // Assert
            result.Should().Contain("- Последняя очистка: Не выполнялась");
        }

        [Fact]
        public void LastCleanupTime_WithValue_ShouldShowFormattedDateTime()
        {
            // Arrange
            var testTime = new DateTime(2024, 12, 25, 15, 45, 30);
            var statistics = new CleanupStatistics
            {
                LastCleanupTime = testTime
            };

            // Act
            var result = statistics.ToString();

            // Assert
            result.Should().Contain("- Последняя очистка: 2024-12-25 15:45:30");
        }
    }
}