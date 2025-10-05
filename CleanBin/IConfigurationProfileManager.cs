using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanBin;

/// <summary>
/// Интерфейс для управления профилями конфигурации
/// </summary>
public interface IConfigurationProfileManager
{
    /// <summary>
    /// Получает все профили
    /// </summary>
    Task<OperationResult<IEnumerable<ConfigurationProfile>>> GetProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает профиль по имени
    /// </summary>
    Task<OperationResult<ConfigurationProfile?>> GetProfileAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет профиль
    /// </summary>
    Task<OperationResult> SaveProfileAsync(ConfigurationProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет профиль
    /// </summary>
    Task<OperationResult> DeleteProfileAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Активирует профиль
    /// </summary>
    Task<OperationResult> ActivateProfileAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает активный профиль
    /// </summary>
    Task<OperationResult<ConfigurationProfile?>> GetActiveProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает профиль по умолчанию
    /// </summary>
    Task<OperationResult<ConfigurationProfile>> CreateDefaultProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Импортирует профили из файла
    /// </summary>
    Task<OperationResult> ImportProfilesAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Экспортирует профили в файл
    /// </summary>
    Task<OperationResult> ExportProfilesAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет существование профиля
    /// </summary>
    Task<OperationResult<bool>> ProfileExistsAsync(string name, CancellationToken cancellationToken = default);
}