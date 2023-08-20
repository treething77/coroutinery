using System;
using System.Collections;
using DTT.Utils.CoroutineManagement;
using UnityEngine;

/// <summary>
/// Contains methods that start advanced waiting coroutines.
/// </summary>
public class AdvancedWaitScriptableObject : ScriptableObject
{
    /// <summary>
    /// Waits for a condition that checks whether the player is created.
    /// </summary>
    private void WaitUntil()
    {
        // Wait for our condition that checks whether the player is created
        // and store a reference to the created coroutine.
        CustomCoroutineWrapper wrapper = CoroutineManager.WaitUntil(IsPlayerCreated, () =>
        {
            /* Code to execute now that the player is created. */
        });
    }

    /// <summary>
    /// Waits for multiple conditions to be met and then executes a callback.
    /// </summary>
    private void WaitForAll()
    {
        // Conditions can be defined as predicates functions.
        Func<bool>[] conditions = new Func<bool>[]
        {
            IsPlayerCreated,
            AreThereEnemiesToSlay,
            AreThereNonPlayerCharacters
        };
        
        // Wait for all our conditions to be met and then execute the callback.
        CompositeCoroutineWrapper wrapper = CoroutineManager.WaitForAll(conditions, () =>
        {
            /* Code to execute after all conditions have been met. */
        });
    }

    /// <summary>
    /// Waits for any of a set of conditions to be met and then executes a callback.
    /// </summary>
    private void WaitForAny()
    {
        // Conditions can be defined as enumerators.
        IEnumerator[] conditions = new IEnumerator[]
        {
            WaitForPlayerCreation(),
            WaitForEnemiesToSpawn(),
            WaitForNonPlayerCharacterSpawns(),
        };
        
        // Wait for any of our conditions to be met and then execute the callback.
        CompositeCoroutineWrapper wrapper = CoroutineManager.WaitForAny(conditions, () =>
        {
            /* Code to execute after any of the conditions has been met. */
        });
    }

    /// <summary>
    /// Returns whether a game object with the player tag is created.
    /// </summary>
    /// <returns>Whether a game object with the player tag is created.</returns>
    private bool IsPlayerCreated()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null;
    }

    /// <summary>
    /// Returns whether game objects with the enemy tag are created.
    /// </summary>
    /// <returns>Whether game objects with the enemy tag are created.</returns>
    private bool AreThereEnemiesToSlay()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.Length != 0;
    }

    /// <summary>
    /// Returns whether game objects with the NPC tag are created.
    /// </summary>
    /// <returns>Whether game objects with the NPC tag are created.</returns>
    private bool AreThereNonPlayerCharacters()
    {
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
        return npcs.Length != 0;
    }

    /// <summary>
    /// Yields until a player is created.
    /// </summary>
    private IEnumerator WaitForPlayerCreation()
    {
        const float checkInterval = 0.75f;

        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        GameObject player = GetPlayer();
        while (player == null)
        {
            yield return wait;
            player = GetPlayer();
        }
        
        GameObject GetPlayer() => GameObject.FindGameObjectWithTag("Player");
    }

    /// <summary>
    /// Yields until enemies are created.
    /// </summary>
    private IEnumerator WaitForEnemiesToSpawn()
    {
        const float checkInterval = 1.25f;
        const int targetEnemyCount = 5;

        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        GameObject[] enemies = GetEnemies();
        while (enemies.Length < targetEnemyCount)
        {
            yield return wait;
            enemies = GetEnemies();
        }
        
        GameObject[] GetEnemies() => GameObject.FindGameObjectsWithTag("Enemy");
    }

    /// <summary>
    /// Yields until npcs are created.
    /// </summary>
    private IEnumerator WaitForNonPlayerCharacterSpawns()
    {
        const float checkInterval = 1f;
        const int targetNpcCount = 8;

        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        GameObject[] npcs = GetNPCS();
        while (npcs.Length < targetNpcCount)
        {
            yield return wait;
            npcs = GetNPCS();
        }
        
        GameObject[] GetNPCS() => GameObject.FindGameObjectsWithTag("NPC");
    }
    
    /// <summary>
    /// Executes all waiting coroutines.
    /// </summary>
    [ContextMenu("Execute")]
    public void Execute()
    {
        WaitUntil();
        WaitForAny();
        WaitForAll();
    }
}
