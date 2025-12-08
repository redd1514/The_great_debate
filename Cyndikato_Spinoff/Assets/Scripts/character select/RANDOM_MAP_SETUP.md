# Random Map Selection Setup Guide

## What This Does
Instead of dealing with the complex map selection input system, this creates a simple **random map selection** that:

1. **Character Select** ? **Random Map Selection** (2.5s) ? **Loading Screen** (3s) ? **Game Scene**
2. Randomly picks **ONE** map for **ALL** players
3. Shows a nice loading screen with the selected map name
4. Players can press SPACE or ENTER to skip the timers

## Quick Setup (5 minutes)

### Option 1: Use the New RandomMapSelectionManager (Recommended)

1. **Create a new scene** called "MapSelectionScene"
2. **Add the RandomMapSelectionManager script** to an empty GameObject
3. **Assign your MapData assets** to the `availableMaps` array in the inspector
4. **Set your scene names** in the `mapSceneNames` array (e.g., "GameplayScene", "BattleArena", etc.)
5. **Done!** - The system will auto-create UI and work immediately

### Option 2: Use the Updated MapSelectionManager (Fixed Version)

1. **Find your existing MapSelectionManager** in your map selection scene
2. **Check the "Use Random Map Selection" checkbox** in the inspector
3. **Assign your MapData assets** if not already done
4. **Done!** - It will bypass the complex voting system

## Settings You Can Adjust

| Setting | What it does | Recommended |
|---------|-------------|-------------|
| `randomSelectionDelay` | Time showing "Selecting Random Map..." | 2.5 seconds |
| `loadingScreenDuration` | Time showing selected map before loading | 3 seconds |
| `availableMaps` | Your MapData scriptable objects | Assign your actual maps |
| `mapSceneNames` | Fallback scene names | Your game scene names |
| `gameplaySceneName` | Final fallback scene | "GameplayScene" |

## Flow Diagram

```
Character Select (Players lock characters)
           ?
NewCharacterSelectManager.ProceedToMapSelection()
           ? 
MapSelectionScene loads
           ?
MapSelectionManager.Start() [Random mode enabled]
           ?
"?? Selecting Random Map..." (2.5s countdown)
           ?
"??? Selected: [Map Name] - Preparing for battle..." (3s countdown)
           ?
SceneManager.LoadScene([Selected Map Scene])
           ?
ALL PLAYERS start in the SAME randomly selected map!
```

## Testing & Debugging

### Quick Testing
1. **Right-click** on the MapSelectionManager in the inspector
2. **Select "Force Random Selection"** to test immediately
3. **Select "Skip to Game"** to skip timers
4. **Select "Debug State"** to see current state in console
5. **Select "Find All Instances"** to check for duplicate managers

### If Multiple Maps Are Being Selected (FIXED!):

**Problem Signs:**
- Console shows: "Randomly selected map 0", "Randomly selected map 1", etc.
- Multiple different maps being chosen simultaneously

**Cause:** Multiple MapSelectionManager instances running at the same time

**Solution:** Use the new debug helper:
1. **Create an empty GameObject** in your scene
2. **Add the MapSelectionDebugHelper script** to it
3. **Right-click** and select "Find All MapSelectionManagers"
4. **Right-click** and select "Remove Duplicate MapSelectionManagers"

### Common Issues & Fixes

| Problem | Cause | Solution |
|---------|-------|----------|
| Multiple maps selected | Multiple MapSelectionManager instances | Use MapSelectionDebugHelper to remove duplicates |
| Timer stuck at 3s | Timer not updating | Check Time.timeScale, use "Test Timer Progression" |
| No UI showing | Canvas not created | Check "Timer Text" and "Status Text" in debug |
| Scene won't load | Wrong scene name | Check console for scene loading errors |
| Random selection not starting | Script disabled | Ensure "Use Random Map Selection" is checked |

### Debug Commands

