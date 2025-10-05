using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;

namespace CleanBin
{
    /// <summary>
    /// Сервис для очистки папок от временных файлов и директорий
    /// </summary>
    public class CleanerService : ICleanerService
    {
        private readonly CleanBinOptions _options;
        private readonly CleanupStatistics _statistics;
        private readonly ConsoleLogger _logger;

        /// <summary>
        /// Конструктор сервиса очистки
        /// </summary>
        /// <param name="options">Опции конфигурации</param>
        public CleanerService(IOptions<CleanBinOptions> options)
        {
            _options = options.Value;
            _statistics = new CleanupStatistics();
            
            // Создаем логгер с уровнем из конфигурации
            var logLevel = Enum.TryParse<LogLevel>(_options.LogLevel, out var level) ? level : LogLevel.Information;
            _logger = new ConsoleLogger("CleanerService", logLevel);
        }

        public OperationResult<IEnumerable<string>> GetDirectories(string path)
        {
            _logger.LogDebug($"Начинаем получение списка директорий: {path}");
            
            // Валидируем входной путь
            var validationResult = PathValidator.ValidateDirectoryPath(path);
            if (!validationResult.IsSuccess)
            {
                _logger.LogError($"Ошибка валидации пути: {validationResult.ErrorMessage}");
                return OperationResult<IEnumerable<string>>.Failure(validationResult.ErrorMessage!);
            }

            try
            {
                var directories = Directory.GetDirectories(path);
                var directoryNames = directories.Select(dir => new DirectoryInfo(dir).Name).ToList();
                
                _statistics.TotalProcessedFolders += directoryNames.Count;
                _logger.LogInformation($"Получено {directoryNames.Count} директорий из {path}");
                
                return OperationResult<IEnumerable<string>>.Success(directoryNames);
            }
            catch (UnauthorizedAccessException ex)
            {
                _statistics.Errors++;
                _logger.LogError($"Нет доступа к папке: {path}", ex);
                return OperationResult<IEnumerable<string>>.Failure($"Нет доступа к папке: {ex.Message}", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                _statistics.Errors++;
                _logger.LogError($"Папка не найдена: {path}", ex);
                return OperationResult<IEnumerable<string>>.Failure($"Папка не найдена: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _statistics.Errors++;
                _logger.LogError($"Ошибка при получении списка папок: {path}", ex);
                return OperationResult<IEnumerable<string>>.Failure($"Ошибка при получении списка папок: {ex.Message}", ex);
            }
        }

        public OperationResult<IEnumerable<string>> CleanFolder(string path, bool needSysClean = false, string[]? ignoreDirectories = null, string[]? cleanDirectories = null)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation($"Начинаем очистку папки: {path}");
            
            // Валидируем входной путь
            var validationResult = PathValidator.ValidateDirectoryPath(path);
            if (!validationResult.IsSuccess)
            {
                _logger.LogError($"Ошибка валидации пути: {validationResult.ErrorMessage}");
                return OperationResult<IEnumerable<string>>.Failure(validationResult.ErrorMessage!);
            }

            // Проверяем, что это не критически важная системная папка
            if (PathValidator.IsCriticalSystemPath(path))
            {
                _logger.LogCritical($"Попытка очистки критически важной системной папки: {path}");
                return OperationResult<IEnumerable<string>>.Failure($"Попытка очистки критически важной системной папки: {path}");
            }

            // Используем конфигурацию по умолчанию, если не переданы параметры
            ignoreDirectories ??= _options.DefaultIgnoreDirectories;
            cleanDirectories ??= _options.DefaultCleanDirectories;
            
            // Используем настройку из конфигурации для системной очистки, если не указано явно
            if (!needSysClean)
                needSysClean = _options.EnableSystemClean;

            try
            {
                var directories = Directory.GetDirectories(path);
                var queue = new Queue<string>(directories);
                var processedPaths = new List<string>();

                while (queue.Count > 0)
                {
                    var dir = queue.Dequeue();
                    var directoryInfo = new DirectoryInfo(dir);
                    processedPaths.Add(dir);
                    _statistics.TotalProcessedFolders++;

                    // Пропускаем если папка в списке игнорирования
                    if (ignoreDirectories.Contains(directoryInfo.Name))
                    {
                        _statistics.SkippedFolders++;
                        continue;
                    }

                    var isSystem = false;
                    if (needSysClean)
                        isSystem = PathValidator.IsSystemPath(dir);

                    // Проверяем и удаляем папки из списка очистки или системные папки
                    if (cleanDirectories.Contains(directoryInfo.Name) || isSystem)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            _statistics.DeletedFolders++;
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            _statistics.Errors++;
                            Console.WriteLine($"Нет доступа для удаления папки {dir}: {ex.Message}");
                        }
                        catch (DirectoryNotFoundException ex)
                        {
                            _statistics.Errors++;
                            Console.WriteLine($"Папка уже удалена {dir}: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _statistics.Errors++;
                            Console.WriteLine($"Ошибка при удалении папки {dir}: {ex.Message}");
                        }
                    }
                    else
                    {
                        try
                        {
                            var subDirectories = Directory.GetDirectories(dir);
                            foreach (var subDirectory in subDirectories)
                            {
                                queue.Enqueue(subDirectory);
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            _statistics.Errors++;
                            Console.WriteLine($"Нет доступа к папке {dir}: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _statistics.Errors++;
                            Console.WriteLine($"Ошибка при доступе к папке {dir}: {ex.Message}");
                        }
                    }
                }

                _statistics.LastCleanupTime = DateTime.UtcNow;
                _statistics.TotalExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogInformation($"Очистка завершена успешно. Обработано: {processedPaths.Count}, удалено: {_statistics.DeletedFolders}, ошибок: {_statistics.Errors}");
                return OperationResult<IEnumerable<string>>.Success(processedPaths);
            }
            catch (Exception ex)
            {
                _statistics.Errors++;
                _statistics.TotalExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogCritical($"Критическая ошибка при очистке папки: {path}", ex);
                return OperationResult<IEnumerable<string>>.Failure($"Критическая ошибка при очистке папки: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Получает список директорий в указанной папке (асинхронно)
        /// </summary>
        /// <param name="path">Путь к папке</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат операции со списком имен директорий</returns>
        public async Task<OperationResult<IEnumerable<string>>> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default)
        {
            // Валидируем входной путь
            var validationResult = PathValidator.ValidateDirectoryPath(path);
            if (!validationResult.IsSuccess)
            {
                return OperationResult<IEnumerable<string>>.Failure(validationResult.ErrorMessage!);
            }

            try
            {
                // Используем Task.Run для выполнения синхронной операции в фоновом потоке
                var directoryNames = await Task.Run(() =>
                {
                    var directories = Directory.GetDirectories(path);
                    return directories.Select(dir => new DirectoryInfo(dir).Name).ToList();
                }, cancellationToken);

                _statistics.TotalProcessedFolders += directoryNames.Count;

                return OperationResult<IEnumerable<string>>.Success(directoryNames);
            }
            catch (OperationCanceledException)
            {
                return OperationResult<IEnumerable<string>>.Failure("Операция была отменена");
            }
            catch (UnauthorizedAccessException ex)
            {
                _statistics.Errors++;
                return OperationResult<IEnumerable<string>>.Failure($"Нет доступа к папке: {ex.Message}", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                _statistics.Errors++;
                return OperationResult<IEnumerable<string>>.Failure($"Папка не найдена: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _statistics.Errors++;
                return OperationResult<IEnumerable<string>>.Failure($"Ошибка при получении списка папок: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Выполняет рекурсивную очистку папки (асинхронно)
        /// </summary>
        /// <param name="path">Путь к папке для очистки</param>
        /// <param name="needSysClean">Нужно ли удалять системные папки (начинающиеся с точки)</param>
        /// <param name="ignoreDirectories">Список папок для игнорирования</param>
        /// <param name="cleanDirectories">Список папок для удаления</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <param name="progress">Прогресс операции</param>
        /// <returns>Результат операции с перечислением обработанных путей</returns>
        public async Task<OperationResult<IEnumerable<string>>> CleanFolderAsync(string path, bool needSysClean = false, string[]? ignoreDirectories = null, string[]? cleanDirectories = null, CancellationToken cancellationToken = default, IProgress<string>? progress = null)
        {
            var startTime = DateTime.UtcNow;

            // Валидируем входной путь
            var validationResult = PathValidator.ValidateDirectoryPath(path);
            if (!validationResult.IsSuccess)
            {
                return OperationResult<IEnumerable<string>>.Failure(validationResult.ErrorMessage!);
            }

            // Проверяем, что это не критически важная системная папка
            if (PathValidator.IsCriticalSystemPath(path))
            {
                return OperationResult<IEnumerable<string>>.Failure($"Попытка очистки критически важной системной папки: {path}");
            }

            // Используем конфигурацию по умолчанию, если не переданы параметры
            ignoreDirectories ??= _options.DefaultIgnoreDirectories;
            cleanDirectories ??= _options.DefaultCleanDirectories;

            // Используем настройку из конфигурации для системной очистки, если не указано явно
            if (!needSysClean)
                needSysClean = _options.EnableSystemClean;

            try
            {
                var processedPaths = await Task.Run(() =>
                {
                    var directories = Directory.GetDirectories(path);
                    var queue = new Queue<string>(directories);
                    var result = new List<string>();

                    while (queue.Count > 0)
                    {
                        // Проверяем отмену операции
                        cancellationToken.ThrowIfCancellationRequested();

                        var dir = queue.Dequeue();
                        var directoryInfo = new DirectoryInfo(dir);
                        result.Add(dir);
                        _statistics.TotalProcessedFolders++;

                        // Уведомляем о прогрессе
                        progress?.Report($"Обрабатывается: {dir}");

                        // Пропускаем если папка в списке игнорирования
                        if (ignoreDirectories.Contains(directoryInfo.Name))
                        {
                            _statistics.SkippedFolders++;
                            continue;
                        }

                        var isSystem = false;
                        if (needSysClean)
                            isSystem = PathValidator.IsSystemPath(dir);

                        // Проверяем и удаляем папки из списка очистки или системные папки
                        if (cleanDirectories.Contains(directoryInfo.Name) || isSystem)
                        {
                            try
                            {
                                Directory.Delete(dir, true);
                                _statistics.DeletedFolders++;
                                progress?.Report($"Удалена: {dir}");
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                _statistics.Errors++;
                                progress?.Report($"Ошибка доступа: {dir} - {ex.Message}");
                            }
                            catch (DirectoryNotFoundException ex)
                            {
                                _statistics.Errors++;
                                progress?.Report($"Папка уже удалена: {dir} - {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                _statistics.Errors++;
                                progress?.Report($"Ошибка удаления: {dir} - {ex.Message}");
                            }
                        }
                        else
                        {
                            try
                            {
                                var subDirectories = Directory.GetDirectories(dir);
                                foreach (var subDirectory in subDirectories)
                                {
                                    queue.Enqueue(subDirectory);
                                }
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                _statistics.Errors++;
                                progress?.Report($"Нет доступа к папке: {dir} - {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                _statistics.Errors++;
                                progress?.Report($"Ошибка доступа к папке: {dir} - {ex.Message}");
                            }
                        }
                    }

                    return result;
                }, cancellationToken);

                _statistics.LastCleanupTime = DateTime.UtcNow;
                _statistics.TotalExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

                return OperationResult<IEnumerable<string>>.Success(processedPaths);
            }
            catch (OperationCanceledException)
            {
                _statistics.TotalExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return OperationResult<IEnumerable<string>>.Failure("Операция очистки была отменена");
            }
            catch (Exception ex)
            {
                _statistics.Errors++;
                _statistics.TotalExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                return OperationResult<IEnumerable<string>>.Failure($"Критическая ошибка при очистке папки: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Получает статистику очистки
        /// </summary>
        /// <returns>Статистика операций очистки</returns>
        public CleanupStatistics GetStatistics()
        {
            return _statistics;
        }
    }
}