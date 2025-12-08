using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tekken 8-style selection indicator with gradient effects and blinking animation.
/// 
/// Features:
/// - Matches character icon shape perfectly using Mask component
/// - Vertical gradient from solid color (bottom) to transparent/fade (top)
/// - Configurable gradient styles (Classic, Bright, Custom)
/// - Smooth blinking animation for active selection
/// - Dynamic color support for multiple players
/// - No outline effects - uses gradient overlay respecting icon shape
/// - Enhanced visual fidelity matching Tekken 8's selection style
/// </summary>

public enum GradientStyle
{
    TekkenClassic,      // Solid color bottom fading to transparent top
    TekkenBright,       // Bright color bottom fading to white top  
    TekkenGlow,         // Enhanced glow effect with color intensity
    Custom              // Custom gradient colors
}

public class PlayerSelectionIndicator : MonoBehaviour
{
    [Header("Gradient Components")]
    public Image backgroundImage;    // Uses character icon sprite for masking
    public Image gradientOverlay;    // Applies gradient effect over the background
    
    [Header("Animation Settings")]
    public bool enableBlinking = true;
    [Range(0.5f, 5f)]
    public float blinkSpeed = 2.5f;
    [Range(0f, 1f)]
    public float minAlpha = 0.2f;
    [Range(0f, 1f)] 
    public float maxAlpha = 1f;
    public AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Gradient Style")]
    public GradientStyle gradientStyle = GradientStyle.TekkenClassic;
    public Color[] playerColors = { 
        new Color(1f, 0.2f, 0.2f, 1f),     // Red
        new Color(0.2f, 0.4f, 1f, 1f),     // Blue  
        new Color(0.2f, 1f, 0.3f, 1f),     // Green
        new Color(1f, 0.8f, 0.2f, 1f)      // Yellow
    };
    
    [Header("Enhanced Effects")]
    [Range(1f, 3f)]
    public float intensityMultiplier = 1.5f;
    [Range(0f, 1f)]
    public float gradientSoftness = 0.8f;
    [Range(0f, 1f)]
    public float gradientStrength = 0.6f;
    
    [Header("Custom Gradient (when style = Custom)")]
    public Color topColor = Color.white;
    public Color bottomColor = Color.red;
    
    private int currentPlayerIndex = -1;
    private bool isAnimating = false;
    private float animationTime = 0f;
    private Texture2D gradientTexture;
    private Sprite gradientSprite;
    private Sprite originalCharacterSprite;
    private Mask maskComponent;
    
    void Awake()
    {
        SetupGradientIndicator();
    }
    
    void SetupGradientIndicator()
    {
        // Setup the main image component as background (will hold character sprite)
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        
        if (backgroundImage == null)
            backgroundImage = gameObject.AddComponent<Image>();
        
        // Configure the background image
        backgroundImage.raycastTarget = false;
        backgroundImage.preserveAspect = true;
        backgroundImage.color = Color.clear; // Make background transparent
        
        // Add Mask component to respect character icon shape
        maskComponent = GetComponent<Mask>();
        if (maskComponent == null)
        {
            maskComponent = gameObject.AddComponent<Mask>();
            maskComponent.showMaskGraphic = false; // Hide the mask graphic, show only children
        }
        
        // Create gradient overlay child object
        CreateGradientOverlay();
        
        // Remove any Outline component if it exists (Tekken 8 doesn't use outlines)
        Outline outline = GetComponent<Outline>();
        if (outline != null)
        {
            if (Application.isPlaying)
                Destroy(outline);
            else
                DestroyImmediate(outline);
            Debug.Log("PlayerSelectionIndicator: Removed Outline component for authentic Tekken 8 gradient effect");
        }
        
        // Create initial gradient texture
        CreateGradientTexture(playerColors[0]); // Default to first player color
        
        Debug.Log("PlayerSelectionIndicator: Setup complete - Enhanced Tekken 8-style gradient indicator with icon masking");
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
        
        Debug.Log("PlayerSelectionIndicator: Created gradient overlay child for masked gradient effect");
    }
    
    void CreateGradientTexture(Color playerColor)
    {
        // Create a higher resolution vertical gradient texture for better quality
        int width = 8;
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
            Color gradientColor = GetGradientColor(playerColor, normalizedHeight);
            
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
        
        Debug.Log($"PlayerSelectionIndicator: Created enhanced gradient texture for player color {playerColor}");
    }
    
