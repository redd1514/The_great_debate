# Map Voting System Setup Guide

## Overview
Complete map voting system with Tekken 8-style visual indicators, multi-player support, and input device persistence from character select.

## Components Created

### 1. Core Scripts
- **MapData.cs** - ScriptableObject for map configuration
- **MapVotingManager.cs** - Full-featured voting manager with all logic
- **MapSelectionController.cs** - Simplified controller for easier scene setup
- **MapVisualIndicator.cs** - Tekken 8-style gradient overlay animations
- **PlayerMapCursor.cs** - Individual player cursor indicators

### 2. Updated Scripts
- **PlayerCharacterData.cs** - Already has device/color persistence
- **GameDataManager.cs** - Already has map data storage

## Scene Setup Instructions

### Option A: Using MapSelectionController (Recommended for simplicity)

1. **Create Map Selection Scene**
   - Add a Canvas with UI elements
   - Create 4 UI Images for map previews in a 2x2 grid

2. **Add MapSelectionController Component**
   - Add to a GameObject in the scene
   - Configure in Inspector:

3. **Map Configuration**
   ```
   Map Options (Array of 4):
     Element 0-3:
       - Map Name: "Map 1", "Map 2", etc.
       - Scene Name: "Map1", "Map2", etc.
       - Map Image: Reference to UI Image
       - Vote Count Text: TextMeshPro text for vote count
       - Visual Indicator: MapVisualIndicator component (see below)
   ```

4. **UI Elements**
   ```
   - Timer Text: TextMeshPro for countdown
   - Instruction Text: TextMeshPro for player instructions
   - Winner Text: TextMeshPro for showing selected map
   - Main Canvas: Reference to main canvas
   ```

5. **Settings**
   ```
   - Voting Duration: 15 seconds (default)
   - Zoom Scale: 1.5 (default)
   - Zoom Duration: 1 second (default)
   ```

### Option B: Using MapVotingManager (Full featured)

Similar setup to MapSelectionController but with MapData ScriptableObjects.

1. **Create MapData Assets**
   - Right-click in Project → Create → Map Select → Map Data
   - Configure each map:
     - Map Name: Internal name
     - Map Display Name: Shown to players
     - Scene Name: Unity scene to load
     - Map Preview Image: Sprite for UI

2. **Setup MapVotingManager**
   - Add component to GameObject
   - Assign MapData assets to Map Options array
   - Configure UI references

### Visual Indicator Setup

For each map option:

1. **Create Voting Overlay**
   - Add child GameObject to map image
   - Add Image component (this will show the gradient)
   - Set to stretch to fill parent
   - Set initial alpha to 0

2. **Add MapVisualIndicator Component**
   - Attach to the overlay GameObject
   - Assign the Image as Gradient Overlay
   - Configure:
     - Blink Speed: 0.5 (default)
     - Gradient Height: 1.0 (default)

3. **Gradient Material (Optional)**
   - Create a material with gradient shader
   - Assign to the overlay Image for better effect

## Player Data Flow

### From Character Select
```
Character Select Scene:
  NewCharacterSelectManager
  ↓ (saves to GameDataManager)
  PlayerCharacterData[] with:
    - playerIndex
    - playerColor (Red/Blue/Green/Yellow)
    - inputDeviceName (Keyboard/Controller1-4)
    - inputDeviceId
    - lockedCharacter
```

### In Map Selection
```
Map Selection Scene:
  MapSelectionController/MapVotingManager
  ↓ (loads from GameDataManager)
  Uses PlayerCharacterData to:
    - Route input from correct devices
    - Show player-specific colors in indicators
    - Track votes per player
  ↓ (saves to GameDataManager)
  MapData of winning map
```

### To Game Scene
```
Game Scene:
  PlayerSpawner (or similar)
  ↓ (loads from GameDataManager)
  Gets:
    - PlayerCharacterData[] (characters, colors, devices)
    - MapData (already loaded in current scene)
```

