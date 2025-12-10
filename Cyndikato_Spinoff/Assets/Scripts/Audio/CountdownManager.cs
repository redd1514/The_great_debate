using UnityEngine;
using System.Collections;

public class CountdownManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip countdownClip; // Your complete 3-2-1-GO audio clip
    
    [Header("Timing Settings")]
    [Tooltip("Number of countdown seconds (should match GameStartCountdown)")]
    public int countdownSeconds = 3;
    [Tooltip("Optional delay before starting countdown")]
    public float initialDelay = 0f;
    
    private AudioSource audioSource;
    private GameStartCountdown gameStartCountdown;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Try to find GameStartCountdown component to sync with it
        gameStartCountdown = FindObjectOfType<GameStartCountdown>();
        
        if (gameStartCountdown != null)
        {
            // Sync our countdown settings with the UI countdown
            countdownSeconds = gameStartCountdown.countdownSeconds;
            Debug.Log($"CountdownManager: Synced with GameStartCountdown - {countdownSeconds} seconds");
        }
        
        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        // Wait for initial delay if specified
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }
        
        // Play the complete countdown audio clip once at the start
        if (countdownClip != null)
        {
            audioSource.PlayOneShot(countdownClip);
            Debug.Log("CountdownManager: Playing complete countdown audio (3-2-1-GO)");
        }
    }
    
    // Public method to restart countdown (useful for testing or game restart)
    public void RestartCountdown()
    {
        StopAllCoroutines();
        StartCoroutine(StartCountdown());
    }
}
