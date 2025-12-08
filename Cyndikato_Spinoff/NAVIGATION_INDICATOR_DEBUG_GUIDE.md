# Navigation & Indicator Troubleshooting Guide

## Current Status
- ? Build compiles successfully
- ?? Navigation not working
- ?? Selection indicator not showing

## Recent Fixes Applied

### 1. Anchor Alignment Fix
**Changed:** Indicator anchors from center (0.5, 0.5) to top-left (0, 1)
**Why:** Must match character icon anchors for proper positioning

### 2. Enhanced Debug Logging
Added detailed logging to:
- `Navigate()` - Shows input values, index changes, and navigation direction
- `UpdatePlayerSelectionDisplay()` - Shows positioning calculations and world/local coordinates
- All positioning operations now log before/after values

## Diagnostic Steps

### Step 1: Check if CharacterGridUI is Initialized
Run the scene and look for these logs in the Console:

? **Expected logs on scene start:**
```
CharacterGridUI: Created X character icons
CharacterGridUI: Created Tekken-style player selection indicators
CharacterGridUI: Setup validation passed.
NewCharacterSelectManager: Initialized Player 1 Tekken-style selection
```

? **If missing:** CharacterGridUI may not be properly assigned in the inspector

### Step 2: Verify Player Joins Successfully
When Player 1 auto-joins with keyboard:

? **Expected logs:**
```
Player 1 joined with Keyboard - Selection Color: RGBA(1.000, 0.000, 0.000, 1.000)
CharacterGridUI: Set Tekken-style gradient for Player 1 to RGBA(...)
CharacterGridUI: Initialized Player 1 Tekken-style selection
```

After 0.1 second delay:
```
CharacterGridUI: Completed delayed positioning for Player 1 after 0.1s delay
CharacterGridUI: Positioning Player 1 indicator over character 0
CharacterGridUI: Activated Player 1 indicator - should now be visible!
```

### Step 3: Test Keyboard Navigation
Press arrow keys or WASD:

? **Expected logs for each key press:**
```
CharacterGridUI.Navigate: Player 1, Input: (1, 0), Current Index: 0
  Navigating RIGHT: 0 -> 1
CharacterGridUI: Player 1 navigated from 0 to 1
CharacterGridUI: Positioning Player 1 indicator over character 1
```

? **If you see this:**
```
CharacterGridUI.Navigate: Player 1 navigation input (0, 0) resulted in no change
```
**Problem:** Input is not reaching Navigate() or input values are zero

? **If you see nothing:**
**Problem:** Navigate() is not being called at all

### Step 4: Check Indicator Creation
Look for these logs during initialization:

? **For each player (0-3):**
```
CharacterGridUI: Created Tekken-style indicator for Player X with matching top-left anchors to icons
PlayerSelectionIndicator: Setup complete - Tekken-style gradient indicator
PlayerSelectionIndicator: Created gradient texture for player color RGBA(...)
```

### Step 5: Verify Positioning Calculations
When indicator is positioned, check these logs:

? **Expected:**
```
CharacterGridUI: Positioning Player 1 indicator over character 0
  Icon Parent: CharacterGridParent, Indicator Parent: CharacterSelectCanvas
  Icon Local Position: (X, Y, Z)
  Icon World Position: (X, Y, Z)
  Set Indicator Local Position: (X, Y, Z)
  Final Indicator World Position: (X, Y, Z)
  Position difference (should be small): 0.001234
CharacterGridUI: Activated Player 1 indicator - should now be visible!
```

? **If position difference > 10:**
**Problem:** Coordinate transformation is incorrect

## Common Issues & Solutions

### Issue 1: Navigate() Never Called
**Symptoms:** No navigation logs appear when pressing keys
**Check:**
1. Is `NewCharacterSelectManager.Update()` being called?
2. Does `HandleAllInputDevices()` call `HandleDeviceInput(InputDevice.Keyboard)`?
3. Does `OnNavigate()` call `characterGrid.Navigate()`?

**Solution:** Check NewCharacterSelectManager input handling

### Issue 2: Navigation Input is (0, 0)
**Symptoms:** Logs show "Input: (0, 0)" when pressing keys
**Check:**
1. Is `GetNavigationInput()` returning correct values?
2. Are you using GetKeyDown vs GetKey?
3. Unity Input Manager configured correctly?

**Solution:** Verify `GetNavigationInput()` in NewCharacterSelectManager

### Issue 3: Indicator Created But Not Visible
**Symptoms:** Creation logs appear but no visual indicator
**Check in Unity Hierarchy:**
1. Is `Player1Indicator` GameObject present?
2. Is it enabled (checkbox ticked)?
3. Is the Image component present?
4. Is Canvas Renderer enabled?

**Check Inspector Values:**
- RectTransform anchors: Min(0, 1), Max(0, 1)
- RectTransform size: Should match icon size (e.g., 100x100)
- Image component: Has sprite/texture assigned?
- Image color: Not transparent (alpha > 0)?

**Solution:** Use "Test First Icon Positioning" context menu

### Issue 4: Indicator Behind Other UI
**Symptoms:** Indicator exists but covered by other elements
**Check:**
1. Transform hierarchy - is indicator before or after icons?
2. Canvas sorting order
3. Z-position of indicator vs icons

