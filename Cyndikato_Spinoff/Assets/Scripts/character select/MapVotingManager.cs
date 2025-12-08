using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Map Voting Manager - Handles multiplayer voting system for map selection
/// Features:
/// - 4 map voting with individual player inputs
/// - Player-specific color indicators (Tekken 8 style gradients)
/// - Vote counting and tie-breaking
/// - Countdown timer with skip functionality
/// - Persistent player data through scenes
/// </summary>
public class MapVotingManager : MonoBehaviour
{
    [Header("Map Data")]
    public MapData[] availableMaps;
    
    [Header("UI References")]
    public Transform mapGridParent;
    public GameObject mapIconPrefab;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;
    public Transform selectionIndicatorContainer;
    
    [Header("Vote Counter UI")]
    public GameObject voteCounterPrefab;
    private TextMeshProUGUI[] voteCounterTexts = new TextMeshProUGUI[4];
    
    [Header("Voting Settings")]
    [Range(5f, 60f)]
    public float votingDuration = 15f;
    public bool allowVoteChanges = true;
    [Tooltip("If true, automatically end voting when all players have voted")]
    public bool autoCompleteWhenAllVoted = false;
    
    [Header("Scene Settings")]
    public string gameplaySceneName = "GameplayScene";
    public string[] mapSceneNames = { "Map1Scene", "Map2Scene", "Map3Scene", "Map4Scene" };
    
    [Header("Debug")]
    public bool enableDebugLogging = true;
    
    // Singleton
    public static MapVotingManager Instance { get; private set; }
    
    // State tracking
    private PlayerCharacterData[] joinedPlayers;
    private Dictionary<int, int> playerVotes = new Dictionary<int, int>(); // playerIndex -> mapIndex
    private int[] mapVoteCounts = new int[4]; // Vote count per map
    private float votingTimer;
    private bool isVotingActive = false;
    private bool hasCompletedVoting = false;
    
    // Map selection indicators (multiple per map for stacked player votes)
    private List<MapSelectionIndicator>[] mapIndicators = new List<MapSelectionIndicator>[4];
    private GameObject[] mapIconObjects = new GameObject[4];
    
    // Current hover/selection per player
    private int[] playerCurrentSelection = new int[4]; // Current map each player is hovering over
    
    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("MapVotingManager: Duplicate instance detected, destroying...");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Initialize arrays
        for (int i = 0; i < 4; i++)
        {
            mapIndicators[i] = new List<MapSelectionIndicator>();
            playerCurrentSelection[i] = 0; // Default to first map
        }
        
