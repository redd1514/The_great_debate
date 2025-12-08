using UnityEngine;

/// <summary>
/// MovingBackgroundManager coordinates cloud and castle animations
/// for a smooth, dynamic scene effect.
/// </summary>
public class MovingBackgroundManager : MonoBehaviour
{
    [SerializeField] private CloudParallax clouds;
    [SerializeField] private FloatingCastleParallax castle;
    
    private bool isActive = true;

    private void Start()
    {
        if (clouds != null)
            clouds.SetMovingSpeed(0.3f);

        if (castle != null)
        {
            castle.SetHorizontalMovement(1.5f, 0.8f);
            castle.SetVerticalMovement(1.2f, 0.6f);
        }
    }

    /// <summary>
    /// Play the parallax animation
    /// </summary>
    public void PlayAnimation()
    {
        isActive = true;
        if (clouds != null) clouds.SetMoving(true);
        if (castle != null) castle.SetMoving(true);
    }

    /// <summary>
    /// Pause the parallax animation
    /// </summary>
    public void PauseAnimation()
    {
        isActive = false;
        if (clouds != null) clouds.SetMoving(false);
        if (castle != null) castle.SetMoving(false);
    }

    /// <summary>
    /// Stop and reset the animation
    /// </summary>
    public void StopAnimation()
    {
        PauseAnimation();
        if (clouds != null) clouds.ResetPosition();
        if (castle != null) castle.ResetPosition();
    }

    /// <summary>
    /// Check if animation is playing
    /// </summary>
    public bool IsAnimationPlaying()
    {
        return isActive;
    }
}
