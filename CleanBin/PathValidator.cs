using System.IO;

namespace CleanBin
{
    /// <summary>
    /// Валидатор путей для операций с файловой системой
    /// </summary>
    public static class PathValidator
    {
        /// <summary>
        /// Максимальная длина пути в Windows
        /// </summary>
        private const int MaxPathLength = 260;

        /// <summary>
        /// Валидирует путь к директории
        /// </summary>
        /// <param name="path">Путь для валидации</param>
        /// <returns>Результат валидации</returns>
        public static OperationResult ValidateDirectoryPath(string? path)
        {
            // Проверяем на null или пустую строку
            if (string.IsNullOrWhiteSpace(path))
            {
                return OperationResult.Failure("Путь не может быть пустым или содержать только пробелы");
            }

            // Проверяем длину пути
            if (path.Length > MaxPathLength)
            {
                return OperationResult.Failure($"Путь слишком длинный. Максимальная длина: {MaxPathLength} символов");
            }

            // Проверяем на недопустимые символы
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return OperationResult.Failure("Путь содержит недопустимые символы");
            }

            try
            {
                // Проверяем, что путь существует
                if (!Directory.Exists(path))
                {
                    return OperationResult.Failure($"Директория не существует: {path}");
                }

                // Проверяем права доступа
                var directoryInfo = new DirectoryInfo(path);
                if (!directoryInfo.Exists)
                {
                    return OperationResult.Failure($"Нет доступа к директории: {path}");
                }

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Ошибка при проверке пути: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Валидирует массив путей
        /// </summary>
        /// <param name="paths">Массив путей для валидации</param>
        /// <param name="allowEmpty">Разрешить пустой массив</param>
        /// <returns>Результат валидации</returns>
        public static OperationResult ValidatePathsArray(string[]? paths, bool allowEmpty = true)
        {
            if (paths == null)
            {
                return allowEmpty ? OperationResult.Success() : OperationResult.Failure("Массив путей не может быть null");
            }

            if (!allowEmpty && paths.Length == 0)
            {
                return OperationResult.Failure("Массив путей не может быть пустым");
            }

            // Проверяем каждый путь в массиве
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return OperationResult.Failure("Массив содержит пустые или null пути");
                }
            }

            return OperationResult.Success();
        }

        /// <summary>
        /// Проверяет, является ли путь системной папкой
        /// </summary>
        /// <param name="path">Путь для проверки</param>
        /// <returns>True, если это системная папка</returns>
        public static bool IsSystemPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var directoryName = Path.GetFileName(path);
            return directoryName.StartsWith(".", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Проверяет, является ли путь критически важной системной папкой
        /// </summary>
        /// <param name="path">Путь для проверки</param>
        /// <returns>True, если это критически важная папка</returns>
        public static bool IsCriticalSystemPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var criticalPaths = new[]
            {
                "windows", "system32", "program files", "program files (x86)",
                "users", "documents and settings", "boot", "recovery"
            };

            var pathLower = path.ToLowerInvariant();
            return criticalPaths.Any(criticalPath => pathLower.Contains(criticalPath));
        }
    }
}