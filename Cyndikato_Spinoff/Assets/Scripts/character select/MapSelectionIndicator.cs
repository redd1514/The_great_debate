using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual indicator for map selection that shows which player is selecting/has voted for a map.
/// Enhanced to work similar to PlayerSelectionIndicator with proper gradient effects and animations.
/// Map Selection Indicator with Tekken 8-style gradient effects and blinking animation.
/// Based on PlayerSelectionIndicator to provide consistent visual experience.
/// 
/// Features:
/// - Vertical gradient from solid color (bottom) to transparent/fade (top)
/// - Smooth blinking animation for active selection
/// - Dynamic color support for multiple players
/// - Enhanced visual fidelity matching Tekken 8's selection style
/// </summary>

public enum MapGradientStyle
{
    TekkenClassic,      // Solid color bottom fading to transparent top
    TekkenBright,       // Bright color bottom fading to white top  
    TekkenGlow,         // Enhanced glow effect with color intensity
    Custom              // Custom gradient colors
}

public class MapSelectionIndicator : MonoBehaviour
{
    [Header("Gradient Components")]
    public Image backgroundImage;    // Uses map icon sprite for masking (optional)
    public Image gradientOverlay;    // Applies gradient effect over the background
    
    [Header("Animation Settings")]
    public bool enableBlinking = true;
    [Range(0.5f, 5f)]
    public float blinkSpeed = 2.8f;
    [Range(0f, 1f)]
    public float minAlpha = 0.15f;
    [Range(0f, 1f)] 
    public float maxAlpha = 0.95f;
    public AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Gradient Style")]
    public MapGradientStyle gradientStyle = MapGradientStyle.TekkenClassic;
    public Color[] playerColors = { 
        new Color(1f, 0.2f, 0.2f, 1f),     // Red
        new Color(0.2f, 0.4f, 1f, 1f),     // Blue  
        new Color(0.2f, 1f, 0.3f, 1f),     // Green
        new Color(1f, 0.8f, 0.2f, 1f)      // Yellow
    };
    
    [Header("Enhanced Effects")]
    [Range(1f, 3f)]
    public float intensityMultiplier = 1.8f;
    [Range(0f, 1f)]
    public float gradientStrength = 0.6f;
    public float gradientSoftness = 0.85f;
    
    [Header("Custom Gradient (when style = Custom)")]
    public Color topColor = Color.white;
    public Color bottomColor = Color.red;
    
    // Legacy support for old MapSelectionIndicator interface
    [Header("Legacy Support")]
    public Image borderImage;
    public GameObject lockIcon;
    public bool enableBlinkAnimation = true;
    
    private int currentPlayerIndex = -1;
    private bool isAnimating = false;
    private float animationTime = 0f;
    private Texture2D gradientTexture;
    private Sprite gradientSprite;
    private Sprite originalMapSprite;
    private Mask maskComponent;
    private bool isLocked = false;
    private bool isVisible = false;
    
    void Awake()
    {
        SetupGradientIndicator();
    }
    
    void SetupGradientIndicator()
    {
        // Setup the main image component as background (will hold map sprite if using masking)
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        
            if (backgroundImage == null)
            {
                // Add Image component if missing
                backgroundImage = gameObject.AddComponent<Image>();
            }
        }
        
