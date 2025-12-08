using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Map Selection Manager that handles voting for maps after character selection.
/// Works with the auto-progression system from NewCharacterSelectManager.
/// Supports multi-player input using the same device system as character selection.
/// Uses the same navigation and selection system as CharacterGridUI for consistency.
/// 
/// Enhanced with timer system that redirects to most voted map when time expires,
/// or immediately proceeds when all players lock their votes.
/// 
/// FIXED: Canvas hierarchy validation and indicator positioning for proper visibility.
/// </summary>
public class MapSelectionManager : MonoBehaviour
{
    [Header("Map Data")]
    public MapData[] availableMaps;
    
    [Header("UI References")]
    public Transform mapGridParent;
    public GameObject mapIconPrefab;
    public GameObject mapSelectionIndicatorPrefab; // Selection indicator for players
    public Transform selectionIndicatorContainer; // Container for indicators (optional)
    
    [Header("Timer UI")]
    public TextMeshProUGUI timerText; // Main timer display
    
    [Header("Grid Settings")]
    public int mapsPerRow = 3;
    
    [Header("Voting Settings")]
    [Range(5f, 60f)]
    public float votingTimeLimit = 15f;
    public bool requireUnanimousVote = false;
    [Range(3f, 10f)]
    public float countdownWarningTime = 5f; // Show big countdown numbers
    
    [Header("Scene Settings")]
    public string gameplaySceneName = "GameplayScene"; // Fallback scene name
    public string[] mapSceneNames = { "Map1Scene", "Map2Scene", "Map3Scene", "Map4Scene" }; // Individual map scenes
    
    [Header("Player Vote Colors")]
    public Color[] playerVoteColors = new Color[] 
    {
        Color.red,      // Player 1
        Color.blue,     // Player 2
        Color.green,    // Player 3
        Color.yellow    // Player 4
    };
    
    // Canvas setup fields
    [Header("Canvas Setup (Auto-detected if empty)")]
    public Canvas mainCanvas;
    public Transform uiContainer; // Container for all UI elements
    
    private PlayerCharacterData[] joinedPlayers;
    private Dictionary<int, int> playerVotes = new Dictionary<int, int>(); // playerIndex -> mapIndex
    private Dictionary<int, bool> playerVoteLocked = new Dictionary<int, bool>(); // playerIndex -> isLocked
    private Dictionary<int, int> playerCurrentSelection = new Dictionary<int, int>(); // playerIndex -> selectedMapIndex
    private float votingTimer;
    private bool votingActive = true;
    private GameObject[] mapIcons;
    private GameObject[] playerSelectionIndicators; // Visual indicators for each player
    
    // Input device mapping (same as character select)
    private Dictionary<InputDevice, int> deviceToPlayerMap = new Dictionary<InputDevice, int>();
    
    public enum InputDevice
    {
        Keyboard,
        Controller1,
        Controller2,
        Controller3,
        Controller4
    }
    
    void Start()
    {
        Debug.Log("=== MapSelectionManager Start ===");
        
        // FIRST: Ensure Canvas hierarchy is properly set up
        ValidateCanvasHierarchy();
        
        // Check available maps first
        if (availableMaps == null || availableMaps.Length == 0)
        {
            Debug.LogError("MapSelectionManager: No maps assigned! Please assign maps in inspector.");
            CreateTestMaps(); // Create test data
        }
        else
        {
            Debug.Log($"MapSelectionManager: Found {availableMaps.Length} maps assigned in inspector");
            for (int i = 0; i < availableMaps.Length; i++)
            {
                if (availableMaps[i] != null)
                {
                    Debug.Log($"  Map {i}: {availableMaps[i].GetDisplayName()} -> Scene: {availableMaps[i].GetSceneName()}");
                }
                else
                {
                    Debug.LogError($"  Map {i}: NULL - Please assign a MapData asset!");
                }
            }
        }
        
        // Get player data from GameDataManager
        if (GameDataManager.Instance != null)
        {
            joinedPlayers = GameDataManager.Instance.GetSelectedCharacters();
            Debug.Log($"MapSelectionManager: Loaded {GetJoinedPlayerCount()} players from character selection");
            
            // Debug: Show detailed player information
            for (int i = 0; i < joinedPlayers.Length; i++)
            {
                if (joinedPlayers[i] != null)
                {
                    Debug.Log($"  Player {i + 1}: Joined={joinedPlayers[i].isJoined}, Locked={joinedPlayers[i].hasLockedCharacter}, Character={(joinedPlayers[i].lockedCharacter != null ? joinedPlayers[i].lockedCharacter.characterName : "None")}");
                }
                else
                {
                    Debug.Log($"  Player {i + 1}: NULL");
                }
            }
        }
        else
        {
            Debug.LogWarning("MapSelectionManager: GameDataManager not found! Creating dummy data for testing.");
            CreateDummyPlayerData();
        }
        
        SetupDeviceMapping();
        InitializeMapSelection();
        InitializeTimer();
        
        Debug.Log("=== MapSelectionManager Start Complete ===");
    }
    
    void ValidateCanvasHierarchy()
    {
        Debug.Log("=== ValidateCanvasHierarchy Start ===");
        
        // Find or create main canvas
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }
        
