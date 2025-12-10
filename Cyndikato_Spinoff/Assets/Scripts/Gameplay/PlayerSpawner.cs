using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

// Spawns selected character prefabs at editor-defined spawn points and pairs controllers
public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Points by Player (0..3)")]
    public Transform[] spawnPoints; // assign in editor: index 0->P1, 1->P2, etc.

    [Header("Fallback (optional)")]
    public GameObject defaultPrefab; // used if a player has no selection

    void Start()
    {
        SpawnSelectedPlayers();
    }

    void SpawnSelectedPlayers()
    {
        foreach (int playerId in CharacterSelectionState.GetPlayerIds().OrderBy(id => id))
        {
            if (!CharacterSelectionState.TryGetSelection(playerId, out GameObject prefab, out UnityEngine.InputSystem.InputDevice device))
            {
                if (defaultPrefab == null) continue;
                prefab = defaultPrefab;
            }

            // Use PlayerInput.Instantiate to auto-pair the device and set player index
            var playerInput = PlayerInput.Instantiate(prefab, playerId, controlScheme: null, pairWithDevice: device);
            var go = playerInput.gameObject;

            // Place at spawn point
            var t = (spawnPoints != null && spawnPoints.Length > playerId) ? spawnPoints[playerId] : null;
            if (t != null)
            {
                go.transform.SetParent(t.parent);
                go.transform.position = t.position;
                go.transform.rotation = t.rotation;
            }

            // Optional: notify controller script of playerId - PlayerController handles this automatically
            var controller = go.GetComponent<PlayerController>();
            if (controller != null)
            {
                Debug.Log($"PlayerSpawner: Player {playerId + 1} controller spawned successfully");
            }
        }
    }
}
