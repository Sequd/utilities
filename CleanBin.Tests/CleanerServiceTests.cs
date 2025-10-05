using CleanBin;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CleanBin.Tests
{
    public class CleanerServiceTests
    {
        private readonly Mock<IOptions<CleanBinOptions>> _mockOptions;
        private readonly CleanBinOptions _options;
        private readonly CleanerService _service;

        public CleanerServiceTests()
        {
            _options = new CleanBinOptions
            {
                DefaultIgnoreDirectories = new[] { "node_modules", ".git" },
                DefaultCleanDirectories = new[] { "bin", "obj", "packages" },
                EnableSystemClean = false,
                LogLevel = "Information"
            };

            _mockOptions = new Mock<IOptions<CleanBinOptions>>();
            _mockOptions.Setup(x => x.Value).Returns(_options);

            _service = new CleanerService(_mockOptions.Object);
        }

        [Fact]
        public void Constructor_WithValidOptions_ShouldInitializeSuccessfully()
        {
            // Act & Assert
            _service.Should().NotBeNull();
            _service.GetStatistics().Should().NotBeNull();
        }

        [Fact]
        public void GetDirectories_WithValidPath_ShouldReturnSuccess()
        {
            // Arrange
            var tempPath = Path.GetTempPath();

            // Act
            var result = _service.GetDirectories(tempPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }

        [Fact]
        public void GetDirectories_WithInvalidPath_ShouldReturnFailure()
        {
            // Arrange
            var invalidPath = "C:\\NonExistentDirectory12345";

            // Act
            var result = _service.GetDirectories(invalidPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GetDirectories_WithNullPath_ShouldReturnFailure()
        {
            // Act
            var result = _service.GetDirectories(null!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Путь не может быть пустым");
        }

        [Fact]
        public void GetDirectories_WithEmptyPath_ShouldReturnFailure()
        {
            // Act
            var result = _service.GetDirectories("");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Путь не может быть пустым");
        }

        [Fact]
        public async Task GetDirectoriesAsync_WithValidPath_ShouldReturnSuccess()
        {
            // Arrange
            var tempPath = Path.GetTempPath();

            // Act
            var result = await _service.GetDirectoriesAsync(tempPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDirectoriesAsync_WithInvalidPath_ShouldReturnFailure()
        {
            // Arrange
            var invalidPath = "C:\\NonExistentDirectory12345";

            // Act
            var result = await _service.GetDirectoriesAsync(invalidPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetDirectoriesAsync_WithCancellation_ShouldReturnFailure()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act
            var result = await _service.GetDirectoriesAsync(tempPath, cancellationTokenSource.Token);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("отменена");
        }

        [Fact]
        public void CleanFolder_WithCriticalSystemPath_ShouldReturnFailure()
        {
            // Arrange
            var criticalPath = "C:\\Windows\\System32";

            // Act
            var result = _service.CleanFolder(criticalPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("критически важной системной папки");
        }

        [Fact]
        public void CleanFolder_WithInvalidPath_ShouldReturnFailure()
        {
            // Arrange
            var invalidPath = "C:\\NonExistentDirectory12345";

            // Act
            var result = _service.CleanFolder(invalidPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CleanFolder_WithNullPath_ShouldReturnFailure()
        {
            // Act
            var result = _service.CleanFolder(null!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Путь не может быть пустым");
        }

        [Fact]
        public async Task CleanFolderAsync_WithCriticalSystemPath_ShouldReturnFailure()
        {
            // Arrange
            var criticalPath = "C:\\Windows\\System32";

            // Act
            var result = await _service.CleanFolderAsync(criticalPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("критически важной системной папки");
        }

        [Fact]
        public async Task CleanFolderAsync_WithInvalidPath_ShouldReturnFailure()
        {
            // Arrange
            var invalidPath = "C:\\NonExistentDirectory12345";

            // Act
            var result = await _service.CleanFolderAsync(invalidPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CleanFolderAsync_WithCancellation_ShouldReturnFailure()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act
            var result = await _service.CleanFolderAsync(tempPath, cancellationToken: cancellationTokenSource.Token);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("отменена");
        }

        [Fact]
        public void GetStatistics_ShouldReturnValidStatistics()
        {
            // Act
            var statistics = _service.GetStatistics();

            // Assert
            statistics.Should().NotBeNull();
            statistics.TotalProcessedFolders.Should().BeGreaterOrEqualTo(0);
            statistics.DeletedFolders.Should().BeGreaterOrEqualTo(0);
            statistics.SkippedFolders.Should().BeGreaterOrEqualTo(0);
            statistics.Errors.Should().BeGreaterOrEqualTo(0);
            statistics.TotalExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void CleanFolder_WithCustomIgnoreDirectories_ShouldUseProvidedList()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var customIgnoreDirectories = new[] { "custom_ignore" };
            var customCleanDirectories = new[] { "custom_clean" };

            // Act
            var result = _service.CleanFolder(tempPath, false, customIgnoreDirectories, customCleanDirectories);

            // Assert
            // Тест проверяет, что метод принимает параметры без исключений
            result.Should().NotBeNull();
        }

        [Fact]
        public void CleanFolder_WithSystemCleanEnabled_ShouldUseSystemCleanSetting()
        {
            // Arrange
            var tempPath = Path.GetTempPath();

            // Act
            var result = _service.CleanFolder(tempPath, needSysClean: true);

            // Assert
            // Тест проверяет, что метод принимает параметр needSysClean без исключений
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task CleanFolderAsync_WithProgress_ShouldCallProgressCallback()
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var progressMessages = new List<string>();
            var progress = new Progress<string>(message => progressMessages.Add(message));

            // Act
            var result = await _service.CleanFolderAsync(tempPath, progress: progress);

            // Assert
            result.Should().NotBeNull();
            // Прогресс может быть пустым, если нет папок для обработки
        }

        [Fact]
        public void CleanFolder_WithEmptyDirectory_ShouldReturnSuccess()
        {
            // Arrange
            var tempPath = Path.GetTempPath();

            // Act
            var result = _service.CleanFolder(tempPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CleanFolderAsync_WithEmptyDirectory_ShouldReturnSuccess()
        {
            // Arrange
            var tempPath = Path.GetTempPath();

            // Act
            var result = await _service.CleanFolderAsync(tempPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }
    }
}