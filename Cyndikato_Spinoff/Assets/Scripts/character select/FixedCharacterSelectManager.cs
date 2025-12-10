using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class FixedCharacterSelectManager : MonoBehaviour
{
    [Header("UI References")]
    public PlayerPlatformUI[] playerPlatforms; // 4 platform UIs
    public CharacterGridUI characterGrid;
    public TextMeshProUGUI instructionText;
    [Tooltip("Shown when players can join")] public TextMeshProUGUI joinPromptText;

    [Header("Character Data")]
    public CharacterSelectData[] availableCharacters;
    [Tooltip("Prefabs matching availableCharacters by index (0..n-1)")]
    public GameObject[] characterPrefabs;

    [Header("Settings")]
    public int maxPlayers = 4;
    public string gameSceneName = "GameplayScene";
    public string mapSelectionSceneName = "MapSelectionScene";
    [Range(0.5f, 5f)]
    public float autoProgressDelay = 2f;
    [Tooltip("Allow Player 1 to use keyboard if no controllers are connected.")]
    public bool allowPlayer1KeyboardFallback = true;

    [Header("Auto-Progression Settings")]
    public bool enableAutoProgression = true;
    public TextMeshProUGUI autoProgressText;
    
    private bool isAutoProgressing = false;
    private float autoProgressTimer = 0f;

    [Header("Player Selection Colors")]
    public Color[] playerSelectionColors = new Color[] 
    {
        Color.red, Color.blue, Color.green, Color.yellow
    };

    private PlayerCharacterData[] players;
    private int currentActivePlayer = 0;
    
    private Dictionary<Gamepad, int> padToPlayer = new Dictionary<Gamepad, int>();
    private Keyboard keyboard;
    private int keyboardPlayerIndex = -1;
    private bool keyboardJoined = false;

    public static FixedCharacterSelectManager Instance;
    private float[] navCooldownX = new float[4];
    private float[] navCooldownY = new float[4];
    [Range(0.05f, 0.5f)] public float navRepeatDelay = 0.25f;

    private HashSet<Gamepad> padsJoinedThisFrame = new HashSet<Gamepad>();
    private bool keyboardJoinedThisFrameFlag = false;
    private bool hasTransitionedToMapSelection = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializePlayers();
    }

    void Start()
    {
        if (Instance != this) return;
        
        keyboard = Keyboard.current;
        for (int i = 0; i < 4; i++) { navCooldownX[i] = 0f; navCooldownY[i] = 0f; }
        
        if (!ValidateSetup()) return;

        if (characterGrid != null)
        {
            characterGrid.ValidateAndFixPlayerState();
            characterGrid.InitializeGrid(availableCharacters);
        }
        else
        {
            Debug.LogError("CharacterGridUI is not assigned!");
            return;
        }

        if (players == null) InitializePlayers();
        
        for (int i = 0; i < maxPlayers; i++)
        {
            if (players[i] != null)
            {
                players[i].isJoined = false;
                players[i].hasLockedCharacter = false;
                players[i].lockedCharacter = null;
                players[i].selectedCharacterIndex = 0;
            }
        }

        AutoJoinConnectedDevices();
        UpdateJoinPrompt();
        UpdateUI();
    }

    void OnEnable() { InputSystem.onDeviceChange += OnDeviceChange; }
    void OnDisable() { InputSystem.onDeviceChange -= OnDeviceChange; }
    void OnDestroy() { InputSystem.onDeviceChange -= OnDeviceChange; }

    // Expose input mapping for MapSelectionManager
    public Dictionary<Gamepad, int> GetPadMappingsSnapshot()
    {
        return new Dictionary<Gamepad, int>(padToPlayer);
    }

    public bool IsKeyboardJoined() => keyboardJoined;
    public int GetKeyboardPlayerIndex() => keyboardPlayerIndex;

    void Update()
    {
        HandlePerPlayerInput();
        
        if (enableAutoProgression) HandleAutoProgression();

        if (padsJoinedThisFrame.Count > 0) padsJoinedThisFrame.Clear();
        if (keyboardJoinedThisFrameFlag) keyboardJoinedThisFrameFlag = false;
    }
    
    private void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            if (change == UnityEngine.InputSystem.InputDeviceChange.Removed || 
                change == UnityEngine.InputSystem.InputDeviceChange.Disconnected)
            {
                var gp = device as Gamepad;
                if (gp != null && padToPlayer.TryGetValue(gp, out int pIndex))
                {
                    padToPlayer.Remove(gp);
                    if (pIndex >= 0 && pIndex < maxPlayers)
                    {
                        players[pIndex].isJoined = false;
                    }
                    UpdateUI();
                }
            }
            AutoJoinConnectedDevices();
            UpdateJoinPrompt();
        }
    }

    void AutoJoinConnectedDevices()
    {
        var existing = new List<Gamepad>(padToPlayer.Keys);
        foreach (var gp in existing)
        {
            bool stillConnected = false;
            foreach (var p in Gamepad.all)
            {
                if (ReferenceEquals(p, gp)) { stillConnected = true; break; }
            }
            if (!stillConnected)
            {
                int idx = padToPlayer[gp];
                padToPlayer.Remove(gp);
                if (idx >= 0 && idx < maxPlayers) players[idx].isJoined = false;
            }
        }
        UpdateUI();
    }

    void HandlePerPlayerInput()
    {
        HandleJoins();
        
        foreach (var kv in padToPlayer)
        {
            var pad = kv.Key;
            int pIndex = kv.Value;
            if (!players[pIndex].isJoined) continue;

            if (padsJoinedThisFrame.Contains(pad)) continue;

            if (isAutoProgressing)
            {
                if (GetSubmitFromPad(pad)) { ProceedToMapSelectionImmediately(); return; }
                if (GetCancelFromPad(pad)) { UnlockCharacterSelection(pIndex); return; }
                continue;
            }

            var nav = GetNavigationFromPad(pIndex, pad);
            if (nav != Vector2.zero) OnNavigate(nav, pIndex);
            if (GetSubmitFromPad(pad)) OnSubmit(pIndex);
            if (GetCancelFromPad(pad)) OnCancel(pIndex);
        }

        if (allowPlayer1KeyboardFallback && keyboardJoined && keyboard != null && keyboardPlayerIndex >= 0)
        {
            if (keyboardJoinedThisFrameFlag) return;
            
            if (isAutoProgressing)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) 
                { 
                    ProceedToMapSelectionImmediately(); 
                    return; 
                }
                if (keyboard.escapeKey.wasPressedThisFrame) 
                { 
                    UnlockCharacterSelection(keyboardPlayerIndex); 
                    return; 
                }
                return;
            }

            Vector2 nav = Vector2.zero;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) nav = Vector2.left;
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) nav = Vector2.right;
            else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) nav = Vector2.up;
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) nav = Vector2.down;
            if (nav != Vector2.zero) OnNavigate(nav, keyboardPlayerIndex);

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) OnSubmit(keyboardPlayerIndex);
            if (keyboard.escapeKey.wasPressedThisFrame) OnCancel(keyboardPlayerIndex);
        }
    }

    void HandleJoins()
    {
        foreach (var pad in Gamepad.all)
        {
            if (padToPlayer.ContainsKey(pad)) continue;
            if (AnyPadButtonPressed(pad))
            {
                int next = GetNextAvailablePlayerIndex();
                if (next != -1) JoinPlayerForPad(next, pad);
            }
        }

        if (allowPlayer1KeyboardFallback && !keyboardJoined && keyboard != null)
        {
            if (KeyboardAnyJoinPressed(keyboard))
            {
                int next = GetNextAvailablePlayerIndex();
                if (next != -1) JoinPlayerForKeyboard(next);
            }
        }

        UpdateJoinPrompt();
    }

    int GetNextAvailablePlayerIndex()
    {
        for (int i = 0; i < maxPlayers; i++)
        {
            if (!players[i].isJoined) return i;
        }
        return -1;
    }

    void JoinPlayerForPad(int playerIndex, Gamepad pad)
    {
        players[playerIndex].isJoined = true;
        players[playerIndex].selectedCharacterIndex = 0;
        padToPlayer[pad] = playerIndex;
        padsJoinedThisFrame.Add(pad);
        if (characterGrid != null)
        {
            characterGrid.InitializePlayerSelection(playerIndex, playerSelectionColors[playerIndex]);
        }
        UpdateUI();
    }

    void JoinPlayerForKeyboard(int playerIndex)
    {
        players[playerIndex].isJoined = true;
        players[playerIndex].selectedCharacterIndex = 0;
        keyboardPlayerIndex = playerIndex;
        keyboardJoined = true;
        keyboardJoinedThisFrameFlag = true;
        if (characterGrid != null)
        {
            characterGrid.InitializePlayerSelection(playerIndex, playerSelectionColors[playerIndex]);
        }
        UpdateUI();
    }

    bool AnyPadButtonPressed(Gamepad pad)
    {
        return pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
               pad.buttonNorth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame ||
               pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame ||
               pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame ||
               pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame ||
               pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame ||
               pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame;
    }

    bool KeyboardAnyJoinPressed(Keyboard kb)
    {
        return kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.anyKey.wasPressedThisFrame;
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

    bool GetSubmitFromPad(Gamepad pad) => pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
    bool GetCancelFromPad(Gamepad pad) => pad.buttonEast.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame;

    void InitializePlayers()
    {
        if (players == null) players = new PlayerCharacterData[maxPlayers];
        
        for (int i = 0; i < maxPlayers; i++)
        {
            if (players[i] == null) players[i] = new PlayerCharacterData(i);
        }
    }

    void OnNavigate(Vector2 input, int playerIndex)
    {
        if (characterGrid == null) return;
        if (players == null) { InitializePlayers(); return; }
        if (playerIndex < 0 || playerIndex >= players.Length) return;
        if (players[playerIndex] == null) { players[playerIndex] = new PlayerCharacterData(playerIndex); return; }

        if (players[playerIndex].isJoined && !players[playerIndex].hasLockedCharacter)
        {
            currentActivePlayer = playerIndex;
            characterGrid.Navigate(input, playerIndex);
            
            int newSelectedIndex = characterGrid.GetPlayerSelectedIndex(playerIndex);
            if (newSelectedIndex >= 0) players[playerIndex].selectedCharacterIndex = newSelectedIndex;
            
            UpdateUI();
        }
    }

    void OnSubmit(int playerIndex)
    {
        if (players[playerIndex].isJoined && !players[playerIndex].hasLockedCharacter)
        {
            LockCharacterSelection(playerIndex);
        }
    }

    void OnCancel(int playerIndex)
    {
        if (playerIndex > 0 && players[playerIndex].isJoined)
        {
            if (players[playerIndex].hasLockedCharacter)
            {
                UnlockCharacterSelection(playerIndex);
            }
            else
            {
                RemovePlayer(playerIndex);
            }
        }
    }

    void LockCharacterSelection(int playerIndex)
    {
        if (players[playerIndex].isJoined && !players[playerIndex].hasLockedCharacter)
        {
            if (players[playerIndex].selectedCharacterIndex >= 0 && players[playerIndex].selectedCharacterIndex < availableCharacters.Length)
            {
                CharacterSelectData selectedCharacter = availableCharacters[players[playerIndex].selectedCharacterIndex];

                if (selectedCharacter != null)
                {
                    players[playerIndex].hasLockedCharacter = true;
                    players[playerIndex].lockedCharacter = selectedCharacter;

                    GameObject prefab = null;
                    int sel = players[playerIndex].selectedCharacterIndex;
                    if (characterPrefabs != null && sel >= 0 && sel < characterPrefabs.Length)
                    {
                        prefab = characterPrefabs[sel];
                    }
                    var device = GetDeviceForPlayer(playerIndex);
                    CharacterSelectionState.SetSelection(playerIndex, prefab, device);

                    UpdateUI();
                    CheckForAutoProgression();
                }
            }
        }
    }

    void UnlockCharacterSelection(int playerIndex)
    {
        if (players[playerIndex].hasLockedCharacter)
        {
            players[playerIndex].hasLockedCharacter = false;
            players[playerIndex].lockedCharacter = null;

            currentActivePlayer = playerIndex;
            characterGrid.SetControllingPlayer(currentActivePlayer);
            UpdateUI();
        }
    }
    
    void CheckForAutoProgression()
    {
        if (enableAutoProgression && CheckAllJoinedPlayersLocked())
        {
            Debug.Log("All joined players have locked their characters!");
        }
    }
    
    void RemovePlayer(int playerIndex)
    {
        if (playerIndex > 0)
        {
            Gamepad padToRemove = null;
            foreach (var kv in padToPlayer)
            {
                if (kv.Value == playerIndex) { padToRemove = kv.Key; break; }
            }
            if (padToRemove != null) padToPlayer.Remove(padToRemove);
            if (keyboardPlayerIndex == playerIndex)
            {
                keyboardPlayerIndex = -1;
                keyboardJoined = false;
            }
            
            players[playerIndex].isJoined = false;
            players[playerIndex].hasLockedCharacter = false;
            players[playerIndex].lockedCharacter = null;

            SwitchActivePlayer(-1);
            UpdateUI();
            UpdateJoinPrompt();
        }
    }

    UnityEngine.InputSystem.InputDevice GetDeviceForPlayer(int playerIndex)
    {
        foreach (var kv in padToPlayer)
        {
            if (kv.Value == playerIndex) return kv.Key;
        }
        if (allowPlayer1KeyboardFallback && keyboardPlayerIndex == playerIndex && keyboard != null)
        {
            return keyboard;
        }
        return null;
    }

    void SwitchActivePlayer(int direction)
    {
        int startIndex = currentActivePlayer;
        int attempts = 0;

        do
        {
            currentActivePlayer = (currentActivePlayer + direction + maxPlayers) % maxPlayers;
            attempts++;
        }
        while (!players[currentActivePlayer].isJoined && attempts < maxPlayers);

        if (!players[currentActivePlayer].isJoined) currentActivePlayer = 0;

        characterGrid.SetControllingPlayer(players[currentActivePlayer].isJoined && !players[currentActivePlayer].hasLockedCharacter ? currentActivePlayer : -1);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (players == null || players.Length < maxPlayers) InitializePlayers();

        int platformCount = (playerPlatforms != null) ? playerPlatforms.Length : 0;
        int loopCount = Mathf.Min(maxPlayers, players != null ? players.Length : 0, platformCount);

        for (int i = 0; i < loopCount; i++)
        {
            var platform = playerPlatforms[i];
            if (platform == null) continue;

            var pdata = players[i];
            CharacterSelectData selectedChar = null;
            if (pdata != null && availableCharacters != null && availableCharacters.Length > 0)
            {
                int sel = pdata.selectedCharacterIndex;
                if (sel >= 0 && sel < availableCharacters.Length)
                    selectedChar = availableCharacters[sel];
            }

            bool isActiveForThisPlayer = pdata != null && pdata.isJoined && !pdata.hasLockedCharacter;
            platform.UpdatePlatform(pdata, selectedChar, isActiveForThisPlayer);
        }

        if (characterGrid != null && players != null)
        {
            characterGrid.UpdateAllPlayerSelections(players, playerSelectionColors);
        }

        if (instructionText != null) UpdateInstructions();
    }

    void UpdateInstructions()
    {
        int joinedCount = 0;
        int lockedCount = 0;

        for (int i = 0; i < maxPlayers; i++)
        {
            if (players[i].isJoined) joinedCount++;
            if (players[i].hasLockedCharacter) lockedCount++;
        }

        string instructions = $"Players Joined: {joinedCount}/4 | Locked: {lockedCount}\n";
        
        if (isAutoProgressing)
        {
            instructions += "? ALL PLAYERS READY! PROCEEDING TO MAP SELECTION...\n";
        }
        else if (CheckAllJoinedPlayersLocked())
        {
            instructions += "? All players ready! Auto-progressing soon...\n";
        }
        else
        {
            string activePlayersList = "";
            for (int i = 0; i < maxPlayers; i++)
            {
                if (players[i].isJoined && !players[i].hasLockedCharacter)
                {
                    Color playerColor = playerSelectionColors[i];
                    string colorName = GetColorName(playerColor);
                    activePlayersList += $"P{i + 1}({colorName}) ";
                }
            }
            
            if (!string.IsNullOrEmpty(activePlayersList))
                instructions += $"Selecting: {activePlayersList}\n";
        }
        
        instructions += "ALL PLAYERS CAN SELECT SIMULTANEOUSLY!\n";
        instructions += "KEYBOARD: WASD/Arrows = Navigate | Enter/Space = Lock | Esc = Cancel\n";
        instructions += "CONTROLLER: D-Pad/Stick = Navigate | A/Start = Lock | B/Back = Cancel/Leave\n";

        if (joinedCount < maxPlayers) instructions += "Connect controllers and press any button to join!";
        
        if (enableAutoProgression && joinedCount > 0)
            instructions += $"\n?? When all players lock characters, auto-proceed to Map Selection in {autoProgressDelay}s";

        instructionText.text = instructions;
    }
    
    string GetColorName(Color color)
    {
        if (color == Color.red) return "Red";
        if (color == Color.blue) return "Blue";
        if (color == Color.green) return "Green";
        if (color == Color.yellow) return "Yellow";
        return "Color";
    }

    void UpdateJoinPrompt()
    {
        if (joinPromptText == null) return;
        int joinedCount = 0;
        for (int i = 0; i < maxPlayers; i++) if (players[i].isJoined) joinedCount++;

        bool canJoinMore = joinedCount < maxPlayers;
        joinPromptText.gameObject.SetActive(canJoinMore);
        if (canJoinMore) joinPromptText.text = "Press any button to join";
    }
    
    public void ToggleAutoProgression(bool enabled)
    {
        enableAutoProgression = enabled;
        if (!enabled) CancelAutoProgression();
    }
    
    public void SetAutoProgressDelay(float delay)
    {
        autoProgressDelay = Mathf.Max(0.5f, delay);
    }
    
    void HandleAutoProgression()
    {
        bool allJoinedPlayersLocked = CheckAllJoinedPlayersLocked();
        
        if (allJoinedPlayersLocked && !isAutoProgressing)
        {
            StartAutoProgression();
        }
        else if (!allJoinedPlayersLocked && isAutoProgressing)
        {
            CancelAutoProgression();
        }
        
        if (isAutoProgressing)
        {
            autoProgressTimer -= Time.deltaTime;
            
            if (autoProgressTimer <= 0f)
            {
                ProceedToMapSelection();
            }
            else
            {
                UpdateAutoProgressUI();
            }
        }
    }
    
    bool CheckAllJoinedPlayersLocked()
    {
        int joinedPlayerCount = 0;
        int lockedPlayerCount = 0;
        
        for (int i = 0; i < maxPlayers; i++)
        {
            if (players[i].isJoined)
            {
                joinedPlayerCount++;
                if (players[i].hasLockedCharacter) lockedPlayerCount++;
            }
        }
        
        return joinedPlayerCount > 0 && joinedPlayerCount == lockedPlayerCount;
    }
    
    void StartAutoProgression()
    {
        isAutoProgressing = true;
        autoProgressTimer = autoProgressDelay;
        UpdateAutoProgressUI();
    }
    
    void CancelAutoProgression()
    {
        isAutoProgressing = false;
        autoProgressTimer = 0f;
        
        if (autoProgressText != null) autoProgressText.gameObject.SetActive(false);
    }
    
    void UpdateAutoProgressUI()
    {
        if (autoProgressText != null)
        {
            autoProgressText.gameObject.SetActive(true);
            int secondsLeft = Mathf.CeilToInt(autoProgressTimer);
            autoProgressText.text = $"All players ready! Proceeding to Map Selection in {secondsLeft}...";
            
            if (secondsLeft <= 1) autoProgressText.color = Color.red;
            else if (secondsLeft <= 2) autoProgressText.color = Color.yellow;
            else autoProgressText.color = Color.white;
        }
    }
    
    void ProceedToMapSelection()
    {
        if (hasTransitionedToMapSelection) return;
        hasTransitionedToMapSelection = true;
        
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetSelectedCharacters(players);
        }
        
        if (Time.timeScale != 1f) Time.timeScale = 1f;

        InputSystem.onDeviceChange -= OnDeviceChange;

        try { DontDestroyOnLoad(gameObject); } catch {}
        Destroy(gameObject);

        SceneManager.LoadScene(mapSelectionSceneName);
    }
    
    void ProceedToMapSelectionImmediately()
    {
        isAutoProgressing = false;
        ProceedToMapSelection();
    }

    public PlayerCharacterData[] GetPlayerData() => players;

    bool ValidateSetup()
    {
        if (availableCharacters == null || availableCharacters.Length == 0) return false;
        if (playerPlatforms == null || playerPlatforms.Length == 0) return false;
        if (playerPlatforms.Length < maxPlayers) return false;
        if (characterGrid == null) return false;
        return true;
    }
}