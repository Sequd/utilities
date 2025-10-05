using System.IO;

namespace CleanBin;

/// <summary>
/// Элемент предварительного просмотра файла
/// </summary>
public class FilePreviewItem
{
    /// <summary>
    /// Полный путь к файлу
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Имя файла
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Размер файла в байтах
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата последнего изменения
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Дата последнего доступа
    /// </summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Атрибуты файла
    /// </summary>
    public FileAttributes Attributes { get; set; }

    /// <summary>
    /// Расширение файла
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Директория файла
    /// </summary>
    public string Directory { get; set; } = string.Empty;

    /// <summary>
    /// Причина удаления
    /// </summary>
    public string RemovalReason { get; set; } = string.Empty;

    /// <summary>
    /// Приоритет удаления (1 = высокий, 5 = низкий)
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Безопасно ли удаление
    /// </summary>
    public bool IsSafeToDelete { get; set; } = true;

    /// <summary>
    /// Предупреждения
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Создает элемент предварительного просмотра из FileInfo
    /// </summary>
    public static FilePreviewItem FromFileInfo(FileInfo fileInfo, string removalReason = "Очистка")
    {
        return new FilePreviewItem
        {
            FullPath = fileInfo.FullName,
            Name = fileInfo.Name,
            Size = fileInfo.Length,
            CreatedAt = fileInfo.CreationTime,
            LastModified = fileInfo.LastWriteTime,
            LastAccessed = fileInfo.LastAccessTime,
            Attributes = fileInfo.Attributes,
            Extension = fileInfo.Extension,
            Directory = fileInfo.DirectoryName ?? string.Empty,
            RemovalReason = removalReason,
            Priority = CalculatePriority(fileInfo),
            IsSafeToDelete = IsFileSafeToDelete(fileInfo),
            Warnings = GetWarnings(fileInfo)
        };
    }

    /// <summary>
    /// Вычисляет приоритет удаления файла
    /// </summary>
    private static int CalculatePriority(FileInfo fileInfo)
    {
        var extension = fileInfo.Extension.ToLowerInvariant();
        var name = fileInfo.Name.ToLowerInvariant();

        // Высокий приоритет для временных файлов
        if (extension == ".tmp" || extension == ".temp" || name.StartsWith("~"))
            return 1;

        // Высокий приоритет для логов
        if (extension == ".log" || name.Contains("log"))
            return 1;

        // Средний приоритет для кэш файлов
        if (extension == ".cache" || name.Contains("cache"))
            return 2;

        // Средний приоритет для старых файлов
        if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-30))
            return 2;

        // Низкий приоритет для остальных
        return 3;
    }

    /// <summary>
    /// Проверяет безопасность удаления файла
    /// </summary>
    private static bool IsFileSafeToDelete(FileInfo fileInfo)
    {
        // Не удаляем системные файлы
        if (fileInfo.Attributes.HasFlag(FileAttributes.System))
            return false;

        // Не удаляем скрытые файлы (кроме временных)
        if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) && 
            !fileInfo.Name.StartsWith("~") && 
            !fileInfo.Extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase))
            return false;

        // Не удаляем файлы только для чтения
        if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
            return false;

        return true;
    }

    /// <summary>
    /// Получает предупреждения для файла
    /// </summary>
    private static List<string> GetWarnings(FileInfo fileInfo)
    {
        var warnings = new List<string>();

        if (fileInfo.Length > 100 * 1024 * 1024) // 100 MB
            warnings.Add("Большой файл");

        if (fileInfo.LastWriteTime > DateTime.Now.AddDays(-1))
            warnings.Add("Недавно изменен");

        if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
            warnings.Add("Скрытый файл");

        if (fileInfo.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
            fileInfo.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
            warnings.Add("Исполняемый файл");

        return warnings;
    }

    /// <summary>
    /// Форматирует размер файла
    /// </summary>
    public string GetFormattedSize()
    {
        return FormatFileSize(Size);
    }

    /// <summary>
    /// Форматирует размер файла в читаемом виде
    /// </summary>
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

    /// <summary>
    /// Получает иконку для типа файла
    /// </summary>
    public string GetFileIcon()
    {
        return Extension.ToLowerInvariant() switch
        {
            ".tmp" or ".temp" => "🗑️",
            ".log" => "📝",
            ".cache" => "💾",
            ".exe" => "⚙️",
            ".dll" => "📦",
            ".txt" => "📄",
            ".pdf" => "📕",
            ".doc" or ".docx" => "📘",
            ".xls" or ".xlsx" => "📗",
            ".jpg" or ".jpeg" or ".png" or ".gif" => "🖼️",
            ".mp3" or ".wav" or ".flac" => "🎵",
            ".mp4" or ".avi" or ".mkv" => "🎬",
            ".zip" or ".rar" or ".7z" => "📦",
            _ => "📄"
        };
    }

    /// <summary>
    /// Получает цветовую индикацию приоритета
    /// </summary>
    public string GetPriorityColor()
    {
        return Priority switch
        {
            1 => "🔴", // Красный - высокий приоритет
            2 => "🟡", // Желтый - средний приоритет
            3 => "🟢", // Зеленый - низкий приоритет
            _ => "⚪"  // Белый - неизвестно
        };
    }
}