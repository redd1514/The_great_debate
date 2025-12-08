using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Complete map selection controller that handles voting, visual indicators,
/// and player input routing with Tekken 8-style visual feedback.
/// </summary>
public class MapSelectionController : MonoBehaviour
{
    [System.Serializable]
    public class MapOption
    {
        public string mapName;
        public string sceneName;
        public Image mapImage;
        public GameObject votingOverlay; // Container for voting indicators
        public TextMeshProUGUI voteCountText;
        public MapVisualIndicator visualIndicator;
        
        [HideInInspector] public Dictionary<int, bool> playerVotes = new Dictionary<int, bool>();
        [HideInInspector] public int gridX; // Grid position
        [HideInInspector] public int gridY;
        
        public int GetVoteCount()
        {
            int count = 0;
            foreach (var vote in playerVotes.Values)
            {
                if (vote) count++;
            }
            return count;
        }
    }
    
    [Header("Map Configuration")]
    [SerializeField] private MapOption[] maps = new MapOption[4];
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Canvas mainCanvas;
    
    [Header("Voting Settings")]
    [SerializeField] private float votingDuration = 15f;
    [SerializeField] private bool skipVotingIfSinglePlayer = false;
    
    [Header("Animation Settings")]
    [SerializeField] private float zoomScale = 1.5f;
    [SerializeField] private float zoomDuration = 1f;
    
    [Header("Grid Layout")]
    [SerializeField] private int gridColumns = 2;
    [SerializeField] private int gridRows = 2;
    
    // Player data
    private PlayerCharacterData[] activePlayers;
    private Dictionary<int, Vector2Int> playerCurrentPosition = new Dictionary<int, Vector2Int>(); // playerIndex -> grid position
    private Dictionary<int, int> playerLockedVotes = new Dictionary<int, int>(); // playerIndex -> mapIndex
    
    // Voting state
    private float votingTimer;
    private bool votingActive = false;
    private bool votingEnded = false;
    
    // Player colors (same as character select)
    private Color[] playerColors = new Color[]
    {
        Color.red,      // Player 1
        Color.blue,     // Player 2
        Color.green,    // Player 3
        Color.yellow    // Player 4
    };
    
    void Start()
    {
        InitializeMapGrid();
        LoadPlayerData();
        
        // Check if we should skip voting for single player
        if (skipVotingIfSinglePlayer && GetJoinedPlayerCount() == 1)
        {
            SelectRandomMapAndProceed();
        }
        else
        {
            StartVoting();
        }
    }
    
    void Update()
    {
        if (votingActive && !votingEnded)
        {
            HandleAllPlayerInputs();
            UpdateTimer();
            UpdateInstructionText();
            
            if (votingTimer <= 0)
            {
                EndVoting();
            }
        }
    }
    
    void InitializeMapGrid()
    {
        // Assign grid positions to maps (2x2 grid)
        for (int i = 0; i < maps.Length && i < 4; i++)
        {
            maps[i].gridX = i % gridColumns;
            maps[i].gridY = i / gridColumns;
            
            // Initialize vote tracking
            maps[i].playerVotes.Clear();
            
            // Initialize visual elements
            if (maps[i].visualIndicator != null)
            {
                maps[i].visualIndicator.ClearAllVotes();
            }
            
            UpdateMapVoteDisplay(maps[i]);
        }
    }
    
