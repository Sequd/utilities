using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CleanBin;

/// <summary>
/// Сервис резервного копирования
/// </summary>
public class BackupService : IBackupService
{
    private readonly ILogger<BackupService> _logger;

    public BackupService(ILogger<BackupService> logger)
    {
        _logger = logger;
    }

    public async Task<OperationResult<BackupInfo>> CreateBackupAsync(
        IEnumerable<FilePreviewItem> files,
        string backupPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(backupPath))
                return OperationResult<BackupInfo>.Failure("Путь для резервной копии не может быть пустым");

            var fileList = files.ToList();
            if (!fileList.Any())
                return OperationResult<BackupInfo>.Failure("Нет файлов для резервного копирования");

            var backupId = Guid.NewGuid().ToString("N")[..8];
            var backupDirectory = Path.Combine(backupPath, $"backup_{backupId}_{DateTime.Now:yyyyMMdd_HHmmss}");
            var backupInfo = new BackupInfo
            {
                Id = backupId,
                BackupPath = backupDirectory,
                CreatedAt = DateTime.UtcNow,
                Description = $"Резервная копия {fileList.Count} файлов",
                SourcePath = Path.GetDirectoryName(fileList.First().FullPath) ?? string.Empty
            };

            Directory.CreateDirectory(backupDirectory);

            var backupFiles = new List<BackupFileInfo>();
            long totalSize = 0;

            foreach (var file in fileList)
            {
                try
                {
                    var relativePath = GetRelativePath(file.FullPath, backupInfo.SourcePath);
                    var backupFilePath = Path.Combine(backupDirectory, relativePath);
                    var backupFileDirectory = Path.GetDirectoryName(backupFilePath);

                    if (!string.IsNullOrEmpty(backupFileDirectory))
                    {
                        Directory.CreateDirectory(backupFileDirectory);
                    }

                    // Копируем файл
                    File.Copy(file.FullPath, backupFilePath, true);

                    // Вычисляем хэш файла
                    var fileHash = await ComputeFileHashAsync(backupFilePath, cancellationToken);

                    var backupFileInfo = new BackupFileInfo
                    {
                        OriginalPath = file.FullPath,
                        BackupFilePath = backupFilePath,
                        Size = file.Size,
                        LastModified = file.LastModified,
                        FileHash = fileHash,
                        Attributes = file.Attributes
                    };

                    backupFiles.Add(backupFileInfo);
                    totalSize += file.Size;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось создать резервную копию файла: {FilePath}", file.FullPath);
                }
            }

            backupInfo.Files = backupFiles;
            backupInfo.FileCount = backupFiles.Count;
            backupInfo.Size = totalSize;
            backupInfo.Checksum = await ComputeBackupChecksumAsync(backupFiles, cancellationToken);

            // Сохраняем метаданные резервной копии
            var metadataPath = Path.Combine(backupDirectory, "backup_metadata.json");
            var metadataJson = JsonSerializer.Serialize(backupInfo, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(metadataPath, metadataJson, cancellationToken);

            _logger.LogInformation("Создана резервная копия {Id} с {Count} файлами, размер: {Size}", 
                backupId, backupFiles.Count, backupInfo.FormattedSize);

            return OperationResult<BackupInfo>.Success(backupInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании резервной копии");
            return OperationResult<BackupInfo>.Failure($"Ошибка при создании резервной копии: {ex.Message}");
        }
    }

