using System;

namespace CleanBin
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            CleanerService service = new CleanerService();
            
            // Получаем путь из аргументов командной строки или используем текущую директорию
            string path = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            
            Console.WriteLine($"Очистка папки: {path}");
            Console.WriteLine($"Найденные директории:");
            
            try
            {
                var dirs = service.Dir(path);
                foreach (var dir in dirs)
                {
                    Console.WriteLine($"  - {dir}");
                }

                Console.WriteLine($"\nНачинаем рекурсивную очистку:");
                var directories = service.CleanFolder(path);
                foreach (var directory in directories)
                {
                    Console.WriteLine($"Обработано: {directory}");
                }
                
                Console.WriteLine("\nОчистка завершена успешно!");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка при очистке: {e.Message}");
            }
            
            Console.WriteLine("\nНажмите Enter для выхода...");
            Console.ReadLine();
        }
    }
}
