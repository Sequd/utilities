using CleanBin;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanBin.Tests;

public class FilePreviewServiceTests : IDisposable
{
    private readonly Mock<ILogger<FilePreviewService>> _mockLogger;
    private readonly FilePreviewService _service;
    private readonly string _tempDirectory;

    public FilePreviewServiceTests()
    {
        _mockLogger = new Mock<ILogger<FilePreviewService>>();
        _service = new FilePreviewService(_mockLogger.Object);
        
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task GetPreviewAsync_WithValidPath_ShouldReturnPreviewItems()
    {
        // Arrange
        var testDir = Path.Combine(_tempDirectory, "TestDir");
        Directory.CreateDirectory(testDir);
        
        // Create test files
        var file1 = Path.Combine(testDir, "test1.tmp");
        var file2 = Path.Combine(testDir, "test2.log");
        await File.WriteAllTextAsync(file1, "test content 1");
        await File.WriteAllTextAsync(file2, "test content 2");

        var profile = new ConfigurationProfile
        {
            CleanDirectories = new[] { "TestDir" },
            FileFilters = new[] { "*.tmp", "*.log" }
        };

        // Act
        var result = await _service.GetPreviewAsync(_tempDirectory, profile);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(f => f.Name == "test1.tmp");
        result.Value.Should().Contain(f => f.Name == "test2.log");
    }

    [Fact]
    public async Task GetPreviewAsync_WithInvalidPath_ShouldFail()
    {
        // Arrange
        var invalidPath = Path.Combine(_tempDirectory, "NonExistentDir");
        var profile = new ConfigurationProfile();

        // Act
        var result = await _service.GetPreviewAsync(invalidPath, profile);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Директория не найдена");
    }

    [Fact]
    public async Task GetPreviewAsync_WithEmptyPath_ShouldFail()
    {
        // Arrange
        var profile = new ConfigurationProfile();

        // Act
        var result = await _service.GetPreviewAsync("", profile);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Путь не может быть пустым");
    }

    [Fact]
    public async Task GetPreviewStatisticsAsync_WithPreviewItems_ShouldReturnStatistics()
    {
        // Arrange
        var previewItems = new List<FilePreviewItem>
        {
            new() { Name = "test1.tmp", Size = 1024, IsSafeToDelete = true },
            new() { Name = "test2.log", Size = 2048, IsSafeToDelete = true },
            new() { Name = "test3.exe", Size = 4096, IsSafeToDelete = false, Warnings = new List<string> { "Исполняемый файл" } }
        };

        // Act
        var result = await _service.GetPreviewStatisticsAsync(previewItems);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalFiles.Should().Be(3);
        result.Value.TotalSize.Should().Be(7168); // 1024 + 2048 + 4096
        result.Value.SafeToDeleteFiles.Should().Be(2);
        result.Value.FilesWithWarnings.Should().Be(1);
        result.Value.SafeFilesPercentage.Should().Be(66.67, 0.01);
    }

    [Fact]
    public async Task FilterFilesAsync_WithSizeFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var files = new List<FilePreviewItem>
        {
            new() { Name = "small.tmp", Size = 1024 },
            new() { Name = "large.tmp", Size = 1024 * 1024 * 10 } // 10MB
        };

        var profile = new ConfigurationProfile
        {
            MaxFileSize = 1024 * 1024 * 5 // 5MB
        };

        // Act
        var result = await _service.FilterFilesAsync(files, profile);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().Contain(f => f.Name == "small.tmp");
    }

    [Fact]
    public async Task FilterFilesAsync_WithAgeFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var files = new List<FilePreviewItem>
        {
            new() { Name = "recent.tmp", LastModified = DateTime.Now.AddDays(-1) },
            new() { Name = "old.tmp", LastModified = DateTime.Now.AddDays(-60) }
        };

        var profile = new ConfigurationProfile
        {
            MaxFileAge = 30 // 30 days
        };

