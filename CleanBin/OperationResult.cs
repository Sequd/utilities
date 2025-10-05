namespace CleanBin
{
    /// <summary>
    /// Результат операции с информацией об успехе или ошибке
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    public class OperationResult<T>
    {
        /// <summary>
        /// Успешность операции
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Значение результата (если операция успешна)
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// Сообщение об ошибке (если операция неуспешна)
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Исключение (если произошло)
        /// </summary>
        public Exception? Exception { get; }

        internal OperationResult(bool isSuccess, T? value, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        /// <summary>
        /// Создает успешный результат
        /// </summary>
        /// <param name="value">Значение результата</param>
        /// <returns>Успешный результат</returns>
        public static OperationResult<T> Success(T value)
        {
            return new OperationResult<T>(true, value, null, null);
        }

        /// <summary>
        /// Создает неуспешный результат с сообщением об ошибке
        /// </summary>
        /// <param name="errorMessage">Сообщение об ошибке</param>
        /// <returns>Неуспешный результат</returns>
        public static OperationResult<T> Failure(string errorMessage)
        {
            return new OperationResult<T>(false, default, errorMessage, null);
        }

        /// <summary>
        /// Создает неуспешный результат с исключением
        /// </summary>
        /// <param name="exception">Исключение</param>
        /// <returns>Неуспешный результат</returns>
        public static OperationResult<T> Failure(Exception exception)
        {
            return new OperationResult<T>(false, default, exception.Message, exception);
        }

        /// <summary>
        /// Создает неуспешный результат с сообщением и исключением
        /// </summary>
        /// <param name="errorMessage">Сообщение об ошибке</param>
        /// <param name="exception">Исключение</param>
        /// <returns>Неуспешный результат</returns>
        public static OperationResult<T> Failure(string errorMessage, Exception exception)
        {
            return new OperationResult<T>(false, default, errorMessage, exception);
        }
    }

    /// <summary>
    /// Результат операции без возвращаемого значения
    /// </summary>
    public class OperationResult : OperationResult<object>
    {
        internal OperationResult(bool isSuccess, string? errorMessage, Exception? exception)
            : base(isSuccess, null, errorMessage, exception)
        {
        }

        /// <summary>
        /// Создает успешный результат
        /// </summary>
        /// <returns>Успешный результат</returns>
        public static OperationResult Success()
        {
            return new OperationResult(true, null, null);
        }

        /// <summary>
        /// Создает неуспешный результат с сообщением об ошибке
        /// </summary>
        /// <param name="errorMessage">Сообщение об ошибке</param>
        /// <returns>Неуспешный результат</returns>
        public static new OperationResult Failure(string errorMessage)
        {
            return new OperationResult(false, errorMessage, null);
        }

        /// <summary>
        /// Создает неуспешный результат с исключением
        /// </summary>
        /// <param name="exception">Исключение</param>
        /// <returns>Неуспешный результат</returns>
        public static new OperationResult Failure(Exception exception)
        {
            return new OperationResult(false, exception.Message, exception);
        }

        /// <summary>
        /// Создает неуспешный результат с сообщением и исключением
        /// </summary>
        /// <param name="errorMessage">Сообщение об ошибке</param>
        /// <param name="exception">Исключение</param>
        /// <returns>Неуспешный результат</returns>
        public static new OperationResult Failure(string errorMessage, Exception exception)
        {
            return new OperationResult(false, errorMessage, exception);
        }
    }
}