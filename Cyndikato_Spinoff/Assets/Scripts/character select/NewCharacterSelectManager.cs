using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class NewCharacterSelectManager : MonoBehaviour
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
    public string mapSelectionSceneName = "MapSelectionScene"; // New field for map selection
    [Range(0.5f, 5f)]
    public float autoProgressDelay = 2f; // Delay before auto-progression
    [Tooltip("Allow Player 1 to use keyboard if no controllers are connected.")]
    public bool allowPlayer1KeyboardFallback = true;

    [Header("Auto-Progression Settings")]
    public bool enableAutoProgression = true; // Allow disabling if needed
    public TextMeshProUGUI autoProgressText; // UI text to show countdown
    
    private bool isAutoProgressing = false;
    private float autoProgressTimer = 0f;

    [Header("Player Selection Colors")]
    public Color[] playerSelectionColors = new Color[] 
    {
        Color.red,      // Player 1
        Color.blue,     // Player 2
        Color.green,    // Player 3
        Color.yellow    // Player 4
    };

    private PlayerCharacterData[] players;
    private int currentActivePlayer = 0; // Keep for UI display purposes
    
    [Header("Input (New Input System)")]
    private Dictionary<Gamepad, int> padToPlayer = new Dictionary<Gamepad, int>();
    private Keyboard keyboard;
    private int keyboardPlayerIndex = -1;
    private bool keyboardJoined = false;

    public static NewCharacterSelectManager Instance;
    private float[] navCooldownX = new float[4];
    private float[] navCooldownY = new float[4];
    [Range(0.05f, 0.5f)] public float navRepeatDelay = 0.25f;

    // Prevent immediate lock on the same frame a device joins
    private HashSet<Gamepad> padsJoinedThisFrame = new HashSet<Gamepad>();
    private bool keyboardJoinedThisFrameFlag = false;
    private bool hasTransitionedToMapSelection = false;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("NewCharacterSelectManager: Created singleton instance");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"NewCharacterSelectManager: Duplicate instance found! Destroying {gameObject.name}, keeping {Instance.gameObject.name}");
            Destroy(gameObject);
            return;
        }

        InitializePlayers();
    }

    void Start()
    {
        // Check if this instance was destroyed in Awake due to duplicate
        if (Instance != this)
        {
            Debug.Log("NewCharacterSelectManager: This instance was destroyed, skipping Start()");
            return;
        }
        
        // CRITICAL: Reset everything when returning from other scenes
        Debug.Log("NewCharacterSelectManager: Starting fresh - resetting all systems");
        
        keyboard = Keyboard.current;
        for (int i = 0; i < 4; i++) { navCooldownX[i] = 0f; navCooldownY[i] = 0f; }
        
        if (!ValidateSetup())
        {
            Debug.LogError("NewCharacterSelectManager: Setup validation failed! Please check the configuration.");
            return;
        }

        if (characterGrid != null)
        {
            // Validate and fix any corrupted state before initializing
            characterGrid.ValidateAndFixPlayerState();
            characterGrid.InitializeGrid(availableCharacters);
        }
        else
        {
            Debug.LogError("NewCharacterSelectManager: CharacterGridUI is not assigned!");
            return;
        }

        // Re-validate players array after potential scene transitions
        if (players == null)
        {
            Debug.LogWarning("NewCharacterSelectManager: Players array is null, re-initializing...");
            InitializePlayers();
        }
        
        // FORCE RESET: Clear any existing player data and restart fresh
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

        // Detect devices; joining happens on button press
        AutoJoinConnectedDevices(); // repurposed to refresh device list only
        UpdateJoinPrompt();
        
        // Force immediate update to ensure everything is properly initialized
        UpdateUI();

        Debug.Log("NewCharacterSelectManager: COMPLETE RESET - All systems reinitialized");
        Debug.Log("New Character Select Started - Keyboard: Player 1 auto-joined | All players can select simultaneously with individual colored indicators!");
    }

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void OnDestroy()
    {
        // Ensure we always detach callbacks
        InputSystem.onDeviceChange -= OnDeviceChange;
        
        // Clear singleton instance if this was the active one
        if (Instance == this)
        {
            Instance = null;
        }
    }

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
        
        // Handle auto-progression to map selection
        if (enableAutoProgression)
        {
            HandleAutoProgression();
        }

        // Clear one-frame join suppression flags
        if (padsJoinedThisFrame.Count > 0) padsJoinedThisFrame.Clear();
        if (keyboardJoinedThisFrameFlag) keyboardJoinedThisFrameFlag = false;
    }
    
    private void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            // If a mapped pad was removed, unjoin the player
            if (change == UnityEngine.InputSystem.InputDeviceChange.Removed || change == UnityEngine.InputSystem.InputDeviceChange.Disconnected)
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
        // Refresh device list and prune mappings for disconnected pads
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
        // Do not auto-join here; joining occurs on button press
        UpdateUI();
    }

    void HandlePerPlayerInput()
    {
        HandleJoins();
        // Iterate joined players and read from their assigned device
        foreach (var kv in padToPlayer)
        {
            var pad = kv.Key;
            int pIndex = kv.Value;
            if (!players[pIndex].isJoined) continue;

            // Skip processing inputs on the first frame a pad joins
            if (padsJoinedThisFrame.Contains(pad))
            {
                continue;
            }

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

        // Keyboard input if joined
        if (allowPlayer1KeyboardFallback && keyboardJoined && keyboard != null && keyboardPlayerIndex >= 0)
        {
            // Skip processing inputs on the first frame the keyboard joins
            if (keyboardJoinedThisFrameFlag)
            {
                // Do not process keyboard inputs this frame
                return;
            }
            if (isAutoProgressing)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) { ProceedToMapSelectionImmediately(); return; }
                if (keyboard.escapeKey.wasPressedThisFrame) { UnlockCharacterSelection(keyboardPlayerIndex); return; }
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
        // Gamepad joins on any button press
        foreach (var pad in Gamepad.all)
        {
            if (padToPlayer.ContainsKey(pad)) continue;
            if (AnyPadButtonPressed(pad))
            {
                int next = GetNextAvailablePlayerIndex();
                if (next != -1)
                {
                    JoinPlayerForPad(next, pad);
                }
            }
        }

        // Keyboard join
        if (allowPlayer1KeyboardFallback && !keyboardJoined && keyboard != null)
        {
            if (KeyboardAnyJoinPressed(keyboard))
            {
                int next = GetNextAvailablePlayerIndex();
                if (next != -1)
                {
                    JoinPlayerForKeyboard(next);
                }
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
        // Debounced D-Pad first
        if (pad.dpad.left.wasPressedThisFrame) return Vector2.left;
        if (pad.dpad.right.wasPressedThisFrame) return Vector2.right;
        if (pad.dpad.up.wasPressedThisFrame) return Vector2.up;
        if (pad.dpad.down.wasPressedThisFrame) return Vector2.down;

        // Stick with simple repeat delay
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

    bool GetSubmitFromPad(Gamepad pad)
    {
        return pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
    }

    bool GetCancelFromPad(Gamepad pad)
    {
        return pad.buttonEast.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame;
    }

    void InitializePlayers()
    {
        if (players == null)
        {
            players = new PlayerCharacterData[maxPlayers];
        }
        
        for (int i = 0; i < maxPlayers; i++)
        {
            if (players[i] == null)
            {
                players[i] = new PlayerCharacterData(i);
            }
        }
        
        Debug.Log("NewCharacterSelectManager: Players array initialized with safety checks");
    }

    void OnNavigate(Vector2 input, int playerIndex)
    {
        if (characterGrid == null)
        {
            Debug.LogWarning("NewCharacterSelectManager: CharacterGridUI is null!");
            return;
        }

        // Safety checks for player data integrity
        if (players == null)
        {
            Debug.LogError("NewCharacterSelectManager: Players array is null! Re-initializing...");
            InitializePlayers();
            return;
        }
        
        if (playerIndex < 0 || playerIndex >= players.Length)
        {
            Debug.LogError($"NewCharacterSelectManager: Invalid playerIndex {playerIndex}");
            return;
        }
        
        if (players[playerIndex] == null)
        {
            Debug.LogError($"NewCharacterSelectManager: Player data at index {playerIndex} is null! Re-initializing...");
            players[playerIndex] = new PlayerCharacterData(playerIndex);
            return;
        }

        // Allow any joined player to navigate regardless of active player
        if (players[playerIndex].isJoined && !players[playerIndex].hasLockedCharacter)
        {
            // Update current active player for UI display (last player who navigated)
            currentActivePlayer = playerIndex;
            
            // Navigate for this specific player
            characterGrid.Navigate(input, playerIndex);
            
            // Update selected character index with safety check
            int newSelectedIndex = characterGrid.GetPlayerSelectedIndex(playerIndex);
            if (newSelectedIndex >= 0)
            {
                players[playerIndex].selectedCharacterIndex = newSelectedIndex;
            }
            
            UpdateUI();
            
            Debug.Log($"Player {playerIndex + 1} navigated to character index: {players[playerIndex].selectedCharacterIndex}");
        }

        // Remove the automatic player switching - players now control independently
        // The Up/Down player switching is no longer needed for simultaneous selection
    }

    void OnSubmit(int playerIndex)
    {
        // Allow any joined player to lock independently
        if (players[playerIndex].isJoined && !players[playerIndex].hasLockedCharacter)
        {
            LockCharacterSelection(playerIndex);
        }
    }

    void OnCancel(int playerIndex)
    {
        if (playerIndex > 0 && players[playerIndex].isJoined) // Player 1 (keyboard) cannot leave
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

                    // Remember selection for gameplay spawn (prefab + device)
                    GameObject prefab = null;
                    int sel = players[playerIndex].selectedCharacterIndex;
                    if (characterPrefabs != null && sel >= 0 && sel < characterPrefabs.Length)
                    {
                        prefab = characterPrefabs[sel];
                    }
                    var device = GetDeviceForPlayer(playerIndex);
                    CharacterSelectionState.SetSelection(playerIndex, prefab, device);

                    // No longer need to switch to next player - each player locks independently
                    UpdateUI();

                    Debug.Log($"Player {playerIndex + 1} locked character: {selectedCharacter.characterName}");
                    
                    // Check if we should start auto-progression
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

            Debug.Log($"Player {playerIndex + 1} unlocked their character selection");
            
            // This will automatically cancel auto-progression in HandleAutoProgression()
        }
    }
    
    void CheckForAutoProgression()
    {
        if (enableAutoProgression && CheckAllJoinedPlayersLocked())
        {
            Debug.Log("NewCharacterSelectManager: All joined players have locked their characters!");
            // Auto-progression will start in HandleAutoProgression() on next frame
        }
    }
    
    void RemovePlayer(int playerIndex)
    {
        if (playerIndex > 0) // Player 1 cannot leave
        {
            // Remove pad mapping if present
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

            Debug.Log($"Player {playerIndex + 1} left the character select");
        }
    }

    UnityEngine.InputSystem.InputDevice GetDeviceForPlayer(int playerIndex)
    {
        foreach (var kv in padToPlayer)
        {
            if (kv.Value == playerIndex) return kv.Key; // Gamepad for this player
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

        if (!players[currentActivePlayer].isJoined)
            currentActivePlayer = 0;

        characterGrid.SetControllingPlayer(players[currentActivePlayer].isJoined && !players[currentActivePlayer].hasLockedCharacter ? currentActivePlayer : -1);
        UpdateUI();

        Debug.Log($"Switched to controlling Player {currentActivePlayer + 1}");
    }

    void UpdateUI()
    {
        // Defensive checks
        if (players == null || players.Length < maxPlayers)
        {
            InitializePlayers();
        }

        int platformCount = (playerPlatforms != null) ? playerPlatforms.Length : 0;
        int loopCount = Mathf.Min(maxPlayers, players != null ? players.Length : 0, platformCount);

        // Update all player platforms with their individual selections
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

        // Update character grid with all player selections
        if (characterGrid != null && players != null)
        {
            characterGrid.UpdateAllPlayerSelections(players, playerSelectionColors);
        }

        // Update instructions and start button UI
        if (instructionText != null)
        {
            UpdateInstructions();
        }
        // No Start button flow; auto-progression only
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
        
        // Show auto-progression status
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
            // Show which players are active
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
            {
                instructions += $"Selecting: {activePlayersList}\n";
            }
        }
        
        // Show device-specific controls
        instructions += "ALL PLAYERS CAN SELECT SIMULTANEOUSLY!\n";
        instructions += "KEYBOARD: WASD/Arrows = Navigate | Enter/Space = Lock | Esc = Cancel\n";
        instructions += "CONTROLLER: D-Pad/Stick = Navigate | A/Start = Lock | B/Back = Cancel/Leave\n";

        if (joinedCount < maxPlayers)
        {
            instructions += "Connect controllers and press any button to join!";
        }
        
        // Show auto-progression info
        if (enableAutoProgression && joinedCount > 0)
        {
            instructions += $"\n?? When all players lock characters, auto-proceed to Map Selection in {autoProgressDelay}s";
        }

        instructionText.text = instructions;
    }

    void UpdateJoinPrompt()
    {
        if (joinPromptText == null) return;
        int joinedCount = 0;
        for (int i = 0; i < maxPlayers; i++) if (players[i].isJoined) joinedCount++;

        bool canJoinMore = joinedCount < maxPlayers;
        joinPromptText.gameObject.SetActive(canJoinMore);
        if (canJoinMore)
        {
            joinPromptText.text = "Press any button to join";
        }
    }
    
    string GetColorName(Color color)
    {
        if (color == Color.red) return "Red";
        if (color == Color.blue) return "Blue";
        if (color == Color.green) return "Green";
        if (color == Color.yellow) return "Yellow";
        return "Color";
    }

    // Start button flow removed: auto-progression only
    
    // Public method to toggle auto-progression
    public void ToggleAutoProgression(bool enabled)
    {
        enableAutoProgression = enabled;
        if (!enabled)
        {
            CancelAutoProgression();
        }
        Debug.Log($"NewCharacterSelectManager: Auto-progression {(enabled ? "enabled" : "disabled")}");
    }
    
    // Public method to set auto-progression delay
    public void SetAutoProgressDelay(float delay)
    {
        autoProgressDelay = Mathf.Max(0.5f, delay);
        Debug.Log($"NewCharacterSelectManager: Auto-progression delay set to {autoProgressDelay} seconds");
    }
    
    void HandleAutoProgression()
    {
        bool allJoinedPlayersLocked = CheckAllJoinedPlayersLocked();
        
        if (allJoinedPlayersLocked && !isAutoProgressing)
        {
            // Start auto-progression timer
            StartAutoProgression();
        }
        else if (!allJoinedPlayersLocked && isAutoProgressing)
        {
            // Cancel auto-progression if a player unlocks their character
            CancelAutoProgression();
        }
        
        // Update auto-progression timer
        if (isAutoProgressing)
        {
            autoProgressTimer -= Time.deltaTime;
            
            if (autoProgressTimer <= 0f)
            {
                // Time's up, proceed to map selection
                ProceedToMapSelection();
            }
            else
            {
                // Update countdown display
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
                if (players[i].hasLockedCharacter)
                {
                    lockedPlayerCount++;
                }
            }
        }
        
        // Need at least one player and all joined players must be locked
        return joinedPlayerCount > 0 && joinedPlayerCount == lockedPlayerCount;
    }
    
    void StartAutoProgression()
    {
        isAutoProgressing = true;
        autoProgressTimer = autoProgressDelay;
        
        Debug.Log($"NewCharacterSelectManager: All joined players have locked their characters! Auto-progressing to Map Selection in {autoProgressDelay} seconds...");
        
        // Show progression message
        UpdateAutoProgressUI();
    }
    
    void CancelAutoProgression()
    {
        isAutoProgressing = false;
        autoProgressTimer = 0f;
        
        Debug.Log("NewCharacterSelectManager: Auto-progression cancelled - a player unlocked their character");
        
        // Hide progression message
        if (autoProgressText != null)
        {
            autoProgressText.gameObject.SetActive(false);
        }
    }
    
    void UpdateAutoProgressUI()
    {
        if (autoProgressText != null)
        {
            autoProgressText.gameObject.SetActive(true);
            int secondsLeft = Mathf.CeilToInt(autoProgressTimer);
            autoProgressText.text = $"All players ready! Proceeding to Map Selection in {secondsLeft}...";
            
            // Add some visual flair with color changes
            if (secondsLeft <= 1)
            {
                autoProgressText.color = Color.red;
            }
            else if (secondsLeft <= 2)
            {
                autoProgressText.color = Color.yellow;
            }
            else
            {
                autoProgressText.color = Color.white;
            }
        }
    }
    
    void ProceedToMapSelection()
    {
        if (hasTransitionedToMapSelection) return;
        hasTransitionedToMapSelection = true;
        Debug.Log("=== AUTO-PROCEEDING TO MAP SELECTION ===");
        
        // Store selected characters in GameDataManager
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetSelectedCharacters(players);
            
            // Log selected characters
            for (int i = 0; i < maxPlayers; i++)
            {
                if (players[i].hasLockedCharacter)
                {
                    Debug.Log($"Player {i + 1}: {players[i].lockedCharacter.characterName}");
                }
            }
        }
        else
        {
            Debug.LogWarning("GameDataManager instance not found!");
        }
        
        // Ensure normal timescale
        if (Time.timeScale != 1f) Time.timeScale = 1f;

        // Detach input callbacks to avoid referencing old scene UI
        InputSystem.onDeviceChange -= OnDeviceChange;

        // Keep this manager instance alive for the map selection scene
        // Don't destroy it as MapSelectionManager needs the device mappings
        DontDestroyOnLoad(gameObject);

        // Load map selection scene
        SceneManager.LoadScene(mapSelectionSceneName);
    }
    
    void ProceedToMapSelectionImmediately()
    {
        Debug.Log("NewCharacterSelectManager: Manual proceed to Map Selection triggered!");
        isAutoProgressing = false; // Stop the timer
        ProceedToMapSelection();
    }

    public PlayerCharacterData[] GetPlayerData()
    {
        return players;
    }

    bool ValidateSetup()
    {
        if (availableCharacters == null || availableCharacters.Length == 0)
        {
            Debug.LogError("NewCharacterSelectManager: availableCharacters array is null or empty! Please assign character data in the inspector.");
            return false;
        }

        if (playerPlatforms == null || playerPlatforms.Length == 0)
        {
            Debug.LogError("NewCharacterSelectManager: playerPlatforms array is null or empty! Please assign player platforms in the inspector.");
            return false;
        }

        if (playerPlatforms.Length < maxPlayers)
        {
            Debug.LogError($"NewCharacterSelectManager: Not enough player platforms! Expected {maxPlayers}, found {playerPlatforms.Length}");
            return false;
        }

        if (characterGrid == null)
        {
            Debug.LogError("NewCharacterSelectManager: CharacterGridUI is not assigned!");
            return false;
        }

        Debug.Log($"NewCharacterSelectManager: Setup validation passed: {availableCharacters.Length} characters, {playerPlatforms.Length} platforms");
        return true;
    }
}