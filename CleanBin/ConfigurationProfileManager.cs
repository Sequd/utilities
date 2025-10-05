using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanBin;

/// <summary>
/// Менеджер профилей конфигурации
/// </summary>
public class ConfigurationProfileManager : IConfigurationProfileManager
{
    private readonly ILogger<ConfigurationProfileManager> _logger;
    private readonly CleanBinOptions _options;
    private readonly string _profilesDirectory;
    private readonly string _profilesFile;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public ConfigurationProfileManager(
        ILogger<ConfigurationProfileManager> logger,
        IOptions<CleanBinOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CleanBin");
        
        _profilesDirectory = Path.Combine(appDataPath, "Profiles");
        _profilesFile = Path.Combine(_profilesDirectory, "profiles.json");
        
        EnsureProfilesDirectoryExists();
    }

    public async Task<OperationResult<IEnumerable<ConfigurationProfile>>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            
            if (!File.Exists(_profilesFile))
            {
                _logger.LogInformation("Файл профилей не найден, создаю профиль по умолчанию");
                var defaultProfileResult = await CreateDefaultProfileAsync(cancellationToken);
                if (!defaultProfileResult.IsSuccess)
                    return OperationResult<IEnumerable<ConfigurationProfile>>.Failure(defaultProfileResult.ErrorMessage);
                
                return OperationResult<IEnumerable<ConfigurationProfile>>.Success(new[] { defaultProfileResult.Value! });
            }

            var json = await File.ReadAllTextAsync(_profilesFile, cancellationToken);
            var profiles = JsonSerializer.Deserialize<List<ConfigurationProfile>>(json, GetJsonOptions());
            
            if (profiles == null)
                return OperationResult<IEnumerable<ConfigurationProfile>>.Failure("Не удалось десериализовать профили");