**Solution:** 
- Indicators should be created AFTER icons in hierarchy
- Use `SetAsLastSibling()` on indicator transform

### Issue 5: Parent Hierarchy Issues
**Symptoms:** Indicator at wrong position or world (0,0,0)
**Check:**
- Icon parent: Should be `characterGridParent`
- Indicator parent: Should be `characterGridParent.parent`
- Both should share a common Canvas ancestor

**Solution:** Verify in Unity Hierarchy view

## Manual Testing Commands

Use these context menu commands (Right-click CharacterGridUI in hierarchy):

### 1. "Debug All Icon Positions"
Shows detailed position data for every character icon
**Use when:** Verifying icon layout and anchors

### 2. "Test First Icon Positioning"
Tests positioning logic on first icon
**Use when:** Indicator not appearing at correct position

### 3. "Force Realign All Indicators"
Manually recalculates all indicator positions
**Use when:** Indicators drift or misalign

### 4. "Check Grid Layout Configuration"
Validates Grid Layout Group settings
**Use when:** Icons not laying out correctly

## Unity Inspector Checklist

### CharacterGridUI Component
- [ ] `characterIconPrefab` assigned
- [ ] `characterGridParent` assigned (the Transform holding icons)
- [ ] `selectionIndicatorPrefab` assigned or left empty (auto-creates)
- [ ] `charactersPerRow` = 4 (or your preferred value)

### NewCharacterSelectManager Component
- [ ] `characterGrid` references CharacterGridUI
- [ ] `availableCharacters` array has characters
- [ ] `playerSelectionColors` has 4 colors defined
  - Index 0: Red (Player 1)
  - Index 1: Blue (Player 2)
  - Index 2: Green (Player 3)
  - Index 3: Yellow (Player 4)

### Character Grid Parent
- [ ] Has GridLayoutGroup component (optional but recommended)
- [ ] If using GridLayoutGroup:
  - Cell Size matches your icon size
  - Spacing configured
  - Constraint: Fixed Column Count
  - Constraint Count: matches `charactersPerRow`

## Quick Fix Attempts

### Try 1: Force Indicator Visibility
Add this temporary code to `InitializePlayerSelection()`:
```csharp
// After SetActive(true)
playerSelectionIndicators[playerIndex].transform.SetAsLastSibling();
Image img = playerSelectionIndicators[playerIndex].GetComponent<Image>();
if (img != null)
{
    Debug.Log($"Indicator {playerIndex} - Color: {img.color}, Sprite: {img.sprite?.name ?? "null"}");
}
```

### Try 2: Test with Manual Positioning
Add to `Start()` method temporarily:
```csharp
StartCoroutine(TestIndicatorManually());
```

```csharp
IEnumerator TestIndicatorManually()
{
    yield return new WaitForSeconds(1f);
    
    if (playerSelectionIndicators != null && playerSelectionIndicators[0] != null)
    {
        playerSelectionIndicators[0].SetActive(true);
        RectTransform rect = playerSelectionIndicators[0].GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(100, -100);
        rect.sizeDelta = new Vector2(150, 150);
        
        Image img = playerSelectionIndicators[0].GetComponent<Image>();
        img.color = Color.red;
        
        Debug.Log("Manual test indicator created at (100, -100) with 150x150 size in RED");
    }
}
```

If you see the red square, positioning works but gradient texture might be the issue.
If you don't see anything, problem is in Canvas/UI hierarchy.

### Try 3: Simplify Navigation Test
Add to NewCharacterSelectManager.Update():
```csharp
// Temporary test code
if (Input.GetKeyDown(KeyCode.T))
{
    Debug.Log("TEST: Forcing navigation RIGHT for player 0");
    characterGrid.Navigate(Vector2.right, 0);
}
```

Press 'T' key to force navigation and see logs.

## Expected Visual Result

When working correctly, you should see:
1. **Character icons** arranged in a grid (4 per row)
2. **Red gradient overlay** on the first character (Player 1's selection)
3. Gradient fades from **solid red at bottom** to **transparent at top**
4. Gradient **blinks smoothly** (alpha oscillates)
5. Pressing arrow keys **moves the gradient** to different characters
6. Gradient **follows immediately** with smooth visual feedback

## Next Steps Based on Logs

### If you see: "Navigate() called with input (0,0)"
? Problem is in NewCharacterSelectManager.GetNavigationInput()

### If you see: No Navigate() calls at all
? Problem is in NewCharacterSelectManager input detection or player join

### If you see: Indicator created but "Position difference: 500+"
? Problem is coordinate system / parent hierarchy

### If you see: All logs correct but no visual
? Problem is Unity Canvas rendering / Image component

### If you see: Navigation works but indicator doesn't move
? Problem is in UpdatePlayerSelectionDisplay() not being called

## Contact Points for Further Help

Provide these details:
1. Copy/paste first 50 lines of Console logs
2. Screenshot of Unity Hierarchy showing:
   - CharacterGridUI and its children
   - Player1Indicator GameObject
3. Inspector screenshot of:
   - CharacterGridUI component
   - Player1Indicator RectTransform
   - Player1Indicator Image component
4. Which keys you pressed and what happened
