using UnityEngine;
using System.Collections;
using TMPro; // Require TextMeshPro package (remove conditional to show field in Inspector)

/// <summary>
/// Shows a start countdown and enables gameplay when finished.
/// Place on a GameObject in the scene (e.g., UI Canvas). Assign optional TextMeshProUGUI.
/// </summary>
public class GameStartCountdown : MonoBehaviour
{
    [Header("Countdown Settings")] [Tooltip("Seconds to count before GO.")] public int countdownSeconds = 3;
    [Tooltip("Time 'GO!' stays visible before clearing.")] public float goDisplayTime = 1f;
    [Tooltip("Automatically start countdown on enable.")] public bool autoStart = true;

    [Header("Animation")]
    [Tooltip("Enable simple fade/scale per step.")] public bool useFade = true;
    [Tooltip("Fade duration per step.")] [Range(0.1f,2f)] public float fadeDuration = 0.5f;
    [Tooltip("Text scale multiplier when appearing.")] public float scaleMultiplier = 1.15f;
    [Tooltip("Apply brief screen flash when GO! shows.")] public bool flashOnGo = true;

    [Header("References")] public TextMeshProUGUI countdownText;
    private bool running;
    private Vector3 originalScale;

    void OnEnable()
    {
        if (autoStart)
        {
            BeginCountdown();
        }
    }

    public void BeginCountdown()
    {
        if (running) return;
        running = true;
        PlayerController.globalGameplayEnabled = false; // Lock gameplay
        StartCoroutine(RunCountdown());
    }

    IEnumerator RunCountdown()
    {
        if (countdownText == null)
        {
            countdownText = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (countdownText == null)
        {
            Debug.LogWarning("GameStartCountdown: No TextMeshProUGUI assigned or found. Countdown will proceed without visual text.");
        }

        originalScale = countdownText != null ? countdownText.rectTransform.localScale : Vector3.one;

        for (int i = countdownSeconds; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                if (useFade) yield return StartCoroutine(FadeStep()); else yield return new WaitForSeconds(1f);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            if (useFade) yield return StartCoroutine(FadeStep()); else yield return new WaitForSeconds(goDisplayTime);
        }
        PlayerController.globalGameplayEnabled = true; // Unlock gameplay
        if (countdownText != null)
        {
            countdownText.text = string.Empty;
            countdownText.rectTransform.localScale = originalScale;
            countdownText.alpha = 1f;
        }
        running = false;
    }

    private IEnumerator FadeStep()
    {
        if (countdownText == null) yield break;
        var rt = countdownText.rectTransform;
        originalScale = rt.localScale;

        // Prep
        countdownText.alpha = 0f;
        rt.localScale = originalScale * scaleMultiplier;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeDuration);
            // Simple ease for alpha and scale back to 1
            countdownText.alpha = p;
            float s = Mathf.Lerp(scaleMultiplier, 1f, p);
            rt.localScale = originalScale * s;
            yield return null;
        }
        countdownText.alpha = 1f;

        // Screen flash for GO!
        if (flashOnGo && countdownText.text == "GO!")
        {
            StartCoroutine(FlashRoutine());
        }

        float hold = countdownText.text == "GO!" ? goDisplayTime : Mathf.Max(0f, 1f - fadeDuration);
        if (hold > 0f) yield return new WaitForSeconds(hold);
    }

    private IEnumerator FlashRoutine()
    {
        // Simple white flash overlay (temporary GameObject) if desired
        var cam = Camera.main;
        if (cam == null) yield break;
        var flash = new GameObject("CountdownFlash");
        var canvas = flash.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var imgGO = new GameObject("FlashImage");
        imgGO.transform.SetParent(flash.transform);
        var img = imgGO.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(1f,1f,1f,0f);
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        float t = 0f; float flashDur = 0.3f;
        while (t < flashDur)
        {
            t += Time.deltaTime;
            float p = t/flashDur;
            // Quick in then fade out
            float a = (p < 0.2f)? p/0.2f : 1f - ((p-0.2f)/0.8f);
            img.color = new Color(1f,1f,1f, Mathf.Clamp01(a) * 0.6f);
            yield return null;
        }
        Destroy(flash);
    }
}
