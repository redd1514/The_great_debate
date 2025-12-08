using UnityEngine;

/// <summary>
/// Enhanced debug helper to identify and fix multiple MapSelectionManager instances
/// </summary>
public class MapSelectionDebugHelper : MonoBehaviour
{
    [ContextMenu("?? QUICK FIX - Stop Duplicates & Restart Timer")]
    public void QuickFixStopDuplicatesAndRestart()
    {
        Debug.LogWarning("?? === QUICK FIX: STOPPING DUPLICATES & RESTARTING TIMER ===");
        
        // 1. Force stop all random selections immediately
        ForceStopAllRandomSelections();
        
        // 2. Remove duplicates immediately
        RemoveDuplicateMapSelectionManagers();
        
        // 3. Force restart the singleton with debug enabled
        MapSelectionManager singleton = MapSelectionManager.Instance;
        if (singleton != null)
        {
            Debug.Log("? Quick fix - forcing restart with debug enabled...");
            singleton.SendMessage("ForceRestartWithDebug", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogError("? Quick fix failed - no singleton instance available!");
        }
    }
    
    [ContextMenu("Find All MapSelectionManagers")]
    public void FindAllMapSelectionManagers()
    {
        MapSelectionManager[] allInstances = FindObjectsByType<MapSelectionManager>(FindObjectsSortMode.None);
        Debug.Log($"=== Found {allInstances.Length} MapSelectionManager instances ===");
        
        if (allInstances.Length == 0)
        {
            Debug.LogWarning("No MapSelectionManager instances found!");
            return;
        }
        
        if (allInstances.Length == 1)
        {
            Debug.Log("? Perfect! Only one MapSelectionManager instance found.");
            LogInstance(allInstances[0], 0);
            return;
        }
        
        Debug.LogError($"? PROBLEM: Found {allInstances.Length} MapSelectionManager instances! There should only be 1.");
        Debug.Log("This causes multiple random map selections and timer issues.");
        
        for (int i = 0; i < allInstances.Length; i++)
        {
            LogInstance(allInstances[i], i);
        }
        
        Debug.Log("=== SOLUTION: Use 'Quick Fix - Stop Duplicates & Restart Timer' to fix this ===");
    }
    
    void LogInstance(MapSelectionManager instance, int index)
    {
        bool isSingleton = MapSelectionManager.Instance == instance;
        bool isActive = instance.gameObject.activeInHierarchy;
        
        string status = isSingleton ? "? SINGLETON" : "? DUPLICATE";
        
        Debug.Log($"{status} Instance {index}: {instance.gameObject.name}");
        Debug.Log($"  - Instance ID: {instance.GetInstanceID()}");
        Debug.Log($"  - Scene: {instance.gameObject.scene.name}");
        Debug.Log($"  - Active: {isActive}");
        Debug.Log($"  - Position: {instance.transform.position}");
        Debug.Log($"  - Parent: {(instance.transform.parent != null ? instance.transform.parent.name : "None")}");
        
        // Check if it's initialized and active
        try
        {
            // Use reflection to check private fields safely
            var isInitializedField = typeof(MapSelectionManager).GetField("isInitialized", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isRandomSelectionActiveField = typeof(MapSelectionManager).GetField("isRandomSelectionActive", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isDestroyedField = typeof(MapSelectionManager).GetField("isDestroyed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hasStartedTimerField = typeof(MapSelectionManager).GetField("hasStartedTimer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (isInitializedField != null && isRandomSelectionActiveField != null && isDestroyedField != null)
            {
                bool isInitialized = (bool)isInitializedField.GetValue(instance);
                bool isRandomSelectionActive = (bool)isRandomSelectionActiveField.GetValue(instance);
                bool isDestroyed = (bool)isDestroyedField.GetValue(instance);
                
                Debug.Log($"  - Initialized: {isInitialized}");
                Debug.Log($"  - Random Selection Active: {isRandomSelectionActive}");
                Debug.Log($"  - Is Destroyed: {isDestroyed}");
                
                if (hasStartedTimerField != null)
                {
                    bool hasStartedTimer = (bool)hasStartedTimerField.GetValue(instance);
                    Debug.Log($"  - Has Started Timer: {hasStartedTimer}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"  - Could not check internal state: {e.Message}");
        }
        
        if (!isSingleton)
        {
            Debug.LogWarning($"  ??  DUPLICATE DETECTED: This instance should be removed!");
            Debug.LogWarning($"       GameObject path: {GetGameObjectPath(instance.transform)}");
        }
    }
    
    string GetGameObjectPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    [ContextMenu("Remove Duplicate MapSelectionManagers")]
    public void RemoveDuplicateMapSelectionManagers()
    {
        MapSelectionManager[] allInstances = FindObjectsByType<MapSelectionManager>(FindObjectsSortMode.None);
        
        if (allInstances.Length <= 1)
        {
            Debug.Log("? No duplicates found or only one instance exists.");
            return;
        }
        
        Debug.Log($"Found {allInstances.Length} instances. Removing duplicates...");
        
        MapSelectionManager singleton = MapSelectionManager.Instance;
        int removedCount = 0;
        
        // If no singleton is set, keep the first active one
        if (singleton == null)
        {
            Debug.LogWarning("No singleton instance set. Keeping the first active instance found.");
            for (int i = 0; i < allInstances.Length; i++)
            {
                if (allInstances[i].gameObject.activeInHierarchy)
                {
                    singleton = allInstances[i];
                    Debug.Log($"Setting {singleton.gameObject.name} as the singleton instance.");
                    break;
                }
            }
        }
        
        for (int i = 0; i < allInstances.Length; i++)
        {
            MapSelectionManager instance = allInstances[i];
            
            // Keep the singleton, remove others
            if (instance != singleton)
            {
                Debug.Log($"Removing duplicate: {instance.gameObject.name} (ID: {instance.GetInstanceID()})");
                Debug.Log($"  Path: {GetGameObjectPath(instance.transform)}");
                
                if (Application.isPlaying)
                {
                    Destroy(instance.gameObject);
                }
                else
                {
                    DestroyImmediate(instance.gameObject);
                }
                
                removedCount++;
            }
        }
        
        Debug.Log($"? Removed {removedCount} duplicate MapSelectionManager instances.");
        Debug.Log($"Singleton instance kept: {(singleton != null ? singleton.gameObject.name : "None")}");
    }
    
    [ContextMenu("Force Stop All Random Selections")]
    public void ForceStopAllRandomSelections()
    {
        Debug.Log("=== Forcing Stop of All Random Selections ===");
        
        MapSelectionManager[] allInstances = FindObjectsByType<MapSelectionManager>(FindObjectsSortMode.None);
        
        for (int i = 0; i < allInstances.Length; i++)
        {
            MapSelectionManager instance = allInstances[i];
            Debug.Log($"Stopping random selection on instance {i}: {instance.gameObject.name}");
            
            try
            {
                // Use reflection to force stop the random selection
                var isRandomSelectionActiveField = typeof(MapSelectionManager).GetField("isRandomSelectionActive", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hasCompletedSelectionField = typeof(MapSelectionManager).GetField("hasCompletedSelection", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hasStartedTimerField = typeof(MapSelectionManager).GetField("hasStartedTimer", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (isRandomSelectionActiveField != null)
                {
                    isRandomSelectionActiveField.SetValue(instance, false);
                    Debug.Log($"  - Disabled random selection for {instance.gameObject.name}");
                }
                
                if (hasCompletedSelectionField != null)
                {
                    hasCompletedSelectionField.SetValue(instance, true);
                    Debug.Log($"  - Marked selection as completed for {instance.gameObject.name}");
                }
                
                if (hasStartedTimerField != null)
                {
                    hasStartedTimerField.SetValue(instance, false);
                    Debug.Log($"  - Reset timer flag for {instance.gameObject.name}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"  - Could not stop random selection on {instance.gameObject.name}: {e.Message}");
            }
        }
        
        Debug.Log("? All random selections have been stopped.");
    }
    
    [ContextMenu("Check Timer Status")]
    public void CheckTimerStatus()
    {
        Debug.Log("=== Checking Timer Status ===");
        
        MapSelectionManager singleton = MapSelectionManager.Instance;
        if (singleton == null)
        {
            Debug.LogError("? No MapSelectionManager singleton found!");
            return;
        }
        
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"Time.deltaTime: {Time.deltaTime:F6}");
        Debug.Log($"Application.isPlaying: {Application.isPlaying}");
        
        // Force the singleton to debug its current state
        singleton.SendMessage("DebugState", SendMessageOptions.DontRequireReceiver);
    }
    
    [ContextMenu("Force Timer Restart")]
    public void ForceTimerRestart()
    {
        Debug.Log("=== Force Timer Restart ===");
        
        MapSelectionManager singleton = MapSelectionManager.Instance;
        if (singleton != null)
        {
            Debug.Log("Forcing timer restart...");
            singleton.SendMessage("ForceRandomSelection", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogError("No singleton found!");
        }
    }
    
    [ContextMenu("Check Scene Loading Issues")]
    public void CheckSceneLoadingIssues()
    {
        Debug.Log("=== Checking Scene Loading Issues ===");
        
        MapSelectionManager manager = MapSelectionManager.Instance;
        if (manager == null)
        {
            Debug.LogError("? No MapSelectionManager singleton found!");
            return;
        }
        
        // Check if maps are assigned
        if (manager.availableMaps == null || manager.availableMaps.Length == 0)
        {
            Debug.LogWarning("??  No maps assigned - will use test maps");
        }
        else
        {
            Debug.Log($"? Found {manager.availableMaps.Length} assigned maps:");
            for (int i = 0; i < manager.availableMaps.Length; i++)
            {
                if (manager.availableMaps[i] != null)
                {
                    string sceneName = manager.availableMaps[i].GetSceneName();
                    Debug.Log($"  Map {i}: '{manager.availableMaps[i].GetDisplayName()}' -> Scene: '{sceneName}'");
                    
                    if (string.IsNullOrEmpty(sceneName))
                    {
                        Debug.LogWarning($"    ??  Map {i} has no scene name assigned!");
                    }
                }
                else
                {
                    Debug.LogWarning($"    ??  Map {i} is null!");
                }
            }
        }
        
        // Check fallback scene names
        Debug.Log($"Fallback scene names: [{string.Join(", ", manager.mapSceneNames)}]");
        Debug.Log($"Final fallback: '{manager.gameplaySceneName}'");
    }
    
    [ContextMenu("Force Clean Restart")]
    public void ForceCleanRestart()
    {
        Debug.Log("=== Force Clean Restart ===");
        
        // First stop all random selections
        ForceStopAllRandomSelections();
        
        // Then remove all duplicates
        RemoveDuplicateMapSelectionManagers();
        
        // Wait a frame, then restart the singleton
        if (Application.isPlaying)
        {
            StartCoroutine(RestartAfterFrame());
        }
    }
    
    System.Collections.IEnumerator RestartAfterFrame()
    {
        yield return null; // Wait one frame
        
        MapSelectionManager singleton = MapSelectionManager.Instance;
        if (singleton != null)
        {
            Debug.Log("Forcing clean restart of MapSelectionManager...");
            singleton.SendMessage("ForceRandomSelection", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogError("No singleton found after cleanup!");
        }
    }
    
    [ContextMenu("Destroy All MapSelectionManagers")]
    public void DestroyAllMapSelectionManagers()
    {
        Debug.LogWarning("=== DESTROYING ALL MapSelectionManager instances ===");
        Debug.LogWarning("Use this only if you want to completely reset and manually add a new one.");
        
        MapSelectionManager[] allInstances = FindObjectsByType<MapSelectionManager>(FindObjectsSortMode.None);
        
        for (int i = 0; i < allInstances.Length; i++)
        {
            MapSelectionManager instance = allInstances[i];
            Debug.Log($"Destroying: {instance.gameObject.name} (ID: {instance.GetInstanceID()})");
            
            if (Application.isPlaying)
            {
                Destroy(instance.gameObject);
            }
            else
            {
                DestroyImmediate(instance.gameObject);
            }
        }
        
        Debug.Log($"Destroyed all {allInstances.Length} MapSelectionManager instances.");
        Debug.Log("You can now manually add a single MapSelectionManager to your scene.");
    }
    
    [ContextMenu("Emergency Fix - Stop Duplicates Now")]
    public void EmergencyFixStopDuplicatesNow()
    {
        Debug.LogWarning("=== EMERGENCY FIX: STOPPING ALL DUPLICATE INSTANCES ===");
        
        // Force stop all random selections immediately
        ForceStopAllRandomSelections();
        
        // Remove duplicates immediately
        RemoveDuplicateMapSelectionManagers();
        
        // Check if we have a working singleton
        MapSelectionManager singleton = MapSelectionManager.Instance;
        if (singleton != null)
        {
            Debug.Log("? Emergency fix complete. Singleton instance is ready.");
            Debug.Log($"Singleton: {singleton.gameObject.name} (ID: {singleton.GetInstanceID()})");
        }
        else
        {
            Debug.LogError("? Emergency fix failed - no singleton instance available!");
        }
    }
    
    [ContextMenu("?? Diagnose Timer Issue")]
    public void DiagnoseTimerIssue()
    {
        Debug.Log("?? === DIAGNOSING TIMER ISSUE ===");
        
        // Check multiple instances
        FindAllMapSelectionManagers();
        
        // Check timer status
        CheckTimerStatus();
        
        // Check Unity time settings
        Debug.Log($"?? Unity Time Settings:");
        Debug.Log($"  - Time.timeScale: {Time.timeScale}");
        Debug.Log($"  - Time.deltaTime: {Time.deltaTime:F6}");
        Debug.Log($"  - Time.fixedDeltaTime: {Time.fixedDeltaTime:F6}");
        Debug.Log($"  - Time.unscaledDeltaTime: {Time.unscaledDeltaTime:F6}");
        Debug.Log($"  - Application.targetFrameRate: {Application.targetFrameRate}");
        
        MapSelectionManager singleton = MapSelectionManager.Instance;
        if (singleton != null)
        {
            Debug.Log("?? RECOMMENDATION:");
            Debug.Log("  1. Use 'Quick Fix - Stop Duplicates & Restart Timer' to fix the issue");
            Debug.Log("  2. If timer is still stuck, check if Time.timeScale is 0");
            Debug.Log("  3. Press Space or Enter to skip the timer manually");
        }
        else
        {
            Debug.LogError("? No singleton found - use 'Quick Fix' first!");
        }
    }
}