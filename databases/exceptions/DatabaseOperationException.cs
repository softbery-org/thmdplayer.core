// Version: 1.0.0.444
using MySql.Data.MySqlClient;
using System;

namespace ThmdPlayer.Core.databases.exceptions
{
    /// <summary>
    /// Represents errors that occur during database operations.
    /// </summary>
    public class DatabaseOperationException : Exception
    {
        /// <summary>
        /// SQL command that caused the exception.
        /// </summary>
        public string SqlCommand { get; }
        /// <summary>
        /// Error code returned by the database operation.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseOperationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">Message</param>
        public DatabaseOperationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseOperationException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Exception</param>
        public DatabaseOperationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseOperationException"/> class with a specified error message and SQL command.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="sqlCommand">Sql command</param>
        /// <param name="errorCode">Error code</param>
        public DatabaseOperationException(string message, string sqlCommand, int errorCode)
            : base($"{message}\nCommand: {sqlCommand.Truncate(200)}\nError Code: {errorCode}")
        {
            SqlCommand = sqlCommand;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseOperationException"/> class with a specified error message, SQL command, and inner exception.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="sqlCommand">Sql command</param>
        /// <param name="innerException">Exception</param>
        public DatabaseOperationException(string message, string sqlCommand, Exception innerException)
            : base($"{message}\nCommand: {sqlCommand.Truncate(200)}", innerException)
        {
            SqlCommand = sqlCommand;
            if (innerException is MySqlException mysqlEx)
            {
                ErrorCode = mysqlEx.Number;
            }
        }
    }

    /// <summary>
    /// Extension methods for string manipulation.
    /// </summary>
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value.Length <= maxLength
                    ? value
                    : value.Substring(0, maxLength) + "...";
        }
    }
}
