using UnityEngine;

[System.Serializable]
public class PlayerCharacterData
{
    public int playerIndex;
    public bool isJoined;
    public bool hasLockedCharacter;
    public CharacterSelectData lockedCharacter;
    public int selectedCharacterIndex;

    // New fields for persistence
    public Color playerColor;
    public string inputDeviceName; // Store device name for reconnection
    public int inputDeviceId; // Store device ID

    public PlayerCharacterData(int index)
    {
        playerIndex = index;
        isJoined = false;
        hasLockedCharacter = false;
        selectedCharacterIndex = 0;

        // Set default player colors
        Color[] defaultColors = { Color.red, Color.blue, Color.green, Color.yellow };
        playerColor = defaultColors[index % defaultColors.Length];
    }
}