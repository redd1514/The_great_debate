using UnityEngine;

public class MapSelectionInputManager : MonoBehaviour
{
    private MapSelect mapSelectScript;

    void Start()
    {
        Debug.Log("MapSelectionInputManager: START method called! ");
        
        // Look for MapSelect script instead of MapSelectionManager
        mapSelectScript = FindObjectOfType<MapSelect>();

        if (mapSelectScript == null)
        {
            Debug.LogError("MapSelectionInputManager: MapSelect script not found!");
        }
        else
        {
            Debug.Log("MapSelectionInputManager: Connected to MapSelect successfully");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("MapSelectionInputManager: Update() is working - Tab pressed!");
        }

        if (mapSelectScript == null) return;

        // Add any additional input handling here if needed
    }
}