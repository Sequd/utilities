using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanBin;

/// <summary>
/// Интерфейс для резервного копирования
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Создает резервную копию файлов
    /// </summary>
    Task<OperationResult<BackupInfo>> CreateBackupAsync(
        IEnumerable<FilePreviewItem> files,
        string backupPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Восстанавливает файлы из резервной копии
    /// </summary>
    Task<OperationResult> RestoreBackupAsync(
        BackupInfo backupInfo,
        string targetPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет резервную копию
    /// </summary>
    Task<OperationResult> DeleteBackupAsync(
        BackupInfo backupInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список резервных копий
    /// </summary>
    Task<OperationResult<IEnumerable<BackupInfo>>> GetBackupsAsync(
        string backupDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет целостность резервной копии
    /// </summary>
    Task<OperationResult<bool>> ValidateBackupAsync(
        BackupInfo backupInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Очищает старые резервные копии
    /// </summary>
    Task<OperationResult<int>> CleanupOldBackupsAsync(
        string backupDirectory,
        int maxAgeInDays,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Информация о резервной копии
/// </summary>
public class BackupInfo
{
    /// <summary>
    /// Уникальный идентификатор резервной копии
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Путь к резервной копии
    /// </summary>
    public string BackupPath { get; set; } = string.Empty;

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Размер резервной копии в байтах
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Количество файлов в резервной копии
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Описание резервной копии
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Исходный путь, для которого создана резервная копия
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Список файлов в резервной копии
    /// </summary>
    public List<BackupFileInfo> Files { get; set; } = new();

    /// <summary>
    /// Хэш для проверки целостности
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Форматированный размер
    /// </summary>
    public string FormattedSize => FormatFileSize(Size);

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

/// <summary>
/// Информация о файле в резервной копии
/// </summary>
public class BackupFileInfo
{
    /// <summary>
    /// Исходный путь файла
    /// </summary>
    public string OriginalPath { get; set; } = string.Empty;

    /// <summary>
    /// Путь в резервной копии
    /// </summary>
    public string BackupFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Размер файла
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Дата последнего изменения
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Хэш файла для проверки целостности
    /// </summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Атрибуты файла
    /// </summary>
    public System.IO.FileAttributes Attributes { get; set; }
}