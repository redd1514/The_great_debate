using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapSelect : MonoBehaviour
{
    [System.Serializable]
    public class MapOption
    {
        public string mapName;
        public string sceneName; // ? ADD THIS: actual scene name to load
        public Image mapImage;
    }

    [Header("Map Options (4)")]
    public MapOption[] maps = new MapOption[4];

    [Header("Timer")]
    public float votingDuration = 10f;
    public TextMeshProUGUI timerText;

    [Header("Navigation")]
    public int mapsPerRow = 2; // 2x2 grid by default
    public float navRepeatDelay = 0.25f; // left stick repeat delay

    [Header("Player Indicators")]
    public Color[] playerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };
    public bool useTextIndicators = true; // show TMP label like P1, P2 with color
    [Header("Keyboard (Optional)")]
    public bool useKeyboardAsPlayer = false; // controllers only by default

    private readonly Dictionary<Gamepad, int> padToPlayer = new Dictionary<Gamepad, int>();
    private Keyboard keyboard;
    private int keyboardPlayerIndex = -1;
    private readonly Dictionary<int, int> playerSelections = new Dictionary<int, int>();
    private VerticalGradientImage[] playerIndicators = new VerticalGradientImage[4];
    private TextMeshProUGUI[] playerLabels = new TextMeshProUGUI[4];
    private float[] navCooldownX = new float[4];
    private float[] navCooldownY = new float[4];

    private float votingTimer;
    private bool votingActive;

    private bool IsMapValid(int index)
    {
        if (index < 0 || index >= maps.Length) return false;
        // ? TEMPORARY: Remove image requirement for testing
        return maps[index] != null && !string.IsNullOrEmpty(maps[index].mapName);
        // Original: return maps[index] != null && maps[index].mapImage != null;
    }

    private int WrapIndex(int index)
    {
        if (maps.Length == 0) return 0;
        return (index % maps.Length + maps.Length) % maps.Length;
    }

    private int FindNextValidIndex(int start, Vector2 nav)
    {
        if (maps == null || maps.Length == 0) return 0;
        int current = WrapIndex(start);
        int attempts = maps.Length;
        int step = 0;
        if (nav.x > 0) step = 1;
        else if (nav.x < 0) step = -1;
        else if (nav.y > 0) step = -mapsPerRow;
        else if (nav.y < 0) step = mapsPerRow;
        if (step == 0) return current;
        int candidate = current;
        while (attempts-- > 0)
        {
            candidate = WrapIndex(candidate + step);
            if (IsMapValid(candidate)) return candidate;
        }
        return current; // no valid move found
    }

    private Color GetPlayerColor(int playerId)
    {
        if (playerColors == null || playerColors.Length == 0) return Color.white;
        int idx = Mathf.Abs(playerId) % playerColors.Length;
        return playerColors[idx];
    }

    void Start()
    {
        SetupControllers();
        CreatePlayerIndicators();
        StartVoting();
    }

    void Update()
    {
        if (!votingActive) return;

        votingTimer -= Time.deltaTime;
        if (timerText != null)
        {
            int t = Mathf.CeilToInt(votingTimer);
            timerText.text = t.ToString();
        }

        HandleControllerInput();

        if (votingTimer <= 0f)
        {
            EndVoting();
        }
    }

    void StartVoting()
    {
        votingActive = true;
        votingTimer = votingDuration;

        // Initialize players to the first map and show indicators
        foreach (var kv in padToPlayer)
        {
            int p = kv.Value;
            int initial = 0;
            if (!IsMapValid(initial)) initial = FindNextValidIndex(initial, Vector2.right);
            playerSelections[p] = initial;
            PositionIndicatorOnMap(p, initial);
            SetIndicatorColor(p, GetPlayerColor(p), 0.7f);
        }
        if (keyboardPlayerIndex >= 0)
        {
            int p = keyboardPlayerIndex;
            int initial = 0;
            if (!IsMapValid(initial)) initial = FindNextValidIndex(initial, Vector2.right);
            playerSelections[p] = initial;
            PositionIndicatorOnMap(p, initial);
            SetIndicatorColor(p, GetPlayerColor(p), 0.7f);
        }
    }

    void EndVoting()
    {
        votingActive = false;

        // Count selections per map index
        int[] counts = new int[maps.Length];
        foreach (var kv in playerSelections)
        {
            int sel = Mathf.Clamp(kv.Value, 0, maps.Length - 1);
            counts[sel]++;
        }

        // Find highest count
        int winnerIdx = 0;
        int max = counts.Length > 0 ? counts[0] : 0;
        for (int i = 1; i < counts.Length; i++)
        {
            if (counts[i] > max)
            {
                max = counts[i];
                winnerIdx = i;
            }
        }

        if (winnerIdx >= 0 && winnerIdx < maps.Length && maps[winnerIdx] != null)
        {
            // ? FIXED: Use sceneName instead of mapName
            string sceneName = !string.IsNullOrEmpty(maps[winnerIdx].sceneName) 
                ? maps[winnerIdx].sceneName 
                : maps[winnerIdx].mapName; // fallback to mapName if sceneName not set
            
            if (!string.IsNullOrEmpty(sceneName))
            {
                Debug.Log($"Loading map: {maps[winnerIdx].mapName} -> Scene: {sceneName}");
                
                // ? ADD: Store selected map in GameDataManager
                if (GameDataManager.Instance != null)
                {
                    // Create a temporary MapData to store the selection
                    MapData selectedMapData = ScriptableObject.CreateInstance<MapData>();
                    selectedMapData.mapName = maps[winnerIdx].mapName;
                    selectedMapData.sceneName = sceneName;
                    
                    GameDataManager.Instance.SetSelectedMap(selectedMapData);
                    Debug.Log($"Stored selected map in GameDataManager: {maps[winnerIdx].mapName}");
                }
                
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("Winner has no scene name assigned.");
            }
        }
    }

    void SetupControllers()
    {
        padToPlayer.Clear();
        keyboard = Keyboard.current;
        
        // ? ENHANCED: Use character selection device mappings if available
        if (NewCharacterSelectManager.Instance != null)
        {
            var charSelectMappings = NewCharacterSelectManager.Instance.GetPadMappingsSnapshot();
            Debug.Log($"MapSelect: Using character select device mappings: {charSelectMappings.Count} controllers");
            
            foreach (var kv in charSelectMappings)
            {
                padToPlayer[kv.Key] = kv.Value;
            }
            
            // Use keyboard from character select if available
            if (NewCharacterSelectManager.Instance.IsKeyboardJoined())
            {
                keyboardPlayerIndex = NewCharacterSelectManager.Instance.GetKeyboardPlayerIndex();
            }
        }
        else
        {
            // Fallback: assign controllers sequentially
            Debug.Log("MapSelect: Character select manager not found, using fallback controller assignment");
            int idx = 0;
            foreach (var pad in Gamepad.all)
            {
                if (idx >= 4) break;
                padToPlayer[pad] = idx;
                idx++;
            }
            
            // Attach keyboard as next player if enabled and slot available
            keyboardPlayerIndex = -1;
            if (useKeyboardAsPlayer && keyboard != null && idx < 4)
            {
                keyboardPlayerIndex = idx;
                idx++;
            }
        }
        
        for (int i = 0; i < 4; i++) { navCooldownX[i] = 0f; navCooldownY[i] = 0f; }
    }

    void HandleControllerInput()
    {
        foreach (var kv in padToPlayer)
        {
            var pad = kv.Key;
            int playerId = kv.Value;

            // Navigation input (D-pad)
            Vector2 nav = Vector2.zero;
            if (pad.dpad.left.wasPressedThisFrame) nav = Vector2.left;
            else if (pad.dpad.right.wasPressedThisFrame) nav = Vector2.right;
            else if (pad.dpad.up.wasPressedThisFrame) nav = Vector2.up;
            else if (pad.dpad.down.wasPressedThisFrame) nav = Vector2.down;

            // Left stick repeat
            float h = pad.leftStick.x.ReadValue();
            float v = pad.leftStick.y.ReadValue();
            navCooldownX[playerId] = Mathf.Max(0f, navCooldownX[playerId] - Time.unscaledDeltaTime);
            navCooldownY[playerId] = Mathf.Max(0f, navCooldownY[playerId] - Time.unscaledDeltaTime);
            const float threshold = 0.6f;
            if (Mathf.Abs(h) > threshold && navCooldownX[playerId] <= 0f)
            {
                nav.x = h > 0 ? 1f : -1f;
                navCooldownX[playerId] = navRepeatDelay;
            }
            if (Mathf.Abs(v) > threshold && navCooldownY[playerId] <= 0f)
            {
                nav.y = v > 0 ? 1f : -1f;
                navCooldownY[playerId] = navRepeatDelay;
            }

            if (nav != Vector2.zero)
            {
                int current = playerSelections.ContainsKey(playerId) ? playerSelections[playerId] : 0;
                int next = FindNextValidIndex(current, nav);
                playerSelections[playerId] = next;
                PositionIndicatorOnMap(playerId, next);
                SetIndicatorColor(playerId, GetPlayerColor(playerId), 0.7f);
                
                // ? ADD: Debug logging for navigation
                Debug.Log($"Player {playerId + 1} navigated to map {next}: {maps[next].mapName}");
            }
        }

        // Keyboard navigation (optional)
        if (keyboardPlayerIndex >= 0 && keyboard != null)
        {
            int playerId = keyboardPlayerIndex;
            Vector2 nav = Vector2.zero;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) nav = Vector2.left;
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) nav = Vector2.right;
            else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) nav = Vector2.up;
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) nav = Vector2.down;

            if (nav != Vector2.zero)
            {
                int current = playerSelections.ContainsKey(playerId) ? playerSelections[playerId] : 0;
                int next = FindNextValidIndex(current, nav);
                playerSelections[playerId] = next;
                PositionIndicatorOnMap(playerId, next);
                SetIndicatorColor(playerId, GetPlayerColor(playerId), 0.7f);
                
                // ? ADD: Debug logging for keyboard navigation
                Debug.Log($"Player {playerId + 1} (keyboard) navigated to map {next}: {maps[next].mapName}");
            }
        }
    }

    void CreatePlayerIndicators()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject go = new GameObject($"Player{i + 1}Indicator");
            var grad = go.AddComponent<VerticalGradientImage>();
            grad.raycastTarget = false;
            Color baseColor = i < playerColors.Length ? playerColors[i] : Color.white;
            Color top = new Color(baseColor.r, baseColor.g, baseColor.b, 0.7f);
            Color bottom = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            grad.SetColors(top, bottom);

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            playerIndicators[i] = grad;
            go.SetActive(false);

            if (useTextIndicators)
            {
                GameObject textObj = new GameObject($"Player{i + 1}Label");
                var tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.text = $"P{i + 1}";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 20f; // requested size
                tmp.enableAutoSizing = false;
                tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                var trt = textObj.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.5f, 0.5f);
                trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.pivot = new Vector2(0.5f, 0.5f);
                trt.localRotation = Quaternion.identity; // ensure not vertical/rotated
                playerLabels[i] = tmp;
                textObj.SetActive(false);
            }
        }
    }

    void PositionIndicatorOnMap(int playerId, int mapIndex)
    {
        if (playerId < 0 || playerId >= playerIndicators.Length) return;
        if (!IsMapValid(mapIndex)) return;

        var indicator = playerIndicators[playerId];
        if (indicator == null) return;

        var mapRect = maps[mapIndex].mapImage.GetComponent<RectTransform>();
        var indRect = indicator.GetComponent<RectTransform>();

        // Parent to the map image and fill it
        if (indRect.parent != mapRect)
            indRect.SetParent(mapRect, false);
        indRect.anchorMin = Vector2.zero;
        indRect.anchorMax = Vector2.one;
        indRect.pivot = mapRect.pivot;
        indRect.anchoredPosition = Vector2.zero;
        indRect.sizeDelta = Vector2.zero;
        indRect.localScale = Vector3.one;
        indicator.transform.SetAsLastSibling();

        indicator.gameObject.SetActive(true);

        // Optional text label centered on the image
        if (useTextIndicators && playerLabels[playerId] != null)
        {
            var label = playerLabels[playerId];
            var lrt = label.GetComponent<RectTransform>();
            if (lrt.parent != mapRect)
                lrt.SetParent(mapRect, false);
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = Vector2.zero;
            label.gameObject.SetActive(true);
            // ensure label draws above overlay
            label.transform.SetAsLastSibling();
        }
    }

    void SetIndicatorColor(int playerId, Color c, float alpha)
    {
        if (playerId < 0 || playerId >= playerIndicators.Length) return;
        var grad = playerIndicators[playerId];
        if (grad == null) return;
        Color top = new Color(c.r, c.g, c.b, alpha);
        Color bottom = new Color(c.r, c.g, c.b, 0f);
        grad.SetColors(top, bottom);
        var outline = grad.GetComponent<Outline>();
        if (outline != null) outline.effectColor = new Color(c.r, c.g, c.b, 1f);
    }
}
