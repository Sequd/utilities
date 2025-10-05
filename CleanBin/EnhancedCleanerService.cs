using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanBin;

/// <summary>
/// Улучшенный сервис очистки с поддержкой всех дополнительных возможностей
/// </summary>
public class EnhancedCleanerService : ICleanerService
{
    private readonly CleanBinOptions _options;
    private readonly CleanupStatistics _statistics;
    private readonly ILogger<EnhancedCleanerService> _logger;
    private readonly IConfigurationProfileManager _profileManager;
    private readonly IFilePreviewService _previewService;
    private readonly IBackupService _backupService;
    private readonly ICacheService _cacheService;

    public EnhancedCleanerService(
        IOptions<CleanBinOptions> options,
        ILogger<EnhancedCleanerService> logger,
        IConfigurationProfileManager profileManager,
        IFilePreviewService previewService,
        IBackupService backupService,
        ICacheService cacheService)
    {
        _options = options.Value;
        _logger = logger;
        _profileManager = profileManager;
        _previewService = previewService;
        _backupService = backupService;
        _cacheService = cacheService;
        _statistics = new CleanupStatistics();
    }

    public OperationResult<IEnumerable<string>> GetDirectories(string path)
    {
        try
        {
            var validationResult = PathValidator.ValidateDirectoryPath(path);
            if (!validationResult.IsSuccess)
                return OperationResult<IEnumerable<string>>.Failure(validationResult.ErrorMessage);

            var directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .Where(d => !PathValidator.IsCriticalSystemPath(d))
                .ToList();

            _logger.LogInformation("Найдено {Count} директорий в {Path}", directories.Count, path);
            return OperationResult<IEnumerable<string>>.Success(directories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении директорий для {Path}", path);
            return OperationResult<IEnumerable<string>>.Failure($"Ошибка при получении директорий: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<string>>> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => GetDirectories(path), cancellationToken);
    }

    public OperationResult<IEnumerable<string>> CleanFolder(string path, bool needSysClean = false, string[]? ignoreDirectories = null, string[]? cleanDirectories = null)
    {
        return CleanFolderAsync(path, needSysClean, ignoreDirectories, cleanDirectories).GetAwaiter().GetResult();
    }

    public async Task<OperationResult<IEnumerable<string>>> CleanFolderAsync(
        string path, 
        bool needSysClean = false, 
        string[]? ignoreDirectories = null, 
        string[]? cleanDirectories = null, 
        CancellationToken cancellationToken = default, 
        IProgress<string>? progress = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _statistics.Reset();

        try
        {
            // Валидация пути
            var validationResult = PathValidator.ValidateDirectoryPath(path);
            if (!validationResult.IsSuccess)
                return OperationResult<IEnumerable<string>>.Failure(validationResult.ErrorMessage);

            // Получаем активный профиль
            var profileResult = await _profileManager.GetActiveProfileAsync(cancellationToken);
            if (!profileResult.IsSuccess)
                return OperationResult<IEnumerable<string>>.Failure(profileResult.ErrorMessage);

            var profile = profileResult.Value ?? await CreateDefaultProfileAsync();

            // Получаем предварительный просмотр
            progress?.Report("🔍 Анализ файлов для удаления...");
            var previewResult = await _previewService.GetPreviewAsync(path, profile, cancellationToken);
            if (!previewResult.IsSuccess)
                return OperationResult<IEnumerable<string>>.Failure(previewResult.ErrorMessage);

            var previewItems = previewResult.Value!.ToList();
            if (!previewItems.Any())
            {
                progress?.Report("✅ Нет файлов для удаления");
                return OperationResult<IEnumerable<string>>.Success(Enumerable.Empty<string>());
            }

            // Показываем предварительный просмотр
            if (profile.ShowPreview)
            {
                await ShowPreviewAsync(previewItems, progress);
            }

            // Создаем резервную копию если нужно
            BackupInfo? backupInfo = null;
            if (profile.CreateBackups)
            {
                progress?.Report("💾 Создание резервной копии...");
                var backupResult = await _backupService.CreateBackupAsync(
                    previewItems, 
                    profile.BackupPath, 
                    cancellationToken);
                
                if (backupResult.IsSuccess)
                {
                    backupInfo = backupResult.Value;
                    progress?.Report($"✅ Резервная копия создана: {backupInfo.Id}");
                }
                else
                {
                    _logger.LogWarning("Не удалось создать резервную копию: {Error}", backupResult.ErrorMessage);
                }
            }

            // Подтверждение удаления
            if (profile.ConfirmDeletion)
            {
                progress?.Report($"⚠️  Найдено {previewItems.Count} файлов для удаления. Продолжить? (Y/N)");
                // В реальном приложении здесь будет интерактивный ввод
            }

            // Выполняем очистку
            progress?.Report("🗑️  Удаление файлов...");
            var deletedFiles = await PerformCleanupAsync(previewItems, profile, progress, cancellationToken);

            // Обновляем статистику
            _statistics.TotalProcessedFolders = 1;
            _statistics.DeletedFolders = deletedFiles.Count;
            _statistics.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            _statistics.LastCleanupTime = DateTime.UtcNow;

            // Кэшируем результаты
            if (profile.EnableCaching)
            {
                await CacheResultsAsync(path, deletedFiles, profile, cancellationToken);
            }

            progress?.Report($"✅ Очистка завершена. Удалено {deletedFiles.Count} файлов за {stopwatch.Elapsed.TotalSeconds:F1}с");
            
            return OperationResult<IEnumerable<string>>.Success(deletedFiles);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Операция очистки отменена для {Path}", path);
            return OperationResult<IEnumerable<string>>.Failure("Операция отменена");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке папки {Path}", path);
            _statistics.Errors++;
            return OperationResult<IEnumerable<string>>.Failure($"Ошибка при очистке: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public CleanupStatistics GetStatistics()
    {
        return _statistics;
    }

    private async Task<ConfigurationProfile> CreateDefaultProfileAsync()
    {
        var defaultProfileResult = await _profileManager.CreateDefaultProfileAsync();
        if (defaultProfileResult.IsSuccess)
        {
            return defaultProfileResult.Value!;
        }
        
        // Fallback к базовому профилю
        return new ConfigurationProfile
        {
            Name = "Default",
            Description = "Профиль по умолчанию",
            IgnoreDirectories = _options.DefaultIgnoreDirectories,
            CleanDirectories = _options.DefaultCleanDirectories,
            EnableSystemClean = _options.EnableSystemClean,
            LogLevel = _options.LogLevel
        };
    }

    private async Task ShowPreviewAsync(List<FilePreviewItem> previewItems, IProgress<string>? progress)
    {
        var statisticsResult = await _previewService.GetPreviewStatisticsAsync(previewItems);
        if (statisticsResult.IsSuccess)
        {
            var stats = statisticsResult.Value!;
            progress?.Report($"📊 Статистика: {stats.TotalFiles} файлов, {stats.FormattedTotalSize}, {stats.SafeFilesPercentage:F1}% безопасных");
            
            // Показываем топ-10 файлов по размеру
            var topFiles = previewItems
                .OrderByDescending(f => f.Size)
                .Take(10)
                .ToList();

            progress?.Report("📋 Топ файлов для удаления:");
            foreach (var file in topFiles)
            {
                var icon = file.GetFileIcon();
                var priority = file.GetPriorityColor();
                var size = file.GetFormattedSize();
                var warnings = file.Warnings.Any() ? $" ⚠️ {string.Join(", ", file.Warnings)}" : "";
                
                progress?.Report($"  {icon} {priority} {file.Name} ({size}){warnings}");
            }
        }
    }

    private async Task<List<string>> PerformCleanupAsync(
        List<FilePreviewItem> previewItems, 
        ConfigurationProfile profile, 
        IProgress<string>? progress, 
        CancellationToken cancellationToken)
    {
        var deletedFiles = new List<string>();
        var safeFiles = previewItems.Where(f => f.IsSafeToDelete).ToList();
        var unsafeFiles = previewItems.Where(f => !f.IsSafeToDelete).ToList();

        if (unsafeFiles.Any())
        {
            _logger.LogWarning("Пропущено {Count} небезопасных файлов", unsafeFiles.Count);
            progress?.Report($"⚠️  Пропущено {unsafeFiles.Count} небезопасных файлов");
        }

        if (profile.EnableParallelProcessing)
        {
            deletedFiles = await PerformParallelCleanupAsync(safeFiles, profile, progress, cancellationToken);
        }
        else
        {
            deletedFiles = await PerformSequentialCleanupAsync(safeFiles, progress, cancellationToken);
        }

        return deletedFiles;
    }

    private async Task<List<string>> PerformParallelCleanupAsync(
        List<FilePreviewItem> files, 
        ConfigurationProfile profile, 
        IProgress<string>? progress, 
        CancellationToken cancellationToken)
    {
        var deletedFiles = new ConcurrentBag<string>();
        var semaphore = new SemaphoreSlim(profile.MaxParallelThreads, profile.MaxParallelThreads);
        
        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (File.Exists(file.FullPath))
                {
                    File.Delete(file.FullPath);
                    deletedFiles.Add(file.FullPath);
                    _logger.LogDebug("Удален файл: {FilePath}", file.FullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить файл: {FilePath}", file.FullPath);
                _statistics.Errors++;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return deletedFiles.ToList();
    }

    private async Task<List<string>> PerformSequentialCleanupAsync(
        List<FilePreviewItem> files, 
        IProgress<string>? progress, 
        CancellationToken cancellationToken)
    {
        var deletedFiles = new List<string>();
        
        for (int i = 0; i < files.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var file = files[i];
            try
            {
                if (File.Exists(file.FullPath))
                {
                    File.Delete(file.FullPath);
                    deletedFiles.Add(file.FullPath);
                    _logger.LogDebug("Удален файл: {FilePath}", file.FullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить файл: {FilePath}", file.FullPath);
                _statistics.Errors++;
            }

            if (i % 10 == 0)
            {
                progress?.Report($"🗑️  Обработано {i + 1}/{files.Count} файлов");
            }
        }

        return deletedFiles;
    }

    private async Task CacheResultsAsync(
        string path, 
        List<string> deletedFiles, 
        ConfigurationProfile profile, 
        CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"cleanup_{path.GetHashCode()}";
            var cacheData = new
            {
                Path = path,
                DeletedFiles = deletedFiles,
                Timestamp = DateTime.UtcNow,
                FileCount = deletedFiles.Count
            };

            var expiration = TimeSpan.FromMinutes(profile.CacheLifetimeMinutes);
            await _cacheService.SetAsync(cacheKey, cacheData, expiration, cancellationToken);
            
            _logger.LogDebug("Результаты очистки закэшированы для {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось закэшировать результаты очистки для {Path}", path);
        }
    }
}