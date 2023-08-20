using System;

namespace DTT.Utils.CoroutineManagement.Exceptions
{
	/// <summary>
	/// The base class for all the exceptions in the Coroutine Manager package.
	/// </summary>
	public abstract class CoroutineManagementException : Exception
	{
		/// <summary>
		/// The prefix of each exception in this package.
		/// </summary>
		private const string PREFIX = "[DTT] - [COROUTINE MANAGER]";
		
		/// <summary>
		/// Create a <see cref="CoroutineManagementException"/> without a message.
		/// </summary>
		public CoroutineManagementException() { }

		/// <summary>
		/// Create a <see cref="CoroutineManagementException"/> with given message.
		/// </summary>
		/// <param name="message">The message to show.</param>
		public CoroutineManagementException(string message)
			: base(FormatMessage(PREFIX, message)) { }

		/// <summary>
		/// Create a <see cref="CoroutineManagementException"/> with given message
		/// and inner exception.
		/// </summary>
		/// <param name="message">The message to show.</param>
		/// <param name="innerException">
		/// The <see cref="Exception"/> instance that caused the current exception.
		/// </param>
		public CoroutineManagementException(string message, Exception innerException)
			: base(FormatMessage(PREFIX, message), innerException) { }
		
		/// <summary>
		/// Will format the message to apply a prefix.
		/// </summary>
		/// <param name="prefix">The prefix to apply.</param>
		/// <param name="message">The message to format.</param>
		/// <returns>The modified message.</returns>
		protected static string FormatMessage(string prefix, string message) => $"{prefix} - {message}";
	}
}