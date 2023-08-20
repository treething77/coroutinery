using DTT.Utils.CoroutineManagement;
using UnityEngine;

/// <summary>
/// Contains methods that start simple wait coroutines.
/// </summary>
public class SimpleWaitScriptableObject : ScriptableObject
{
    /// <summary>
    /// Waits for a delayed callback and stores a reference to the created coroutine.
    /// </summary>
    private void WaitForDelayedCallback()
    {
        // Wait for a delayed callback and store a reference to the created coroutine.
        CoroutineWrapper wrapper = CoroutineManager.WaitForSeconds(0.5f, () =>
        {
            /* Delayed execution code. */
        });
    }

    /// <summary>
    /// Waits for the end of the frame before calling back and stores a reference to the created coroutine.
    /// </summary>
    private void WaitForEndOfFrame()
    {
        CoroutineWrapper wrapper = CoroutineManager.WaitForEndOfFrame(() =>
        {
            /* Code to execute at the end of the frame. */
        });
    }

    /// <summary>
    /// Waits for five frames before calling back and stores a reference to the created coroutine.
    /// </summary>
    private void WaitForFiveFrames()
    {
        CustomCoroutineWrapper wrapper = CoroutineManager.WaitForFrames(5, () =>
        {
            /* Code to execute after 5 frames have passed. */
        });
    }
    
    [ContextMenu("Execute")]
    public void Execute()
    {
        WaitForDelayedCallback();
        WaitForFiveFrames();
        WaitForEndOfFrame();
    }
}
