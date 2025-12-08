using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Mainmenu : MonoBehaviour
{
    [Header("Menu UI Elements")]
    public Button[] menuButtons; // 0: Start, 1: Settings, 2: Exit
    public GameObject selectionIndicator; // Visual indicator for current selection
    
    [Header("Scene Names")]
    public string characterSelectSceneName = "CharacterSelect";
    public string settingsSceneName = "Settings";
    
    [Header("Menu Settings")]
    public Color normalButtonColor = Color.white;
    public Color selectedButtonColor = Color.yellow;
    public AudioSource menuAudioSource; // Optional for menu sounds
    public AudioClip navigationSound;
    public AudioClip selectSound;
    
    private int currentSelectedIndex = 0;
    private const int MENU_BUTTON_COUNT = 3;
    
    // Input tracking (same system as character select but only for Player 1)
    private bool previousVerticalInput = false;
    private bool previousHorizontalInput = false; // Added for horizontal navigation tracking
    
    void Start()
    {
        InitializeMenu();
        UpdateMenuVisuals();
        
        Debug.Log("Main Menu loaded - Player 1 controls: WASD/D-Pad = Navigate, X/Enter = Select, Start = Quick Start");
    }
    
    void Update()
    {
        HandlePlayer1Input();
    }
    
    void InitializeMenu()
    {
        // Validate menu setup
        if (menuButtons == null || menuButtons.Length != MENU_BUTTON_COUNT)
        {
            Debug.LogError("MainMenu: menuButtons array must contain exactly 3 buttons (Start, Settings, Exit)");
            return;
        }
        
        // Set initial selection to Start button
        currentSelectedIndex = 0;
        
        // Setup button click events (for mouse users)
        if (menuButtons[0] != null) menuButtons[0].onClick.AddListener(() => OnStartGame());
        if (menuButtons[1] != null) menuButtons[1].onClick.AddListener(() => OnSettings());
        if (menuButtons[2] != null) menuButtons[2].onClick.AddListener(() => OnExitGame());
    }
    
    void HandlePlayer1Input()
    {
        // Handle keyboard input (Player 1)
        Vector2 keyboardInput = GetKeyboardInput();
        if (keyboardInput != Vector2.zero)
        {
            HandleNavigation(keyboardInput);
        }
        
        // Handle Controller 1 input (Player 1)
        Vector2 controllerInput = GetController1Input();
        if (controllerInput != Vector2.zero)
        {
            HandleNavigation(controllerInput);
        }
        
        // Handle submit input
        if (GetPlayer1SubmitInput())
        {
            OnSubmit();
        }
        
        // Handle quick start (Start button on controller)
        if (GetPlayer1QuickStartInput())
        {
            OnStartGame();
        }
    }
    
    Vector2 GetKeyboardInput()
    {
        Vector2 input = Vector2.zero;
        
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            input.y = 1f;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            input.y = -1f;
        else if (Input.GetKeyDown(KeyCode.A)) // Left navigation
            input.x = -1f;
        else if (Input.GetKeyDown(KeyCode.D)) // Right navigation
            input.x = 1f;
            
        return input;
    }
    
    Vector2 GetController1Input()
    {
        Vector2 input = Vector2.zero;
        string joystickName = "joystick 1";
        
        // D-pad navigation for PS4 controller
        if (Input.GetKeyDown($"{joystickName} button 11")) // D-pad up
            input.y = 1f;
        else if (Input.GetKeyDown($"{joystickName} button 12")) // D-pad down
            input.y = -1f;
        else if (Input.GetKeyDown($"{joystickName} button 13")) // D-pad left (left arrow)
            input.x = -1f;
        else if (Input.GetKeyDown($"{joystickName} button 14")) // D-pad right (right arrow)
            input.x = 1f;
        
        // Left stick navigation (with deadzone and repeat prevention)
        try
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            // Horizontal navigation with analog stick
            if (Mathf.Abs(horizontal) > 0.8f && !previousHorizontalInput)
            {
                input.x = horizontal > 0 ? 1f : -1f;
                previousHorizontalInput = true;
            }
            else if (Mathf.Abs(horizontal) <= 0.3f)
            {
                previousHorizontalInput = false;
            }
            
            // Vertical navigation with analog stick
            if (Mathf.Abs(vertical) > 0.8f && !previousVerticalInput)
            {
                input.y = vertical > 0 ? 1f : -1f;
                previousVerticalInput = true;
            }
            else if (Mathf.Abs(vertical) <= 0.3f)
            {
                previousVerticalInput = false;
            }
        }
        catch (System.ArgumentException)
        {
            // Default axes not available, use D-pad only
        }
        
        return input;
    }
    
    bool GetPlayer1SubmitInput()
    {
        // Keyboard submit
        bool keyboardSubmit = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        
        // Controller submit - PS4 X button (Cross) and alternatives
        bool controllerSubmit = false;
        string joystickName = "joystick 1";
        
        controllerSubmit = Input.GetKeyDown($"{joystickName} button 0") || // X button (Cross) - PS4 South button
                          Input.GetKeyDown($"{joystickName} button 1");   // Circle button (alternative for some regions)
        
        return keyboardSubmit || controllerSubmit;
    }
    
    bool GetPlayer1QuickStartInput()
    {
        string joystickName = "joystick 1";
        return Input.GetKeyDown($"{joystickName} button 7") || // Start button
               Input.GetKeyDown($"{joystickName} button 9");   // Start button (PS4)
    }
    
    void HandleNavigation(Vector2 input)
    {
        // Handle vertical navigation (up/down through menu items)
        if (Mathf.Abs(input.y) > 0.5f)
        {
            int direction = input.y > 0 ? -1 : 1; // Up = -1 (previous), Down = 1 (next)
            
            currentSelectedIndex = (currentSelectedIndex + direction + MENU_BUTTON_COUNT) % MENU_BUTTON_COUNT;
            
            UpdateMenuVisuals();
            PlayNavigationSound();
            
            Debug.Log($"Main Menu: Selected {GetButtonName(currentSelectedIndex)} (Vertical Navigation)");
        }
        
        // Handle horizontal navigation (left/right through menu items - alternative navigation)
        if (Mathf.Abs(input.x) > 0.5f)
        {
            int direction = input.x > 0 ? 1 : -1; // Right = 1 (next), Left = -1 (previous)
            
            currentSelectedIndex = (currentSelectedIndex + direction + MENU_BUTTON_COUNT) % MENU_BUTTON_COUNT;
            
            UpdateMenuVisuals();
            PlayNavigationSound();
            
            Debug.Log($"Main Menu: Selected {GetButtonName(currentSelectedIndex)} (Horizontal Navigation)");
        }
    }
    
    void UpdateMenuVisuals()
    {
        // Update button colors
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
            {
                ColorBlock colors = menuButtons[i].colors;
                colors.normalColor = (i == currentSelectedIndex) ? selectedButtonColor : normalButtonColor;
                menuButtons[i].colors = colors;
            }
        }
        
        // Update selection indicator position
        if (selectionIndicator != null && currentSelectedIndex < menuButtons.Length && menuButtons[currentSelectedIndex] != null)
        {
            RectTransform indicatorRect = selectionIndicator.GetComponent<RectTransform>();
            RectTransform buttonRect = menuButtons[currentSelectedIndex].GetComponent<RectTransform>();
            
            if (indicatorRect != null && buttonRect != null)
            {
                indicatorRect.position = buttonRect.position;
                indicatorRect.sizeDelta = buttonRect.sizeDelta * 1.1f;
            }
        }
    }
    
    void OnSubmit()
    {
        PlaySelectSound();
        
        switch (currentSelectedIndex)
        {
            case 0: // Start
                OnStartGame();
                break;
            case 1: // Settings
                OnSettings();
                break;
            case 2: // Exit
                OnExitGame();
                break;
        }
    }
    
    public void OnStartGame()
    {
        Debug.Log("Starting game - Loading Character Select...");
        
        // Load character select scene
        if (!string.IsNullOrEmpty(characterSelectSceneName))
        {
            SceneManager.LoadScene(characterSelectSceneName);
        }
        else
        {
            Debug.LogError("Character Select scene name is not set!");
        }
    }
    
    public void OnSettings()
    {
        Debug.Log("Opening settings menu...");
        
        // Load settings scene (when you create it)
        if (!string.IsNullOrEmpty(settingsSceneName))
        {
            // Uncomment when settings scene is ready
            // SceneManager.LoadScene(settingsSceneName);
            Debug.Log("Settings scene not implemented yet - will load when ready");
        }
        else
        {
            Debug.LogWarning("Settings scene name is not set!");
        }
    }
    
    public void OnExitGame()
    {
        Debug.Log("Exiting game...");
        
        #if UNITY_EDITOR
            // In editor, stop playing
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // In build, quit application
            Application.Quit();
        #endif
    }
    
    string GetButtonName(int index)
    {
        switch (index)
        {
            case 0: return "Start Game";
            case 1: return "Settings";
            case 2: return "Exit Game";
            default: return "Unknown";
        }
    }
    
    void PlayNavigationSound()
    {
        if (menuAudioSource != null && navigationSound != null)
        {
            menuAudioSource.PlayOneShot(navigationSound);
        }
    }
    
    void PlaySelectSound()
    {
        if (menuAudioSource != null && selectSound != null)
        {
            menuAudioSource.PlayOneShot(selectSound);
        }
    }
    
    // Public method for debugging
    [ContextMenu("Test Menu Navigation")]
    public void TestMenuNavigation()
    {
        Debug.Log("=== MAIN MENU CONTROLS ===");
        Debug.Log("KEYBOARD: W/S or Up/Down arrows = Navigate Vertically, A/D = Navigate Horizontally, Enter/Space = Select");
        Debug.Log("PS4 CONTROLLER: D-Pad or Left Stick = Navigate, X button (Cross) = Select, Start button = Quick Start");
        Debug.Log("Note: Only Player 1 (Keyboard + Controller 1) can control the main menu");
        Debug.Log($"Current selection: {GetButtonName(currentSelectedIndex)}");
    }
}
