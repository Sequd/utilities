namespace CleanBin
{
    /// <summary>
    /// Простой логгер для вывода в консоль
    /// </summary>
    public class ConsoleLogger
    {
        private readonly string _categoryName;
        private readonly LogLevel _minLevel;

        /// <summary>
        /// Конструктор логгера
        /// </summary>
        /// <param name="categoryName">Имя категории лога</param>
        /// <param name="minLevel">Минимальный уровень логирования</param>
        public ConsoleLogger(string categoryName, LogLevel minLevel = LogLevel.Information)
        {
            _categoryName = categoryName;
            _minLevel = minLevel;
        }

        /// <summary>
        /// Логирует сообщение
        /// </summary>
        /// <param name="level">Уровень логирования</param>
        /// <param name="message">Сообщение</param>
        /// <param name="exception">Исключение (опционально)</param>
        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            if (level < _minLevel)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var levelName = GetLevelName(level);
            var category = _categoryName;

            Console.WriteLine($"[{timestamp}] [{levelName}] [{category}] {message}");

            if (exception != null)
            {
                Console.WriteLine($"  Исключение: {exception.Message}");
                if (exception.StackTrace != null)
                {
                    Console.WriteLine($"  StackTrace: {exception.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Логирует отладочное сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        public void LogDebug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// Логирует информационное сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        public void LogInformation(string message)
        {
            Log(LogLevel.Information, message);
        }

        /// <summary>
        /// Логирует предупреждение
        /// </summary>
        /// <param name="message">Сообщение</param>
        public void LogWarning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// Логирует ошибку
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="exception">Исключение (опционально)</param>
        public void LogError(string message, Exception? exception = null)
        {
            Log(LogLevel.Error, message, exception);
        }

        /// <summary>
        /// Логирует критическую ошибку
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="exception">Исключение (опционально)</param>
        public void LogCritical(string message, Exception? exception = null)
        {
            Log(LogLevel.Critical, message, exception);
        }

        /// <summary>
        /// Получает строковое представление уровня логирования
        /// </summary>
        /// <param name="level">Уровень логирования</param>
        /// <returns>Строковое представление</returns>
        private static string GetLevelName(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO ",
                LogLevel.Warning => "WARN ",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "CRIT ",
                _ => "UNKNW"
            };
        }
    }

    /// <summary>
    /// Уровни логирования
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Отладочная информация
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Информационные сообщения
        /// </summary>
        Information = 1,

        /// <summary>
        /// Предупреждения
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Ошибки
        /// </summary>
        Error = 3,

        /// <summary>
        /// Критические ошибки
        /// </summary>
        Critical = 4
    }
}