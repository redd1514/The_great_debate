using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroSceneController : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer; // Video player component
    public RenderTexture videoRenderTexture; // Optional: for UI display
    public RawImage videoDisplay; // UI element to display video
    
    [Header("Intro Settings")]
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Space;
    
    [Header("Fallback Settings (if no video)")]
    public bool useFallbackImageIntro = true;
    public Image logoImage; // Fallback logo/company image
    public float fallbackDuration = 3f;

    [Header("UI References")]
    public CanvasGroup fadeCanvasGroup; // For fade in/out effects
    public TextMeshProUGUI skipText; // Optional: "Press SPACE to skip" text

    [Header("Animation Settings")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1f;

    private bool introCompleted = false;
    private bool skipPressed = false;
    private bool isVideoIntro = false;
    private float videoDuration = 0f;

    void Start()
    {
        StartCoroutine(InitializeIntroSequence());
    }

    void Update()
    {
        // Check for skip input
        if (allowSkip && !introCompleted && Input.GetKeyDown(skipKey))
        {
            skipPressed = true;
        }

        // Alternative skip methods (any key or controller input)
        if (allowSkip && !introCompleted && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            skipPressed = true;
        }
    }

    IEnumerator InitializeIntroSequence()
    {
        // Initialize fade state
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f; // Start with black screen
        }

        // Show skip text
        if (skipText != null)
        {
            skipText.gameObject.SetActive(allowSkip);
        }

        // Check if we have a video to play
        if (videoPlayer != null && videoPlayer.clip != null)
        {
            yield return StartCoroutine(SetupVideoIntro());
        }
        else
        {
            yield return StartCoroutine(SetupFallbackIntro());
        }

        // Start the appropriate intro sequence
        if (isVideoIntro)
        {
            yield return StartCoroutine(PlayVideoIntroSequence());
        }
        else
        {
            yield return StartCoroutine(PlayFallbackIntroSequence());
        }

        // Mark as completed and transition
        introCompleted = true;
        TransitionToCredits();
    }

    IEnumerator SetupVideoIntro()
    {
        isVideoIntro = true;
        
        // Prepare the video player
        videoPlayer.Prepare();
        
        // Wait for video to be prepared
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        
        // Get the video duration automatically
        videoDuration = (float)videoPlayer.length;
        Debug.Log($"IntroSceneController: Video duration detected: {videoDuration} seconds");
        
        // Setup render texture if needed
        if (videoRenderTexture != null)
        {
            videoPlayer.targetTexture = videoRenderTexture;
            
            if (videoDisplay != null)
            {
                videoDisplay.texture = videoRenderTexture;
                videoDisplay.gameObject.SetActive(true);
            }
        }
        
        // Hide fallback logo if video is being used
        if (logoImage != null)
        {
            logoImage.gameObject.SetActive(false);
        }
    }

    IEnumerator SetupFallbackIntro()
    {
        isVideoIntro = false;
        
        Debug.Log("IntroSceneController: No video found, using fallback image intro");
        
        // Hide video display if it exists
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(false);
        }
        
        // Show fallback logo
        if (logoImage != null)
        {
            logoImage.gameObject.SetActive(true);
        }
        
        yield return null;
    }

    IEnumerator PlayVideoIntroSequence()
    {
        Debug.Log("IntroSceneController: Starting video intro sequence");
        
        // Fade in
        yield return StartCoroutine(FadeIn());
        
        // Start playing the video
        videoPlayer.Play();
        
        // Wait for video to complete or skip
        float timer = 0f;
        while (timer < videoDuration && !skipPressed && videoPlayer.isPlaying)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Stop video if it's still playing
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // Fade out
        yield return StartCoroutine(FadeOut());
    }

    IEnumerator PlayFallbackIntroSequence()
    {
        Debug.Log("IntroSceneController: Starting fallback image intro sequence");
        
        // Fade in logo
        yield return StartCoroutine(FadeIn());

        // Hold logo (or wait for skip)
        float holdTimer = 0f;
        while (holdTimer < fallbackDuration && !skipPressed)
        {
            holdTimer += Time.deltaTime;
            yield return null;
        }

        // Fade out
        yield return StartCoroutine(FadeOut());
    }

    IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        float timer = 0f;
        while (timer < fadeInDuration && !skipPressed)
        {
            timer += Time.deltaTime;
            float alpha = 1f - (timer / fadeInDuration);
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }

        if (!skipPressed)
        {
            fadeCanvasGroup.alpha = 0f;
        }
    }

    IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        float timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float alpha = timer / fadeOutDuration;
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    void TransitionToCredits()
    {
        Debug.Log("IntroSceneController: Intro completed, transitioning to Credits scene");
        
        // Use SceneFlowManager to load credits
        if (SceneFlowManager.Instance != null)
        {
            Debug.Log("IntroSceneController: SceneFlowManager found, loading Credits scene");
            SceneFlowManager.Instance.LoadCreditsFromIntro();
        }
        else
        {
            Debug.LogError("IntroSceneController: SceneFlowManager instance is NULL! Creating fallback.");
            
            // Try to find SceneFlowManager in the scene
            SceneFlowManager foundManager = FindFirstObjectByType<SceneFlowManager>();
            if (foundManager != null)
            {
                Debug.Log("IntroSceneController: Found SceneFlowManager in scene, using it");
                foundManager.LoadCreditsFromIntro();
            }
            else
            {
                Debug.LogWarning("IntroSceneController: No SceneFlowManager found. Loading Credits scene directly.");
                // Load credits scene directly as fallback
                UnityEngine.SceneManagement.SceneManager.LoadScene("Credits");
            }
        }
    }

    // Public method to manually set video clip
    public void SetVideoClip(VideoClip clip)
    {
        if (videoPlayer != null)
        {
            videoPlayer.clip = clip;
        }
    }
    
    // Debug method to check video info
    [ContextMenu("Debug Video Info")]
    public void DebugVideoInfo()
    {
        if (videoPlayer != null && videoPlayer.clip != null)
        {
            Debug.Log($"Video Clip: {videoPlayer.clip.name}");
            Debug.Log($"Video Length: {videoPlayer.clip.length} seconds");
            Debug.Log($"Video Frame Rate: {videoPlayer.clip.frameRate}");
            Debug.Log($"Video Resolution: {videoPlayer.clip.width}x{videoPlayer.clip.height}");
        }
        else
        {
            Debug.Log("No video player or video clip assigned");
        }
    }
    
    // Public method for manual testing or external calls
    public void SkipIntro()
    {
        if (!introCompleted)
        {
            skipPressed = true;
        }
    }
    
    // Debug method to test the transition flow
    [ContextMenu("Test Transition To Credits")]
    public void TestTransitionToCredits()
    {
        TransitionToCredits();
    }
}