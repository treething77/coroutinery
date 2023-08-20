using System;
using UnityEngine;

namespace DTT.Utils.CoroutineManagement.CustomYieldInstructions
{
	/// <summary>
	/// Custom yield instructor for waiting until a 
	/// predicate has finished or when a timeout is reached.
	/// </summary>
	public class WaitUntilTimeout : CustomYieldInstruction
	{
		/// <summary>
		/// Keep waiting until the predicate evaluates to true or the timeout is reached.
		/// <!--We inverse the predicate since it means the opposite to keepWaiting.-->
		/// </summary>
		public override bool keepWaiting => !_predicate() && Time.time < _startTime + _timeout;
		
		/// <summary>
		/// The time the coroutine was started.
		/// Used for determining whether the timeout has been reached.
		/// </summary>
		private readonly float _startTime;

		/// <summary>
		/// The amount of time it should take before we stop 
		/// evaluating <see cref="_predicate"/> and make the callback.
		/// </summary>
		private readonly float _timeout;

		/// <summary>
		/// The predicate that when evaluated to true 
		/// should stop the coroutine and make the callback.
		/// </summary>
		private readonly Func<bool> _predicate;
		
		/// <summary>
		/// Creates a new yield instruction that waits until the
		/// predicates evaluates to true or the timeout is reached.
		/// </summary>
		/// <param name="predicate">
		/// The predicate that determines when 
		/// the instruction should stop waiting.
		/// </param>
		/// <param name="timeout">
		/// The duration in seconds for when to give up on the
		/// predicate and the instruction should stop waiting.
		/// </param>
		
		public WaitUntilTimeout(Func<bool> predicate, float timeout)
		{
			if (timeout < 0)
			{
				timeout = 0;
				Debug.LogWarning("Attempting to start WaitUntilTimout " +
					"yield instructor with a negative timeout.");
			}

			this._timeout = timeout;
			this._predicate = predicate;
			this._startTime = Time.time;
		}

		/// <summary>
		/// Creates a new yield instruction that waits until the 
		/// predicates evaluates to true.
		/// </summary>
		/// <param name="predicate">
		/// The predicate that determines when 
		/// the instruction should stop waiting.
		/// </param>
		public WaitUntilTimeout(Func<bool> predicate) : this(predicate, float.MaxValue) { }
	}
}
