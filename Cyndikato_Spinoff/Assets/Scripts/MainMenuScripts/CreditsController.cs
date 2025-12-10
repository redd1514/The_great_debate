using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the credits scene with fade in/out animations and smooth transitions.
/// Handles the display of credits and transitions to the next scene in the flow.
/// Integrates with SceneFlowManager for proper scene flow coordination.
/// </summary>
public class CreditsController : MonoBehaviour
{
    [Header("Fade Animation Settings")]
    [Tooltip("Duration of fade in animation")]
    public float fadeInDuration = 1.5f;
    
    [Tooltip("Duration of fade out animation")]
    public float fadeOutDuration = 1.5f;
    
    [Tooltip("Time to display credits before fade out (overridden by SceneFlowManager if available)")]
    public float creditsDisplayTime = 4f;
    
    [Tooltip("Delay before starting fade in")]
    public float initialDelay = 0.5f;
    
    [Header("SceneFlowManager Integration")]
    [Tooltip("Use SceneFlowManager timing if available")]
    public bool useSceneFlowManagerTiming = true;
    
    [Tooltip("Let SceneFlowManager handle scene transitions")]
    public bool useSceneFlowManagerTransitions = true;
    
    [Header("UI References")]
    [Tooltip("Main Canvas Group for fading the entire screen")]
    public CanvasGroup mainCanvasGroup;
    
    [Tooltip("Background image for credits")]
    public Image backgroundImage;
    
    [Tooltip("Credits text component")]
    public TextMeshProUGUI creditsText;
    
    [Tooltip("Optional logo or title image")]
    public Image logoImage;
    
    [Header("Credits Content")]
    [TextArea(5, 15)]
    [Tooltip("Credits text content")]
    public string creditsContent = "Game Development\n\nProgramming\nYour Name\n\nArt & Design\nYour Name\n\nMusic & Audio\nYour Name\n\nSpecial Thanks\nUnity Technologies\n\nThank you for playing!";
    
    [Header("Input Settings")]
    [Tooltip("Allow skipping credits with input")]
    public bool allowSkip = true;
    
    [Tooltip("Show skip instruction text")]
    public bool showSkipInstruction = true;
    
    [Tooltip("Skip instruction text")]
    public TextMeshProUGUI skipInstructionText;
    
    [Header("Audio Settings")]
    [Tooltip("Optional credits music")]
    public AudioClip creditsMusic;
    
