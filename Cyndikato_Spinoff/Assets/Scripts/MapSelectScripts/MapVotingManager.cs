using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MapVotingManager : MonoBehaviour
{
    [System.Serializable]
    public class MapVoteOption
    {
        public MapData mapData;
        public Image mapImage;
        public MapVisualIndicator visualIndicator;
        public TextMeshProUGUI voteCountText;
        public GameObject selectionHighlight; // Optional highlight for current selection
        
        [HideInInspector] public Dictionary<int, bool> playerVotes = new Dictionary<int, bool>();
        
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
    
    [Header("Map Options")]
    [SerializeField] private MapVoteOption[] mapOptions = new MapVoteOption[4];
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI selectedMapText;
    [SerializeField] private Canvas mainCanvas;
    
    [Header("Voting Settings")]
    [SerializeField] private float votingDuration = 15f;
    [SerializeField] private float zoomDuration = 1f;
    [SerializeField] private float zoomScale = 1.5f;
    
    [Header("Player Colors")]
    private Color[] playerColors = new Color[]
    {
        Color.red,      // Player 1
        Color.blue,     // Player 2
        Color.green,    // Player 3
        Color.yellow    // Player 4
    };
    
    private PlayerCharacterData[] activePlayers;
    private Dictionary<int, int> playerCurrentSelection = new Dictionary<int, int>(); // playerIndex -> mapIndex
    private Dictionary<int, int> playerLockedVotes = new Dictionary<int, int>(); // playerIndex -> mapIndex
    private float votingTimer;
    private bool votingActive = false;
    private bool votingEnded = false;
    
    // Input tracking
    private Dictionary<string, int> deviceToPlayerMap = new Dictionary<string, int>();
    
    void Start()
    {
        LoadPlayerData();
        SetupInputMapping();
        InitializeMapOptions();
        StartVoting();
    }
    
    void Update()
    {
        if (votingActive && !votingEnded)
        {
            HandleAllPlayerInputs();
            UpdateTimer();
            
            if (votingTimer <= 0)
            {
                EndVoting();
            }
        }
    }
    
    void LoadPlayerData()
    {
        if (GameDataManager.Instance != null)
        {
            activePlayers = GameDataManager.Instance.GetSelectedCharacters();
            
            if (activePlayers != null)
            {
                Debug.Log($"MapVotingManager: Loaded {activePlayers.Length} players from GameDataManager");
                foreach (var player in activePlayers)
                {
                    if (player != null && player.isJoined)
                    {
                        Debug.Log($"  Player {player.playerIndex + 1}: Device={player.inputDeviceName}, Color={player.playerColor}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("MapVotingManager: No player data found in GameDataManager!");
            }
        }
        else
        {
            Debug.LogError("MapVotingManager: GameDataManager instance not found!");
        }
    }
    
    void SetupInputMapping()
    {
        if (activePlayers == null) return;
        
        deviceToPlayerMap.Clear();
        
        foreach (var player in activePlayers)
        {
            if (player != null && player.isJoined)
            {
                // Map device name to player index
                deviceToPlayerMap[player.inputDeviceName] = player.playerIndex;
                
                // Initialize player's current selection to first map
                playerCurrentSelection[player.playerIndex] = 0;
            }
        }
        
        Debug.Log($"MapVotingManager: Input mapping setup for {deviceToPlayerMap.Count} devices");
    }
    
    void InitializeMapOptions()
    {
        foreach (var option in mapOptions)
        {
            if (option != null)
            {
                option.playerVotes.Clear();
                
                if (option.visualIndicator != null)
                {
                    option.visualIndicator.ClearAllVotes();
                }
                
                UpdateVoteDisplay(option);
                
                if (option.selectionHighlight != null)
                {
                    option.selectionHighlight.SetActive(false);
                }
            }
        }
    }
    
    void StartVoting()
    {
        votingActive = true;
        votingTimer = votingDuration;
        votingEnded = false;
        
        UpdateInstructionText();
        
        Debug.Log("MapVotingManager: Voting started!");
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
        
        // Get navigation input
        Vector2 input = GetNavigationInput(deviceName);
        
        if (input.x != 0 || input.y != 0)
        {
            NavigatePlayer(playerIndex, input);
        }
        
        // Get submit input (lock vote)
        if (GetSubmitInput(deviceName))
        {
            LockPlayerVote(playerIndex);
        }
        
        // Get cancel input (unlock vote)
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
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                input.y = 1;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                input.y = -1;
        }
        else if (deviceName.StartsWith("Controller"))
        {
            // Extract controller number (e.g., "Controller1" -> 1)
            if (int.TryParse(deviceName.Replace("Controller", ""), out int controllerNum))
            {
                string joystickName = $"joystick {controllerNum}";
                
                // D-pad navigation
                if (Input.GetKeyDown($"{joystickName} button 13")) // D-pad left
                    input.x = -1;
                else if (Input.GetKeyDown($"{joystickName} button 14")) // D-pad right
                    input.x = 1;
                else if (Input.GetKeyDown($"{joystickName} button 11")) // D-pad up
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
        // Don't allow navigation if player has locked their vote
        if (playerLockedVotes.ContainsKey(playerIndex))
        {
            return;
        }
        
        if (!playerCurrentSelection.ContainsKey(playerIndex))
        {
            playerCurrentSelection[playerIndex] = 0;
        }
        
        int currentIndex = playerCurrentSelection[playerIndex];
        int newIndex = currentIndex;
        
        // Grid navigation (2x2)
        if (input.x < 0) // Left
        {
            newIndex = (currentIndex % 2 == 0) ? currentIndex + 1 : currentIndex - 1;
        }
        else if (input.x > 0) // Right
        {
            newIndex = (currentIndex % 2 == 0) ? currentIndex + 1 : currentIndex - 1;
        }
        else if (input.y > 0) // Up
        {
            newIndex = (currentIndex >= 2) ? currentIndex - 2 : currentIndex + 2;
        }
        else if (input.y < 0) // Down
        {
            newIndex = (currentIndex >= 2) ? currentIndex - 2 : currentIndex + 2;
        }
        
        // Clamp to valid range
        newIndex = Mathf.Clamp(newIndex, 0, mapOptions.Length - 1);
        
        if (newIndex != currentIndex)
        {
            playerCurrentSelection[playerIndex] = newIndex;
            UpdatePlayerSelectionHighlight(playerIndex, newIndex);
            
            Debug.Log($"Player {playerIndex + 1} navigated to map {newIndex}");
        }
    }
    
    void UpdatePlayerSelectionHighlight(int playerIndex, int mapIndex)
    {
        // Update the visual highlight for this player's current selection
        // This could show a colored border or glow based on player color
        if (mapIndex >= 0 && mapIndex < mapOptions.Length)
        {
            var option = mapOptions[mapIndex];
            if (option.selectionHighlight != null)
            {
                // Show highlight with player's color
                Image highlightImage = option.selectionHighlight.GetComponent<Image>();
                if (highlightImage != null)
                {
                    Color playerColor = GetPlayerColor(playerIndex);
                    highlightImage.color = playerColor;
                }
                
                option.selectionHighlight.SetActive(true);
            }
        }
    }
    
    void LockPlayerVote(int playerIndex)
    {
        // Don't allow re-voting if already locked
        if (playerLockedVotes.ContainsKey(playerIndex))
        {
            return;
        }
        
        if (!playerCurrentSelection.ContainsKey(playerIndex))
        {
            return;
        }
        
        int mapIndex = playerCurrentSelection[playerIndex];
        
        if (mapIndex >= 0 && mapIndex < mapOptions.Length)
        {
            var option = mapOptions[mapIndex];
            
            // Lock the vote
            playerLockedVotes[playerIndex] = mapIndex;
            option.playerVotes[playerIndex] = true;
            
            // Update visual indicator
            Color playerColor = GetPlayerColor(playerIndex);
            if (option.visualIndicator != null)
            {
                option.visualIndicator.AddPlayerVote(playerColor);
            }
            
            UpdateVoteDisplay(option);
            
            Debug.Log($"Player {playerIndex + 1} locked vote for map {mapIndex}");
            
            // Check if all players have voted
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
        
        if (mapIndex >= 0 && mapIndex < mapOptions.Length)
        {
            var option = mapOptions[mapIndex];
            
            // Unlock the vote
            playerLockedVotes.Remove(playerIndex);
            option.playerVotes[playerIndex] = false;
            
            // Update visual indicator
            Color playerColor = GetPlayerColor(playerIndex);
            if (option.visualIndicator != null)
            {
                option.visualIndicator.RemovePlayerVote(playerColor);
            }
            
            UpdateVoteDisplay(option);
            
            Debug.Log($"Player {playerIndex + 1} unlocked vote for map {mapIndex}");
        }
    }
    
    bool AllPlayersVoted()
    {
        if (activePlayers == null) return false;
        
        int joinedCount = 0;
        int votedCount = 0;
        
        foreach (var player in activePlayers)
        {
            if (player != null && player.isJoined)
            {
                joinedCount++;
                if (playerLockedVotes.ContainsKey(player.playerIndex))
                {
                    votedCount++;
                }
            }
        }
        
        return joinedCount > 0 && joinedCount == votedCount;
    }
    
    void UpdateVoteDisplay(MapVoteOption option)
    {
        if (option.voteCountText != null)
        {
            int voteCount = option.GetVoteCount();
            if (voteCount > 0)
            {
                option.voteCountText.text = voteCount.ToString();
                option.voteCountText.gameObject.SetActive(true);
            }
            else
            {
                option.voteCountText.gameObject.SetActive(false);
            }
        }
    }
    
    void UpdateTimer()
    {
        if (votingTimer > 0)
        {
            votingTimer -= Time.deltaTime;
            
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(votingTimer);
                timerText.text = seconds.ToString();
                
                // Change color when time is running out
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
    }
    
    void UpdateInstructionText()
    {
        if (instructionText == null) return;
        
        string text = "Vote for your favorite map!\n\n";
        text += "Navigate: WASD/Arrows (Keyboard) or D-Pad (Controller)\n";
        text += "Lock Vote: Enter/Space (Keyboard) or A (Controller)\n";
        text += "Change Vote: Escape (Keyboard) or B (Controller)\n\n";
        
        if (activePlayers != null)
        {
            int votedCount = playerLockedVotes.Count;
            int totalCount = 0;
            foreach (var player in activePlayers)
            {
                if (player != null && player.isJoined) totalCount++;
            }
            
            text += $"Votes Locked: {votedCount}/{totalCount}";
        }
        
        instructionText.text = text;
    }
    
    void EndVoting()
    {
        if (votingEnded) return;
        
        votingActive = false;
        votingEnded = true;
        
        Debug.Log("MapVotingManager: Voting ended!");
        
        // Determine winner
        MapVoteOption winningMap = GetWinningMap();
        
        if (winningMap != null && winningMap.mapData != null)
        {
            Debug.Log($"MapVotingManager: Winning map is {winningMap.mapData.mapDisplayName}");
            
            // Save winning map to GameDataManager
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SetSelectedMap(winningMap.mapData);
            }
            
            // Show result
            if (selectedMapText != null)
            {
                selectedMapText.text = $"Selected: {winningMap.mapData.mapDisplayName}";
                selectedMapText.gameObject.SetActive(true);
            }
            
            // Hide timer
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
            
            // Start transition
            StartCoroutine(TransitionToMap(winningMap));
        }
    }
    
    MapVoteOption GetWinningMap()
    {
        MapVoteOption winner = null;
        int maxVotes = -1;
        List<MapVoteOption> tiedMaps = new List<MapVoteOption>();
        
        foreach (var option in mapOptions)
        {
            if (option != null)
            {
                int votes = option.GetVoteCount();
                
                if (votes > maxVotes)
                {
                    maxVotes = votes;
                    winner = option;
                    tiedMaps.Clear();
                    tiedMaps.Add(option);
                }
                else if (votes == maxVotes && votes > 0)
                {
                    tiedMaps.Add(option);
                }
            }
        }
        
        // Handle ties with random selection
        if (tiedMaps.Count > 1)
        {
            int randomIndex = Random.Range(0, tiedMaps.Count);
            winner = tiedMaps[randomIndex];
            Debug.Log($"MapVotingManager: Tie broken randomly - selected {winner.mapData.mapDisplayName}");
        }
        
        // If no votes, select random map
        if (winner == null || maxVotes == 0)
        {
            int randomIndex = Random.Range(0, mapOptions.Length);
            winner = mapOptions[randomIndex];
            Debug.Log($"MapVotingManager: No votes - random selection {winner.mapData.mapDisplayName}");
        }
        
        return winner;
    }
    
    IEnumerator TransitionToMap(MapVoteOption winningMap)
    {
        // Hide all non-winning maps
        foreach (var option in mapOptions)
        {
            if (option != null && option != winningMap)
            {
                if (option.mapImage != null)
                {
                    CanvasGroup cg = option.mapImage.gameObject.GetComponent<CanvasGroup>();
                    if (cg == null) cg = option.mapImage.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                }
                
                if (option.visualIndicator != null)
                {
                    option.visualIndicator.gameObject.SetActive(false);
                }
                
                if (option.voteCountText != null)
                {
                    option.voteCountText.gameObject.SetActive(false);
                }
            }
        }
        
        // Zoom and center winning map
        if (winningMap.mapImage != null)
        {
            RectTransform rectTransform = winningMap.mapImage.GetComponent<RectTransform>();
            Vector3 originalPosition = rectTransform.position;
            Vector3 originalScale = rectTransform.localScale;
            
            Vector3 centerPosition = mainCanvas != null ? mainCanvas.transform.position : Vector3.zero;
            
            float elapsed = 0f;
            while (elapsed < zoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / zoomDuration;
                
                rectTransform.position = Vector3.Lerp(originalPosition, centerPosition, t);
                rectTransform.localScale = Vector3.Lerp(originalScale, originalScale * zoomScale, t);
                
                yield return null;
            }
        }
        
        // Wait a moment to show the winner
        yield return new WaitForSeconds(1.5f);
        
        // Load the map scene
        if (winningMap.mapData != null && !string.IsNullOrEmpty(winningMap.mapData.sceneName))
        {
            SceneManager.LoadScene(winningMap.mapData.sceneName);
        }
        else
        {
            Debug.LogError("MapVotingManager: Winning map has no scene name!");
        }
    }
    
    Color GetPlayerColor(int playerIndex)
    {
        if (activePlayers != null && playerIndex < activePlayers.Length)
        {
            return activePlayers[playerIndex].playerColor;
        }
        
        // Fallback to default colors
        return playerColors[playerIndex % playerColors.Length];
    }
}