    Color GetGradientColor(Color playerColor, float normalizedHeight)
    {
        // Apply intensity multiplier to make colors more vibrant
        Color enhancedColor = new Color(
            Mathf.Min(playerColor.r * intensityMultiplier, 1f),
            Mathf.Min(playerColor.g * intensityMultiplier, 1f), 
            Mathf.Min(playerColor.b * intensityMultiplier, 1f),
            playerColor.a
        );
        
        switch (gradientStyle)
        {
            case GradientStyle.TekkenClassic:
                // Solid color at bottom (normalizedHeight = 0) fading to transparent at top (normalizedHeight = 1)
                // Apply softness curve for more natural fade
                float alpha = (1f - Mathf.Pow(normalizedHeight, 1f / gradientSoftness)) * gradientStrength;
                return new Color(enhancedColor.r, enhancedColor.g, enhancedColor.b, alpha);
                
            case GradientStyle.TekkenBright:
                // Player color at bottom fading to bright white at top
                Color brightResult = Color.Lerp(enhancedColor, Color.white, normalizedHeight);
                brightResult.a *= gradientStrength;
                return brightResult;
                
            case GradientStyle.TekkenGlow:
                // Enhanced glow effect - brighter at bottom, with subtle color shift
                float glowIntensity = Mathf.Pow(1f - normalizedHeight, 0.7f) * gradientStrength;
                Color glowColor = Color.Lerp(enhancedColor, enhancedColor * 1.5f, glowIntensity);
                glowColor.a = glowIntensity;
                return glowColor;
                
            case GradientStyle.Custom:
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
        if (isAnimating && enableBlinking && gradientOverlay != null)
        {
            AnimateBlink();
        }
    }
    
    public void SetPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerColors.Length) return;
        
        currentPlayerIndex = playerIndex;
        Color playerColor = playerColors[playerIndex];
        
        // Recreate gradient texture with new player color
        CreateGradientTexture(playerColor);
        
