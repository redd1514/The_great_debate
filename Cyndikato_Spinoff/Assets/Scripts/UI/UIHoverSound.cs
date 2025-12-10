using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Add this component to any UI button to automatically play hover sounds when mouse hovers over it.
/// Works with MenuAudioManager or can use its own AudioClip.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIHoverSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    [Header("Hover Audio Settings")]
    [Tooltip("Play sound when mouse hovers over this button")]
    public bool playOnHover = true;
    
    [Tooltip("Play sound when this button is selected via keyboard/controller")]
    public bool playOnSelect = true;
    
    [Tooltip("Custom hover sound (if null, will use MenuAudioManager)")]
    public AudioClip customHoverSound;
    
    [Tooltip("Volume for the hover sound")]
    [Range(0f, 1f)]
    public float hoverVolume = 0.6f;
    
    [Header("Visual Feedback")]
    [Tooltip("Enable subtle visual feedback on hover")]
    public bool enableVisualFeedback = true;
    
    [Tooltip("Scale multiplier when hovering")]
    [Range(0.9f, 1.2f)]
    public float hoverScale = 1.05f;
    
    [Tooltip("Duration of scale animation")]
    public float animationDuration = 0.1f;
    
    private Button button;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private MenuAudioManager menuAudioManager;
    private AudioSource localAudioSource;
    private bool isHovering = false;
    
    void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        
        // Try to find MenuAudioManager
        menuAudioManager = MenuAudioManager.Instance;
        
        // Create local audio source if needed
        if (customHoverSound != null)
        {
            localAudioSource = gameObject.AddComponent<AudioSource>();
            localAudioSource.playOnAwake = false;
            localAudioSource.spatialBlend = 0f; // 2D sound
        }
    }
    
    void Start()
    {
        // Re-attempt to find MenuAudioManager if it wasn't available in Awake
        if (menuAudioManager == null)
        {
            menuAudioManager = MenuAudioManager.Instance;
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playOnHover || !button.interactable) return;
        
        isHovering = true;
        PlayHoverSound();
        
        if (enableVisualFeedback)
        {
            AnimateScale(hoverScale);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovering) return;
        
        isHovering = false;
        
        if (enableVisualFeedback)
        {
            AnimateScale(1f);
        }
    }
    
    public void OnSelect(BaseEventData eventData)
    {
        if (!playOnSelect || !button.interactable) return;
        
        // Only play if this is a keyboard/controller selection, not mouse
        if (eventData is PointerEventData) return;
        
        PlayHoverSound();
        
        if (enableVisualFeedback)
        {
            AnimateScale(hoverScale);
        }
    }
    
    void PlayHoverSound()
    {
        // Use custom sound if available
        if (customHoverSound != null && localAudioSource != null)
        {
            localAudioSource.PlayOneShot(customHoverSound, hoverVolume);
        }
        // Use MenuAudioManager if available
        else if (menuAudioManager != null)
        {
            menuAudioManager.PlayHoverSound();
        }
    }
    
    void AnimateScale(float targetScale)
    {
        // Stop any existing animations
        StopAllCoroutines();
        
        // Start scale animation
        StartCoroutine(ScaleAnimation(targetScale));
    }
    
    System.Collections.IEnumerator ScaleAnimation(float targetScale)
    {
        Vector3 startScale = rectTransform.localScale;
        Vector3 endScale = originalScale * targetScale;
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            
            // Smooth animation curve
            t = Mathf.SmoothStep(0f, 1f, t);
            
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            yield return null;
        }
        
        rectTransform.localScale = endScale;
    }
    
    void OnDisable()
    {
        // Reset scale when disabled
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
        
        isHovering = false;
    }
    
    void OnValidate()
    {
        // Clamp values in inspector
        hoverVolume = Mathf.Clamp01(hoverVolume);
        hoverScale = Mathf.Clamp(hoverScale, 0.5f, 2f);
        animationDuration = Mathf.Max(0.01f, animationDuration);
    }
    
    // Public methods for external control
    public void SetHoverSound(AudioClip clip)
    {
        customHoverSound = clip;
        
        if (clip != null && localAudioSource == null)
        {
            localAudioSource = gameObject.AddComponent<AudioSource>();
            localAudioSource.playOnAwake = false;
            localAudioSource.spatialBlend = 0f;
        }
    }
    
    public void SetHoverVolume(float volume)
    {
        hoverVolume = Mathf.Clamp01(volume);
    }
    
    public void EnableHoverSound(bool enabled)
    {
        playOnHover = enabled;
    }
    
    // Context menu for testing
    [ContextMenu("Test Hover Sound")]
    void TestHoverSound()
    {
        PlayHoverSound();
        Debug.Log($"UIHoverSound: Testing hover sound for {gameObject.name}");
    }
}