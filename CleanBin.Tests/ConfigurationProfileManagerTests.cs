using CleanBin;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CleanBin.Tests;

public class ConfigurationProfileManagerTests : IDisposable
{
    private readonly Mock<ILogger<ConfigurationProfileManager>> _mockLogger;
    private readonly Mock<IOptions<CleanBinOptions>> _mockOptions;
    private readonly ConfigurationProfileManager _manager;
    private readonly string _tempDirectory;

    public ConfigurationProfileManagerTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationProfileManager>>();
        _mockOptions = new Mock<IOptions<CleanBinOptions>>();
        
        _mockOptions.Setup(x => x.Value).Returns(new CleanBinOptions
        {
            DefaultIgnoreDirectories = new[] { "node_modules", ".git" },
            DefaultCleanDirectories = new[] { "bin", "obj" },
            EnableSystemClean = false,
            LogLevel = "Information"
        });

        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        
        // Создаем временный AppData для тестов
        Environment.SetEnvironmentVariable("APPDATA", _tempDirectory);
        
        _manager = new ConfigurationProfileManager(_mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task CreateDefaultProfileAsync_ShouldCreateDefaultProfile()
    {
        // Act
        var result = await _manager.CreateDefaultProfileAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Default");
        result.Value.Description.Should().Be("Профиль по умолчанию");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SaveProfileAsync_WithValidProfile_ShouldSucceed()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "TestProfile",
            Description = "Test Description",
            IgnoreDirectories = new[] { "test" },
            CleanDirectories = new[] { "clean" }
        };

        // Act
        var result = await _manager.SaveProfileAsync(profile);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SaveProfileAsync_WithInvalidProfile_ShouldFail()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "", // Invalid: empty name
            Description = "Test Description"
        };

        // Act
        var result = await _manager.SaveProfileAsync(profile);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Имя профиля не может быть пустым");
    }

    [Fact]
    public async Task GetProfileAsync_WithExistingProfile_ShouldReturnProfile()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "TestProfile",
            Description = "Test Description"
        };
        await _manager.SaveProfileAsync(profile);

        // Act
        var result = await _manager.GetProfileAsync("TestProfile");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("TestProfile");
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistingProfile_ShouldReturnNull()
    {
        // Act
        var result = await _manager.GetProfileAsync("NonExistingProfile");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfileAsync_WithExistingProfile_ShouldSucceed()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "TestProfile",
            Description = "Test Description"
        };
        await _manager.SaveProfileAsync(profile);

        // Act
        var result = await _manager.DeleteProfileAsync("TestProfile");

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify profile is deleted
        var getResult = await _manager.GetProfileAsync("TestProfile");
        getResult.Value.Should().BeNull();
    }

    [Fact]
    public async Task ActivateProfileAsync_WithExistingProfile_ShouldSucceed()
    {
        // Arrange
        var profile1 = new ConfigurationProfile
        {
            Name = "Profile1",
            Description = "Profile 1"
        };
        var profile2 = new ConfigurationProfile
        {
            Name = "Profile2",
            Description = "Profile 2",
            IsActive = true
        };
        
        await _manager.SaveProfileAsync(profile1);
        await _manager.SaveProfileAsync(profile2);

        // Act
        var result = await _manager.ActivateProfileAsync("Profile1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify Profile1 is active and Profile2 is not
        var activeProfile = await _manager.GetActiveProfileAsync();
        activeProfile.IsSuccess.Should().BeTrue();
        activeProfile.Value!.Name.Should().Be("Profile1");
    }

    [Fact]
    public async Task GetProfilesAsync_ShouldReturnAllProfiles()
    {
        // Arrange
        var profile1 = new ConfigurationProfile
        {
            Name = "Profile1",
            Description = "Profile 1"
        };
        var profile2 = new ConfigurationProfile
        {
            Name = "Profile2",
            Description = "Profile 2"
        };
        
        await _manager.SaveProfileAsync(profile1);
        await _manager.SaveProfileAsync(profile2);

        // Act
        var result = await _manager.GetProfilesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(p => p.Name == "Profile1");
        result.Value.Should().Contain(p => p.Name == "Profile2");
    }

    [Fact]
    public async Task ProfileExistsAsync_WithExistingProfile_ShouldReturnTrue()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "TestProfile",
            Description = "Test Description"
        };
        await _manager.SaveProfileAsync(profile);

        // Act
        var result = await _manager.ProfileExistsAsync("TestProfile");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ProfileExistsAsync_WithNonExistingProfile_ShouldReturnFalse()
    {
        // Act
        var result = await _manager.ProfileExistsAsync("NonExistingProfile");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task ExportProfilesAsync_WithProfiles_ShouldSucceed()
    {
        // Arrange
        var profile = new ConfigurationProfile
        {
            Name = "TestProfile",
            Description = "Test Description"
        };
        await _manager.SaveProfileAsync(profile);
        
        var exportPath = Path.Combine(_tempDirectory, "export.json");

        // Act
        var result = await _manager.ExportProfilesAsync(exportPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(exportPath).Should().BeTrue();
    }

    [Fact]
    public async Task ImportProfilesAsync_WithValidFile_ShouldSucceed()
    {
        // Arrange
        var profiles = new List<ConfigurationProfile>
        {
            new() { Name = "ImportedProfile1", Description = "Imported 1" },
            new() { Name = "ImportedProfile2", Description = "Imported 2" }
        };
        
        var exportPath = Path.Combine(_tempDirectory, "import.json");
        var json = System.Text.Json.JsonSerializer.Serialize(profiles, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(exportPath, json);

        // Act
        var result = await _manager.ImportProfilesAsync(exportPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Verify profiles were imported
        var getResult = await _manager.GetProfilesAsync();
        getResult.Value.Should().Contain(p => p.Name == "ImportedProfile1");
        getResult.Value.Should().Contain(p => p.Name == "ImportedProfile2");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}