    [Tooltip("Volume for credits music")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;
    
    [Tooltip("Fade out music with credits")]
    public bool fadeOutMusic = true;
    
    private AudioSource audioSource;
    private bool isSkipped = false;
    private bool isTransitioning = false;
    private bool creditsCompleted = false;
    private Coroutine creditsSequence;
    private float effectiveDisplayTime;
    
    void Awake()
    {
        SetupComponents();
        ValidateReferences();
        DetermineEffectiveDisplayTime();
    }
    
    void Start()
    {
        StartCreditsSequence();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void DetermineEffectiveDisplayTime()
    {
        // Check if SceneFlowManager is available and we should use its timing
        if (useSceneFlowManagerTiming && SceneFlowManager.Instance != null)
        {
            effectiveDisplayTime = SceneFlowManager.Instance.creditsDisplayTime;
            Debug.Log($"CreditsController: Using SceneFlowManager display time: {effectiveDisplayTime}s");
        }
        else
        {
            effectiveDisplayTime = creditsDisplayTime;
            Debug.Log($"CreditsController: Using local display time: {effectiveDisplayTime}s");
        }
    }
    
    void SetupComponents()
    {
        // Auto-create CanvasGroup if not assigned
        if (mainCanvasGroup == null)
        {
            mainCanvasGroup = GetComponent<CanvasGroup>();
            if (mainCanvasGroup == null)
            {
                // Try to find it in children
                mainCanvasGroup = GetComponentInChildren<CanvasGroup>();
                if (mainCanvasGroup == null)
                {
                    // Create one on a child Canvas
                    Canvas canvas = GetComponentInChildren<Canvas>();
                    if (canvas != null)
                    {
                        mainCanvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
                    }
                    else
                    {
                        // Create one on this GameObject
                        mainCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                    }
                    Debug.Log("CreditsController: Created CanvasGroup for fading");
                }
            }
        }
        
        // Setup audio source for music
        if (creditsMusic != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.clip = creditsMusic;
            audioSource.volume = musicVolume;
            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }
        
        // Set initial state - start completely transparent
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 0f;
            mainCanvasGroup.interactable = false;
            mainCanvasGroup.blocksRaycasts = false;
        }
    }
    
    void ValidateReferences()
    {
        // Auto-find components if not assigned
        if (creditsText == null)
        {
            creditsText = GetComponentInChildren<TextMeshProUGUI>();
            if (creditsText != null)
            {
                Debug.Log("CreditsController: Auto-found credits text component");
            }
        }
        
        if (backgroundImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>();
            if (images.Length > 0)
            {
                // Try to find an image that looks like a background
                foreach (Image img in images)
                {
                    if (img.name.ToLower().Contains("background") || 
                        (img.rectTransform.anchorMin == Vector2.zero && img.rectTransform.anchorMax == Vector2.one))
                    {
                        backgroundImage = img;
                        Debug.Log($"CreditsController: Auto-found background image: {img.name}");
                        break;
                    }
                }
            }
        }
        
        // Setup credits content
        if (creditsText != null && !string.IsNullOrEmpty(creditsContent))
        {
            creditsText.text = creditsContent;
        }
        
        // Setup skip instruction
        if (showSkipInstruction && skipInstructionText != null)
        {
            skipInstructionText.text = "Press any key to skip";
            skipInstructionText.alpha = 0f; // Start invisible
        }
    }
    
    void StartCreditsSequence()
    {
        Debug.Log("CreditsController: Starting credits sequence with fade animations");
        creditsSequence = StartCoroutine(CreditsSequenceCoroutine());
    }
    
    IEnumerator CreditsSequenceCoroutine()
    {
        // Initial delay
        yield return new WaitForSeconds(initialDelay);
        
        // Start music if available
        if (audioSource != null && creditsMusic != null)
        {
            audioSource.Play();
            Debug.Log("CreditsController: Started credits music");
        }
        
        // Fade in
        yield return StartCoroutine(FadeIn());
        
        // Show skip instruction after fade in
        if (showSkipInstruction && skipInstructionText != null)
        {
            StartCoroutine(FadeInSkipInstruction());
        }
        
        // Display credits for the effective time
        float displayTimer = 0f;
        while (displayTimer < effectiveDisplayTime && !isSkipped)
        {
            displayTimer += Time.deltaTime;
            yield return null;
        }
        
        creditsCompleted = true;
        
        if (!isSkipped)
        {
            // Check if SceneFlowManager will handle the transition
            if (useSceneFlowManagerTransitions && SceneFlowManager.Instance != null && SceneFlowManager.Instance.autoTransitionFromCredits)
            {
                Debug.Log("CreditsController: Credits display complete, letting SceneFlowManager handle transition");
                // SceneFlowManager will handle the transition, we just fade out
                yield return StartCoroutine(FadeOut());
            }
            else
            {
                // Handle transition ourselves
                Debug.Log("CreditsController: Credits display complete, handling transition locally");
                yield return StartCoroutine(FadeOutAndTransition());
            }
        }
    }
    
    IEnumerator FadeIn()
    {
        Debug.Log("CreditsController: Starting fade in animation");
        
        if (mainCanvasGroup == null) 
        {
            Debug.LogWarning("CreditsController: No CanvasGroup found for fade in!");
            yield break;
        }
        
        float elapsed = 0f;
        float startAlpha = mainCanvasGroup.alpha;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInDuration;
            
            // Smooth fade curve using easing
            progress = EaseInOutQuad(progress);
            
            mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
            
            yield return null;
        }
        
        mainCanvasGroup.alpha = 1f;
        mainCanvasGroup.interactable = true;
        mainCanvasGroup.blocksRaycasts = true;
        
        Debug.Log("CreditsController: Fade in animation complete");
    }
    
    IEnumerator FadeOut()
    {
        Debug.Log("CreditsController: Starting fade out animation");
        
        if (mainCanvasGroup == null)
        {
            Debug.LogWarning("CreditsController: No CanvasGroup found for fade out!");
            yield break;
        }
        
        // Start music fade out if enabled
        if (fadeOutMusic && audioSource != null && audioSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic());
        }
        
        float elapsed = 0f;
        float startAlpha = mainCanvasGroup.alpha;
        
        mainCanvasGroup.interactable = false;
        mainCanvasGroup.blocksRaycasts = false;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutDuration;
            
            // Smooth fade curve using easing
            progress = EaseInOutQuad(progress);
            
            mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            
            yield return null;
        }
        
        mainCanvasGroup.alpha = 0f;
        
        Debug.Log("CreditsController: Fade out animation complete");
    }
    
    IEnumerator FadeOutAndTransition()
    {
        isTransitioning = true;
        
        yield return StartCoroutine(FadeOut());
        
        // Transition to next scene
        TransitionToNextScene();
    }
    
    IEnumerator FadeInSkipInstruction()
    {
        if (skipInstructionText == null) yield break;
        
        yield return new WaitForSeconds(1f); // Wait a bit after main fade in
        
        float elapsed = 0f;
        float duration = 0.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            skipInstructionText.alpha = Mathf.Lerp(0f, 0.7f, progress);
            
            yield return null;
        }
        
        skipInstructionText.alpha = 0.7f;
    }
    
    IEnumerator FadeOutMusic()
    {
        if (audioSource == null) yield break;
        
        float startVolume = audioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration && audioSource.isPlaying)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutDuration;
            
            audioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            
            yield return null;
        }
        
        audioSource.volume = 0f;
        audioSource.Stop();
    }
    
    // Easing function for smooth animation curves
    float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    }
    
    void HandleInput()
    {
        if (!allowSkip || isSkipped || isTransitioning) return;
        
        // Check for any key press or controller button
        bool inputDetected = Input.anyKeyDown;
        
        // Also check for controller input
        if (!inputDetected)
        {
            for (int i = 0; i < 4; i++)
            {
                string joystickName = $"joystick {i + 1}";
                for (int button = 0; button < 20; button++)
                {
                    if (Input.GetKeyDown($"{joystickName} button {button}"))
                    {
                        inputDetected = true;
                        break;
                    }
                }
                if (inputDetected) break;
            }
        }
        
        if (inputDetected)
        {
            SkipCredits();
        }
    }
    
    public void SkipCredits()
    {
        if (isSkipped || isTransitioning) return;
        
        Debug.Log("CreditsController: Credits skipped by user input");
        isSkipped = true;
        
        // Stop current sequence
        if (creditsSequence != null)
        {
            StopCoroutine(creditsSequence);
        }
        
        // If using SceneFlowManager, tell it to skip
        if (useSceneFlowManagerTransitions && SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.SkipCredits();
            // Just fade out, SceneFlowManager will handle the transition
            StartCoroutine(FadeOut());
        }
        else
        {
            // Handle transition ourselves
            StartCoroutine(FadeOutAndTransition());
        }
    }
    
    void TransitionToNextScene()
    {
        Debug.Log("CreditsController: Transitioning to main menu");
        
        // Use SceneFlowManager if available
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadSceneWithLoading(SceneFlowManager.Instance.mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("CreditsController: SceneFlowManager not found, loading Menu scene directly");
            SceneManager.LoadScene("Menu");
        }
    }
    
    // Public methods for external control
    public void SetCreditsText(string newCreditsText)
    {
        creditsContent = newCreditsText;
        if (creditsText != null)
        {
            creditsText.text = creditsContent;
        }
    }
    
    public void SetDisplayTime(float time)
    {
        creditsDisplayTime = time;
        effectiveDisplayTime = time;
    }
    
    public void SetFadeTimings(float fadeIn, float fadeOut)
    {
        fadeInDuration = fadeIn;
        fadeOutDuration = fadeOut;
    }
    
    // Public properties for SceneFlowManager integration
    public bool IsCreditsCompleted => creditsCompleted;
    public bool IsTransitioning => isTransitioning;
    public float EffectiveDisplayTime => effectiveDisplayTime;
    
    // Context menu methods for testing
    [ContextMenu("Test Fade In")]
    public void TestFadeIn()
    {
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 0f;
            StartCoroutine(FadeIn());
        }
    }
    
    [ContextMenu("Test Fade Out")]
    public void TestFadeOut()
    {
        if (mainCanvasGroup != null)
        {
            StartCoroutine(FadeOut());
        }
    }
    
    [ContextMenu("Test Full Credits Sequence")]
    public void TestFullCreditsSequence()
    {
        if (creditsSequence != null)
        {
            StopCoroutine(creditsSequence);
        }
        
        // Reset state
        isSkipped = false;
        isTransitioning = false;
        creditsCompleted = false;
        
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 0f;
        }
        
        StartCreditsSequence();
    }
    
    [ContextMenu("Test Skip Credits")]
    public void TestSkipCredits()
    {
        SkipCredits();
    }
    
    [ContextMenu("Debug Credits State")]
    public void DebugCreditsState()
    {
        Debug.Log("=== CreditsController Debug State ===");
        Debug.Log($"Is Skipped: {isSkipped}");
        Debug.Log($"Is Transitioning: {isTransitioning}");
        Debug.Log($"Credits Completed: {creditsCompleted}");
        Debug.Log($"Effective Display Time: {effectiveDisplayTime}s");
        Debug.Log($"Allow Skip: {allowSkip}");
        Debug.Log($"Use SceneFlowManager Timing: {useSceneFlowManagerTiming}");
        Debug.Log($"Use SceneFlowManager Transitions: {useSceneFlowManagerTransitions}");
        Debug.Log($"SceneFlowManager Available: {(SceneFlowManager.Instance != null ? "Yes" : "No")}");
        if (mainCanvasGroup != null)
        {
            Debug.Log($"Canvas Group Alpha: {mainCanvasGroup.alpha}");
        }
        if (audioSource != null)
        {
            Debug.Log($"Audio Playing: {audioSource.isPlaying}");
        }
    }
    
    void OnValidate()
    {
        // Clamp values in inspector
        fadeInDuration = Mathf.Max(0.1f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0.1f, fadeOutDuration);
        creditsDisplayTime = Mathf.Max(0.5f, creditsDisplayTime);
        initialDelay = Mathf.Max(0f, initialDelay);
        musicVolume = Mathf.Clamp01(musicVolume);
    }
}