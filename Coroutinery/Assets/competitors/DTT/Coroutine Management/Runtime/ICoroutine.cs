using System;

namespace DTT.Utils.CoroutineManagement
{
	/// <summary>
	/// Interface for how coroutines in the <see cref="CoroutineManager"/> should behave.
	/// </summary>
	public interface ICoroutine
	{
		/// <summary>
		/// Should be called when the coroutine has finished.
		/// </summary>
		event Action Finished;

		/// <summary>
		/// Should be called when the coroutine has been stopped.
		/// </summary>
		event Action Stopped;
		
		/// <summary>
		/// Should reflect whether it's in progress.
		/// </summary>
		bool InProgress { get; }

		/// <summary>
		/// Should reflect whether it's been started.
		/// </summary>
		bool HasStarted { get; }

		/// <summary>
		/// Should reflect whether it has finished.
		/// </summary>
		bool HasFinished { get; }

		/// <summary>
		/// Should reflect whether it's been stopped.
		/// </summary>
		bool HasStopped { get; }

		/// <summary>
		/// Should stop the coroutine.
		/// </summary>
		void Stop();
	}
}