    void LoadPlayerData()
    {
        if (GameDataManager.Instance != null)
        {
            activePlayers = GameDataManager.Instance.GetSelectedCharacters();
            
            if (activePlayers != null)
            {
                Debug.Log($"MapSelectionController: Loaded {activePlayers.Length} players");
                
                // Initialize each player's position to center (map 0)
                foreach (var player in activePlayers)
                {
                    if (player != null && player.isJoined)
                    {
                        playerCurrentPosition[player.playerIndex] = new Vector2Int(0, 0);
                        Debug.Log($"  Player {player.playerIndex + 1}: {player.inputDeviceName} - {player.playerColor}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("MapSelectionController: No player data available!");
            }
        }
        else
        {
            Debug.LogError("MapSelectionController: GameDataManager not found!");
        }
    }
    
    void StartVoting()
    {
        votingActive = true;
        votingTimer = votingDuration;
        votingEnded = false;
        
        Debug.Log("MapSelectionController: Voting started!");
    }
    
    void HandleAllPlayerInputs()
    {
        if (activePlayers == null) return;
        
        foreach (var player in activePlayers)
        {
            if (player != null && player.isJoined)
            {
                HandlePlayerInput(player);
            }
        }
    }
    
    void HandlePlayerInput(PlayerCharacterData player)
    {
        int playerIndex = player.playerIndex;
        string deviceName = player.inputDeviceName;
        
        // Navigation
        Vector2 input = GetNavigationInput(deviceName);
        if (input != Vector2.zero)
        {
            NavigatePlayer(playerIndex, input);
        }
        
        // Submit (lock vote)
        if (GetSubmitInput(deviceName))
        {
            LockPlayerVote(playerIndex);
        }
        
        // Cancel (unlock vote)
        if (GetCancelInput(deviceName))
        {
            UnlockPlayerVote(playerIndex);
        }
    }
    
    Vector2 GetNavigationInput(string deviceName)
    {
        Vector2 input = Vector2.zero;
        
        if (deviceName == "Keyboard")
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                input.x = -1;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                input.x = 1;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                input.y = 1;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                input.y = -1;
        }
        else if (deviceName.StartsWith("Controller"))
        {
            if (int.TryParse(deviceName.Replace("Controller", ""), out int controllerNum))
            {
                string joystickName = $"joystick {controllerNum}";
                
                // D-pad buttons
                if (Input.GetKeyDown($"{joystickName} button 13")) // D-pad left
                    input.x = -1;
                else if (Input.GetKeyDown($"{joystickName} button 14")) // D-pad right
                    input.x = 1;
                if (Input.GetKeyDown($"{joystickName} button 11")) // D-pad up
                    input.y = 1;
                else if (Input.GetKeyDown($"{joystickName} button 12")) // D-pad down
                    input.y = -1;
            }
        }
        
        return input;
    }
    
    bool GetSubmitInput(string deviceName)
    {
        if (deviceName == "Keyboard")
        {
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        }
        else if (deviceName.StartsWith("Controller"))
        {
            if (int.TryParse(deviceName.Replace("Controller", ""), out int controllerNum))
            {
                string joystickName = $"joystick {controllerNum}";
                return Input.GetKeyDown($"{joystickName} button 0"); // A button
            }
        }
        return false;
    }
    
    bool GetCancelInput(string deviceName)
    {
        if (deviceName == "Keyboard")
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
        else if (deviceName.StartsWith("Controller"))
        {
            if (int.TryParse(deviceName.Replace("Controller", ""), out int controllerNum))
            {
                string joystickName = $"joystick {controllerNum}";
                return Input.GetKeyDown($"{joystickName} button 1"); // B button
            }
        }
        return false;
    }
    
    void NavigatePlayer(int playerIndex, Vector2 input)
    {
        // Don't navigate if player has locked their vote
        if (playerLockedVotes.ContainsKey(playerIndex))
        {
            return;
        }
        
        if (!playerCurrentPosition.ContainsKey(playerIndex))
        {
            playerCurrentPosition[playerIndex] = new Vector2Int(0, 0);
        }
        
        Vector2Int currentPos = playerCurrentPosition[playerIndex];
        Vector2Int newPos = currentPos;
        
        // Navigate in grid
        if (input.x < 0) // Left
        {
            newPos.x = Mathf.Max(0, currentPos.x - 1);
        }
        else if (input.x > 0) // Right
        {
            newPos.x = Mathf.Min(gridColumns - 1, currentPos.x + 1);
        }
        
        if (input.y > 0) // Up
        {
            newPos.y = Mathf.Max(0, currentPos.y - 1);
        }
        else if (input.y < 0) // Down
        {
            newPos.y = Mathf.Min(gridRows - 1, currentPos.y + 1);
        }
        
        if (newPos != currentPos)
        {
            playerCurrentPosition[playerIndex] = newPos;
            Debug.Log($"Player {playerIndex + 1} moved to grid position ({newPos.x}, {newPos.y})");
        }
    }
    
    void LockPlayerVote(int playerIndex)
    {
        // Already locked
        if (playerLockedVotes.ContainsKey(playerIndex))
        {
            return;
        }
        
        if (!playerCurrentPosition.ContainsKey(playerIndex))
        {
            return;
        }
        
        // Get map index from grid position
        Vector2Int pos = playerCurrentPosition[playerIndex];
        int mapIndex = pos.y * gridColumns + pos.x;
        
        if (mapIndex >= 0 && mapIndex < maps.Length)
        {
            var map = maps[mapIndex];
            
            // Lock vote
            playerLockedVotes[playerIndex] = mapIndex;
            map.playerVotes[playerIndex] = true;
            
            // Update visuals
            Color playerColor = GetPlayerColor(playerIndex);
            if (map.visualIndicator != null)
            {
                map.visualIndicator.AddPlayerVote(playerColor);
            }
            
            UpdateMapVoteDisplay(map);
            
            Debug.Log($"Player {playerIndex + 1} locked vote for {map.mapName}");
            
            // Check if all players voted
            if (AllPlayersVoted())
            {
                EndVoting();
            }
        }
    }
    
    void UnlockPlayerVote(int playerIndex)
    {
        if (!playerLockedVotes.ContainsKey(playerIndex))
        {
            return;
        }
        
        int mapIndex = playerLockedVotes[playerIndex];
        
        if (mapIndex >= 0 && mapIndex < maps.Length)
        {
            var map = maps[mapIndex];
            
            // Unlock vote
            playerLockedVotes.Remove(playerIndex);
            map.playerVotes[playerIndex] = false;
            
            // Update visuals
            Color playerColor = GetPlayerColor(playerIndex);
            if (map.visualIndicator != null)
            {
                map.visualIndicator.RemovePlayerVote(playerColor);
            }
            
            UpdateMapVoteDisplay(map);
            
            Debug.Log($"Player {playerIndex + 1} unlocked vote");
        }
    }
    
    bool AllPlayersVoted()
    {
        int joinedCount = GetJoinedPlayerCount();
        return joinedCount > 0 && playerLockedVotes.Count >= joinedCount;
    }
    
    int GetJoinedPlayerCount()
    {
        if (activePlayers == null) return 0;
        
        int count = 0;
        foreach (var player in activePlayers)
        {
            if (player != null && player.isJoined) count++;
        }
        return count;
    }
    
    void UpdateMapVoteDisplay(MapOption map)
    {
        if (map.voteCountText != null)
        {
            int votes = map.GetVoteCount();
            if (votes > 0)
            {
                map.voteCountText.text = votes.ToString();
                map.voteCountText.gameObject.SetActive(true);
            }
            else
            {
                map.voteCountText.gameObject.SetActive(false);
            }
        }
    }
    
    void UpdateTimer()
    {
        votingTimer -= Time.deltaTime;
        
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(votingTimer);
            timerText.text = seconds.ToString();
            
            // Color coding
            if (votingTimer <= 5f)
            {
                timerText.color = Color.red;
            }
            else if (votingTimer <= 10f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }
    
    void UpdateInstructionText()
    {
        if (instructionText == null) return;
        
        int votedCount = playerLockedVotes.Count;
        int totalCount = GetJoinedPlayerCount();
        
        string text = $"Vote for Your Map! ({votedCount}/{totalCount} voted)\n\n";
        text += "Navigate: WASD/Arrows or Controller D-Pad\n";
        text += "Lock Vote: Enter/Space or A Button\n";
        text += "Change Vote: Esc or B Button";
        
        instructionText.text = text;
    }
    
    void EndVoting()
    {
        if (votingEnded) return;
        
        votingActive = false;
        votingEnded = true;
        
        MapOption winner = GetWinningMap();
        
        if (winner != null)
        {
            Debug.Log($"MapSelectionController: Winner is {winner.mapName}");
            
            // Show winner
            if (winnerText != null)
            {
                winnerText.text = $"Selected: {winner.mapName}";
                winnerText.gameObject.SetActive(true);
            }
            
            // Hide timer
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
            
            StartCoroutine(TransitionToMap(winner));
        }
    }
    
    MapOption GetWinningMap()
    {
        MapOption winner = null;
        int maxVotes = -1;
        List<MapOption> tiedMaps = new List<MapOption>();
        
        foreach (var map in maps)
        {
            int votes = map.GetVoteCount();
            
            if (votes > maxVotes)
            {
                maxVotes = votes;
                winner = map;
                tiedMaps.Clear();
                tiedMaps.Add(map);
            }
            else if (votes == maxVotes && votes > 0)
            {
                tiedMaps.Add(map);
            }
        }
        
        // Handle ties
        if (tiedMaps.Count > 1)
        {
            winner = tiedMaps[Random.Range(0, tiedMaps.Count)];
            Debug.Log($"Tie broken randomly: {winner.mapName}");
        }
        
        // No votes - random selection
        if (winner == null || maxVotes == 0)
        {
            winner = maps[Random.Range(0, maps.Length)];
            Debug.Log($"No votes - random selection: {winner.mapName}");
        }
        
        return winner;
    }
    
    IEnumerator TransitionToMap(MapOption winner)
    {
        // Hide non-winning maps
        foreach (var map in maps)
        {
            if (map != winner && map.mapImage != null)
            {
                CanvasGroup cg = map.mapImage.GetComponent<CanvasGroup>();
                if (cg == null) cg = map.mapImage.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }
        
        // Zoom winning map
        if (winner.mapImage != null)
        {
            RectTransform rt = winner.mapImage.GetComponent<RectTransform>();
            Vector3 startPos = rt.position;
            Vector3 startScale = rt.localScale;
            Vector3 targetPos = mainCanvas != null ? mainCanvas.transform.position : Vector3.zero;
            
            float elapsed = 0f;
            while (elapsed < zoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / zoomDuration;
                
                rt.position = Vector3.Lerp(startPos, targetPos, t);
                rt.localScale = Vector3.Lerp(startScale, startScale * zoomScale, t);
                
                yield return null;
            }
        }
        
        yield return new WaitForSeconds(1f);
        
        // Load scene
        if (!string.IsNullOrEmpty(winner.sceneName))
        {
            SceneManager.LoadScene(winner.sceneName);
        }
        else
        {
            Debug.LogError($"No scene name for map: {winner.mapName}");
        }
    }
    
    void SelectRandomMapAndProceed()
    {
        Debug.Log("Single player - skipping vote, selecting random map");
        
        MapOption randomMap = maps[Random.Range(0, maps.Length)];
        
        if (winnerText != null)
        {
            winnerText.text = $"Map: {randomMap.mapName}";
            winnerText.gameObject.SetActive(true);
        }
        
        StartCoroutine(TransitionToMap(randomMap));
    }
    
    Color GetPlayerColor(int playerIndex)
    {
        if (activePlayers != null && playerIndex < activePlayers.Length)
        {
            return activePlayers[playerIndex].playerColor;
        }
        return playerColors[playerIndex % playerColors.Length];
    }
}
