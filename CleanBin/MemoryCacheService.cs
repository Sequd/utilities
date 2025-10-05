using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CleanBin;

/// <summary>
/// Сервис кэширования в памяти
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly Timer _cleanupTimer;
    private long _hitCount;
    private long _missCount;

    public MemoryCacheService(ILogger<MemoryCacheService> logger)
    {
        _logger = logger;
        
        // Запускаем таймер для очистки истекших записей каждые 5 минут
        _cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<OperationResult<T?>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return OperationResult<T?>.Failure("Ключ не может быть пустым");

            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    Interlocked.Increment(ref _hitCount);
                    entry.AccessCount++;
                    entry.LastAccessed = DateTime.UtcNow;

                    if (entry.Value is T directValue)
                    {
                        return OperationResult<T?>.Success(directValue);
                    }

                    if (entry.Value is string jsonValue)
                    {
                        var deserializedValue = JsonSerializer.Deserialize<T>(jsonValue);
                        return OperationResult<T?>.Success(deserializedValue);
                    }

                    return OperationResult<T?>.Success((T)entry.Value);
                }
                else
                {
                    // Запись истекла, удаляем её
                    _cache.TryRemove(key, out _);
                }
            }

            Interlocked.Increment(ref _missCount);
            return OperationResult<T?>.Success(default(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении значения из кэша для ключа: {Key}", key);
            return OperationResult<T?>.Failure($"Ошибка при получении из кэша: {ex.Message}");
        }
    }

    public async Task<OperationResult> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return OperationResult.Failure("Ключ не может быть пустым");

            var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(1));
            var size = CalculateSize(value);
            
            var entry = new CacheEntry
            {
                Key = key,
                Value = value,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                Size = size,
                AccessCount = 0,
                LastAccessed = DateTime.UtcNow
            };

            _cache.AddOrUpdate(key, entry, (k, existing) => entry);

            _logger.LogDebug("Сохранено в кэш: {Key}, размер: {Size} байт", key, size);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении значения в кэш для ключа: {Key}", key);
            return OperationResult.Failure($"Ошибка при сохранении в кэш: {ex.Message}");
        }
    }

    public async Task<OperationResult> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return OperationResult.Failure("Ключ не может быть пустым");

            var removed = _cache.TryRemove(key, out _);
            if (removed)
            {
                _logger.LogDebug("Удалено из кэша: {Key}", key);
            }

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении значения из кэша для ключа: {Key}", key);
            return OperationResult.Failure($"Ошибка при удалении из кэша: {ex.Message}");
        }
    }

    public async Task<OperationResult> ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = _cache.Count;
            _cache.Clear();
            
            _logger.LogInformation("Очищен кэш, удалено {Count} записей", count);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке кэша");
            return OperationResult.Failure($"Ошибка при очистке кэша: {ex.Message}");
        }
    }

    public async Task<OperationResult<bool>> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return OperationResult<bool>.Failure("Ключ не может быть пустым");

            var exists = _cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow;
            return OperationResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке существования ключа в кэше: {Key}", key);
            return OperationResult<bool>.Failure($"Ошибка при проверке существования: {ex.Message}");
        }
    }

    public async Task<OperationResult<IEnumerable<string>>> GetKeysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var validKeys = _cache
                .Where(kvp => kvp.Value.ExpiresAt > now)
                .Select(kvp => kvp.Key)
                .ToList();

            return OperationResult<IEnumerable<string>>.Success(validKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении ключей кэша");
            return OperationResult<IEnumerable<string>>.Failure($"Ошибка при получении ключей: {ex.Message}");
        }
    }

    public async Task<OperationResult<CacheStatistics>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var entries = _cache.Values.ToList();
            
            var statistics = new CacheStatistics
            {
                TotalEntries = entries.Count,
                ExpiredEntries = entries.Count(e => e.ExpiresAt <= now),
                TotalSize = entries.Sum(e => e.Size),
                HitCount = Interlocked.Read(ref _hitCount),
                MissCount = Interlocked.Read(ref _missCount)
            };

            return OperationResult<CacheStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики кэша");
            return OperationResult<CacheStatistics>.Failure($"Ошибка при получении статистики: {ex.Message}");
        }
    }

    public async Task<OperationResult<int>> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.ExpiresAt <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            var removedCount = 0;
            foreach (var key in expiredKeys)
            {
                if (_cache.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _logger.LogDebug("Очищено {Count} истекших записей из кэша", removedCount);
            }

            return OperationResult<int>.Success(removedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке истекших записей кэша");
            return OperationResult<int>.Failure($"Ошибка при очистке кэша: {ex.Message}");
        }
    }

    private void PerformCleanup(object? state)
    {
        try
        {
            var cleanupResult = CleanupExpiredAsync().Result;
            if (cleanupResult.IsSuccess && cleanupResult.Value > 0)
            {
                _logger.LogDebug("Автоматическая очистка кэша: удалено {Count} записей", cleanupResult.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при автоматической очистке кэша");
        }
    }

    private static long CalculateSize<T>(T value)
    {
        try
        {
            if (value == null)
                return 0;

            if (value is string str)
                return str.Length * 2; // Unicode characters

            if (value is byte[] bytes)
                return bytes.Length;

            // Для сложных объектов используем JSON сериализацию
            var json = JsonSerializer.Serialize(value);
            return json.Length * 2; // Unicode characters
        }
        catch
        {
            // Если не можем вычислить размер, возвращаем примерную оценку
            return 1024; // 1KB по умолчанию
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}