using UnityEngine;

/// <summary>
/// Centralized audio manager for all menu sounds including navigation, selection, and UI feedback.
/// This component should be attached to a persistent GameObject or each menu that needs audio.
/// </summary>
public class MenuAudioManager : MonoBehaviour
{
    [Header("Menu Audio Clips")]
    [Tooltip("Sound played when navigating between menu options")]
    public AudioClip navigationSound;
    
    [Tooltip("Sound played when selecting/confirming a menu option")]
    public AudioClip selectSound;
    
    [Tooltip("Sound played when canceling/going back")]
    public AudioClip cancelSound;
    
    [Tooltip("Sound played when hovering over buttons (mouse hover)")]
    public AudioClip hoverSound;
    
    [Tooltip("Sound played for invalid actions or errors")]
    public AudioClip errorSound;
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [Tooltip("Master volume for all menu sounds")]
    public float masterVolume = 1f;
    
    [Range(0f, 1f)]
    [Tooltip("Volume specifically for navigation sounds")]
    public float navigationVolume = 0.8f;
    
    [Range(0f, 1f)]
    [Tooltip("Volume specifically for selection sounds")]
    public float selectionVolume = 1f;
    
    [Range(0f, 1f)]
    [Tooltip("Volume specifically for hover sounds")]
    public float hoverVolume = 0.6f;
    
    [Header("Audio Components")]
    [Tooltip("AudioSource for playing menu sounds (will auto-create if null)")]
    public AudioSource audioSource;
    
    [Tooltip("Optional secondary AudioSource for overlapping sounds")]
    public AudioSource secondaryAudioSource;
    
    [Header("Advanced Settings")]
    [Tooltip("Prevent rapid-fire navigation sounds")]
    public float navigationCooldown = 0.1f;
    
    [Tooltip("Allow sounds to overlap or cut previous sounds")]
    public bool allowSoundOverlap = true;
    
    [Tooltip("Use 3D spatial audio for menu sounds")]
    public bool use3DAudio = false;
    
    private float lastNavigationTime;
    private static MenuAudioManager instance;
    
    /// <summary>
    /// Singleton instance for easy access from other scripts
    /// </summary>
    public static MenuAudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MenuAudioManager>();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
            
            // Don't destroy on load if this is a persistent menu audio manager
            if (gameObject.name.Contains("Persistent") || gameObject.name.Contains("DontDestroy"))
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (instance != this)
        {
            Debug.LogWarning($"MenuAudioManager: Multiple instances detected. Destroying duplicate on {gameObject.name}");
            Destroy(this);
            return;
        }
        
