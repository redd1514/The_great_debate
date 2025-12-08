using UnityEngine;

[System.Serializable]
public class PlayerCharacterData
{
    public int playerIndex;
    public bool isJoined;
    public bool hasLockedCharacter;
    public int selectedCharacterIndex = -1;
    public CharacterSelectData lockedCharacter;
    
    // Enhanced fields for map voting system
    public InputDeviceType inputDevice = InputDeviceType.None;
    public Color playerColor = Color.white;
    public int currentMapVote = -1; // -1 = no vote, 0-3 = map index

    public PlayerCharacterData(int index)
    {
        playerIndex = index;
        isJoined = false;
        hasLockedCharacter = false;
        selectedCharacterIndex = 0; // Start at first character
        inputDevice = InputDeviceType.None;
        playerColor = GetDefaultPlayerColor(index);
        currentMapVote = -1;
    }
    
    // Get default color for player based on index
    private Color GetDefaultPlayerColor(int index)
    {
        switch (index)
        {
            case 0: return Color.red;      // Player 1
            case 1: return Color.blue;     // Player 2
            case 2: return Color.green;    // Player 3
            case 3: return Color.yellow;   // Player 4
            default: return Color.white;
        }
    }
}

// Input device type enum
public enum InputDeviceType
{
    None,
    Keyboard,
    Controller1,
    Controller2,
    Controller3
}