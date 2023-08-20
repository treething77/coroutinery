using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DTT.Utils.CoroutineManagement
{
    /// <summary>
    /// Composites multiple coroutine wrappers to provide functionalities like waiting until
    /// all or any of them have finished.
    /// </summary>
    public sealed class CompositeCoroutineWrapper : CoroutineWrapperBase
    {
        /// <summary>
        /// The coroutines this composite is waiting for.
        /// </summary>
        private readonly CustomCoroutineWrapper[] _coroutines;

        /// <summary>
        /// The coroutines this composite is waiting for.
        /// </summary>
        public ReadOnlyCollection<CustomCoroutineWrapper> Coroutines => Array.AsReadOnly(_coroutines);

        /// <summary>
        /// The wait yield instruction.
        /// </summary>
        private readonly WaitUntil _yieldInstruction;

        /// <summary>
        /// Initializes the class with enumerators its can wait for.
        /// </summary>
        /// <param name="routines">The coroutines this composite will be waiting for.</param>
        /// <param name="waitCondition">The wait condition the composite should use.</param>
        public CompositeCoroutineWrapper(IEnumerable<IEnumerator> routines, CompositeWaitCondition waitCondition)
        {
            _coroutines = routines.Select(routine => new CustomCoroutineWrapper(routine)).ToArray();

            switch (waitCondition)
            {
                case CompositeWaitCondition.ALL:
                    _yieldInstruction = new WaitUntil(AllCompleted);
                    break;
                
                case CompositeWaitCondition.ANY:
                    _yieldInstruction =  new WaitUntil(AnyCompleted);
                    break;
                
                default:
                    throw new NotImplementedException($"Wait condition {waitCondition} is not yet supported.");
            }
        }

        /// <summary>
        /// Waits for the coroutines to finish.
        /// </summary>
        protected override IEnumerator WaitOnYield()
        {
            yield return _yieldInstruction;
        }

        /// <summary>
        /// Returns whether all coroutines have finished.
        /// </summary>
        /// <returns>Whether all coroutines have finished.</returns>
        private bool AllCompleted() => _coroutines.All(c => c.HasFinished);

        /// <summary>
        /// Returns whether any coroutine has finished.
        /// </summary>
        /// <returns>Whether any coroutine has finished.</returns>
        private bool AnyCompleted() => _coroutines.Any(c => c.HasFinished);
    }
}