        if (enableDebugLogging)
        {
            Debug.Log("MapVotingManager: Initialized");
        }
    }
    
    void Start()
    {
        // Load player data from GameDataManager
        if (GameDataManager.Instance != null)
        {
            joinedPlayers = GameDataManager.Instance.GetSelectedCharacters();
            
            if (joinedPlayers != null && joinedPlayers.Length > 0)
            {
                Debug.Log($"MapVotingManager: Loaded {joinedPlayers.Length} players from GameDataManager");
                foreach (var player in joinedPlayers)
                {
                    if (player.isJoined)
                    {
                        Debug.Log($"  Player {player.playerIndex + 1}: Device={player.inputDevice}, Color={player.playerColor}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("MapVotingManager: No player data found in GameDataManager!");
            }
        }
        else
        {
            Debug.LogError("MapVotingManager: GameDataManager.Instance is null!");
        }
        
        // Validate map data
        if (availableMaps == null || availableMaps.Length < 4)
        {
            Debug.LogWarning("MapVotingManager: Need at least 4 maps for voting system");
            CreateTestMaps();
        }
        
        // Setup UI
        SetupMapVotingUI();
        
        // Start voting
        StartVoting();
    }
    
    void Update()
    {
        if (!isVotingActive || hasCompletedVoting) return;
        
        // Update timer
        votingTimer -= Time.deltaTime;
        UpdateTimerDisplay();
        
        if (votingTimer <= 0f)
        {
            EndVoting();
            return;
        }
        
        // Handle input for all joined players
        if (joinedPlayers != null)
        {
            for (int i = 0; i < joinedPlayers.Length; i++)
            {
                if (joinedPlayers[i].isJoined)
                {
                    HandlePlayerInput(i);
                }
            }
        }
    }
    
    void SetupMapVotingUI()
    {
        Debug.Log("MapVotingManager: Setting up voting UI...");
        
        // Create map icons in grid
        if (mapGridParent != null)
        {
            for (int i = 0; i < 4 && i < availableMaps.Length; i++)
            {
                CreateMapIcon(i);
            }
        }
        else
        {
            Debug.LogWarning("MapVotingManager: mapGridParent is null!");
        }
        
        // Initialize status text
        if (statusText != null)
        {
            statusText.text = "Select your map!";
        }
    }
    
    void CreateMapIcon(int mapIndex)
    {
        if (availableMaps[mapIndex] == null) return;
        
        GameObject iconObj;
        
        if (mapIconPrefab != null)
        {
            iconObj = Instantiate(mapIconPrefab, mapGridParent);
        }
        else
        {
            // Create basic icon if no prefab
            iconObj = new GameObject($"MapIcon_{mapIndex}");
            iconObj.transform.SetParent(mapGridParent, false);
            
            Image img = iconObj.AddComponent<Image>();
            if (availableMaps[mapIndex].mapIcon != null)
            {
                img.sprite = availableMaps[mapIndex].mapIcon;
            }
            else
            {
                img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }
            
            RectTransform rect = iconObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 200);
        }
        
        iconObj.name = $"MapIcon_{availableMaps[mapIndex].mapName}";
        mapIconObjects[mapIndex] = iconObj;
        
        // Add vote counter
        CreateVoteCounter(mapIndex, iconObj.transform);
        
        Debug.Log($"MapVotingManager: Created map icon for {availableMaps[mapIndex].mapName}");
    }
    
    void CreateVoteCounter(int mapIndex, Transform parent)
    {
        GameObject counterObj;
        
        if (voteCounterPrefab != null)
        {
            counterObj = Instantiate(voteCounterPrefab, parent);
        }
        else
        {
            // Create basic counter
            counterObj = new GameObject($"VoteCounter_{mapIndex}");
            counterObj.transform.SetParent(parent, false);
            
            TextMeshProUGUI text = counterObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 36;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = "0";
            
            RectTransform rect = counterObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 10);
            rect.sizeDelta = new Vector2(80, 50);
        }
        
        voteCounterTexts[mapIndex] = counterObj.GetComponent<TextMeshProUGUI>();
        counterObj.SetActive(false); // Hide until votes are cast
    }
    
    void StartVoting()
    {
        Debug.Log("=== MapVotingManager: Starting Voting Phase ===");
        
        isVotingActive = true;
        hasCompletedVoting = false;
        votingTimer = votingDuration;
        
        // Reset vote tracking
        playerVotes.Clear();
        for (int i = 0; i < 4; i++)
        {
            mapVoteCounts[i] = 0;
        }
        
        // Initialize player selections to first map
        if (joinedPlayers != null)
        {
            for (int i = 0; i < joinedPlayers.Length; i++)
            {
                if (joinedPlayers[i].isJoined)
                {
                    playerCurrentSelection[i] = 0;
                    // Show initial selection indicator
                    UpdatePlayerSelectionIndicator(i, 0);
                }
            }
        }
        
        UpdateVoteCounters();
        
        if (statusText != null)
        {
            statusText.text = "Vote for your favorite map!";
        }
        
        Debug.Log($"MapVotingManager: Voting started with {votingDuration}s timer");
    }
    
    void HandlePlayerInput(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= joinedPlayers.Length) return;
        if (!joinedPlayers[playerIndex].isJoined) return;
        
        InputDeviceType device = joinedPlayers[playerIndex].inputDevice;
        
        // Navigation input
        Vector2 navInput = GetNavigationInput(device);
        if (navInput != Vector2.zero)
        {
            HandlePlayerNavigation(playerIndex, navInput);
        }
        
        // Submit vote
        if (GetSubmitInput(device))
        {
            SubmitPlayerVote(playerIndex);
        }
        
        // Note: Early skip can be implemented by checking if all players have voted
        // This is a more intuitive approach than requiring special input combinations
    }
    
    void HandlePlayerNavigation(int playerIndex, Vector2 input)
    {
        int currentMap = playerCurrentSelection[playerIndex];
        int newMap = currentMap;
        
        // Horizontal navigation (primary)
        if (input.x > 0.5f)
        {
            newMap = (currentMap + 1) % 4; // Move right, wrap around
        }
        else if (input.x < -0.5f)
        {
            newMap = (currentMap - 1 + 4) % 4; // Move left, wrap around
        }
        // Vertical navigation (secondary)
        else if (input.y > 0.5f)
        {
            newMap = (currentMap - 2 + 4) % 4; // Move up
        }
        else if (input.y < -0.5f)
        {
            newMap = (currentMap + 2) % 4; // Move down
        }
        
        if (newMap != currentMap)
        {
            playerCurrentSelection[playerIndex] = newMap;
            UpdatePlayerSelectionIndicator(playerIndex, newMap);
            
            if (enableDebugLogging)
            {
                Debug.Log($"Player {playerIndex + 1} navigated to map {newMap}");
            }
        }
    }
    
    void UpdatePlayerSelectionIndicator(int playerIndex, int mapIndex)
    {
        // Create or update selection indicator for this player on this map
        // This shows the blinking gradient while hovering
        
        if (selectionIndicatorContainer == null || mapIconObjects[mapIndex] == null) return;
        
        // Find or create indicator for this player
        MapSelectionIndicator indicator = GetOrCreateIndicator(playerIndex, mapIndex);
        
        if (indicator != null)
        {
            indicator.SetPlayer(playerIndex);
            indicator.SetPlayerColor(joinedPlayers[playerIndex].playerColor);
            indicator.SetVisible(true);
            indicator.SetSelectionState(true); // Start blinking
            indicator.SetLockedState(false); // Not locked yet
        }
        
        // Hide indicators on other maps for this player
        for (int i = 0; i < 4; i++)
        {
            if (i != mapIndex)
            {
                HidePlayerIndicatorOnMap(playerIndex, i);
            }
        }
    }
    
    MapSelectionIndicator GetOrCreateIndicator(int playerIndex, int mapIndex)
    {
        Transform mapIconTransform = mapIconObjects[mapIndex]?.transform;
        if (mapIconTransform == null) return null;
        
        // Look for existing indicator for this player on this map
        foreach (var indicator in mapIndicators[mapIndex])
        {
            // Check if this indicator is for our player (we can add a tracking mechanism)
            if (indicator.gameObject.name.Contains($"_P{playerIndex}"))
            {
                return indicator;
            }
        }
        
        // Create new indicator
        GameObject indicatorObj = new GameObject($"SelectionIndicator_P{playerIndex}");
        indicatorObj.transform.SetParent(mapIconTransform, false);
        
        MapSelectionIndicator indicator = indicatorObj.AddComponent<MapSelectionIndicator>();
        
        // Setup RectTransform to fill parent
        RectTransform rect = indicatorObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        mapIndicators[mapIndex].Add(indicator);
        
        return indicator;
    }
    
    void HidePlayerIndicatorOnMap(int playerIndex, int mapIndex)
    {
        foreach (var indicator in mapIndicators[mapIndex])
        {
            if (indicator.gameObject.name.Contains($"_P{playerIndex}"))
            {
                indicator.SetVisible(false);
            }
        }
    }
    
    void SubmitPlayerVote(int playerIndex)
    {
        int selectedMap = playerCurrentSelection[playerIndex];
        
        // Check if player is changing vote
        if (playerVotes.ContainsKey(playerIndex))
        {
            if (!allowVoteChanges)
            {
                Debug.Log($"Player {playerIndex + 1} cannot change vote (locked)");
                return;
            }
            
            int previousVote = playerVotes[playerIndex];
            if (previousVote == selectedMap)
            {
                Debug.Log($"Player {playerIndex + 1} already voted for map {selectedMap}");
                return;
            }
            
            // Remove old vote
            mapVoteCounts[previousVote]--;
            Debug.Log($"Player {playerIndex + 1} changed vote from map {previousVote} to {selectedMap}");
        }
        else
        {
            Debug.Log($"Player {playerIndex + 1} voted for map {selectedMap}");
        }
        
        // Register new vote
        playerVotes[playerIndex] = selectedMap;
        mapVoteCounts[selectedMap]++;
        
        // Update visual - lock the indicator
        MapSelectionIndicator indicator = GetOrCreateIndicator(playerIndex, selectedMap);
        if (indicator != null)
        {
            indicator.SetLockedState(true);
            indicator.FlashSelection(0.3f);
        }
        
        UpdateVoteCounters();
        
        // Check if all players have voted
        if (AllPlayersVoted())
        {
            Debug.Log("MapVotingManager: All players have voted!");
            
            // Auto-complete voting if enabled
            if (autoCompleteWhenAllVoted)
            {
                Debug.Log("MapVotingManager: Auto-completing voting (all players voted)");
                votingTimer = Mathf.Min(votingTimer, 2f); // Give 2 seconds to see results
            }
        }
    }
    
    bool AllPlayersVoted()
    {
        if (joinedPlayers == null) return false;
        
        int joinedCount = 0;
        foreach (var player in joinedPlayers)
        {
            if (player.isJoined) joinedCount++;
        }
        
        return playerVotes.Count >= joinedCount;
    }
    
    void UpdateVoteCounters()
    {
        for (int i = 0; i < 4; i++)
        {
            if (voteCounterTexts[i] != null)
            {
                voteCounterTexts[i].text = mapVoteCounts[i].ToString();
                voteCounterTexts[i].gameObject.SetActive(mapVoteCounts[i] > 0);
            }
        }
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int secondsLeft = Mathf.CeilToInt(votingTimer);
            timerText.text = $"{secondsLeft}s";
            
            // Change color when time is running out
            if (votingTimer <= 5f)
            {
                timerText.color = Color.red;
            }
            else if (votingTimer <= 10f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }
    
    void EndVoting()
    {
        if (hasCompletedVoting) return;
        
        hasCompletedVoting = true;
        isVotingActive = false;
        
        Debug.Log("=== MapVotingManager: Voting Phase Complete ===");
        
        // Determine winning map
        int winningMapIndex = DetermineWinningMap();
        
        if (winningMapIndex >= 0 && winningMapIndex < availableMaps.Length)
        {
            MapData winningMap = availableMaps[winningMapIndex];
            string mapName = winningMap.GetDisplayName();
            
            Debug.Log($"Winning Map: {mapName} (Index: {winningMapIndex}) with {mapVoteCounts[winningMapIndex]} votes");
            
            // Save to GameDataManager
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SetSelectedMap(winningMap);
            }
            
            // Update UI
            if (statusText != null)
            {
                statusText.text = $"Map Selected: {mapName}!";
            }
            
            // Load the scene after a brief delay
            StartCoroutine(LoadWinningMapScene(winningMapIndex));
        }
        else
        {
            Debug.LogError("MapVotingManager: Failed to determine winning map!");
        }
    }
    
    int DetermineWinningMap()
    {
        // Find map with most votes
        int maxVotes = mapVoteCounts.Max();
        
        if (maxVotes == 0)
        {
            // No votes cast, random selection
            Debug.Log("MapVotingManager: No votes cast, selecting random map");
            return Random.Range(0, 4);
        }
        
        // Get all maps with max votes (for tie handling)
        List<int> tiedMaps = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            if (mapVoteCounts[i] == maxVotes)
            {
                tiedMaps.Add(i);
            }
        }
        
        if (tiedMaps.Count > 1)
        {
            // Tie - random selection among tied maps
            int randomIndex = Random.Range(0, tiedMaps.Count);
            int winner = tiedMaps[randomIndex];
            Debug.Log($"MapVotingManager: Tie detected! {tiedMaps.Count} maps with {maxVotes} votes. Randomly selected map {winner}");
            return winner;
        }
        
        return tiedMaps[0];
    }
    
    IEnumerator LoadWinningMapScene(int mapIndex)
    {
        // Show result for a moment
        yield return new WaitForSeconds(2f);
        
        string sceneToLoad = GetSceneForMap(mapIndex);
        
        Debug.Log($"MapVotingManager: Loading scene '{sceneToLoad}'");
        
        try
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene '{sceneToLoad}': {e.Message}");
            Debug.Log($"Attempting to load fallback scene: {gameplaySceneName}");
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
    
    string GetSceneForMap(int mapIndex)
    {
        // Try to get scene name from MapData first
        if (mapIndex >= 0 && mapIndex < availableMaps.Length && availableMaps[mapIndex] != null)
        {
            string sceneName = availableMaps[mapIndex].GetSceneName();
            if (!string.IsNullOrEmpty(sceneName))
            {
                return sceneName;
            }
        }
        
        // Fall back to array
        if (mapIndex >= 0 && mapIndex < mapSceneNames.Length)
        {
            return mapSceneNames[mapIndex];
        }
        
        // Final fallback
        return gameplaySceneName;
    }
    
    // Input handling methods
    Vector2 GetNavigationInput(InputDeviceType device)
    {
        switch (device)
        {
            case InputDeviceType.Keyboard:
                return GetKeyboardNavigation();
            case InputDeviceType.Controller1:
                return GetControllerNavigation(0);
            case InputDeviceType.Controller2:
                return GetControllerNavigation(1);
            case InputDeviceType.Controller3:
                return GetControllerNavigation(2);
            default:
                return Vector2.zero;
        }
    }
    
    Vector2 GetKeyboardNavigation()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            return Vector2.left;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            return Vector2.right;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            return Vector2.up;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            return Vector2.down;
        
        return Vector2.zero;
    }
    
    Vector2 GetControllerNavigation(int controllerIndex)
    {
        string joystickName = $"joystick {controllerIndex + 1}";
        
        // D-pad buttons (Xbox controller standard mapping)
        // Note: These may vary by controller type. For production, consider using Unity's new Input System
        if (Input.GetKeyDown($"{joystickName} button 13")) return Vector2.left;   // D-pad left
        if (Input.GetKeyDown($"{joystickName} button 14")) return Vector2.right;  // D-pad right
        if (Input.GetKeyDown($"{joystickName} button 11")) return Vector2.up;     // D-pad up
        if (Input.GetKeyDown($"{joystickName} button 12")) return Vector2.down;   // D-pad down
        
        return Vector2.zero;
    }
    
    bool GetSubmitInput(InputDeviceType device)
    {
        switch (device)
        {
            case InputDeviceType.Keyboard:
                return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
            case InputDeviceType.Controller1:
                return GetControllerSubmit(0);
            case InputDeviceType.Controller2:
                return GetControllerSubmit(1);
            case InputDeviceType.Controller3:
                return GetControllerSubmit(2);
            default:
                return false;
        }
    }
    
    bool GetControllerSubmit(int controllerIndex)
    {
        string joystickName = $"joystick {controllerIndex + 1}";
        // A button (Xbox) / Cross button (PlayStation) - Standard button 0
        // Note: For production, consider using Unity's new Input System for better cross-platform support
        return Input.GetKeyDown($"{joystickName} button 0");
    }
    
    void CreateTestMaps()
    {
        availableMaps = new MapData[4];
        for (int i = 0; i < 4; i++)
        {
            var testMap = ScriptableObject.CreateInstance<MapData>();
            testMap.mapName = $"Test Map {i + 1}";
            testMap.sceneName = $"TestMap{i + 1}Scene";
            availableMaps[i] = testMap;
        }
        Debug.Log($"MapVotingManager: Created {availableMaps.Length} test maps");
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // Debug helpers
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("=== MapVotingManager Debug State ===");
        Debug.Log($"Is Voting Active: {isVotingActive}");
        Debug.Log($"Timer: {votingTimer:F1}s");
        Debug.Log($"Joined Players: {joinedPlayers?.Length ?? 0}");
        
        if (joinedPlayers != null)
        {
            foreach (var player in joinedPlayers)
            {
                if (player.isJoined)
                {
                    Debug.Log($"  Player {player.playerIndex + 1}: Device={player.inputDevice}, Color={player.playerColor}");
                }
            }
        }
        
        Debug.Log("Vote Counts:");
        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"  Map {i}: {mapVoteCounts[i]} votes");
        }
        
        Debug.Log("Player Votes:");
        foreach (var kvp in playerVotes)
        {
            Debug.Log($"  Player {kvp.Key + 1} voted for Map {kvp.Value}");
        }
    }
}
