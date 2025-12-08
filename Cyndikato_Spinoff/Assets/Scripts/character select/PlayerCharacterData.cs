using UnityEngine;

[System.Serializable]
public class PlayerCharacterData
{
    public int playerIndex;
    public bool isJoined;
    public bool hasLockedCharacter;
    public int selectedCharacterIndex = -1;
    public CharacterSelectData lockedCharacter;

    public PlayerCharacterData(int index)
    {
        playerIndex = index;
        isJoined = false;
        hasLockedCharacter = false;
        selectedCharacterIndex = 0; // Start at first character
    }
}