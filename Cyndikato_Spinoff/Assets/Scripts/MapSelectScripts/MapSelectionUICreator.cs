using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to automatically create map selection UI when images aren't assigned
/// Attach this to any GameObject in your Map Selection scene and it will create the UI
/// </summary>
public class MapSelectionUICreator : MonoBehaviour
{
    [Header("Auto-Create Settings")]
    public int numberOfMaps = 4;
    public Vector2 gridSpacing = new Vector2(220, 220);
    public Vector2 mapImageSize = new Vector2(200, 150);
    public Color[] mapColors = new Color[] { Color.blue, Color.green, Color.red, Color.yellow };
    
    [Header("Layout")]
    public int mapsPerRow = 2;
    public Vector2 startPosition = new Vector2(-110, 75);
    
    [Header("References")]
    public Canvas targetCanvas;
    public MapSelect mapSelectScript;
    
    [ContextMenu("Create Map Selection UI")]
    public void CreateMapSelectionUI()
    {
        // Find components if not assigned
        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();
        if (mapSelectScript == null)
            mapSelectScript = FindObjectOfType<MapSelect>();
            
        if (targetCanvas == null || mapSelectScript == null)
        {
            Debug.LogError("MapSelectionUICreator: Missing Canvas or MapSelect script!");
            return;
        }
        
        // Get the maps array using reflection
        var mapsField = typeof(MapSelect).GetField("maps", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (mapsField == null)
        {
            Debug.LogError("MapSelectionUICreator: Could not access maps field!");
            return;
        }
        
        var maps = (MapSelect.MapOption[])mapsField.GetValue(mapSelectScript);
        if (maps == null)
        {
            Debug.LogError("MapSelectionUICreator: Maps array is null!");
            return;
        }
        
        Debug.Log($"Creating UI for {numberOfMaps} maps...");
        
        // Create UI container
        GameObject uiContainer = new GameObject("MapSelectionContainer");
        uiContainer.transform.SetParent(targetCanvas.transform, false);
        
        RectTransform containerRect = uiContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        
        // Create map images
        for (int i = 0; i < numberOfMaps && i < maps.Length; i++)
        {
            if (maps[i] == null) continue;
            
            // Calculate position in grid
            int row = i / mapsPerRow;
            int col = i % mapsPerRow;
            Vector2 position = startPosition + new Vector2(col * gridSpacing.x, -row * gridSpacing.y);
            
            // Create map image GameObject
            GameObject mapImageObj = new GameObject($"MapImage_{i}_{maps[i].mapName}");
            mapImageObj.transform.SetParent(uiContainer.transform, false);
            
            RectTransform imageRect = mapImageObj.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = position;
            imageRect.sizeDelta = mapImageSize;
            
            // Add Image component
            Image mapImage = mapImageObj.AddComponent<Image>();
            Color mapColor = i < mapColors.Length ? mapColors[i] : Color.gray;
            mapImage.color = mapColor;
            
            // Create map label
            GameObject labelObj = new GameObject("MapLabel");
            labelObj.transform.SetParent(mapImageObj.transform, false);
            
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = mapImageSize;
            
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = !string.IsNullOrEmpty(maps[i].mapName) ? maps[i].mapName : $"Map {i + 1}";
            label.fontSize = 24;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            
            // Add outline for visibility
            var outline = labelObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
            
            // Assign to MapSelect script
            maps[i].mapImage = mapImage;
            
            Debug.Log($"Created map UI for: {maps[i].mapName} at position {position}");
        }
        
        // Update the maps array back to the script
        mapsField.SetValue(mapSelectScript, maps);
        
        Debug.Log("Map Selection UI created successfully! You can now navigate the maps.");
    }
    
    [ContextMenu("Clear Existing Map UI")]
    public void ClearExistingMapUI()
    {
        // Find and destroy existing map container
        Transform existing = targetCanvas?.transform.Find("MapSelectionContainer");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
            Debug.Log("Cleared existing map selection UI");
        }
    }
    
    void Start()
    {
        // Auto-create if no map images are assigned
        if (mapSelectScript != null)
        {
            var mapsField = typeof(MapSelect).GetField("maps", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (mapsField != null)
            {
                var maps = (MapSelect.MapOption[])mapsField.GetValue(mapSelectScript);
                if (maps != null && maps.Length > 0)
                {
                    bool hasImages = false;
                    foreach (var map in maps)
                    {
                        if (map?.mapImage != null)
                        {
                            hasImages = true;
                            break;
                        }
                    }
                    
                    if (!hasImages)
                    {
                        Debug.Log("No map images found, auto-creating UI...");
                        CreateMapSelectionUI();
                    }
                }
            }
        }
    }
}