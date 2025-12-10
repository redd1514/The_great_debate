using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPlatformUI : MonoBehaviour
{
    [Header("Platform Elements")]
    public TextMeshProUGUI playerLabel;
    public Image characterFullArt;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI statusText;
    public GameObject joinPrompt;
    public GameObject characterDisplay;
    public GameObject lockedIndicator;

    [Header("Visual Effects")]
    public Image platformBackground;
    public Color activePlayerColor = Color.yellow;
    public Color joinedPlayerColor = Color.white;
    public Color notJoinedPlayerColor = Color.gray;
    public Color lockedPlayerColor = Color.green;

    [Header("Animation (Optional - Auto-setup if empty)")]
    [SerializeField] private CharacterSpriteAnimator characterAnimator;

    private int playerIndex;
    private string inputDeviceName = "";

    void Start()
    {
        playerIndex = transform.GetSiblingIndex();
        if (playerLabel) playerLabel.text = $"Player {playerIndex + 1}";

        // Auto-setup animator if not assigned and characterFullArt exists
        SetupAnimator();
        SetInitialState();
    }

    void SetupAnimator()
    {
        if (characterAnimator == null && characterFullArt != null)
        {
            // Try to find existing component first
            characterAnimator = characterFullArt.gameObject.GetComponent<CharacterSpriteAnimator>();
            
            // If not found, add the component
            if (characterAnimator == null)
            {
                characterAnimator = characterFullArt.gameObject.AddComponent<CharacterSpriteAnimator>();
            }
            
            // Configure the animator
            characterAnimator.targetImage = characterFullArt;
            characterAnimator.playOnStart = false;
            
            Debug.Log($"CharacterSpriteAnimator auto-setup complete for Player {playerIndex + 1}");
        }
    }

    void SetInitialState()
    {
        if (joinPrompt) joinPrompt.SetActive(true);
        if (characterDisplay) characterDisplay.SetActive(false);
        if (lockedIndicator) lockedIndicator.SetActive(false);
        
        UpdateJoinPromptText();

        // Stop any animation when in initial state
        if (characterAnimator) characterAnimator.StopAnimation();

        SetPlatformColor(notJoinedPlayerColor);
    }

    void UpdateJoinPromptText()
    {
        if (statusText)
        {
            if (playerIndex == 0)
            {
                statusText.text = "";
            }
            else
            {
                statusText.text = "";
            }
        }
    }

    public void UpdatePlatform(PlayerCharacterData playerData, CharacterSelectData selectedCharacter, bool isActivePlayer)
    {
        playerIndex = playerData.playerIndex;

        // Update input device display
        UpdateInputDeviceDisplay(playerData);

        if (!playerData.isJoined)
        {
            ShowJoinPrompt();
        }
        else if (playerData.hasLockedCharacter)
        {
            ShowLockedCharacter(playerData.lockedCharacter);
        }
        else
        {
            ShowCharacterPreview(selectedCharacter, isActivePlayer);
        }

        // Update platform color
        if (playerData.hasLockedCharacter)
            SetPlatformColor(lockedPlayerColor);
        else if (isActivePlayer && playerData.isJoined)
            SetPlatformColor(activePlayerColor);
        else if (playerData.isJoined)
            SetPlatformColor(joinedPlayerColor);
        else
            SetPlatformColor(notJoinedPlayerColor);
    }

    void UpdateInputDeviceDisplay(PlayerCharacterData playerData)
    {
        // Get the input device name from the manager
        if (NewCharacterSelectManager.Instance != null)
        {
            inputDeviceName = GetInputDeviceNameForPlayer(playerData.playerIndex);
        }

        // Update player label with device info
        if (playerLabel)
        {
            if (playerData.isJoined && !string.IsNullOrEmpty(inputDeviceName))
            {
                playerLabel.text = $"Player {playerIndex + 1}";
            }
            else
            {
                playerLabel.text = $"Player {playerIndex + 1}";
            }
        }
    }

    string GetInputDeviceNameForPlayer(int playerIndex)
    {
        // This is a simple mapping - ideally you'd get this from the manager
        if (playerIndex == 0) return "Keyboard";
        else return $"Controller {playerIndex}";
    }

    void ShowJoinPrompt()
    {
        if (joinPrompt) joinPrompt.SetActive(true);
        if (characterDisplay) characterDisplay.SetActive(false);
        if (lockedIndicator) lockedIndicator.SetActive(false);
        
        UpdateJoinPromptText();
        
        // Stop animation when showing join prompt
        if (characterAnimator) characterAnimator.StopAnimation();
    }

    void ShowCharacterPreview(CharacterSelectData character, bool isActivePlayer)
    {
        if (joinPrompt) joinPrompt.SetActive(false);
        if (characterDisplay) characterDisplay.SetActive(true);
        if (lockedIndicator) lockedIndicator.SetActive(false);

        if (character != null)
        {
            if (characterName) characterName.text = character.characterName;
            
            // Use animated sprite instead of static sprite
            if (characterAnimator && character.idleAnimationFrames != null && character.idleAnimationFrames.Length > 0)
            {
                characterAnimator.SetAnimation(character.idleAnimationFrames, character.animationSpeed, character.loopAnimation);
                characterAnimator.PlayAnimation();
            }
            else if (characterFullArt)
            {
                // Fallback to static sprite if no animation frames
                if (characterAnimator) characterAnimator.StopAnimation();
                characterFullArt.sprite = character.characterFullArt;
            }
        }

    }

    void ShowLockedCharacter(CharacterSelectData character)
    {
        if (joinPrompt) joinPrompt.SetActive(false);
        if (characterDisplay) characterDisplay.SetActive(true);
        if (lockedIndicator) lockedIndicator.SetActive(true);

        if (character != null)
        {
            if (characterName) characterName.text = character.characterName;
            
            // Continue animation when locked (or you could stop it if you prefer)
            if (characterAnimator && character.idleAnimationFrames != null && character.idleAnimationFrames.Length > 0)
            {
                characterAnimator.SetAnimation(character.idleAnimationFrames, character.animationSpeed, character.loopAnimation);
                characterAnimator.PlayAnimation();
            }
            else if (characterFullArt)
            {
                // Fallback to static sprite if no animation frames
                if (characterAnimator) characterAnimator.StopAnimation();
                characterFullArt.sprite = character.characterFullArt;
            }
        }

        if (statusText)
        {
            statusText.text = "LOCKED IN!";
            statusText.color = Color.green;
        }
    }

    void SetPlatformColor(Color color)
    {
        if (platformBackground)
            platformBackground.color = color;
    }

    public int GetPlayerIndex()
    {
        return playerIndex;
    }

    // Public method to manually setup animator if needed
    [ContextMenu("Setup Animator")]
    public void ForceSetupAnimator()
    {
        SetupAnimator();
    }
}