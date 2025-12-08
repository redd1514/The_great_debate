# Character Selection Not Working - Diagnostic Guide

## Immediate Testing Steps (5 minutes)

### Step 1: Add Debug Helper
1. Select your `CharacterGridUI` GameObject in the Hierarchy
2. Click "Add Component" ? Search for "CharacterGridDebugHelper"
3. Add the script
4. Press Play

### Step 2: Test Basic Functionality
**In Play Mode:**
- Press **T** key ? Should show navigation logs and move indicator
- Press **Y** key ? Should force show a red indicator
- Press **Arrow Keys** ? Should show "ARROW PRESSED" logs
- Press **F1** ? Toggle debug panel on/off

### Step 3: Run Diagnostics
- Click the "Run Diagnostics Now" button in the debug panel
- Check the Console for detailed output

---

## Most Common Issues & Fixes

### Issue 1: No Selection Indicator Visible ?

**Symptoms:**
- Characters load but no colored overlay appears
- Navigation seems to work (logs appear) but no visual feedback

**Quick Test:**
Press **Y** key in play mode

**? Red square appears:** Canvas works, problem is positioning
**? Nothing appears:** Canvas/Image component issue

**Fix for Red Square Appears:**
```csharp
// The issue is likely anchor mismatch
// Check if your indicators have different anchors than icons
```

**Fix for Nothing Appears:**
1. Check Scene has Canvas component (Screen Space - Overlay)
2. Check Scene has EventSystem
3. Verify characterGridParent is assigned in inspector

### Issue 2: Navigation Not Working ?

**Symptoms:**
- No logs when pressing arrow keys
- Indicator doesn't move
- Manual test (T key) works but normal navigation doesn't

**Quick Test:**
Press **Arrow Keys** and watch console

**? See "ARROW PRESSED" logs:** Input detected, problem in NewCharacterSelectManager
**? No logs:** Unity Input Manager or focus issue

**Fix for Input Detected:**
- Check NewCharacterSelectManager ? characterGrid reference is assigned
- Verify Player 1 has joined (should see "Player 1 joined with Keyboard" log)

**Fix for No Logs:**
1. Click on Game window to ensure focus
2. Try WASD instead of arrow keys
3. Check Input Manager settings

### Issue 3: Indicator in Wrong Position ?

**Symptoms:**
- Red indicator appears but not over character icons
- Indicator shows but in corner or wrong location

**Quick Fix:**
Run this context menu command:
1. Right-click CharacterGridUI in Hierarchy
2. Select "Force Realign All Indicators"

---

## Detailed Diagnostics

### Check 1: Component Setup
**Required Components:**
- CharacterGridUI (with references assigned)
- NewCharacterSelectManager (with characterGrid reference)
- Canvas in scene
- EventSystem in scene

**Inspector Checklist:**
```
CharacterGridUI:
? characterIconPrefab assigned
? characterGridParent assigned
? selectionIndicatorPrefab (optional)

NewCharacterSelectManager:
? characterGrid references CharacterGridUI
? availableCharacters has elements
? playerPlatforms assigned
```

### Check 2: Runtime State
**After pressing Play, console should show:**
```
CharacterGridUI: Setup validation passed
CharacterGridUI: Created X character icons
CharacterGridUI: Created Tekken-style player selection indicators
Player 1 joined with Keyboard
CharacterGridUI: Activated Player 1 indicator - should now be visible!
```

**? Missing any of these:** Component setup issue

### Check 3: Player Join Status
**Run diagnostics and look for:**
```
Player 1: Joined=True, Locked=False
```

**? Joined=False:** Player didn't auto-join
**Fix:** Check NewCharacterSelectManager.Start() method

## Advanced Troubleshooting

### Issue: Anchor Positioning Problems

The most common issue is anchor mismatch between character icons and indicators.

**Diagnosis:**
1. Press Play ? Pause
2. Hierarchy ? Find "Player1EnhancedTekkenIndicator"
3. Inspector ? RectTransform ? Anchors section

**Should show:**
```
Min: X: 0, Y: 1
Max: X: 0, Y: 1
```

**? Shows different values (like 0.5, 0.5):**
- Stop Play mode
- Build the project (File ? Build Settings ? Build)
- Or restart Unity and try again

### Issue: GameDataManager Conflicts

If you recently added GameDataManager to map selection scene:

**Quick Fix:**
```csharp
// In NewCharacterSelectManager.Start(), add this validation:
if (players == null)
{
    Debug.LogWarning("Players array corrupted, re-initializing...");
    InitializePlayers();
}

// Reset device mappings
deviceToPlayerMap.Clear();
usedDevices.Clear();
```

## Quick Resolution Checklist

### 5-Minute Fix Attempt:
1. ? Add CharacterGridDebugHelper component
2. ? Press Play ? Press Y ? Red square appears?
3. ? Press T ? Navigation logs appear?
4. ? Press Arrow Keys ? Input logs appear?
5. ? Run Diagnostics ? All components found?

**If all ?:** Minor positioning issue, run "Force Realign All Indicators"
**If any ?:** Follow specific fix for that step

### Nuclear Option (If Nothing Works):
1. Stop Play mode
2. Right-click CharacterGridUI ? "Reset"
3. Reassign all Inspector references
4. Add CharacterGridDebugHelper
5. Press Play and test again

## Expected Working Behavior

When properly working:
1. **Scene Start:** Red gradient overlay appears on first character
2. **Arrow Keys:** Overlay moves smoothly to different characters
3. **Visual Style:** Gradient fades from solid red (bottom) to transparent (top)
4. **Animation:** Gentle blinking/pulsing effect
5. **Responsive:** Immediate visual feedback on navigation

## Support Information

If still not working, provide these details:
1. **Console Output:** First 20 lines after pressing Play
2. **Diagnostic Results:** Copy/paste from "Run Diagnostics Now"
3. **Test Results:**
   - Y key: Red square appears? (Yes/No)
   - T key: Navigation logs? (Yes/No)
   - Arrow keys: Input detected? (Yes/No)
4. **Screenshots:** Unity Inspector showing CharacterGridUI settings

---

**Most issues are fixed by:**
1. Adding the debug helper ?
2. Running diagnostics to identify the specific problem
3. Following the targeted fix for that issue