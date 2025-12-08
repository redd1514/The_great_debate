using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add TextMeshPro namespace

/// <summary>
/// CharacterGridUI manages the character selection grid with Tekken 8-style selection indicators.
/// 
/// Tekken-Style Indicators:
/// - Uses gradient effects that fade from solid color at bottom to transparent at top
/// - Includes blinking animation to show active selection
/// - No outline effects - pure gradient-based visual feedback
/// - Supports multiple players with different colors
/// - Automatically handles positioning over character icons
/// </summary>
public class CharacterGridUI : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject characterIconPrefab;
    public Transform characterGridParent;
    public int charactersPerRow = 4;

    [Header("Multi-Player Selection")]
    public GameObject selectionIndicatorPrefab; // Prefab for individual player indicators
    
    private CharacterSelectData[] characters;
    private GameObject[] characterIcons;
    private GameObject[] playerSelectionIndicators; // Array of indicators for each player
    private int[] playerSelectedIndexes = new int[4]; // Each player's selected character index
    private Color[] playerColors = new Color[4]; // Colors for each player
    
    // Legacy compatibility fields
    private int currentSelectedIndex = 0; // For backwards compatibility
    private int currentPlayerControlling = -1; // For backwards compatibility

    public void InitializeGrid(CharacterSelectData[] availableCharacters)
    {
        if (availableCharacters == null || availableCharacters.Length == 0)
        {
            Debug.LogError("CharacterGridUI: availableCharacters is null or empty!");
            return;
        }

        if (characterIconPrefab == null)
        {
            Debug.LogError("CharacterGridUI: characterIconPrefab is not assigned!");
            return;
        }

        if (characterGridParent == null)
        {
            Debug.LogError("CharacterGridUI: characterGridParent is not assigned!");
            return;
        }

        characters = availableCharacters;
        
        // Ensure player selection arrays are properly initialized
        if (playerSelectedIndexes == null)
        {
            playerSelectedIndexes = new int[4];
        }
        
        if (playerColors == null)
        {
            playerColors = new Color[4];
            // Set default colors
            playerColors[0] = Color.red;
            playerColors[1] = Color.blue;
            playerColors[2] = Color.green;
            playerColors[3] = Color.yellow;
        }
        
        // Initialize player selection arrays
        for (int i = 0; i < 4; i++)
        {
            playerSelectedIndexes[i] = 0; // All start at first character
        }
        
        Debug.Log("CharacterGridUI: Player selection arrays properly initialized");
        
        CreateCharacterIcons();
        CreatePlayerSelectionIndicators();
    }

    void CreateCharacterIcons()
    {
        if (characters == null)
        {
            Debug.LogError("CharacterGridUI: characters array is null in CreateCharacterIcons!");
            return;
        }

        // Clear existing icons
        foreach (Transform child in characterGridParent)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        characterIcons = new GameObject[characters.Length];

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
            {
                Debug.LogWarning($"CharacterGridUI: Character at index {i} is null!");
                continue;
            }

            GameObject iconObj = Instantiate(characterIconPrefab, characterGridParent);
            if (iconObj == null)
            {
                Debug.LogError($"CharacterGridUI: Failed to instantiate character icon for index {i}!");
                continue;
            }

            // Normalize the prefab's RectTransform settings for consistent positioning
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                // Force consistent anchoring for all prefab instances
                iconRect.anchorMin = new Vector2(0f, 1f); // Top-left anchor
                iconRect.anchorMax = new Vector2(0f, 1f); // Top-left anchor
                iconRect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                
                // Let Grid Layout Group handle positioning if it exists
                GridLayoutGroup gridLayout = characterGridParent.GetComponent<GridLayoutGroup>();
                if (gridLayout == null)
                {
                    // Manual positioning if no Grid Layout Group
                    float iconWidth = iconRect.sizeDelta.x;
                    float iconHeight = iconRect.sizeDelta.y;
                    float spacing = 10f; // Adjust as needed
                    
                    int row = i / charactersPerRow;
                    int col = i % charactersPerRow;
                    
                    Vector2 position = new Vector2(
                        col * (iconWidth + spacing),
                        -row * (iconHeight + spacing)
                    );
                    
                    iconRect.anchoredPosition = position;
                }
            }

            // Try to get or add Image component
            Image iconImage = iconObj.GetComponent<Image>();
            if (iconImage == null)
            {
                Debug.LogWarning($"CharacterGridUI: No Image component found on character icon {i}, adding one...");
                iconImage = iconObj.AddComponent<Image>();
            }

            // Set the sprite if available
            if (iconImage != null && characters[i].characterIcon != null)
            {
                iconImage.sprite = characters[i].characterIcon;
            }
            else if (characters[i].characterIcon == null)
            {
                Debug.LogWarning($"CharacterGridUI: Character {i} ({characters[i].characterName}) has no characterIcon sprite assigned!");
                if (iconImage != null)
                {
                    iconImage.color = Color.gray;
                }
            }

            // Handle character name text
            SetupCharacterNameText(iconObj, i);
            characterIcons[i] = iconObj;
        }

        Debug.Log($"CharacterGridUI: Created {characterIcons.Length} character icons");
        RefreshCharacterNames();
        StartCoroutine(ForceCanvasUpdateNextFrame());
    }

    void SetupCharacterNameText(GameObject iconObj, int characterIndex)
    {
        // Try to get Text component (legacy or TextMeshPro) for character name
        Text legacyText = iconObj.GetComponentInChildren<Text>(true);
        TextMeshProUGUI tmpText = iconObj.GetComponentInChildren<TextMeshProUGUI>(true);
        
        // Also try direct child search by name
        if (legacyText == null && tmpText == null)
        {
            Transform characterNameChild = iconObj.transform.Find("CharacterName");
            if (characterNameChild != null)
            {
                legacyText = characterNameChild.GetComponent<Text>();
                tmpText = characterNameChild.GetComponent<TextMeshProUGUI>();
            }
        }

        // Set character name based on which component type we found
        if (tmpText != null)
        {
            tmpText.text = characters[characterIndex].characterName;
        }
        else if (legacyText != null)
        {
            legacyText.text = characters[characterIndex].characterName;
            legacyText.enabled = true;
        }
        else
        {
            Debug.LogWarning($"CharacterGridUI: No Text component found for character {characterIndex}, creating TextMeshPro...");
            
            // Create a child GameObject for the text
            GameObject textObj = new GameObject("CharacterName");
            textObj.transform.SetParent(iconObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = characters[characterIndex].characterName;
            tmpText.fontSize = 14;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            tmpText.raycastTarget = false;
            
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0.3f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textRect.localScale = Vector3.one;
        }
    }

    void CreatePlayerSelectionIndicators()
    {
        // Clear existing indicators
        if (playerSelectionIndicators != null)
        {
            for (int i = 0; i < playerSelectionIndicators.Length; i++)
            {
                if (playerSelectionIndicators[i] != null)
                {
                    if (Application.isPlaying)
                        Destroy(playerSelectionIndicators[i]);
                    else
                        DestroyImmediate(playerSelectionIndicators[i]);
                }
            }
        }

        playerSelectionIndicators = new GameObject[4]; // 4 players max

        for (int playerIndex = 0; playerIndex < 4; playerIndex++)
        {
            GameObject indicator;
            
            if (selectionIndicatorPrefab != null)
            {
                // Check if the custom prefab has the required script
                PlayerSelectionIndicator prefabScript = selectionIndicatorPrefab.GetComponent<PlayerSelectionIndicator>();
                if (prefabScript != null)
                {
                    // Use custom prefab - it has PlayerSelectionIndicator script
                    indicator = Instantiate(selectionIndicatorPrefab, characterGridParent.parent);
                    
                    // Configure the enhanced Tekken-style indicator
                    PlayerSelectionIndicator indicatorScript = indicator.GetComponent<PlayerSelectionIndicator>();
                    if (indicatorScript != null)
                    {
                        indicatorScript.UpdatePlayerColors(playerColors);
                        indicatorScript.SetPlayer(playerIndex);
                        indicatorScript.SetGradientStyle(GradientStyle.TekkenClassic);
                        Debug.Log($"CharacterGridUI: Configured enhanced Tekken 8-style prefab for Player {playerIndex + 1}");
                    }
                    
                    Debug.Log($"CharacterGridUI: Using custom enhanced Tekken 8-style prefab for Player {playerIndex + 1}");
                }
                else
                {
                    // Custom prefab doesn't have the required script, warn and create enhanced indicator instead
                    Debug.LogWarning($"CharacterGridUI: Custom prefab '{selectionIndicatorPrefab.name}' missing PlayerSelectionIndicator script for Player {playerIndex + 1}. Creating enhanced Tekken-style indicator instead.");
                    indicator = CreateEnhancedTekkenStyleIndicator(playerIndex);
                }
            }
            else
            {
                // No custom prefab assigned, create enhanced Tekken-style indicator
                indicator = CreateEnhancedTekkenStyleIndicator(playerIndex);
                Debug.Log($"CharacterGridUI: No custom prefab assigned, created enhanced Tekken-style indicator for Player {playerIndex + 1}");
            }
            
            indicator.SetActive(false); // Start hidden
            playerSelectionIndicators[playerIndex] = indicator;
        }

        Debug.Log("CharacterGridUI: Created Tekken-style player selection indicators");
    }

    GameObject CreateEnhancedTekkenStyleIndicator(int playerIndex)
    {
        // Create main indicator object as a sibling of characterGridParent for proper positioning
        GameObject indicator = new GameObject($"Player{playerIndex + 1}EnhancedTekkenIndicator");
        indicator.transform.SetParent(characterGridParent.parent, false);
        
        // Add the enhanced PlayerSelectionIndicator script first
        PlayerSelectionIndicator indicatorScript = indicator.AddComponent<PlayerSelectionIndicator>();
        
        // The script will automatically create its background and overlay images
        // Set up RectTransform with matching anchors to character icons for proper positioning
        RectTransform rectTransform = indicator.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100); // Will be adjusted when positioned
        
        // Match the icon anchors for consistent positioning - TOP-LEFT anchors
        rectTransform.anchorMin = new Vector2(0f, 1f);   // Top-left anchor (match icons)
        rectTransform.anchorMax = new Vector2(0f, 1f);   // Top-left anchor (match icons)
        rectTransform.pivot = new Vector2(0.5f, 0.5f);   // Center pivot
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Configure the indicator script
        indicatorScript.UpdatePlayerColors(playerColors);
        indicatorScript.SetPlayer(playerIndex);
        indicatorScript.SetGradientStyle(GradientStyle.TekkenClassic); // Set authentic Tekken 8 style
        
        // Configure enhanced settings for better visual appeal
        indicatorScript.blinkSpeed = 2.8f; // Slightly faster for more dynamic feel
        indicatorScript.minAlpha = 0.15f; // Lower minimum for more dramatic effect
        indicatorScript.maxAlpha = 0.95f; // Slightly lower max to avoid overpowering
        indicatorScript.intensityMultiplier = 1.8f; // Higher intensity for vibrant colors
        indicatorScript.gradientSoftness = 0.85f; // Smoother gradient fade
        indicatorScript.gradientStrength = 0.6f; // Moderate strength to avoid overpowering icons
        
        Debug.Log($"CharacterGridUI: Created enhanced Tekken 8-style indicator with character masking for Player {playerIndex + 1}");
        return indicator;
    }

    public void InitializePlayerSelection(int playerIndex, Color playerColor)
    {
        if (playerIndex < 0 || playerIndex >= 4) return;
        
        // Ensure arrays are properly initialized
        if (playerColors == null)
        {
            playerColors = new Color[4];
        }
        
        if (playerSelectedIndexes == null)
        {
            playerSelectedIndexes = new int[4];
            for (int i = 0; i < 4; i++)
            {
                playerSelectedIndexes[i] = 0;
            }
        }
        
        playerColors[playerIndex] = playerColor;
        playerSelectedIndexes[playerIndex] = 0; // Start at first character
        
        // Update the indicator if it exists
        if (playerSelectionIndicators != null && playerSelectionIndicators[playerIndex] != null)
        {
            // Work with enhanced PlayerSelectionIndicator script for Tekken 8-style gradient
            PlayerSelectionIndicator indicatorScript = playerSelectionIndicators[playerIndex].GetComponent<PlayerSelectionIndicator>();
            if (indicatorScript != null)
            {
                indicatorScript.UpdatePlayerColors(playerColors);
                indicatorScript.SetPlayerWithAnimation(playerIndex); // Use enhanced method with immediate animation
                indicatorScript.SetGradientStyle(GradientStyle.TekkenClassic); // Set authentic Tekken style
                
                // Set initial character sprite if available
                if (characterIcons != null && characterIcons.Length > 0 && characterIcons[0] != null)
                {
                    Image iconImage = characterIcons[0].GetComponent<Image>();
                    if (iconImage != null && iconImage.sprite != null)
                    {
                        indicatorScript.SetCharacterSprite(iconImage.sprite);
                        Debug.Log($"CharacterGridUI: Set initial character sprite for Player {playerIndex + 1} masking");
                    }
                }
                
                Debug.Log($"CharacterGridUI: Set enhanced Tekken 8-style gradient with masking for Player {playerIndex + 1} to {playerColor}");
            }
            else
            {
                Debug.LogWarning($"CharacterGridUI: No PlayerSelectionIndicator script found on indicator for Player {playerIndex + 1}");
            }
            
            playerSelectionIndicators[playerIndex].SetActive(true);
        }
        
        // Use delayed coroutine to ensure canvas layout is complete before positioning
        StartCoroutine(DelayedUpdatePlayerSelectionDisplay(playerIndex, 0.1f));
        Debug.Log($"CharacterGridUI: Initialized Player {playerIndex + 1} enhanced Tekken 8-style selection with character masking");
    }

    // Coroutine to delay positioning update until canvas layout is settled
    private System.Collections.IEnumerator DelayedUpdatePlayerSelectionDisplay(int playerIndex, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        
        // Force multiple canvas updates to ensure layout is complete
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();
        
        // Now position the indicator
        UpdatePlayerSelectionDisplay(playerIndex);
        
        Debug.Log($"CharacterGridUI: Completed delayed positioning for Player {playerIndex + 1} after {delaySeconds}s delay");
    }

    public void Navigate(Vector2 input, int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= 4)
        {
            Debug.LogWarning($"CharacterGridUI.Navigate: Invalid playerIndex {playerIndex}");
            return;
        }
        
        if (characters == null || characterIcons == null)
        {
            Debug.LogWarning($"CharacterGridUI.Navigate: characters or characterIcons is null!");
            return;
        }

        // Check if playerSelectedIndexes array is properly initialized
        if (playerSelectedIndexes == null)
        {
            Debug.LogError("CharacterGridUI.Navigate: playerSelectedIndexes is null! Re-initializing...");
            playerSelectedIndexes = new int[4];
            for (int i = 0; i < 4; i++)
            {
                playerSelectedIndexes[i] = 0; // Reset all to first character
            }
        }

        int currentIndex = playerSelectedIndexes[playerIndex];
        int newIndex = currentIndex;

        Debug.Log($"CharacterGridUI.Navigate: Player {playerIndex + 1}, Input: {input}, Current Index: {currentIndex}");

        // Horizontal navigation
        if (input.x > 0.5f) // Right
        {
            newIndex++;
            Debug.Log($"  Navigating RIGHT: {currentIndex} -> {newIndex}");
        }
        else if (input.x < -0.5f) // Left
        {
            newIndex--;
            Debug.Log($"  Navigating LEFT: {currentIndex} -> {newIndex}");
        }

        // Vertical navigation
        if (input.y > 0.5f) // Up
        {
            newIndex -= charactersPerRow;
            Debug.Log($"  Navigating UP: {currentIndex} -> {newIndex}");
        }
        else if (input.y < -0.5f) // Down
        {
            newIndex += charactersPerRow;
            Debug.Log($"  Navigating DOWN: {currentIndex} -> {newIndex}");
        }

        // Wrap around bounds
        if (newIndex < 0)
        {
            newIndex = characters.Length - 1;
            Debug.Log($"  Wrapped around to end: {newIndex}");
        }
        if (newIndex >= characters.Length)
        {
            newIndex = 0;
            Debug.Log($"  Wrapped around to start: {newIndex}");
        }

        if (newIndex != currentIndex)
        {
            playerSelectedIndexes[playerIndex] = newIndex;
            Debug.Log($"CharacterGridUI: Player {playerIndex + 1} navigated from {currentIndex} to {newIndex}");
            UpdatePlayerSelectionDisplay(playerIndex);
            
            // Update legacy compatibility field
            if (playerIndex == 0) currentSelectedIndex = newIndex;
        }
        else
        {
            Debug.Log($"CharacterGridUI: Player {playerIndex + 1} navigation input {input} resulted in no change (stayed at {currentIndex})");
        }
    }

    void UpdatePlayerSelectionDisplay(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= 4) return;
        if (playerSelectionIndicators == null || playerSelectionIndicators[playerIndex] == null) return;
        if (characterIcons == null) return;

        // Safety check for playerSelectedIndexes array
        if (playerSelectedIndexes == null)
        {
            Debug.LogError("CharacterGridUI.UpdatePlayerSelectionDisplay: playerSelectedIndexes is null! Re-initializing...");
            playerSelectedIndexes = new int[4];
            for (int i = 0; i < 4; i++)
            {
                playerSelectedIndexes[i] = 0;
            }
        }

        int selectedIndex = playerSelectedIndexes[playerIndex];
        
        if (selectedIndex >= 0 && selectedIndex < characterIcons.Length && characterIcons[selectedIndex] != null)
        {
            // Position the player's indicator over their selected character
            RectTransform indicatorRect = playerSelectionIndicators[playerIndex].GetComponent<RectTransform>();
            RectTransform iconRect = characterIcons[selectedIndex].GetComponent<RectTransform>();
            Image iconImage = characterIcons[selectedIndex].GetComponent<Image>();
            
            if (indicatorRect != null && iconRect != null)
            {
                Debug.Log($"CharacterGridUI: Positioning Player {playerIndex + 1} indicator over character {selectedIndex}");
                Debug.Log($"  Icon Parent: {iconRect.parent.name}, Indicator Parent: {indicatorRect.parent.name}");
                Debug.Log($"  Icon Local Position: {iconRect.localPosition}");
                Debug.Log($"  Icon Anchored Position: {iconRect.anchoredPosition}");
                Debug.Log($"  Icon World Position: {iconRect.position}");
                Debug.Log($"  Icon Anchors: Min({iconRect.anchorMin}), Max({iconRect.anchorMax})");
                Debug.Log($"  Indicator Anchors: Min({indicatorRect.anchorMin}), Max({indicatorRect.anchorMax})");
                
                // Since icon is child of characterGridParent and indicator is sibling of characterGridParent,
                // we need to use world position for accurate positioning
                Vector3 iconWorldPos = iconRect.position;
                Vector3 indicatorLocalPos = indicatorRect.parent.InverseTransformPoint(iconWorldPos);
                indicatorRect.localPosition = indicatorLocalPos;
                
                // Copy size and pivot to match the icon exactly
                indicatorRect.sizeDelta = iconRect.sizeDelta;
                indicatorRect.pivot = iconRect.pivot;
                
                Debug.Log($"  Set Indicator Local Position: {indicatorRect.localPosition}");
                Debug.Log($"  Set Indicator Size Delta: {indicatorRect.sizeDelta}");
                
                // Force canvas update to ensure positioning is applied
                Canvas.ForceUpdateCanvases();
                
                // Apply offset for multiple players on same character
                Vector2 offset = GetIndicatorOffset(playerIndex, selectedIndex);
                if (offset != Vector2.zero)
                {
                    // Apply offset in local space
                    Vector3 currentLocal = indicatorRect.localPosition;
                    indicatorRect.localPosition = new Vector3(
                        currentLocal.x + offset.x,
                        currentLocal.y + offset.y,
                        currentLocal.z
                    );
                    
                    Debug.Log($"  Applied offset: {offset}, Final Local Position: {indicatorRect.localPosition}");
                }
                
                // Final position check
                Debug.Log($"  Final Indicator World Position: {indicatorRect.position}");
                Debug.Log($"  Position difference (should be small): {Vector3.Distance(iconRect.position, indicatorRect.position)}");
            }
            
            // Ensure the enhanced PlayerSelectionIndicator script is running its animation
            PlayerSelectionIndicator indicatorScript = playerSelectionIndicators[playerIndex].GetComponent<PlayerSelectionIndicator>();
            if (indicatorScript != null)
            {
                // Set character sprite for proper masking
                if (iconImage != null && iconImage.sprite != null)
                {
                    indicatorScript.SetCharacterSprite(iconImage.sprite);
                }
                
                indicatorScript.StartAnimation(); // Ensure enhanced blinking is active
                
                // Trigger flash effect for visual feedback on navigation
                indicatorScript.FlashSelection(0.2f);
                
                Debug.Log($"  Applied character sprite masking for authentic Tekken 8 shape");
            }
            
            playerSelectionIndicators[playerIndex].SetActive(true);
            Debug.Log($"CharacterGridUI: Activated Player {playerIndex + 1} indicator - should now be visible with character shape!");
        }
        else
        {
            playerSelectionIndicators[playerIndex].SetActive(false);
            Debug.Log($"CharacterGridUI: Deactivated Player {playerIndex + 1} indicator");
        }
    }

    Vector2 GetIndicatorOffset(int playerIndex, int characterIndex)
    {
        // Create small offsets so multiple player indicators don't overlap completely
        Vector2 baseOffset = Vector2.zero;
        
        // Check how many other players are selecting the same character
        int playersOnSameCharacter = 0;
        int thisPlayerOrder = 0;
        
        for (int i = 0; i < 4; i++)
        {
            if (i != playerIndex && playerSelectedIndexes[i] == characterIndex)
            {
                if (i < playerIndex) thisPlayerOrder++;
                playersOnSameCharacter++;
            }
        }
        
        if (playersOnSameCharacter > 0)
        {
            // Create a small circular offset based on player order
            float angle = (thisPlayerOrder * 90f) * Mathf.Deg2Rad; // 90 degrees apart
            float radius = 15f;
            baseOffset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }
        
        return baseOffset;
    }

    public void UpdateAllPlayerSelections(PlayerCharacterData[] players, Color[] colors)
    {
        if (players == null || colors == null) return;
        
        // Safety check for playerSelectedIndexes array
        if (playerSelectedIndexes == null)
        {
            Debug.LogError("CharacterGridUI.UpdateAllPlayerSelections: playerSelectedIndexes is null! Re-initializing...");
            playerSelectedIndexes = new int[4];
            for (int i = 0; i < 4; i++)
            {
                playerSelectedIndexes[i] = 0;
            }
        }
        
        // Safety check for playerColors array
        if (playerColors == null)
        {
            playerColors = new Color[4];
        }
        
        for (int i = 0; i < Mathf.Min(players.Length, 4); i++)
        {
            if (players[i] != null && players[i].isJoined && !players[i].hasLockedCharacter)
            {
                // Update color if changed - work with enhanced PlayerSelectionIndicator script
                if (i < colors.Length)
                {
                    playerColors[i] = colors[i];
                    
                    if (playerSelectionIndicators != null && playerSelectionIndicators[i] != null)
                    {
                        // Update enhanced PlayerSelectionIndicator script directly
                        PlayerSelectionIndicator indicatorScript = playerSelectionIndicators[i].GetComponent<PlayerSelectionIndicator>();
                        if (indicatorScript != null)
                        {
                            indicatorScript.UpdatePlayerColors(playerColors);
                            indicatorScript.SetPlayer(i);
                            indicatorScript.StartAnimation(); // Ensure enhanced blinking animation
                            indicatorScript.SetGradientStyle(GradientStyle.TekkenClassic); // Maintain authentic Tekken style
                        }
                    }
                }
                
                // Update selection index with safety check
                if (players[i].selectedCharacterIndex >= 0)
                {
                    playerSelectedIndexes[i] = players[i].selectedCharacterIndex;
                }
                
                UpdatePlayerSelectionDisplay(i);
            }
            else
            {
                // Hide indicator for non-joined or locked players
                if (playerSelectionIndicators != null && playerSelectionIndicators[i] != null)
                {
                    // Stop enhanced animation and hide
                    PlayerSelectionIndicator indicatorScript = playerSelectionIndicators[i].GetComponent<PlayerSelectionIndicator>();
                    if (indicatorScript != null)
                    {
                        indicatorScript.StopAnimation();
                    }
                    
                    playerSelectionIndicators[i].SetActive(false);
                }
            }
        }
    }

    public int GetPlayerSelectedIndex(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= 4) return -1;
        
        // Safety check for playerSelectedIndexes array
        if (playerSelectedIndexes == null)
        {
            Debug.LogError("CharacterGridUI.GetPlayerSelectedIndex: playerSelectedIndexes is null! Re-initializing...");
            playerSelectedIndexes = new int[4];
            for (int i = 0; i < 4; i++)
            {
                playerSelectedIndexes[i] = 0;
            }
            return 0;
        }
        
        return playerSelectedIndexes[playerIndex];
    }

    public CharacterSelectData GetCurrentSelectedCharacter()
    {
        // Return the character selected by player 0 (for compatibility with old system)
        return GetPlayerSelectedCharacter(0);
    }

    public CharacterSelectData GetPlayerSelectedCharacter(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= 4) return null;
        if (characters == null) return null;
        
        int selectedIndex = playerSelectedIndexes[playerIndex];
        if (selectedIndex >= 0 && selectedIndex < characters.Length)
            return characters[selectedIndex];
        
        return null;
    }

    public int GetCurrentSelectedIndex()
    {
        // Return the index selected by player 0 (for compatibility with old system)
        return GetPlayerSelectedIndex(0);
    }

    // Legacy compatibility method
    public void SetControllingPlayer(int playerIndex)
    {
        // This method is kept for compatibility but is no longer needed
        // since all players can control simultaneously
        currentPlayerControlling = playerIndex;
        Debug.Log($"CharacterGridUI: SetControllingPlayer called with {playerIndex} (legacy compatibility)");
    }

    // Keep all the existing helper methods for character name setup and validation
    void Start()
    {
        ValidateSetup();
    }

    void ValidateSetup()
    {
        bool isValid = true;

        if (characterIconPrefab == null)
        {
            Debug.LogError("CharacterGridUI: characterIconPrefab is not assigned in the inspector!");
            isValid = false;
        }

        if (characterGridParent == null)
        {
            Debug.LogError("CharacterGridUI: characterGridParent is not assigned in the inspector!");
            isValid = false;
        }

        if (selectionIndicatorPrefab == null)
        {
            Debug.LogWarning("CharacterGridUI: selectionIndicatorPrefab is not assigned - will create simple indicators automatically");
        }

        if (!isValid)
        {
            Debug.LogError("CharacterGridUI: Setup validation failed! Please check inspector assignments.");
        }
        else
        {
            Debug.Log("CharacterGridUI: Setup validation passed.");
        }
    }

    // Add method to detect and fix corrupted state
    public void ValidateAndFixPlayerState()
    {
        Debug.Log("CharacterGridUI: Validating and fixing player state...");
        
        // Fix player selection indexes array
        if (playerSelectedIndexes == null || playerSelectedIndexes.Length != 4)
        {
            Debug.LogWarning("CharacterGridUI: playerSelectedIndexes corrupted, re-initializing...");
            playerSelectedIndexes = new int[4];
            for (int i = 0; i < 4; i++)
            {
                playerSelectedIndexes[i] = 0;
            }
        }
        
        // Fix player colors array  
        if (playerColors == null || playerColors.Length != 4)
        {
            Debug.LogWarning("CharacterGridUI: playerColors corrupted, re-initializing...");
            playerColors = new Color[4];
            playerColors[0] = Color.red;
            playerColors[1] = Color.blue;
            playerColors[2] = Color.green;
            playerColors[3] = Color.yellow;
        }
        
        // Validate character selection indicators
        if (playerSelectionIndicators == null || playerSelectionIndicators.Length != 4)
        {
            Debug.LogWarning("CharacterGridUI: playerSelectionIndicators corrupted, re-creating...");
            CreatePlayerSelectionIndicators();
        }
        
        Debug.Log("CharacterGridUI: Player state validation complete");
    }

    public void RefreshCharacterNames()
    {
        if (characterIcons == null || characters == null) return;

        for (int i = 0; i < characterIcons.Length; i++)
        {
            if (characterIcons[i] == null || characters[i] == null) continue;

            Text legacyText = characterIcons[i].GetComponentInChildren<Text>();
            TextMeshProUGUI tmpText = characterIcons[i].GetComponentInChildren<TextMeshProUGUI>();

            if (tmpText != null)
            {
                tmpText.text = characters[i].characterName;
                tmpText.enabled = true;
                tmpText.transform.SetAsLastSibling();
            }
            else if (legacyText != null)
            {
                legacyText.text = characters[i].characterName;
                legacyText.enabled = true;
                legacyText.transform.SetAsLastSibling();
            }
        }
        
        Canvas.ForceUpdateCanvases();
    }

    private System.Collections.IEnumerator ForceCanvasUpdateNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
    }
}