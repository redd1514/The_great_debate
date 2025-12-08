using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewCharacterSelectManager : MonoBehaviour
{
    [Header("UI References")]
    public PlayerPlatformUI[] playerPlatforms; // 4 platform UIs
    public CharacterGridUI characterGrid;
    public Button startGameButton;
    public TextMeshProUGUI instructionText;

    [Header("Character Data")]
    public CharacterSelectData[] availableCharacters;

    [Header("Settings")]
    public int maxPlayers = 4;
    public string gameSceneName = "GameplayScene";
    public string mapSelectionSceneName = "MapSelectionScene"; // New field for map selection
    [Range(0.5f, 5f)]
    public float autoProgressDelay = 2f; // Delay before auto-progression

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
    
    [Header("Input Device Tracking")]
    private Dictionary<InputDevice, int> deviceToPlayerMap = new Dictionary<InputDevice, int>();
    private HashSet<InputDevice> usedDevices = new HashSet<InputDevice>();
    
    // Track previous input states to prevent repeated navigation
    private bool[] previousHorizontalInput = new bool[4];
    private bool[] previousVerticalInput = new bool[4];

    public static NewCharacterSelectManager Instance;

    public enum InputDevice
    {
        Keyboard,
        Controller1,
        Controller2,
        Controller3,
        Controller4
    }

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
        
        // Clear any corrupted state from scene transitions
        deviceToPlayerMap.Clear();
        usedDevices.Clear();
        
        // Reset input state arrays
        for (int i = 0; i < 4; i++)
        {
            previousHorizontalInput[i] = false;
            previousVerticalInput[i] = false;
        }
        
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

        // Player 1 joins automatically with keyboard (whoever started the game)
        JoinPlayerWithDevice(0, InputDevice.Keyboard);
        
        // Force immediate update to ensure everything is properly initialized
        UpdateUI();

        Debug.Log("NewCharacterSelectManager: COMPLETE RESET - All systems reinitialized");
        Debug.Log("New Character Select Started - Keyboard: Player 1 auto-joined | All players can select simultaneously with individual colored indicators!");
    }

    void Update()
    {
        HandleAllInputDevices();
        
        // Handle auto-progression to map selection
        if (enableAutoProgression)
        {
            HandleAutoProgression();
        }
    }
    
    void HandleAllInputDevices()
    {
        // CRITICAL DEBUG: Show that this method is running
        if (Input.anyKeyDown)
        {
            Debug.Log($"NewCharacterSelectManager.HandleAllInputDevices: Input detected! Checking {deviceToPlayerMap.Count} mapped devices");
        }
        
        // Handle keyboard input
        HandleDeviceInput(InputDevice.Keyboard);
        
        // Handle controller inputs
        HandleDeviceInput(InputDevice.Controller1);
        HandleDeviceInput(InputDevice.Controller2);
        HandleDeviceInput(InputDevice.Controller3);
        HandleDeviceInput(InputDevice.Controller4);
    }

    void HandleDeviceInput(InputDevice device)
    {
        // CRITICAL: Show which device we're checking
        if (device == InputDevice.Keyboard && Input.anyKeyDown)
        {
            Debug.Log($"NewCharacterSelectManager.HandleDeviceInput: Checking keyboard input");
        }
        
        // Check for join input (any key/button pressed)
        if (GetAnyButtonDown(device))
        {
            TryJoinWithDevice(device);
        }

        // Only handle navigation/actions for devices that have joined players
        if (deviceToPlayerMap.ContainsKey(device))
        {
            int playerIndex = deviceToPlayerMap[device];
            
            Debug.Log($"NewCharacterSelectManager: Device {device} mapped to Player {playerIndex + 1}");
            
            // Check for special input during auto-progression
            if (isAutoProgressing)
            {
                // Any submit input during auto-progression proceeds immediately
                if (GetSubmitInput(device))
                {
                    ProceedToMapSelectionImmediately();
                    return;
                }
                
                // Any cancel input cancels auto-progression (unlocks the player's character)
                if (GetCancelInput(device))
                {
                    UnlockCharacterSelection(playerIndex);
                    return;
                }
                
                // Don't allow navigation during auto-progression
                return;
            }
            
            // Normal navigation and actions when not auto-progressing
            // Navigation
            Vector2 input = GetNavigationInput(device);
            if (input != Vector2.zero)
            {
                Debug.Log($"NewCharacterSelectManager: Navigation input {input} detected for Player {playerIndex + 1}");
                OnNavigate(input, playerIndex);
            }

            // Submit
            if (GetSubmitInput(device))
            {
                OnSubmit(playerIndex);
            }

            // Cancel (only for non-keyboard players)
            if (GetCancelInput(device) && device != InputDevice.Keyboard)
            {
                OnCancel(playerIndex);
            }
        }
        else if (device == InputDevice.Keyboard && Input.anyKeyDown)
        {
            Debug.LogWarning($"NewCharacterSelectManager: Keyboard input detected but device not mapped to any player!");
            Debug.LogWarning($"Current device mappings: {deviceToPlayerMap.Count}");
            foreach (var mapping in deviceToPlayerMap)
            {
                Debug.LogWarning($"  {mapping.Key} -> Player {mapping.Value + 1}");
            }
        }
    }

    bool GetAnyButtonDown(InputDevice device)
    {
        switch (device)
        {
            case InputDevice.Keyboard:
                // Exclude navigation and action keys from join detection
                return Input.anyKeyDown && !IsNavigationOrActionKey();
                
            case InputDevice.Controller1:
                return GetControllerAnyButtonDown("joystick 1");
                
            case InputDevice.Controller2:
                return GetControllerAnyButtonDown("joystick 2");
                
            case InputDevice.Controller3:
                return GetControllerAnyButtonDown("joystick 3");
                
            case InputDevice.Controller4:
                return GetControllerAnyButtonDown("joystick 4");
        }
        return false;
    }

    bool GetControllerAnyButtonDown(string joystickName)
    {
        // Check all common controller buttons
        for (int i = 0; i < 20; i++) // Check buttons 0-19
        {
            if (Input.GetKeyDown($"{joystickName} button {i}"))
                return true;
        }
        return false;
    }

    bool IsNavigationOrActionKey()
    {
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
               Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
               Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
               Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
               Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.Escape);
    }

    Vector2 GetNavigationInput(InputDevice device)
    {
        Vector2 input = Vector2.zero;
        
        switch (device)
        {
            case InputDevice.Keyboard:
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    input = Vector2.left;
                    Debug.Log("!!! KEYBOARD LEFT INPUT DETECTED !!!");
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    input = Vector2.right;
                    Debug.Log("!!! KEYBOARD RIGHT INPUT DETECTED !!!");
                }
                else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                {
                    input = Vector2.up;
                    Debug.Log("!!! KEYBOARD UP INPUT DETECTED !!!");
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                {
                    input = Vector2.down;
                    Debug.Log("!!! KEYBOARD DOWN INPUT DETECTED !!!");
                }
                break;
                
            case InputDevice.Controller1:
            case InputDevice.Controller2:
            case InputDevice.Controller3:
            case InputDevice.Controller4:
                input = GetControllerNavigationSimple(device);
                break;
        }
        
        return input;
    }

    Vector2 GetControllerNavigationSimple(InputDevice device)
    {
        int controllerIndex = (int)device - 1; // Controller1 = 0, Controller2 = 1, etc.
        string joystickName = $"joystick {controllerIndex + 1}";
        
        Vector2 input = Vector2.zero;
        
        // Use button-based navigation for reliability
        // Standard Xbox controller button mappings
        if (Input.GetKeyDown($"{joystickName} button 13")) // D-pad left
        {
            input.x = -1f;
            Debug.Log($"Controller {controllerIndex + 1}: D-pad Left");
        }
        else if (Input.GetKeyDown($"{joystickName} button 14")) // D-pad right
        {
            input.x = 1f;  
            Debug.Log($"Controller {controllerIndex + 1}: D-pad Right");
        }
        
        if (Input.GetKeyDown($"{joystickName} button 11")) // D-pad up
        {
            input.y = 1f;  
            Debug.Log($"Controller {controllerIndex + 1}: D-pad Up");
        }
        else if (Input.GetKeyDown($"{joystickName} button 12")) // D-pad down
        {
            input.y = -1f; 
            Debug.Log($"Controller {controllerIndex + 1}: D-pad Down");
        }
        
        // Try analog stick input for the first controller only (to avoid Input Manager issues)
        if (controllerIndex == 0 && input == Vector2.zero)
        {
            try 
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                
                // Convert analog input to discrete navigation
                if (Mathf.Abs(horizontal) > 0.8f && !previousHorizontalInput[controllerIndex])
                {
                    input.x = horizontal > 0 ? 1f : -1f;
                    previousHorizontalInput[controllerIndex] = true;
                    Debug.Log($"Controller {controllerIndex + 1}: Analog stick horizontal");
                }
                else if (Mathf.Abs(horizontal) <= 0.3f)
                {
                    previousHorizontalInput[controllerIndex] = false;
                }
                
                if (Mathf.Abs(vertical) > 0.8f && !previousVerticalInput[controllerIndex])
                {
                    input.y = vertical > 0 ? 1f : -1f;
                    previousVerticalInput[controllerIndex] = true;
                    Debug.Log($"Controller {controllerIndex + 1}: Analog stick vertical");
                }
                else if (Mathf.Abs(vertical) <= 0.3f)
                {
                    previousVerticalInput[controllerIndex] = false;
                }
            }
            catch (System.ArgumentException)
            {
                // Default axes not available, stick with D-pad only
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
                
                // A button (button 0) and Start button (button 7 or 9 depending on controller)
                bool submitPressed = Input.GetKeyDown($"{joystickName} button 0") ||    // A button
                                   Input.GetKeyDown($"{joystickName} button 7") ||    // Start button (Xbox)
                                   Input.GetKeyDown($"{joystickName} button 9");     // Start button (PS4)
                
                if (submitPressed)
                {
                    Debug.Log($"Controller {controllerIndex + 1}: Submit button pressed");
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
                
                // B button (button 1) and Back/Select button
                bool cancelPressed = Input.GetKeyDown($"{joystickName} button 1") ||    // B button
                                   Input.GetKeyDown($"{joystickName} button 6") ||    // Back button (Xbox)
                                   Input.GetKeyDown($"{joystickName} button 8");     // Select/Share button (PS4)
                
                if (cancelPressed)
                {
                    Debug.Log($"Controller {controllerIndex + 1}: Cancel button pressed");
                }
                
                return cancelPressed;
        }
        return false;
    }

    void TryJoinWithDevice(InputDevice device)
    {
        // Don't allow join if device is already used
        if (usedDevices.Contains(device))
        {
            return;
        }

        // Find next available player slot
        for (int i = 0; i < maxPlayers; i++)
        {
            if (!players[i].isJoined)
            {
                JoinPlayerWithDevice(i, device);
                break;
            }
        }
    }

    void JoinPlayerWithDevice(int playerIndex, InputDevice device)
    {
        if (playerIndex >= 0 && playerIndex < maxPlayers && !players[playerIndex].isJoined)
        {
            players[playerIndex].isJoined = true;
            players[playerIndex].selectedCharacterIndex = 0; // Start at first character
            
            // Store input device and color for map voting persistence
            players[playerIndex].inputDevice = ConvertToInputDeviceType(device);
            players[playerIndex].playerColor = playerSelectionColors[playerIndex];
            
            usedDevices.Add(device);
            deviceToPlayerMap[device] = playerIndex;
            
            // Don't automatically switch active player - let players control independently
            if (currentActivePlayer == -1 || !players[currentActivePlayer].isJoined)
                currentActivePlayer = playerIndex;
                
            // Initialize character grid for this player
            if (characterGrid != null)
            {
                characterGrid.InitializePlayerSelection(playerIndex, playerSelectionColors[playerIndex]);
            }
            
            UpdateUI();

            string deviceName = device == InputDevice.Keyboard ? "Keyboard" : $"Controller {(int)device}";
            Debug.Log($"Player {playerIndex + 1} joined with {deviceName} - Selection Color: {playerSelectionColors[playerIndex].ToString()}");
        }
    }
    
    // Convert local InputDevice enum to PlayerCharacterData.InputDeviceType
    InputDeviceType ConvertToInputDeviceType(InputDevice device)
    {
        switch (device)
        {
            case InputDevice.Keyboard: return InputDeviceType.Keyboard;
            case InputDevice.Controller1: return InputDeviceType.Controller1;
            case InputDevice.Controller2: return InputDeviceType.Controller2;
            case InputDevice.Controller3: return InputDeviceType.Controller3;
            default: return InputDeviceType.None;
        }
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
        if (playerIndex == currentActivePlayer && players[playerIndex].isJoined && !players[playerIndex].hasLockedCharacter)
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
            // Find and remove the device mapping
            InputDevice deviceToRemove = InputDevice.Keyboard;
            foreach (var kvp in deviceToPlayerMap)
            {
                if (kvp.Value == playerIndex)
                {
                    deviceToRemove = kvp.Key;
                    break;
                }
            }
            
            deviceToPlayerMap.Remove(deviceToRemove);
            usedDevices.Remove(deviceToRemove);
            
            players[playerIndex].isJoined = false;
            players[playerIndex].hasLockedCharacter = false;
            players[playerIndex].lockedCharacter = null;

            SwitchActivePlayer(-1);
            UpdateUI();

            Debug.Log($"Player {playerIndex + 1} left the character select");
        }
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
        // Update all player platforms with their individual selections
        for (int i = 0; i < playerPlatforms.Length; i++)
        {
            CharacterSelectData selectedChar = null;
            if (players[i].selectedCharacterIndex >= 0 && players[i].selectedCharacterIndex < availableCharacters.Length)
                selectedChar = availableCharacters[players[i].selectedCharacterIndex];

            // All joined players are considered "active" for their own selections
            bool isActiveForThisPlayer = players[i].isJoined && !players[i].hasLockedCharacter;
            playerPlatforms[i].UpdatePlatform(players[i], selectedChar, isActiveForThisPlayer);
        }

        // Update character grid with all player selections
        if (characterGrid != null)
        {
            characterGrid.UpdateAllPlayerSelections(players, playerSelectionColors);
        }

        UpdateInstructions();
        CheckCanStartGame();
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
            instructions += "?? ALL PLAYERS READY! PROCEEDING TO MAP SELECTION...\n";
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
    
    string GetColorName(Color color)
    {
        if (color == Color.red) return "Red";
        if (color == Color.blue) return "Blue";
        if (color == Color.green) return "Green";
        if (color == Color.yellow) return "Yellow";
        return "Color";
    }

    void CheckCanStartGame()
    {
        bool canStart = false;
        bool allJoinedPlayersReady = CheckAllJoinedPlayersLocked();

        for (int i = 0; i < maxPlayers; i++)
        {
            if (players[i].hasLockedCharacter)
            {
                canStart = true;
                break;
            }
        }

        startGameButton.interactable = canStart;
        
        // Update button text based on state
        if (startGameButton.GetComponentInChildren<Text>() != null)
        {
            Text buttonText = startGameButton.GetComponentInChildren<Text>();
            if (allJoinedPlayersReady)
            {
                buttonText.text = "Go to Map Selection";
            }
            else
            {
                buttonText.text = "Start Game";
            }
        }
    }

    public void StartGame()
    {
        // Check if we should go to map selection instead
        if (CheckAllJoinedPlayersLocked())
        {
            ProceedToMapSelection();
            return;
        }
        
        // Original start game logic for when not all players are ready
        Debug.Log("=== STARTING GAME WITH SELECTED CHARACTERS ===");

        List<CharacterSelectData> selectedCharacters = new List<CharacterSelectData>();

        for (int i = 0; i < maxPlayers; i++)
        {
            if (players[i].hasLockedCharacter)
            {
                selectedCharacters.Add(players[i].lockedCharacter);
                Debug.Log($"Player {i + 1}: {players[i].lockedCharacter.characterName}");
            }
        }

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetSelectedCharacters(players);
        }
        else
        {
            Debug.LogWarning("GameDataManager instance not found!");
        }

        SceneManager.LoadScene(gameSceneName);
    }
    
    // New public method for UI buttons
    public void ForceStartMapSelection()
    {
        if (CheckAllJoinedPlayersLocked())
        {
            ProceedToMapSelection();
        }
        else
        {
            Debug.LogWarning("NewCharacterSelectManager: Cannot proceed to Map Selection - not all joined players have locked their characters!");
        }
    }
    
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