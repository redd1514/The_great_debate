using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Main Menu Selection Indicator with Tekken 8-style gradient effects and blinking animation.
/// Based on PlayerSelectionIndicator to provide consistent visual experience across the game.
/// 
/// Features:
/// - Vertical gradient from solid color (bottom) to transparent/fade (top)
/// - Smooth blinking animation for active selection
/// - Enhanced visual fidelity matching Tekken 8's selection style
/// - Optimized for menu buttons and UI elements
/// </summary>

public enum MainMenuGradientStyle
{
    TekkenClassic,      // Solid color bottom fading to transparent top
    TekkenBright,       // Bright color bottom fading to white top  
    TekkenGlow,         // Enhanced glow effect with color intensity
    Custom              // Custom gradient colors
}

public class MainMenuSelectionIndicator : MonoBehaviour
{
    [Header("Gradient Components")]
    public Image backgroundImage;    // Base image component
    public Image gradientOverlay;    // Applies gradient effect over the background
    
    [Header("Animation Settings")]
    public bool enableBlinking = true;
    [Range(0.5f, 5f)]
    public float blinkSpeed = 3.2f;
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;
    [Range(0f, 1f)] 
    public float maxAlpha = 1f;
    public AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Gradient Style")]
    public MainMenuGradientStyle gradientStyle = MainMenuGradientStyle.TekkenClassic;
    public Color menuHighlightColor = new Color(1f, 0.8f, 0.2f, 1f); // Gold/Yellow for main menu
    
    [Header("Enhanced Effects")]
    [Range(1f, 3f)]
    public float intensityMultiplier = 2.2f;
    [Range(0f, 1f)]
    public float gradientStrength = 0.75f;
    [Range(0f, 1f)]
    public float gradientSoftness = 0.9f;
    
    [Header("Custom Gradient (when style = Custom)")]
    public Color topColor = Color.white;
    public Color bottomColor = new Color(1f, 0.8f, 0.2f, 1f);
    
    [Header("Button Integration")]
    public Button targetButton; // The button this indicator highlights
    public Vector2 sizePadding = new Vector2(20f, 10f); // Extra size around button
    
    private bool isAnimating = false;
    private float animationTime = 0f;
    private Texture2D gradientTexture;
    private Sprite gradientSprite;
    private bool isVisible = false;
    private Vector3 originalScale;
    
    void Awake()
    {
        originalScale = transform.localScale;
        SetupGradientIndicator();
    }
    
