using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Debug helper for CharacterGridUI to diagnose selection issues.
/// Provides runtime diagnostics and manual testing controls.
/// </summary>
public class CharacterGridDebugHelper : MonoBehaviour
{
    [Header("Debug Controls")]
    public KeyCode debugNavigateKey = KeyCode.T;
    public KeyCode forceShowIndicatorKey = KeyCode.Y;
    public KeyCode toggleDebugPanelKey = KeyCode.F1;
    
    [Header("Debug Panel")]
    public bool showDebugPanel = true;
    
    private CharacterGridUI characterGrid;
    private GameObject debugPanel;
    private Text debugText;
    private bool debugPanelVisible = true;
    
    void Start()
    {
        Debug.Log("=== CharacterGridDebugHelper: Starting ===");
        
        characterGrid = GetComponent<CharacterGridUI>();
        if (characterGrid == null)
        {
            Debug.LogError("CharacterGridDebugHelper: CharacterGridUI component not found!");
            return;
        }
        
        Debug.Log("CharacterGridDebugHelper: Found CharacterGridUI component");
        
        CreateDebugPanel();
        
        // Initial diagnostics
        StartCoroutine(DelayedDiagnostics());
        
        Debug.Log("CharacterGridDebugHelper: Initialization complete");
        Debug.Log("=== DEBUG CONTROLS ===");
        Debug.Log($"Press {debugNavigateKey} to force navigate right");
        Debug.Log($"Press {forceShowIndicatorKey} to force show indicator");
        Debug.Log($"Press {toggleDebugPanelKey} to toggle debug panel");
        Debug.Log("Press Arrow Keys or WASD to test navigation");
    }
    
    void Update()
    {
        HandleDebugInput();
        UpdateDebugPanel();
    }
    
