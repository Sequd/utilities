using CleanBin;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CleanBin.Tests
{
    public class IntegrationTests
    {
        private readonly CleanBinOptions _options;
        private readonly CleanerService _service;

        public IntegrationTests()
        {
            _options = new CleanBinOptions
            {
                DefaultIgnoreDirectories = new[] { "node_modules", ".git", ".vs" },
                DefaultCleanDirectories = new[] { "bin", "obj", "packages", "Debug", "Release" },
                EnableSystemClean = false,
                LogLevel = "Information"
            };

            var mockOptions = new Mock<IOptions<CleanBinOptions>>();
            mockOptions.Setup(x => x.Value).Returns(_options);

            _service = new CleanerService(mockOptions.Object);
        }

        [Fact]
        public async Task FullWorkflow_GetDirectoriesAndClean_ShouldWorkCorrectly()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var testDirectory = Path.Combine(tempPath, "CleanBinTest_" + Guid.NewGuid().ToString("N")[..8]);
            
            try
            {
                // Создаем тестовую структуру папок
                Directory.CreateDirectory(testDirectory);
                Directory.CreateDirectory(Path.Combine(testDirectory, "bin"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "obj"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "packages"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "src"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "src", "bin"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "src", "obj"));

                // Act 1: Получаем список директорий
                var getDirectoriesResult = await _service.GetDirectoriesAsync(testDirectory);

                // Assert 1
                getDirectoriesResult.IsSuccess.Should().BeTrue();
                getDirectoriesResult.Value.Should().NotBeNull();
                var directories = getDirectoriesResult.Value!.ToList();
                directories.Should().Contain("bin");
                directories.Should().Contain("obj");
                directories.Should().Contain("packages");
                directories.Should().Contain("src");

                // Act 2: Выполняем очистку
                var cleanResult = await _service.CleanFolderAsync(testDirectory);

                // Assert 2
                cleanResult.IsSuccess.Should().BeTrue();
                cleanResult.Value.Should().NotBeNull();

                // Act 3: Проверяем статистику
                var statistics = _service.GetStatistics();

                // Assert 3
                statistics.Should().NotBeNull();
                statistics.TotalProcessedFolders.Should().BeGreaterThan(0);
                statistics.DeletedFolders.Should().BeGreaterThan(0);

                // Act 4: Проверяем, что папки действительно удалены
                var afterCleanResult = await _service.GetDirectoriesAsync(testDirectory);

                // Assert 4
                afterCleanResult.IsSuccess.Should().BeTrue();
                var remainingDirectories = afterCleanResult.Value!.ToList();
                remainingDirectories.Should().NotContain("bin");
                remainingDirectories.Should().NotContain("obj");
                remainingDirectories.Should().NotContain("packages");
                remainingDirectories.Should().Contain("src"); // src не должна быть удалена
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                }
            }
        }

        [Fact]
        public async Task CleanFolderAsync_WithProgressReporting_ShouldReportProgress()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var testDirectory = Path.Combine(tempPath, "CleanBinProgressTest_" + Guid.NewGuid().ToString("N")[..8]);
            var progressMessages = new List<string>();
            var progress = new Progress<string>(message => progressMessages.Add(message));

            try
            {
                // Создаем тестовую структуру папок
                Directory.CreateDirectory(testDirectory);
                Directory.CreateDirectory(Path.Combine(testDirectory, "bin"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "obj"));

                // Act
                var result = await _service.CleanFolderAsync(testDirectory, progress: progress);

                // Assert
                result.IsSuccess.Should().BeTrue();
                progressMessages.Should().NotBeEmpty();
                progressMessages.Should().Contain(msg => msg.Contains("Обрабатывается"));
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                }
            }
        }

        [Fact]
        public async Task CleanFolderAsync_WithCancellation_ShouldCancelOperation()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var testDirectory = Path.Combine(tempPath, "CleanBinCancelTest_" + Guid.NewGuid().ToString("N")[..8]);
            var cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Создаем тестовую структуру папок
                Directory.CreateDirectory(testDirectory);
                Directory.CreateDirectory(Path.Combine(testDirectory, "bin"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "obj"));

                // Отменяем операцию сразу
                cancellationTokenSource.Cancel();

                // Act
                var result = await _service.CleanFolderAsync(testDirectory, cancellationToken: cancellationTokenSource.Token);

                // Assert
                result.IsSuccess.Should().BeFalse();
                result.ErrorMessage.Should().Contain("отменена");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                }
            }
        }

        [Fact]
        public async Task ErrorHandling_WithUnauthorizedAccess_ShouldHandleGracefully()
        {
            // Arrange
            var systemPath = "C:\\Windows\\System32";

            // Act
            var result = await _service.CleanFolderAsync(systemPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("критически важной системной папки");
        }

        [Fact]
        public async Task StatisticsTracking_ShouldAccumulateCorrectly()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var testDirectory1 = Path.Combine(tempPath, "CleanBinStatsTest1_" + Guid.NewGuid().ToString("N")[..8]);
            var testDirectory2 = Path.Combine(tempPath, "CleanBinStatsTest2_" + Guid.NewGuid().ToString("N")[..8]);

            try
            {
                // Создаем две тестовые структуры папок
                Directory.CreateDirectory(testDirectory1);
                Directory.CreateDirectory(Path.Combine(testDirectory1, "bin"));
                Directory.CreateDirectory(Path.Combine(testDirectory1, "obj"));

                Directory.CreateDirectory(testDirectory2);
                Directory.CreateDirectory(Path.Combine(testDirectory2, "packages"));
                Directory.CreateDirectory(Path.Combine(testDirectory2, "Debug"));

                // Act 1: Первая очистка
                var result1 = await _service.CleanFolderAsync(testDirectory1);
                var stats1 = _service.GetStatistics();

                // Act 2: Вторая очистка
                var result2 = await _service.CleanFolderAsync(testDirectory2);
                var stats2 = _service.GetStatistics();

                // Assert
                result1.IsSuccess.Should().BeTrue();
                result2.IsSuccess.Should().BeTrue();

                stats2.TotalProcessedFolders.Should().BeGreaterThan(stats1.TotalProcessedFolders);
                stats2.DeletedFolders.Should().BeGreaterThan(stats1.DeletedFolders);
                stats2.LastCleanupTime.Should().BeAfter(stats1.LastCleanupTime ?? DateTime.MinValue);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDirectory1))
                {
                    Directory.Delete(testDirectory1, true);
                }
                if (Directory.Exists(testDirectory2))
                {
                    Directory.Delete(testDirectory2, true);
                }
            }
        }

        [Fact]
        public async Task ConfigurationIntegration_ShouldUseDefaultSettings()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var testDirectory = Path.Combine(tempPath, "CleanBinConfigTest_" + Guid.NewGuid().ToString("N")[..8]);

            try
            {
                // Создаем тестовую структуру папок
                Directory.CreateDirectory(testDirectory);
                Directory.CreateDirectory(Path.Combine(testDirectory, "bin"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "obj"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "packages"));
                Directory.CreateDirectory(Path.Combine(testDirectory, "node_modules")); // Должна быть проигнорирована
                Directory.CreateDirectory(Path.Combine(testDirectory, ".git")); // Должна быть проигнорирована

                // Act
                var result = await _service.CleanFolderAsync(testDirectory);

                // Assert
                result.IsSuccess.Should().BeTrue();

                // Проверяем, что папки из DefaultCleanDirectories удалены
                var afterCleanResult = await _service.GetDirectoriesAsync(testDirectory);
                var remainingDirectories = afterCleanResult.Value!.ToList();
                remainingDirectories.Should().NotContain("bin");
                remainingDirectories.Should().NotContain("obj");
                remainingDirectories.Should().NotContain("packages");

                // Проверяем, что папки из DefaultIgnoreDirectories не удалены
                remainingDirectories.Should().Contain("node_modules");
                remainingDirectories.Should().Contain(".git");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                }
            }
        }
    }
}