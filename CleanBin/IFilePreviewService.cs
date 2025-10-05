using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanBin;

/// <summary>
/// Интерфейс для предварительного просмотра файлов
/// </summary>
public interface IFilePreviewService
{
    /// <summary>
    /// Получает предварительный просмотр файлов для удаления
    /// </summary>
    Task<OperationResult<IEnumerable<FilePreviewItem>>> GetPreviewAsync(
        string path, 
        ConfigurationProfile profile, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает предварительный просмотр файлов с фильтрацией
    /// </summary>
    Task<OperationResult<IEnumerable<FilePreviewItem>>> GetPreviewAsync(
        string path, 
        ConfigurationProfile profile,
        string[] fileFilters,
        long maxFileSize,
        int maxFileAge,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает статистику предварительного просмотра
    /// </summary>
    Task<OperationResult<PreviewStatistics>> GetPreviewStatisticsAsync(
        IEnumerable<FilePreviewItem> previewItems,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Фильтрует файлы по критериям
    /// </summary>
    Task<OperationResult<IEnumerable<FilePreviewItem>>> FilterFilesAsync(
        IEnumerable<FilePreviewItem> files,
        ConfigurationProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет безопасность удаления файлов
    /// </summary>
    Task<OperationResult<IEnumerable<FilePreviewItem>>> ValidateSafetyAsync(
        IEnumerable<FilePreviewItem> files,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Статистика предварительного просмотра
/// </summary>
public class PreviewStatistics
{
    /// <summary>
    /// Общее количество файлов
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Общий размер файлов в байтах
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Количество безопасных для удаления файлов
    /// </summary>
    public int SafeToDeleteFiles { get; set; }

    /// <summary>
    /// Количество файлов с предупреждениями
    /// </summary>
    public int FilesWithWarnings { get; set; }

    /// <summary>
    /// Количество файлов по приоритетам
    /// </summary>
    public Dictionary<int, int> FilesByPriority { get; set; } = new();

    /// <summary>
    /// Количество файлов по типам
    /// </summary>
    public Dictionary<string, int> FilesByType { get; set; } = new();

    /// <summary>
    /// Количество файлов по причинам удаления
    /// </summary>
    public Dictionary<string, int> FilesByReason { get; set; } = new();

    /// <summary>
    /// Форматированный общий размер
    /// </summary>
    public string FormattedTotalSize => FormatFileSize(TotalSize);

    /// <summary>
    /// Процент безопасных файлов
    /// </summary>
    public double SafeFilesPercentage => TotalFiles > 0 ? (double)SafeToDeleteFiles / TotalFiles * 100 : 0;

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number = number / 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}