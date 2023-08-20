using System;

namespace DTT.Utils.CoroutineManagement.Exceptions
{
	/// <summary>
	/// Should be thrown when trying to start an already started coroutine.
	/// </summary>
	public class StartingStartedCoroutineException : CoroutineManagementException
	{
		/// <summary>
		/// The prefixed message in front of any
		/// <see cref="StartingStartedCoroutineException"/>.
		/// </summary>
		private const string PREFIX = " - [You can't start a coroutine that has already been started] - ";
		
		/// <summary>
		/// Create <see cref="StartingStartedCoroutineException"/> without a message.
		/// </summary>
		public StartingStartedCoroutineException() { }

		/// <summary>
		/// Create <see cref="StartingStartedCoroutineException"/> with a message.
		/// </summary>
		/// <param name="message">A message to further clarify the exception.</param>
		public StartingStartedCoroutineException(string message) : base(FormatMessage(PREFIX, message)) { }

		/// <summary>
		/// Create <see cref="StartingStartedCoroutineException"/> with a message and an inner exception.
		/// </summary>
		/// <param name="message">A message to further clarify the exception.</param>
		/// <param name="inner">The <see cref="Exception"/> instance that caused the current exception.</param>
		public StartingStartedCoroutineException(string message, Exception inner) : base(FormatMessage(PREFIX, message), inner) { }
	}
}