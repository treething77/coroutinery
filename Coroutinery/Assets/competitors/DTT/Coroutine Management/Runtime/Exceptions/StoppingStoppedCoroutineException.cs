using System;

namespace DTT.Utils.CoroutineManagement.Exceptions
{
	/// <summary>
	/// Should be thrown when trying to stop a coroutine that has already been stopped.
	/// </summary>
	public class StoppingStoppedCoroutineException : CoroutineManagementException
	{
		/// <summary>
		/// The prefixed message in front of any
		/// <see cref="StoppingStoppedCoroutineException"/>.
		/// </summary>
		private const string PREFIX = " - [You can't stop a coroutine that has already been stopped] - ";
		
		/// <summary>
		/// Create <see cref="CoroutineCantBeFoundException"/> without a message.
		/// </summary>
		public StoppingStoppedCoroutineException() { }

		/// <summary>
		/// Create <see cref="StoppingStoppedCoroutineException"/> with a message.
		/// </summary>
		/// <param name="message">A message to further clarify the exception.</param>
		public StoppingStoppedCoroutineException(string message) : base(FormatMessage(PREFIX, message)) { }

		/// <summary>
		/// Create <see cref="StoppingStoppedCoroutineException"/> with a message and an inner exception.
		/// </summary>
		/// <param name="message">A message to further clarify the exception.</param>
		/// <param name="inner">The <see cref="Exception"/> instance that caused the current exception.</param>
		public StoppingStoppedCoroutineException(string message, Exception inner) : base(FormatMessage(PREFIX, message), inner) { }
	}
}