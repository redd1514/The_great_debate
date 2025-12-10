using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Utility script to automatically set up a Credits scene with proper UI components.
/// This script helps create all the necessary UI elements for the CreditsController to work properly.
/// 
/// Usage:
/// 1. Add this script to an empty GameObject in your Credits scene
/// 2. Run the "Setup Credits Scene" context menu option or call SetupCreditsScene() in code
/// 3. Configure the created components as needed
/// </summary>
public class CreditsSceneSetup : MonoBehaviour
{
    [Header("Credits Setup Configuration")]
    [Tooltip("Create a background image")]
    public bool createBackground = true;
    
    [Tooltip("Background color if creating background")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
    
    [Tooltip("Create credits text component")]
    public bool createCreditsText = true;
    
    [Tooltip("Create skip instruction text")]
    public bool createSkipInstruction = true;
    
    [Tooltip("Create logo/title image placeholder")]
    public bool createLogoPlaceholder = false;
    
    [Header("Layout Settings")]
    [Tooltip("Canvas scale mode")]
    public CanvasScaler.ScaleMode canvasScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    
    [Tooltip("Reference resolution")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    
    [Header("Credits Content")]
    [TextArea(5, 15)]
    public string defaultCreditsText = @"SPECIAL THANKS TO

ARTISTS:                    DEVELOPERS:
ROB IVAN COLECO           JOSE LEJARO
JOSHUA DIMAANO           ELMER BENITEZ II
MARK CHRISTIAN YUMUL

DOCUMENTATION:
CHRISTIAN JAMES
BALDONADO

MUSIC & SOUND EFFECTS:
Music by: Audio Team
Sound Effects: SFX Library

Thank you for playing!

© 2024 Game Studio";

    [ContextMenu("Setup Credits Scene")]
    public void SetupCreditsScene()
    {
        Debug.Log("CreditsSceneSetup: Setting up credits scene...");
        
        // Create main canvas if it doesn't exist
        Canvas mainCanvas = CreateOrFindCanvas();
        
        // Create canvas group for fading
        CanvasGroup canvasGroup = EnsureCanvasGroup(mainCanvas);
        
        // Create UI elements
        if (createBackground)
        {
            CreateBackgroundImage(mainCanvas.transform);
        }
        
        if (createCreditsText)
        {
            CreateCreditsText(mainCanvas.transform);
        }
        
        if (createSkipInstruction)
        {
            CreateSkipInstructionText(mainCanvas.transform);
        }
        
        if (createLogoPlaceholder)
        {
            CreateLogoPlaceholder(mainCanvas.transform);
        }
        
        // Create or configure CreditsController
        SetupCreditsController(canvasGroup);
        
        Debug.Log("CreditsSceneSetup: Credits scene setup complete!");
    }
    
    Canvas CreateOrFindCanvas()
    {
        Canvas existingCanvas = FindFirstObjectByType<Canvas>();
        if (existingCanvas != null)
        {
            Debug.Log($"CreditsSceneSetup: Using existing canvas: {existingCanvas.name}");
            ConfigureCanvas(existingCanvas);
            return existingCanvas;
        }
        
        // Create new canvas
        GameObject canvasObj = new GameObject("CreditsCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        
        // Add CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = canvasScaleMode;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // Add GraphicRaycaster
        canvasObj.AddComponent<GraphicRaycaster>();
        
        Debug.Log("CreditsSceneSetup: Created new credits canvas");
        return canvas;
    }
    
    void ConfigureCanvas(Canvas canvas)
    {
        // Ensure proper canvas settings
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }
        
        scaler.uiScaleMode = canvasScaleMode;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
    
    CanvasGroup EnsureCanvasGroup(Canvas canvas)
    {
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            Debug.Log("CreditsSceneSetup: Added CanvasGroup to canvas");
        }
        
        // Set initial state for fading
        canvasGroup.alpha = 1f; // Will be set to 0 by CreditsController at start
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        return canvasGroup;
    }
    
    void CreateBackgroundImage(Transform parent)
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(parent, false);
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = backgroundColor;
        bgImage.raycastTarget = false;
        
        RectTransform bgRect = bgImage.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Set as first sibling to render behind everything
        bgObj.transform.SetAsFirstSibling();
        
        Debug.Log("CreditsSceneSetup: Created background image");
    }
    
    void CreateCreditsText(Transform parent)
    {
        GameObject textObj = new GameObject("CreditsText");
        textObj.transform.SetParent(parent, false);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = defaultCreditsText;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0.1f, 0.1f);
        textRect.anchorMax = new Vector2(0.9f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("CreditsSceneSetup: Created credits text");
    }
    
    void CreateSkipInstructionText(Transform parent)
    {
        GameObject skipObj = new GameObject("SkipInstructionText");
        skipObj.transform.SetParent(parent, false);
        
        TextMeshProUGUI skipText = skipObj.AddComponent<TextMeshProUGUI>();
        skipText.text = "Press any key to skip";
        skipText.fontSize = 18;
        skipText.color = new Color(1f, 1f, 1f, 0.7f);
        skipText.alignment = TextAlignmentOptions.BottomRight;
        skipText.raycastTarget = false;
        
        RectTransform skipRect = skipText.rectTransform;
        skipRect.anchorMin = new Vector2(0.7f, 0.05f);
        skipRect.anchorMax = new Vector2(0.95f, 0.15f);
        skipRect.offsetMin = Vector2.zero;
        skipRect.offsetMax = Vector2.zero;
        skipRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("CreditsSceneSetup: Created skip instruction text");
    }
    
    void CreateLogoPlaceholder(Transform parent)
    {
        GameObject logoObj = new GameObject("LogoPlaceholder");
        logoObj.transform.SetParent(parent, false);
        
        Image logoImage = logoObj.AddComponent<Image>();
        logoImage.color = new Color(1f, 1f, 1f, 0.8f);
        logoImage.raycastTarget = false;
        
        RectTransform logoRect = logoImage.rectTransform;
        logoRect.anchorMin = new Vector2(0.4f, 0.8f);
        logoRect.anchorMax = new Vector2(0.6f, 0.95f);
        logoRect.offsetMin = Vector2.zero;
        logoRect.offsetMax = Vector2.zero;
        logoRect.anchoredPosition = Vector2.zero;
        
        // Add a placeholder text
        GameObject placeholderTextObj = new GameObject("PlaceholderText");
        placeholderTextObj.transform.SetParent(logoObj.transform, false);
        
        TextMeshProUGUI placeholderText = placeholderTextObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "LOGO";
        placeholderText.fontSize = 24;
        placeholderText.color = Color.black;
        placeholderText.alignment = TextAlignmentOptions.Center;
        placeholderText.raycastTarget = false;
        
        RectTransform placeholderRect = placeholderText.rectTransform;
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        placeholderRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("CreditsSceneSetup: Created logo placeholder");
    }
    
    void SetupCreditsController(CanvasGroup canvasGroup)
    {
        // Check if CreditsController already exists
        CreditsController existingController = FindFirstObjectByType<CreditsController>();
        if (existingController != null)
        {
            Debug.Log("CreditsSceneSetup: CreditsController already exists, configuring references");
            ConfigureExistingCreditsController(existingController, canvasGroup);
            return;
        }
        
        // Create CreditsController
        GameObject controllerObj = new GameObject("CreditsController");
        CreditsController controller = controllerObj.AddComponent<CreditsController>();
        
        // Configure the controller
        controller.mainCanvasGroup = canvasGroup;
        
        // Find and assign UI references
        controller.backgroundImage = FindFirstObjectByType<Image>();
        controller.creditsText = GameObject.Find("CreditsText")?.GetComponent<TextMeshProUGUI>();
        controller.skipInstructionText = GameObject.Find("SkipInstructionText")?.GetComponent<TextMeshProUGUI>();
        controller.logoImage = GameObject.Find("LogoPlaceholder")?.GetComponent<Image>();
        
        // Set credits content
        controller.creditsContent = defaultCreditsText;
        
        // Configure default settings
        controller.fadeInDuration = 1.5f;
        controller.fadeOutDuration = 1.5f;
        controller.creditsDisplayTime = 5f;
        controller.allowSkip = true;
        controller.showSkipInstruction = true;
        controller.useSceneFlowManagerTiming = true;
        controller.useSceneFlowManagerTransitions = true;
        
        Debug.Log("CreditsSceneSetup: Created and configured CreditsController");
    }
    
    void ConfigureExistingCreditsController(CreditsController controller, CanvasGroup canvasGroup)
    {
        // Update references if they're not already set
        if (controller.mainCanvasGroup == null)
        {
            controller.mainCanvasGroup = canvasGroup;
        }
        
        if (controller.creditsText == null)
        {
            controller.creditsText = GameObject.Find("CreditsText")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (controller.skipInstructionText == null)
        {
            controller.skipInstructionText = GameObject.Find("SkipInstructionText")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (controller.backgroundImage == null)
        {
            Image[] images = FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (Image img in images)
            {
                if (img.name.ToLower().Contains("background"))
                {
                    controller.backgroundImage = img;
                    break;
                }
            }
        }
        
        Debug.Log("CreditsSceneSetup: Configured existing CreditsController references");
    }
    
    [ContextMenu("Test Credits Setup")]
    public void TestCreditsSetup()
    {
        CreditsController controller = FindFirstObjectByType<CreditsController>();
        if (controller != null)
        {
            controller.TestFullCreditsSequence();
            Debug.Log("CreditsSceneSetup: Started test credits sequence");
        }
        else
        {
            Debug.LogWarning("CreditsSceneSetup: No CreditsController found. Run 'Setup Credits Scene' first.");
        }
    }
    
    [ContextMenu("Debug Credits Components")]
    public void DebugCreditsComponents()
    {
        Debug.Log("=== Credits Scene Components Debug ===");
        
        Canvas canvas = FindFirstObjectByType<Canvas>();
        Debug.Log($"Canvas: {(canvas != null ? canvas.name : "NOT FOUND")}");
        
        CanvasGroup canvasGroup = FindFirstObjectByType<CanvasGroup>();
        Debug.Log($"CanvasGroup: {(canvasGroup != null ? canvasGroup.name : "NOT FOUND")}");
        
        CreditsController controller = FindFirstObjectByType<CreditsController>();
        Debug.Log($"CreditsController: {(controller != null ? controller.name : "NOT FOUND")}");
        
        TextMeshProUGUI[] texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        Debug.Log($"TextMeshPro components found: {texts.Length}");
        foreach (var text in texts)
        {
            Debug.Log($"  - {text.name}: \"{text.text.Substring(0, Mathf.Min(30, text.text.Length))}...\"");
        }
        
        Image[] images = FindObjectsByType<Image>(FindObjectsSortMode.None);
        Debug.Log($"Image components found: {images.Length}");
        foreach (var img in images)
        {
            Debug.Log($"  - {img.name}: Color {img.color}");
        }
    }
    
    [ContextMenu("Connect Existing Components")]
    public void ConnectExistingComponents()
    {
        Debug.Log("CreditsSceneSetup: Connecting existing components...");
        
        // Find the CreditsController
        CreditsController controller = FindFirstObjectByType<CreditsController>();
        if (controller == null)
        {
            Debug.LogWarning("CreditsSceneSetup: No CreditsController found in scene!");
            return;
        }
        
        // Find or create Canvas and CanvasGroup
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("CreditsSceneSetup: No Canvas found in scene!");
            return;
        }
        
        CanvasGroup canvasGroup = EnsureCanvasGroup(canvas);
        
        // Connect main canvas group
        controller.mainCanvasGroup = canvasGroup;
        Debug.Log($"CreditsSceneSetup: Connected CanvasGroup to controller");
        
        // Find and connect existing credits text
        if (controller.creditsText == null)
        {
            // Look for credits text component in the hierarchy
            TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var text in allTexts)
            {
                if (text.name.ToLower().Contains("credits") && !text.name.ToLower().Contains("skip"))
                {
                    controller.creditsText = text;
                    Debug.Log($"CreditsSceneSetup: Found and connected credits text: {text.name}");
                    
                    // Update the text content if it's different
                    if (text.text != defaultCreditsText)
                    {
                        Debug.Log($"CreditsSceneSetup: Updating credits text content");
                        text.text = defaultCreditsText;
                    }
                    break;
                }
            }
        }
        
        // Create skip instruction if it doesn't exist
        if (controller.skipInstructionText == null)
        {
            Debug.Log("CreditsSceneSetup: Creating skip instruction text");
            CreateSkipInstructionText(canvas.transform);
            controller.skipInstructionText = GameObject.Find("SkipInstructionText")?.GetComponent<TextMeshProUGUI>();
        }
        
        // Find background image
        if (controller.backgroundImage == null)
        {
            Image[] allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (var img in allImages)
            {
                if (img.name.ToLower().Contains("bg") || img.name.ToLower().Contains("background"))
                {
                    controller.backgroundImage = img;
                    Debug.Log($"CreditsSceneSetup: Found and connected background image: {img.name}");
                    break;
                }
            }
        }
        
        // Set credits content
        controller.creditsContent = defaultCreditsText;
        
        // Apply the content to the text component
        if (controller.creditsText != null)
        {
            controller.creditsText.text = defaultCreditsText;
        }
        
        Debug.Log("CreditsSceneSetup: Existing components connected successfully!");
    }
    
    [ContextMenu("Update Credits Text Content")]
    public void UpdateCreditsTextContent()
    {
        CreditsController controller = FindFirstObjectByType<CreditsController>();
        if (controller != null && controller.creditsText != null)
        {
            controller.creditsText.text = defaultCreditsText;
            controller.creditsContent = defaultCreditsText;
            Debug.Log("CreditsSceneSetup: Updated credits text content");
        }
        else
        {
            Debug.LogWarning("CreditsSceneSetup: Could not find CreditsController or credits text component");
        }
    }
    
    [ContextMenu("Create Missing Skip Text")]
    public void CreateMissingSkipText()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("CreditsSceneSetup: No Canvas found!");
            return;
        }
        
        // Check if skip text already exists
        GameObject existingSkip = GameObject.Find("SkipInstructionText");
        if (existingSkip != null)
        {
            Debug.Log("CreditsSceneSetup: Skip instruction text already exists");
            return;
        }
        
        CreateSkipInstructionText(canvas.transform);
        
        // Connect to CreditsController if available
        CreditsController controller = FindFirstObjectByType<CreditsController>();
        if (controller != null)
        {
            controller.skipInstructionText = GameObject.Find("SkipInstructionText")?.GetComponent<TextMeshProUGUI>();
            Debug.Log("CreditsSceneSetup: Connected new skip text to CreditsController");
        }
    }
}