    void HandleDebugInput()
    {
        if (characterGrid == null) return;
        
        // ALWAYS show when debug keys are pressed
        if (Input.GetKeyDown(debugNavigateKey))
        {
            Debug.Log("=== MANUAL NAVIGATION TEST TRIGGERED ===");
            characterGrid.Navigate(Vector2.right, 0);
        }
        
        if (Input.GetKeyDown(forceShowIndicatorKey))
        {
            Debug.Log("=== FORCE SHOW INDICATOR TEST TRIGGERED ===");
            ForceShowIndicator();
        }
        
        if (Input.GetKeyDown(toggleDebugPanelKey))
        {
            debugPanelVisible = !debugPanelVisible;
            if (debugPanel != null)
            {
                debugPanel.SetActive(debugPanelVisible);
            }
            Debug.Log($"=== DEBUG PANEL TOGGLED: {debugPanelVisible} ===");
        }
        
        // CRITICAL: Show ALL input detection
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log("!!! DEBUG HELPER: LEFT ARROW PRESSED !!!");
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("!!! DEBUG HELPER: RIGHT ARROW PRESSED !!!");
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Debug.Log("!!! DEBUG HELPER: UP ARROW PRESSED !!!");
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Debug.Log("!!! DEBUG HELPER: DOWN ARROW PRESSED !!!");
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("!!! DEBUG HELPER: A KEY PRESSED !!!");
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("!!! DEBUG HELPER: D KEY PRESSED !!!");
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("!!! DEBUG HELPER: W KEY PRESSED !!!");
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("!!! DEBUG HELPER: S KEY PRESSED !!!");
        }
    }
    
    IEnumerator DelayedDiagnostics()
    {
        yield return new WaitForSeconds(1f);
        RunDiagnostics();
    }
    
    void CreateDebugPanel()
    {
        if (!showDebugPanel) return;
        
        // Create canvas if needed
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Debug Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create debug panel
        debugPanel = new GameObject("Character Grid Debug Panel");
        debugPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = debugPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0.5f);
        panelRect.offsetMin = new Vector2(10, 10);
        panelRect.offsetMax = new Vector2(-10, -10);
        
        Image panelImage = debugPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        // Create text
        GameObject textGO = new GameObject("Debug Text");
        textGO.transform.SetParent(debugPanel.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        
        debugText = textGO.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.fontSize = 14;
        debugText.color = Color.white;
        debugText.alignment = TextAnchor.UpperLeft;
        debugText.verticalOverflow = VerticalWrapMode.Overflow;
        
        // Create button
        CreateDiagnosticsButton(canvas);
    }
    
    void CreateDiagnosticsButton(Canvas canvas)
    {
        GameObject buttonGO = new GameObject("Run Diagnostics Button");
        buttonGO.transform.SetParent(canvas.transform, false);
        
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0.5f);
        buttonRect.anchorMax = new Vector2(0.3f, 0.6f);
        buttonRect.offsetMin = new Vector2(10, 10);
        buttonRect.offsetMax = new Vector2(-10, -10);
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        
        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(RunDiagnostics);
        
        GameObject buttonTextGO = new GameObject("Button Text");
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform buttonTextRect = buttonTextGO.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        
        Text buttonText = buttonTextGO.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.text = "Run Diagnostics Now";
    }
    
    void UpdateDebugPanel()
    {
        if (debugText == null || !showDebugPanel) return;
        
        string debugInfo = "CHARACTER GRID DEBUG HELPER\n";
        debugInfo += "=================================\n\n";
        
        debugInfo += "CONTROLS:\n";
        debugInfo += $"• {debugNavigateKey} - Force Navigate Right\n";
        debugInfo += $"• {forceShowIndicatorKey} - Force Show Indicator\n";
        debugInfo += $"• {toggleDebugPanelKey} - Toggle This Panel\n";
        debugInfo += "• Arrow Keys - Normal Navigation\n\n";
        
        if (characterGrid != null)
        {
            debugInfo += "CHARACTER GRID STATUS:\n";
            debugInfo += $"Component: {(characterGrid != null ? "Found" : "NULL")}\n";
            
            // Access private fields using reflection for debugging
            var charactersField = typeof(CharacterGridUI).GetField("characters", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var iconsField = typeof(CharacterGridUI).GetField("characterIcons", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var indicatorsField = typeof(CharacterGridUI).GetField("playerSelectionIndicators", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (charactersField != null)
            {
                var characters = charactersField.GetValue(characterGrid) as CharacterSelectData[];
                debugInfo += $"Characters: {(characters?.Length ?? 0)}\n";
            }
            
            if (iconsField != null)
            {
                var icons = iconsField.GetValue(characterGrid) as GameObject[];
                debugInfo += $"Icons: {(icons?.Length ?? 0)}\n";
            }
            
            if (indicatorsField != null)
            {
                var indicators = indicatorsField.GetValue(characterGrid) as GameObject[];
                debugInfo += $"Indicators: {(indicators?.Length ?? 0)}\n";
            }
        }
        
        debugText.text = debugInfo;
    }
    
    public void RunDiagnostics()
    {
        Debug.Log("=== CHARACTER GRID DIAGNOSTICS ===");
        
        if (characterGrid == null)
        {
            Debug.LogError("CharacterGridUI component not found!");
            return;
        }
        
        // Check setup
        var charactersField = typeof(CharacterGridUI).GetField("characters", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var iconsField = typeof(CharacterGridUI).GetField("characterIcons", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var indicatorsField = typeof(CharacterGridUI).GetField("playerSelectionIndicators", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Debug.Log("--- Character Grid Setup ---");
        
        if (charactersField != null)
        {
            var characters = charactersField.GetValue(characterGrid) as CharacterSelectData[];
            Debug.Log($"Characters array: {(characters?.Length.ToString() ?? "NULL")}");
        }
        
        if (iconsField != null)
        {
            var icons = iconsField.GetValue(characterGrid) as GameObject[];
            Debug.Log($"Character icons: {(icons?.Length.ToString() ?? "NULL")}");
            
            if (icons != null)
            {
                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i] != null)
                    {
                        Debug.Log($"  Icon {i}: {icons[i].name} - Active: {icons[i].activeInHierarchy}");
                    }
                }
            }
        }
        
        if (indicatorsField != null)
        {
            var indicators = indicatorsField.GetValue(characterGrid) as GameObject[];
            Debug.Log($"Player indicators: {(indicators?.Length.ToString() ?? "NULL")}");
            
            if (indicators != null)
            {
                for (int i = 0; i < indicators.Length; i++)
                {
                    if (indicators[i] != null)
                    {
                        Debug.Log($"  Indicator {i}: {indicators[i].name}");
                        Debug.Log($"    Active: {indicators[i].activeInHierarchy}");
                        
                        Image img = indicators[i].GetComponent<Image>();
                        if (img != null)
                        {
                            Debug.Log($"    Image Color: {img.color}");
                        }
                        
                        PlayerSelectionIndicator indicator = indicators[i].GetComponent<PlayerSelectionIndicator>();
                        if (indicator != null)
                        {
                            Debug.Log($"    PlayerSelectionIndicator: Found");
                        }
                    }
                    else
                    {
                        Debug.Log($"  Indicator {i}: NULL");
                    }
                }
            }
        }
        
        // Check NewCharacterSelectManager
        var manager = FindObjectOfType<NewCharacterSelectManager>();
        if (manager != null)
        {
            Debug.Log("--- NewCharacterSelectManager ---");
            Debug.Log($"Manager found: {manager.name}");
            
            var playersField = typeof(NewCharacterSelectManager).GetField("players", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (playersField != null)
            {
                var players = playersField.GetValue(manager) as PlayerCharacterData[];
                if (players != null)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (players[i] != null)
                        {
                            Debug.Log($"  Player {i + 1}: Joined={players[i].isJoined}, Locked={players[i].hasLockedCharacter}");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError("NewCharacterSelectManager not found!");
        }
    }
    
    void ForceShowIndicator()
    {
        if (characterGrid == null) return;
        
        // Try to manually show the first indicator
        var indicatorsField = typeof(CharacterGridUI).GetField("playerSelectionIndicators", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (indicatorsField != null)
        {
            var indicators = indicatorsField.GetValue(characterGrid) as GameObject[];
            if (indicators != null && indicators.Length > 0 && indicators[0] != null)
            {
                indicators[0].SetActive(true);
                
                // Position it at a fixed location for testing
                RectTransform rect = indicators[0].GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(100, -100);
                    rect.sizeDelta = new Vector2(100, 100);
                }
                
                // Try to start animation
                PlayerSelectionIndicator indicator = indicators[0].GetComponent<PlayerSelectionIndicator>();
                if (indicator != null)
                {
                    indicator.SetPlayerWithAnimation(0);
                }
                
                Debug.Log("ForceShowIndicator: Activated indicator at fixed position");
            }
        }
    }
    
    [ContextMenu("Test First Icon Positioning")]
    public void TestFirstIconPositioning()
    {
        if (characterGrid == null) return;
        
        Debug.Log("=== TESTING FIRST ICON POSITIONING ===");
        
        var iconsField = typeof(CharacterGridUI).GetField("characterIcons", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var indicatorsField = typeof(CharacterGridUI).GetField("playerSelectionIndicators", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (iconsField != null && indicatorsField != null)
        {
            var icons = iconsField.GetValue(characterGrid) as GameObject[];
            var indicators = indicatorsField.GetValue(characterGrid) as GameObject[];
            
            if (icons != null && icons.Length > 0 && icons[0] != null &&
                indicators != null && indicators.Length > 0 && indicators[0] != null)
            {
                RectTransform iconRect = icons[0].GetComponent<RectTransform>();
                RectTransform indicatorRect = indicators[0].GetComponent<RectTransform>();
                
                Debug.Log($"Icon Position: {iconRect.position}");
                Debug.Log($"Icon Anchored Position: {iconRect.anchoredPosition}");
                Debug.Log($"Icon Anchors: Min({iconRect.anchorMin}), Max({iconRect.anchorMax})");
                Debug.Log($"Indicator Position: {indicatorRect.position}");
                Debug.Log($"Indicator Anchored Position: {indicatorRect.anchoredPosition}");
                Debug.Log($"Indicator Anchors: Min({indicatorRect.anchorMin}), Max({indicatorRect.anchorMax})");
                Debug.Log($"Parents - Icon: {iconRect.parent?.name}, Indicator: {indicatorRect.parent?.name}");
            }
        }
    }
    
    [ContextMenu("Force Realign All Indicators")]
    public void ForceRealignAllIndicators()
    {
        if (characterGrid == null) return;
        
        Debug.Log("=== FORCE REALIGNING ALL INDICATORS ===");
        
        // Call the validation method
        characterGrid.ValidateAndFixPlayerState();
        
        // Force update all player selections
        var manager = FindObjectOfType<NewCharacterSelectManager>();
        if (manager != null)
        {
            var playersField = typeof(NewCharacterSelectManager).GetField("players", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (playersField != null)
            {
                var players = playersField.GetValue(manager) as PlayerCharacterData[];
                if (players != null)
                {
                    // Create color array
                    Color[] colors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };
                    characterGrid.UpdateAllPlayerSelections(players, colors);
                    
                    Debug.Log("Force realignment complete");
                }
            }
        }
    }
    
    [ContextMenu("Debug All Icon Positions")]
    public void DebugAllIconPositions()
    {
        if (characterGrid == null) return;
        
        var iconsField = typeof(CharacterGridUI).GetField("characterIcons", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (iconsField != null)
        {
            var icons = iconsField.GetValue(characterGrid) as GameObject[];
            if (icons != null)
            {
                Debug.Log("=== ALL ICON POSITIONS ===");
                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i] != null)
                    {
                        RectTransform rect = icons[i].GetComponent<RectTransform>();
                        Debug.Log($"Icon {i}: Pos={rect.position}, AnchoredPos={rect.anchoredPosition}, Size={rect.sizeDelta}");
                    }
                }
            }
        }
    }
}