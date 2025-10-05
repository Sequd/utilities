using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanBin;

/// <summary>
/// Интерфейс для кэширования
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Получает значение из кэша
    /// </summary>
    Task<OperationResult<T?>> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет значение в кэш
    /// </summary>
    Task<OperationResult> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет значение из кэша
    /// </summary>
    Task<OperationResult> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Очищает весь кэш
    /// </summary>
    Task<OperationResult> ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет существование ключа в кэше
    /// </summary>
    Task<OperationResult<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает все ключи в кэше
    /// </summary>
    Task<OperationResult<IEnumerable<string>>> GetKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает статистику кэша
    /// </summary>
    Task<OperationResult<CacheStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Очищает истекшие записи
    /// </summary>
    Task<OperationResult<int>> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Статистика кэша
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Общее количество записей
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Количество истекших записей
    /// </summary>
    public int ExpiredEntries { get; set; }

    /// <summary>
    /// Общий размер кэша в байтах
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// Количество попаданий в кэш
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// Количество промахов кэша
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// Процент попаданий
    /// </summary>
    public double HitRate => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) * 100 : 0;

    /// <summary>
    /// Форматированный размер
    /// </summary>
    public string FormattedSize => FormatFileSize(TotalSize);

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
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

/// <summary>
/// Запись в кэше
/// </summary>
internal class CacheEntry
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public long Size { get; set; }
    public int AccessCount { get; set; }
    public DateTime LastAccessed { get; set; }
}