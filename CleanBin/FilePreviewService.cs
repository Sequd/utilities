using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CleanBin;

/// <summary>
/// Сервис предварительного просмотра файлов
/// </summary>
public class FilePreviewService : IFilePreviewService
{
    private readonly ILogger<FilePreviewService> _logger;

    public FilePreviewService(ILogger<FilePreviewService> logger)
    {
        _logger = logger;
    }

    public async Task<OperationResult<IEnumerable<FilePreviewItem>>> GetPreviewAsync(
        string path, 
        ConfigurationProfile profile, 
        CancellationToken cancellationToken = default)
    {
        return await GetPreviewAsync(
            path, 
            profile, 
            profile.FileFilters, 
            profile.MaxFileSize, 
            profile.MaxFileAge, 
            cancellationToken);
    }

    public async Task<OperationResult<IEnumerable<FilePreviewItem>>> GetPreviewAsync(
        string path, 
        ConfigurationProfile profile,
        string[] fileFilters,
        long maxFileSize,
        int maxFileAge,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return OperationResult<IEnumerable<FilePreviewItem>>.Failure("Путь не может быть пустым");

            if (!Directory.Exists(path))
                return OperationResult<IEnumerable<FilePreviewItem>>.Failure($"Директория не найдена: {path}");

            var previewItems = new List<FilePreviewItem>();
            var directories = await GetDirectoriesToCleanAsync(path, profile, cancellationToken);

            foreach (var directory in directories)
            {
                var files = await GetFilesInDirectoryAsync(directory, fileFilters, maxFileSize, maxFileAge, cancellationToken);
                previewItems.AddRange(files);
            }

            // Фильтруем файлы по профилю
            var filteredItems = await FilterFilesAsync(previewItems, profile, cancellationToken);
            if (!filteredItems.IsSuccess)
                return filteredItems;

            // Валидируем безопасность
            var validatedItems = await ValidateSafetyAsync(filteredItems.Value!, cancellationToken);
            if (!validatedItems.IsSuccess)
                return validatedItems;

            _logger.LogInformation("Найдено {Count} файлов для предварительного просмотра в {Path}", 
                validatedItems.Value!.Count(), path);

            return OperationResult<IEnumerable<FilePreviewItem>>.Success(validatedItems.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении предварительного просмотра для {Path}", path);
            return OperationResult<IEnumerable<FilePreviewItem>>.Failure($"Ошибка при получении предварительного просмотра: {ex.Message}");
        }
    }

