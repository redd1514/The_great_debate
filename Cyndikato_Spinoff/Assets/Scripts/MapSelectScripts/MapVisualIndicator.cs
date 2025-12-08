using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MapVisualIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Image gradientOverlay;
    [SerializeField] private float blinkSpeed = 0.5f;
    [SerializeField] private float gradientHeight = 1.0f;
    
    private List<Color> activePlayerColors = new List<Color>();
    private Coroutine blinkCoroutine;
    private Material gradientMaterial;
    
    void Awake()
    {
        // Create a unique material instance for this indicator
        if (gradientOverlay != null)
        {
            gradientMaterial = new Material(gradientOverlay.material);
            gradientOverlay.material = gradientMaterial;
        }
    }
    
    public void AddPlayerVote(Color playerColor)
    {
        if (!activePlayerColors.Contains(playerColor))
        {
            activePlayerColors.Add(playerColor);
            UpdateVisual();
        }
    }
    
    public void RemovePlayerVote(Color playerColor)
    {
        if (activePlayerColors.Contains(playerColor))
        {
            activePlayerColors.Remove(playerColor);
            UpdateVisual();
        }
    }
    
    public void ClearAllVotes()
    {
        activePlayerColors.Clear();
        UpdateVisual();
    }
    
    public int GetVoteCount()
    {
        return activePlayerColors.Count;
    }
    
    private void UpdateVisual()
    {
        if (gradientOverlay == null) return;
        
        if (activePlayerColors.Count > 0)
        {
            gradientOverlay.gameObject.SetActive(true);
            
            // Stop previous blink coroutine if running
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }
            
            // Start blinking animation with player colors
            blinkCoroutine = StartCoroutine(BlinkGradient());
        }
        else
        {
            // No votes, hide the overlay
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            gradientOverlay.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator BlinkGradient()
    {
        while (true)
        {
            // Cycle through player colors if multiple players voted
            foreach (Color playerColor in activePlayerColors)
            {
                // Fade in gradient from bottom to top
                float elapsed = 0f;
                while (elapsed < blinkSpeed)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / blinkSpeed;
                    
                    // Set gradient color with alpha animation
                    Color topColor = playerColor;
                    topColor.a = Mathf.Lerp(0.3f, 0.7f, Mathf.PingPong(t * 2, 1));
                    
                    Color bottomColor = playerColor;
                    bottomColor.a = Mathf.Lerp(0.6f, 1.0f, Mathf.PingPong(t * 2, 1));
                    
                    // Update gradient using vertical gradient
                    if (gradientMaterial != null)
                    {
                        gradientOverlay.color = bottomColor;
                    }
                    
                    yield return null;
                }
                
                // If only one player, stay with their color
                if (activePlayerColors.Count == 1)
                {
                    yield return new WaitForSeconds(blinkSpeed * 0.5f);
                }
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up the material instance
        if (gradientMaterial != null)
        {
            Destroy(gradientMaterial);
        }
    }
}
