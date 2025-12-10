using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    [Header("Scene Names")]
    public string introSceneName = "IntroScene";
    public string creditsSceneName = "Credits";
    public string loadingSceneName = "LoadingScene";
    public string mainMenuSceneName = "Menu";
    public string characterSelectSceneName = "CharacterSelect";

    [Header("Loading Settings")]
    public float minimumLoadingTime = 2f;
    public bool useAsyncLoading = true;

    [Header("Credits Settings")]
    [Tooltip("Time to display credits before transitioning to menu")]
    public float creditsDisplayTime = 5f;
    [Tooltip("Auto-transition from credits to menu")]
    public bool autoTransitionFromCredits = true;

    private string targetScene;
    private bool isLoading = false;
    
    // Static backup to preserve target scene across scene transitions
    private static string staticTargetScene;

    void Awake()
    {
        // Singleton pattern with enhanced safety
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("SceneFlowManager: Instance created and marked as DontDestroyOnLoad");
            
            // Restore target scene if it was preserved
            if (!string.IsNullOrEmpty(staticTargetScene))
            {
                targetScene = staticTargetScene;
                Debug.Log($"SceneFlowManager: Restored target scene: {targetScene}");
            }
        }
        else
        {
            Debug.LogWarning("SceneFlowManager: Duplicate instance detected, destroying duplicate");
            Destroy(gameObject);
            return;
        }
        
        // Subscribe to scene loaded events to ensure persistence
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-establish instance reference after scene transition
        if (Instance == null)
        {
            Instance = this;
        }
        
        // Handle credits scene logic
        if (scene.name == creditsSceneName)
        {
            Debug.Log("SceneFlowManager: Credits scene loaded");
            
            // Look for CreditsController to coordinate timing
            CreditsController creditsController = FindFirstObjectByType<CreditsController>();
            if (creditsController != null)
            {
                Debug.Log("SceneFlowManager: Found CreditsController, coordinating timing and transitions");
                
                // If the CreditsController is using our timing, make sure it knows our settings
                if (creditsController.useSceneFlowManagerTiming)
                {
                    creditsController.SetDisplayTime(creditsDisplayTime);
                }
                
                // Start our credits flow coroutine only if auto-transition is enabled
                if (autoTransitionFromCredits)
                {
                    StartCoroutine(HandleCreditsFlowWithController(creditsController));
                }
            }
            else
            {
                Debug.LogWarning("SceneFlowManager: No CreditsController found in credits scene");
                // Fallback to original logic if no CreditsController
                if (autoTransitionFromCredits)
                {
                    StartCoroutine(HandleCreditsFlow());
                }
            }
        }
        
        // If this is the loading scene, ensure target scene is preserved
        if (scene.name == loadingSceneName)
        {
            Debug.Log($"SceneFlowManager: Loading scene loaded - Checking target preservation");
            Debug.Log($"SceneFlowManager: Instance target: '{targetScene}', Static target: '{staticTargetScene}'");
            
            // Restore from static backup if needed
            if (string.IsNullOrEmpty(targetScene) && !string.IsNullOrEmpty(staticTargetScene))
            {
                targetScene = staticTargetScene;
                Debug.Log($"SceneFlowManager: Restored target scene in OnSceneLoaded: {targetScene}");
            }
        }
    }

    private IEnumerator HandleCreditsFlow()
    {
        Debug.Log($"SceneFlowManager: Starting credits flow - Will transition to menu via loading scene in {creditsDisplayTime} seconds");
        
        // Wait for credits display time
        yield return new WaitForSeconds(creditsDisplayTime);
        
        // Transition to main menu via loading screen
        LoadSceneWithLoading(mainMenuSceneName);
    }

    /// <summary>
    /// Handle credits flow when CreditsController is present for better coordination
    /// </summary>
    /// <param name="creditsController">The credits controller to coordinate with</param>
    private IEnumerator HandleCreditsFlowWithController(CreditsController creditsController)
    {
        Debug.Log($"SceneFlowManager: Starting coordinated credits flow with CreditsController");
        
        // Wait for the CreditsController to complete its sequence
        // This includes fade in time + display time + any user interactions
        while (!creditsController.IsCreditsCompleted && !creditsController.IsTransitioning)
        {
            yield return null;
        }
        
        Debug.Log("SceneFlowManager: CreditsController sequence completed, transitioning to menu");
        
        // Give a brief moment for any final fade out
        yield return new WaitForSeconds(0.5f);
        
        // Transition to main menu via loading screen
        LoadSceneWithLoading(mainMenuSceneName);
    }

    /// <summary>
    /// Start the intro sequence (IntroScene → Credits → LoadingScene → Menu)
    /// </summary>
    public void StartIntroSequence()
    {
        Debug.Log("SceneFlowManager: Starting intro sequence");
        LoadSceneDirectly(introSceneName);
    }

    /// <summary>
    /// Transition from intro to credits - CRITICAL FIX: Clear backup data first to prevent interference
    /// </summary>
    public void LoadCreditsFromIntro()
    {
        Debug.Log("SceneFlowManager: Transitioning from intro to credits");
        Debug.Log($"SceneFlowManager: Credits scene name set to: '{creditsSceneName}'");
        
        // CRITICAL FIX: Clear any existing target scene data before loading credits
        // This prevents the LoadingScreenController from thinking it should load a different scene
        ClearBackupData();
        
        // Reset loading state
        isLoading = false;
        
        // Load credits scene directly - no loading screen needed for this transition
        Debug.Log($"SceneFlowManager: Loading credits scene directly: '{creditsSceneName}'");
        if (string.IsNullOrEmpty(creditsSceneName))
        {
            Debug.LogError("SceneFlowManager: creditsSceneName is not set! Please set it in the inspector.");
            Debug.LogError("SceneFlowManager: Falling back to 'Credits' as scene name");
            SceneManager.LoadScene("Credits");
        }
        else
        {
            SceneManager.LoadScene(creditsSceneName);
        }
    }

    /// <summary>
    /// Skip credits and go directly to main menu
    /// </summary>
    public void SkipCredits()
    {
        Debug.Log("SceneFlowManager: Skipping credits, going to main menu");
        StopAllCoroutines(); // Stop any auto-transition
        LoadSceneWithLoading(mainMenuSceneName);
    }

    public void LoadSceneWithLoading(string sceneName)
    {
        if (isLoading) 
        {
            Debug.LogWarning($"SceneFlowManager: Already loading a scene, ignoring request to load {sceneName}");
            return;
        }

        // Validate scene name
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneFlowManager: Scene name is null or empty!");
            return;
        }

        // Set both instance and static target scene for backup
        targetScene = sceneName;
        staticTargetScene = sceneName;
        isLoading = true;
        
        Debug.Log($"SceneFlowManager: Starting scene transition - Target: '{targetScene}', Loading Scene: '{loadingSceneName}', Static Backup: '{staticTargetScene}'");

        // Ensure we have a static reference to this instance before loading
        if (Instance == null) Instance = this;

        // Store target in PlayerPrefs as an additional safety net
        PlayerPrefs.SetString("SceneFlowTargetScene", sceneName);
        PlayerPrefs.Save();

        // Load the loading screen first
        SceneManager.LoadScene(loadingSceneName);
    }

    public void LoadTargetScene()
    {
        Debug.Log($"SceneFlowManager: LoadTargetScene called - Instance target: '{targetScene}', Static target: '{staticTargetScene}'");
        
        // Try to restore target scene from static backup if instance target is empty
        if (string.IsNullOrEmpty(targetScene) && !string.IsNullOrEmpty(staticTargetScene))
        {
            targetScene = staticTargetScene;
            Debug.Log($"SceneFlowManager: Restored target scene from static backup: {targetScene}");
        }
        
        // If still empty, try PlayerPrefs backup
        if (string.IsNullOrEmpty(targetScene))
        {
            string prefsTarget = PlayerPrefs.GetString("SceneFlowTargetScene", "");
            if (!string.IsNullOrEmpty(prefsTarget))
            {
                targetScene = prefsTarget;
                staticTargetScene = prefsTarget;
                Debug.Log($"SceneFlowManager: Restored target scene from PlayerPrefs backup: {targetScene}");
            }
        }
        
        if (string.IsNullOrEmpty(targetScene)) 
        {
            Debug.LogError("SceneFlowManager: No target scene set! Cannot load target scene. Falling back to Menu.");
            Debug.LogError($"SceneFlowManager: Debug - Instance target: '{targetScene}', Static target: '{staticTargetScene}'");
            // Emergency fallback to main menu
            targetScene = mainMenuSceneName;
            staticTargetScene = mainMenuSceneName;
        }

        Debug.Log($"SceneFlowManager: Loading target scene: {targetScene}");

        if (useAsyncLoading)
        {
            StartCoroutine(LoadSceneAsync(targetScene));
        }
        else
        {
            SceneManager.LoadScene(targetScene);
            isLoading = false;
            // Clear backup data after successful load
            ClearBackupData();
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure loading screen is visible

        Debug.Log($"SceneFlowManager: Starting async load of {sceneName}");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Give the async operation to LoadingScreenController for real progress tracking
        if (LoadingScreenController.Instance != null)
        {
            LoadingScreenController.Instance.SetAsyncOperation(asyncLoad, minimumLoadingTime);
        }

        float timer = 0f;

        while (timer < minimumLoadingTime || asyncLoad.progress < 0.9f)
        {
            timer += Time.deltaTime;

            // Calculate real progress
            float asyncProgress = asyncLoad.progress / 0.9f; // AsyncOperation progress caps at 0.9
            float timeProgress = timer / minimumLoadingTime;
            float progress = Mathf.Min(asyncProgress, timeProgress);

            // Update progress (this will be handled by LoadingScreenController now, but keep as backup)
            LoadingScreenController.Instance?.UpdateProgress(progress);

            yield return null;
        }

        // Final progress update
        LoadingScreenController.Instance?.UpdateProgress(1f);
        yield return new WaitForSeconds(0.5f); // Brief pause at 100%

        Debug.Log($"SceneFlowManager: Activating scene {sceneName}");
        asyncLoad.allowSceneActivation = true;
        isLoading = false;
        
        // Clear backup data after successful load
        ClearBackupData();
    }

    private void ClearBackupData()
    {
        staticTargetScene = null;
        PlayerPrefs.DeleteKey("SceneFlowTargetScene");
        PlayerPrefs.Save();
        Debug.Log("SceneFlowManager: Cleared backup data");
    }

    public void LoadSceneDirectly(string sceneName)
    {
        Debug.Log($"SceneFlowManager: Loading scene directly: {sceneName}");
        SceneManager.LoadScene(sceneName);
        // Clear backup data since we're not using the loading system
        ClearBackupData();
    }
    
    // Public method to check if target scene is set
    public bool HasTargetScene()
    {
        return !string.IsNullOrEmpty(targetScene) || !string.IsNullOrEmpty(staticTargetScene) || !string.IsNullOrEmpty(PlayerPrefs.GetString("SceneFlowTargetScene", ""));
    }
    
    // Public method to get current target scene
    public string GetTargetScene()
    {
        if (!string.IsNullOrEmpty(targetScene)) return targetScene;
        if (!string.IsNullOrEmpty(staticTargetScene)) return staticTargetScene;
        return PlayerPrefs.GetString("SceneFlowTargetScene", "");
    }
    
    // Static method to get target scene even if instance is null
    public static string GetStaticTargetScene()
    {
        if (!string.IsNullOrEmpty(staticTargetScene)) return staticTargetScene;
        return PlayerPrefs.GetString("SceneFlowTargetScene", "");
    }
    
    // Static method to set target scene even if instance is null
    public static void SetStaticTargetScene(string sceneName)
    {
        staticTargetScene = sceneName;
        PlayerPrefs.SetString("SceneFlowTargetScene", sceneName);
        PlayerPrefs.Save();
        Debug.Log($"SceneFlowManager: Static target scene set to: {staticTargetScene}");
    }
    
    // Debug method to check current state
    [ContextMenu("Debug Scene Flow State")]
    public void DebugState()
    {
        Debug.Log("=== SceneFlowManager Debug State ===");
        Debug.Log($"Instance: {(Instance != null ? "Valid" : "NULL")}");
        Debug.Log($"Target Scene: '{targetScene}'");
        Debug.Log($"Static Target Scene: '{staticTargetScene}'");
        Debug.Log($"PlayerPrefs Target Scene: '{PlayerPrefs.GetString("SceneFlowTargetScene", "NONE")}'");
        Debug.Log($"Is Loading: {isLoading}");
        Debug.Log($"Credits Scene Name: '{creditsSceneName}' (CHECK THIS!)");
        Debug.Log($"Loading Scene Name: '{loadingSceneName}'");
        Debug.Log($"Main Menu Scene Name: '{mainMenuSceneName}'");
        Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Has Target Scene: {HasTargetScene()}");
        Debug.Log($"Auto Transition From Credits: {autoTransitionFromCredits}");
        Debug.Log($"Credits Display Time: {creditsDisplayTime}s");
    }
}