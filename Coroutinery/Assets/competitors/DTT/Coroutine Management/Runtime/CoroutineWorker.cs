using DTT.Utils.CoroutineManagement.CustomYieldInstructions;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DTT.Utils.CoroutineManagement
{
	/// <summary>
	/// Simple class used for starting coroutines such as Wait(time, action) 
	/// or WaitUntil(predictor, action). Can be used instead of writing 
	/// IEnumerator methods. Also useful for inactive scripts.
	/// </summary>
	[HelpURL("https://dtt-dev.atlassian.net/wiki/spaces/UCP/pages/1464991786/Coroutine+Manager")]
	internal sealed class CoroutineWorker : MonoBehaviour
	{
		/// <summary>
		/// Contains all the coroutines that have been 
		/// started using the <see cref="CoroutineManager"/>.
		/// </summary>
		public ReadOnlyCollection<ICoroutine> ActiveCoroutines => _activeCoroutines.AsReadOnly();
		
		/// <summary>
		/// Contains all the coroutines that have been 
		/// started using the <see cref="CoroutineManager"/>.
		/// </summary>
		private List<ICoroutine> _activeCoroutines = new List<ICoroutine>();

		/// <summary>
		/// Creating an <see cref="ICoroutine"/> from an IEnumerable so it can be tracked by <see cref="CoroutineWorker"/>.
		/// </summary>
		/// <param name="userCoroutine">IEnumerable to create an ICoroutine from.</param>
		/// <returns>A reference to the created <see cref="UserCoroutineWrapper"/>.</returns>
		public CustomCoroutineWrapper StartIEnumerator(IEnumerator userCoroutine)
		{
			CustomCoroutineWrapper coroutine = new CustomCoroutineWrapper(userCoroutine);
			KickOffCoroutine(coroutine);
			return coroutine;
		}
		
		/// <summary>
		/// Wait the given amount of seconds before invoking the callback.
		/// </summary>
		/// <param name="duration">The duration of the wait.</param>
		/// <param name="callback">The callback for when the operation finishes.</param>
		/// <returns>A reference to the created <see cref="CoroutineWrapper"/>.</returns>
		public CoroutineWrapper WaitForSeconds(float duration, Action callback)
		{
			CoroutineWrapper coroutine = new CoroutineWrapper(new WaitForSeconds(duration));
			KickOffCoroutine(coroutine, callback);
			return coroutine;
		}

		/// <summary>
		/// Wait for the given amount of frames before invoking the callback.
		/// </summary>
		/// <param name="frameCount">The amount of frames to wait for.</param>
		/// <param name="callback">The callback for when the operation finished.</param>
		/// <returns>A reference to the created <see cref="CustomCoroutineWrapper"/>.</returns>
		public CustomCoroutineWrapper WaitForFrames(int frameCount, Action callback)
		{
			CustomCoroutineWrapper coroutine = new CustomCoroutineWrapper(new WaitForFrames(frameCount));
			KickOffCoroutine(coroutine, callback);
			return coroutine;
		}

		/// <summary>
		/// Waits until any of a collection of conditions is true before executing a callback function.
		/// </summary>
		/// <param name="routines">The routines to wait for.</param>
		/// <param name="callback">The callback to execute.</param>
		/// <returns>The reference to the composite coroutine wrapper.</returns>
		public CompositeCoroutineWrapper WaitForAll(IEnumerable<IEnumerator> routines, Action callback)
		{
			CompositeCoroutineWrapper composite = new CompositeCoroutineWrapper(routines, CompositeWaitCondition.ALL);
			KickOffCoroutine(composite, callback);
			
			foreach(CustomCoroutineWrapper coroutine in composite.Coroutines)
				KickOffCoroutine(coroutine);
			
			return composite;
		}
		
		/// <summary>
		/// Waits until a collection of conditions is true before executing a callback function.
		/// </summary>
		/// <param name="conditions">The conditions to wait for.</param>
		/// <param name="callback">The callback to execute.</param>
		/// <returns>The reference to the composite coroutine wrapper.</returns>
		public CompositeCoroutineWrapper WaitForAll(IEnumerable<Func<bool>> conditions, Action callback)
		{
			IEnumerable<IEnumerator> routines = conditions.Select(condition => new WaitUntil(condition)).AsEnumerable();
			return WaitForAll(routines, callback);
		}
		
		/// <summary>
		/// Waits until any of a collection of conditions is true before executing a callback function.
		/// </summary>
		/// <param name="routines">The routines to wait for.</param>
		/// <param name="callback">The callback to execute.</param>
		/// <returns>The reference to the composite coroutine wrapper.</returns>
		public CompositeCoroutineWrapper WaitForAny(IEnumerable<IEnumerator> routines, Action callback)
		{
			CompositeCoroutineWrapper composite = new CompositeCoroutineWrapper(routines, CompositeWaitCondition.ANY);
			KickOffCoroutine(composite, callback);
			
			foreach(CustomCoroutineWrapper coroutine in composite.Coroutines)
				KickOffCoroutine(coroutine);
			
			return composite;
		}
		
		/// <summary>
		/// Waits until any of a collection of conditions is true before executing a callback function.
		/// </summary>
		/// <param name="conditions">The conditions to check.</param>
		/// <param name="callback">The callback to execute.</param>
		/// <returns>The Reference to the composite coroutine wrapper.</returns>
		public CompositeCoroutineWrapper WaitForAny(IEnumerable<Func<bool>> conditions, Action callback)
		{
			IEnumerable<IEnumerator> routines = conditions.Select(condition => new WaitUntil(condition)).AsEnumerable();
			return WaitForAny(routines, callback);
		}

		/// <summary>
		/// Waits for the predicate to switch to true before invoking the callback.
		/// </summary>
		/// <param name="predicate">
		/// The predicate that defines when the callback should be invoked.
		/// </param>
		/// <param name="callback">The callback for when the operation finishes.</param>
		/// <returns>A reference to the created <see cref="CustomCoroutineWrapper"/>.</returns>
		public CustomCoroutineWrapper WaitUntil(Func<bool> predicate, Action callback)
			=> WaitForCustomYield(new WaitUntilTimeout(predicate), callback);

		/// <summary>
		/// Waits for a certain predictor to be true or when the time out has been reached.
		/// Can prove useful for when dealing with API calls.
		/// </summary>
		/// <param name="predicate">
		/// The predicate that defines when the callback should be invoked.
		/// </param>
		/// <param name="timeout">The maximum duration to wait checking the predicate.</param>
		/// <param name="callback">The callback for when the operation finishes.</param>
		/// <returns>A reference to the created <see cref="CustomCoroutineWrapper"/>.</returns>
		public CustomCoroutineWrapper WaitUntil(Func<bool> predicate, float timeout, Action callback)
			=> WaitForCustomYield(new WaitUntilTimeout(predicate, timeout), callback);

		/// <summary>
		/// Wait a frame before invoking the callback.
		/// </summary>
		/// <param name="callback">The callback for when the operation finishes.</param>
		/// <returns>A reference to the created <see cref="CoroutineWrapper"/>.</returns>
		public CoroutineWrapper WaitForEndOfFrame(Action callback)
		{
			CoroutineWrapper coroutine = new CoroutineWrapper(new WaitForEndOfFrame());
			KickOffCoroutine(coroutine, callback);
			return coroutine;
		}

		/// <summary>
		/// Can use a custom <see cref="CustomYieldInstruction"/> to 
		/// create a coroutine with the <see cref="CoroutineManager"/>.
		/// </summary>
		/// <param name="yieldInstruction">The custom instruction to wait on.</param>
		/// <param name="callback">The callback for when the operation finishes.</param>
		/// <returns>A reference to the created <see cref="CustomCoroutineWrapper"/>.</returns>
		public CustomCoroutineWrapper WaitForCustomYield(CustomYieldInstruction yieldInstruction, Action callback)
		{
			CustomCoroutineWrapper coroutine = new CustomCoroutineWrapper(yieldInstruction);
			KickOffCoroutine(coroutine, callback);
			return coroutine;
		}

		/// <summary>
		/// Helper method for starting a coroutine, adding listeners 
		/// and tracking it in the <see cref="CoroutineManager"/>.
		/// </summary>
		/// <param name="coroutine">The coroutine to kick-off.</param>
		private void KickOffCoroutine(CoroutineWrapperBase coroutine)
		{
			coroutine.Start();
			coroutine.Finished += () => OnCoroutineFinished(coroutine);
			coroutine.Stopped += () => OnCoroutineStopped(coroutine);

			_activeCoroutines.Add(coroutine);
		}

		/// <summary>
		/// Helper method for starting a coroutine, adding listeners 
		/// and tracking it in the <see cref="CoroutineManager"/>.
		/// </summary>
		/// <param name="coroutine">The coroutine to kick-off.</param>
		/// <param name="callback">The callback for when the operation finishes.</param>
		private void KickOffCoroutine(CoroutineWrapperBase coroutine, Action callback)
		{
			coroutine.Start();
			coroutine.Finished += callback;
			coroutine.Finished += () => OnCoroutineFinished(coroutine);
			coroutine.Stopped += () => OnCoroutineStopped(coroutine);

			_activeCoroutines.Add(coroutine);
		}

		/// <summary>
		/// Gets called when a coroutine finishes.
		/// Will remove the listeners from the coroutine and stop tracking it.
		/// </summary>
		/// <param name="coroutine">The coroutine that has finished.</param>
		private void OnCoroutineFinished(ICoroutine coroutine) => _activeCoroutines.Remove(coroutine);

		/// <summary>
		/// Gets called when a coroutine gets stopped.
		/// Will remove the listeners from the coroutine and stop tracking it.
		/// </summary>
		/// <param name="coroutine"></param>
		private void OnCoroutineStopped(ICoroutine coroutine) => _activeCoroutines.Remove(coroutine);
	}
}