        // Act
        var result = await _service.FilterFilesAsync(files, profile);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().Contain(f => f.Name == "old.tmp");
    }

    [Fact]
    public async Task FilterFilesAsync_WithFileFilters_ShouldFilterCorrectly()
    {
        // Arrange
        var files = new List<FilePreviewItem>
        {
            new() { Name = "test.tmp", Extension = ".tmp" },
            new() { Name = "test.log", Extension = ".log" },
            new() { Name = "test.txt", Extension = ".txt" }
        };

        var profile = new ConfigurationProfile
        {
            FileFilters = new[] { "*.tmp", "*.log" }
        };

        // Act
        var result = await _service.FilterFilesAsync(files, profile);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(f => f.Name == "test.tmp");
        result.Value.Should().Contain(f => f.Name == "test.log");
    }

    [Fact]
    public async Task ValidateSafetyAsync_WithSafeAndUnsafeFiles_ShouldMarkCorrectly()
    {
        // Arrange
        var files = new List<FilePreviewItem>
        {
            new() { Name = "safe.tmp", FullPath = Path.Combine(_tempDirectory, "safe.tmp"), IsSafeToDelete = true },
            new() { Name = "unsafe.exe", FullPath = Path.Combine(_tempDirectory, "unsafe.exe"), IsSafeToDelete = true }
        };

        // Act
        var result = await _service.ValidateSafetyAsync(files);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        
        var safeFile = result.Value.First(f => f.Name == "safe.tmp");
        var unsafeFile = result.Value.First(f => f.Name == "unsafe.exe");
        
        safeFile.IsSafeToDelete.Should().BeTrue();
        unsafeFile.IsSafeToDelete.Should().BeFalse();
        unsafeFile.Warnings.Should().Contain("Исполняемый файл");
    }

    [Fact]
    public void FilePreviewItem_FromFileInfo_ShouldCreateCorrectly()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.tmp");
        File.WriteAllText(testFile, "test content");
        var fileInfo = new FileInfo(testFile);

        // Act
        var previewItem = FilePreviewItem.FromFileInfo(fileInfo, "Test reason");

        // Assert
        previewItem.Name.Should().Be("test.tmp");
        previewItem.FullPath.Should().Be(testFile);
        previewItem.Size.Should().Be(fileInfo.Length);
        previewItem.RemovalReason.Should().Be("Test reason");
        previewItem.Extension.Should().Be(".tmp");
        previewItem.Directory.Should().Be(_tempDirectory);
    }

    [Fact]
    public void FilePreviewItem_GetFormattedSize_ShouldFormatCorrectly()
    {
        // Arrange
        var previewItem = new FilePreviewItem { Size = 1024 * 1024 }; // 1MB

        // Act
        var formattedSize = previewItem.GetFormattedSize();

        // Assert
        formattedSize.Should().Be("1.0 MB");
    }

    [Fact]
    public void FilePreviewItem_GetFileIcon_ShouldReturnCorrectIcon()
    {
        // Arrange
        var tmpItem = new FilePreviewItem { Extension = ".tmp" };
        var logItem = new FilePreviewItem { Extension = ".log" };
        var exeItem = new FilePreviewItem { Extension = ".exe" };

        // Act & Assert
        tmpItem.GetFileIcon().Should().Be("🗑️");
        logItem.GetFileIcon().Should().Be("📝");
        exeItem.GetFileIcon().Should().Be("⚙️");
    }

    [Fact]
    public void FilePreviewItem_GetPriorityColor_ShouldReturnCorrectColor()
    {
        // Arrange
        var highPriority = new FilePreviewItem { Priority = 1 };
        var mediumPriority = new FilePreviewItem { Priority = 2 };
        var lowPriority = new FilePreviewItem { Priority = 3 };

        // Act & Assert
        highPriority.GetPriorityColor().Should().Be("🔴");
        mediumPriority.GetPriorityColor().Should().Be("🟡");
        lowPriority.GetPriorityColor().Should().Be("🟢");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}