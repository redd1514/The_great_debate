using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Map Selection Manager with Random Map Selection.
/// FIXED: Multiple instance protection and single map selection
/// Flow: Character Select -> Random Map Selection with Loading Screen -> Gameplay
/// </summary>
public class MapSelectionManager : MonoBehaviour
{
    [Header("Map Data")]
    public MapData[] availableMaps;
    
    [Header("Random Map Selection")]
    [Tooltip("If enabled, randomly selects a map and shows loading screen instead of voting")]
    public bool useRandomMapSelection = true;
    [Range(1f, 10f)]
    public float randomSelectionDelay = 3f;
    [Range(1f, 10f)]
    public float loadingScreenDuration = 4f;
    
    [Header("UI References")]
    public Transform mapGridParent;
    public GameObject mapIconPrefab;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;
    
    [Header("Scene Settings")]
    public string gameplaySceneName = "GameplayScene";
    public string[] mapSceneNames = { "Map1Scene", "Map2Scene", "Map3Scene", "Map4Scene" };
    
    [Header("Canvas Setup")]
    public Canvas mainCanvas;
    public Transform uiContainer;
    
    [Header("Debug")]
    [Tooltip("Enable detailed debug logging")]
    public bool enableDebugLogging = true;
    
    // Singleton protection - IMPROVED
    public static MapSelectionManager Instance { get; private set; }
    private static bool isQuittingApplication = false;
    private static int instanceCreationCount = 0;
    
    // Random selection state
    private bool isRandomSelectionActive = false;
    private float randomSelectionTimer = 0f;
    private int selectedRandomMapIndex = -1;
    private bool showingLoadingScreen = false;
    private bool isInitialized = false;
    private bool hasCompletedSelection = false;
    private bool isDestroyed = false;
    private bool hasStartedTimer = false;
    
    private PlayerCharacterData[] joinedPlayers;
    
    void Awake()
    {
        instanceCreationCount++;
        
        if (enableDebugLogging)
        {
            Debug.Log($"MapSelectionManager Awake() called - Instance #{instanceCreationCount} (ID: {GetInstanceID()}) on GameObject: {gameObject.name}");
        }
        
        // IMPROVED singleton protection - more aggressive
        if (isQuittingApplication)
        {
            if (enableDebugLogging) Debug.Log($"Application quitting, destroying instance {GetInstanceID()}");
            Destroy(gameObject);
            return;
        }
        
        if (Instance != null)
        {
            // Another instance already exists
            if (enableDebugLogging)
            {
                Debug.LogWarning($"MapSelectionManager: Duplicate instance #{instanceCreationCount} detected! Destroying {gameObject.name} (ID: {GetInstanceID()}), keeping {Instance.gameObject.name} (ID: {Instance.GetInstanceID()})");
            }
            
            // Mark this instance as destroyed immediately to prevent any Start() calls
            isDestroyed = true;
            
            // Destroy immediately to prevent any Update() calls
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
            return;
        }
        
        // This is the first/only instance
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scene loads if needed
        
        if (enableDebugLogging)
        {
            Debug.Log($"? MapSelectionManager: Singleton instance #{instanceCreationCount} established on {gameObject.name} (ID: {GetInstanceID()})");
        }
    }
    
