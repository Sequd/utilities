using CleanBin;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanBin.Tests;

public class MemoryCacheServiceTests : IDisposable
{
    private readonly Mock<ILogger<MemoryCacheService>> _mockLogger;
    private readonly MemoryCacheService _service;

    public MemoryCacheServiceTests()
    {
        _mockLogger = new Mock<ILogger<MemoryCacheService>>();
        _service = new MemoryCacheService(_mockLogger.Object);
    }

    [Fact]
    public async Task SetAsync_WithValidKeyAndValue_ShouldSucceed()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        var result = await _service.SetAsync(key, value);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsync_WithEmptyKey_ShouldFail()
    {
        // Arrange
        var value = "test-value";

        // Act
        var result = await _service.SetAsync("", value);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Ключ не может быть пустым");
    }

    [Fact]
    public async Task GetAsync_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _service.SetAsync(key, value);

        // Act
        var result = await _service.GetAsync<string>(key);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = await _service.GetAsync<string>(key);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithExpiredKey_ShouldReturnNull()
    {
        // Arrange
        var key = "expired-key";
        var value = "test-value";
        await _service.SetAsync(key, value, TimeSpan.FromMilliseconds(1));
        
        // Wait for expiration
        await Task.Delay(10);

        // Act
        var result = await _service.GetAsync<string>(key);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithExistingKey_ShouldSucceed()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _service.SetAsync(key, value);

        // Act
        var result = await _service.RemoveAsync(key);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify key is removed
        var getResult = await _service.GetAsync<string>(key);
        getResult.Value.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithNonExistingKey_ShouldSucceed()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = await _service.RemoveAsync(key);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ClearAsync_WithExistingKeys_ShouldSucceed()
    {
        // Arrange
        await _service.SetAsync("key1", "value1");
        await _service.SetAsync("key2", "value2");

        // Act
        var result = await _service.ClearAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify all keys are removed
        var getResult1 = await _service.GetAsync<string>("key1");
        var getResult2 = await _service.GetAsync<string>("key2");
        getResult1.Value.Should().BeNull();
        getResult2.Value.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _service.SetAsync(key, value);

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingKey_ShouldReturnFalse()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithExpiredKey_ShouldReturnFalse()
    {
        // Arrange
        var key = "expired-key";
        var value = "test-value";
        await _service.SetAsync(key, value, TimeSpan.FromMilliseconds(1));
        
        // Wait for expiration
        await Task.Delay(10);

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task GetKeysAsync_WithExistingKeys_ShouldReturnKeys()
    {
        // Arrange
        await _service.SetAsync("key1", "value1");
        await _service.SetAsync("key2", "value2");

        // Act
        var result = await _service.GetKeysAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().Contain("key1");
        result.Value.Should().Contain("key2");
    }

    [Fact]
    public async Task GetKeysAsync_WithNoKeys_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetKeysAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatisticsAsync_WithOperations_ShouldReturnStatistics()
    {
        // Arrange
        await _service.SetAsync("key1", "value1");
        await _service.SetAsync("key2", "value2");
        await _service.GetAsync<string>("key1"); // Hit
        await _service.GetAsync<string>("key3"); // Miss

        // Act
        var result = await _service.GetStatisticsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalEntries.Should().Be(2);
        result.Value.HitCount.Should().Be(1);
        result.Value.MissCount.Should().Be(1);
        result.Value.HitRate.Should().Be(50.0);
    }

    [Fact]
    public async Task CleanupExpiredAsync_WithExpiredKeys_ShouldRemoveExpiredKeys()
    {
        // Arrange
        await _service.SetAsync("key1", "value1", TimeSpan.FromMilliseconds(1));
        await _service.SetAsync("key2", "value2", TimeSpan.FromHours(1));
        
        // Wait for key1 to expire
        await Task.Delay(10);

        // Act
        var result = await _service.CleanupExpiredAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        
        // Verify only key2 remains
        var getResult1 = await _service.GetAsync<string>("key1");
        var getResult2 = await _service.GetAsync<string>("key2");
        getResult1.Value.Should().BeNull();
        getResult2.Value.Should().Be("value2");
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_ShouldSucceed()
    {
        // Arrange
        var key = "complex-key";
        var value = new { Name = "Test", Value = 123, Items = new[] { "item1", "item2" } };

        // Act
        var result = await _service.SetAsync(key, value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify we can retrieve it
        var getResult = await _service.GetAsync<object>(key);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiration_ShouldRespectExpiration()
    {
        // Arrange
        var key = "expiring-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _service.SetAsync(key, value, expiration);
        
        // Verify it exists initially
        var initialResult = await _service.GetAsync<string>(key);
        initialResult.Value.Should().Be(value);
        
        // Wait for expiration
        await Task.Delay(150);
        
        // Verify it's expired
        var expiredResult = await _service.GetAsync<string>(key);
        expiredResult.Value.Should().BeNull();
    }

    [Fact]
    public void CacheStatistics_FormattedSize_ShouldFormatCorrectly()
    {
        // Arrange
        var statistics = new CacheStatistics { TotalSize = 1024 * 1024 }; // 1MB

        // Act
        var formattedSize = statistics.FormattedSize;

        // Assert
        formattedSize.Should().Be("1.0 MB");
    }

    [Fact]
    public void CacheStatistics_HitRate_WithNoOperations_ShouldBeZero()
    {
        // Arrange
        var statistics = new CacheStatistics { HitCount = 0, MissCount = 0 };

        // Act
        var hitRate = statistics.HitRate;

        // Assert
        hitRate.Should().Be(0.0);
    }

    [Fact]
    public void CacheStatistics_HitRate_WithOperations_ShouldCalculateCorrectly()
    {
        // Arrange
        var statistics = new CacheStatistics { HitCount = 3, MissCount = 2 };

        // Act
        var hitRate = statistics.HitRate;

        // Assert
        hitRate.Should().Be(60.0);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}