using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace DTT.Utils.CoroutineManagement
{
    /// <summary>
    /// Provides global access to coroutine management features.
    /// </summary>
    public static class CoroutineManager
    {
        /// <summary>
        /// Reference to the CoroutineManager Instance on an MonoBehaviour to start coroutines on.
        /// </summary>
        private static CoroutineWorker _worker;

        /// <summary>
        /// Creates a game object with a CoroutineManager instance on it and
        /// makes sure the GameObject doesn't get destroyed on scene swaps.
        /// </summary>
        static CoroutineManager()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"Calling the {nameof(CoroutineManager)} during edit mode is not allowed.");
                return;
            }
            
            GameObject gameObject = new GameObject(nameof(CoroutineManager));
            _worker = gameObject.AddComponent<CoroutineWorker>();
            
            GameObject.DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Starts a Coroutine using the given IEnumerator.
        /// You can stop this Coroutine with <see cref"ICoroutine.Stop">.
        /// </summary>
        /// <param name="behaviour">The behaviour to be executed by the Coroutine.</param>
        /// <returns>Reference to the started ICoroutine.</returns>
        public static CustomCoroutineWrapper StartCoroutine(IEnumerator behaviour)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));

            return _worker.StartIEnumerator(behaviour);
        }

        /// <summary>
        /// Returns the active Coroutines stored in the <see cref="CoroutineWorker"/>.
        /// </summary>
        public static ReadOnlyCollection<ICoroutine> GetActiveCoroutines() => _worker.ActiveCoroutines;

        /// <summary>
        /// Waits for a given amount of seconds before executing a callback function.
        /// </summary>
        /// <param name="duration">The amount of seconds to wait.</param>
        /// <param name="callback">The callback to execute.</param>
        /// <returns>The reference to the coroutine wrapper.</returns>
        public static CoroutineWrapper WaitForSeconds(float duration, Action callback)
        {
            if (duration < 0)
                throw new ArgumentException("Duration can't be smaller than 0.");

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            
            return _worker.WaitForSeconds(duration, callback);
        }

        /// <summary>
        /// Waits until any of a collection of conditions is true before executing a callback function.
        /// </summary>
        /// <param name="conditions">The conditions to check.</param>
        /// <param name="callback">The callback to execute.</param>
        /// <returns>The Reference to the composite coroutine wrapper.</returns>
        public static CompositeCoroutineWrapper WaitForAny(IEnumerable<Func<bool>> conditions, Action callback)
        {
            if (conditions == null)
                throw new ArgumentNullException(nameof(conditions));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return _worker.WaitForAny(conditions, callback);
        }

        /// <summary>
        /// Waits until any of a collection of conditions is true before executing a callback function.
        /// </summary>
        /// <param name="routines">The routines to wait for.</param>
        /// <param name="callback">The callback to execute.</param>
        /// <returns>The reference to the composite coroutine wrapper.</returns>
        public static CompositeCoroutineWrapper WaitForAny(IEnumerable<IEnumerator> routines, Action callback)
        {
            if (routines == null)
                throw new ArgumentNullException(nameof(routines));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return _worker.WaitForAny(routines, callback);
        }

        /// <summary>
        /// Waits until a collection of conditions is true before executing a callback function.
        /// </summary>
        /// <param name="conditions">The conditions to wait for.</param>
        /// <param name="callback">The callback to execute.</param>
        /// <returns>The reference to the composite coroutine wrapper.</returns>
        public static CompositeCoroutineWrapper WaitForAll(IEnumerable<Func<bool>> conditions, Action callback)
        {
            if (conditions == null)
                throw new ArgumentNullException(nameof(conditions));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));


            return _worker.WaitForAll(conditions, callback);
        }

        /// <summary>
        /// Waits until a collection of conditions is true before executing a callback function.
        /// </summary>
        /// <param name="routines">The routines to wait for.</param>
        /// <param name="callback">The callback to execute.</param>
        /// <returns>The reference to the composite coroutine wrapper.</returns>
        public static CompositeCoroutineWrapper WaitForAll(IEnumerable<IEnumerator> routines, Action callback)
        {
            if (routines == null)
                throw new ArgumentNullException(nameof(routines));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return _worker.WaitForAll(routines, callback);
        }
        
        /// <summary>
        /// Waits for a given amount of frames before executing a callback.
        /// </summary>
        /// <param name="frames">The amount of frames to wait for.</param>
        /// <param name="callback">The callback to execute.</param>
        /// <returns>The reference to the custom coroutine wrapper.</returns>
        public static CustomCoroutineWrapper WaitForFrames(int frames, Action callback)
        {
            if (frames < 0)
                throw new ArgumentException(nameof(frames));
            
            return _worker.WaitForFrames(frames, callback);
        }
        /// <summary>
        /// Waits until a given condition is met before executing a callback function.
        /// </summary>
        public static CustomCoroutineWrapper WaitUntil(Func<bool> condition, Action callback)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            
            return _worker.WaitUntil(condition, callback);
        }
        /// <summary>
        /// Waits until a given condition or timeout is met before executing a callback function.
        /// </summary>
        public static CustomCoroutineWrapper WaitUntil(Func<bool> condition, float timeout, Action callback)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            if (timeout < 0)
                throw new ArgumentException("Timeout can't be smaller than 0.");

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            
            return _worker.WaitUntil(condition, timeout, callback);
        }

        /// <summary>
        /// Waits for the end of a frame before executing a callback function.
        /// </summary>
        public static CoroutineWrapper WaitForEndOfFrame(Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            
            return _worker.WaitForEndOfFrame(callback);
        }

        /// <summary>
        /// Waits for a custom yield instruction before executing a callback function.
        /// </summary>
        public static CustomCoroutineWrapper WaitForCustomYield(CustomYieldInstruction yieldInstruction,
            Action callback)
        {
            if (yieldInstruction == null)
                throw new ArgumentNullException(nameof(yieldInstruction));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            
            return _worker.WaitForCustomYield(yieldInstruction, callback);
        }

        /// <summary>
        /// Starts a Coroutine on the CoroutineManager MonoBehaviour.
        /// </summary>
        /// <param name="behaviour">The behaviour to be executed by the Coroutine.</param>
        /// <returns>Reference to the started Coroutine.</returns>
        internal static Coroutine KickOffCoroutine(IEnumerator behaviour)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));

            return _worker.StartCoroutine(behaviour);
        }

        /// <summary>
        /// Stops a Coroutine on the CoroutineManager MonoBehaviour.
        /// </summary>
        /// <param name="coroutine">The coroutine that needs to be stopped.</param>
        internal static void StopCoroutine(Coroutine coroutine)
        {
            if (coroutine == null)
                throw new ArgumentNullException(nameof(coroutine));

            _worker.StopCoroutine(coroutine);
        }
    }
}
