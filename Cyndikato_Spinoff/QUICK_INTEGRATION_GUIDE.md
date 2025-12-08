# Quick Integration Guide - Map Voting System

## 🚀 5-Minute Setup

### Step 1: Choose Your Manager
Pick one of these options:

**Option A: MapSelectionController** (Recommended for beginners)
- Simpler setup
- Configure maps directly in Inspector
- No ScriptableObjects needed

**Option B: MapVotingManager** (Advanced)
- Uses MapData ScriptableObjects
- More flexible and data-driven
- Better for many maps

### Step 2: Scene Setup (MapSelectionController)

1. **Open your MapSelectionScene in Unity**

2. **Create/Find these UI elements:**
   - 4 UI Images for map previews (arrange in 2x2 grid)
   - 4 child Image objects for voting overlays (one per map)
   - 4 TextMeshPro texts for vote counters
   - 1 TextMeshPro for timer
   - 1 TextMeshPro for instructions
   - 1 TextMeshPro for winner display

3. **Add MapSelectionController:**
   - Create new GameObject (name it "MapVotingSystem")
   - Add Component → MapSelectionController
   - Configure in Inspector:

4. **Configure Each Map (0-3):**
   ```
   Map Name: "Forest Arena"
   Scene Name: "Map1"  (must match Build Settings)
   Map Image: (drag UI Image here)
   Voting Overlay: (leave empty or assign overlay GameObject)
   Vote Count Text: (drag TextMeshPro here)
   Visual Indicator: (see next step)
   ```

5. **Add MapVisualIndicator to each overlay:**
   - Select overlay Image GameObject
   - Add Component → MapVisualIndicator
   - Gradient Overlay: (assign the Image component)
   - Blink Speed: 0.5
   - Keep defaults for other settings

6. **Assign UI References:**
   ```
   Timer Text: (drag timer TextMeshPro)
   Instruction Text: (drag instructions TextMeshPro)
   Winner Text: (drag winner TextMeshPro)
   Main Canvas: (drag Canvas GameObject)
   ```

7. **Configure Settings:**
   ```
   Voting Duration: 15 (seconds)
   Skip Voting If Single Player: false (or true if desired)
   Zoom Scale: 1.5
   Zoom Duration: 1.0
   Grid Columns: 2
   Grid Rows: 2
   ```

### Step 3: Verify Scene Names

Make sure these scenes are in Build Settings:
- CharacterSelect (or your character select scene name)
- MapSelectionScene
- Map1, Map2, Map3, Map4 (your actual map scenes)

**File → Build Settings → Add Open Scenes**

### Step 4: Test Flow

1. **Play CharacterSelect scene**
2. **Select characters and proceed**
3. **MapSelectionScene should load with:**
   - Timer counting down
   - All players can navigate (WASD/D-Pad)
   - Lock votes (Enter/A Button)
   - Visual indicators show player colors
   - Winning map loads after voting

### Common Issues & Quick Fixes

**❌ "No player data found"**
- Make sure GameDataManager exists in CharacterSelect scene
- Verify NewCharacterSelectManager saves player data
- Check Console for "Character selections saved" message

**❌ "Player can't navigate"**
- Check that inputDeviceName is set correctly (Keyboard, Controller1, etc.)
- Verify player.isJoined is true
- Look for navigation input logs in Console

**❌ "Visual indicators not showing"**
- Verify MapVisualIndicator component is attached to overlay
- Check that overlay Image is assigned in Inspector
- Make sure overlay GameObject is initially inactive

**❌ "Map scene won't load"**
- Verify scene name spelling matches exactly
- Check scene is in Build Settings
- Look for "No scene name" error in Console

### Input Controls Reference

**Player 1 (Keyboard - Red):**
- Navigate: WASD or Arrow Keys
- Lock Vote: Enter or Space
- Unlock Vote: Escape

**Players 2-4 (Controllers - Blue/Green/Yellow):**
- Navigate: D-Pad (buttons 11-14)
- Lock Vote: A Button (button 0)
- Unlock Vote: B Button (button 1)

### Checklist Before First Run

- [ ] MapSelectionController added to scene
- [ ] All 4 maps configured with names and scene names
- [ ] All UI references assigned (images, texts)
- [ ] MapVisualIndicator on each overlay
- [ ] All map scenes in Build Settings
- [ ] CharacterSelect scene saves player data
- [ ] GameDataManager exists and persists

## 📚 Need More Help?

- **Detailed Setup:** See MAP_VOTING_SYSTEM_SETUP.md
- **Implementation Details:** See MAP_VOTING_SYSTEM_IMPLEMENTATION_SUMMARY.md
- **Inline Help:** Hover over fields in Unity Inspector

## 🎮 Testing Tips

1. **Single Player Test:**
   - Start with just keyboard
   - Verify basic voting works
   - Check scene transition

2. **Multi-Player Test:**
   - Connect controllers before starting
   - Test simultaneous voting
   - Verify color indicators match

3. **Edge Cases:**
   - Test with no votes (should pick random)
   - Test with tied votes (should pick random from tied)
   - Test voting ending early (all players vote)
   - Test changing votes before locking

## 🔧 Advanced Customization

**Change voting time:**
```csharp
Voting Duration: 20  // Change from 15 to 20 seconds
```

**Skip vote for single player:**
```csharp
Skip Voting If Single Player: true
```

**Adjust zoom effect:**
```csharp
Zoom Scale: 2.0  // More dramatic zoom
Zoom Duration: 1.5  // Slower zoom
```

**Change grid layout:**
```csharp
Grid Columns: 3
Grid Rows: 2
// Now supports 6 maps instead of 4
```

## ✅ Success Indicators

You'll know it's working when you see:
- ✅ Timer counting down from 15 (or your duration)
- ✅ Colored overlays on maps when players vote
- ✅ Vote counters showing numbers
- ✅ Instruction text updating with vote count
- ✅ Winning map zooming and scene loading

## 🆘 Still Having Issues?

Check Unity Console for debug messages:
- "MapSelectionController: Loaded X players"
- "Player X locked vote for map Y"
- "MapSelectionController: Winner is Z"

Enable additional debugging:
- Check "Enable Debug Logging" on components
- Add Debug.Log statements to track flow
- Use Unity Profiler to check performance

---

**Ready to go? Run CharacterSelect scene and test the complete flow!** 🎉
