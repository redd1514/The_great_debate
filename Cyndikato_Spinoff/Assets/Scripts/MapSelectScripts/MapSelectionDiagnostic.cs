using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Diagnostic tool to debug map selection issues
/// Attach this to any GameObject in your Map Selection scene to diagnose problems
/// </summary>
public class MapSelectionDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Settings")]
    public bool enableContinuousLogging = true;
    public float logInterval = 2f;
    
    [Header("Manual Tests")]
    public bool testKeyboardInput = true;
    public bool testControllerInput = true;
    
    private float logTimer = 0f;
    private MapSelect mapSelectScript;
    
    void Start()
    {
        Debug.Log("=== MAP SELECTION DIAGNOSTIC STARTED ===");
        
        // Find MapSelect script
        mapSelectScript = FindObjectOfType<MapSelect>();
        if (mapSelectScript != null)
        {
            Debug.Log("? MapSelect script found");
            AnalyzeMapSelectSetup();
        }
        else
        {
            Debug.LogError("? MapSelect script NOT found in scene!");
        }
        
        // Analyze input systems
        AnalyzeInputSystems();
        
        // Analyze character selection data
        AnalyzeCharacterSelectionData();
    }
    
    void Update()
    {
        if (enableContinuousLogging)
        {
            logTimer += Time.deltaTime;
            if (logTimer >= logInterval)
            {
                LogCurrentState();
                logTimer = 0f;
            }
        }
        
        // Manual input testing
        TestInputSystems();
    }
    
    void AnalyzeMapSelectSetup()
    {
        Debug.Log("--- MapSelect Setup Analysis ---");
        
        // Get the private fields using reflection
        var mapsField = typeof(MapSelect).GetField("maps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (mapsField != null)
        {
            var maps = (MapSelect.MapOption[])mapsField.GetValue(mapSelectScript);
            if (maps != null)
            {
                Debug.Log($"Map Options: {maps.Length}");
                for (int i = 0; i < maps.Length; i++)
                {
                    if (maps[i] != null)
                    {
                        bool hasImage = maps[i].mapImage != null;
                        string mapName = !string.IsNullOrEmpty(maps[i].mapName) ? maps[i].mapName : "No Name";
                        string sceneName = !string.IsNullOrEmpty(maps[i].sceneName) ? maps[i].sceneName : "No Scene";
                        Debug.Log($"  Map {i}: {mapName} -> {sceneName} | Has Image: {hasImage}");
                        
                        if (!hasImage)
                        {
                            Debug.LogError($"? Map {i} missing UI Image!");
                        }
                    }
                    else
                    {
                        Debug.LogError($"? Map {i} is null!");
                    }
                }
            }
            else
            {
                Debug.LogError("? Maps array is null!");
            }
        }
    }
    
    void AnalyzeInputSystems()
    {
        Debug.Log("--- Input Systems Analysis ---");
        
        // Check connected controllers
        var gamepads = Gamepad.all;
        Debug.Log($"Connected Gamepads: {gamepads.Count}");
        for (int i = 0; i < gamepads.Count; i++)
        {
            Debug.Log($"  Gamepad {i}: {gamepads[i].displayName}");
        }
        
        // Check keyboard
        var keyboard = Keyboard.current;
        Debug.Log($"Keyboard Available: {keyboard != null}");
        
        // Check NewCharacterSelectManager
        if (NewCharacterSelectManager.Instance != null)
        {
            var mappings = NewCharacterSelectManager.Instance.GetPadMappingsSnapshot();
            Debug.Log($"Character Select Device Mappings: {mappings.Count}");
            foreach (var kv in mappings)
            {
                Debug.Log($"  {kv.Key.displayName} -> Player {kv.Value + 1}");
            }
            
            bool keyboardJoined = NewCharacterSelectManager.Instance.IsKeyboardJoined();
            int keyboardPlayer = NewCharacterSelectManager.Instance.GetKeyboardPlayerIndex();
            Debug.Log($"Keyboard Joined: {keyboardJoined}, Player: {keyboardPlayer + 1}");
        }
        else
        {
            Debug.LogWarning("?? NewCharacterSelectManager.Instance is null");
        }
    }
    
    void AnalyzeCharacterSelectionData()
    {
        Debug.Log("--- Character Selection Data Analysis ---");
        
        if (GameDataManager.Instance != null)
        {
            var players = GameDataManager.Instance.GetSelectedCharacters();
            if (players != null)
            {
                int joinedCount = 0;
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i] != null && players[i].isJoined && players[i].hasLockedCharacter)
                    {
                        joinedCount++;
                        Debug.Log($"  Player {i + 1}: {players[i].lockedCharacter.characterName}");
                    }
                }
                Debug.Log($"Total Joined Players: {joinedCount}");
            }
            else
            {
                Debug.LogWarning("?? No character selection data found");
            }
        }
        else
        {
            Debug.LogError("? GameDataManager.Instance is null!");
        }
    }
    
    void LogCurrentState()
    {
        if (mapSelectScript == null) return;
        
        // Use reflection to access private fields
        var votingActiveField = typeof(MapSelect).GetField("votingActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var votingTimerField = typeof(MapSelect).GetField("votingTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerSelectionsField = typeof(MapSelect).GetField("playerSelections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (votingActiveField != null && votingTimerField != null)
        {
            bool votingActive = (bool)votingActiveField.GetValue(mapSelectScript);
            float votingTimer = (float)votingTimerField.GetValue(mapSelectScript);
            
            Debug.Log($"[MapSelection] Voting Active: {votingActive}, Timer: {votingTimer:F1}s");
            
            if (playerSelectionsField != null)
            {
                var selections = (Dictionary<int, int>)playerSelectionsField.GetValue(mapSelectScript);
                if (selections != null && selections.Count > 0)
                {
                    Debug.Log($"[MapSelection] Player Selections: {selections.Count}");
                    foreach (var kv in selections)
                    {
                        Debug.Log($"  Player {kv.Key + 1} -> Map {kv.Value}");
                    }
                }
                else
                {
                    Debug.Log("[MapSelection] No player selections yet");
                }
            }
        }
    }
    
    void TestInputSystems()
    {
        if (!testKeyboardInput && !testControllerInput) return;
        
        // Test keyboard input
        if (testKeyboardInput && Keyboard.current != null)
        {
            var kb = Keyboard.current;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
                Debug.Log("?? KEYBOARD: Left/A pressed");
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
                Debug.Log("?? KEYBOARD: Right/D pressed");
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                Debug.Log("?? KEYBOARD: Up/W pressed");
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                Debug.Log("?? KEYBOARD: Down/S pressed");
            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                Debug.Log("?? KEYBOARD: Enter/Space pressed");
        }
        
        // Test controller input
        if (testControllerInput)
        {
            foreach (var pad in Gamepad.all)
            {
                if (pad.dpad.left.wasPressedThisFrame)
                    Debug.Log($"?? CONTROLLER ({pad.displayName}): D-Pad Left pressed");
                if (pad.dpad.right.wasPressedThisFrame)
                    Debug.Log($"?? CONTROLLER ({pad.displayName}): D-Pad Right pressed");
                if (pad.dpad.up.wasPressedThisFrame)
                    Debug.Log($"?? CONTROLLER ({pad.displayName}): D-Pad Up pressed");
                if (pad.dpad.down.wasPressedThisFrame)
                    Debug.Log($"?? CONTROLLER ({pad.displayName}): D-Pad Down pressed");
                if (pad.buttonSouth.wasPressedThisFrame)
                    Debug.Log($"?? CONTROLLER ({pad.displayName}): A/Cross button pressed");
                
                // Test analog stick
                Vector2 leftStick = pad.leftStick.ReadValue();
                if (leftStick.magnitude > 0.7f)
                {
                    Debug.Log($"?? CONTROLLER ({pad.displayName}): Left Stick: {leftStick}");
                }
            }
        }
    }
    
    [ContextMenu("Force Map Selection Analysis")]
    public void ForceAnalysis()
    {
        AnalyzeMapSelectSetup();
        AnalyzeInputSystems();
        AnalyzeCharacterSelectionData();
    }
    
    [ContextMenu("Test Input for 10 seconds")]
    public void TestInputFor10Seconds()
    {
        StartCoroutine(TestInputCoroutine());
    }
    
    System.Collections.IEnumerator TestInputCoroutine()
    {
        Debug.Log("?? Starting 10-second input test - press any controller buttons or keys!");
        testKeyboardInput = true;
        testControllerInput = true;
        
        yield return new WaitForSeconds(10f);
        
        Debug.Log("?? Input test complete");
        testKeyboardInput = false;
        testControllerInput = false;
    }
}