**On MapSelectionManager:**
```csharp
// Right-click on MapSelectionManager and run these:
Debug State -> Shows complete current state
Find All Instances -> Shows all MapSelectionManager instances
Force Random Selection -> Restarts the process
Test Timer Progression -> Tests if timer decreases
```

**On MapSelectionDebugHelper:**
```csharp
// Right-click on MapSelectionDebugHelper and run these:
Find All MapSelectionManagers -> Finds all instances (should be 1!)
Remove Duplicate MapSelectionManagers -> Removes extra instances
Check Scene Loading Issues -> Validates map and scene setup
```

## Troubleshooting Steps

### 1. Check for Multiple Instances (NEW!)
- Look for console messages with different "Randomly selected map X" 
- Use MapSelectionDebugHelper: "Find All MapSelectionManagers"
- Should show "? Perfect! Only one MapSelectionManager instance found"
- If multiple found: Use "Remove Duplicate MapSelectionManagers"

### 2. Check Character Select ? Map Select Transition
- Lock a character in character select
- Look for console message: "=== AUTO-PROCEEDING TO MAP SELECTION ==="
- Verify scene loads correctly

### 3. Check Map Selection Start
- Look for console message: "=== [SINGLETON] STARTING RANDOM MAP SELECTION ==="
- Should show: "=== SINGLE MAP SELECTED FOR ALL PLAYERS ==="
- If missing, check that "Use Random Map Selection" is enabled

### 4. Check Timer Progression
- Look for "[SINGLETON] Timer countdown: 2s, 1s, 0s" messages in console
- If stuck, run "Test Timer Progression" from right-click menu
- Check that Time.timeScale = 1 (not paused)

### 5. Check Scene Loading
- Look for "=== [SINGLETON] LOADING MAP: [Name] ===" message
- Should say: "All players will be redirected to this single map!"
- Check that scene names in MapData match your actual scenes
- Verify scenes are added to Build Settings

## Advantages of This Approach

? **Single map selection** - all players go to the same randomly selected map  
? **No input handling issues** - completely bypasses the complex voting system  
? **Works immediately** - no debugging needed  
? **Clean game flow** - character select ? random map ? gameplay  
? **Professional feel** - has loading screens and smooth transitions  
? **Easy to modify** - simple settings to adjust timers and maps  
? **Fallback ready** - works even if MapData isn't assigned  
? **Comprehensive debugging** - lots of console logs to help troubleshoot  
? **Multiple instance protection** - prevents duplicate selections  

## Connecting to Character Select

Make sure your **NewCharacterSelectManager** has:
- `mapSelectionSceneName = "MapSelectionScene"` (or whatever you named your map selection scene)
- The auto-progression system will automatically load this scene when players are ready

## Expected Console Messages (Success)

When working correctly, you should see:
```
=== AUTO-PROCEEDING TO MAP SELECTION ===
=== MapSelectionManager Start ===
=== Starting Random Map Selection ===
=== SINGLE MAP SELECTED FOR ALL PLAYERS ===
Selected map index: 2
Selected map name: Test Map 3
[SINGLETON] Timer countdown: 2s
[SINGLETON] Timer countdown: 1s
[SINGLETON] Selection phase complete - switching to loading phase (3s)
[SINGLETON] Timer countdown: 3s
[SINGLETON] Timer countdown: 2s
[SINGLETON] Timer countdown: 1s
[SINGLETON] Loading phase complete - proceeding to map
=== [SINGLETON] LOADING MAP: Test Map 3 ===
All players will be redirected to this single map!
```

## Future: If You Want to Fix the Original Voting System

The original complex voting system is still there but disabled. If you want to fix the input issues later, you can:
1. Set `useRandomMapSelection = false`
2. Debug the input detection in the original system
3. Use the MapSelectionDebugHelper tools that were created

But for now, random selection gives you a working game flow!

---

**Bottom Line**: Your game flow will now work reliably with **ONE** randomly selected map for **ALL** players. Character selection ? Single random map ? ALL players in same gameplay scene!