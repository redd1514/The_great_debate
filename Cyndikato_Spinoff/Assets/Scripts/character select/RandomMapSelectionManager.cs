using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Simple Random Map Selection Manager - bypasses complex voting system.
/// Flow: Character Select -> Random Map Selection with Loading Screen -> Gameplay
/// </summary>
public class RandomMapSelectionManager : MonoBehaviour
{
    [Header("Map Data")]
    public MapData[] availableMaps;
    
    [Header("Random Selection Settings")]
    [Range(1f, 5f)]
    public float selectionDelay = 2.5f; // Time to show "Selecting Random Map..." message
    [Range(1f, 5f)]
    public float loadingDuration = 3f; // Time to show selected map before loading
    
    [Header("Scene Settings")]
    public string gameplaySceneName = "GameplayScene"; // Fallback scene name
    public string[] mapSceneNames = { "Map1Scene", "Map2Scene", "Map3Scene", "Map4Scene" };
    
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI instructionText;
    
    // State
    private int selectedMapIndex = -1;
    private bool isSelecting = true;
    private float timer;
    private PlayerCharacterData[] joinedPlayers;
    
    void Start()
    {
        Debug.Log("=== Random Map Selection Started ===");
        
        // Get player data from character selection
        if (GameDataManager.Instance != null)
        {
            joinedPlayers = GameDataManager.Instance.GetSelectedCharacters();
            LogPlayerData();
        }
        else
        {
            Debug.LogWarning("No GameDataManager found - creating test player data");
            CreateTestPlayerData();
        }
        
        // Ensure we have maps
        if (availableMaps == null || availableMaps.Length == 0)
        {
            CreateTestMaps();
        }
        
        // Setup UI
        SetupUI();
        
        // Start selection process
        StartRandomSelection();
    }
    
    void SetupUI()
    {
        // Find UI elements if not assigned
        if (titleText == null) titleText = GameObject.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (statusText == null) statusText = GameObject.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
        if (timerText == null) timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        if (instructionText == null) instructionText = GameObject.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();
        
        // Create basic UI if none exists
        if (titleText == null) CreateBasicUI();
        
        // Set initial text
        if (titleText != null) titleText.text = "MAP SELECTION";
        if (instructionText != null) instructionText.text = "Press SPACE or ENTER to skip timer";
    }
    
    void CreateBasicUI()
    {
        // Create a simple Canvas if needed
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("RandomSelectionCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create title
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvas.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "MAP SELECTION";
        titleText.fontSize = 48f;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
        titleRect.anchorMax = new Vector2(0.5f, 0.8f);
        titleRect.sizeDelta = new Vector2(600f, 100f);
        titleRect.anchoredPosition = Vector2.zero;
        
        // Create status
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(canvas.transform, false);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 32f;
        statusText.color = Color.yellow;
        statusText.alignment = TextAlignmentOptions.Center;
        
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.6f);
        statusRect.anchorMax = new Vector2(0.5f, 0.6f);
        statusRect.sizeDelta = new Vector2(800f, 100f);
        statusRect.anchoredPosition = Vector2.zero;
        
        // Create timer
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(canvas.transform, false);
        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 24f;
        timerText.color = Color.white;
        timerText.alignment = TextAlignmentOptions.Center;
        
        RectTransform timerRect = timerText.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0.4f);
        timerRect.anchorMax = new Vector2(0.5f, 0.4f);
        timerRect.sizeDelta = new Vector2(400f, 50f);
        timerRect.anchoredPosition = Vector2.zero;
        
        // Create instructions
        GameObject instrObj = new GameObject("InstructionText");
        instrObj.transform.SetParent(canvas.transform, false);
        instructionText = instrObj.AddComponent<TextMeshProUGUI>();
        instructionText.text = "Press SPACE or ENTER to skip timer";
        instructionText.fontSize = 18f;
        instructionText.color = Color.gray;
        instructionText.alignment = TextAlignmentOptions.Center;
        
