namespace DTT.Utils.CoroutineManagement
{
    /// <summary>
    /// Represents the composite wait condition types.
    /// </summary>
    public enum CompositeWaitCondition 
    {
        /// <summary>
        /// Waits until all coroutines have finished.
        /// </summary>
        ALL = 0,
        
        /// <summary>
        /// Waits until one of the coroutines has finished.
        /// </summary>
        ANY = 1,
    }
}