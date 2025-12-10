using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.Experimental.GraphView.GraphView;

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
            Debug.Log("GameDataManager: Singleton instance created and marked as DontDestroyOnLoad");
        }
        else
        {
            Debug.Log("GameDataManager: Duplicate instance found, destroying...");
            Destroy(gameObject);
        }
    }

    public void SetSelectedCharacters(PlayerCharacterData[] playerData)
    {
        if (playerData == null)
        {
            selectedPlayers = null;
            Debug.Log("GameDataManager: Character selections cleared");
            return;
        }

        selectedPlayers = new PlayerCharacterData[playerData.Length];
        for (int i = 0; i < playerData.Length; i++)
        {
            selectedPlayers[i] = playerData[i];
        }

        // Log the selections for debugging
        int lockedCount = 0;
        for (int i = 0; i < selectedPlayers.Length; i++)
        {
            if (selectedPlayers[i] != null && selectedPlayers[i].hasLockedCharacter)
            {
                lockedCount++;
                Debug.Log($"GameDataManager: Player {i + 1} selected {selectedPlayers[i].lockedCharacter.characterName}");
            }
        }

        Debug.Log($"GameDataManager: Character selections saved - {lockedCount} players locked in");
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
                if (player != null && player.hasLockedCharacter && player.lockedCharacter != null)
                    lockedChars.Add(player.lockedCharacter);
            }
        }

        Debug.Log($"GameDataManager: Retrieved {lockedChars.Count} locked characters");
        return lockedChars.ToArray();
    }
    
    public void SetSelectedMap(MapData mapData)
    {
        selectedMap = mapData;
        string mapInfo = mapData != null ? $"{mapData.GetDisplayName()} (Scene: {mapData.GetSceneName()})" : "null";
        Debug.Log($"GameDataManager: Selected map saved - {mapInfo}");
    }
    
    public MapData GetSelectedMap()
    {
        return selectedMap;
    }

    /// <summary>
    /// Gets the number of joined players from character selection
    /// </summary>
    public int GetJoinedPlayerCount()
    {
        if (selectedPlayers == null) return 0;
        
        int count = 0;
        for (int i = 0; i < selectedPlayers.Length; i++)
        {
            if (selectedPlayers[i] != null && selectedPlayers[i].isJoined)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Gets the number of players who have locked their character selection
    /// </summary>
    public int GetLockedPlayerCount()
    {
        if (selectedPlayers == null) return 0;
        
        int count = 0;
        for (int i = 0; i < selectedPlayers.Length; i++)
        {
            if (selectedPlayers[i] != null && selectedPlayers[i].hasLockedCharacter)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Gets a specific player's character selection
    /// </summary>
    public PlayerCharacterData GetPlayerCharacterData(int playerIndex)
    {
        if (selectedPlayers == null || playerIndex < 0 || playerIndex >= selectedPlayers.Length)
        {
            return null;
        }
        return selectedPlayers[playerIndex];
    }

    /// <summary>
    /// Checks if a specific player has joined and locked their character
    /// </summary>
    public bool IsPlayerReady(int playerIndex)
    {
        var playerData = GetPlayerCharacterData(playerIndex);
        return playerData != null && playerData.isJoined && playerData.hasLockedCharacter;
    }

    /// <summary>
    /// Gets information about the current game state for debugging
    /// </summary>
    public void LogGameState()
    {
        Debug.Log("=== GAME DATA MANAGER STATE ===");
        Debug.Log($"Selected Map: {(selectedMap != null ? selectedMap.GetDisplayName() : "None")}");
        Debug.Log($"Joined Players: {GetJoinedPlayerCount()}");
        Debug.Log($"Locked Players: {GetLockedPlayerCount()}");
        
        if (selectedPlayers != null)
        {
            for (int i = 0; i < selectedPlayers.Length; i++)
            {
                if (selectedPlayers[i] != null && selectedPlayers[i].isJoined)
                {
                    string characterName = selectedPlayers[i].hasLockedCharacter ? 
                        selectedPlayers[i].lockedCharacter?.characterName ?? "Unknown" : 
                        "Not Locked";
                    Debug.Log($"  Player {i + 1}: {characterName}");
                }
            }
        }
        Debug.Log("===============================");
    }

    /// <summary>
    /// Clears all game data (useful for returning to main menu)
    /// </summary>
    public void ClearAllData()
    {
        selectedPlayers = null;
        selectedMap = null;
        Debug.Log("GameDataManager: All game data cleared");
    }

    /// <summary>
    /// Validates that the game data is ready for gameplay
    /// </summary>
    public bool ValidateGameData()
    {
        bool hasPlayers = GetLockedPlayerCount() > 0;
        bool hasMap = selectedMap != null;
        
        Debug.Log($"GameDataManager: Validation - Players Ready: {hasPlayers}, Map Selected: {hasMap}");
        
        return hasPlayers; // Map is optional, but we need at least one player
    }

    /// <summary>
    /// Called when transitioning between scenes to ensure data persistence
    /// </summary>
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Called when a scene is loaded - useful for logging and validation
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"GameDataManager: Scene loaded - {scene.name}");
        
        // Log current game state when entering gameplay scenes
        if (scene.name.Contains("Gameplay") || scene.name.Contains("Map") || scene.name.Contains("Game"))
        {
            LogGameState();
            if (!ValidateGameData())
            {
                Debug.LogWarning("GameDataManager: Entered gameplay scene without valid game data!");
            }
        }
    }

    // Context menu methods for debugging in the editor
    [ContextMenu("Log Current Game State")]
    void DebugLogGameState()
    {
        LogGameState();
    }

    [ContextMenu("Clear All Game Data")]
    void DebugClearAllData()
    {
        ClearAllData();
    }

    [ContextMenu("Validate Game Data")]
    void DebugValidateGameData()
    {
        bool isValid = ValidateGameData();
        Debug.Log($"GameDataManager: Game data validation result: {isValid}");
    }
}