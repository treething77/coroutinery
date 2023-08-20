using DTT.Utils.CoroutineManagement.Exceptions;
using System;
using System.Collections;
using UnityEngine;

namespace DTT.Utils.CoroutineManagement
{
	/// <summary>
	/// Provides a base implementation for a Coroutine Wrapper.
	/// </summary>
	public abstract class CoroutineWrapperBase : ICoroutine
	{
		/// <summary>
		/// Gets called when the coroutine has finished.
		/// </summary>
		public event Action Finished;

		/// <summary>
		/// Gets called when the coroutine has been stopped.
		/// </summary>
		public event Action Stopped;

		/// <summary>
		/// Whether the coroutine has been started but hasn't finished yet.
		/// </summary>
		public bool InProgress => _hasStarted && !_hasFinished;

		/// <summary>
		/// Whether the coroutine has finished.
		/// </summary>
		public bool HasFinished => _hasFinished;

		/// <summary>
		/// Whether the coroutine has been started.
		/// </summary>
		public bool HasStarted => _hasStarted;

		/// <summary>
		/// Whether the coroutine has been stopped.
		/// </summary>
		public bool HasStopped => _hasStopped;

		/// <summary>
		/// Reference to the underlying coroutine used by the Unity API.
		/// </summary>
		public Coroutine Value { get; private set; }

		/// <summary>
		/// The moment at which the coroutine was created.
		/// </summary>
		public readonly DateTime creationTime;

		/// <summary>
		/// Provides a base for creating a Coroutine Wrapper
		/// This should generally only happen by <see cref="CoroutineManager"/>.
		/// </summary>
		protected CoroutineWrapperBase() => this.creationTime = DateTime.Now;
		
		/// <summary>
		/// Whether the coroutine has finished.
		/// </summary>
		private bool _hasFinished = false;

		/// <summary>
		/// Whether the coroutine has been started.
		/// </summary>
		private bool _hasStarted = false;

		/// <summary>
		/// Whether the coroutine has been stopped.
		/// </summary>
		private bool _hasStopped = false;

		/// <summary>
		/// Stops the coroutine from running.
		/// <b>This will not call <see cref="Finished"/></b>
		/// </summary>
		public void Stop()
		{
			if (_hasFinished)
			{
				throw new StoppingFinishedCoroutineException(
					"Trying to stop a coroutine that has already finished.");
			} 
			
			if (_hasStopped)
			{
				throw new StoppingStoppedCoroutineException(
					"Trying to stop a coroutine that has already been stopped.");
			}

			CoroutineManager.StopCoroutine(Value);

			_hasStopped = true;

			Stopped?.Invoke();
		}
		
		/// <summary>
		/// Starts the coroutine.
		/// </summary>
		internal void Start()
		{
			if (_hasStarted)
			{
				throw new StartingStartedCoroutineException("Trying to start a coroutine " +
				                                            "that has already been started.");
			}

			// The KickOffCoroutine() method is not a method on the 
			// CoroutineManager but instead from a MonoBehaviour.
			// We however need to call KickOffCoroutine() on a 
			// MonoBehaviour and CoroutineManager provides easy access for this.
			Value = CoroutineManager.KickOffCoroutine(StartCoroutine());
			_hasStarted = true;
		}
		
		/// <summary>
		/// Should wait on the yield instruction.
		/// </summary>
		/// <returns>The yield instruction to wait on.</returns>
		protected abstract IEnumerator WaitOnYield();
		
		/// <summary>
		/// Helper method that defines an implementation for 
		/// waiting on a yield instruction and after fires the callback.
		/// </summary>
		/// <returns>The yield instruction to wait on.</returns>
		private IEnumerator StartCoroutine()
		{
			yield return WaitOnYield();

			_hasFinished = true;

			CleanseFinishedCallbackTargets();
			Finished?.Invoke();
		}

		/// <summary>
		/// Cleanses the finished callback targets from targets that are null.
		/// </summary>
		private void CleanseFinishedCallbackTargets()
		{
			Delegate[] delegates = Finished.GetInvocationList();
			for (int i = delegates.Length - 1; i >= 0; i--)
			{
				Delegate del = delegates[i];
				object target = del.Target;
				if (target.Equals(null))
					Finished -= (Action)del;
			}
		}
	}
}