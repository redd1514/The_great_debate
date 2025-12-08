using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Enhanced loading screen with realistic progress bar and loading phases
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    [Header("UI References")]
    public Slider progressBar;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI percentageText;

    [Header("Loading Phases")]
    public string[] loadingPhases = new string[]
    {
        "Initializing...",
        "Loading Assets...",
        "Preparing Scene...",
        "Almost Ready..."
    };

    [Header("Animation Settings")]
    public float progressSmoothSpeed = 2f;
    public float phaseTransitionTime = 0.5f;

    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private int currentPhaseIndex = 0;

    void Start()
    {
        ValidateUIReferences();
        
        // Initialize UI
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        UpdateLoadingText();
        StartCoroutine(AnimateLoading());
    }
    
    private void ValidateUIReferences()
    {
        if (progressBar == null)
        {
            Debug.LogWarning("LoadingScreenController: Progress Bar reference is not assigned!");
        }
        if (loadingText == null)
        {
            Debug.LogWarning("LoadingScreenController: Loading Text reference is not assigned!");
        }
        if (percentageText == null)
        {
            Debug.LogWarning("LoadingScreenController: Percentage Text reference is not assigned!");
        }
    }

    void Update()
    {
        // Get actual loading progress from SceneFlowManager
        SceneFlowManager manager = SceneFlowManager.Instance;
        if (manager != null && manager.IsLoading())
        {
            float actualProgress = manager.GetLoadingProgress();
            targetProgress = actualProgress;
        }

        // Smoothly animate progress bar
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * progressSmoothSpeed);

        // Update UI
        if (progressBar != null)
        {
            progressBar.value = currentProgress;
        }

        if (percentageText != null)
        {
            percentageText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
        }

        // Update loading phase based on progress
        UpdateLoadingPhase();
    }

    private IEnumerator AnimateLoading()
    {
        // Simulate initial loading progress for better UX
        // This gives immediate feedback while actual loading starts
        targetProgress = 0.1f;
        yield return new WaitForSeconds(0.2f);

        targetProgress = 0.2f;
        yield return new WaitForSeconds(0.3f);

        // After initial simulation, actual loading progress takes over in Update()
    }

    private void UpdateLoadingPhase()
    {
        // Determine which phase we should be in based on progress
        int newPhaseIndex = Mathf.FloorToInt(currentProgress * loadingPhases.Length);
        newPhaseIndex = Mathf.Clamp(newPhaseIndex, 0, loadingPhases.Length - 1);

        // If phase changed, update the text
        if (newPhaseIndex != currentPhaseIndex)
        {
            currentPhaseIndex = newPhaseIndex;
            UpdateLoadingText();
        }
    }

    private void UpdateLoadingText()
    {
        if (loadingText != null && currentPhaseIndex < loadingPhases.Length)
        {
            loadingText.text = loadingPhases[currentPhaseIndex];
        }
    }

    /// <summary>
    /// Set custom loading text (optional, for specific loading scenarios)
    /// </summary>
    public void SetCustomLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }

    /// <summary>
    /// Manually set progress (for testing or special cases)
    /// </summary>
    public void SetProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
    }
}