    void SetupGradientIndicator()
    {
        // Setup the main image component as background
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
            }
        }
        
        // Configure the background image
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = false;
            backgroundImage.preserveAspect = false;
            backgroundImage.color = Color.clear; // Make background transparent initially
        }
        
        // Create gradient overlay child object
        CreateGradientOverlay();
        
        // Create initial gradient texture
        CreateGradientTexture(menuHighlightColor);
        
        // Start hidden
        SetVisible(false);
        
        Debug.Log("MainMenuSelectionIndicator: Setup complete - Enhanced Tekken 8-style gradient indicator for main menu");
    }
    
    void CreateGradientOverlay()
    {
        // Create gradient overlay as child of this indicator
        GameObject overlayObj = new GameObject("GradientOverlay");
        overlayObj.transform.SetParent(transform, false);
        
        gradientOverlay = overlayObj.AddComponent<Image>();
        gradientOverlay.raycastTarget = false;
        gradientOverlay.preserveAspect = false;
        
        // Setup RectTransform to fill parent
        RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.localScale = Vector3.one;
        
        Debug.Log("MainMenuSelectionIndicator: Created gradient overlay child for menu gradient effect");
    }
    
    void CreateGradientTexture(Color highlightColor)
    {
        // Create a higher resolution vertical gradient texture for better quality
        int width = 16;
        int height = 256; // Higher resolution for smoother gradients
        
        if (gradientTexture != null)
        {
            if (Application.isPlaying)
                Destroy(gradientTexture);
            else
                DestroyImmediate(gradientTexture);
        }
        
        gradientTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        gradientTexture.wrapMode = TextureWrapMode.Clamp;
        gradientTexture.filterMode = FilterMode.Bilinear;
        gradientTexture.anisoLevel = 0;
        
        Color[] pixels = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            float normalizedHeight = (float)y / (height - 1);
            Color gradientColor = GetGradientColor(highlightColor, normalizedHeight);
            
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = gradientColor;
            }
        }
        
        gradientTexture.SetPixels(pixels);
        gradientTexture.Apply();
        
        // Create sprite from texture
        if (gradientSprite != null)
        {
            if (Application.isPlaying)
                Destroy(gradientSprite);
            else
                DestroyImmediate(gradientSprite);
        }
        
        gradientSprite = Sprite.Create(gradientTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        
        // Apply to gradient overlay
        if (gradientOverlay != null)
        {
            gradientOverlay.sprite = gradientSprite;
            gradientOverlay.color = Color.white; // Let the texture handle the coloring
        }
        
        Debug.Log($"MainMenuSelectionIndicator: Created enhanced gradient texture for highlight color {highlightColor}");
    }
    
    Color GetGradientColor(Color highlightColor, float normalizedHeight)
    {
        // Apply intensity multiplier to make colors more vibrant
        Color enhancedColor = new Color(
            Mathf.Min(highlightColor.r * intensityMultiplier, 1f),
            Mathf.Min(highlightColor.g * intensityMultiplier, 1f),
            Mathf.Min(highlightColor.b * intensityMultiplier, 1f),
            highlightColor.a
        );
        
        switch (gradientStyle)
        {
            case MainMenuGradientStyle.TekkenClassic:
                // Solid color at bottom (normalizedHeight = 0) fading to transparent at top (normalizedHeight = 1)
                // Apply softness curve for more natural fade
                float alpha = (1f - Mathf.Pow(normalizedHeight, 1f / gradientSoftness)) * gradientStrength;
                return new Color(enhancedColor.r, enhancedColor.g, enhancedColor.b, alpha);
                
            case MainMenuGradientStyle.TekkenBright:
                // Highlight color at bottom fading to bright white at top
                Color brightResult = Color.Lerp(enhancedColor, Color.white, normalizedHeight);
                brightResult.a *= gradientStrength;
                return brightResult;
                
            case MainMenuGradientStyle.TekkenGlow:
                // Enhanced glow effect - brighter at bottom, with subtle color shift
                float glowIntensity = Mathf.Pow(1f - normalizedHeight, 0.7f) * gradientStrength;
                Color glowColor = Color.Lerp(enhancedColor, enhancedColor * 1.5f, glowIntensity);
                glowColor.a = glowIntensity;
                return glowColor;
                
            case MainMenuGradientStyle.Custom:
                // Custom gradient from bottomColor to topColor
                Color customResult = Color.Lerp(bottomColor, topColor, normalizedHeight);
                customResult.a *= gradientStrength;
                return customResult;
                
            default:
                return enhancedColor;
        }
    }
    
    void Update()
    {
        if (isAnimating && enableBlinking && gradientOverlay != null && isVisible)
        {
            AnimateBlink();
        }
    }
    
    void AnimateBlink()
    {
        animationTime += Time.deltaTime * blinkSpeed;
        
        // Use sine wave with custom curve for more dynamic blinking
        float sineValue = (Mathf.Sin(animationTime) + 1f) / 2f;
        float curveValue = blinkCurve.Evaluate(sineValue);
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, curveValue);
        
        Color currentColor = gradientOverlay.color;
        gradientOverlay.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
    }
    
    // Public interface methods
    public void SetHighlightColor(Color highlightColor)
    {
        menuHighlightColor = highlightColor;
        CreateGradientTexture(highlightColor);
        Debug.Log($"MainMenuSelectionIndicator: Set to enhanced gradient color {highlightColor}");
    }
    
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        
        Debug.Log($"MainMenuSelectionIndicator.SetVisible: {visible}");
        
        if (visible)
        {
            gameObject.SetActive(true);
            
            if (gradientOverlay != null)
            {
                gradientOverlay.color = new Color(1f, 1f, 1f, minAlpha);
                StartAnimation();
            }
        }
        else
        {
            gameObject.SetActive(false);
            StopAnimation();
        }
    }
    
    public void StartAnimation()
    {
        if (isVisible)
        {
            isAnimating = true;
            animationTime = 0f;
            Debug.Log("MainMenuSelectionIndicator: Started enhanced Tekken 8-style blinking animation");
        }
    }
    
    public void StopAnimation()
    {
        isAnimating = false;
        
        if (gradientOverlay != null && isVisible)
        {
            gradientOverlay.color = new Color(1f, 1f, 1f, maxAlpha);
        }
        
        Debug.Log("MainMenuSelectionIndicator: Stopped blinking animation");
    }
    
    public void SetGradientStyle(MainMenuGradientStyle style)
    {
        gradientStyle = style;
        CreateGradientTexture(menuHighlightColor);
        Debug.Log($"MainMenuSelectionIndicator: Changed gradient style to {style}");
    }
    
    public void SetCustomGradientColors(Color bottom, Color top)
    {
        bottomColor = bottom;
        topColor = top;
        
        if (gradientStyle == MainMenuGradientStyle.Custom)
        {
            CreateGradientTexture(menuHighlightColor);
        }
    }
    
    // Method to position the indicator over a button
    public void PositionOverButton(Button button)
    {
        if (button == null) return;
        
        targetButton = button;
        RectTransform indicatorRect = GetComponent<RectTransform>();
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        
        if (indicatorRect == null || buttonRect == null) return;
        
        // Ensure same parent for consistent positioning
        if (indicatorRect.parent != buttonRect.parent)
        {
            // Use world position conversion for accurate positioning
            Vector3 worldPos = buttonRect.TransformPoint(Vector3.zero);
            indicatorRect.position = worldPos;
        }
        else
        {
            // Same parent, use anchored position for precise alignment
            indicatorRect.anchorMin = buttonRect.anchorMin;
            indicatorRect.anchorMax = buttonRect.anchorMax;
            indicatorRect.anchoredPosition = buttonRect.anchoredPosition;
        }
        
        // Match size with padding
        indicatorRect.sizeDelta = buttonRect.sizeDelta + sizePadding;
        indicatorRect.pivot = buttonRect.pivot;
        
        Debug.Log($"MainMenuSelectionIndicator: Positioned over button: {button.name}");
    }
    
    // Method to flash selection for immediate feedback
    public void FlashSelection(float duration = 0.3f)
    {
        if (gameObject.activeInHierarchy && isVisible)
        {
            StartCoroutine(FlashRoutine(duration));
        }
    }
    
    private IEnumerator FlashRoutine(float duration)
    {
        if (gradientOverlay == null) yield break;
        
        Color originalFlashColor = gradientOverlay.color;
        Color flashColor = new Color(1f, 1f, 1f, maxAlpha); // Full intensity for flash
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float flashIntensity = 1f - progress; // Fade out the flash
            
            Color currentFlashColor = Color.Lerp(originalFlashColor, flashColor, flashIntensity * 0.8f);
            gradientOverlay.color = currentFlashColor;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Restore original color
        gradientOverlay.color = originalFlashColor;
    }
    
    // Method to create selection feedback with scale effect
    public void SelectionPulse(float intensity = 1.2f, float duration = 0.2f)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(PulseRoutine(intensity, duration));
        }
    }
    
    private IEnumerator PulseRoutine(float intensity, float duration)
    {
        Vector3 targetScale = originalScale * intensity;
        
        // Scale up
        float elapsed = 0f;
        float halfDuration = duration * 0.5f;
        
        while (elapsed < halfDuration)
        {
            float progress = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float progress = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    void OnDestroy()
    {
        // Clean up texture and sprite
        if (gradientTexture != null)
        {
            if (Application.isPlaying)
                Destroy(gradientTexture);
            else
                DestroyImmediate(gradientTexture);
        }
        
        if (gradientSprite != null)
        {
            if (Application.isPlaying)
                Destroy(gradientSprite);
            else
                DestroyImmediate(gradientSprite);
        }
    }
    
    // Debug and testing methods
    [ContextMenu("Test Tekken Classic Style")]
    public void TestTekkenClassicStyle()
    {
        SetGradientStyle(MainMenuGradientStyle.TekkenClassic);
        SetVisible(true);
        Debug.Log("Testing enhanced Tekken 8 Classic gradient style for main menu");
    }
    
    [ContextMenu("Test Tekken Glow Style")]
    public void TestTekkenGlowStyle()
    {
        SetGradientStyle(MainMenuGradientStyle.TekkenGlow);
        SetVisible(true);
        Debug.Log("Testing enhanced Tekken 8 Glow gradient style for main menu");
    }
    
    [ContextMenu("Test Flash Effect")]
    public void TestFlashEffect()
    {
        FlashSelection(0.5f);
        Debug.Log("Testing flash selection effect");
    }
    
    [ContextMenu("Test Selection Pulse")]
    public void TestSelectionPulse()
    {
        SelectionPulse(1.3f, 0.25f);
        Debug.Log("Testing selection pulse effect");
    }
}