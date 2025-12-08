using UnityEngine;
using UnityEngine.UI;

public class CharacterSpriteAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public Image targetImage;
    public bool playOnStart = false;
    public bool isPlaying = false;
    
    [Header("Current Animation")]
    public Sprite[] animationFrames;
    public float frameTime = 0.1f;
    public bool loop = true;
    
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    private bool animationComplete = false;
    
    void Start()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
            
        if (playOnStart && animationFrames != null && animationFrames.Length > 0)
        {
            PlayAnimation();
        }
    }
    
    void Update()
    {
        if (isPlaying && animationFrames != null && animationFrames.Length > 0)
        {
            UpdateAnimation();
        }
    }
    
    void UpdateAnimation()
    {
        frameTimer += Time.deltaTime;
        
        if (frameTimer >= frameTime)
        {
            frameTimer = 0f;
            currentFrameIndex++;
            
            if (currentFrameIndex >= animationFrames.Length)
            {
                if (loop)
                {
                    currentFrameIndex = 0;
                }
                else
                {
                    currentFrameIndex = animationFrames.Length - 1;
                    isPlaying = false;
                    animationComplete = true;
                }
            }
            
            // Update the sprite
            if (targetImage != null && currentFrameIndex < animationFrames.Length)
            {
                targetImage.sprite = animationFrames[currentFrameIndex];
            }
        }
    }
    
    public void SetAnimation(Sprite[] frames, float speed = 0.1f, bool shouldLoop = true)
    {
        animationFrames = frames;
        frameTime = speed;
        loop = shouldLoop;
        currentFrameIndex = 0;
        frameTimer = 0f;
        animationComplete = false;
    }
    
    public void PlayAnimation()
    {
        if (animationFrames != null && animationFrames.Length > 0)
        {
            isPlaying = true;
            currentFrameIndex = 0;
            frameTimer = 0f;
            animationComplete = false;
            
            // Set first frame immediately
            if (targetImage != null)
            {
                targetImage.sprite = animationFrames[0];
            }
        }
    }
    
    public void StopAnimation()
    {
        isPlaying = false;
    }
    
    public void PauseAnimation()
    {
        isPlaying = false;
    }
    
    public void ResumeAnimation()
    {
        if (animationFrames != null && animationFrames.Length > 0)
        {
            isPlaying = true;
        }
    }
    
    public void SetStaticSprite(Sprite sprite)
    {
        StopAnimation();
        if (targetImage != null)
        {
            targetImage.sprite = sprite;
        }
    }
    
    public bool IsAnimationComplete()
    {
        return animationComplete;
    }
    
    public void SetFrame(int frameIndex)
    {
        if (animationFrames != null && frameIndex >= 0 && frameIndex < animationFrames.Length)
        {
            currentFrameIndex = frameIndex;
            if (targetImage != null)
            {
                targetImage.sprite = animationFrames[frameIndex];
            }
        }
    }
}