using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Controls the intro scene with logo display and auto-transition to main menu
/// </summary>
public class IntroSceneController : MonoBehaviour
{
    [Header("Intro Settings")]
    public float introDuration = 3f; // Duration to show intro
    public bool allowSkip = true; // Allow players to skip intro
    
    [Header("UI References (Optional)")]
    public Image logoImage; // Optional logo image to fade in/out
    public CanvasGroup canvasGroup; // Optional for fade effects

    [Header("Fade Settings")]
    public bool useFadeIn = true;
    public bool useFadeOut = true;
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1f;

    private bool isSkipped = false;
    private float timer = 0f;

    void Start()
    {
        Debug.Log("Intro scene started");
        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        // Check for skip input
        if (allowSkip && !isSkipped)
        {
            if (GetSkipInput())
            {
                Debug.Log("Intro skipped by player");
                isSkipped = true;
            }
        }
    }

    private IEnumerator IntroSequence()
    {
        // Fade in if enabled
        if (useFadeIn && canvasGroup != null)
        {
            yield return StartCoroutine(FadeIn());
        }

        // Wait for intro duration or until skipped
        timer = 0f;
        while (timer < introDuration && !isSkipped)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Fade out if enabled
        if (useFadeOut && canvasGroup != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        // Transition to main menu
        TransitionToMainMenu();
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 1f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    private void TransitionToMainMenu()
    {
        Debug.Log("Transitioning from intro to main menu");
        
        // Use SceneFlowManager for smooth transition
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadMainMenu();
        }
        else
        {
            // Fallback if SceneFlowManager not available
            Debug.LogWarning("SceneFlowManager not found, using direct scene load");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
    }

    private bool GetSkipInput()
    {
        // Keyboard input
        if (Input.GetKeyDown(KeyCode.Space) || 
            Input.GetKeyDown(KeyCode.Return) || 
            Input.GetKeyDown(KeyCode.Escape))
        {
            return true;
        }

        // Controller input - using hardcoded button strings for consistency with existing codebase
        // Note: This matches the input style used in Mainmenu.cs
        // For better cross-platform support, consider migrating to Unity's Input System
        string joystickName = "joystick 1";
        
        // X button (PS4/Xbox) - button 0 is typically the south button
        if (Input.GetKeyDown($"{joystickName} button 0") || 
            Input.GetKeyDown($"{joystickName} button 1"))
        {
            return true;
        }

        // Start button
        if (Input.GetKeyDown($"{joystickName} button 7") || 
            Input.GetKeyDown($"{joystickName} button 9"))
        {
            return true;
        }

        // Mouse click
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Public method to manually skip intro (can be called from button)
    /// </summary>
    public void SkipIntro()
    {
        if (!isSkipped)
        {
            Debug.Log("Intro manually skipped");
            isSkipped = true;
        }
    }
}
