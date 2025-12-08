using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

/// <summary>
/// Spawns up to 4 players and pairs each with a distinct connected Gamepad.
/// Place this in a scene on an empty GameObject. Assign the player prefab (must contain PlayerController).
/// If fewer gamepads are connected than maxPlayers, remaining players won't spawn.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("Setup")] public GameObject playerPrefab; // Prefab with PlayerController
    [Range(1,4)] public int maxPlayers = 4;
    public float horizontalSpacing = 2.5f;
    public Vector2 startPosition = new Vector2(0, 0);

    [Header("Auto Spawn")]
    public bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnPlayers();
        }
    }

    public void SpawnPlayers()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner: playerPrefab not assigned.");
            return;
        }

        var pads = Gamepad.all;
        int toSpawn = Mathf.Min(maxPlayers, pads.Count);
        if (toSpawn == 0)
        {
            Debug.LogWarning("PlayerSpawner: No gamepads connected; nothing spawned.");
            return;
        }

        for (int i = 0; i < toSpawn; i++)
        {
            Vector3 pos = new Vector3(startPosition.x + i * horizontalSpacing, startPosition.y, 0);
            GameObject go = Instantiate(playerPrefab, pos, Quaternion.identity);
            var controller = go.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogError("PlayerSpawner: Spawned prefab missing PlayerController component.");
                continue;
            }

            // Assign player number (1-based)
            var numberField = typeof(PlayerController).GetField("playerNumber", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (numberField != null)
            {
                numberField.SetValue(controller, i + 1);
            }
            else
            {
                Debug.LogWarning("PlayerSpawner: Could not set playerNumber via reflection.");
            }

            Debug.Log($"PlayerSpawner: Spawned Player {i + 1} at {pos}");
        }
    }
}
