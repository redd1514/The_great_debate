# Map Voting System - Setup Guide

## Overview

This guide explains how to set up the new **Map Voting System** that allows 1-4 players to vote for their preferred map using their individual input devices (keyboard or controllers).

## Features

✅ **Multi-Player Voting**: Each player votes independently using their own input device  
✅ **Player-Specific Colors**: Red (P1), Blue (P2), Green (P3), Yellow (P4)  
✅ **Tekken 8-Style Visual Indicators**: Blinking gradient overlays show player selections  
✅ **Vote Counting**: Real-time vote counts displayed on each map  
✅ **Vote Changes**: Players can change their vote before timer expires  
✅ **Tie Handling**: Random selection among tied maps  
✅ **Countdown Timer**: Visual timer with color changes (white → yellow → red)  
✅ **Data Persistence**: Player data flows through all scenes  

## Quick Setup (Unity Editor)

### Step 1: Prepare Map Selection Scene

1. Open or create a scene called "MapSelectionScene"
2. Ensure it's added to **Build Settings** → **Scenes in Build**

### Step 2: Add MapVotingManager

1. **Create Empty GameObject**: In the scene hierarchy, create a new empty GameObject
2. **Name it**: "MapVotingManager"
3. **Add Component**: Add the `MapVotingManager` script

### Step 3: Configure MapVotingManager

In the Inspector, configure the following:

#### Map Data (Required)
- **Available Maps**: Assign 4 MapData ScriptableObjects
  - These should be your actual map data assets
  - Each should have a valid `mapIcon` sprite and `sceneName`

#### UI References (Required)
- **Map Grid Parent**: Transform where map icons will be created
  - Create an empty GameObject under your Canvas called "MapGridParent"
  - Add a Grid Layout Group component (2 columns x 2 rows recommended)
- **Map Icon Prefab**: (Optional) Prefab for map display
  - If not assigned, system creates basic icons automatically
- **Timer Text**: TextMeshProUGUI for countdown display
- **Status Text**: TextMeshProUGUI for messages ("Vote for your map!")
- **Selection Indicator Container**: (Optional) Parent for indicators

#### Vote Counter UI (Optional)
- **Vote Counter Prefab**: Prefab with TextMeshProUGUI for vote counts
  - If not assigned, system creates basic counters

#### Voting Settings
- **Voting Duration**: 15 seconds (default) - adjust as needed
- **Allow Vote Changes**: True (default) - players can change votes

#### Scene Settings
- **Gameplay Scene Name**: "GameplayScene" (fallback)
- **Map Scene Names**: Array of scene names for each map
  - Index 0 = Map 0's scene name
  - Index 1 = Map 1's scene name, etc.

### Step 4: Update MapSelectionManager (If Present)

If you have an existing `MapSelectionManager` in your scene:

1. **Select the GameObject** with MapSelectionManager
2. In Inspector, find **Map Selection Mode**
3. **Change to "Voting"**
4. **Assign Voting Manager**: Drag the MapVotingManager GameObject to the "Voting Manager" field

### Step 5: Configure Character Select Scene

In your Character Select scene:

1. Find the **NewCharacterSelectManager** GameObject
2. Verify **Map Selection Scene Name** is set to "MapSelectionScene"
3. Ensure **Enable Auto Progression** is checked (recommended)
4. Set **Auto Progress Delay** to 2 seconds (default)

## Scene Structure Example

```
MapSelectionScene
├── Canvas (Screen Space - Overlay)
│   ├── MapGridParent (Grid Layout Group)
│   │   └── (Map icons created at runtime)
│   ├── TimerText (TextMeshProUGUI)
│   ├── StatusText (TextMeshProUGUI)
│   └── SelectionIndicatorContainer (Optional)
├── MapVotingManager (GameObject with script)
└── EventSystem
```

## Input Controls

### Player 1 (Red) - Keyboard
- **Navigate**: WASD or Arrow Keys
- **Vote**: Enter or Space
- **Change Vote**: Navigate to another map and press Enter/Space again

### Player 2 (Blue) - Controller 1
- **Navigate**: D-Pad or Left Analog Stick
- **Vote**: A Button
- **Change Vote**: Navigate and press A again

### Player 3 (Green) - Controller 2
- Same as Player 2, using Controller 2

### Player 4 (Yellow) - Controller 3
- Same as Player 2, using Controller 3

## Game Flow

```
1. Character Select Screen
   └── Players join and select characters
   └── All players lock their selections
   └── Auto-progression to Map Selection

2. Map Voting Screen
   └── Each player navigates independently
   └── Players see their colored gradient on hovered map
   └── Players press vote button to lock vote
   └── Vote counters update in real-time
   └── Players can change votes before time expires
   └── Timer counts down (15 seconds default)

3. Results
   └── Map with most votes wins
   └── Ties broken randomly
   └── Brief display of winning map
   └── Load winning map's scene

4. Gameplay
   └── All players spawn in the same map
   └── Character and color data persists
```

