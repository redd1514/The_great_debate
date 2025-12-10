using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Map Selection Manager that handles voting for maps after character selection.
/// Works with the auto-progression system from NewCharacterSelectManager.
/// Supports multi-player input using the same device system as character selection.
/// Enhanced with a timer that auto-selects the most voted map when it expires
/// or proceeds immediately when all joined players lock their votes.
/// </summary>
public class MapSelectionManager : MonoBehaviour
{
    [Header("Map Data")]
    public MapData[] availableMaps;

    [Header("UI References")]
    public Transform mapGridParent;
    public GameObject mapIconPrefab;
    public GameObject mapSelectionIndicatorPrefab;
    public Transform selectionIndicatorContainer;

    [Header("Timer UI")]
    public TextMeshProUGUI timerText;

    [Header("Grid Settings")]
    public int mapsPerRow = 3;

    [Header("Voting Settings")]
    [Range(5f, 60f)] public float votingTimeLimit = 15f;
    public bool requireUnanimousVote = false; // Reserved if needed later
    [Range(3f, 10f)] public float countdownWarningTime = 5f;

    [Header("Scene Settings")]
    public string gameplaySceneName = "GameplayScene";
    public string[] mapSceneNames = { "Map1Scene", "Map2Scene", "Map3Scene", "Map4Scene" };

    [Header("Player Vote Colors")]
    public Color[] playerVoteColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

    [Header("Canvas Setup (Auto-detected if empty)")]
    public Canvas mainCanvas;
    public Transform uiContainer;

    // Runtime state
    private PlayerCharacterData[] joinedPlayers;
    private Dictionary<int, int> playerVotes = new Dictionary<int, int>();
    private Dictionary<int, bool> playerVoteLocked = new Dictionary<int, bool>();
    private Dictionary<int, int> playerCurrentSelection = new Dictionary<int, int>();
    private float votingTimer;
    private bool votingActive = true;
    private GameObject[] mapIcons;
    private GameObject[] playerSelectionIndicators;

    // Input mapping (re-using mappings from NewCharacterSelectManager)
    private Dictionary<Gamepad, int> padToPlayer = new Dictionary<Gamepad, int>();
    private Keyboard keyboard;
    private int keyboardPlayerIndex = -1;
    private bool keyboardJoined = false;

    // Navigation repeat control per player
    private float[] navCooldownX = new float[4];
    private float[] navCooldownY = new float[4];
    [Range(0.05f, 0.5f)] public float navRepeatDelay = 0.25f;

    void Start()
    {
        ValidateCanvasHierarchy();

        if (availableMaps == null || availableMaps.Length == 0)
            CreateTestMaps();

        if (GameDataManager.Instance != null)
            joinedPlayers = GameDataManager.Instance.GetSelectedCharacters();
        else
            CreateDummyPlayerData();

        SetupDeviceMapping();
        InitializeMapSelection();
        InitializeTimer();
    }

    void Update()
    {
        if (!votingActive) return;
        HandleVotingTimer();
        HandlePerPlayerInput();
    }

    // Canvas and UI hierarchy helpers
    void ValidateCanvasHierarchy()
    {
        if (mainCanvas == null)
            mainCanvas = Object.FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
            CreateMainCanvas();

        ConfigureCanvas();
        SetupUIContainerHierarchy();
        EnsureEventSystem();
    }

