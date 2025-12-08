using UnityEngine;
using UnityEngine.InputSystem;
#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// Optional: Displays the paired device name above the player.
/// Assign a TextMeshPro component in the inspector or it will just log.
/// </summary>
public class PlayerDeviceLabel : MonoBehaviour
{
#if TMP_PRESENT
    public TextMeshPro text;
#endif
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    private void Start()
    {
        var pad = GetPairedGamepad();
        string label = pad != null ? pad.displayName : "Keyboard";

#if TMP_PRESENT
        if (text == null)
        {
            var go = new GameObject("DeviceLabel");
            go.transform.SetParent(transform);
            go.transform.localPosition = offset;
            text = go.AddComponent<TextMeshPro>();
            text.fontSize = 3f;
            text.alignment = TextAlignmentOptions.Center;
        }
        text.text = label;
#endif
        Debug.Log($"[{name}] Paired input: {label}");
    }

    private Gamepad GetPairedGamepad()
    {
        // Heuristic: choose gamepad by proximity of index to PlayerController.playerNumber
        var controller = GetComponent<PlayerController>();
        if (controller == null) return null;
        var pads = Gamepad.all;
        if (pads.Count == 0) return null;
        int idx = Mathf.Clamp(controller.GetHashCode() % pads.Count, 0, pads.Count - 1);
        return pads[idx];
    }
}
