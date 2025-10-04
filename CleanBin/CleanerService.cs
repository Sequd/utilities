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

        /// <summary>
        /// Конструктор сервиса очистки
        /// </summary>
        /// <param name="options">Опции конфигурации</param>
        public CleanerService(IOptions<CleanBinOptions> options)
        {
            _options = options.Value;
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                var directoryInfo = new DirectoryInfo(directory);
                yield return directoryInfo.Name;
            }
        }

        public IEnumerable<string> CleanFolder(string path, bool needSysClean = false, string[]? ignoreDirectories = null, string[]? cleanDirectories = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            // Используем конфигурацию по умолчанию, если не переданы параметры
            ignoreDirectories ??= _options.DefaultIgnoreDirectories;
            cleanDirectories ??= _options.DefaultCleanDirectories;
            
            // Используем настройку из конфигурации для системной очистки, если не указано явно
            if (!needSysClean)
                needSysClean = _options.EnableSystemClean;

            var directories = Directory.GetDirectories(path);
            var queue = new Queue<string>(directories);
            while (queue.Count > 0)
            {
                var dir = queue.Dequeue();
                var directoryInfo = new DirectoryInfo(dir);
                yield return dir;

                // Пропускаем если папка в списке игнорирования
                if (ignoreDirectories.Contains(directoryInfo.Name)) continue;

                var isSystem = false;
                if (needSysClean)
                    isSystem = directoryInfo.Name.ToLower().StartsWith(".");

                // Проверяем и удаляем папки из списка очистки или системные папки
                if (cleanDirectories.Contains(directoryInfo.Name) || isSystem)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    var subDirectories = Directory.GetDirectories(dir);
                    foreach (var subDirectory in subDirectories)
                    {
                        queue.Enqueue(subDirectory);
                    }
                }
            }
        }
    }
}