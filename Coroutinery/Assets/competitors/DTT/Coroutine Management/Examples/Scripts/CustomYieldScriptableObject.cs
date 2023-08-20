using System;
using DTT.Utils.CoroutineManagement;
using UnityEngine;

/// <summary>
/// Contains a custom yield instruction and a method to yield it.
/// </summary>
public class CustomYieldScriptableObject : ScriptableObject
{
    /// <summary>
    /// Represents a custom yield instruction that wait for a given amount of enemies to spawn.
    /// </summary>
    private class WaitForEnemyCount : CustomYieldInstruction
    {
        /// <summary>
        /// Whether we need to keep waiting for enemies to spawn.
        /// </summary>
        public override bool keepWaiting
        {
            get
            {
               GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
               return enemies.Length >= _targetEnemyCount;
            }
        }

        /// <summary>
        /// The target enemy count.
        /// </summary>
        private readonly int _targetEnemyCount;

        /// <summary>
        /// Creates a new instance of this yield instruction.
        /// </summary>
        /// <param name="enemyCount">The amount of enemies to wait for.</param>
        public WaitForEnemyCount(int enemyCount) => _targetEnemyCount = enemyCount;
    }

    /// <summary>
    /// The enemy count to wait for.
    /// </summary>
    [SerializeField]
    private int _enemyCountToWaitFor = 5;

    /// <summary>
    /// The custom coroutine wrapper reference returned by the coroutine manager.
    /// </summary>
    private CustomCoroutineWrapper _wrapper;
    
    /// <summary>
    /// Executes the coroutine that waits for the custom yield instruction.
    /// </summary>
    [ContextMenu("Execute")]
    public void Execute()
    {
        // Start waiting for our custom yield instruction that waits for enemies to be spawned.
        WaitForEnemyCount waitForEnemyCount = new WaitForEnemyCount(_enemyCountToWaitFor);
        _wrapper = CoroutineManager.WaitForCustomYield(waitForEnemyCount, OnEnemiesSpawned);
    }

    /// <summary>
    /// Fired when enemies have been spawned to execute some code and log the spawn time.
    /// </summary>
    private void OnEnemiesSpawned()
    {
        /* Code to execute now that the enemies have been spawned. */

        int spawnTime = (_wrapper.creationTime - DateTime.Now).Seconds;
        Debug.Log($"All {_enemyCountToWaitFor} where spawned after {spawnTime} seconds.");
    }
}