    public async Task<OperationResult<PreviewStatistics>> GetPreviewStatisticsAsync(
        IEnumerable<FilePreviewItem> previewItems,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = previewItems.ToList();
            var statistics = new PreviewStatistics
            {
                TotalFiles = items.Count,
                TotalSize = items.Sum(x => x.Size),
                SafeToDeleteFiles = items.Count(x => x.IsSafeToDelete),
                FilesWithWarnings = items.Count(x => x.Warnings.Any())
            };

            // Группировка по приоритетам
            statistics.FilesByPriority = items
                .GroupBy(x => x.Priority)
                .ToDictionary(g => g.Key, g => g.Count());

            // Группировка по типам файлов
            statistics.FilesByType = items
                .GroupBy(x => x.Extension.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Count());

            // Группировка по причинам удаления
            statistics.FilesByReason = items
                .GroupBy(x => x.RemovalReason)
                .ToDictionary(g => g.Key, g => g.Count());

            _logger.LogInformation("Статистика предварительного просмотра: {TotalFiles} файлов, {TotalSize}", 
                statistics.TotalFiles, statistics.FormattedTotalSize);

            return OperationResult<PreviewStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при вычислении статистики предварительного просмотра");
            return OperationResult<PreviewStatistics>.Failure($"Ошибка при вычислении статистики: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<FilePreviewItem>>> FilterFilesAsync(
        IEnumerable<FilePreviewItem> files,
        ConfigurationProfile profile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filteredFiles = files.Where(file =>
            {
                // Фильтр по размеру
                if (profile.MaxFileSize > 0 && file.Size > profile.MaxFileSize)
                    return false;

                // Фильтр по возрасту
                if (profile.MaxFileAge > 0 && file.LastModified < DateTime.Now.AddDays(-profile.MaxFileAge))
                    return false;

                // Фильтр по расширению
                if (profile.FileFilters.Any())
                {
                    var matchesFilter = profile.FileFilters.Any(filter =>
                    {
                        if (filter.StartsWith("*."))
                        {
                            var extension = filter.Substring(1);
                            return file.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase);
                        }
                        return file.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
                    });

                    if (!matchesFilter)
                        return false;
                }

                return true;
            }).ToList();

            _logger.LogInformation("Отфильтровано {Count} файлов из {Total} по критериям профиля", 
                filteredFiles.Count, files.Count());

            return OperationResult<IEnumerable<FilePreviewItem>>.Success(filteredFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при фильтрации файлов");
            return OperationResult<IEnumerable<FilePreviewItem>>.Failure($"Ошибка при фильтрации файлов: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<FilePreviewItem>>> ValidateSafetyAsync(
        IEnumerable<FilePreviewItem> files,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validatedFiles = new List<FilePreviewItem>();

            foreach (var file in files)
            {
                var validatedFile = file.Clone();
                
                // Дополнительные проверки безопасности
                if (IsSystemFile(validatedFile.FullPath))
                {
                    validatedFile.IsSafeToDelete = false;
                    validatedFile.Warnings.Add("Системный файл");
                }

                if (IsExecutableFile(validatedFile.Extension))
                {
                    validatedFile.IsSafeToDelete = false;
                    validatedFile.Warnings.Add("Исполняемый файл");
                }

                if (IsRecentlyModified(validatedFile.LastModified))
                {
                    validatedFile.Warnings.Add("Недавно изменен");
                }

                validatedFiles.Add(validatedFile);
            }

            var unsafeCount = validatedFiles.Count(f => !f.IsSafeToDelete);
            if (unsafeCount > 0)
            {
                _logger.LogWarning("Найдено {Count} небезопасных файлов для удаления", unsafeCount);
            }

            return OperationResult<IEnumerable<FilePreviewItem>>.Success(validatedFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при валидации безопасности файлов");
            return OperationResult<IEnumerable<FilePreviewItem>>.Failure($"Ошибка при валидации безопасности: {ex.Message}");
        }
    }

    private async Task<IEnumerable<string>> GetDirectoriesToCleanAsync(
        string path, 
        ConfigurationProfile profile, 
        CancellationToken cancellationToken)
    {
        var directories = new List<string>();

        if (profile.CleanDirectories.Any())
        {
            foreach (var cleanDir in profile.CleanDirectories)
            {
                var fullPath = Path.Combine(path, cleanDir);
                if (Directory.Exists(fullPath))
                {
                    directories.Add(fullPath);
                }
            }
        }
        else
        {
            // Стандартные папки для очистки
            var standardDirs = new[] { "bin", "obj", "packages", "node_modules", ".vs", "Debug", "Release" };
            foreach (var dir in standardDirs)
            {
                var fullPath = Path.Combine(path, dir);
                if (Directory.Exists(fullPath))
                {
                    directories.Add(fullPath);
                }
            }
        }

        return directories;
    }

    private async Task<IEnumerable<FilePreviewItem>> GetFilesInDirectoryAsync(
        string directory,
        string[] fileFilters,
        long maxFileSize,
        int maxFileAge,
        CancellationToken cancellationToken)
    {
        var files = new List<FilePreviewItem>();

        try
        {
            var searchPatterns = fileFilters.Any() ? fileFilters : new[] { "*.*" };

            foreach (var pattern in searchPatterns)
            {
                var directoryFiles = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
                
                foreach (var filePath in directoryFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        
                        // Проверяем размер файла
                        if (maxFileSize > 0 && fileInfo.Length > maxFileSize)
                            continue;

                        // Проверяем возраст файла
                        if (maxFileAge > 0 && fileInfo.LastWriteTime > DateTime.Now.AddDays(-maxFileAge))
                            continue;

                        var previewItem = FilePreviewItem.FromFileInfo(fileInfo, "Очистка папки");
                        files.Add(previewItem);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось обработать файл: {FilePath}", filePath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сканировании директории: {Directory}", directory);
        }

        return files;
    }

    private static bool IsSystemFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Attributes.HasFlag(FileAttributes.System) ||
                   fileInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                   filePath.Contains("System32", StringComparison.OrdinalIgnoreCase) ||
                   filePath.Contains("Windows", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true; // Если не можем проверить, считаем небезопасным
        }
    }

    private static bool IsExecutableFile(string extension)
    {
        var executableExtensions = new[] { ".exe", ".dll", ".sys", ".bat", ".cmd", ".com", ".scr" };
        return executableExtensions.Contains(extension.ToLowerInvariant());
    }

    private static bool IsRecentlyModified(DateTime lastModified)
    {
        return lastModified > DateTime.Now.AddHours(-1);
    }
}

/// <summary>
/// Расширения для FilePreviewItem
/// </summary>
public static class FilePreviewItemExtensions
{
    /// <summary>
    /// Создает копию FilePreviewItem
    /// </summary>
    public static FilePreviewItem Clone(this FilePreviewItem item)
    {
        return new FilePreviewItem
        {
            FullPath = item.FullPath,
            Name = item.Name,
            Size = item.Size,
            CreatedAt = item.CreatedAt,
            LastModified = item.LastModified,
            LastAccessed = item.LastAccessed,
            Attributes = item.Attributes,
            Extension = item.Extension,
            Directory = item.Directory,
            RemovalReason = item.RemovalReason,
            Priority = item.Priority,
            IsSafeToDelete = item.IsSafeToDelete,
            Warnings = new List<string>(item.Warnings)
        };
    }
}