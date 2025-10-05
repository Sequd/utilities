using CleanBin;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanBin.Tests;

public class BackupServiceTests : IDisposable
{
    private readonly Mock<ILogger<BackupService>> _mockLogger;
    private readonly BackupService _service;
    private readonly string _tempDirectory;
    private readonly string _backupDirectory;

    public BackupServiceTests()
    {
        _mockLogger = new Mock<ILogger<BackupService>>();
        _service = new BackupService(_mockLogger.Object);
        
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _backupDirectory = Path.Combine(_tempDirectory, "Backups");
        Directory.CreateDirectory(_tempDirectory);
        Directory.CreateDirectory(_backupDirectory);
    }

    [Fact]
    public async Task CreateBackupAsync_WithValidFiles_ShouldSucceed()
    {
        // Arrange
        var testFile1 = Path.Combine(_tempDirectory, "test1.txt");
        var testFile2 = Path.Combine(_tempDirectory, "test2.log");
        await File.WriteAllTextAsync(testFile1, "test content 1");
        await File.WriteAllTextAsync(testFile2, "test content 2");

        var previewItems = new List<FilePreviewItem>
        {
            new() { FullPath = testFile1, Name = "test1.txt", Size = 15 },
            new() { FullPath = testFile2, Name = "test2.log", Size = 15 }
        };

        // Act
        var result = await _service.CreateBackupAsync(previewItems, _backupDirectory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().NotBeNullOrEmpty();
        result.Value.BackupPath.Should().NotBeNullOrEmpty();
        result.Value.FileCount.Should().Be(2);
        result.Value.Size.Should().Be(30);
        result.Value.Files.Should().HaveCount(2);
        
        // Verify backup directory exists
        Directory.Exists(result.Value.BackupPath).Should().BeTrue();
        
        // Verify metadata file exists
        var metadataPath = Path.Combine(result.Value.BackupPath, "backup_metadata.json");
        File.Exists(metadataPath).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBackupAsync_WithEmptyFileList_ShouldFail()
    {
        // Arrange
        var emptyList = new List<FilePreviewItem>();

        // Act
        var result = await _service.CreateBackupAsync(emptyList, _backupDirectory);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Нет файлов для резервного копирования");
    }

    [Fact]
    public async Task CreateBackupAsync_WithEmptyBackupPath_ShouldFail()
    {
        // Arrange
        var previewItems = new List<FilePreviewItem>
        {
            new() { FullPath = "test.txt", Name = "test.txt", Size = 10 }
        };

        // Act
        var result = await _service.CreateBackupAsync(previewItems, "");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Путь для резервной копии не может быть пустым");
    }

    [Fact]
    public async Task RestoreBackupAsync_WithValidBackup_ShouldSucceed()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var previewItems = new List<FilePreviewItem>
        {
            new() { FullPath = testFile, Name = "test.txt", Size = 12 }
        };

        var backupResult = await _service.CreateBackupAsync(previewItems, _backupDirectory);
        backupResult.IsSuccess.Should().BeTrue();

        var restoreDirectory = Path.Combine(_tempDirectory, "Restore");

        // Act
        var result = await _service.RestoreBackupAsync(backupResult.Value!, restoreDirectory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(restoreDirectory).Should().BeTrue();
        
        var restoredFile = Path.Combine(restoreDirectory, "test.txt");
        File.Exists(restoredFile).Should().BeTrue();
        (await File.ReadAllTextAsync(restoredFile)).Should().Be("test content");
    }

    [Fact]
    public async Task RestoreBackupAsync_WithNonExistentBackup_ShouldFail()
    {
        // Arrange
        var backupInfo = new BackupInfo
        {
            Id = "non-existent",
            BackupPath = Path.Combine(_tempDirectory, "NonExistentBackup")
        };

        var restoreDirectory = Path.Combine(_tempDirectory, "Restore");

        // Act
        var result = await _service.RestoreBackupAsync(backupInfo, restoreDirectory);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Резервная копия не найдена");
    }

    [Fact]
    public async Task DeleteBackupAsync_WithExistingBackup_ShouldSucceed()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var previewItems = new List<FilePreviewItem>
        {
            new() { FullPath = testFile, Name = "test.txt", Size = 12 }
        };

        var backupResult = await _service.CreateBackupAsync(previewItems, _backupDirectory);
        backupResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _service.DeleteBackupAsync(backupResult.Value!);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(backupResult.Value!.BackupPath).Should().BeFalse();
    }

    [Fact]
    public async Task GetBackupsAsync_WithExistingBackups_ShouldReturnBackups()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var previewItems = new List<FilePreviewItem>
        {
            new() { FullPath = testFile, Name = "test.txt", Size = 12 }
        };

        await _service.CreateBackupAsync(previewItems, _backupDirectory);

        // Act
        var result = await _service.GetBackupsAsync(_backupDirectory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(1);
        result.Value.First().Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetBackupsAsync_WithNonExistentDirectory_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentDirectory = Path.Combine(_tempDirectory, "NonExistent");

        // Act
        var result = await _service.GetBackupsAsync(nonExistentDirectory);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateBackupAsync_WithValidBackup_ShouldReturnTrue()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var previewItems = new List<FilePreviewItem>
        {
            new() { FullPath = testFile, Name = "test.txt", Size = 12 }
        };

        var backupResult = await _service.CreateBackupAsync(previewItems, _backupDirectory);
        backupResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _service.ValidateBackupAsync(backupResult.Value!);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBackupAsync_WithCorruptedBackup_ShouldReturnFalse()
    {
        // Arrange
        var backupInfo = new BackupInfo
        {
            Id = "corrupted",
            BackupPath = Path.Combine(_tempDirectory, "CorruptedBackup"),
            Files = new List<BackupFileInfo>
            {
                new() { BackupFilePath = "non-existent-file.txt", FileHash = "invalid-hash" }
            }
        };

        // Act
        var result = await _service.ValidateBackupAsync(backupInfo);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_WithOldBackups_ShouldDeleteOldOnes()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var previewItems = new List<FilePreviewItem>
        {
            new() { FullPath = testFile, Name = "test.txt", Size = 12 }
        };

        // Create a backup
        var backupResult = await _service.CreateBackupAsync(previewItems, _backupDirectory);
        backupResult.IsSuccess.Should().BeTrue();

        // Manually modify the creation date to make it old
        var metadataPath = Path.Combine(backupResult.Value!.BackupPath, "backup_metadata.json");
        var metadata = await File.ReadAllTextAsync(metadataPath);
        var backupInfo = System.Text.Json.JsonSerializer.Deserialize<BackupInfo>(metadata);
        backupInfo!.CreatedAt = DateTime.UtcNow.AddDays(-10); // 10 days old
        var modifiedMetadata = System.Text.Json.JsonSerializer.Serialize(backupInfo, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, modifiedMetadata);

        // Act
        var result = await _service.CleanupOldBackupsAsync(_backupDirectory, 5); // Delete backups older than 5 days

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        Directory.Exists(backupResult.Value.BackupPath).Should().BeFalse();
    }

    [Fact]
    public void BackupInfo_FormattedSize_ShouldFormatCorrectly()
    {
        // Arrange
        var backupInfo = new BackupInfo { Size = 1024 * 1024 }; // 1MB

        // Act
        var formattedSize = backupInfo.FormattedSize;

        // Assert
        formattedSize.Should().Be("1.0 MB");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}