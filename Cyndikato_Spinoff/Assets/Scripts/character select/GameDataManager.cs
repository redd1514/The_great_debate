using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    private PlayerCharacterData[] selectedPlayers;
    private MapData selectedMap;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetSelectedCharacters(PlayerCharacterData[] playerData)
    {
        selectedPlayers = new PlayerCharacterData[playerData.Length];
        for (int i = 0; i < playerData.Length; i++)
        {
            selectedPlayers[i] = playerData[i];
        }

        Debug.Log("Character selections saved to GameDataManager");
    }

    public PlayerCharacterData[] GetSelectedCharacters()
    {
        return selectedPlayers;
    }

    public CharacterSelectData[] GetLockedCharacters()
    {
        var lockedChars = new System.Collections.Generic.List<CharacterSelectData>();

        if (selectedPlayers != null)
        {
            foreach (var player in selectedPlayers)
            {
                if (player.hasLockedCharacter)
                    lockedChars.Add(player.lockedCharacter);
            }
        }

        return lockedChars.ToArray();
    }
    
    public void SetSelectedMap(MapData mapData)
    {
        selectedMap = mapData;
        Debug.Log($"Selected map saved to GameDataManager: {mapData?.mapName ?? "null"}");
    }
    
    public MapData GetSelectedMap()
    {
        return selectedMap;
    }
}