        Debug.Log($"PlayerSelectionIndicator: Set to Player {playerIndex + 1} with enhanced Tekken 8-style gradient color {playerColor}");
    }
    
    public void SetCharacterSprite(Sprite characterSprite)
    {
        originalCharacterSprite = characterSprite;
        
        if (backgroundImage != null)
        {
            backgroundImage.sprite = characterSprite;
            
            // Enable mask graphic to show the character shape
            if (maskComponent != null)
            {
                maskComponent.showMaskGraphic = true;
                backgroundImage.color = new Color(1, 1, 1, 0.01f); // Very low alpha for masking only
            }
        }
        
        Debug.Log($"PlayerSelectionIndicator: Set character sprite for masking: {(characterSprite != null ? characterSprite.name : "null")}");
    }
    
    public void SetGradientStyle(GradientStyle style)
    {
        gradientStyle = style;
        
        // Recreate gradient with current player color if set
        if (currentPlayerIndex >= 0 && currentPlayerIndex < playerColors.Length)
        {
            CreateGradientTexture(playerColors[currentPlayerIndex]);
        }
        
        Debug.Log($"PlayerSelectionIndicator: Changed gradient style to {style}");
    }
    
    public void SetCustomGradientColors(Color bottom, Color top)
    {
        bottomColor = bottom;
        topColor = top;
        
        if (gradientStyle == GradientStyle.Custom && currentPlayerIndex >= 0)
        {
            CreateGradientTexture(playerColors[currentPlayerIndex]);
        }
    }
    
    public void StartAnimation()
    {
        isAnimating = true;
        animationTime = 0f;
        
        // Ensure the indicator is visible when starting animation
        gameObject.SetActive(true);
        
        Debug.Log("PlayerSelectionIndicator: Started enhanced Tekken 8-style blinking animation");
    }
    
    public void StopAnimation()
    {
        isAnimating = false;
        
        // Reset to full alpha with smooth transition
        if (gradientOverlay != null)
        {
            Color currentColor = gradientOverlay.color;
            gradientOverlay.color = new Color(currentColor.r, currentColor.g, currentColor.b, maxAlpha);
        }
        
        Debug.Log("PlayerSelectionIndicator: Stopped blinking animation");
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
    
    // Method to update colors if they change dynamically
    public void UpdatePlayerColors(Color[] newPlayerColors)
    {
        if (newPlayerColors != null && newPlayerColors.Length >= 4)
        {
            playerColors = newPlayerColors;
            
            // Re-apply current player color if set
            if (currentPlayerIndex >= 0)
            {
                SetPlayer(currentPlayerIndex);
            }
            
            Debug.Log("PlayerSelectionIndicator: Updated player colors");
        }
    }
    
    // Method to ensure indicator is properly positioned and sized
    public void PositionOverCharacterIcon(RectTransform characterIconRect, Image characterIconImage = null)
    {
        if (characterIconRect == null) return;
        
        RectTransform indicatorRect = GetComponent<RectTransform>();
        if (indicatorRect == null) return;
        
        // Copy character icon sprite for masking
        if (characterIconImage != null && characterIconImage.sprite != null)
        {
            SetCharacterSprite(characterIconImage.sprite);
        }
        
        // Ensure same parent for consistent positioning
        if (indicatorRect.parent != characterIconRect.parent)
        {
            // Use world position conversion for accurate positioning
            Vector3 worldPos = characterIconRect.TransformPoint(Vector3.zero);
            indicatorRect.position = worldPos;
        }
        else
        {
            // Same parent, use anchored position for precise alignment
            indicatorRect.anchorMin = characterIconRect.anchorMin;
            indicatorRect.anchorMax = characterIconRect.anchorMax;
            indicatorRect.anchoredPosition = characterIconRect.anchoredPosition;
        }
        
        // Match size exactly
        indicatorRect.sizeDelta = characterIconRect.sizeDelta;
        indicatorRect.pivot = characterIconRect.pivot;
        
        Debug.Log($"PlayerSelectionIndicator: Positioned over character icon with sprite masking");
    }
    
    // Enhanced method to set player with immediate visual feedback
    public void SetPlayerWithAnimation(int playerIndex)
    {
        SetPlayer(playerIndex);
        StartAnimation();
    }
    
    // Method to temporarily boost intensity for selection feedback
    public void FlashSelection(float duration = 0.3f)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashRoutine(duration));
        }
    }
    
    private System.Collections.IEnumerator FlashRoutine(float duration)
    {
        float originalStrength = gradientStrength;
        float flashStrength = Mathf.Min(originalStrength * 1.5f, 1f);
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            gradientStrength = Mathf.Lerp(flashStrength, originalStrength, progress);
            
            // Recreate gradient with new strength
            if (currentPlayerIndex >= 0)
            {
                CreateGradientTexture(playerColors[currentPlayerIndex]);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        gradientStrength = originalStrength;
        if (currentPlayerIndex >= 0)
        {
            CreateGradientTexture(playerColors[currentPlayerIndex]);
        }
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
    
    // Backward compatibility property
    public Image gradientImage 
    { 
        get { return gradientOverlay; } 
        set { gradientOverlay = value; } 
    }
    
    // Debug and testing methods
    [ContextMenu("Test Tekken Classic Style")]
    public void TestTekkenClassicStyle()
    {
        SetGradientStyle(GradientStyle.TekkenClassic);
        SetPlayerWithAnimation(0); // Test with Player 1 (red)
        Debug.Log("Testing enhanced Tekken 8 Classic gradient style with masking");
    }
    
    [ContextMenu("Test Tekken Glow Style")]
    public void TestTekkenGlowStyle()
    {
        SetGradientStyle(GradientStyle.TekkenGlow);
        SetPlayerWithAnimation(0);
        Debug.Log("Testing enhanced Tekken 8 Glow gradient style with masking");
    }
    
    [ContextMenu("Flash Selection Test")]
    public void TestFlashSelection()
    {
        FlashSelection(0.5f);
        Debug.Log("Testing flash selection effect");
    }
    
    // Editor helper methods for previewing different gradient styles
    [ContextMenu("Preview Classic Style")]
    public void PreviewClassicStyle()
    {
        SetGradientStyle(GradientStyle.TekkenClassic);
        if (currentPlayerIndex >= 0) SetPlayer(currentPlayerIndex);
    }
    
    [ContextMenu("Preview Bright Style")]  
    public void PreviewBrightStyle()
    {
        SetGradientStyle(GradientStyle.TekkenBright);
        if (currentPlayerIndex >= 0) SetPlayer(currentPlayerIndex);
    }
    
    [ContextMenu("Preview Glow Style")]
    public void PreviewGlowStyle()
    {
        SetGradientStyle(GradientStyle.TekkenGlow);
        if (currentPlayerIndex >= 0) SetPlayer(currentPlayerIndex);
    }
}