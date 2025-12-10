using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Persist selected character prefab and controller device per player across scenes
public class CharacterSelectionState : MonoBehaviour
{
    public static CharacterSelectionState Instance { get; private set; }

    private readonly Dictionary<int, GameObject> _prefabs = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, UnityEngine.InputSystem.InputDevice> _devices = new Dictionary<int, UnityEngine.InputSystem.InputDevice>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void SetSelection(int playerId, GameObject prefab, UnityEngine.InputSystem.InputDevice device)
    {
        EnsureInstance();
        Instance._prefabs[playerId] = prefab;
        Instance._devices[playerId] = device;
    }

    public static bool TryGetSelection(int playerId, out GameObject prefab, out UnityEngine.InputSystem.InputDevice device)
    {
        EnsureInstance();
        Instance._prefabs.TryGetValue(playerId, out prefab);
        Instance._devices.TryGetValue(playerId, out device);
        return prefab != null;
    }

    public static IEnumerable<int> GetPlayerIds()
    {
        EnsureInstance();
        return Instance._prefabs.Keys;
    }

    private static void EnsureInstance()
    {
        if (Instance == null)
        {
            var go = new GameObject("CharacterSelectionState");
            Instance = go.AddComponent<CharacterSelectionState>();
            DontDestroyOnLoad(go);
        }
    }
}
