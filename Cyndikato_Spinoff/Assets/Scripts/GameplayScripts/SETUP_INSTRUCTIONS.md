# GameplaySceneManager Setup Instructions

## Current Issue
The GameplaySceneManager script shows "None" references because the required GameObjects don't exist in your scene yet.

## Step-by-Step Setup

### 1. Create Map Container
1. In Hierarchy, right-click in empty space
2. Select "Create Empty"
3. Rename it to "MapContainer"
4. Position it at (0, 0, 0)
5. Drag this GameObject to the "Map Container" field in GameplaySceneManager Inspector

### 2. Set Up Player Spawner Reference
1. In your Hierarchy, you already have "PlayerSpawner" under GameManagers
2. Drag the "PlayerSpawner" GameObject to the "Player Spawner" field in GameplaySceneManager Inspector

### 3. Create Audio Source for Gameplay
1. In Hierarchy, right-click in empty space  
2. Select "Audio" ? "Audio Source"
3. Rename it to "GameplayAudioSource"
4. In the AudioSource component, uncheck "Play On Awake"
5. Drag this GameObject to the "Gameplay Audio Source" field in GameplaySceneManager Inspector

### 4. Set Up Countdown Manager (Optional)
Looking at your scene, I can see you have a "Countdown" GameObject under Canvas.

**Option A: Use Existing Countdown**
1. Click on your "Countdown" GameObject under Canvas
2. If it doesn't have GameStartCountdown script, add it:
   - Click "Add Component"
   - Type "GameStartCountdown"
   - Add the script
3. Drag the "Countdown" GameObject to the "Countdown Manager" field in GameplaySceneManager Inspector

**Option B: Create New Countdown**
1. Under Canvas, right-click
2. Select "UI" ? "Text - TextMeshPro"
3. Rename to "GameStartCountdown"
4. Add GameStartCountdown script component
5. Drag this to the "Countdown Manager" field

### 5. Final Hierarchy Structure
After setup, your scene should look like this:

```
Your Scene
??? Map1 (your existing map objects)
??? Main Camera
??? EventSystem
??? GameManagers
?   ??? CollisionManager
?   ??? PlayerSpawner (? assign this)
?   ??? GamePlayManager (GameplaySceneManager script)
??? MapContainer (? create and assign this)
??? GameplayAudioSource (? create and assign this)
??? Background
??? Platforms
??? KillZone
??? Canvas
?   ??? Countdown (? assign this if using countdown)
?   ??? Police, Doctor, etc.
??? (other existing objects)
```

## Field Assignment Checklist

Once you create the GameObjects, assign them in GameplaySceneManager Inspector:

### Scene Setup
- ? Auto Initialize On Start: true
- Initialization Delay: 0.5

### Map Setup  
- Map Container: Drag "MapContainer" GameObject here
- ? Use Selected Map Prefab: true

### Player Setup
- Player Spawner: Drag "PlayerSpawner" GameObject here  
- ? Auto Spawn Players: true

### UI Setup
- Countdown Manager: Drag "Countdown" GameObject here (optional)
- ? Use Countdown: true (if using countdown)

### Audio
- Gameplay Audio Source: Drag "GameplayAudioSource" GameObject here
- Map Loaded Sound: Assign audio clip (optional)
- Game Start Sound: Assign audio clip (optional)

### Debug
- ? Enable Debug Logs: true

## Testing

After setup:
1. Make sure GameDataManager exists in your scene (it should persist from character select)
2. Press Play
3. Check Console for logs like:
   ```
   [GameplaySceneManager] === INITIALIZING GAMEPLAY SCENE ===
   [GameplaySceneManager] Loading game data from GameDataManager...
   ```

If you see errors, the missing references are likely the cause.