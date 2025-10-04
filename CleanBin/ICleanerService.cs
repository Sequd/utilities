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
        /// <returns>Перечисление имен директорий</returns>
        IEnumerable<string> GetDirectories(string path);

        /// <summary>
        /// Выполняет рекурсивную очистку папки
        /// </summary>
        /// <param name="path">Путь к папке для очистки</param>
        /// <param name="needSysClean">Нужно ли удалять системные папки (начинающиеся с точки)</param>
        /// <param name="ignoreDirectories">Список папок для игнорирования</param>
        /// <param name="cleanDirectories">Список папок для удаления</param>
        /// <returns>Перечисление обработанных путей</returns>
        IEnumerable<string> CleanFolder(string path, bool needSysClean = false, string[]? ignoreDirectories = null, string[]? cleanDirectories = null);
    }
}