    void Start()
    {
        // SAFETY CHECK: Don't proceed if this instance is destroyed or not the singleton
        if (isDestroyed || Instance != this)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"MapSelectionManager: Skipping Start() - instance destroyed or not singleton. Destroyed: {isDestroyed}, IsSingleton: {Instance == this}");
            }
            return;
        }
        
        if (isInitialized) 
        {
            Debug.LogWarning("MapSelectionManager: Already initialized, skipping Start()");
            return;
        }
        
        Debug.Log("=== MapSelectionManager Start (SINGLETON) ===");
        Debug.Log($"GameObject: {gameObject.name}, Instance ID: {GetInstanceID()}");
        Debug.Log($"Total instances created so far: {instanceCreationCount}");
        
        // Verify we're still the only instance
        MapSelectionManager[] allInstances = FindObjectsByType<MapSelectionManager>(FindObjectsSortMode.None);
        if (allInstances.Length > 1)
        {
            Debug.LogError($"CRITICAL: Found {allInstances.Length} MapSelectionManager instances during Start()! This should not happen!");
            for (int i = 0; i < allInstances.Length; i++)
            {
                Debug.LogError($"  Instance {i}: {allInstances[i].gameObject.name} (ID: {allInstances[i].GetInstanceID()}) - IsThis: {allInstances[i] == this}");
            }
        }
        
        // Get player data
        if (GameDataManager.Instance != null)
        {
            joinedPlayers = GameDataManager.Instance.GetSelectedCharacters();
            Debug.Log($"Loaded {joinedPlayers?.Length} player data from GameDataManager");
        }
        else
        {
            Debug.LogWarning("GameDataManager.Instance is null - creating test player data");
        }
        
        // Ensure we have maps
        if (availableMaps == null || availableMaps.Length == 0)
        {
            Debug.LogWarning("No maps assigned - creating test maps");
            CreateTestMaps();
        }
        else
        {
            Debug.Log($"Found {availableMaps.Length} assigned maps");
        }
        
        // Always use random selection (bypass input issues)
        if (useRandomMapSelection)
        {
            // Start with a small delay to ensure everything is set up
            StartCoroutine(StartRandomMapSelectionDelayed());
        }
        else
        {
            Debug.LogWarning("Random map selection is disabled - falling back to old system");
        }
        
        isInitialized = true;
    }
    
    IEnumerator StartRandomMapSelectionDelayed()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure all setup is complete
        StartRandomMapSelection();
    }
    
    void OnDestroy()
    {
        isDestroyed = true;
        
        if (Instance == this)
        {
            Instance = null;
            if (enableDebugLogging)
            {
                Debug.Log("MapSelectionManager: Singleton instance destroyed and cleared");
            }
        }
        else
        {
            if (enableDebugLogging)
            {
                Debug.Log($"MapSelectionManager: Non-singleton instance destroyed (ID: {GetInstanceID()})");
            }
        }
    }
    
    void OnApplicationQuit()
    {
        isQuittingApplication = true;
    }
    
    void StartRandomMapSelection()
    {
        // SAFETY CHECK: Only proceed if we're the singleton and not destroyed
        if (isDestroyed || Instance != this)
        {
            Debug.LogWarning("StartRandomMapSelection: Called on non-singleton or destroyed instance, ignoring");
            return;
        }
        
        // Prevent multiple selections
        if (isRandomSelectionActive)
        {
            Debug.LogWarning("MapSelectionManager: Random selection already active, ignoring duplicate call");
            return;
        }
        
        Debug.Log("=== Starting Random Map Selection (SINGLETON) ===");
        Debug.Log($"Instance: {gameObject.name} (ID: {GetInstanceID()})");
        
        isRandomSelectionActive = true;
        randomSelectionTimer = randomSelectionDelay;
        showingLoadingScreen = false;
        hasCompletedSelection = false;
        hasStartedTimer = false;
        
        // Setup basic UI
        SetupRandomSelectionUI();
        
        // Randomly select ONE map for all players
        selectedRandomMapIndex = Random.Range(0, availableMaps.Length);
        
        string mapName = GetMapName(selectedRandomMapIndex);
        Debug.Log($"=== SINGLE MAP SELECTED FOR ALL PLAYERS (SINGLETON) ===");
        Debug.Log($"Selected map index: {selectedRandomMapIndex}");
        Debug.Log($"Selected map name: {mapName}");
        Debug.Log($"Initial timer set to: {randomSelectionTimer} seconds");
        
        UpdateRandomSelectionUI();
        hasStartedTimer = true;
        
        Debug.Log($"? Random selection setup complete. Timer should start counting down from {randomSelectionTimer}s");
    }
    
    void SetupRandomSelectionUI()
    {
        EnsureEventSystem();
        
        // Find or create UI elements
        if (timerText == null)
        {
            timerText = FindFirstObjectByType<TextMeshProUGUI>();
            Debug.Log($"Found timer text: {timerText != null}");
        }
        
        if (timerText == null)
        {
            Debug.Log("No timer text found - creating basic UI");
            CreateBasicUI();
        }
        else
        {
            Debug.Log("Using existing timer text");
        }
    }
    
    void CreateBasicUI()
    {
        Debug.Log("Creating basic UI from scratch");
        
        // Create canvas if needed
        if (mainCanvas == null)
        {
            CreateMainCanvas();
        }
        
        // Create timer text
        GameObject timerObj = new GameObject("RandomSelectionTimer");
        timerObj.transform.SetParent(mainCanvas.transform, false);
        
        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "Random Selection";
        timerText.fontSize = 48f;
        timerText.color = Color.yellow;
        timerText.alignment = TextAlignmentOptions.Center;
        
        RectTransform timerRect = timerText.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0.8f);
        timerRect.anchorMax = new Vector2(0.5f, 0.8f);
        timerRect.sizeDelta = new Vector2(600f, 100f);
        timerRect.anchoredPosition = Vector2.zero;
        
        // Create status text
        GameObject statusObj = new GameObject("RandomSelectionStatus");
        statusObj.transform.SetParent(mainCanvas.transform, false);
        
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 32f;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.Center;
        
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.4f);
        statusRect.anchorMax = new Vector2(0.5f, 0.4f);
        statusRect.sizeDelta = new Vector2(800f, 200f);
        statusRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("? Basic UI created successfully");
    }
    
    void Update()
    {
        // CRITICAL SAFETY CHECK: Only the singleton instance should process updates
        if (isDestroyed || Instance != this) 
        {
            return;
        }
        
        if (!isInitialized) return;
        
        if (useRandomMapSelection && isRandomSelectionActive && !hasCompletedSelection)
        {
            HandleRandomSelection();
        }
        
        // Allow skip with space/enter
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            SkipTimer();
        }
        
        // Debug every 3 seconds if timer seems stuck
        if (isRandomSelectionActive && hasStartedTimer && Time.frameCount % 180 == 0)
        {
            Debug.Log($"?? Timer Debug: {randomSelectionTimer:F1}s remaining, Loading: {showingLoadingScreen}, Completed: {hasCompletedSelection}, Time.deltaTime: {Time.deltaTime:F3}");
        }
    }
    
    void HandleRandomSelection()
    {
        if (hasCompletedSelection) return; // Prevent multiple executions
        
        // Decrease timer
        float previousTimer = randomSelectionTimer;
        randomSelectionTimer -= Time.deltaTime;
        
        // Debug every time timer crosses a whole second boundary
        if (Mathf.FloorToInt(previousTimer) != Mathf.FloorToInt(randomSelectionTimer) && enableDebugLogging)
        {
            Debug.Log($"[SINGLETON] Timer countdown: {Mathf.CeilToInt(randomSelectionTimer)}s (Loading: {showingLoadingScreen})");
        }
        
        if (randomSelectionTimer <= 0f)
        {
            if (!showingLoadingScreen)
            {
                // Switch to loading phase
                showingLoadingScreen = true;
                randomSelectionTimer = loadingScreenDuration;
                Debug.Log($"[SINGLETON] Selection phase complete - switching to loading phase ({loadingScreenDuration}s)");
                Debug.Log($"Final selected map: {GetMapName(selectedRandomMapIndex)}");
            }
            else
            {
                // Load the map
                hasCompletedSelection = true; // Prevent multiple calls
                Debug.Log("[SINGLETON] Loading phase complete - proceeding to map");
                ProceedToSelectedMap();
                return; // Exit early to prevent further updates
            }
        }
        
        UpdateRandomSelectionUI();
    }
    
    void UpdateRandomSelectionUI()
    {
        if (!isRandomSelectionActive) return;
        
        string mapName = GetMapName(selectedRandomMapIndex);
        
        if (!showingLoadingScreen)
        {
            // Selection phase
            if (timerText != null)
            {
                int timeLeft = Mathf.CeilToInt(randomSelectionTimer);
                timerText.text = $"Random Selection: {timeLeft}s";
            }
            
            if (statusText != null)
            {
                statusText.text = "?? Selecting Random Map...";
            }
        }
        else
        {
            // Loading phase
            if (timerText != null)
            {
                int timeLeft = Mathf.CeilToInt(randomSelectionTimer);
                timerText.text = $"Loading: {timeLeft}s";
            }
            
            if (statusText != null)
            {
                statusText.text = $"?? Selected: {mapName}\nPreparing for battle...";
            }
        }
    }
    
    void SkipTimer()
    {
        if (!isRandomSelectionActive) return;
        
        Debug.Log("[SINGLETON] Timer skipped by player input!");
        randomSelectionTimer = 0f;
    }
    
    void ProceedToSelectedMap()
    {
        if (hasCompletedSelection) return; // Prevent multiple calls
        
        hasCompletedSelection = true;
        isRandomSelectionActive = false;
        
        string mapName = GetMapName(selectedRandomMapIndex);
        string sceneToLoad = GetSceneForMap(selectedRandomMapIndex);
        
        Debug.Log($"=== [SINGLETON] LOADING MAP: {mapName} ===");
        Debug.Log($"Scene to load: '{sceneToLoad}'");
        Debug.Log($"Map index: {selectedRandomMapIndex}");
        Debug.Log($"All players will be redirected to this single map!");
        
        // Validate scene name before loading
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene name is null or empty! Using fallback scene.");
            sceneToLoad = gameplaySceneName;
        }
        
        // Show loading message for a moment
        if (statusText != null)
        {
            statusText.text = $"Loading {mapName}...\nAll players proceeding together!";
        }
        
        // Load the scene after a brief delay to show the message
        StartCoroutine(LoadSceneAfterDelay(sceneToLoad, 1.5f));
    }
    
    IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        Debug.Log($"[SINGLETON] Waiting {delay} seconds before loading scene '{sceneName}'");
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"[SINGLETON] Now loading scene: {sceneName}");
        
        // Try to load the scene
        try
        {
            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene '{sceneName}': {e.Message}");
            Debug.Log($"Attempting to load fallback scene: {gameplaySceneName}");
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
    
    string GetMapName(int mapIndex)
    {
        if (mapIndex >= 0 && mapIndex < availableMaps.Length && availableMaps[mapIndex] != null)
        {
            string name = availableMaps[mapIndex].GetDisplayName();
            return name;
        }
        
        string fallback = $"Map {mapIndex + 1}";
        return fallback;
    }
    
    string GetSceneForMap(int mapIndex)
    {
        string sceneName = "";
        
        // Try to get scene name from MapData first
        if (mapIndex >= 0 && mapIndex < availableMaps.Length && availableMaps[mapIndex] != null)
        {
            sceneName = availableMaps[mapIndex].GetSceneName();
            if (!string.IsNullOrEmpty(sceneName))
            {
                Debug.Log($"GetSceneForMap({mapIndex}) -> '{sceneName}' (from MapData)");
                return sceneName;
            }
        }
        
        // Fall back to array
        if (mapIndex >= 0 && mapIndex < mapSceneNames.Length)
        {
            sceneName = mapSceneNames[mapIndex];
            Debug.Log($"GetSceneForMap({mapIndex}) -> '{sceneName}' (from array)");
            return sceneName;
        }
        
        // Final fallback
        sceneName = gameplaySceneName;
        Debug.Log($"GetSceneForMap({mapIndex}) -> '{sceneName}' (final fallback)");
        return sceneName;
    }
    
    void CreateMainCanvas()
    {
        Debug.Log("Creating main canvas");
        GameObject canvasObj = new GameObject("RandomMapSelectionCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100; // Higher priority
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        Debug.Log("? Main canvas created with higher sort order");
    }
    
    void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            Debug.Log("Creating EventSystem");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
    }
    
    void CreateTestMaps()
    {
        availableMaps = new MapData[4];
        for (int i = 0; i < 4; i++)
        {
            var testMap = ScriptableObject.CreateInstance<MapData>();
            testMap.mapName = $"Random Test Map {i + 1}";
            testMap.sceneName = $"TestMap{i + 1}Scene";
            availableMaps[i] = testMap;
        }
        Debug.Log($"Created {availableMaps.Length} test maps for random selection");
        
        // Log all created test maps
        for (int i = 0; i < availableMaps.Length; i++)
        {
            Debug.Log($"  Test Map {i}: '{availableMaps[i].mapName}' -> '{availableMaps[i].sceneName}'");
        }
    }
    
    // Public methods for testing
    [ContextMenu("Force Random Selection")]
    public void ForceRandomSelection()
    {
        Debug.Log("=== Force Random Selection Called ===");
        
        if (!useRandomMapSelection)
        {
            useRandomMapSelection = true;
            Debug.Log("Enabled random map selection");
        }
        
        // Reset all state
        isRandomSelectionActive = false;
        randomSelectionTimer = 0f;
        selectedRandomMapIndex = -1;
        showingLoadingScreen = false;
        hasCompletedSelection = false;
        hasStartedTimer = false;
        
        StartRandomMapSelection();
    }
    
    [ContextMenu("Skip to Game")]
    public void SkipToGame()
    {
        Debug.Log("=== Skip to Game Called ===");
        randomSelectionTimer = 0f;
    }
    
    [ContextMenu("Debug State")]
    public void DebugState()
    {
        Debug.Log("=== MapSelectionManager Debug State ===");
        Debug.Log($"Is Singleton Instance: {Instance == this}");
        Debug.Log($"GameObject: {gameObject.name} (ID: {GetInstanceID()})");
        Debug.Log($"Total instances created: {instanceCreationCount}");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Destroyed: {isDestroyed}");
        Debug.Log($"Use Random Selection: {useRandomMapSelection}");
        Debug.Log($"Random Selection Active: {isRandomSelectionActive}");
        Debug.Log($"Has Started Timer: {hasStartedTimer}");
        Debug.Log($"Selected Map Index: {selectedRandomMapIndex}");
        Debug.Log($"Showing Loading Screen: {showingLoadingScreen}");
        Debug.Log($"Has Completed Selection: {hasCompletedSelection}");
        Debug.Log($"Timer: {randomSelectionTimer:F2}s");
        Debug.Log($"Selection Delay: {randomSelectionDelay}s");
        Debug.Log($"Loading Duration: {loadingScreenDuration}s");
        Debug.Log($"Available Maps: {availableMaps?.Length ?? 0}");
        Debug.Log($"Timer Text: {(timerText != null ? "Found" : "NULL")}");
        Debug.Log($"Status Text: {(statusText != null ? "Found" : "NULL")}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"Time.deltaTime: {Time.deltaTime:F3}");
        
        if (selectedRandomMapIndex >= 0 && availableMaps != null && selectedRandomMapIndex < availableMaps.Length)
        {
            string mapName = GetMapName(selectedRandomMapIndex);
            string sceneName = GetSceneForMap(selectedRandomMapIndex);
            Debug.Log($"Selected Map: '{mapName}' -> Scene: '{sceneName}'");
        }
        
        // Check for multiple instances
        MapSelectionManager[] allInstances = FindObjectsByType<MapSelectionManager>(FindObjectsSortMode.None);
        Debug.Log($"Total MapSelectionManager instances in scene: {allInstances.Length}");
        for (int i = 0; i < allInstances.Length; i++)
        {
            Debug.Log($"  Instance {i}: {allInstances[i].gameObject.name} (ID: {allInstances[i].GetInstanceID()}) - Active: {allInstances[i].gameObject.activeInHierarchy}");
        }
    }
    
    [ContextMenu("Find All Instances")]
    public void FindAllInstances()
    {
        MapSelectionManager[] allInstances = FindObjectsByType<MapSelectionManager>(FindObjectsSortMode.None);
        Debug.Log($"=== Found {allInstances.Length} MapSelectionManager instances ===");
        
        for (int i = 0; i < allInstances.Length; i++)
        {
            MapSelectionManager instance = allInstances[i];
            bool isSingleton = Instance == instance;
            bool isActive = instance.gameObject.activeInHierarchy;
            bool isInitialized = instance.isInitialized;
            
            Debug.Log($"Instance {i}: {instance.gameObject.name}");
            Debug.Log($"  - Instance ID: {instance.GetInstanceID()}");
            Debug.Log($"  - Is Singleton: {isSingleton}");
            Debug.Log($"  - Active: {isActive}");
            Debug.Log($"  - Initialized: {isInitialized}");
            Debug.Log($"  - Selection Active: {instance.isRandomSelectionActive}");
            Debug.Log($"  - Selected Map: {instance.selectedRandomMapIndex}");
        }
    }
    
    [ContextMenu("Force Restart With Debug")]
    public void ForceRestartWithDebug()
    {
        enableDebugLogging = true;
        Debug.Log("?? === FORCE RESTART WITH DEBUG ENABLED ===");
        
        // Stop current selection
        isRandomSelectionActive = false;
        hasCompletedSelection = true;
        
        // Wait a moment and restart
        StartCoroutine(RestartAfterDelay());
    }
    
    IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        ForceRandomSelection();
    }
}