        if (mainCanvas == null)
        {
            Debug.LogWarning("MapSelectionManager: No Canvas found! Creating one...");
            CreateMainCanvas();
        }
        else
        {
            Debug.Log($"MapSelectionManager: Using existing Canvas: {mainCanvas.name}");
        }
        
        // Ensure Canvas is properly configured
        ConfigureCanvas();
        
        // Set up UI container hierarchy
        SetupUIContainerHierarchy();
        
        // Ensure EventSystem exists
        EnsureEventSystem();
        
        Debug.Log("=== ValidateCanvasHierarchy Complete ===");
    }
    
    void CreateMainCanvas()
    {
        GameObject canvasObj = new GameObject("MapSelectionCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 10; // Above most other UI
        
        // Add CanvasScaler for responsive design
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // Add GraphicRaycaster for input
        canvasObj.AddComponent<GraphicRaycaster>();
        
        Debug.Log("MapSelectionManager: Created main Canvas with CanvasScaler and GraphicRaycaster");
    }
    
    void ConfigureCanvas()
    {
        if (mainCanvas == null) return;
        
        // Ensure Canvas is active and enabled
        mainCanvas.gameObject.SetActive(true);
        mainCanvas.enabled = true;
        
        // Ensure proper render mode
        if (mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Debug.LogWarning($"MapSelectionManager: Canvas was in {mainCanvas.renderMode} mode, switching to ScreenSpaceOverlay");
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        
        // Ensure Canvas has necessary components
        if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("MapSelectionManager: Added missing GraphicRaycaster to Canvas");
        }
        
        Debug.Log($"MapSelectionManager: Canvas configured - Mode: {mainCanvas.renderMode}, SortingOrder: {mainCanvas.sortingOrder}");
    }
    
    void SetupUIContainerHierarchy()
    {
        // Find or create UI container
        if (uiContainer == null)
        {
            Transform existingContainer = mainCanvas.transform.Find("UIContainer");
            if (existingContainer != null)
            {
                uiContainer = existingContainer;
            }
            else
            {
                GameObject containerObj = new GameObject("UIContainer");
                containerObj.transform.SetParent(mainCanvas.transform, false);
                uiContainer = containerObj.transform;
                
                // Setup container RectTransform to fill canvas
                RectTransform containerRect = containerObj.AddComponent<RectTransform>();
                containerRect.anchorMin = Vector2.zero;
                containerRect.anchorMax = Vector2.one;
                containerRect.offsetMin = Vector2.zero;
                containerRect.offsetMax = Vector2.zero;
            }
        }
        
        // Ensure mapGridParent is properly set up
        if (mapGridParent == null)
        {
            Transform existingGrid = uiContainer.Find("MapGridParent");
            if (existingGrid != null)
            {
                mapGridParent = existingGrid;
            }
            else
            {
                GameObject gridObj = new GameObject("MapGridParent");
                gridObj.transform.SetParent(uiContainer, false);
                mapGridParent = gridObj.transform;
                
                // Setup grid parent RectTransform
                RectTransform gridRect = gridObj.AddComponent<RectTransform>();
                gridRect.anchorMin = new Vector2(0.1f, 0.3f); // Center area of screen
                gridRect.anchorMax = new Vector2(0.9f, 0.8f);
                gridRect.offsetMin = Vector2.zero;
                gridRect.offsetMax = Vector2.zero;
                
                // Add GridLayoutGroup for automatic layout
                GridLayoutGroup gridLayout = gridObj.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(200, 200);
                gridLayout.spacing = new Vector2(20, 20);
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = mapsPerRow;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;
            }
        }
        
        // Set up indicator container (same level as grid parent for proper positioning)
        if (selectionIndicatorContainer == null)
        {
            selectionIndicatorContainer = uiContainer; // Use UI container as indicator parent
        }
        
        Debug.Log($"MapSelectionManager: UI hierarchy set up - Canvas: {mainCanvas.name}, Container: {uiContainer.name}, Grid: {mapGridParent.name}");
    }
    
    void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("MapSelectionManager: Created EventSystem");
        }
        else
        {
            Debug.Log($"MapSelectionManager: Using existing EventSystem: {EventSystem.current.name}");
        }
    }
    
    void CreateTestMaps()
    {
        // Create test map data if none assigned
        availableMaps = new MapData[4];
        for (int i = 0; i < 4; i++)
        {
            var testMap = ScriptableObject.CreateInstance<MapData>();
            testMap.mapName = $"Test Map {i + 1}";
            testMap.sceneName = $"TestMap{i + 1}Scene";
            availableMaps[i] = testMap;
        }
        Debug.Log("MapSelectionManager: Created 4 test maps");
    }
    
    void CreateDummyPlayerData()
    {
        Debug.Log("=== CreateDummyPlayerData Start ===");
        
        joinedPlayers = new PlayerCharacterData[4];
        for (int i = 0; i < 4; i++)
        {
            joinedPlayers[i] = new PlayerCharacterData(i);
            // For testing purposes, create only 1 player to match single player test
            if (i == 0) // Only create first player for single player testing
            {
                joinedPlayers[i].isJoined = true;
                joinedPlayers[i].hasLockedCharacter = true;
                joinedPlayers[i].selectedCharacterIndex = 0;
                Debug.Log($"CreateDummyPlayerData: Created Player {i + 1} as joined and locked");
            }
        }
        
        Debug.Log("MapSelectionManager: Created dummy data with 1 test player for single player testing");
    }
    
    void SetupDeviceMapping()
    {
        Debug.Log("=== SetupDeviceMapping Start ===");
        
        // Map devices to players based on their join order (same as character select)
        // Player 1 (index 0) always gets keyboard
        // Subsequent joined players get controllers in order
        
        int controllerIndex = 1; // Start with Controller1 for non-keyboard players
        
        deviceToPlayerMap.Clear();
        playerCurrentSelection.Clear();
        playerVoteLocked.Clear();
        
        for (int i = 0; i < (joinedPlayers != null ? joinedPlayers.Length : 0); i++)
        {
            if (IsPlayerJoined(i))
            {
                InputDevice device;
                
                if (i == 0) // Player 1 always gets keyboard
                {
                    device = InputDevice.Keyboard;
                }
                else
                {
                    // Other players get controllers in order
                    device = (InputDevice)controllerIndex;
                    controllerIndex++;
                    
                    // Make sure we don't exceed available controllers
                    if (controllerIndex > 4) 
                    {
                        Debug.LogWarning($"MapSelectionManager: Too many players for available controllers! Player {i + 1} won't have input.");
                        continue;
                    }
                }
                
                deviceToPlayerMap[device] = i; // Map to actual player index
                
                // Initialize player selection state
                playerCurrentSelection[i] = 0; // Start at first map
                playerVoteLocked[i] = false;
                
                Debug.Log($"MapSelectionManager: Player {i + 1} (actual index {i}) mapped to {device}");
            }
        }
        
        // Fallback: ensure single-player keyboard mapping if nothing mapped
        if (deviceToPlayerMap.Count == 0)
        {
            Debug.LogWarning("MapSelectionManager: No joined player mappings found. Forcing single-player keyboard control for Player 1.");
            deviceToPlayerMap[InputDevice.Keyboard] = 0;
            playerCurrentSelection[0] = 0;
            playerVoteLocked[0] = false;
        }
        
        Debug.Log($"MapSelectionManager: Device mapping complete - {deviceToPlayerMap.Count} players mapped");
        foreach (var mapping in deviceToPlayerMap)
        {
            Debug.Log($"  {mapping.Key} -> Player {mapping.Value + 1}");
        }
        
        // Show initial selection displays for all joined players
        StartCoroutine(DelayedInitialSelectionDisplay());
    }
    
    private System.Collections.IEnumerator DelayedInitialSelectionDisplay()
    {
        Debug.Log("=== DelayedInitialSelectionDisplay Start ===");
        
        // Wait for layout to settle and canvas updates
        yield return new WaitForSeconds(0.5f);
        
        // Force canvas updates multiple times to ensure proper layout
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // Show indicators for all joined players at their starting positions
        foreach (var kvp in playerCurrentSelection)
        {
            int playerIndex = kvp.Key;
            Debug.Log($"DelayedInitialSelectionDisplay: Setting up Player {playerIndex + 1}");
            
            // Configure and show the indicator
            if (playerSelectionIndicators != null && playerIndex < playerSelectionIndicators.Length && playerSelectionIndicators[playerIndex] != null)
            {
                playerSelectionIndicators[playerIndex].SetActive(true);
                
                MapSelectionIndicator indicatorScript = playerSelectionIndicators[playerIndex].GetComponent<MapSelectionIndicator>();
                if (indicatorScript != null)
                {
                    indicatorScript.SetPlayer(playerIndex);
                    indicatorScript.SetPlayerColor(playerVoteColors[playerIndex]);
                    indicatorScript.SetVisible(true);
                    indicatorScript.SetSelectionState(true);
                    indicatorScript.StartAnimation();
                }
                
                UpdatePlayerSelectionDisplay(playerIndex);
            }
        }
        
        Debug.Log("=== DelayedInitialSelectionDisplay Complete ===");
    }
    
    void InitializeMapSelection()
    {
        Debug.Log("=== InitializeMapSelection Start ===");
        
        CreateMapIcons();
        CreatePlayerSelectionIndicators();
        UpdateAllSelectionDisplays();
        UpdateUI();
        
        // Start voting timer
        votingActive = true;
        
        Debug.Log("MapSelectionManager: Map voting started!");
        LogJoinedPlayers();
        
        Debug.Log("=== InitializeMapSelection Complete ===");
    }
    
    void CreateMapIcons()
    {
        if (availableMaps == null || availableMaps.Length == 0)
        {
            Debug.LogError("MapSelectionManager: No maps available!");
            return;
        }
        
        Debug.Log($"MapSelectionManager: Creating {availableMaps.Length} map icons");
        mapIcons = new GameObject[availableMaps.Length];
        
        for (int i = 0; i < availableMaps.Length; i++)
        {
            GameObject mapIcon;
            
            if (mapIconPrefab != null)
            {
                mapIcon = Instantiate(mapIconPrefab, mapGridParent);
            }
            else
            {
                // Create default map icon
                mapIcon = CreateDefaultMapIcon(i);
            }
            
            mapIcon.name = $"MapIcon_{i}_{(availableMaps[i] != null ? availableMaps[i].GetDisplayName() : "TestMap")}";
            
            // Setup map icon image and text
            SetupMapIconImage(mapIcon, i);
            SetupMapNameText(mapIcon, i);
            
            mapIcons[i] = mapIcon;
            
            Debug.Log($"MapSelectionManager: Created map icon {i}: {mapIcon.name}");
        }
        
        Debug.Log($"MapSelectionManager: Finished creating {mapIcons.Length} map icons");
        
        // Force canvas update after creation
        StartCoroutine(ForceCanvasUpdateNextFrame());
    }
    
    GameObject CreateDefaultMapIcon(int index)
    {
        GameObject mapIcon = new GameObject($"MapIcon_{index}")
        {
            transform = { parent = mapGridParent, localScale = Vector3.one }
        };
        
        // Add Image component
        Image iconImage = mapIcon.AddComponent<Image>();
        iconImage.color = Color.gray; // Default color
        iconImage.raycastTarget = false;
        
        // Setup RectTransform
        RectTransform iconRect = mapIcon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(200, 200); // Match grid layout cell size
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.zero;
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        
        return mapIcon;
    }
    
    void SetupMapIconImage(GameObject mapIcon, int mapIndex)
    {
        Image iconImage = mapIcon.GetComponent<Image>();
        if (iconImage == null)
            iconImage = mapIcon.GetComponentInChildren<Image>();
        
        if (iconImage != null && availableMaps[mapIndex] != null && availableMaps[mapIndex].mapIcon != null)
        {
            iconImage.sprite = availableMaps[mapIndex].mapIcon;
            Debug.Log($"MapSelectionManager: Set map icon sprite for {availableMaps[mapIndex].GetDisplayName()}");
        }
        else if (iconImage != null)
        {
            iconImage.color = Color.gray; // Show placeholder
            Debug.Log($"MapSelectionManager: Using placeholder for map {mapIndex}");
        }
    }
    
    void SetupMapNameText(GameObject mapIcon, int mapIndex)
    {
        string mapName = availableMaps[mapIndex] != null ? availableMaps[mapIndex].GetDisplayName() : $"Test Map {mapIndex + 1}";
        
        Text legacyText = mapIcon.GetComponentInChildren<Text>();
        TMPro.TextMeshProUGUI tmpText = mapIcon.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        
        if (tmpText != null)
        {
            tmpText.text = mapName;
            tmpText.enabled = true;
        }
        else if (legacyText != null)
        {
            legacyText.text = mapName;
            legacyText.enabled = true;
        }
        else
        {
            // Create a child GameObject for the text
            GameObject textObj = new GameObject("MapNameText");
            textObj.transform.SetParent(mapIcon.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            Text newText = textObj.AddComponent<Text>();
            newText.text = mapName;
            newText.fontSize = 18;
            newText.alignment = TextAnchor.MiddleCenter;
            newText.color = Color.white;
            newText.raycastTarget = false;
            
            // Position at bottom of map icon
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0.3f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont != null)
                newText.font = defaultFont;
        }
        
        Debug.Log($"MapSelectionManager: Set map name: {mapName}");
    }
    
    private System.Collections.IEnumerator ForceCanvasUpdateNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();
        Debug.Log("MapSelectionManager: Forced canvas updates for proper layout");
    }
    
    void CreatePlayerSelectionIndicators()
    {
        Debug.Log("=== CreatePlayerSelectionIndicators Start ===");
        
        playerSelectionIndicators = new GameObject[4];
        
        Transform indicatorParent = selectionIndicatorContainer != null ? selectionIndicatorContainer : uiContainer;
        Debug.Log($"Indicator parent: {(indicatorParent != null ? indicatorParent.name : "NULL")}");
        
        for (int i = 0; i < 4; i++)
        {
            GameObject indicator = CreateMapIndicator(i, indicatorParent);
            indicator.name = $"Player{i + 1}MapSelectionIndicator";
            
            MapSelectionIndicator indicatorComponent = indicator.GetComponent<MapSelectionIndicator>();
            if (indicatorComponent != null)
            {
                indicatorComponent.UpdatePlayerColors(playerVoteColors);
                indicatorComponent.SetPlayer(i);
                indicatorComponent.SetGradientStyle(MapGradientStyle.TekkenClassic);
            }
            
            // Initially hide - will be shown for joined players in DelayedInitialSelectionDisplay
            indicator.SetActive(false);
            playerSelectionIndicators[i] = indicator;
            
            Debug.Log($"Created map selection indicator for Player {i + 1}");
        }
        
        Debug.Log("=== CreatePlayerSelectionIndicators Complete ===");
    }
    
    GameObject CreateMapIndicator(int playerIndex, Transform parent)
    {
        GameObject indicator;
        
        if (mapSelectionIndicatorPrefab != null)
        {
            indicator = Instantiate(mapSelectionIndicatorPrefab, parent);
        }
        else
        {
            // Create enhanced indicator
            indicator = new GameObject($"Player{playerIndex + 1}MapIndicator");
            indicator.transform.SetParent(parent, false);
            
            // Add MapSelectionIndicator script
            MapSelectionIndicator indicatorScript = indicator.AddComponent<MapSelectionIndicator>();
            
            // Set up RectTransform
            RectTransform rectTransform = indicator.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 200); // Match map icon size
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            // Configure script
            indicatorScript.SetPlayer(playerIndex);
            indicatorScript.SetGradientStyle(MapGradientStyle.TekkenClassic);
        }
        
        return indicator;
    }
    
    void InitializeTimer()
    {
        votingTimer = votingTimeLimit;
        UpdateTimerDisplay();
        Debug.Log($"MapSelectionManager: Timer initialized - {votingTimeLimit} seconds for voting");
    }
    
    void Update()
    {
        if (votingActive)
        {
            HandleVotingTimer();
            
            // If no mappings existed, still handle keyboard for Player 1
            if (deviceToPlayerMap.Count == 0)
            {
                HandleDeviceInput(InputDevice.Keyboard, 0);
            }
            else
            {
                HandleAllInputDevices();
            }
        }
    }
    
    void HandleAllInputDevices()
    {
        foreach (var deviceMapping in deviceToPlayerMap)
        {
            HandleDeviceInput(deviceMapping.Key, deviceMapping.Value);
        }
    }
    
    void HandleDeviceInput(InputDevice device, int playerIndex)
    {
        // Don't handle input for locked players
        if (playerVoteLocked.ContainsKey(playerIndex) && playerVoteLocked[playerIndex])
            return;
            
        // Navigation
        Vector2 input = GetNavigationInput(device);
        if (input != Vector2.zero)
        {
            Debug.Log($"MapSelectionManager: Navigation input {input} from {device} for Player {playerIndex + 1}");
            NavigatePlayer(input, playerIndex);
        }

        // Submit (lock vote)
        if (GetSubmitInput(device))
        {
            Debug.Log($"MapSelectionManager: Submit input from {device} for Player {playerIndex + 1}");
            LockPlayerVote(playerIndex);
        }

        // Cancel (unlock vote if already locked)
        if (GetCancelInput(device))
        {
            Debug.Log($"MapSelectionManager: Cancel input from {device} for Player {playerIndex + 1}");
            UnlockPlayerVote(playerIndex);
        }
    }
    
    Vector2 GetNavigationInput(InputDevice device)
    {
        Vector2 input = Vector2.zero;
        
        switch (device)
        {
            case InputDevice.Keyboard:
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                    input = Vector2.left;
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                    input = Vector2.right;
                else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                    input = Vector2.up;
                else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                    input = Vector2.down;
                break;
                
            case InputDevice.Controller1:
            case InputDevice.Controller2:
            case InputDevice.Controller3:
            case InputDevice.Controller4:
                input = GetControllerNavigationInput(device);
                break;
        }
        
        if (input != Vector2.zero)
        {
            Debug.Log($"MapSelectionManager: Detected {device} input: {input}");
        }
        
        return input;
    }
    
    Vector2 GetControllerNavigationInput(InputDevice device)
    {
        int controllerIndex = (int)device - 1; // Controller1 = 0, Controller2 = 1, etc.
        string joystickName = $"joystick {controllerIndex + 1}";
        
        Vector2 input = Vector2.zero;
        
        // D-pad navigation (most reliable)
        if (Input.GetKeyDown($"{joystickName} button 13")) // D-pad left
        {
            input = Vector2.left;
            Debug.Log($"MapSelectionManager: Controller {controllerIndex + 1} D-pad LEFT");
        }
        else if (Input.GetKeyDown($"{joystickName} button 14")) // D-pad right
        {
            input = Vector2.right;
            Debug.Log($"MapSelectionManager: Controller {controllerIndex + 1} D-pad RIGHT");
        }
        
        if (Input.GetKeyDown($"{joystickName} button 11")) // D-pad up
        {
            input = Vector2.up;
            Debug.Log($"MapSelectionManager: Controller {controllerIndex + 1} D-pad UP");
        }
        else if (Input.GetKeyDown($"{joystickName} button 12")) // D-pad down
        {
            input = Vector2.down;
            Debug.Log($"MapSelectionManager: Controller {controllerIndex + 1} D-pad DOWN");
        }
        
        // Try analog stick for first controller only (to avoid input manager issues)
        if (controllerIndex == 0 && input == Vector2.zero)
        {
            try 
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                
                // Convert analog to discrete with deadzone
                if (Mathf.Abs(horizontal) > 0.8f)
                {
                    input.x = horizontal > 0 ? 1f : -1f;
                    Debug.Log($"MapSelectionManager: Controller 1 analog stick horizontal: {input.x}");
                }
                
                if (Mathf.Abs(vertical) > 0.8f)
                {
                    input.y = vertical > 0 ? 1f : -1f;
                    Debug.Log($"MapSelectionManager: Controller 1 analog stick vertical: {input.y}");
                }
            }
            catch (System.ArgumentException)
            {
                // Default axes not available, stick with D-pad only
                Debug.LogWarning("MapSelectionManager: Default axes not available for controller input");
            }
        }
        
        return input;
    }
    
    bool GetSubmitInput(InputDevice device)
    {
        switch (device)
        {
            case InputDevice.Keyboard:
                return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
                
            case InputDevice.Controller1:
            case InputDevice.Controller2:
            case InputDevice.Controller3:
            case InputDevice.Controller4:
                int controllerIndex = (int)device - 1;
                string joystickName = $"joystick {controllerIndex + 1}";
                
                bool submitPressed = Input.GetKeyDown($"{joystickName} button 0") ||    // A button
                                   Input.GetKeyDown($"{joystickName} button 7") ||    // Start button (Xbox)
                                   Input.GetKeyDown($"{joystickName} button 9");     // Start button (PS4)
                
                if (submitPressed)
                {
                    Debug.Log($"MapSelectionManager: Controller {controllerIndex + 1} submit pressed");
                }
                
                return submitPressed;
        }
        return false;
    }
    
    bool GetCancelInput(InputDevice device)
    {
        switch (device)
        {
            case InputDevice.Keyboard:
                return Input.GetKeyDown(KeyCode.Escape);
                
            case InputDevice.Controller1:
            case InputDevice.Controller2:
            case InputDevice.Controller3:
            case InputDevice.Controller4:
                int controllerIndex = (int)device - 1;
                string joystickName = $"joystick {controllerIndex + 1}";
                
                bool cancelPressed = Input.GetKeyDown($"{joystickName} button 1") ||    // B button
                                   Input.GetKeyDown($"{joystickName} button 6") ||    // Back button (Xbox)
                                   Input.GetKeyDown($"{joystickName} button 8");     // Select/Share button (PS4)
                
                if (cancelPressed)
                {
                    Debug.Log($"MapSelectionManager: Controller {controllerIndex + 1} cancel pressed");
                }
                
                return cancelPressed;
        }
        return false;
    }
    
    void NavigatePlayer(Vector2 input, int playerIndex)
    {
        Debug.Log($"=== NavigatePlayer called for Player {playerIndex + 1} with input {input} ===");
        
        if (!playerCurrentSelection.ContainsKey(playerIndex))
        {
            Debug.LogError($"NavigatePlayer: playerCurrentSelection does not contain key {playerIndex}");
            return;
        }
            
        int currentSelection = playerCurrentSelection[playerIndex];
        int newSelection = currentSelection;
        
        if (input.x > 0) // Right
        {
            newSelection = (currentSelection + 1) % availableMaps.Length;
        }
        else if (input.x < 0) // Left
        {
            newSelection = (currentSelection - 1 + availableMaps.Length) % availableMaps.Length;
        }
        else if (input.y > 0) // Up
        {
            newSelection = (currentSelection - mapsPerRow + availableMaps.Length) % availableMaps.Length;
        }
        else if (input.y < 0) // Down
        {
            newSelection = (currentSelection + mapsPerRow) % availableMaps.Length;
        }
        
        if (newSelection != currentSelection)
        {
            playerCurrentSelection[playerIndex] = newSelection;
            string mapName = availableMaps[newSelection] != null ? availableMaps[newSelection].GetDisplayName() : $"Map {newSelection + 1}";
            Debug.Log($"Player {playerIndex + 1} navigated from {currentSelection} to {newSelection}: {mapName}");
            UpdatePlayerSelectionDisplay(playerIndex);
        }
    }
    
    void UpdatePlayerSelectionDisplay(int playerIndex)
    {
        Debug.Log($"=== UpdatePlayerSelectionDisplay for Player {playerIndex + 1} ===");
        
        if (playerSelectionIndicators == null || playerIndex >= playerSelectionIndicators.Length || playerSelectionIndicators[playerIndex] == null)
        {
            Debug.LogError($"Invalid indicator setup for Player {playerIndex + 1}");
            return;
        }
        
        if (!IsPlayerJoined(playerIndex))
        {
            playerSelectionIndicators[playerIndex].SetActive(false);
            return;
        }
        
        if (!playerCurrentSelection.ContainsKey(playerIndex))
        {
            Debug.LogError($"playerCurrentSelection does not contain key {playerIndex}");
            return;
        }
            
        int mapIndex = playerCurrentSelection[playerIndex];
        
        if (mapIndex >= 0 && mapIndex < mapIcons.Length && mapIcons[mapIndex] != null)
        {
            Debug.Log($"Positioning indicator for Player {playerIndex + 1} at map {mapIndex}");
            
            RectTransform indicatorRect = playerSelectionIndicators[playerIndex].GetComponent<RectTransform>();
            RectTransform mapRect = mapIcons[mapIndex].GetComponent<RectTransform>();
            
            if (indicatorRect != null && mapRect != null)
            {
                // Enhanced positioning - ensure both are in same coordinate space
                Vector3 mapWorldPos = mapRect.position;
                Vector3 indicatorLocalPos = indicatorRect.parent.InverseTransformPoint(mapWorldPos);
                
                // Set position and size to match map icon
                indicatorRect.position = mapWorldPos;
                indicatorRect.sizeDelta = mapRect.sizeDelta;
                
                Debug.Log($"  Map World Position: {mapWorldPos}");
                Debug.Log($"  Indicator Local Position: {indicatorLocalPos}");
                Debug.Log($"  Indicator World Position: {indicatorRect.position}");
                Debug.Log($"  Size Delta: {indicatorRect.sizeDelta}");
            }
            
            // Show and configure the indicator
            playerSelectionIndicators[playerIndex].SetActive(true);
            
            MapSelectionIndicator indicatorScript = playerSelectionIndicators[playerIndex].GetComponent<MapSelectionIndicator>();
            if (indicatorScript != null)
            {
                bool isLocked = playerVoteLocked.ContainsKey(playerIndex) && playerVoteLocked[playerIndex];
                
                indicatorScript.SetPlayer(playerIndex);
                indicatorScript.SetPlayerColor(playerVoteColors[playerIndex]);
                indicatorScript.SetVisible(true);
                indicatorScript.SetSelectionState(true);
                indicatorScript.SetLockedState(isLocked);
                
                if (!isLocked)
                {
                    indicatorScript.StartAnimation();
                    indicatorScript.FlashSelection(0.2f);
                }
                
                Debug.Log($"Indicator configured for Player {playerIndex + 1}!");
            }
        }
        else
        {
            Debug.LogWarning($"Invalid map selection for Player {playerIndex + 1}: mapIndex={mapIndex}");
            playerSelectionIndicators[playerIndex].SetActive(false);
        }
    }
    
    void UpdateAllSelectionDisplays()
    {
        foreach (var kvp in playerCurrentSelection)
        {
            UpdatePlayerSelectionDisplay(kvp.Key);
        }
    }
    
    void LockPlayerVote(int playerIndex)
    {
        if (!playerCurrentSelection.ContainsKey(playerIndex))
            return;
            
        int selectedMap = playerCurrentSelection[playerIndex];
        playerVotes[playerIndex] = selectedMap;
        playerVoteLocked[playerIndex] = true;
        
        UpdatePlayerSelectionDisplay(playerIndex);
        UpdateUI();
        
        string mapName = availableMaps[selectedMap] != null ? availableMaps[selectedMap].GetDisplayName() : $"Map {selectedMap + 1}";
        Debug.Log($"Player {playerIndex + 1} LOCKED vote for: {mapName}");
        
        CheckForConsensus();
    }
    
    void UnlockPlayerVote(int playerIndex)
    {
        if (playerVoteLocked.ContainsKey(playerIndex) && playerVoteLocked[playerIndex])
        {
            playerVoteLocked[playerIndex] = false;
            if (playerVotes.ContainsKey(playerIndex))
                playerVotes.Remove(playerIndex);
            
            UpdatePlayerSelectionDisplay(playerIndex);
            UpdateUI();
            
            Debug.Log($"Player {playerIndex + 1} UNLOCKED their vote");
        }
    }
    
    void HandleVotingTimer()
    {
        votingTimer -= Time.deltaTime;
        
        if (votingTimer <= 0f)
        {
            FinishVotingDueToTimeout();
        }
        
        UpdateTimerDisplay();
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int timeLeft = Mathf.CeilToInt(votingTimer);
            timerText.text = $"Time: {timeLeft}s";
        }
    }
    
    void CheckForConsensus()
    {
        int joinedPlayerCount = GetJoinedPlayerCount();
        int lockedVoteCount = GetLockedVoteCount();
        
        if (lockedVoteCount >= joinedPlayerCount && joinedPlayerCount > 0)
        {
            Debug.Log("MapSelectionManager: All players have voted! Auto-starting game...");
            FinishVotingDueToConsensus();
        }
    }
    
    void FinishVotingDueToTimeout()
    {
        votingActive = false;
        int winningMapIndex = GetWinningMap();
        
        if (winningMapIndex >= 0)
        {
            string mapName = availableMaps[winningMapIndex] != null ? availableMaps[winningMapIndex].GetDisplayName() : $"Map {winningMapIndex + 1}";
            Debug.Log($"MapSelectionManager: Time expired! Selected map: {mapName}");
        }
        else
        {
            winningMapIndex = 0;
            Debug.Log("MapSelectionManager: No votes, defaulting to first map");
        }
        
        StartCoroutine(ProceedToMapScene(winningMapIndex, "Time Expired"));
    }
    
    void FinishVotingDueToConsensus()
    {
        votingActive = false;
        int winningMapIndex = GetWinningMap();
        
        if (winningMapIndex >= 0)
        {
            string mapName = availableMaps[winningMapIndex] != null ? availableMaps[winningMapIndex].GetDisplayName() : $"Map {winningMapIndex + 1}";
            Debug.Log($"MapSelectionManager: All players voted! Selected map: {mapName}");
            StartCoroutine(ProceedToMapScene(winningMapIndex, "All Players Ready"));
        }
    }
    
    int GetWinningMap()
    {
        if (playerVotes.Count == 0) return -1;
        
        Dictionary<int, int> voteCounts = new Dictionary<int, int>();
        
        foreach (var vote in playerVotes.Values)
        {
            if (voteCounts.ContainsKey(vote))
                voteCounts[vote]++;
            else
                voteCounts[vote] = 1;
        }
        
        int winningMap = -1;
        int maxVotes = 0;
        
        foreach (var kvp in voteCounts)
        {
            if (kvp.Value > maxVotes)
            {
                maxVotes = kvp.Value;
                winningMap = kvp.Key;
            }
        }
        
        return winningMap;
    }
    
    IEnumerator ProceedToMapScene(int selectedMapIndex, string reason)
    {
        if (timerText != null)
        {
            string mapName = availableMaps[selectedMapIndex] != null ? availableMaps[selectedMapIndex].GetDisplayName() : $"Map {selectedMapIndex + 1}";
            timerText.text = $"Loading: {mapName}";
        }
        
        yield return new WaitForSeconds(2f);
        
        string sceneToLoad = GetSceneForMap(selectedMapIndex);
        Debug.Log($"MapSelectionManager: Loading scene '{sceneToLoad}'");
        
        SceneManager.LoadScene(sceneToLoad);
    }
    
    string GetSceneForMap(int mapIndex)
    {
        if (mapIndex >= 0 && mapIndex < availableMaps.Length && availableMaps[mapIndex] != null)
        {
            string mapSceneName = availableMaps[mapIndex].GetSceneName();
            if (!string.IsNullOrEmpty(mapSceneName))
                return mapSceneName;
        }
        
        if (mapIndex >= 0 && mapIndex < mapSceneNames.Length)
            return mapSceneNames[mapIndex];
        
        return gameplaySceneName;
    }
    
    void UpdateUI()
    {
        UpdateTimerDisplay();
    }
    
    bool IsPlayerJoined(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < joinedPlayers.Length &&
               joinedPlayers[playerIndex] != null && 
               joinedPlayers[playerIndex].isJoined && 
               joinedPlayers[playerIndex].hasLockedCharacter;
    }
    
    int GetJoinedPlayerCount()
    {
        if (joinedPlayers == null) return 0;
        
        int count = 0;
        for (int i = 0; i < joinedPlayers.Length; i++)
        {
            if (IsPlayerJoined(i))
                count++;
        }
        return count;
    }
    
    int GetLockedVoteCount()
    {
        int count = 0;
        foreach (var kvp in playerVoteLocked)
        {
            if (IsPlayerJoined(kvp.Key) && kvp.Value)
                count++;
        }
        return count;
    }
    
    void LogJoinedPlayers()
    {
        Debug.Log("=== LogJoinedPlayers ===");
        for (int i = 0; i < joinedPlayers.Length; i++)
        {
            if (IsPlayerJoined(i))
            {
                string characterName = joinedPlayers[i].lockedCharacter != null ? 
                    joinedPlayers[i].lockedCharacter.characterName : "Unknown";
                Debug.Log($"Player {i + 1}: {characterName} (Joined: {joinedPlayers[i].isJoined}, Locked: {joinedPlayers[i].hasLockedCharacter})");
            }
        }
    }
    
    // Debug methods for testing
    [ContextMenu("Test Navigation Right")]
    public void TestNavigationRight()
    {
        if (playerCurrentSelection.ContainsKey(0))
            NavigatePlayer(Vector2.right, 0);
    }
    
    [ContextMenu("Test Navigation Left")]  
    public void TestNavigationLeft()
    {
        if (playerCurrentSelection.ContainsKey(0))
            NavigatePlayer(Vector2.left, 0);
    }
    
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("=== MapSelectionManager Debug State ===");
        Debug.Log($"Voting Active: {votingActive}");
        Debug.Log($"Device Mappings: {deviceToPlayerMap.Count}");
        Debug.Log($"Player Selections: {playerCurrentSelection.Count}");
        Debug.Log($"Map Icons: {mapIcons?.Length}");
        Debug.Log($"Player Indicators: {playerSelectionIndicators?.Length}");
        Debug.Log($"Main Canvas: {(mainCanvas != null ? mainCanvas.name : "NULL")}");
        Debug.Log($"UI Container: {(uiContainer != null ? uiContainer.name : "NULL")}");
        Debug.Log($"Map Grid Parent: {(mapGridParent != null ? mapGridParent.name : "NULL")}");
        
        foreach (var selection in playerCurrentSelection)
        {
            Debug.Log($"  Player {selection.Key + 1} selecting map {selection.Value}");
        }
    }
    
    [ContextMenu("Validate Canvas Hierarchy")]
    public void ValidateCanvasHierarchyMenu()
    {
        ValidateCanvasHierarchy();
    }
    
    [ContextMenu("Force Show All Indicators")]
    public void ForceShowAllIndicators()
    {
        Debug.Log("=== Force Showing All Indicators ===");
        
        for (int i = 0; i < 4; i++)
        {
            if (playerSelectionIndicators != null && i < playerSelectionIndicators.Length && playerSelectionIndicators[i] != null)
            {
                playerSelectionIndicators[i].SetActive(true);
                
                MapSelectionIndicator indicator = playerSelectionIndicators[i].GetComponent<MapSelectionIndicator>();
                if (indicator != null)
                {
                    indicator.SetPlayer(i);
                    indicator.SetPlayerColor(playerVoteColors[i]);
                    indicator.SetVisible(true);
                    indicator.SetSelectionState(true);
                    indicator.StartAnimation();
                }
                
                // Position on first map for testing
                if (mapIcons != null && mapIcons.Length > 0 && mapIcons[0] != null)
                {
                    RectTransform indicatorRect = playerSelectionIndicators[i].GetComponent<RectTransform>();
                    RectTransform mapRect = mapIcons[0].GetComponent<RectTransform>();
                    
                    if (indicatorRect != null && mapRect != null)
                    {
                        indicatorRect.position = mapRect.position;
                        indicatorRect.sizeDelta = mapRect.sizeDelta;
                    }
                }
                
                Debug.Log($"Forced showing indicator for Player {i + 1}");
            }
        }
    }
}