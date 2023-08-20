using System;

namespace DTT.Utils.CoroutineManagement.Exceptions
{
	/// <summary>
	/// Should be thrown when trying to stop a coroutine that has already finished.
	/// </summary>
	public class StoppingFinishedCoroutineException : CoroutineManagementException
	{
		/// <summary>
		/// The prefixed message in front of any
		/// <see cref="StoppingFinishedCoroutineException"/>.
		/// </summary>
		private const string PREFIX = " - [You can't stop a coroutine that has already finished] - ";
		
		/// <summary>
		/// Create <see cref="StoppingFinishedCoroutineException"/> without a message.
		/// </summary>
		public StoppingFinishedCoroutineException() { }

		/// <summary>
		/// Create <see cref="StoppingFinishedCoroutineException"/> with a message.
		/// </summary>
		/// <param name="message">A message to further clarify the exception.</param>
		public StoppingFinishedCoroutineException(string message) : base(FormatMessage(PREFIX, message)) { }

		/// <summary>
		/// Create <see cref="StoppingFinishedCoroutineException"/> with a message and an inner exception.
		/// </summary>
		/// <param name="message">A message to further clarify the exception.</param>
		/// <param name="inner">The <see cref="Exception"/> instance that caused the current exception.</param>
		public StoppingFinishedCoroutineException(string message, Exception inner) : base(FormatMessage(PREFIX, message), inner) { }
	}
}