        // Configure the background image
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = false;
            backgroundImage.preserveAspect = false; // Fill the area completely for maps
            backgroundImage.color = Color.clear; // Make background transparent initially
        }
        
        // Create gradient overlay child object
        CreateGradientOverlay();
        
        // Create initial gradient texture
        CreateGradientTexture(playerColors[0]); // Default to first player color
        
        Debug.Log("MapSelectionIndicator: Setup complete - Enhanced Tekken 8-style gradient indicator");
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
        
        Debug.Log("MapSelectionIndicator: Created gradient overlay child for masked gradient effect");
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
        
        Debug.Log($"MapSelectionIndicator: Created enhanced gradient texture for player color {playerColor}");
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
            case MapGradientStyle.TekkenClassic:
                // Solid color at bottom (normalizedHeight = 0) fading to transparent at top (normalizedHeight = 1)
                // Apply softness curve for more natural fade
                float alpha = (1f - Mathf.Pow(normalizedHeight, 1f / gradientSoftness)) * gradientStrength;
                return new Color(enhancedColor.r, enhancedColor.g, enhancedColor.b, alpha);
                
            case MapGradientStyle.TekkenBright:
                // Player color at bottom fading to bright white at top
                Color brightResult = Color.Lerp(enhancedColor, Color.white, normalizedHeight);
                brightResult.a *= gradientStrength;
                return brightResult;
                
            case MapGradientStyle.TekkenGlow:
                // Enhanced glow effect - brighter at bottom, with subtle color shift
                float glowIntensity = Mathf.Pow(1f - normalizedHeight, 0.7f) * gradientStrength;
                Color glowColor = Color.Lerp(enhancedColor, enhancedColor * 1.5f, glowIntensity);
                glowColor.a = glowIntensity;
                return glowColor;
                
            case MapGradientStyle.Custom:
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
        if (isAnimating && enableBlinking && gradientOverlay != null && isVisible && !isLocked)
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
    
    // Interface methods to match MapSelectionManager expectations
    public void SetPlayerColor(Color playerColor)
    {
        // Apply intensity multiplier for more vibrant colors
        Color enhancedColor = new Color(
            Mathf.Min(playerColor.r * intensityMultiplier, 1f),
            Mathf.Min(playerColor.g * intensityMultiplier, 1f),
            Mathf.Min(playerColor.b * intensityMultiplier, 1f),
            playerColor.a
        );
        
        // Update player colors array
        if (currentPlayerIndex >= 0 && currentPlayerIndex < playerColors.Length)
        {
            playerColors[currentPlayerIndex] = enhancedColor;
        }
        
        // Recreate gradient texture with new player color
        CreateGradientTexture(enhancedColor);
        
        Debug.Log($"MapSelectionIndicator: Set to enhanced gradient color {enhancedColor}");
    }
    
    public void SetPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerColors.Length) return;
        
        currentPlayerIndex = playerIndex;
        Color playerColor = playerColors[playerIndex];
        
        // Recreate gradient texture with new player color
        CreateGradientTexture(playerColor);
        
        Debug.Log($"MapSelectionIndicator: Set to Player {playerIndex + 1} with enhanced Tekken 8-style gradient");
    }
    
    public void SetSelectionState(bool isSelecting)
    {
        isAnimating = isSelecting && !isLocked && isVisible;
        
        Debug.Log($"MapSelectionIndicator.SetSelectionState: isSelecting={isSelecting}, isLocked={isLocked}, isVisible={isVisible}");
        
        if (gradientOverlay != null && isVisible)
        {
            if (isLocked)
            {
                // Solid color for locked vote
                gradientOverlay.color = new Color(1f, 1f, 1f, maxAlpha);
            }
            else if (isSelecting)
            {
                // Start with transparent color - animation will handle blinking
                gradientOverlay.color = new Color(1f, 1f, 1f, minAlpha);
            }
            else
            {
                gradientOverlay.color = Color.clear; // Hidden when not selecting
            }
        }
    }
    
    public void SetLockedState(bool locked)
    {
        isLocked = locked;
        isAnimating = !locked && isVisible; // Only animate if visible and not locked
        
        Debug.Log($"MapSelectionIndicator.SetLockedState: locked={locked}, isVisible={isVisible}");
        
        if (gradientOverlay != null && isVisible)
        {
            if (locked)
            {
                // Solid color when locked
                gradientOverlay.color = new Color(1f, 1f, 1f, maxAlpha);
            }
            else
            {
                // Semi-transparent when unlocked
                gradientOverlay.color = new Color(1f, 1f, 1f, minAlpha);
                isAnimating = true; // Start blinking again
            }
        }
        
        if (lockIcon != null)
        {
            lockIcon.SetActive(locked);
        }
        
        // Add scale effect for feedback
        if (locked && isVisible)
        {
            StartCoroutine(LockPulseEffect());
        }
    }
    
    System.Collections.IEnumerator LockPulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        
        // Scale up
        float elapsed = 0f;
        float duration = 0.1f;
        
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        
        Debug.Log($"MapSelectionIndicator.SetVisible: {visible}");
        
        if (visible)
        {
            gameObject.SetActive(true);
            
            // Apply current state colors
            if (gradientOverlay != null)
            {
                if (isLocked)
                {
                    gradientOverlay.color = new Color(1f, 1f, 1f, maxAlpha);
                }
                else
                {
                    gradientOverlay.color = new Color(1f, 1f, 1f, minAlpha);
                    isAnimating = true; // Start blinking when made visible
                }
            }
        }
        else
        {
            gameObject.SetActive(false);
            isAnimating = false;
        }
    }
    
    // Enhanced methods matching PlayerSelectionIndicator
    public void StartAnimation()
    {
        if (isVisible && !isLocked)
        {
            isAnimating = true;
            animationTime = 0f;
            Debug.Log("MapSelectionIndicator: Started enhanced Tekken 8-style blinking animation");
        }
    }
    
    public void StopAnimation()
    {
        isAnimating = false;
        
        if (gradientOverlay != null && isVisible)
        {
            gradientOverlay.color = new Color(1f, 1f, 1f, maxAlpha);
        }
        
        Debug.Log("MapSelectionIndicator: Stopped blinking animation");
    }
    
    public void SetGradientStyle(MapGradientStyle style)
    {
        gradientStyle = style;
        
        // Recreate gradient with current player color if set
        if (currentPlayerIndex >= 0 && currentPlayerIndex < playerColors.Length)
        {
            CreateGradientTexture(playerColors[currentPlayerIndex]);
        }
        
        Debug.Log($"MapSelectionIndicator: Changed gradient style to {style}");
    }
    
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
            
            Debug.Log("MapSelectionIndicator: Updated player colors");
        }
    }
    
    // Method to flash selection for feedback
    public void FlashSelection(float duration = 0.3f)
    {
        if (gameObject.activeInHierarchy && isVisible)
        {
            StartCoroutine(FlashRoutine(duration));
        }
    }
    
    private System.Collections.IEnumerator FlashRoutine(float duration)
    {
        if (gradientOverlay == null) yield break;
        
        Color originalFlashColor = gradientOverlay.color;
        Color flashColor = new Color(1f, 1f, 1f, maxAlpha); // Full intensity for flash
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float flashIntensity = 1f - progress; // Fade out the flash
            
            Color currentFlashColor = Color.Lerp(originalFlashColor, flashColor, flashIntensity * 0.5f);
            gradientOverlay.color = currentFlashColor;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Restore original color
        gradientOverlay.color = originalFlashColor;
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
        SetGradientStyle(MapGradientStyle.TekkenClassic);
        SetPlayer(0); // Test with Player 1 (red)
        StartAnimation();
        Debug.Log("Testing enhanced Tekken 8 Classic gradient style");
    }
    
    [ContextMenu("Test Flash Effect")]
    public void TestFlashEffect()
    {
        FlashSelection(0.5f);
        Debug.Log("Testing flash selection effect");
    }
}