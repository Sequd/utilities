namespace CleanBin
{
    /// <summary>
    /// Статистика операций очистки
    /// </summary>
    public class CleanupStatistics
    {
        /// <summary>
        /// Общее количество обработанных папок
        /// </summary>
        public int TotalProcessedFolders { get; set; }

        /// <summary>
        /// Количество удаленных папок
        /// </summary>
        public int DeletedFolders { get; set; }

        /// <summary>
        /// Количество пропущенных папок (из-за игнорирования)
        /// </summary>
        public int SkippedFolders { get; set; }

        /// <summary>
        /// Количество ошибок при обработке
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// Общее время выполнения операции (в миллисекундах)
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }

        /// <summary>
        /// Дата и время последней операции очистки
        /// </summary>
        public DateTime? LastCleanupTime { get; set; }

        /// <summary>
        /// Сбрасывает статистику
        /// </summary>
        public void Reset()
        {
            TotalProcessedFolders = 0;
            DeletedFolders = 0;
            SkippedFolders = 0;
            Errors = 0;
            TotalExecutionTimeMs = 0;
            LastCleanupTime = null;
        }

        /// <summary>
        /// Возвращает строковое представление статистики
        /// </summary>
        /// <returns>Строка со статистикой</returns>
        public override string ToString()
        {
            return $"Статистика очистки:\n" +
                   $"- Обработано папок: {TotalProcessedFolders}\n" +
                   $"- Удалено папок: {DeletedFolders}\n" +
                   $"- Пропущено папок: {SkippedFolders}\n" +
                   $"- Ошибок: {Errors}\n" +
                   $"- Время выполнения: {TotalExecutionTimeMs} мс\n" +
                   $"- Последняя очистка: {LastCleanupTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Не выполнялась"}";
        }
    }
}