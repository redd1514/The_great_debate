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
                // Fade in gradient from bottom to top with blinking effect
                float elapsed = 0f;
                while (elapsed < blinkSpeed)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / blinkSpeed;
                    
                    // Create blinking effect with alpha animation
                    float alpha = Mathf.Lerp(0.4f, 0.9f, Mathf.PingPong(t * 2, 1));
                    
                    // Apply color with animated alpha
                    Color blinkColor = playerColor;
                    blinkColor.a = alpha;
                    
                    // Update the overlay color to create the blink effect
                    // Note: For true gradient effect, use a UI shader with gradient properties
                    // or a shader that supports vertical color gradients
                    gradientOverlay.color = blinkColor;
                    
                    yield return null;
                }
                
                // If only one player, pause briefly before repeating
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
