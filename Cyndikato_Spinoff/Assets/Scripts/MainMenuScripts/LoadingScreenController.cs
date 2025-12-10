using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance { get; private set; }

    [Header("UI References")]
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI loadingStatusText;
    public Image backgroundImage; // Optional: for fade effects

    [Header("Loading Messages")]
    public string[] loadingMessages = {
        "Initializing...",
        "Loading Assets...",
        "Preparing Game World...",
        "Finishing Up..."
    };

    [Header("Animation Settings")]
    public float progressBarSpeed = 2f;
    public float messageChangeInterval = 1.5f;
    public bool useProgressBarAnimation = true;
    public float minimumLoadingTime = 2f; // Minimum time to show loading screen

    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private int currentMessageIndex = 0;
    private Coroutine messageCoroutine;
    private bool isLoading = true;
    private AsyncOperation currentAsyncOperation;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Debug.Log("LoadingScreenController: Starting loading screen");
        
        // Initialize progress bar
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        // Start loading messages
        StartLoadingMessages();

        // Add a small delay to ensure SceneFlowManager has time to initialize after scene transition
        StartCoroutine(InitializeLoadingSequence());
    }
    
    private IEnumerator InitializeLoadingSequence()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;
        
        Debug.Log("LoadingScreenController: Checking SceneFlowManager state...");
        Debug.Log($"LoadingScreenController: SceneFlowManager.Instance = {(SceneFlowManager.Instance != null ? "Valid" : "NULL")}");
        Debug.Log($"LoadingScreenController: Static target scene = '{SceneFlowManager.GetStaticTargetScene()}'");
        
        string targetScene = "";
        
        // Check SceneFlowManager instance first
        if (SceneFlowManager.Instance != null && SceneFlowManager.Instance.HasTargetScene())
        {
            Debug.Log("LoadingScreenController: SceneFlowManager instance found with target scene, loading target scene");
            targetScene = SceneFlowManager.Instance.GetTargetScene();
            SceneFlowManager.Instance.LoadTargetScene();
        }
        // Check static target scene data
        else if (!string.IsNullOrEmpty(SceneFlowManager.GetStaticTargetScene()))
        {
            targetScene = SceneFlowManager.GetStaticTargetScene();
            Debug.Log($"LoadingScreenController: SceneFlowManager instance not ready but static target found: {targetScene}");
            StartCoroutine(LoadStaticTargetSceneAsync());
        }
        // Check PlayerPrefs backup
        else
        {
            string prefsTarget = PlayerPrefs.GetString("SceneFlowTargetScene", "");
            if (!string.IsNullOrEmpty(prefsTarget))
            {
                targetScene = prefsTarget;
                Debug.Log($"LoadingScreenController: Found target scene in PlayerPrefs backup: {targetScene}");
                StartCoroutine(LoadPlayerPrefsTargetSceneAsync());
            }
            else
            {
                Debug.LogError("LoadingScreenController: No target scene found in any backup system! Using fallback loading.");
                StartCoroutine(FallbackLoadMainMenuAsync());
            }
        }
    }
    
    private IEnumerator LoadStaticTargetSceneAsync()
    {
        string targetScene = SceneFlowManager.GetStaticTargetScene();
        Debug.Log($"LoadingScreenController: Loading static target scene: {targetScene}");
        
        yield return StartCoroutine(LoadSceneAsyncWithProgressTracking(targetScene));
    }
    
    private IEnumerator LoadPlayerPrefsTargetSceneAsync()
    {
        string targetScene = PlayerPrefs.GetString("SceneFlowTargetScene", "");
        Debug.Log($"LoadingScreenController: Loading PlayerPrefs target scene: {targetScene}");
        
        yield return StartCoroutine(LoadSceneAsyncWithProgressTracking(targetScene));
    }
    
    private IEnumerator LoadSceneAsyncWithProgressTracking(string sceneName)
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure loading screen is visible

        // Start async loading
        currentAsyncOperation = SceneManager.LoadSceneAsync(sceneName);
        currentAsyncOperation.allowSceneActivation = false;

        float startTime = Time.time;
        
        while (!currentAsyncOperation.isDone)
        {
            // Calculate real loading progress
            float asyncProgress = currentAsyncOperation.progress / 0.9f; // AsyncOperation progress caps at 0.9
            
            // Calculate time-based progress for minimum loading time
            float timeProgress = (Time.time - startTime) / minimumLoadingTime;
            
            // Use the slower of the two to ensure minimum loading time
            float progress = Mathf.Min(asyncProgress, timeProgress);
            
            // Update progress
            UpdateProgress(progress);
            
            Debug.Log($"LoadingScreenController: Loading progress - Async: {asyncProgress:F2}, Time: {timeProgress:F2}, Final: {progress:F2}");
            
            // If both async loading and minimum time are complete
            if (asyncProgress >= 0.99f && timeProgress >= 1f)
            {
                UpdateProgress(1f);
                yield return new WaitForSeconds(0.5f); // Brief pause at 100%
                break;
            }
            
            yield return null;
        }
        
        Debug.Log($"LoadingScreenController: Activating scene: {sceneName}");
        currentAsyncOperation.allowSceneActivation = true;
        isLoading = false;
        
        // Clear backup data
        PlayerPrefs.DeleteKey("SceneFlowTargetScene");
        PlayerPrefs.Save();
    }
    
    private IEnumerator FallbackLoadMainMenuAsync()
    {
        Debug.Log("LoadingScreenController: Starting fallback async loading sequence");
        
        // Start async loading of Menu scene (not MainMenu)
        currentAsyncOperation = SceneManager.LoadSceneAsync("Menu");
        currentAsyncOperation.allowSceneActivation = false; // Prevent auto-activation
        
        float startTime = Time.time;
        float progress = 0f;
        
        while (!currentAsyncOperation.isDone)
        {
            // Calculate real loading progress
            float asyncProgress = currentAsyncOperation.progress / 0.9f; // AsyncOperation progress caps at 0.9
            
            // Calculate time-based progress for minimum loading time
            float timeProgress = (Time.time - startTime) / minimumLoadingTime;
            
            // Use the slower of the two to ensure minimum loading time
            progress = Mathf.Min(asyncProgress, timeProgress);
            
            // Update progress
            UpdateProgress(progress);
            
            Debug.Log($"LoadingScreenController: Fallback progress - Async: {asyncProgress:F2}, Time: {timeProgress:F2}, Final: {progress:F2}");
            
            // If both async loading and minimum time are complete
            if (asyncProgress >= 0.99f && timeProgress >= 1f)
            {
                UpdateProgress(1f);
                yield return new WaitForSeconds(0.5f); // Brief pause at 100%
                break;
            }
            
            yield return null;
        }
        
        Debug.Log("LoadingScreenController: Activating Menu scene");
        currentAsyncOperation.allowSceneActivation = true;
        isLoading = false;
    }

    void Update()
    {
        // Smooth progress bar animation
        if (useProgressBarAnimation && progressBar != null)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, progressBarSpeed * Time.deltaTime);
            progressBar.value = currentProgress;

            // Update percentage text
            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
            }
        }
    }

    public void UpdateProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);

        if (!useProgressBarAnimation && progressBar != null)
        {
            progressBar.value = targetProgress;
            currentProgress = targetProgress; // Update current progress for message cycling

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(targetProgress * 100)}%";
            }
        }
        else
        {
            // Even with animation, update currentProgress for message cycling
            // currentProgress will be smoothly animated in Update()
        }
    }

    void StartLoadingMessages()
    {
        if (loadingMessages.Length > 0)
        {
            messageCoroutine = StartCoroutine(CycleLoadingMessages());
        }
    }

    IEnumerator CycleLoadingMessages()
    {
        while (isLoading && currentProgress < 0.99f)
        {
            if (loadingStatusText != null && currentMessageIndex < loadingMessages.Length)
            {
                loadingStatusText.text = loadingMessages[currentMessageIndex];
                currentMessageIndex = (currentMessageIndex + 1) % loadingMessages.Length;
            }

            yield return new WaitForSeconds(messageChangeInterval);
        }

        // Final message
        if (loadingStatusText != null)
        {
            loadingStatusText.text = "Complete!";
        }
    }

    // Method for SceneFlowManager to provide real async operation
    public void SetAsyncOperation(AsyncOperation asyncOp, float minLoadTime = 2f)
    {
        currentAsyncOperation = asyncOp;
        minimumLoadingTime = minLoadTime;
        StartCoroutine(TrackRealAsyncProgress());
    }

    private IEnumerator TrackRealAsyncProgress()
    {
        if (currentAsyncOperation == null) yield break;
        
        Debug.Log("LoadingScreenController: Tracking real async loading progress");
        float startTime = Time.time;
        
        while (!currentAsyncOperation.isDone)
        {
            // Calculate real loading progress
            float asyncProgress = currentAsyncOperation.progress / 0.9f; // AsyncOperation progress caps at 0.9
            
            // Calculate time-based progress for minimum loading time
            float timeProgress = (Time.time - startTime) / minimumLoadingTime;
            
            // Use the slower of the two to ensure minimum loading time
            float progress = Mathf.Min(asyncProgress, timeProgress);
            
            // Update progress
            UpdateProgress(progress);
            
            // If both async loading and minimum time are complete
            if (asyncProgress >= 0.99f && timeProgress >= 1f)
            {
                UpdateProgress(1f);
                yield return new WaitForSeconds(0.5f); // Brief pause at 100%
                break;
            }
            
            yield return null;
        }
        
        isLoading = false;
        Debug.Log("LoadingScreenController: Real async loading complete");
    }

    void OnDestroy()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Debug method
    [ContextMenu("Debug Loading State")]
    public void DebugLoadingState()
    {
        Debug.Log("=== LoadingScreenController Debug State ===");
        Debug.Log($"Is Loading: {isLoading}");
        Debug.Log($"Current Progress: {currentProgress:F2}");
        Debug.Log($"Target Progress: {targetProgress:F2}");
        Debug.Log($"Current Message Index: {currentMessageIndex}");
        Debug.Log($"Async Operation: {(currentAsyncOperation != null ? "Valid" : "NULL")}");
        Debug.Log($"SceneFlowManager Instance: {(SceneFlowManager.Instance != null ? "Valid" : "NULL")}");
        Debug.Log($"Static Target Scene: '{SceneFlowManager.GetStaticTargetScene()}'");
        Debug.Log($"PlayerPrefs Target Scene: '{PlayerPrefs.GetString("SceneFlowTargetScene", "NONE")}'");
        if (currentAsyncOperation != null)
        {
            Debug.Log($"Async Progress: {currentAsyncOperation.progress:F2}");
            Debug.Log($"Async Done: {currentAsyncOperation.isDone}");
        }
    }
}