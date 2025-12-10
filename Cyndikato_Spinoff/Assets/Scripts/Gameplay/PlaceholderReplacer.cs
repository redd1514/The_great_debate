using UnityEngine;
using UnityEngine.InputSystem;

// Optional interface your character controller can implement to receive playerId
public interface IPlayerController
{
    void SetPlayerId(int playerId);
}

// Replaces editor-placed placeholder prefabs with the characters selected in Character Select.
// Assign your 4 placeholders (Player 1..4) in the inspector.
public class PlaceholderReplacer : MonoBehaviour
{
    [Header("Placeholders by Player (0..3)")]
    public GameObject[] placeholdersByPlayer = new GameObject[4];

    [Header("Behavior")]
    public bool destroyPlaceholders = false; // if false, disables instead
    public bool keepPlaceholderIfNoSelection = true; // if false and no selection, hides the placeholder

    void Start()
    {
        ReplaceAll();
    }

    public void ReplaceAll()
    {
        Debug.Log("PlaceholderReplacer: Starting replacement process");

        for (int playerId = 0; playerId < placeholdersByPlayer.Length; playerId++)
        {
            var placeholder = placeholdersByPlayer[playerId];

            // Get selection for this player
            if (CharacterSelectionState.TryGetSelection(playerId, out GameObject prefab, out InputDevice device) && prefab != null)
            {
                Debug.Log($"PlaceholderReplacer: Found selection for Player {playerId + 1}: {prefab.name} with device {device?.displayName ?? "null"}");

                Vector3 pos = placeholder != null ? placeholder.transform.position : Vector3.zero;
                Quaternion rot = placeholder != null ? placeholder.transform.rotation : Quaternion.identity;
                Transform parent = placeholder != null ? placeholder.transform.parent : null;

                // Simple instantiation without PlayerInput.Instantiate since we don't use that system
                var go = Instantiate(prefab, pos, rot, parent);

                // Set up player controller if it exists
                var controller = go.GetComponent<IPlayerController>();
                if (controller != null)
                {
                    controller.SetPlayerId(playerId);
                    Debug.Log($"PlaceholderReplacer: Set player ID {playerId + 1} on controller");
                }
                else
                {
                    // Try PlayerController if IPlayerController is not implemented
                    var playerController = go.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        // Use reflection to set player number as in the original PlayerSpawner
                        var numberField = typeof(PlayerController).GetField("playerNumber", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                        if (numberField != null)
                        {
                            numberField.SetValue(playerController, playerId + 1); // 1-based player number
                            Debug.Log($"PlaceholderReplacer: Set player number {playerId + 1} on PlayerController via reflection");
                        }
                        else
                        {
                            Debug.LogWarning($"PlaceholderReplacer: Could not set playerNumber on PlayerController for player {playerId + 1}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"PlaceholderReplacer: No PlayerController or IPlayerController found on instantiated prefab for player {playerId + 1}");
                    }
                }

                // Hide or destroy the placeholder
                if (placeholder != null)
                {
                    if (destroyPlaceholders)
                    {
                        Debug.Log($"PlaceholderReplacer: Destroying placeholder for Player {playerId + 1}");
                        Destroy(placeholder);
                    }
                    else
                    {
                        Debug.Log($"PlaceholderReplacer: Disabling placeholder for Player {playerId + 1}");
                        placeholder.SetActive(false);
                    }
                }

                Debug.Log($"PlaceholderReplacer: Successfully replaced placeholder for Player {playerId + 1}");
            }
            else
            {
                Debug.Log($"PlaceholderReplacer: No selection found for Player {playerId + 1}");

                // No selection for this player
                if (!keepPlaceholderIfNoSelection && placeholder != null)
                {
                    Debug.Log($"PlaceholderReplacer: Hiding placeholder for unselected Player {playerId + 1}");
                    placeholder.SetActive(false);
                }
            }
        }

        Debug.Log("PlaceholderReplacer: Replacement process complete");
    }
}
