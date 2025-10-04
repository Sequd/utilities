namespace CleanBin
{
    /// <summary>
    /// Опции конфигурации для CleanBin
    /// </summary>
    public class CleanBinOptions
    {
        /// <summary>
        /// Папки для игнорирования по умолчанию
        /// </summary>
        public string[] DefaultIgnoreDirectories { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Папки для очистки по умолчанию
        /// </summary>
        public string[] DefaultCleanDirectories { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Включить очистку системных папок (начинающихся с точки)
        /// </summary>
        public bool EnableSystemClean { get; set; } = false;

        /// <summary>
        /// Уровень логирования
        /// </summary>
        public string LogLevel { get; set; } = "Information";
    }
}