    public async Task<OperationResult> RestoreBackupAsync(
        BackupInfo backupInfo,
        string targetPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (backupInfo == null)
                return OperationResult.Failure("Информация о резервной копии не может быть null");

            if (string.IsNullOrWhiteSpace(targetPath))
                return OperationResult.Failure("Целевой путь не может быть пустым");

            if (!Directory.Exists(backupInfo.BackupPath))
                return OperationResult.Failure($"Резервная копия не найдена: {backupInfo.BackupPath}");

            Directory.CreateDirectory(targetPath);

            var restoredCount = 0;
            foreach (var backupFile in backupInfo.Files)
            {
                try
                {
                    var relativePath = GetRelativePath(backupFile.BackupFilePath, backupInfo.BackupPath);
                    var targetFilePath = Path.Combine(targetPath, relativePath);
                    var targetFileDirectory = Path.GetDirectoryName(targetFilePath);

                    if (!string.IsNullOrEmpty(targetFileDirectory))
                    {
                        Directory.CreateDirectory(targetFileDirectory);
                    }

                    // Копируем файл
                    File.Copy(backupFile.BackupFilePath, targetFilePath, true);

                    // Восстанавливаем атрибуты
                    var fileInfo = new FileInfo(targetFilePath);
                    fileInfo.Attributes = backupFile.Attributes;
                    fileInfo.LastWriteTime = backupFile.LastModified;

                    restoredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось восстановить файл: {FilePath}", backupFile.OriginalPath);
                }
            }

            _logger.LogInformation("Восстановлено {Count} файлов из резервной копии {Id}", 
                restoredCount, backupInfo.Id);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при восстановлении резервной копии {Id}", backupInfo?.Id);
            return OperationResult.Failure($"Ошибка при восстановлении резервной копии: {ex.Message}");
        }
    }

    public async Task<OperationResult> DeleteBackupAsync(
        BackupInfo backupInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (backupInfo == null)
                return OperationResult.Failure("Информация о резервной копии не может быть null");

            if (Directory.Exists(backupInfo.BackupPath))
            {
                Directory.Delete(backupInfo.BackupPath, true);
                _logger.LogInformation("Удалена резервная копия {Id}", backupInfo.Id);
            }

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении резервной копии {Id}", backupInfo?.Id);
            return OperationResult.Failure($"Ошибка при удалении резервной копии: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<BackupInfo>>> GetBackupsAsync(
        string backupDirectory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(backupDirectory) || !Directory.Exists(backupDirectory))
                return OperationResult<IEnumerable<BackupInfo>>.Success(Enumerable.Empty<BackupInfo>());

            var backups = new List<BackupInfo>();
            var backupDirs = Directory.GetDirectories(backupDirectory, "backup_*");

            foreach (var backupDir in backupDirs)
            {
                try
                {
                    var metadataPath = Path.Combine(backupDir, "backup_metadata.json");
                    if (File.Exists(metadataPath))
                    {
                        var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
                        var backupInfo = JsonSerializer.Deserialize<BackupInfo>(metadataJson);
                        if (backupInfo != null)
                        {
                            backups.Add(backupInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось загрузить метаданные резервной копии: {Path}", backupDir);
                }
            }

            _logger.LogInformation("Найдено {Count} резервных копий в {Directory}", 
                backups.Count, backupDirectory);

            return OperationResult<IEnumerable<BackupInfo>>.Success(backups.OrderByDescending(b => b.CreatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка резервных копий");
            return OperationResult<IEnumerable<BackupInfo>>.Failure($"Ошибка при получении резервных копий: {ex.Message}");
        }
    }

    public async Task<OperationResult<bool>> ValidateBackupAsync(
        BackupInfo backupInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (backupInfo == null)
                return OperationResult<bool>.Failure("Информация о резервной копии не может быть null");

            if (!Directory.Exists(backupInfo.BackupPath))
                return OperationResult<bool>.Success(false);

            // Проверяем целостность каждого файла
            foreach (var backupFile in backupInfo.Files)
            {
                if (!File.Exists(backupFile.BackupFilePath))
                    return OperationResult<bool>.Success(false);

                var currentHash = await ComputeFileHashAsync(backupFile.BackupFilePath, cancellationToken);
                if (currentHash != backupFile.FileHash)
                    return OperationResult<bool>.Success(false);
            }

            // Проверяем общую целостность
            var currentChecksum = await ComputeBackupChecksumAsync(backupInfo.Files, cancellationToken);
            if (currentChecksum != backupInfo.Checksum)
                return OperationResult<bool>.Success(false);

            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при валидации резервной копии {Id}", backupInfo?.Id);
            return OperationResult<bool>.Failure($"Ошибка при валидации резервной копии: {ex.Message}");
        }
    }

    public async Task<OperationResult<int>> CleanupOldBackupsAsync(
        string backupDirectory,
        int maxAgeInDays,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(backupDirectory) || !Directory.Exists(backupDirectory))
                return OperationResult<int>.Success(0);

            var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeInDays);
            var deletedCount = 0;

            var backupDirs = Directory.GetDirectories(backupDirectory, "backup_*");
            foreach (var backupDir in backupDirs)
            {
                try
                {
                    var metadataPath = Path.Combine(backupDir, "backup_metadata.json");
                    if (File.Exists(metadataPath))
                    {
                        var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
                        var backupInfo = JsonSerializer.Deserialize<BackupInfo>(metadataJson);
                        
                        if (backupInfo != null && backupInfo.CreatedAt < cutoffDate)
                        {
                            Directory.Delete(backupDir, true);
                            deletedCount++;
                            _logger.LogInformation("Удалена старая резервная копия {Id} от {Date}", 
                                backupInfo.Id, backupInfo.CreatedAt);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить старую резервную копию: {Path}", backupDir);
                }
            }

            _logger.LogInformation("Удалено {Count} старых резервных копий", deletedCount);
            return OperationResult<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке старых резервных копий");
            return OperationResult<int>.Failure($"Ошибка при очистке резервных копий: {ex.Message}");
        }
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        var fullPathUri = new Uri(fullPath);
        var basePathUri = new Uri(basePath + Path.DirectorySeparatorChar);
        return Uri.UnescapeDataString(basePathUri.MakeRelativeUri(fullPathUri).ToString());
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static async Task<string> ComputeBackupChecksumAsync(
        IEnumerable<BackupFileInfo> files, 
        CancellationToken cancellationToken)
    {
        var combinedData = string.Join("|", files.Select(f => $"{f.OriginalPath}:{f.FileHash}:{f.Size}"));
        var bytes = Encoding.UTF8.GetBytes(combinedData);
        
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(bytes, cancellationToken);
        return Convert.ToHexString(hash);
    }
}