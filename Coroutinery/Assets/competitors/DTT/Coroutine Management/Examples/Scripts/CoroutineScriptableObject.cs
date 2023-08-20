using System.Collections;
using DTT.Utils.CoroutineManagement;
using UnityEngine;

/// <summary>
/// Contains a method that starts a custom coroutine.
/// </summary>
public class CoroutineScriptableObject : ScriptableObject
{
    /// <summary>
    /// The current lerp value.
    /// </summary>
    private float _value;
    
    /// <summary>
    /// Executes the custom linear interpolation coroutine.
    /// </summary>
    [ContextMenu("Execute")]
    public void Execute()
    {
        // Start a custom coroutine from an object that is not a MonoBehaviour.
        CustomCoroutineWrapper wrapper = CoroutineManager.StartCoroutine(LinearlyInterpolateValue());
    }

    /// <summary>
    /// Linearly interpolates the value from 0 to 1 in 1 second.
    /// </summary>
    private IEnumerator LinearlyInterpolateValue()
    {
        const float LERP_TARGET = 1.0f;
        const float LERP_TIME = 1.0f;
        
        float currentLerpTime = 0.0f;
        while (currentLerpTime < LERP_TIME)
        {
            currentLerpTime += Time.deltaTime;

            float percentage = currentLerpTime / LERP_TIME;
            _value = Mathf.Lerp(0.0f, LERP_TARGET, percentage);

            yield return null;
        }
    }
}
