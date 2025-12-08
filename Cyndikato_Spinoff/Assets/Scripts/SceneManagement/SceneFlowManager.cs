using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Central manager for handling all scene transitions with smooth loading
/// Singleton pattern for global access
/// </summary>
public class SceneFlowManager : MonoBehaviour
{
    // Singleton instance
    private static SceneFlowManager _instance;
    public static SceneFlowManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SceneFlowManager");
                _instance = go.AddComponent<SceneFlowManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Scene Names")]
    public string introSceneName = "introScene";
    public string loadingSceneName = "LoadingScene";
    public string mainMenuSceneName = "Menu";
    public string characterSelectSceneName = "CharacterSelect";

    [Header("Loading Settings")]
    public float minimumLoadingTime = 2f; // Minimum time to show loading screen
    
    // Current loading state
    private bool isLoading = false;
    private string targetSceneName = "";
    private AsyncOperation currentLoadOperation;

    void Awake()
    {
        // Ensure singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("SceneFlowManager initialized");
    }

    /// <summary>
    /// Load a scene with the loading screen
    /// </summary>
    public void LoadSceneWithLoading(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning($"Already loading a scene. Cannot load {sceneName}");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is null or empty!");
            return;
        }

        targetSceneName = sceneName;
        StartCoroutine(LoadSceneSequence());
    }

    /// <summary>
    /// Load a scene directly without loading screen (for quick transitions)
    /// </summary>
    public void LoadSceneDirect(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is null or empty!");
            return;
        }

        try
        {
            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene {sceneName}: {e.Message}");
        }
    }

    /// <summary>
    /// Get current loading progress (0 to 1)
    /// </summary>
    public float GetLoadingProgress()
    {
        if (currentLoadOperation != null)
        {
            // Unity's progress goes from 0 to 0.9, then jumps to 1 when ready
            // We normalize this to 0-1 for better UX
            return Mathf.Clamp01(currentLoadOperation.progress / 0.9f);
        }
        return 0f;
    }

    /// <summary>
    /// Check if currently loading
    /// </summary>
    public bool IsLoading()
    {
        return isLoading;
    }

    /// <summary>
    /// Get the target scene being loaded
    /// </summary>
    public string GetTargetSceneName()
    {
        return targetSceneName;
    }

    private IEnumerator LoadSceneSequence()
    {
        isLoading = true;
        float startTime = Time.time;

        Debug.Log($"Starting scene transition to {targetSceneName}");

        // First, load the loading scene
        yield return SceneManager.LoadSceneAsync(loadingSceneName);

        // Give LoadingScreenController a frame to initialize
        yield return null;

        // Start loading the target scene in the background
        currentLoadOperation = SceneManager.LoadSceneAsync(targetSceneName);
        currentLoadOperation.allowSceneActivation = false; // Don't activate immediately

        // Wait for the scene to load
        while (currentLoadOperation.progress < 0.9f)
        {
            yield return null;
        }

        // Ensure minimum loading time for better UX
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
        }

        // Activate the loaded scene
        currentLoadOperation.allowSceneActivation = true;

        // Wait for scene activation
        while (!currentLoadOperation.isDone)
        {
            yield return null;
        }

        Debug.Log($"Scene transition to {targetSceneName} complete");

        // Clean up
        currentLoadOperation = null;
        targetSceneName = "";
        isLoading = false;
    }

    /// <summary>
    /// Load the intro scene (typically called at game start)
    /// </summary>
    public void LoadIntroScene()
    {
        LoadSceneDirect(introSceneName);
    }

    /// <summary>
    /// Load the main menu
    /// </summary>
    public void LoadMainMenu()
    {
        LoadSceneWithLoading(mainMenuSceneName);
    }

    /// <summary>
    /// Load character select
    /// </summary>
    public void LoadCharacterSelect()
    {
        LoadSceneWithLoading(characterSelectSceneName);
    }

    /// <summary>
    /// Quit the game
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