    void CreateMainCanvas()
    {
        GameObject canvasObj = new GameObject("MapSelectionCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    void ConfigureCanvas()
    {
        if (mainCanvas == null) return;
        mainCanvas.gameObject.SetActive(true);
        mainCanvas.enabled = true;

        if (mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
            mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    void SetupUIContainerHierarchy()
    {
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

                RectTransform containerRect = containerObj.AddComponent<RectTransform>();
                containerRect.anchorMin = Vector2.zero;
                containerRect.anchorMax = Vector2.one;
                containerRect.offsetMin = Vector2.zero;
                containerRect.offsetMax = Vector2.zero;
            }
        }

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

                RectTransform gridRect = gridObj.AddComponent<RectTransform>();
                gridRect.anchorMin = new Vector2(0.1f, 0.3f);
                gridRect.anchorMax = new Vector2(0.9f, 0.8f);
                gridRect.offsetMin = Vector2.zero;
                gridRect.offsetMax = Vector2.zero;

                GridLayoutGroup gridLayout = gridObj.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(200, 200);
                gridLayout.spacing = new Vector2(20, 20);
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = Mathf.Max(1, mapsPerRow);
                gridLayout.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        if (selectionIndicatorContainer == null)
            selectionIndicatorContainer = uiContainer;
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
    }

    // Data setup
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
    }

    void CreateDummyPlayerData()
    {
        joinedPlayers = new PlayerCharacterData[4];
        for (int i = 0; i < 4; i++)
        {
            joinedPlayers[i] = new PlayerCharacterData(i);
            if (i == 0)
            {
                joinedPlayers[i].isJoined = true;
                joinedPlayers[i].hasLockedCharacter = true;
                joinedPlayers[i].selectedCharacterIndex = 0;
            }
        }
    }

    void SetupDeviceMapping()
    {
        padToPlayer.Clear();
        keyboard = Keyboard.current;
        for (int i = 0; i < 4; i++) { navCooldownX[i] = 0f; navCooldownY[i] = 0f; }

        Debug.Log($"MapSelectionManager: Setting up device mapping...");
        Debug.Log($"MapSelectionManager: NewCharacterSelectManager.Instance = {(NewCharacterSelectManager.Instance != null ? "Found" : "NULL")}");

        if (NewCharacterSelectManager.Instance != null)
        {
            var snapshot = NewCharacterSelectManager.Instance.GetPadMappingsSnapshot();
            Debug.Log($"MapSelectionManager: Got {snapshot.Count} pad mappings from character select");
            
            foreach (var kv in snapshot)
            {
                int pIndex = kv.Value;
                Debug.Log($"MapSelectionManager: Pad {kv.Key.displayName} -> Player {pIndex + 1}, IsPlayerJoined: {IsPlayerJoined(pIndex)}");
                if (IsPlayerJoined(pIndex)) 
                {
                    padToPlayer[kv.Key] = pIndex;
                    Debug.Log($"MapSelectionManager: Added pad mapping for Player {pIndex + 1}");
                }
            }

            keyboardJoined = NewCharacterSelectManager.Instance.IsKeyboardJoined();
            keyboardPlayerIndex = NewCharacterSelectManager.Instance.GetKeyboardPlayerIndex();
            Debug.Log($"MapSelectionManager: Keyboard joined: {keyboardJoined}, Player index: {keyboardPlayerIndex}");
            
            if (keyboardJoined && (keyboardPlayerIndex < 0 || !IsPlayerJoined(keyboardPlayerIndex)))
            {
                Debug.LogWarning($"MapSelectionManager: Keyboard player {keyboardPlayerIndex} not joined, disabling keyboard");
                keyboardJoined = false;
                keyboardPlayerIndex = -1;
            }
        }
        else
        {
            Debug.LogWarning("MapSelectionManager: NewCharacterSelectManager.Instance is null, creating fallback device mapping");
            List<int> joined = new List<int>();
            for (int i = 0; i < (joinedPlayers != null ? joinedPlayers.Length : 0); i++)
                if (IsPlayerJoined(i)) joined.Add(i);

            Debug.Log($"MapSelectionManager: Found {joined.Count} joined players for fallback mapping");

            int idx = 0;
            foreach (var pad in Gamepad.all)
            {
                if (idx >= joined.Count) break;
                padToPlayer[pad] = joined[idx];
                Debug.Log($"MapSelectionManager: Fallback - Assigned {pad.displayName} to Player {joined[idx] + 1}");
                idx++;
            }

            if (joined.Count > 0)
            {
                keyboardJoined = true;
                keyboardPlayerIndex = joined[0];
                Debug.Log($"MapSelectionManager: Fallback - Assigned keyboard to Player {keyboardPlayerIndex + 1}");
            }
        }

        playerCurrentSelection.Clear();
        playerVoteLocked.Clear();
        int deviceMappings = 0;
        for (int i = 0; i < (joinedPlayers != null ? joinedPlayers.Length : 0); i++)
        {
            if (IsPlayerJoined(i)) 
            { 
                playerCurrentSelection[i] = 0; 
                playerVoteLocked[i] = false;
                deviceMappings++;
            }
        }
        
        Debug.Log($"MapSelectionManager: Device mapping complete. Active mappings: {padToPlayer.Count} pads + {(keyboardJoined ? 1 : 0)} keyboard = {deviceMappings} total players");

        StartCoroutine(DelayedInitialSelectionDisplay());
    }

    IEnumerator DelayedInitialSelectionDisplay()
    {
        yield return new WaitForSeconds(0.5f);
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;
        foreach (var kvp in playerCurrentSelection)
        {
            int p = kvp.Key;
            if (playerSelectionIndicators != null && p < playerSelectionIndicators.Length && playerSelectionIndicators[p] != null)
            {
                playerSelectionIndicators[p].SetActive(true);
                var indicator = playerSelectionIndicators[p].GetComponent<MapSelectionIndicator>();
                if (indicator != null)
                {
                    indicator.SetPlayer(p);
                    indicator.SetPlayerColor(playerVoteColors[p]);
                    indicator.SetVisible(true);
                    indicator.SetSelectionState(true);
                    indicator.StartAnimation();
                }
                UpdatePlayerSelectionDisplay(p);
            }
        }
    }

    // Initialization
    void InitializeMapSelection()
    {
        CreateMapIcons();
        CreatePlayerSelectionIndicators();
        UpdateAllSelectionDisplays();
        UpdateUI();
        votingActive = true;
    }

    void InitializeTimer()
    {
        votingTimer = votingTimeLimit;
        UpdateTimerDisplay();
    }

    // Map icons
    void CreateMapIcons()
    {
        if (availableMaps == null || availableMaps.Length == 0)
        {
            Debug.LogError("MapSelectionManager: No maps available!");
            return;
        }

        mapIcons = new GameObject[availableMaps.Length];
        for (int i = 0; i < availableMaps.Length; i++)
        {
            GameObject mapIcon = mapIconPrefab != null ? Instantiate(mapIconPrefab, mapGridParent)
                                                       : CreateDefaultMapIcon(i);
            mapIcon.name = $"MapIcon_{i}_{(availableMaps[i] != null ? availableMaps[i].GetDisplayName() : "Map")}";
            SetupMapIconImage(mapIcon, i);
            SetupMapNameText(mapIcon, i);
            mapIcons[i] = mapIcon;
        }

        StartCoroutine(ForceCanvasUpdateNextFrame());
    }

    IEnumerator ForceCanvasUpdateNextFrame()
    {
        yield return null; Canvas.ForceUpdateCanvases();
        yield return null; Canvas.ForceUpdateCanvases();
        yield return null; Canvas.ForceUpdateCanvases();
    }

    GameObject CreateDefaultMapIcon(int index)
    {
        GameObject go = new GameObject($"MapIcon_{index + 1}");
        go.transform.SetParent(mapGridParent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 200);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        img.raycastTarget = true;

        GameObject labelObj = new GameObject("Name");
        labelObj.transform.SetParent(go.transform, false);
        var nameRect = labelObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0f);
        nameRect.anchorMax = new Vector2(0.5f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0f, -24f);
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28;
        tmp.text = availableMaps != null && index < availableMaps.Length && availableMaps[index] != null
            ? availableMaps[index].GetDisplayName()
            : $"Map {index + 1}";

        return go;
    }

