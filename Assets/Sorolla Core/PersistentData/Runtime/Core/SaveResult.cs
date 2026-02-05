using System;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Result of a save/load operation.
    /// </summary>
    public readonly struct SaveResult
    {
        /// <summary>
        /// Whether the operation succeeded.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Exception that caused the failure, if any.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// The file path involved in the operation.
        /// </summary>
        public string FilePath { get; }

        private SaveResult(bool success, string filePath, string errorMessage = null, Exception exception = null)
        {
            Success = success;
            FilePath = filePath;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static SaveResult Ok(string filePath) => new SaveResult(true, filePath);

        /// <summary>
        /// Creates a failed result with an error message.
        /// </summary>
        public static SaveResult Fail(string filePath, string errorMessage) =>
            new SaveResult(false, filePath, errorMessage);

        /// <summary>
        /// Creates a failed result from an exception.
        /// </summary>
        public static SaveResult Fail(string filePath, Exception exception) =>
            new SaveResult(false, filePath, exception.Message, exception);

        public override string ToString() =>
            Success ? $"Success: {FilePath}" : $"Failed: {ErrorMessage}";
    }
}
