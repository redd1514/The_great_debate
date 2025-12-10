using UnityEngine;

using UnityEngine.SceneManagement;
using TMPro;

public class PlayerSurvivalChecker : MonoBehaviour
{
    // Screen flash effect
    public UnityEngine.UI.Image screenFlashImage; // Assign a fullscreen UI Image in inspector
    private float flashAlpha = 0f;
    private float flashFadeSpeed = 2.5f;
    public TextMeshProUGUI resultText; // Assign in inspector
    public TextMeshProUGUI continueText; // Assign in inspector
    private bool resultShown = false;
    private float resultTimer = 0f;
    private bool canContinue = false;

    void Update()
    {
        // Handle screen flash fade out
        if (screenFlashImage != null && flashAlpha > 0f)
        {
            flashAlpha -= Time.deltaTime * flashFadeSpeed;
            flashAlpha = Mathf.Clamp01(flashAlpha);
            var c = screenFlashImage.color;
            c.a = flashAlpha;
            screenFlashImage.color = c;
        }
        // (No animation for result text)
        var players = GameObject.FindGameObjectsWithTag("Player");
        int aliveCount = 0;
        GameObject lastAlive = null;
        string debugPlayerStates = "";
        foreach (var player in players)
        {
            var controller = player.GetComponent<PlayerController>();
            debugPlayerStates += $"{player.name}: ";
            if (controller != null)
            {
                debugPlayerStates += $"isEliminated={controller.isEliminated}\n";
                if (!controller.isEliminated)
                {
                    aliveCount++;
                    lastAlive = player;
                }
            }
            else
            {
                debugPlayerStates += "NO CONTROLLER\n";
            }
        }
        Debug.Log($"[PlayerSurvivalChecker] Player states:\n{debugPlayerStates}");
        Debug.Log($"[PlayerSurvivalChecker] Found {aliveCount} players alive. LastAlive: {(lastAlive != null ? lastAlive.name : "null")}");

        if (aliveCount == 1)
        {
            if (!resultShown)
            {
                ShowResult(lastAlive);
                resultShown = true;
                resultTimer = 0f;
            }
            else if (!canContinue)
            {
                resultTimer += Time.deltaTime;
                if (resultTimer >= 3f)
                {
                    if (continueText != null)
                        continueText.gameObject.SetActive(true);
                    canContinue = true;
                }
            }
            else if (canContinue)
            {
                if (Input.anyKeyDown)
                {
                    SceneManager.LoadScene("Menu"); // Change to your actual menu scene name if needed
                }
            }
        }
        else
        {
            // Hide texts if not exactly one player alive
            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }
            if (continueText != null) continueText.gameObject.SetActive(false);
            resultShown = false;
            canContinue = false;
        }
}

    void ShowResult(GameObject winner)
    {
        // Trigger screen flash
        if (screenFlashImage != null)
        {
            var c = screenFlashImage.color;
            c.a = 0.7f; // Flash strength
            screenFlashImage.color = c;
            flashAlpha = 0.7f;
        }
        if (resultText != null)
        {
            int winnerNumber = 0;
            if (winner != null)
            {
                var controller = winner.GetComponent<PlayerController>();
                if (controller != null)
                    winnerNumber = controller.GetPlayerNumber();
            }
            Debug.Log($"[PlayerSurvivalChecker] Showing result for winner: Player {winnerNumber}");
            resultText.text = $"Player {winnerNumber} wins!";
            resultText.gameObject.SetActive(true);
        }
        if (continueText != null)
            continueText.gameObject.SetActive(false);
    }
}