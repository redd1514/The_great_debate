# Gameplay Scene Setup Guide

This guide explains how to set up the enhanced gameplay system that integrates character selection and map selection with your gameplay scene.

## Overview

The new system consists of three main components:
1. **Enhanced PlayerSpawner** - Spawns players based on character selections
2. **GameplaySceneManager** - Manages overall scene initialization
3. **Enhanced GameDataManager** - Persists selection data between scenes

## Setup Instructions

### 1. GameplaySceneManager Setup

1. Create an empty GameObject in your gameplay scene called "GameplaySceneManager"
2. Add the `GameplaySceneManager` script to it
3. Configure the script in Inspector:

**Scene Setup:**
- ? Auto Initialize On Start: true
- Initialization Delay: 0.5 (seconds)

**Map Setup:**
- Map Container: Create an empty GameObject called "MapContainer" and assign it here
- ? Use Selected Map Prefab: true (if your maps have prefabs)

**Player Setup:**
- Player Spawner: Assign your PlayerSpawner GameObject
- ? Auto Spawn Players: true

**UI Setup:**
- Countdown Manager: Assign your GameStartCountdown GameObject (if using countdown)
- ? Use Countdown: true (optional)

### 2. Enhanced PlayerSpawner Setup

1. Find your existing PlayerSpawner GameObject (or create one)
2. The PlayerSpawner script has been enhanced with new fields:

**Fallback Setup:**
- Default Player Prefab: Assign a generic player prefab (fallback when character selection unavailable)

**Spawn Configuration:**
- Max Players: 4
- Horizontal Spacing: 2.5
- Default Start Position: (0, 0)

**Map-Specific Spawn Points:**
- Map Spawn Points: Array of Transform objects for specific spawn locations
- ? Use Spawn Points: true (will use map spawn points if available)
- ? Assign Controls On Spawn: true

### 3. Character Prefab Setup

For each character in your CharacterSelectData:

1. **Create Character-Specific Prefabs:**
   - Duplicate your default player prefab
   - Rename it to match the character (e.g., "PlayerCharacter_Warrior")
   - Customize visuals (sprite, animator, etc.)
   - Assign the prefab to CharacterSelectData.characterPrefab

2. **Ensure PlayerController Compatibility:**
   - Each character prefab must have a PlayerController component
   - The PlayerController will be automatically configured with player number and input devices

### 4. Map Prefab Setup (Optional)

If you want map-specific environments:

1. **Create Map Prefabs:**
   - Create prefab with your map environment/level design
   - Include spawn points as child objects named "SpawnPoint1", "SpawnPoint2", etc.
   - Assign the prefab to MapData.mapPrefab

2. **Map Spawn Points:**
   - Create empty GameObjects as children of your map prefab
   - Name them: "SpawnPoint1", "SpawnPoint2", "SpawnPoint3", "SpawnPoint4"
   - Position them where players should spawn
   - The system will automatically detect and use these

### 5. GameDataManager Persistence

The GameDataManager automatically persists between scenes:
- Character selections from character select screen
- Map selection from map selection screen
- All data is available in the gameplay scene

## How It Works

### Flow Sequence:
1. **Character Select Scene:** Players join and select characters
2. **Map Selection Scene:** Players vote on/select a map  
3. **Gameplay Scene:** Enhanced system automatically:
   - Loads selected map prefab (if available)
   - Spawns players with their selected character prefabs
   - Positions players at map-specific spawn points
   - Assigns input devices consistently from character select
   - Applies character-specific stats/properties

### Character Selection Integration:
- Each player gets their chosen character's prefab
- Visual appearance matches selection (sprite, animations)
- Stats can be applied based on CharacterSelectData properties
- Input devices are consistently mapped from character select

### Map Integration:
- Selected map prefab is instantiated automatically
- Map-specific spawn points override default positioning
- Map settings (lighting, environment) can be applied
- Fallback to default environment if no map selected

## Testing the System

### In Character Select:
1. Join players and select different characters
2. Lock in selections and proceed to map selection
3. Select a map and proceed to gameplay

### In Gameplay:
1. Check console for initialization logs:
   ```
   [GameplaySceneManager] === INITIALIZING GAMEPLAY SCENE ===
   [PlayerSpawner] Spawning Player 1: CharacterName
   [GameplaySceneManager] Spawned 2 players from character selection
   ```

2. Verify players spawn with correct characters
3. Confirm map environment loads if using map prefabs

### Debug Tools:
- **GameDataManager:** Right-click ? "Log Current Game State"
- **PlayerSpawner:** Right-click ? "Debug Spawn Info"  
- **GameplaySceneManager:** Right-click ? "Debug Scene Info"

## Fallback Behavior

The system gracefully handles missing data:

**No Character Selection Data:**
- Falls back to spawning default player prefabs
- Uses basic gamepad + keyboard assignment

**No Map Selection:**
- Uses default scene environment
- Uses default/spaced spawn positions

**Missing Character Prefabs:**
- Falls back to default player prefab
- Logs warning about missing prefab

## Customization Examples

### Character Stats Integration:
In PlayerSpawner.ApplyCharacterStats(), you can customize:
```csharp
void ApplyCharacterStats(PlayerController controller, CharacterSelectData characterData)
{
    // Apply character-specific modifications
    if (characterData.speed > 5) 
        controller.moveSpeed *= 1.2f;
    
    if (characterData.attack > 10) 
        controller.lightAttackKnockback *= 1.1f;
    
    // Set character-specific properties
    // controller.maxHealth = characterData.health;
}
```

### Map-Specific Settings:
In GameplaySceneManager.ApplyMapSettings(), you can customize:
```csharp
void ApplyMapSettings()
{
    // Apply map-specific physics
    Physics2D.gravity = new Vector2(0, currentMap.customGravity);
    
    // Set map-specific audio
    AudioSettings.SetMapReverb(currentMap.reverbZone);
    
    // Apply visual effects
    if (currentMap.hasWeatherEffects)
        EnableWeatherSystem();
}
```

## Troubleshooting

**Players don't spawn:**
- Check GameDataManager.Instance exists
- Verify character selection data was saved
- Check PlayerSpawner has default prefab assigned

**Wrong characters spawn:**
- Verify CharacterSelectData.characterPrefab is assigned
- Check console for "No prefab available" messages

**Players spawn in wrong positions:**
- Check map prefab has properly named spawn points
- Verify PlayerSpawner.useSpawnPoints is enabled
- Check mapSpawnPoints array assignment

**Input doesn't work:**
- Verify device mappings were preserved from character select
- Check PlayerController input assignment code
- Test with fallback keyboard controls (Player 1)

## Scene Structure Example

```
GameplayScene
??? GameplaySceneManager (GameplaySceneManager script)
??? MapContainer (Empty GameObject)
??? PlayerSpawner (Enhanced PlayerSpawner script)
??? UI
?   ??? Canvas
?   ??? GameStartCountdown (GameStartCountdown script)
??? Audio
?   ??? GameplayAudioSource (AudioSource)
??? Environment
    ??? (Default environment objects)
```

The system will automatically instantiate the selected map prefab as a child of MapContainer and spawn players with their selected character prefabs!