        RectTransform instrRect = instructionText.GetComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0.5f, 0.1f);
        instrRect.anchorMax = new Vector2(0.5f, 0.1f);
        instrRect.sizeDelta = new Vector2(500f, 50f);
        instrRect.anchoredPosition = Vector2.zero;
    }
    
    void StartRandomSelection()
    {
        // Randomly select a map
        selectedMapIndex = Random.Range(0, availableMaps.Length);
        timer = selectionDelay;
        isSelecting = true;
        
        string mapName = GetMapName(selectedMapIndex);
        Debug.Log($"Randomly selected map {selectedMapIndex}: {mapName}");
        
        UpdateUI();
    }
    
    void Update()
    {
        // Handle skip input
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            SkipTimer();
        }
        
        // Update timer
        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            if (isSelecting)
            {
                // Switch to loading phase
                isSelecting = false;
                timer = loadingDuration;
                Debug.Log("Selection complete - starting loading phase");
            }
            else
            {
                // Load the selected map
                LoadSelectedMap();
            }
        }
        
        UpdateUI();
    }
    
    void UpdateUI()
    {
        string mapName = GetMapName(selectedMapIndex);
        
        if (isSelecting)
        {
            if (statusText != null) statusText.text = "?? Selecting Random Map...";
            if (timerText != null) timerText.text = $"Selection: {Mathf.CeilToInt(timer)}s";
        }
        else
        {
            if (statusText != null) statusText.text = $"??? Selected: {mapName}";
            if (timerText != null) timerText.text = $"Loading: {Mathf.CeilToInt(timer)}s";
        }
    }
    
    void SkipTimer()
    {
        Debug.Log("Timer skipped by player input");
        timer = 0f;
    }
    
    void LoadSelectedMap()
    {
        string mapName = GetMapName(selectedMapIndex);
        string sceneToLoad = GetSceneForMap(selectedMapIndex);
        
        Debug.Log($"=== LOADING MAP: {mapName} ===");
        Debug.Log($"Scene: {sceneToLoad}");
        
        // Log player data one more time before scene load
        LogPlayerData();
        
        SceneManager.LoadScene(sceneToLoad);
    }
    
    string GetMapName(int mapIndex)
    {
        if (mapIndex >= 0 && mapIndex < availableMaps.Length && availableMaps[mapIndex] != null)
        {
            return availableMaps[mapIndex].GetDisplayName();
        }
        return $"Map {mapIndex + 1}";
    }
    
    string GetSceneForMap(int mapIndex)
    {
        // Try to get scene name from MapData first
        if (mapIndex >= 0 && mapIndex < availableMaps.Length && availableMaps[mapIndex] != null)
        {
            string mapSceneName = availableMaps[mapIndex].GetSceneName();
            if (!string.IsNullOrEmpty(mapSceneName))
                return mapSceneName;
        }
        
        // Fall back to mapSceneNames array
        if (mapIndex >= 0 && mapIndex < mapSceneNames.Length)
            return mapSceneNames[mapIndex];
        
        // Final fallback
        return gameplaySceneName;
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
        Debug.Log("Created 4 test maps for random selection");
    }
    
    void CreateTestPlayerData()
    {
        joinedPlayers = new PlayerCharacterData[1];
        joinedPlayers[0] = new PlayerCharacterData(0);
        joinedPlayers[0].isJoined = true;
        joinedPlayers[0].hasLockedCharacter = true;
        joinedPlayers[0].selectedCharacterIndex = 0;
        Debug.Log("Created test player data for random selection");
    }
    
    void LogPlayerData()
    {
        Debug.Log("=== Player Data for Random Map Selection ===");
        if (joinedPlayers != null)
        {
            for (int i = 0; i < joinedPlayers.Length; i++)
            {
                if (joinedPlayers[i] != null && joinedPlayers[i].isJoined)
                {
                    string charName = joinedPlayers[i].lockedCharacter != null ? 
                        joinedPlayers[i].lockedCharacter.characterName : "Unknown";
                    Debug.Log($"Player {i + 1}: {charName} (Ready for battle!)");
                }
            }
        }
    }
    
    // Context menu methods for testing
    [ContextMenu("Skip to Loading")]
    public void SkipToLoading()
    {
        isSelecting = false;
        timer = 1f;
    }
    
    [ContextMenu("Skip to Game")]
    public void SkipToGame()
    {
        timer = 0f;
    }
    
    [ContextMenu("Reselect Random Map")]
    public void ReselectMap()
    {
        StartRandomSelection();
    }
}