    void SetupMapIconImage(GameObject mapIcon, int i)
    {
        // Placeholder: If MapData exposes a sprite/thumbnail later, assign it here.
        var img = mapIcon.GetComponent<Image>();
        if (img == null) img = mapIcon.AddComponent<Image>();
        img.color = img.color == default ? new Color(0.25f, 0.25f, 0.25f, 1f) : img.color;
    }

    void SetupMapNameText(GameObject mapIcon, int i)
    {
        // Ensure a TMP label exists and shows the map name
        TextMeshProUGUI tmp = mapIcon.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
        {
            GameObject labelObj = new GameObject("Name");
            labelObj.transform.SetParent(mapIcon.transform, false);
            var nameRect = labelObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, -24f);
            tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28;
        }

        string mapName = (availableMaps != null && i < availableMaps.Length && availableMaps[i] != null)
            ? availableMaps[i].GetDisplayName()
            : $"Map {i + 1}";
        tmp.text = mapName;
    }

    // Player indicators
    void CreatePlayerSelectionIndicators()
    {
        playerSelectionIndicators = new GameObject[4];
        Transform indicatorParent = selectionIndicatorContainer != null ? selectionIndicatorContainer : uiContainer;
        for (int i = 0; i < 4; i++)
        {
            GameObject indicator = CreateMapIndicator(i, indicatorParent);
            indicator.name = $"Player{i + 1}MapSelectionIndicator";
            var comp = indicator.GetComponent<MapSelectionIndicator>();
            if (comp != null)
            {
                comp.UpdatePlayerColors(playerVoteColors);
                comp.SetPlayer(i);
                comp.SetGradientStyle(MapGradientStyle.TekkenClassic);
            }
            indicator.SetActive(false);
            playerSelectionIndicators[i] = indicator;
        }
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
            indicator = new GameObject($"Player{playerIndex + 1}MapIndicator");
            indicator.transform.SetParent(parent, false);
            var ind = indicator.AddComponent<MapSelectionIndicator>();
            RectTransform rect = indicator.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 200);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            ind.SetPlayer(playerIndex);
            ind.SetGradientStyle(MapGradientStyle.TekkenClassic);
        }
        return indicator;
    }

    // Input handling
    void HandlePerPlayerInput()
    {
        foreach (var kv in padToPlayer)
        {
            var pad = kv.Key;
            int pIndex = kv.Value;
            if (!IsPlayerJoined(pIndex)) continue;
            if (playerVoteLocked.ContainsKey(pIndex) && playerVoteLocked[pIndex]) continue;

            var nav = GetNavigationFromPad(pIndex, pad);
            if (nav != Vector2.zero) NavigatePlayer(nav, pIndex);
            if (GetSubmitFromPad(pad)) LockPlayerVote(pIndex);
            if (GetCancelFromPad(pad)) UnlockPlayerVote(pIndex);
        }

        if (keyboardJoined && keyboard != null && keyboardPlayerIndex >= 0 && IsPlayerJoined(keyboardPlayerIndex))
        {
            if (!(playerVoteLocked.ContainsKey(keyboardPlayerIndex) && playerVoteLocked[keyboardPlayerIndex]))
            {
                Vector2 nav = Vector2.zero;
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) nav = Vector2.left;
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) nav = Vector2.right;
                else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) nav = Vector2.up;
                else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) nav = Vector2.down;

                if (nav != Vector2.zero) NavigatePlayer(nav, keyboardPlayerIndex);
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) LockPlayerVote(keyboardPlayerIndex);
                if (keyboard.escapeKey.wasPressedThisFrame) UnlockPlayerVote(keyboardPlayerIndex);
            }
        }
    }

    Vector2 GetNavigationFromPad(int playerIndex, Gamepad pad)
    {
        if (pad.dpad.left.wasPressedThisFrame) return Vector2.left;
        if (pad.dpad.right.wasPressedThisFrame) return Vector2.right;
        if (pad.dpad.up.wasPressedThisFrame) return Vector2.up;
        if (pad.dpad.down.wasPressedThisFrame) return Vector2.down;

        Vector2 nav = Vector2.zero;
        float h = pad.leftStick.x.ReadValue();
        float v = pad.leftStick.y.ReadValue();
        navCooldownX[playerIndex] = Mathf.Max(0f, navCooldownX[playerIndex] - Time.unscaledDeltaTime);
        navCooldownY[playerIndex] = Mathf.Max(0f, navCooldownY[playerIndex] - Time.unscaledDeltaTime);

        const float threshold = 0.6f;
        if (Mathf.Abs(h) > threshold && navCooldownX[playerIndex] <= 0f)
        {
            nav.x = h > 0 ? 1f : -1f;
            navCooldownX[playerIndex] = navRepeatDelay;
        }
        if (Mathf.Abs(v) > threshold && navCooldownY[playerIndex] <= 0f)
        {
            nav.y = v > 0 ? 1f : -1f;
            navCooldownY[playerIndex] = navRepeatDelay;
        }
        return nav;
    }

    bool GetSubmitFromPad(Gamepad pad) { return pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame; }
    bool GetCancelFromPad(Gamepad pad) { return pad.buttonEast.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame; }

    // Navigation and selection
    void NavigatePlayer(Vector2 input, int playerIndex)
    {
        if (!playerCurrentSelection.ContainsKey(playerIndex)) return;
        int currentSelection = playerCurrentSelection[playerIndex];
        int newSelection = currentSelection;

        if (input.x > 0) newSelection = (currentSelection + 1) % availableMaps.Length;
        else if (input.x < 0) newSelection = (currentSelection - 1 + availableMaps.Length) % availableMaps.Length;
        else if (input.y > 0) newSelection = (currentSelection - mapsPerRow + availableMaps.Length) % availableMaps.Length;
        else if (input.y < 0) newSelection = (currentSelection + mapsPerRow) % availableMaps.Length;

        if (newSelection != currentSelection)
        {
            playerCurrentSelection[playerIndex] = newSelection;
            UpdatePlayerSelectionDisplay(playerIndex);
        }
    }

    void UpdatePlayerSelectionDisplay(int playerIndex)
    {
        if (playerSelectionIndicators == null || playerIndex >= playerSelectionIndicators.Length || playerSelectionIndicators[playerIndex] == null)
            return;

        if (!IsPlayerJoined(playerIndex))
        {
            playerSelectionIndicators[playerIndex].SetActive(false);
            return;
        }

        if (!playerCurrentSelection.ContainsKey(playerIndex)) return;
        int mapIndex = playerCurrentSelection[playerIndex];

        if (mapIcons == null || mapIndex < 0 || mapIndex >= mapIcons.Length || mapIcons[mapIndex] == null)
        {
            playerSelectionIndicators[playerIndex].SetActive(false);
            return;
        }

        RectTransform indicatorRect = playerSelectionIndicators[playerIndex].GetComponent<RectTransform>();
        RectTransform mapRect = mapIcons[mapIndex].GetComponent<RectTransform>();

        if (indicatorRect != null && mapRect != null)
        {
            Vector3 mapWorldPos = mapRect.position;
            indicatorRect.position = mapWorldPos;
            indicatorRect.sizeDelta = mapRect.sizeDelta;
        }

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
        }
    }

    void UpdateAllSelectionDisplays()
    {
        foreach (var kvp in playerCurrentSelection)
            UpdatePlayerSelectionDisplay(kvp.Key);
    }

    void LockPlayerVote(int playerIndex)
    {
        if (!playerCurrentSelection.ContainsKey(playerIndex)) return;
        int selectedMap = playerCurrentSelection[playerIndex];
        playerVotes[playerIndex] = selectedMap;
        playerVoteLocked[playerIndex] = true;
        UpdatePlayerSelectionDisplay(playerIndex);
        UpdateUI();
        CheckForConsensus();
    }

    void UnlockPlayerVote(int playerIndex)
    {
        if (playerVoteLocked.ContainsKey(playerIndex) && playerVoteLocked[playerIndex])
        {
            playerVoteLocked[playerIndex] = false;
            if (playerVotes.ContainsKey(playerIndex)) playerVotes.Remove(playerIndex);
            UpdatePlayerSelectionDisplay(playerIndex);
            UpdateUI();
        }
    }

    // Timer and consensus
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
        if (timerText == null) return;
        int timeLeft = Mathf.CeilToInt(votingTimer);
        timerText.text = $"Time: {timeLeft}s";
    }

    void CheckForConsensus()
    {
        int joinedPlayerCount = GetJoinedPlayerCount();
        int lockedVoteCount = GetLockedVoteCount();
        if (lockedVoteCount >= joinedPlayerCount && joinedPlayerCount > 0)
        {
            FinishVotingDueToConsensus();
        }
    }

    void FinishVotingDueToTimeout()
    {
        votingActive = false;
        int winningMapIndex = GetWinningMap();
        if (winningMapIndex < 0) winningMapIndex = 0; // default
        StartCoroutine(ProceedToMapScene(winningMapIndex, "Time Expired"));
    }

    void FinishVotingDueToConsensus()
    {
        votingActive = false;
        int winningMapIndex = GetWinningMap();
        if (winningMapIndex >= 0)
        {
            StartCoroutine(ProceedToMapScene(winningMapIndex, "All Players Ready"));
        }
    }

    int GetWinningMap()
    {
        if (playerVotes.Count == 0) return -1;
        Dictionary<int, int> voteCounts = new Dictionary<int, int>();
        foreach (var vote in playerVotes.Values)
        {
            if (voteCounts.ContainsKey(vote)) voteCounts[vote]++;
            else voteCounts[vote] = 1;
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
            string mapName = availableMaps != null && selectedMapIndex >= 0 && selectedMapIndex < availableMaps.Length && availableMaps[selectedMapIndex] != null
                ? availableMaps[selectedMapIndex].GetDisplayName()
                : $"Map {selectedMapIndex + 1}";
            timerText.text = $"Loading: {mapName}";
        }

        yield return new WaitForSeconds(2f);

        string sceneToLoad = GetSceneForMap(selectedMapIndex);
        SceneManager.LoadScene(sceneToLoad);
    }

    string GetSceneForMap(int mapIndex)
    {
        if (availableMaps != null && mapIndex >= 0 && mapIndex < availableMaps.Length && availableMaps[mapIndex] != null)
        {
            string mapSceneName = availableMaps[mapIndex].GetSceneName();
            if (!string.IsNullOrEmpty(mapSceneName)) return mapSceneName;
        }
        if (mapIndex >= 0 && mapIndex < mapSceneNames.Length) return mapSceneNames[mapIndex];
        return gameplaySceneName;
    }

    void UpdateUI()
    {
        UpdateTimerDisplay();
    }

    // Helpers
    bool IsPlayerJoined(int playerIndex)
    {
        return joinedPlayers != null && playerIndex >= 0 && playerIndex < joinedPlayers.Length &&
               joinedPlayers[playerIndex] != null &&
               joinedPlayers[playerIndex].isJoined &&
               joinedPlayers[playerIndex].hasLockedCharacter;
    }

    int GetJoinedPlayerCount()
    {
        if (joinedPlayers == null) return 0;
        int count = 0;
        for (int i = 0; i < joinedPlayers.Length; i++) if (IsPlayerJoined(i)) count++;
        return count;
    }

    int GetLockedVoteCount()
    {
        int count = 0;
        foreach (var kvp in playerVoteLocked)
            if (IsPlayerJoined(kvp.Key) && kvp.Value) count++;
        return count;
    }

    // Debug helpers
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("=== MapSelectionManager Debug State ===");
        Debug.Log($"Voting Active: {votingActive}");
        Debug.Log($"Device Mappings: {padToPlayer.Count} (pads) + {(keyboardJoined ? 1 : 0)} (keyboard)");
        Debug.Log($"Player Selections: {playerCurrentSelection.Count}");
        Debug.Log($"Map Icons: {mapIcons?.Length}");
        Debug.Log($"Player Indicators: {playerSelectionIndicators?.Length}");
        Debug.Log($"Main Canvas: {(mainCanvas != null ? mainCanvas.name : "NULL")}");
        Debug.Log($"UI Container: {(uiContainer != null ? uiContainer.name : "NULL")}");
        Debug.Log($"Map Grid Parent: {(mapGridParent != null ? mapGridParent.name : "NULL")}");
        foreach (var selection in playerCurrentSelection)
            Debug.Log($"  Player {selection.Key + 1} selecting map {selection.Value}");
    }

    [ContextMenu("Force Show All Indicators")]
    public void ForceShowAllIndicators()
    {
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
            }
        }
    }
}
