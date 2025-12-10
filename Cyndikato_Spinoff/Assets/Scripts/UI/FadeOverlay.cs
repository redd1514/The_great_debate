using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple fade overlay component for smooth screen transitions.
/// Can be used standalone or in combination with other transition effects.
/// </summary>
[RequireComponent(typeof(Image))]
public class FadeOverlay : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("Default fade duration")]
    public float defaultFadeDuration = 1f;
    
    [Tooltip("Fade color (usually black)")]
    public Color fadeColor = Color.black;
    
    [Tooltip("Automatically setup overlay on awake")]
    public bool autoSetup = true;
    
    private Image fadeImage;
    private CanvasGroup canvasGroup;
    private static FadeOverlay instance;
    
    /// <summary>
    /// Singleton instance for easy global access
    /// </summary>
    public static FadeOverlay Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<FadeOverlay>();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
            
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        if (autoSetup)
        {
            SetupFadeOverlay();
        }
    }
    
    void SetupFadeOverlay()
    {
        // Get or create Image component
        fadeImage = GetComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = true; // Block input during fade
        
        // Get or create CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Setup RectTransform to cover entire screen
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        // Ensure this is on a Canvas with proper sorting
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            // Create a canvas for the overlay
            GameObject canvasObj = new GameObject("FadeOverlayCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // High sorting order to appear on top
            
            // Add CanvasScaler and GraphicRaycaster
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Parent the fade overlay to the canvas
            transform.SetParent(canvasObj.transform, false);
            
            Debug.Log("FadeOverlay: Created overlay canvas");
        }
        else
        {
            // Ensure high sorting order
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 1000);
        }
        
        // Start invisible
        SetAlpha(0f);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        Debug.Log("FadeOverlay: Setup complete");
    }
    
    /// <summary>
    /// Fade to black (or fade color)
    /// </summary>
    public void FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        StartCoroutine(FadeCoroutine(0f, 1f, duration));
    }
    
    /// <summary>
    /// Fade from black (or fade color) to transparent
    /// </summary>
    public void FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        StartCoroutine(FadeCoroutine(1f, 0f, duration));
    }
    
    /// <summary>
    /// Fade from current alpha to target alpha
    /// </summary>
    public void FadeTo(float targetAlpha, float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        float currentAlpha = canvasGroup != null ? canvasGroup.alpha : fadeImage.color.a;
        StartCoroutine(FadeCoroutine(currentAlpha, targetAlpha, duration));
    }
    
    /// <summary>
    /// Set alpha immediately without animation
    /// </summary>
    public void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
        
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
        
        // Update interaction settings
        if (canvasGroup != null)
        {
            canvasGroup.interactable = alpha > 0.01f;
            canvasGroup.blocksRaycasts = alpha > 0.01f;
        }
    }
    
    /// <summary>
    /// Get current alpha value
    /// </summary>
    public float GetAlpha()
    {
        if (canvasGroup != null)
        {
            return canvasGroup.alpha;
        }
        else if (fadeImage != null)
        {
            return fadeImage.color.a;
        }
        return 0f;
    }
    
    /// <summary>
    /// Check if fade is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return GetAlpha() > 0.01f;
    }
    
    /// <summary>
    /// Set the fade color
    /// </summary>
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (fadeImage != null)
        {
            Color currentColor = fadeImage.color;
            color.a = currentColor.a; // Preserve alpha
            fadeImage.color = color;
        }
    }
    
    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        
        // Ensure interaction is properly set during fade
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time to work during pause
            float progress = elapsed / duration;
            
            // Apply easing curve for smooth animation
            progress = Mathf.SmoothStep(0f, 1f, progress);
            
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            SetAlpha(currentAlpha);
            
            yield return null;
        }
        
        // Ensure final value is set
        SetAlpha(endAlpha);
        
        Debug.Log($"FadeOverlay: Fade complete - Alpha: {endAlpha}");
    }
    
    /// <summary>
    /// Fade in, wait, then fade out
    /// </summary>
    public IEnumerator FadeInOutCoroutine(float fadeInDuration, float holdDuration, float fadeOutDuration)
    {
        yield return StartCoroutine(FadeCoroutine(GetAlpha(), 1f, fadeInDuration));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(FadeCoroutine(1f, 0f, fadeOutDuration));
    }
    
    // Static convenience methods
    public static void StaticFadeIn(float duration = 1f)
    {
        if (Instance != null)
        {
            Instance.FadeIn(duration);
        }
    }
    
    public static void StaticFadeOut(float duration = 1f)
    {
        if (Instance != null)
        {
            Instance.FadeOut(duration);
        }
    }
    
    public static void StaticSetAlpha(float alpha)
    {
        if (Instance != null)
        {
            Instance.SetAlpha(alpha);
        }
    }
    
    // Context menu methods for testing
    [ContextMenu("Test Fade In")]
    public void TestFadeIn()
    {
        FadeIn();
    }
    
    [ContextMenu("Test Fade Out")]
    public void TestFadeOut()
    {
        FadeOut();
    }
    
    [ContextMenu("Set Alpha 0")]
    public void TestSetAlpha0()
    {
        SetAlpha(0f);
    }
    
    [ContextMenu("Set Alpha 1")]
    public void TestSetAlpha1()
    {
        SetAlpha(1f);
    }
    
    void OnValidate()
    {
        defaultFadeDuration = Mathf.Max(0.1f, defaultFadeDuration);
        
        if (fadeImage != null && Application.isPlaying)
        {
            SetFadeColor(fadeColor);
        }
    }
}