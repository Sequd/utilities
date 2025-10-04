using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace CleanBin
{
    /// <summary>
    /// Главный класс приложения
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Точка входа в приложение
        /// </summary>
        /// <param name="args">Аргументы командной строки</param>
        public static Task Main(string[] args)
        {
            // Создаем хост с конфигурацией
            var host = CreateHostBuilder(args).Build();
            
            // Получаем сервис очистки из DI контейнера
            var cleanerService = host.Services.GetRequiredService<ICleanerService>();
            
            // Получаем путь из аргументов командной строки или используем текущую директорию
            string path = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            
            Console.WriteLine($"Очистка папки: {path}");
            Console.WriteLine($"Найденные директории:");
            
            try
            {
                var dirs = cleanerService.GetDirectories(path);
                foreach (var dir in dirs)
                {
                    Console.WriteLine($"  - {dir}");
                }

                Console.WriteLine($"\nНачинаем рекурсивную очистку:");
                var directories = cleanerService.CleanFolder(path);
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
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Создает конфигурацию хоста с DI
        /// </summary>
        /// <param name="args">Аргументы командной строки</param>
        /// <returns>Построитель хоста</returns>
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Конфигурируем опции CleanBin
                    services.Configure<CleanBinOptions>(
                        context.Configuration.GetSection("CleanBin"));
                    
                    // Регистрируем сервисы
                    services.AddSingleton<ICleanerService, CleanerService>();
                });
    }
}
