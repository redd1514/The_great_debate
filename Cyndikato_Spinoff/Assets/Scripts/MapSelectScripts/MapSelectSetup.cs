using UnityEngine;

/// <summary>
/// Complete Map Setup Helper for MapSelect script
/// This automatically configures your MapSelect with proper map data
/// </summary>
public class MapSelectSetup : MonoBehaviour
{
    [Header("Auto-Configure MapSelect")]
    public MapSelect mapSelectScript;
    
    [Header("Map Configuration")]
    public string[] mapNames = { "Canyon Arena", "Forest Battle", "Urban Warfare", "Space Station" };
    public string[] sceneNames = { "Map1", "Map2", "Map3", "Map4" };
    public Color[] mapColors = { 
        new Color(1f, 0.7f, 0.4f), // Canyon - Orange
        new Color(0.4f, 0.8f, 0.4f), // Forest - Green  
        new Color(0.6f, 0.6f, 1f),  // Urban - Blue
        new Color(0.8f, 0.8f, 0.8f) // Space - Gray
    };
    
    [Header("UI Settings")]
    public Vector2 mapImageSize = new Vector2(200, 150);
    public Vector2 gridSpacing = new Vector2(220, 170);
    public Vector2 gridStartPosition = new Vector2(-110, 85);
    
    void Start()
    {
        if (mapSelectScript == null)
            mapSelectScript = GetComponent<MapSelect>();
            
        SetupMapData();
        CreateMapUI();
    }
    
    [ContextMenu("Setup Complete Map System")]
    public void SetupMapData()
    {
        if (mapSelectScript == null)
        {
            Debug.LogError("MapSelect script not found!");
            return;
        }
        
        Debug.Log("=== SETTING UP MAP DATA ===");
        
        // Get maps array using reflection
        var mapsField = typeof(MapSelect).GetField("maps", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (mapsField == null)
        {
            Debug.LogError("Could not access maps field!");
            return;
        }
        
        var maps = (MapSelect.MapOption[])mapsField.GetValue(mapSelectScript);
        if (maps == null || maps.Length != 4)
        {
            maps = new MapSelect.MapOption[4];
            for (int i = 0; i < 4; i++)
                maps[i] = new MapSelect.MapOption();
        }
        
        // Configure each map
        for (int i = 0; i < 4; i++)
        {
            if (i < mapNames.Length && i < sceneNames.Length)
            {
                maps[i].mapName = mapNames[i];
                maps[i].sceneName = sceneNames[i];
                Debug.Log($"Configured Map {i}: {maps[i].mapName} -> {maps[i].sceneName}");
            }
        }
        
        // Update the maps array
        mapsField.SetValue(mapSelectScript, maps);
        
        Debug.Log("Map data setup complete!");
    }
    
    [ContextMenu("Create Map UI")]
    public void CreateMapUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene!");
            return;
        }
        
        Debug.Log("=== CREATING MAP UI ===");
        
        // Create UI container
        GameObject uiContainer = canvas.transform.Find("MapSelectionUI")?.gameObject;
        if (uiContainer == null)
        {
            uiContainer = new GameObject("MapSelectionUI");
            uiContainer.transform.SetParent(canvas.transform, false);
            
            RectTransform containerRect = uiContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
        }
        
        // Get maps array
        var mapsField = typeof(MapSelect).GetField("maps", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var maps = (MapSelect.MapOption[])mapsField.GetValue(mapSelectScript);
        
        // Create map images
        for (int i = 0; i < 4; i++)
        {
            CreateMapImage(i, uiContainer.transform, maps);
        }
        
        Debug.Log("Map UI creation complete!");
    }
    
    void CreateMapImage(int index, Transform parent, MapSelect.MapOption[] maps)
    {
        // Calculate position in 2x2 grid
        int row = index / 2;
        int col = index % 2;
        Vector2 position = gridStartPosition + new Vector2(col * gridSpacing.x, -row * gridSpacing.y);
        
        // Create map image GameObject
        GameObject mapImageObj = new GameObject($"MapImage_{index}_{mapNames[index]}");
        mapImageObj.transform.SetParent(parent, false);
        
        RectTransform imageRect = mapImageObj.AddComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = position;
        imageRect.sizeDelta = mapImageSize;
        
        // Add Image component with map color
        UnityEngine.UI.Image mapImage = mapImageObj.AddComponent<UnityEngine.UI.Image>();
        mapImage.color = index < mapColors.Length ? mapColors[index] : Color.gray;
        
        // Create map name label
        GameObject labelObj = new GameObject("MapLabel");
        labelObj.transform.SetParent(mapImageObj.transform, false);
        
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = mapImageSize;
        
        TMPro.TextMeshProUGUI label = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        label.text = index < mapNames.Length ? mapNames[index] : $"Map {index + 1}";
        label.fontSize = 18;
        label.color = Color.white;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.fontStyle = TMPro.FontStyles.Bold;
        
        // Add outline
        var outline = labelObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);
        
        // Assign to MapSelect
        if (maps != null && index < maps.Length)
        {
            maps[index].mapImage = mapImage;
            Debug.Log($"Created UI for map {index}: {mapNames[index]}");
        }
    }
    
    [ContextMenu("Test Map Navigation")]
    public void TestMapNavigation()
    {
        Debug.Log("=== TESTING MAP NAVIGATION ===");
        Debug.Log("Controls:");
        Debug.Log("- D-Pad/Arrow Keys: Navigate between maps");
        Debug.Log("- Left Stick: Navigate between maps");
        Debug.Log("- Wait for timer or test navigation manually");
        
        if (mapSelectScript != null)
        {
            var mapsField = typeof(MapSelect).GetField("maps", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var maps = (MapSelect.MapOption[])mapsField.GetValue(mapSelectScript);
            
            if (maps != null)
            {
                Debug.Log("Available maps:");
                for (int i = 0; i < maps.Length; i++)
                {
                    if (maps[i] != null)
                    {
                        string mapInfo = $"  {i}: {maps[i].mapName}";
                        if (maps[i].mapImage != null) mapInfo += " ? (UI Ready)";
                        else mapInfo += " ? (No UI)";
                        Debug.Log(mapInfo);
                    }
                }
            }
        }
    }
    
    [ContextMenu("Complete Setup")]
    public void CompleteSetup()
    {
        SetupMapData();
        CreateMapUI();
        TestMapNavigation();
        
        Debug.Log("=== COMPLETE SETUP FINISHED ===");
        Debug.Log("Your map selection should now be fully functional!");
        Debug.Log("Test the flow: Character Select ? Map Select ? Gameplay");
    }
}