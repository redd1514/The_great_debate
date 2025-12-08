using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class MapSelect : MonoBehaviour
{
    [System.Serializable]
    public class MapOption
    {
        public string mapName;
        public Image mapImage;
        public TextMeshProUGUI voteCountText;
        public int votes = 0;
    }

    [Header("Map Options")]
    public MapOption[] maps = new MapOption[4];
    public MapOption randomOption;

    [Header("Voting Settings")]
    public float votingDuration = 10f;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI selectedMapText;
    
    private Vector2 timerOriginalPivot;

    [Header("Animation Settings")]
    public float zoomScale = 1.5f;
    public float zoomDuration = 0.5f;
    public float timerPulseScale = 1.3f;
    public float timerPulseDuration = 0.5f;
    public Canvas canvas;

    private Dictionary<int, int> playerVotes = new Dictionary<int, int>();
    private float votingTimer;
    private bool votingActive = false;
    private bool isTimerPulsing = false;
    private int lastSecond = -1;

    void Start()
    {
        SetupClickableImages();
        
        // Store and set timer pivot to center for proper scaling
        if (timerText != null)
        {
            RectTransform timerRect = timerText.GetComponent<RectTransform>();
            timerOriginalPivot = timerRect.pivot;
            timerRect.pivot = new Vector2(0.5f, 0.5f);
        }
        
        StartVoting();
    }

    void Update()
    {
        if (votingActive)
        {
            votingTimer -= Time.deltaTime;
            if (timerText != null)
            {
                int currentSecond = Mathf.CeilToInt(votingTimer);
                timerText.text = currentSecond.ToString();
                
                // Change color to red when 5 seconds or less remaining
                if (votingTimer <= 5f)
                {
                    timerText.color = Color.red;
                    
                    // Trigger pulse animation when second changes
                    if (currentSecond != lastSecond && !isTimerPulsing)
                    {
                        StartCoroutine(PulseTimer());
                    }
                }
                else
                {
                    timerText.color = Color.white;
                }
                
                lastSecond = currentSecond;
            }

            if (votingTimer <= 0)
            {
                EndVoting();
            }
        }
    }

    void SetupClickableImages()
    {
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].mapImage != null)
            {
                int index = i;
                AddClickListener(maps[i].mapImage.gameObject, () => VoteForMap(index));
            }
        }

        if (randomOption != null && randomOption.mapImage != null)
        {
            AddClickListener(randomOption.mapImage.gameObject, () => VoteForRandom());
        }
    }

    void AddClickListener(GameObject obj, System.Action callback)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = obj.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { callback(); });
        trigger.triggers.Add(entry);
    }

    public void StartVoting()
    {
        votingActive = true;
        votingTimer = votingDuration;

        foreach (var map in maps)
        {
            map.votes = 0;
            UpdateVoteDisplay(map);
        }
        if (randomOption != null)
        {
            randomOption.votes = 0;
            UpdateVoteDisplay(randomOption);
        }

        playerVotes.Clear();
    }

    public void VoteForMap(int mapIndex, int playerId = 0)
    {
        if (!votingActive) return;

        if (playerVotes.ContainsKey(playerId))
        {
            int previousVote = playerVotes[playerId];
            if (previousVote == -1 && randomOption != null)
            {
                randomOption.votes--;
                UpdateVoteDisplay(randomOption);
            }
            else if (previousVote >= 0 && previousVote < maps.Length)
            {
                maps[previousVote].votes--;
                UpdateVoteDisplay(maps[previousVote]);
            }
        }

        maps[mapIndex].votes++;
        playerVotes[playerId] = mapIndex;
        UpdateVoteDisplay(maps[mapIndex]);
    }

    public void VoteForRandom(int playerId = 0)
    {
        if (!votingActive || randomOption == null) return;

        if (playerVotes.ContainsKey(playerId))
        {
            int previousVote = playerVotes[playerId];
            if (previousVote == -1)
            {
                randomOption.votes--;
                UpdateVoteDisplay(randomOption);
            }
            else if (previousVote >= 0 && previousVote < maps.Length)
            {
                maps[previousVote].votes--;
                UpdateVoteDisplay(maps[previousVote]);
            }
        }

        randomOption.votes++;
        playerVotes[playerId] = -1;
        UpdateVoteDisplay(randomOption);
    }

    void UpdateVoteDisplay(MapOption map)
    {
        if (map.voteCountText != null)
        {
            if (map.votes > 0)
            {
                map.voteCountText.text = map.votes.ToString();
                map.voteCountText.gameObject.SetActive(true);
            }
            else
            {
                map.voteCountText.gameObject.SetActive(false);
            }
        }
    }

    void EndVoting()
    {
        votingActive = false;

        MapOption selectedMap = GetMapWithMostVotes();

        if (selectedMap != null)
        {
            if (selectedMap == randomOption)
            {
                int randomIndex = Random.Range(0, maps.Length);
                selectedMap = maps[randomIndex];
            }

            if (selectedMapText != null)
            {
                selectedMapText.text = selectedMap.mapName;
            }

            Debug.Log("Selected Map: " + selectedMap.mapName);
            
            // Hide the timer
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
            
            // Move selected map to last sibling (renders on top), then hide others
            if (selectedMap.mapImage != null)
            {
                selectedMap.mapImage.transform.SetAsLastSibling();
                HideUnselectedMaps(selectedMap);
                StartCoroutine(ZoomAndCenterMap(selectedMap.mapImage));
            }
            
            LoadMap(selectedMap.mapName);
        }
    }

    void HideUnselectedMaps(MapOption selectedMap)
    {
        // Hide vote count for selected map
        if (selectedMap.voteCountText != null)
        {
            selectedMap.voteCountText.gameObject.SetActive(false);
        }
        
        foreach (var map in maps)
        {
            if (map != selectedMap && map.mapImage != null)
            {
                // Disable raycasts and make fully transparent
                map.mapImage.raycastTarget = false;
                CanvasGroup canvasGroup = map.mapImage.gameObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = map.mapImage.gameObject.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }
        
        if (randomOption != null && randomOption != selectedMap && randomOption.mapImage != null)
        {
            randomOption.mapImage.raycastTarget = false;
            CanvasGroup canvasGroup = randomOption.mapImage.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = randomOption.mapImage.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    System.Collections.IEnumerator ZoomAndCenterMap(Image mapImage)
    {
        RectTransform rectTransform = mapImage.GetComponent<RectTransform>();
        Vector3 originalPosition = rectTransform.position;
        Vector3 originalScale = rectTransform.localScale;

        // Get canvas center position
        Vector3 canvasCenter = Vector3.zero;
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasCenter = canvasRect.position;
        }

        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomDuration;
            
            // Smooth zoom and move to center
            rectTransform.position = Vector3.Lerp(originalPosition, canvasCenter, t);
            rectTransform.localScale = Vector3.Lerp(originalScale, originalScale * zoomScale, t);
            
            yield return null;
        }

        rectTransform.position = canvasCenter;
        rectTransform.localScale = originalScale * zoomScale;
    }

    System.Collections.IEnumerator PulseTimer()
    {
        if (timerText == null) yield break;
        
        isTimerPulsing = true;
        RectTransform rectTransform = timerText.GetComponent<RectTransform>();
        Vector3 originalScale = rectTransform.localScale;
        Vector3 originalPosition = rectTransform.anchoredPosition;
        Color originalColor = timerText.color;
        
        float startScale = 3f; // Start very large
        float elapsed = 0f;
        
        // Pop in large then shrink and fade
        while (elapsed < timerPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / timerPulseDuration;
            
            // Quick ease out for snappy feel
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            
            // Scale from large to slightly smaller than normal
            float currentScale = Mathf.Lerp(startScale, 0.8f, easedT);
            rectTransform.localScale = originalScale * currentScale;
            
            // Fade out as it shrinks
            Color color = originalColor;
            color.a = Mathf.Lerp(1f, 0f, easedT);
            timerText.color = color;
            
            yield return null;
        }
        
        // Reset for next number
        rectTransform.localScale = originalScale;
        rectTransform.anchoredPosition = originalPosition;
        timerText.color = originalColor;
        
        isTimerPulsing = false;
    }

    MapOption GetMapWithMostVotes()
    {
        MapOption winner = maps[0];
        int maxVotes = maps[0].votes;

        for (int i = 1; i < maps.Length; i++)
        {
            if (maps[i].votes > maxVotes)
            {
                maxVotes = maps[i].votes;
                winner = maps[i];
            }
        }

        if (randomOption != null && randomOption.votes > maxVotes)
        {
            winner = randomOption;
        }

        return winner;
    }

    void LoadMap(string mapName)
    {
        // Implement your map loading logic here
        // For example: SceneManager.LoadScene(mapName);
        Debug.Log("Loading map: " + mapName);
    }
}