        SetupAudioSources();
    }
    
    void Start()
    {
        ValidateAudioClips();
    }
    
    void SetupAudioSources()
    {
        // Setup primary AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("MenuAudioManager: Created primary AudioSource");
            }
        }
        
        // Configure primary AudioSource
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = use3DAudio ? 1f : 0f; // 0 = 2D, 1 = 3D
        audioSource.volume = masterVolume;
        
        // Setup secondary AudioSource for overlapping sounds
        if (secondaryAudioSource == null && allowSoundOverlap)
        {
            GameObject secondaryObj = new GameObject("SecondaryMenuAudio");
            secondaryObj.transform.SetParent(transform);
            secondaryAudioSource = secondaryObj.AddComponent<AudioSource>();
            
            secondaryAudioSource.playOnAwake = false;
            secondaryAudioSource.loop = false;
            secondaryAudioSource.spatialBlend = use3DAudio ? 1f : 0f;
            secondaryAudioSource.volume = masterVolume;
            
            Debug.Log("MenuAudioManager: Created secondary AudioSource for overlapping sounds");
        }
    }
    
    void ValidateAudioClips()
    {
        bool hasAnyClips = navigationSound != null || selectSound != null || cancelSound != null || hoverSound != null;
        
        if (!hasAnyClips)
        {
            Debug.LogWarning("MenuAudioManager: No audio clips assigned! Please assign audio clips in the inspector.");
        }
        else
        {
            Debug.Log($"MenuAudioManager initialized with {CountAssignedClips()} audio clips");
        }
    }
    
    int CountAssignedClips()
    {
        int count = 0;
        if (navigationSound != null) count++;
        if (selectSound != null) count++;
        if (cancelSound != null) count++;
        if (hoverSound != null) count++;
        if (errorSound != null) count++;
        return count;
    }
    
    #region Public Audio Methods
    
    /// <summary>
    /// Play navigation sound (moving between menu options)
    /// </summary>
    public void PlayNavigationSound()
    {
        // Cooldown check to prevent rapid-fire sounds
        if (Time.time - lastNavigationTime < navigationCooldown)
        {
            return;
        }
        
        if (navigationSound != null)
        {
            PlaySound(navigationSound, navigationVolume);
            lastNavigationTime = Time.time;
            
            Debug.Log("MenuAudioManager: Navigation sound played");
        }
    }
    
    /// <summary>
    /// Play selection/confirm sound
    /// </summary>
    public void PlaySelectSound()
    {
        if (selectSound != null)
        {
            PlaySound(selectSound, selectionVolume);
            Debug.Log("MenuAudioManager: Selection sound played");
        }
    }
    
    /// <summary>
    /// Play cancel/back sound
    /// </summary>
    public void PlayCancelSound()
    {
        if (cancelSound != null)
        {
            PlaySound(cancelSound, selectionVolume);
            Debug.Log("MenuAudioManager: Cancel sound played");
        }
    }
    
    /// <summary>
    /// Play hover sound (mouse hover over buttons)
    /// </summary>
    public void PlayHoverSound()
    {
        if (hoverSound != null)
        {
            PlaySound(hoverSound, hoverVolume);
        }
    }
    
    /// <summary>
    /// Play error sound (invalid actions)
    /// </summary>
    public void PlayErrorSound()
    {
        if (errorSound != null)
        {
            PlaySound(errorSound, selectionVolume);
            Debug.Log("MenuAudioManager: Error sound played");
        }
    }
    
    /// <summary>
    /// Play a custom audio clip with specified volume
    /// </summary>
    public void PlayCustomSound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            PlaySound(clip, volume);
        }
    }
    
    #endregion
    
    #region Private Audio Methods
    
    void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        
        float finalVolume = volume * masterVolume;
        
        if (allowSoundOverlap && secondaryAudioSource != null && audioSource.isPlaying)
        {
            // Use secondary audio source if primary is busy
            secondaryAudioSource.PlayOneShot(clip, finalVolume);
        }
        else
        {
            audioSource.PlayOneShot(clip, finalVolume);
        }
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// Set master volume for all menu sounds
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (audioSource != null) audioSource.volume = masterVolume;
        if (secondaryAudioSource != null) secondaryAudioSource.volume = masterVolume;
    }
    
    /// <summary>
    /// Mute/unmute all menu audio
    /// </summary>
    public void SetMuted(bool muted)
    {
        if (audioSource != null) audioSource.mute = muted;
        if (secondaryAudioSource != null) secondaryAudioSource.mute = muted;
        
        Debug.Log($"MenuAudioManager: Audio {(muted ? "muted" : "unmuted")}");
    }
    
    /// <summary>
    /// Stop all currently playing menu sounds
    /// </summary>
    public void StopAllSounds()
    {
        if (audioSource != null) audioSource.Stop();
        if (secondaryAudioSource != null) secondaryAudioSource.Stop();
    }
    
    #endregion
    
    #region Integration Helper Methods
    
    /// <summary>
    /// Auto-integrate with existing main menu
    /// Call this to connect MenuAudioManager with your existing Mainmenu script
    /// </summary>
    [ContextMenu("Auto-Integrate with Main Menu")]
    public void AutoIntegrateWithMainMenu()
    {
        Mainmenu mainMenu = FindObjectOfType<Mainmenu>();
        
        if (mainMenu == null)
        {
            Debug.LogWarning("MenuAudioManager: No Mainmenu component found for auto-integration");
            return;
        }
        
        // Set up audio source reference
        if (mainMenu.menuAudioSource == null)
        {
            mainMenu.menuAudioSource = audioSource;
            Debug.Log("MenuAudioManager: Connected AudioSource to main menu");
        }
        
        // Set up audio clips
        if (mainMenu.navigationSound == null && navigationSound != null)
        {
            mainMenu.navigationSound = navigationSound;
            Debug.Log("MenuAudioManager: Connected navigation sound to main menu");
        }
        
        if (mainMenu.selectSound == null && selectSound != null)
        {
            mainMenu.selectSound = selectSound;
            Debug.Log("MenuAudioManager: Connected select sound to main menu");
        }
        
        Debug.Log("MenuAudioManager: Auto-integration completed!");
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Test Navigation Sound")]
    public void TestNavigationSound()
    {
        PlayNavigationSound();
    }
    
    [ContextMenu("Test Select Sound")]
    public void TestSelectSound()
    {
        PlaySelectSound();
    }
    
    [ContextMenu("Test All Sounds")]
    public void TestAllSounds()
    {
        Debug.Log("Testing all menu sounds...");
        
        if (navigationSound != null)
        {
            PlayNavigationSound();
            Invoke(nameof(DelayedSelectTest), 0.5f);
        }
        else
        {
            TestSelectSound();
        }
    }
    
    void DelayedSelectTest()
    {
        TestSelectSound();
        
        if (cancelSound != null)
        {
            Invoke(nameof(DelayedCancelTest), 0.5f);
        }
    }
    
    void DelayedCancelTest()
    {
        PlayCancelSound();
        Debug.Log("Menu audio test completed!");
    }
    
    #endregion
    
    void OnValidate()
    {
        // Clamp volume values in inspector
        masterVolume = Mathf.Clamp01(masterVolume);
        navigationVolume = Mathf.Clamp01(navigationVolume);
        selectionVolume = Mathf.Clamp01(selectionVolume);
        hoverVolume = Mathf.Clamp01(hoverVolume);
        
        // Update audio source volume if available
        if (audioSource != null)
        {
            audioSource.volume = masterVolume;
        }
    }
}