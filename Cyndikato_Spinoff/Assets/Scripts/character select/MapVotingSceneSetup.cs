using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to automatically setup a Map Voting scene.
/// Attach this to an empty GameObject and click "Setup Voting Scene" in the Inspector.
/// This creates all necessary UI elements and configures the MapVotingManager.
/// </summary>
public class MapVotingSceneSetup : MonoBehaviour
{
    [Header("Scene Setup")]
    [Tooltip("Map data assets to use for voting (need 4)")]
    public MapData[] mapDataAssets;
    
    [Header("Options")]
    [Tooltip("Voting duration in seconds")]
    [Range(5f, 60f)]
    public float votingDuration = 15f;
    
    [Tooltip("Allow players to change votes")]
    public bool allowVoteChanges = true;
    
    [Tooltip("Reference resolution for UI scaling")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    
    [Header("Generated Objects (Auto-filled)")]
    public Canvas mainCanvas;
    public GameObject votingManagerObject;
    public Transform mapGridParent;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;
    
    [ContextMenu("Setup Voting Scene")]
    public void SetupVotingScene()
    {
        Debug.Log("=== Setting up Map Voting Scene ===");
        
        // Validate map data
        if (mapDataAssets == null || mapDataAssets.Length < 4)
        {
            Debug.LogError("MapVotingSceneSetup: Need at least 4 MapData assets assigned!");
            return;
        }
        
        // Create Canvas
        CreateMainCanvas();
        
        // Create UI elements
        CreateMapGrid();
        CreateTimerUI();
        CreateStatusUI();
        
        // Create and configure MapVotingManager
        CreateVotingManager();
        
        Debug.Log("✅ Map Voting Scene setup complete!");
        Debug.Log("Next steps:");
        Debug.Log("1. Assign your MapData assets to the MapVotingManager");
        Debug.Log("2. Configure map scene names");
        Debug.Log("3. Test in Play mode");
    }
    
    void CreateMainCanvas()
    {
        // Check if canvas already exists
        mainCanvas = FindFirstObjectByType<Canvas>();
        
        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("MapVotingCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("✓ Created main canvas");
        }
        else
        {
            Debug.Log("✓ Using existing canvas");
        }
        
        // Ensure EventSystem exists
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("✓ Created EventSystem");
        }
    }
    
    void CreateMapGrid()
    {
        GameObject gridObj = new GameObject("MapGridParent");
        gridObj.transform.SetParent(mainCanvas.transform, false);
        
        mapGridParent = gridObj.transform;
        
        // Add Grid Layout Group
        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(400, 400);
        grid.spacing = new Vector2(40, 40);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;
        
        // Position in center
        RectTransform rect = gridObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -50);
        rect.sizeDelta = new Vector2(880, 880);
        
