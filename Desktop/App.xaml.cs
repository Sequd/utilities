using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CleanBin;

namespace Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Создаем хост с DI
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Регистрируем сервисы
                    services.AddSingleton<ICleanerService, CleanerService>();
                    services.AddTransient<MainWindow>();
                })
                .Build();

            // Получаем MainWindow из DI контейнера
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}