## Visual Feedback

### Selection Indicators (Tekken 8 Style)
- **Blinking Gradient**: Animated from bottom (solid color) to top (transparent)
- **Player Colors**: Each player has unique color gradient
- **Hover State**: Indicator blinks when player hovers over map
- **Locked State**: Indicator stops blinking when vote is locked

### Vote Counters
- **Display**: Number showing total votes for each map
- **Hidden**: When vote count is 0
- **Visible**: When at least 1 vote

### Timer
- **White**: More than 10 seconds remaining
- **Yellow**: 5-10 seconds remaining
- **Red**: Less than 5 seconds remaining

## Troubleshooting

### Issue: No Input Detected

**Solution:**
1. Check that players joined in Character Select
2. Verify input device is properly connected
3. Check console for "Player X joined with [Device]" messages
4. Ensure GameDataManager has player data

### Issue: Visual Indicators Not Showing

**Solution:**
1. Check that MapSelectionIndicator components exist on map icons
2. Verify Selection Indicator Container is assigned (or leave null for auto-creation)
3. Check console for indicator creation messages
4. Ensure map icons have proper RectTransform setup

### Issue: Scene Won't Load After Voting

**Solution:**
1. Verify map scene names are correct in Inspector
2. Ensure target scenes are added to Build Settings
3. Check MapData assets have valid sceneName values
4. Look for scene loading errors in console

### Issue: Multiple Players Can't Navigate

**Solution:**
1. Verify each player has unique input device in Character Select
2. Check that deviceToPlayerMap is populated
3. Ensure no input device conflicts
4. Check debug logs for device assignments

## Debug Commands

Right-click on MapVotingManager in Inspector and select:

- **Debug Current State**: Shows complete voting state in console
  - Active players and their devices
  - Current votes per map
  - Individual player votes
  - Timer status

## Advanced Customization

### Custom Map Icon Prefab

Create a prefab with:
- Image component (for map sprite)
- Child object with TextMeshProUGUI (for vote counter)
- Proper RectTransform setup

Assign to **Map Icon Prefab** field.

### Custom Vote Counter Prefab

Create a prefab with:
- TextMeshProUGUI component
- Desired font, size, and styling
- RectTransform positioned at bottom of map icon

Assign to **Vote Counter Prefab** field.

### Adjust Visual Style

On MapSelectionIndicator components:
- **Gradient Style**: TekkenClassic, TekkenBright, TekkenGlow, Custom
- **Blink Speed**: How fast the gradient blinks
- **Min/Max Alpha**: Opacity range for blinking
- **Intensity Multiplier**: Color brightness multiplier

## Testing in Unity Editor

1. **Play Character Select Scene**
2. **Join Player 1** (automatic with keyboard)
3. **Join additional players** by pressing controller buttons
4. **Lock all characters**
5. **Wait for auto-progression** to Map Selection
6. **Test voting**:
   - Navigate with WASD/Arrow keys (P1)
   - Press Enter/Space to vote (P1)
   - Try changing vote by selecting another map
   - Test with controllers if available
7. **Wait for timer** or let it expire
8. **Verify scene transition** to winning map

## Integration with Existing Systems

### GameDataManager
- Automatically stores player data from Character Select
- MapVotingManager loads this data on Start
- Selected map is saved back to GameDataManager
- Data persists through scene transitions

### PlayerSpawner
- In gameplay scenes, PlayerSpawner should read from GameDataManager
- Use `GameDataManager.Instance.GetSelectedCharacters()`
- Spawn players with correct colors and input devices
- Map selection data available via `GameDataManager.Instance.GetSelectedMap()`

## Performance Notes

- Voting system is lightweight and runs at 60+ FPS
- Visual indicators use simple gradients (no complex shaders)
- Input polling only for active players
- Minimal memory allocation during voting

## Future Enhancements

Potential improvements you could add:

1. **Sound Effects**: Add audio feedback for votes and navigation
2. **Map Previews**: Show larger preview when hovering
3. **Vote History**: Display who voted for what map
4. **Random Option**: Add a 5th "Random" option
5. **Map Bans**: Allow players to ban maps first
6. **Best of 3**: Multiple rounds of voting

---

## Quick Reference

**Minimum Required Setup:**
1. MapVotingManager GameObject with script
2. 4 MapData assets assigned
3. UI: MapGridParent, TimerText, StatusText
4. Scenes added to Build Settings

**Player Colors:**
- Player 1: Red (Keyboard)
- Player 2: Blue (Controller 1)
- Player 3: Green (Controller 2)
- Player 4: Yellow (Controller 3)

**Default Timer:** 15 seconds

**Vote Changes:** Enabled by default

**Tie Resolution:** Random selection

**That's it!** The system is designed to work with minimal configuration.