        Debug.Log("✓ Created map grid (2x2 layout)");
    }
    
    void CreateTimerUI()
    {
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(mainCanvas.transform, false);
        
        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "15";
        timerText.fontSize = 72;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        
        // Add outline
        timerText.outlineWidth = 0.2f;
        timerText.outlineColor = Color.black;
        
        RectTransform rect = timerObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.9f);
        rect.anchorMax = new Vector2(0.5f, 0.9f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(200, 100);
        
        Debug.Log("✓ Created timer display");
    }
    
    void CreateStatusUI()
    {
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(mainCanvas.transform, false);
        
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Vote for your favorite map!";
        statusText.fontSize = 48;
        statusText.fontStyle = FontStyles.Bold;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        
        // Add outline
        statusText.outlineWidth = 0.2f;
        statusText.outlineColor = Color.black;
        
        RectTransform rect = statusObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.05f);
        rect.anchorMax = new Vector2(0.5f, 0.05f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1400, 100);
        
        Debug.Log("✓ Created status text");
    }
    
    void CreateVotingManager()
    {
        // Check if already exists
        MapVotingManager existing = FindFirstObjectByType<MapVotingManager>();
        
        if (existing != null)
        {
            Debug.LogWarning("MapVotingManager already exists in scene. Configuring existing one...");
            votingManagerObject = existing.gameObject;
        }
        else
        {
            votingManagerObject = new GameObject("MapVotingManager");
            existing = votingManagerObject.AddComponent<MapVotingManager>();
            Debug.Log("✓ Created MapVotingManager");
        }
        
        // Configure the manager
        existing.availableMaps = mapDataAssets;
        existing.mapGridParent = mapGridParent;
        existing.timerText = timerText;
        existing.statusText = statusText;
        existing.votingDuration = votingDuration;
        existing.allowVoteChanges = allowVoteChanges;
        existing.enableDebugLogging = true;
        
        Debug.Log("✓ Configured MapVotingManager");
    }
    
    [ContextMenu("Clear Generated Objects")]
    public void ClearGeneratedObjects()
    {
        Debug.Log("=== Clearing generated objects ===");
        
        if (votingManagerObject != null)
        {
            DestroyImmediate(votingManagerObject);
            Debug.Log("✓ Removed MapVotingManager");
        }
        
        if (mapGridParent != null)
        {
            DestroyImmediate(mapGridParent.gameObject);
            Debug.Log("✓ Removed MapGridParent");
        }
        
        if (timerText != null)
        {
            DestroyImmediate(timerText.gameObject);
            Debug.Log("✓ Removed TimerText");
        }
        
        if (statusText != null)
        {
            DestroyImmediate(statusText.gameObject);
            Debug.Log("✓ Removed StatusText");
        }
        
        // Clear references
        mapGridParent = null;
        timerText = null;
        statusText = null;
        votingManagerObject = null;
        
        Debug.Log("✅ Cleanup complete");
    }
    
    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        Debug.Log("=== Validating Map Voting Setup ===");
        
        bool isValid = true;
        
        // Check MapVotingManager
        MapVotingManager manager = FindFirstObjectByType<MapVotingManager>();
        if (manager == null)
        {
            Debug.LogError("✗ MapVotingManager not found in scene!");
            isValid = false;
        }
        else
        {
            Debug.Log("✓ MapVotingManager found");
            
            // Check configuration
            if (manager.availableMaps == null || manager.availableMaps.Length < 4)
            {
                Debug.LogError("✗ Need at least 4 maps assigned to MapVotingManager!");
                isValid = false;
            }
            else
            {
                Debug.Log($"✓ {manager.availableMaps.Length} maps assigned");
            }
            
            if (manager.mapGridParent == null)
            {
                Debug.LogError("✗ MapGridParent not assigned!");
                isValid = false;
            }
            else
            {
                Debug.Log("✓ MapGridParent assigned");
            }
            
            if (manager.timerText == null)
            {
                Debug.LogWarning("⚠ TimerText not assigned (will be created at runtime)");
            }
            else
            {
                Debug.Log("✓ TimerText assigned");
            }
            
            if (manager.statusText == null)
            {
                Debug.LogWarning("⚠ StatusText not assigned (optional)");
            }
            else
            {
                Debug.Log("✓ StatusText assigned");
            }
        }
        
        // Check Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("✗ No Canvas found in scene!");
            isValid = false;
        }
        else
        {
            Debug.Log("✓ Canvas found");
        }
        
        // Check EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            Debug.LogError("✗ No EventSystem found in scene!");
            isValid = false;
        }
        else
        {
            Debug.Log("✓ EventSystem found");
        }
        
        // Check GameDataManager
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("⚠ GameDataManager not found. This is OK if testing standalone, but required for full game flow.");
        }
        else
        {
            Debug.Log("✓ GameDataManager found");
        }
        
        if (isValid)
        {
            Debug.Log("✅ Setup validation passed! Scene is ready to use.");
        }
        else
        {
            Debug.LogError("❌ Setup validation failed. Please fix the issues above.");
        }
    }
}
