using UnityEngine;

/// <summary>
/// Helper script to automatically create spawn points for the current map
/// Attach this to any GameObject and run to create properly positioned spawn points
/// </summary>
public class SpawnPointCreator : MonoBehaviour
{
    [Header("Spawn Point Configuration")]
    public int numberOfSpawnPoints = 4;
    public float spawnHeight = 2f; // Height above the platform
    public float horizontalSpacing = 3f; // Distance between spawn points
    public Vector3 centerPosition = Vector3.zero; // Center point for spawn arrangement
    
    [Header("Auto-Detection")]
    public bool autoDetectPlatform = true;
    public string platformTag = "Platform"; // Tag to search for main platform
    
    [Header("Created Spawn Points")]
    public Transform[] createdSpawnPoints;
    
    [ContextMenu("Create Spawn Points")]
    public void CreateSpawnPoints()
    {
        // Clear existing spawn points
        ClearExistingSpawnPoints();
        
        // Auto-detect center position if enabled
        if (autoDetectPlatform)
        {
            DetectPlatformCenter();
        }
        
        // Create new spawn points
        createdSpawnPoints = new Transform[numberOfSpawnPoints];
        
        for (int i = 0; i < numberOfSpawnPoints; i++)
        {
            // Calculate position
            float xOffset = (i - (numberOfSpawnPoints - 1) / 2f) * horizontalSpacing;
            Vector3 spawnPosition = centerPosition + new Vector3(xOffset, spawnHeight, 0);
            
            // Create GameObject
            GameObject spawnPoint = new GameObject($"SpawnPoint{i + 1}");
            spawnPoint.transform.position = spawnPosition;
            
            // Add visual indicator (optional)
            CreateVisualIndicator(spawnPoint);
            
            createdSpawnPoints[i] = spawnPoint.transform;
            
            Debug.Log($"Created {spawnPoint.name} at position {spawnPosition}");
        }
        
        Debug.Log($"Successfully created {numberOfSpawnPoints} spawn points!");
        Debug.Log("Now assign these to your PlayerSpawner's Map Spawn Points array.");
    }
    
    [ContextMenu("Auto-Assign to PlayerSpawner")]
    public void AssignToPlayerSpawner()
    {
        // Find PlayerSpawner in scene
        PlayerSpawner playerSpawner = FindObjectOfType<PlayerSpawner>();
        
        if (playerSpawner == null)
        {
            Debug.LogError("No PlayerSpawner found in scene!");
            return;
        }
        
        if (createdSpawnPoints == null || createdSpawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points created yet! Run 'Create Spawn Points' first.");
            return;
        }
        
        // Use reflection to set the mapSpawnPoints field
        var field = typeof(PlayerSpawner).GetField("mapSpawnPoints");
        if (field != null)
        {
            field.SetValue(playerSpawner, createdSpawnPoints);
            Debug.Log($"Successfully assigned {createdSpawnPoints.Length} spawn points to PlayerSpawner!");
        }
        else
        {
            Debug.LogError("Could not find mapSpawnPoints field in PlayerSpawner!");
        }
    }
    
    void DetectPlatformCenter()
    {
        // Try to find the main platform
        GameObject[] platforms = GameObject.FindGameObjectsWithTag(platformTag);
        
        if (platforms.Length == 0)
        {
            // Fallback: look for objects with "Platform" in the name
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.ToLower().Contains("platform") && obj.GetComponent<Collider2D>() != null)
                {
                    centerPosition = obj.transform.position + Vector3.up * spawnHeight;
                    Debug.Log($"Auto-detected platform center: {centerPosition}");
                    return;
                }
            }
            
            Debug.LogWarning("Could not auto-detect platform. Using center position (0,0,0).");
            centerPosition = Vector3.zero;
        }
        else
        {
            // Use the first platform found
            centerPosition = platforms[0].transform.position + Vector3.up * spawnHeight;
            Debug.Log($"Auto-detected platform center: {centerPosition}");
        }
    }
    
    void CreateVisualIndicator(GameObject spawnPoint)
    {
        // Create a simple visual indicator for the spawn point
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "SpawnIndicator";
        indicator.transform.SetParent(spawnPoint.transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
        
        // Make it semi-transparent green
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0, 1, 0, 0.5f);
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }
        
        // Remove the collider (we don't need it for visual indication)
        Collider collider = indicator.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
    }
    
    [ContextMenu("Clear Spawn Points")]
    public void ClearExistingSpawnPoints()
    {
        if (createdSpawnPoints != null)
        {
            foreach (var spawnPoint in createdSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    DestroyImmediate(spawnPoint.gameObject);
                }
            }
        }
        
        createdSpawnPoints = null;
        Debug.Log("Cleared existing spawn points.");
    }
    
    [ContextMenu("Remove Visual Indicators")]
    public void RemoveVisualIndicators()
    {
        if (createdSpawnPoints == null) return;
        
        foreach (var spawnPoint in createdSpawnPoints)
        {
            if (spawnPoint != null)
            {
                Transform indicator = spawnPoint.Find("SpawnIndicator");
                if (indicator != null)
                {
                    DestroyImmediate(indicator.gameObject);
                    Debug.Log($"Removed visual indicator from {spawnPoint.name}");
                }
            }
        }
    }
}