## Input Mapping

The system automatically maps inputs based on PlayerCharacterData:

### Player 1 (Red) - Keyboard
- Navigate: WASD or Arrow Keys
- Select: Enter or Space
- Back: Escape

### Player 2-4 (Blue/Green/Yellow) - Controllers 1-3
- Navigate: D-Pad or Left Stick
- Select: A Button
- Back: B Button

## Visual Indicators

### Tekken 8-Style Gradient
Each map shows a blinking gradient overlay when voted for:
- **Single Vote**: Constant blink in player's color
- **Multiple Votes**: Cycles through all voting players' colors
- **Gradient Direction**: Bottom to top (higher opacity at bottom)
- **Animation**: Smooth alpha pulse creating blink effect

### Vote Counter
- Displays number of votes (1, 2, 3, 4)
- Hidden when no votes
- Updates in real-time

### Selection Cursors (Optional)
Use PlayerMapCursor for showing where each player is currently hovering:
- Colored border/highlight in player's color
- Pulse animation
- Only visible before vote is locked

## Voting Logic

1. **Timer**: 15 seconds (configurable)
2. **Navigation**: Players can move between 4 maps in 2x2 grid
3. **Vote Locking**: Press submit to lock vote
4. **Vote Changing**: Press cancel to unlock and change vote
5. **Early End**: If all players vote, voting ends immediately
6. **Tie Breaking**: Random selection among tied maps
7. **No Votes**: Random map selection
8. **Transition**: Winning map zooms and centers, then loads scene

## Testing Checklist

- [ ] Character select persists player count
- [ ] Player colors match between scenes (Red/Blue/Green/Yellow)
- [ ] Keyboard controls work for Player 1
- [ ] Controller inputs work for Players 2-4
- [ ] Visual indicators show correct player colors
- [ ] Vote counter updates correctly
- [ ] Timer counts down properly
- [ ] Timer changes color (white→yellow→red)
- [ ] All players can vote simultaneously
- [ ] Voting ends when all players vote
- [ ] Voting ends when timer reaches 0
- [ ] Tie-breaking works (random selection)
- [ ] Winning map zooms and centers
- [ ] Scene transitions with character data intact
- [ ] Multiple voting rounds work correctly

## Troubleshooting

### Players can't navigate
- Check that GameDataManager has player data
- Verify inputDeviceName matches expected values
- Check controller is connected and recognized

### Visual indicators not showing
- Verify MapVisualIndicator component is attached
- Check that Gradient Overlay Image is assigned
- Ensure overlay GameObject is active

### Wrong player colors
- Check PlayerCharacterData.playerColor values
- Verify colors match between character select and map select
- Default: Red, Blue, Green, Yellow

### Scene doesn't load
- Check MapData has correct sceneName
- Verify scene is in Build Settings
- Check scene name spelling

## Advanced Customization

### Change Grid Layout
In MapSelectionController:
```csharp
[SerializeField] private int gridColumns = 2;
[SerializeField] private int gridRows = 2;
```

### Adjust Voting Time
```csharp
[SerializeField] private float votingDuration = 15f;
```

### Skip Voting for Single Player
```csharp
[SerializeField] private bool skipVotingIfSinglePlayer = true;
```

### Custom Gradient Animation
Modify MapVisualIndicator.cs:
- Change `blinkSpeed` for faster/slower animation
- Adjust alpha values in `BlinkGradient()` coroutine
- Modify color cycling logic for multiple voters

## Architecture Notes

### Why Two Manager Options?

**MapSelectionController**: 
- Simpler setup, no ScriptableObjects needed
- Better for quick prototyping
- Less flexible for adding map metadata

**MapVotingManager**:
- Uses MapData ScriptableObjects
- More flexible and data-driven
- Better for larger projects with many maps
- Easier to add map properties

Both support the full feature set and are compatible with the same visual indicator system.
