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
        public static async Task Main(string[] args)
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
                // Получаем список директорий асинхронно
                var dirsResult = await cleanerService.GetDirectoriesAsync(path);
                if (!dirsResult.IsSuccess)
                {
                    Console.WriteLine($"Ошибка при получении списка папок: {dirsResult.ErrorMessage}");
                    return;
                }

                Console.WriteLine("Найденные директории:");
                foreach (var dir in dirsResult.Value!)
                {
                    Console.WriteLine($"  - {dir}");
                }

                Console.WriteLine($"\nНачинаем рекурсивную очистку:");
                
                // Создаем прогресс-репорт для отображения прогресса
                var progress = new Progress<string>(message => Console.WriteLine($"  {message}"));
                
                var cleanResult = await cleanerService.CleanFolderAsync(path, progress: progress);
                
                if (cleanResult.IsSuccess)
                {
                    Console.WriteLine("\nОчистка завершена успешно!");
                    
                    // Показываем статистику
                    var statistics = cleanerService.GetStatistics();
                    Console.WriteLine($"\n{statistics}");
                }
                else
                {
                    Console.WriteLine($"Ошибка при очистке: {cleanResult.ErrorMessage}");
                    if (cleanResult.Exception != null)
                    {
                        Console.WriteLine($"Детали ошибки: {cleanResult.Exception}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Неожиданная ошибка: {e.Message}");
                Console.WriteLine($"Детали: {e}");
            }
            
            Console.WriteLine("\nНажмите Enter для выхода...");
            Console.ReadLine();
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
                    services.AddSingleton<IConfigurationProfileManager, ConfigurationProfileManager>();
                    services.AddSingleton<IFilePreviewService, FilePreviewService>();
                    services.AddSingleton<IBackupService, BackupService>();
                    services.AddSingleton<ICacheService, MemoryCacheService>();
                    services.AddSingleton<ICleanerService, EnhancedCleanerService>();
                });
    }
}
