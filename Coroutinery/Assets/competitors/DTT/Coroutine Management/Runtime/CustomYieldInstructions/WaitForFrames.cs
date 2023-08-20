using UnityEngine;

namespace DTT.Utils.CoroutineManagement.CustomYieldInstructions
{
    /// <summary>
    /// Waits for a given amount of frames.
    /// Note that frames checks are queued after MonoBehaviour.Update and before MonoBehaviour.LateUpdate.
    /// </summary>
    public class WaitForFrames : CustomYieldInstruction
    {
        /// <summary>
        /// Whether the target frame count has not been reached yet.
        /// </summary>
        public override bool keepWaiting
        {
            get
            {
                _currentFrameCount++;
                return _currentFrameCount != _targetTargetFrameCount;
            }
        }

        /// <summary>
        /// The current frame count.
        /// </summary>
        private int _currentFrameCount;

        /// <summary>
        /// The target frame count to wait for.
        /// </summary>
        private readonly int _targetTargetFrameCount;

        /// <summary>
        /// Initializes the class with the target frame count to wait for.
        /// </summary>
        /// <param name="targetFrameCount">The target frame count to wait for.</param>
        public WaitForFrames(int targetFrameCount) => _targetTargetFrameCount = targetFrameCount;
    }
}
