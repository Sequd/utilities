using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CleanBin
{
    public class CleanerService
    {
        private static readonly string[] DEFAULT_IGNORE_DIR = new[] {""};
        private static readonly string[] DEFAULT_CLEAN_DIR = new[] {"bin", "obj", "packages"};

        public IEnumerable<string> Dir(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            string[] directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                var directoryInfo = new DirectoryInfo(directory);
                yield return directoryInfo.Name;
            }
        }

        public IEnumerable<string> CleanFolder(string path, bool needSysClean = false, string[] ingnoreDir = null,
            string[] cleanDir = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            if (ingnoreDir == null)
                ingnoreDir = DEFAULT_IGNORE_DIR;
            if (cleanDir == null)
                cleanDir = DEFAULT_CLEAN_DIR;

            string[] directories = Directory.GetDirectories(path);
            Queue<string> queue = new Queue<string>(directories);
            while (queue.Count > 0)
            {
                var dir = queue.Dequeue();
                var directoryInfo = new DirectoryInfo(dir);
                yield return dir;

                // пропусткаем если совпало с игнором
                if (ingnoreDir.Contains(directoryInfo.Name)) continue;

                bool isSystem = false;
                if (needSysClean)
                    isSystem = directoryInfo.Name.ToLower().StartsWith(".");

                // проверяем и удаляем
                // игнорировать скрытые directoryInfo.Attributes & FileAttributes.Hidden) != 0 &&
                if (cleanDir.Contains(directoryInfo.Name) || isSystem)
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
