# CleanBin - Утилита для очистки папок

CleanBin - это современная утилита для очистки папок от временных файлов и директорий (bin, obj, packages и т.д.), написанная на .NET 8.

## 🚀 Возможности

- **Автоматическая очистка** папок bin, obj, packages и других временных директорий
- **Рекурсивная обработка** - очистка всех подпапок
- **Безопасность** - защита от удаления критически важных системных папок
- **Асинхронная обработка** с поддержкой отмены операций
- **Прогресс-репорты** для отслеживания выполнения
- **Статистика операций** с детальными метриками
- **Гибкая конфигурация** через JSON файлы
- **Логирование** с настраиваемыми уровнями
- **Dependency Injection** для легкого тестирования и расширения

## 📦 Проекты в решении

- **CleanBin** - основная библиотека с логикой очистки
- **Desktop** - WPF приложение с графическим интерфейсом
- **CleanBin.Tests** - модульные и интеграционные тесты

## 🛠 Установка и сборка

### Требования
- .NET 8 SDK или выше
- Visual Studio 2022 или VS Code

### Сборка проекта
```bash
# Клонирование репозитория
git clone <repository-url>
cd CleanBin

# Восстановление зависимостей
dotnet restore

# Сборка всех проектов
dotnet build

# Запуск тестов
dotnet test
```

## 🎯 Использование

### Консольное приложение

```bash
# Запуск с настройками по умолчанию
dotnet run --project CleanBin

# Очистка конкретной папки
dotnet run --project CleanBin -- "C:\MyProject"
```

### WPF приложение

```bash
# Запуск графического интерфейса
dotnet run --project Desktop
```

### Использование как библиотека

```csharp
using CleanBin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// Настройка DI контейнера
var services = new ServiceCollection();
services.Configure<CleanBinOptions>(options =>
{
    options.DefaultCleanDirectories = new[] { "bin", "obj", "packages" };
    options.DefaultIgnoreDirectories = new[] { "node_modules", ".git" };
    options.EnableSystemClean = false;
    options.LogLevel = "Information";
});
services.AddSingleton<ICleanerService, CleanerService>();

var serviceProvider = services.BuildServiceProvider();
var cleanerService = serviceProvider.GetRequiredService<ICleanerService>();

// Синхронная очистка
var result = cleanerService.CleanFolder("C:\\MyProject");
if (result.IsSuccess)
{
    Console.WriteLine($"Очистка завершена. Обработано папок: {result.Value.Count()}");
}

// Асинхронная очистка с прогрессом
var progress = new Progress<string>(message => Console.WriteLine(message));
var asyncResult = await cleanerService.CleanFolderAsync(
    "C:\\MyProject", 
    progress: progress);

// Получение статистики
var statistics = cleanerService.GetStatistics();
Console.WriteLine(statistics.ToString());
```

## ⚙️ Конфигурация

### appsettings.json

```json
{
  "DefaultCleanDirectories": ["bin", "obj", "packages", "Debug", "Release"],
  "DefaultIgnoreDirectories": ["node_modules", ".git", ".vs", ".vscode"],
  "EnableSystemClean": false,
  "LogLevel": "Information"
}
```

### Параметры конфигурации

- **DefaultCleanDirectories** - папки для удаления по умолчанию
- **DefaultIgnoreDirectories** - папки для игнорирования
- **EnableSystemClean** - включить очистку системных папок (начинающихся с точки)
- **LogLevel** - уровень логирования (Debug, Information, Warning, Error, Critical)

## 🧪 Тестирование

Проект включает полный набор модульных и интеграционных тестов:

```bash
# Запуск всех тестов
dotnet test

# Запуск тестов с покрытием кода
dotnet test --collect:"XPlat Code Coverage"

# Запуск конкретного тестового класса
dotnet test --filter "ClassName=PathValidatorTests"
```

### Покрытие тестами

- ✅ **PathValidator** - валидация путей и проверка безопасности
- ✅ **OperationResult** - результат операций с обработкой ошибок
- ✅ **CleanupStatistics** - статистика операций
- ✅ **CleanerService** - основная логика очистки
- ✅ **Интеграционные тесты** - полные сценарии использования

## 📊 API Документация

### ICleanerService

```csharp
public interface ICleanerService
{
    // Синхронные методы
    OperationResult<IEnumerable<string>> GetDirectories(string path);
    OperationResult<IEnumerable<string>> CleanFolder(string path, bool needSysClean = false, 
        string[]? ignoreDirectories = null, string[]? cleanDirectories = null);
    
    // Асинхронные методы
    Task<OperationResult<IEnumerable<string>>> GetDirectoriesAsync(string path, 
        CancellationToken cancellationToken = default);
    Task<OperationResult<IEnumerable<string>>> CleanFolderAsync(string path, 
        bool needSysClean = false, string[]? ignoreDirectories = null, 
        string[]? cleanDirectories = null, CancellationToken cancellationToken = default, 
        IProgress<string>? progress = null);
    
    // Статистика
    CleanupStatistics GetStatistics();
}
```

### OperationResult<T>

```csharp
public class OperationResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }
    
    // Статические методы для создания результатов
    public static OperationResult<T> Success(T value);
    public static OperationResult<T> Failure(string errorMessage);
    public static OperationResult<T> Failure(Exception exception);
    public static OperationResult<T> Failure(string errorMessage, Exception exception);
}
```

## 🔒 Безопасность

- **Валидация путей** - проверка существования и доступности директорий
- **Защита системных папок** - блокировка очистки критически важных папок
- **Обработка ошибок** - graceful handling всех исключений
- **Проверка прав доступа** - валидация прав на чтение/запись

## 🏗 Архитектура

Проект построен с использованием современных принципов:

- **Clean Architecture** - разделение слоев и зависимостей
- **Dependency Injection** - внедрение зависимостей через DI контейнер
- **Result Pattern** - явная обработка успешных и неуспешных операций
- **Async/Await** - асинхронное программирование
- **Configuration Pattern** - конфигурация через JSON файлы
- **Logging** - структурированное логирование

## 📈 Производительность

- **Асинхронная обработка** - не блокирует UI поток
- **Отмена операций** - поддержка CancellationToken
- **Прогресс-репорты** - отображение хода выполнения
- **Статистика** - детальные метрики производительности

## 🤝 Вклад в проект

1. Форкните репозиторий
2. Создайте ветку для новой функции (`git checkout -b feature/amazing-feature`)
3. Зафиксируйте изменения (`git commit -m 'Add amazing feature'`)
4. Отправьте в ветку (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

## 📄 Лицензия

Этот проект распространяется под лицензией MIT. См. файл `LICENSE` для получения дополнительной информации.

## 🆘 Поддержка

Если у вас возникли вопросы или проблемы:

1. Проверьте [Issues](../../issues) на наличие похожих проблем
2. Создайте новый Issue с подробным описанием
3. Приложите логи и конфигурацию для диагностики

## 📝 История изменений

### v2.0.0 (Текущая версия)
- ✅ Обновление до .NET 8
- ✅ Переход на SDK-style проекты
- ✅ Внедрение Dependency Injection
- ✅ Добавление асинхронных методов
- ✅ Улучшение обработки ошибок
- ✅ Добавление логирования и статистики
- ✅ Создание современного UI
- ✅ Полное покрытие тестами

### v1.0.0 (Исходная версия)
- Базовая функциональность очистки папок
- Простой консольный интерфейс