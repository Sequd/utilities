using System.Text.Json.Serialization;

namespace CleanBin;

/// <summary>
/// Профиль конфигурации для CleanBin
/// </summary>
public class ConfigurationProfile
{
    /// <summary>
    /// Имя профиля
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Описание профиля
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Папки для игнорирования
    /// </summary>
    public string[] IgnoreDirectories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Папки для очистки
    /// </summary>
    public string[] CleanDirectories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Включить системную очистку
    /// </summary>
    public bool EnableSystemClean { get; set; } = false;

    /// <summary>
    /// Уровень логирования
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Создавать резервные копии
    /// </summary>
    public bool CreateBackups { get; set; } = false;

    /// <summary>
    /// Путь для резервных копий
    /// </summary>
    public string BackupPath { get; set; } = string.Empty;

    /// <summary>
    /// Фильтры файлов для исключения
    /// </summary>
    public string[] FileFilters { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Максимальный размер файла для удаления (в байтах)
    /// </summary>
    public long MaxFileSize { get; set; } = 0;

    /// <summary>
    /// Возраст файлов для удаления (в днях)
    /// </summary>
    public int MaxFileAge { get; set; } = 0;

    /// <summary>
    /// Параллельная обработка
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// Максимальное количество параллельных потоков
    /// </summary>
    public int MaxParallelThreads { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Показывать предварительный просмотр
    /// </summary>
    public bool ShowPreview { get; set; } = true;

    /// <summary>
    /// Подтверждать удаление
    /// </summary>
    public bool ConfirmDeletion { get; set; } = true;

    /// <summary>
    /// Включить кэширование
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Время жизни кэша (в минутах)
    /// </summary>
    public int CacheLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Дата создания профиля
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата последнего изменения
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Является ли профиль активным
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Создает копию профиля
    /// </summary>
    public ConfigurationProfile Clone()
    {
        return new ConfigurationProfile
        {
            Name = Name,
            Description = Description,
            IgnoreDirectories = IgnoreDirectories.ToArray(),
            CleanDirectories = CleanDirectories.ToArray(),
            EnableSystemClean = EnableSystemClean,
            LogLevel = LogLevel,
            CreateBackups = CreateBackups,
            BackupPath = BackupPath,
            FileFilters = FileFilters.ToArray(),
            MaxFileSize = MaxFileSize,
            MaxFileAge = MaxFileAge,
            EnableParallelProcessing = EnableParallelProcessing,
            MaxParallelThreads = MaxParallelThreads,
            ShowPreview = ShowPreview,
            ConfirmDeletion = ConfirmDeletion,
            EnableCaching = EnableCaching,
            CacheLifetimeMinutes = CacheLifetimeMinutes,
            CreatedAt = CreatedAt,
            LastModified = LastModified,
            IsActive = IsActive
        };
    }

    /// <summary>
    /// Обновляет время последнего изменения
    /// </summary>
    public void UpdateLastModified()
    {
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Валидирует профиль
    /// </summary>
    public OperationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return OperationResult.Failure("Имя профиля не может быть пустым");

        if (MaxFileSize < 0)
            return OperationResult.Failure("Максимальный размер файла не может быть отрицательным");

        if (MaxFileAge < 0)
            return OperationResult.Failure("Максимальный возраст файла не может быть отрицательным");

        if (MaxParallelThreads <= 0)
            return OperationResult.Failure("Максимальное количество потоков должно быть больше 0");

        if (CacheLifetimeMinutes <= 0)
            return OperationResult.Failure("Время жизни кэша должно быть больше 0");

        if (CreateBackups && string.IsNullOrWhiteSpace(BackupPath))
            return OperationResult.Failure("Путь для резервных копий должен быть указан");

        return OperationResult.Success();
    }
}