            _logger.LogInformation("Загружено {Count} профилей", profiles.Count);
            return OperationResult<IEnumerable<ConfigurationProfile>>.Success(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении профилей");
            return OperationResult<IEnumerable<ConfigurationProfile>>.Failure($"Ошибка при получении профилей: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<OperationResult<ConfigurationProfile?>> GetProfileAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult<ConfigurationProfile?>.Failure("Имя профиля не может быть пустым");

        var profilesResult = await GetProfilesAsync(cancellationToken);
        if (!profilesResult.IsSuccess)
            return OperationResult<ConfigurationProfile?>.Failure(profilesResult.ErrorMessage);

        var profile = profilesResult.Value!.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return OperationResult<ConfigurationProfile?>.Success(profile);
    }

    public async Task<OperationResult> SaveProfileAsync(ConfigurationProfile profile, CancellationToken cancellationToken = default)
    {
        if (profile == null)
            return OperationResult.Failure("Профиль не может быть null");

        var validationResult = profile.Validate();
        if (!validationResult.IsSuccess)
            return validationResult;

        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            var profilesResult = await GetProfilesAsync(cancellationToken);
            if (!profilesResult.IsSuccess)
                return profilesResult;

            var profiles = profilesResult.Value!.ToList();
            var existingIndex = profiles.FindIndex(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));

            profile.UpdateLastModified();

            if (existingIndex >= 0)
            {
                profiles[existingIndex] = profile;
                _logger.LogInformation("Обновлен профиль: {Name}", profile.Name);
            }
            else
            {
                profiles.Add(profile);
                _logger.LogInformation("Создан новый профиль: {Name}", profile.Name);
            }

            await SaveProfilesToFileAsync(profiles, cancellationToken);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении профиля {Name}", profile.Name);
            return OperationResult.Failure($"Ошибка при сохранении профиля: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<OperationResult> DeleteProfileAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult.Failure("Имя профиля не может быть пустым");

        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            var profilesResult = await GetProfilesAsync(cancellationToken);
            if (!profilesResult.IsSuccess)
                return profilesResult;

            var profiles = profilesResult.Value!.ToList();
            var profileToRemove = profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (profileToRemove == null)
                return OperationResult.Failure($"Профиль '{name}' не найден");

            profiles.Remove(profileToRemove);
            await SaveProfilesToFileAsync(profiles, cancellationToken);

            _logger.LogInformation("Удален профиль: {Name}", name);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении профиля {Name}", name);
            return OperationResult.Failure($"Ошибка при удалении профиля: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<OperationResult> ActivateProfileAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult.Failure("Имя профиля не может быть пустым");

        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            var profilesResult = await GetProfilesAsync(cancellationToken);
            if (!profilesResult.IsSuccess)
                return profilesResult;

            var profiles = profilesResult.Value!.ToList();
            var targetProfile = profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (targetProfile == null)
                return OperationResult.Failure($"Профиль '{name}' не найден");

            // Деактивируем все профили
            foreach (var profile in profiles)
            {
                profile.IsActive = false;
            }

            // Активируем целевой профиль
            targetProfile.IsActive = true;
            targetProfile.UpdateLastModified();

            await SaveProfilesToFileAsync(profiles, cancellationToken);

            _logger.LogInformation("Активирован профиль: {Name}", name);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при активации профиля {Name}", name);
            return OperationResult.Failure($"Ошибка при активации профиля: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<OperationResult<ConfigurationProfile?>> GetActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        var profilesResult = await GetProfilesAsync(cancellationToken);
        if (!profilesResult.IsSuccess)
            return OperationResult<ConfigurationProfile?>.Failure(profilesResult.ErrorMessage);

        var activeProfile = profilesResult.Value!.FirstOrDefault(p => p.IsActive);
        return OperationResult<ConfigurationProfile?>.Success(activeProfile);
    }

    public async Task<OperationResult<ConfigurationProfile>> CreateDefaultProfileAsync(CancellationToken cancellationToken = default)
    {
        var defaultProfile = new ConfigurationProfile
        {
            Name = "Default",
            Description = "Профиль по умолчанию",
            IgnoreDirectories = _options.DefaultIgnoreDirectories,
            CleanDirectories = _options.DefaultCleanDirectories,
            EnableSystemClean = _options.EnableSystemClean,
            LogLevel = _options.LogLevel,
            CreateBackups = false,
            BackupPath = string.Empty,
            FileFilters = new[] { "*.tmp", "*.temp", "*.log" },
            MaxFileSize = 100 * 1024 * 1024, // 100 MB
            MaxFileAge = 30, // 30 дней
            EnableParallelProcessing = true,
            MaxParallelThreads = Environment.ProcessorCount,
            ShowPreview = true,
            ConfirmDeletion = true,
            EnableCaching = true,
            CacheLifetimeMinutes = 60,
            IsActive = true
        };

        var saveResult = await SaveProfileAsync(defaultProfile, cancellationToken);
        if (!saveResult.IsSuccess)
            return OperationResult<ConfigurationProfile>.Failure(saveResult.ErrorMessage);

        return OperationResult<ConfigurationProfile>.Success(defaultProfile);
    }

    public async Task<OperationResult> ImportProfilesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return OperationResult.Failure("Путь к файлу не может быть пустым");

        if (!File.Exists(filePath))
            return OperationResult.Failure($"Файл не найден: {filePath}");

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var importedProfiles = JsonSerializer.Deserialize<List<ConfigurationProfile>>(json, GetJsonOptions());

            if (importedProfiles == null)
                return OperationResult.Failure("Не удалось десериализовать импортируемые профили");

            var profilesResult = await GetProfilesAsync(cancellationToken);
            if (!profilesResult.IsSuccess)
                return profilesResult;

            var existingProfiles = profilesResult.Value!.ToList();

            foreach (var importedProfile in importedProfiles)
            {
                var validationResult = importedProfile.Validate();
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning("Пропущен невалидный профиль: {Name} - {Error}", 
                        importedProfile.Name, validationResult.ErrorMessage);
                    continue;
                }

                var existingIndex = existingProfiles.FindIndex(p => p.Name.Equals(importedProfile.Name, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    existingProfiles[existingIndex] = importedProfile;
                    _logger.LogInformation("Обновлен импортированный профиль: {Name}", importedProfile.Name);
                }
                else
                {
                    existingProfiles.Add(importedProfile);
                    _logger.LogInformation("Добавлен импортированный профиль: {Name}", importedProfile.Name);
                }
            }

            await SaveProfilesToFileAsync(existingProfiles, cancellationToken);
            _logger.LogInformation("Импортировано {Count} профилей из {FilePath}", importedProfiles.Count, filePath);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при импорте профилей из {FilePath}", filePath);
            return OperationResult.Failure($"Ошибка при импорте профилей: {ex.Message}");
        }
    }

    public async Task<OperationResult> ExportProfilesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return OperationResult.Failure("Путь к файлу не может быть пустым");

        try
        {
            var profilesResult = await GetProfilesAsync(cancellationToken);
            if (!profilesResult.IsSuccess)
                return profilesResult;

            var json = JsonSerializer.Serialize(profilesResult.Value, GetJsonOptions());
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogInformation("Экспортировано {Count} профилей в {FilePath}", 
                profilesResult.Value!.Count(), filePath);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при экспорте профилей в {FilePath}", filePath);
            return OperationResult.Failure($"Ошибка при экспорте профилей: {ex.Message}");
        }
    }

    public async Task<OperationResult<bool>> ProfileExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult<bool>.Failure("Имя профиля не может быть пустым");

        var profileResult = await GetProfileAsync(name, cancellationToken);
        if (!profileResult.IsSuccess)
            return OperationResult<bool>.Failure(profileResult.ErrorMessage);

        return OperationResult<bool>.Success(profileResult.Value != null);
    }

    private void EnsureProfilesDirectoryExists()
    {
        if (!Directory.Exists(_profilesDirectory))
        {
            Directory.CreateDirectory(_profilesDirectory);
            _logger.LogInformation("Создана директория профилей: {Directory}", _profilesDirectory);
        }
    }

    private async Task SaveProfilesToFileAsync(List<ConfigurationProfile> profiles, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(profiles, GetJsonOptions());
        await File.WriteAllTextAsync(_profilesFile, json, cancellationToken);
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}