using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component to automatically setup main menu UI references.
/// Attach this to your main menu Canvas or main menu GameObject.
/// </summary>
public class MainMenuSetupHelper : MonoBehaviour
{
    [Header("Auto-Setup Options")]
    public bool autoFindButtons = true;
    public bool autoCreateSelectionIndicator = true;
    
    [Header("Button Search Names (case-insensitive)")]
    public string startButtonName = "start";
    public string settingsButtonName = "settings"; 
    public string exitButtonName = "exit";
    
    [Header("Selection Indicator Settings")]
    public GameObject selectionIndicatorPrefab;
    public Color indicatorColor = Color.yellow;
    public float indicatorScale = 1.1f;
    
    void Start()
    {
        SetupMainMenu();
    }
    
    [ContextMenu("Setup Main Menu")]
    public void SetupMainMenu()
    {
        Mainmenu mainMenu = FindObjectOfType<Mainmenu>();
        
        if (mainMenu == null)
        {
            Debug.LogError("MainMenuSetupHelper: No Mainmenu component found in scene!");
            return;
        }
        
        if (autoFindButtons)
        {
            AutoFindButtons(mainMenu);
        }
        
        if (autoCreateSelectionIndicator)
        {
            AutoCreateSelectionIndicator(mainMenu);
        }
        
        Debug.Log("Main Menu setup completed!");
    }
    
    void AutoFindButtons(Mainmenu mainMenu)
    {
        Button[] allButtons = FindObjectsOfType<Button>();
        Button[] menuButtons = new Button[3];
        
        foreach (Button button in allButtons)
        {
            string buttonName = button.name.ToLower();
            
            if (buttonName.Contains(startButtonName.ToLower()))
            {
                menuButtons[0] = button;
                Debug.Log($"Found Start button: {button.name}");
            }
            else if (buttonName.Contains(settingsButtonName.ToLower()))
            {
                menuButtons[1] = button;
                Debug.Log($"Found Settings button: {button.name}");
            }
            else if (buttonName.Contains(exitButtonName.ToLower()))
            {
                menuButtons[2] = button;
                Debug.Log($"Found Exit button: {button.name}");
            }
        }
        
        // Assign to main menu
        if (mainMenu.menuButtons == null || mainMenu.menuButtons.Length != 3)
        {
            mainMenu.menuButtons = new Button[3];
        }
        
        for (int i = 0; i < 3; i++)
        {
            mainMenu.menuButtons[i] = menuButtons[i];
        }
        
        // Validate setup
        for (int i = 0; i < 3; i++)
        {
            if (menuButtons[i] == null)
            {
                string[] buttonNames = { "Start", "Settings", "Exit" };
                Debug.LogWarning($"MainMenuSetupHelper: {buttonNames[i]} button not found! Make sure button name contains '{GetSearchName(i)}'");
            }
        }
    }
    
    void AutoCreateSelectionIndicator(Mainmenu mainMenu)
    {
        if (mainMenu.selectionIndicator != null)
        {
            Debug.Log("Selection indicator already exists");
            return;
        }
        
        GameObject indicator;
        
        if (selectionIndicatorPrefab != null)
        {
            indicator = Instantiate(selectionIndicatorPrefab, transform);
        }
        else
        {
            // Create a simple indicator
            indicator = new GameObject("SelectionIndicator");
            indicator.transform.SetParent(transform);
            
            // Add Image component
            Image indicatorImage = indicator.AddComponent<Image>();
            indicatorImage.color = indicatorColor;
            
            // Add RectTransform
            RectTransform rectTransform = indicator.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 50);
        }
        
        mainMenu.selectionIndicator = indicator;
        Debug.Log("Selection indicator created");
    }
    
    string GetSearchName(int index)
    {
        switch (index)
        {
            case 0: return startButtonName;
            case 1: return settingsButtonName;
            case 2: return exitButtonName;
            default: return "";
        }
    }
    
    [ContextMenu("Find All Buttons")]
    public void DebugFindAllButtons()
    {
        Debug.Log("=== ALL BUTTONS IN SCENE ===");
        Button[] allButtons = FindObjectsOfType<Button>();
        
        for (int i = 0; i < allButtons.Length; i++)
        {
            Debug.Log($"Button {i + 1}: {allButtons[i].name}");
        }
        
        if (allButtons.Length == 0)
        {
            Debug.LogWarning("No buttons found in scene!");
        }
    }
}