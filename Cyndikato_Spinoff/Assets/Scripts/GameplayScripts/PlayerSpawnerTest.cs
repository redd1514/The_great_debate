using UnityEngine;

/// <summary>
/// Quick test script to verify PlayerSpawner assignment works
/// Attach this to any GameObject and try assigning your PlayerSpawner
/// </summary>
public class PlayerSpawnerTest : MonoBehaviour
{
    [Header("Test Assignment")]
    public PlayerSpawner testPlayerSpawner; // This should accept your PlayerSpawner GameObject
    
    [Header("Debug Info")]
    public bool showDebugInfo = true;
    
    void Start()
    {
        if (showDebugInfo)
        {
            if (testPlayerSpawner != null)
            {
                Debug.Log($"[PlayerSpawnerTest] SUCCESS: PlayerSpawner assigned - {testPlayerSpawner.gameObject.name}");
                Debug.Log($"[PlayerSpawnerTest] PlayerSpawner component type: {testPlayerSpawner.GetType().Name}");
            }
            else
            {
                Debug.Log("[PlayerSpawnerTest] WARNING: No PlayerSpawner assigned to test field");
            }
        }
    }
    
    [ContextMenu("Test PlayerSpawner Assignment")]
    void TestAssignment()
    {
        if (testPlayerSpawner != null)
        {
            Debug.Log($"[PlayerSpawnerTest] PlayerSpawner '{testPlayerSpawner.gameObject.name}' is properly assigned!");
            Debug.Log($"[PlayerSpawnerTest] Has enhanced features: {testPlayerSpawner.GetType().GetMethod("SpawnPlayersFromSelection") != null}");
        }
        else
        {
            Debug.LogError("[PlayerSpawnerTest] PlayerSpawner field is still null!");
        }
    }
}