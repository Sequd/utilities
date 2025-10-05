using System.Collections.Generic;

namespace CleanBin
{
    /// <summary>
    /// Интерфейс для сервиса очистки папок
    /// </summary>
    public interface ICleanerService
    {
        /// <summary>
        /// Получает список директорий в указанной папке
        /// </summary>
        /// <param name="path">Путь к папке</param>
        /// <returns>Результат операции со списком имен директорий</returns>
        OperationResult<IEnumerable<string>> GetDirectories(string path);

        /// <summary>
        /// Получает список директорий в указанной папке (асинхронно)
        /// </summary>
        /// <param name="path">Путь к папке</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат операции со списком имен директорий</returns>
        Task<OperationResult<IEnumerable<string>>> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Выполняет рекурсивную очистку папки
        /// </summary>
        /// <param name="path">Путь к папке для очистки</param>
        /// <param name="needSysClean">Нужно ли удалять системные папки (начинающиеся с точки)</param>
        /// <param name="ignoreDirectories">Список папок для игнорирования</param>
        /// <param name="cleanDirectories">Список папок для удаления</param>
        /// <returns>Результат операции с перечислением обработанных путей</returns>
        OperationResult<IEnumerable<string>> CleanFolder(string path, bool needSysClean = false, string[]? ignoreDirectories = null, string[]? cleanDirectories = null);

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
        Task<OperationResult<IEnumerable<string>>> CleanFolderAsync(string path, bool needSysClean = false, string[]? ignoreDirectories = null, string[]? cleanDirectories = null, CancellationToken cancellationToken = default, IProgress<string>? progress = null);

        /// <summary>
        /// Получает статистику очистки
        /// </summary>
        /// <returns>Статистика операций очистки</returns>
        CleanupStatistics GetStatistics();
    }
}