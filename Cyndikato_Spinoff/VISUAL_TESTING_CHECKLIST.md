# VISUAL TESTING CHECKLIST

## ?? Goal
Get the red gradient indicator showing and responding to navigation

---

## ? QUICK TEST (2 minutes)

### Step 1: Setup (One Time Only)
```
1. Open Unity
2. Open character select scene
3. Find "CharacterGridUI" in Hierarchy
4. Add Component ? "CharacterGridDebugHelper"
5. Done! (Never need to do this again)
```

### Step 2: Test Run
```
1. Press Play ??
2. Press 'Y' key
```

### ? SUCCESS: You see a RED SQUARE appear on screen
**What this means:**
- Canvas rendering: ? Works
- Image component: ? Works
- Problem: Positioning or gradient texture

**Next:** Press 'T' key to test navigation

### ? FAIL: No red square appears
**What this means:**
- Canvas or Image component issue

**Fix:**
1. Check console for errors
2. Check Canvas exists in scene
3. Check EventSystem exists in scene

---

## ?? NAVIGATION TEST (1 minute)

### Test A: Forced Navigation
```
Press 'T' key
```

### ? SUCCESS: Console shows these logs:
```
!!! TEST KEY 'T' PRESSED - FORCING NAVIGATION RIGHT !!!
CharacterGridUI.Navigate: Player 1, Input: (1, 0)
  Navigating RIGHT: 0 -> 1
```

**What this means:**
- CharacterGridUI.Navigate(): ? Works
- Input system: ?? Needs checking

**Next:** Test normal arrow keys

### ? FAIL: No logs appear
**What this means:**
- CharacterGridUI or player join issue

**Check debug panel:**
```
Player 1:
  Joined: True or False?
```

---

## ?? KEYBOARD INPUT TEST (1 minute)

### Test B: Arrow Keys
```
Press RIGHT ARROW key
```

### ? SUCCESS: Console shows:
```
!!! RIGHT ARROW PRESSED !!!
```

**What this means:**
- Unity input detection: ? Works
- Problem: Input not reaching Navigate()

**Check:** NewCharacterSelectManager.HandleDeviceInput()

### ? FAIL: No "ARROW PRESSED" log
**What this means:**
- Unity not detecting keyboard
- Game window not focused

**Fix:**
1. Click game window
2. Try again

---

## ?? ANCHOR VERIFICATION (30 seconds)

```
1. Press Play ??
2. Press Pause ??
3. Hierarchy ? Find "Player1Indicator"
4. Look at Inspector ? RectTransform
5. Find "Anchors" section
```

### ? SUCCESS: Shows
```
Min: X: 0, Y: 1
Max: X: 0, Y: 1
```

**Perfect!** Anchors are correct.

### ? FAIL: Shows different values
```
Min: X: 0.5, Y: 0.5
Max: X: 0.5, Y: 0.5
```

**Fix needed:**
1. Stop play mode
2. File ? Build Settings ? Build
3. Or restart Unity
4. Test again

---

## ?? DIAGNOSTICS PANEL (2 minutes)

### Open Debug Panel
```
1. Press Play ??
2. Look top-left corner of Game view
3. You should see "CHARACTER GRID DEBUG HELPER" panel
4. If not, press F1 key to toggle
```

### Click "Run Diagnostics Now" Button

### ? SUCCESS: Console shows:
```
--- Character Grid Setup ---
Characters array: 8
Character icons: 8
Player indicators: 4
  Indicator 0: Player1Indicator
    Active: True
    Image Color: RGBA(1, 0, 0, 1)
```

### Read the Output

**Check 1: Characters array**
- ? Shows a number (e.g., "8")
- ? Shows "NULL"

**Check 2: Character icons**
- ? Shows same number as characters
- ? Shows "NULL" or "0"

**Check 3: Player indicators**
- ? Shows "4"
- ? Shows "NULL"

**Check 4: Indicator 0 Active**
- ? Shows "True"
- ? Shows "False"

**Check 5: Image Color**
- ? Shows "RGBA(1, 0, 0, 1)" or similar
- ? Shows "NULL"

---

## ?? VISUAL CHECKLIST

### What You Should See (Final Result)

```
When working correctly:

?? Character Grid
   ??? [ Icon 1 ] ? Red gradient overlay (Player 1)
   ??? [ Icon 2 ]
   ??? [ Icon 3 ]
   ??? [ Icon 4 ]

The red gradient:
   - Bottom: Solid red ????
   - Middle: Semi red ????
   - Top:    Faint red ????
   - Blinks smoothly (alpha oscillates)

Press RIGHT ARROW:
   ?? Character Grid
      ??? [ Icon 1 ]
      ??? [ Icon 2 ] ? Red moves here immediately
      ??? [ Icon 3 ]
      ??? [ Icon 4 ]
```

---

## ?? COMMON ISSUES QUICK REFERENCE

| Symptom | Cause | Fix |
|---------|-------|-----|
| No red square on 'Y' | Canvas issue | Check Canvas & EventSystem exist |
| Red square but no gradient | Texture issue | Check PlayerSelectionIndicator script |
| No logs on 'T' | Player not joined | Check debug panel "Player 1: Joined" |
| No logs on Arrow keys | Unity input | Click game window, try again |
| Anchors wrong (0.5, 0.5) | Build didn't apply | Rebuild or restart Unity |
| Gradient at wrong spot | Position calc issue | Right-click CharacterGridUI ? Force Realign |

---

## ?? TEST RESULT FORM

**Fill this out if asking for help:**

```
Date: __________

? Added CharacterGridDebugHelper: Yes / No
? Pressed Play: Yes / No

?? Test Results:
[ ] Pressed 'Y' - Red square appeared
[ ] Pressed 'T' - Navigation logs appeared  
[ ] Pressed Arrow - "ARROW PRESSED" log appeared
[ ] Anchors show (0, 1)
[ ] Debug panel shows Player 1 Joined: True

?? Console Output:
(Paste first 20 lines here)


??? Screenshots Attached:
[ ] Hierarchy showing Player1Indicator
[ ] Player1Indicator Inspector
[ ] Debug panel diagnostics
[ ] Game view showing (or not showing) indicator
```

---

## ?? SUCCESS INDICATORS

You'll know it's working when:

1. ? Press Play ? Red gradient appears immediately on first character
2. ? Gradient blinks smoothly (fade in/out)
3. ? Press RIGHT ARROW ? Gradient moves to next character
4. ? No errors in console
5. ? Debug panel shows all systems operational

---

## ?? PRO TIPS

**Tip 1:** Keep debug panel open (F1) while testing
**Tip 2:** Watch Console window simultaneously  
**Tip 3:** Use 'Y' test first to isolate Canvas issues
**Tip 4:** Use 'T' test to isolate input issues
**Tip 5:** Context menu tests work in play mode only

---

## ?? NEED HELP?

If tests fail, provide:
1. ? Completed test result form (above)
2. ? Console output (first 50 lines)
3. ? Screenshot of Player1Indicator Inspector
4. ? Result of pressing 'Y', 'T', and arrow keys

This helps diagnose the exact issue quickly!
