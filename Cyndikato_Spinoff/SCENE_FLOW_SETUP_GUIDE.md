# Scene Flow System Setup Guide

This guide explains how to set up the Scene Flow System in Unity Editor.

## Overview

The Scene Flow System provides:
- **SceneFlowManager**: Central singleton for managing scene transitions
- **LoadingScreenController**: Enhanced loading screen with progress bar
- **IntroSceneController**: Intro sequence management with skip functionality

## Setup Instructions

### 1. Create Loading Scene

1. In Unity Editor, go to **File > New Scene**
2. Choose **2D** or **Basic (Built-in)** template
3. Save the scene as `LoadingScene.unity` in `Assets/Scenes/`

#### Add Loading Screen UI

1. Create UI Canvas:
   - Right-click in Hierarchy > **UI > Canvas**
   - Set Canvas Scaler to "Scale With Screen Size"
   - Reference Resolution: 1920x1080

2. Add Background:
   - Right-click Canvas > **UI > Image**
   - Name it "Background"
   - Set color to dark (e.g., #1a1a1a)
   - Stretch to fill entire canvas

3. Add Progress Bar:
   - Right-click Canvas > **UI > Slider**
   - Name it "ProgressBar"
   - Position in lower center of screen
   - Adjust size (e.g., Width: 800, Height: 30)
   - Remove "Handle Slide Area" child (we don't need a draggable handle)
   - Style the Fill Area:
     - Set Fill color to a bright color (e.g., #00ff00 or #ffaa00)
     - Set Background color to dark gray (#333333)
   - Set Min Value: 0, Max Value: 1, Value: 0

4. Add Loading Text:
   - Right-click Canvas > **UI > Text - TextMeshPro**
   - Name it "LoadingText"
   - Position above progress bar
   - Set text: "Initializing..."
   - Set font size: 36
   - Set alignment: Center
   - Set color: White

5. Add Percentage Text:
   - Right-click Canvas > **UI > Text - TextMeshPro**
   - Name it "PercentageText"
   - Position below or inside progress bar
   - Set text: "0%"
   - Set font size: 24
   - Set alignment: Center
   - Set color: White

6. Add LoadingScreenController Component:
   - Create an empty GameObject in scene root (not under Canvas)
   - Name it "LoadingScreenController"
   - Add Component: **LoadingScreenController** script
   - Assign references:
     - Progress Bar: Drag the Slider component
     - Loading Text: Drag the LoadingText TextMeshProUGUI component
     - Percentage Text: Drag the PercentageText TextMeshProUGUI component
   - Configure settings:
     - Loading Phases: Keep default or customize
     - Progress Smooth Speed: 2 (default)
     - Phase Transition Time: 0.5 (default)

7. Save the scene

### 2. Configure Intro Scene

The `introScene.unity` already exists. Now configure it:

1. Open `Assets/Scenes/introScene.unity`

2. Add IntroSceneController:
   - Create an empty GameObject (or add to existing root object)
   - Name it "IntroSceneController"
   - Add Component: **IntroSceneController** script
   - Configure settings:
     - Intro Duration: 3 (seconds to show intro)
     - Allow Skip: ✓ (enabled)
     - Use Fade In: ✓ (optional)
     - Use Fade Out: ✓ (optional)
     - Fade In Duration: 1 (optional)
     - Fade Out Duration: 1 (optional)

3. If using fade effects:
   - Add a Canvas if not present
   - Add all intro UI elements under this Canvas
   - Add a **Canvas Group** component to the Canvas
   - Assign the Canvas Group to IntroSceneController's "Canvas Group" field

4. If using logo image:
   - Create UI Image for your logo
   - Assign to IntroSceneController's "Logo Image" field (optional)

5. Save the scene

### 3. Update Build Settings

1. Open **File > Build Settings**

2. Ensure scenes are in this order:
   1. `Assets/Scenes/introScene.unity`
   2. `Assets/Scenes/LoadingScene.unity` (newly created)
   3. `Assets/Scenes/Menu.unity`
   4. `Assets/Scenes/play/CharacterSelect.unity`
   5. Other scenes as needed...

3. Make sure all scenes have checkboxes ✓ enabled

4. Close Build Settings

### 4. Configure Scene Names

The SceneFlowManager uses these default scene names:
- Intro Scene: "introScene"
- Loading Scene: "LoadingScene"
- Main Menu: "Menu"
- Character Select: "CharacterSelect"

If your scene names differ, you have two options:

**Option A: Rename your scenes to match defaults** (recommended)

**Option B: Configure SceneFlowManager at runtime**
1. Create a GameObject in your first scene
2. Add SceneFlowManager component manually
3. Assign custom scene names in inspector

### 5. Test the Flow

1. Set `introScene.unity` as the active scene
2. Press Play in Unity Editor
3. Verify sequence:
   - Intro scene displays
   - Press Space/Enter to skip (optional)
   - Loading screen appears with progress bar
   - Main menu loads after loading completes

### 6. Optional: Add SceneFlowManager GameObject

While SceneFlowManager creates itself automatically, you can pre-create it:

1. In introScene, create empty GameObject
2. Name it "SceneFlowManager"
3. Add **SceneFlowManager** component
4. Configure scene names if needed
5. This GameObject will persist across scenes (DontDestroyOnLoad)

## Testing Controls

### Intro Scene Skip Controls:
- **Keyboard**: Space, Enter, or Escape
- **Controller**: X button, Start button
- **Mouse**: Any click

### Main Menu Navigation:
- Existing controls remain unchanged
- Scene transitions now use loading screen

## Troubleshooting

### Issue: Loading screen doesn't appear
- Check that LoadingScene is in Build Settings
- Verify scene name matches in SceneFlowManager
- Check Console for error messages

### Issue: Progress bar doesn't move
- Ensure LoadingScreenController references are assigned
- Check that progressBar is a Slider component
- Verify SceneFlowManager is active

### Issue: Intro scene doesn't transition
- Check IntroSceneController is active
- Verify scene names in SceneFlowManager
- Check Console for errors

### Issue: Scenes don't exist errors
- Open Build Settings (Ctrl+Shift+B)
- Verify all scenes are added and enabled
- Check scene names match exactly (case-sensitive)

## Advanced Customization

### Custom Loading Phases
Edit LoadingScreenController's "Loading Phases" array to customize text:
```
Initializing...
Loading Assets...
Preparing Scene...
Almost Ready...
```

### Custom Transition Durations
Adjust these in SceneFlowManager:
- **Minimum Loading Time**: Ensures loading screen shows for minimum duration

### Skip Intro by Default
In IntroSceneController:
- Set "Intro Duration" to 0.1 for instant skip
- Or disable IntroSceneController component entirely

### Direct Scene Loading
For quick transitions without loading screen, use:
```csharp
SceneFlowManager.Instance.LoadSceneDirect("SceneName");
```

## Integration with Existing Code

The MainMenu.cs has been updated to use SceneFlowManager automatically. The system includes fallbacks for compatibility:

```csharp
if (SceneFlowManager.Instance != null)
{
    SceneFlowManager.Instance.LoadCharacterSelect();
}
else
{
    // Fallback to direct loading
    SceneManager.LoadScene(characterSelectSceneName);
}
```

This ensures the game works even if SceneFlowManager is not set up.

## Next Steps

1. Complete the Unity Editor setup as described above
2. Test the full flow from intro to main menu
3. Customize visual appearance of loading screen
4. Add additional loading phases if needed
5. Consider adding fade effects or animations

## Support

If you encounter issues:
1. Check Unity Console for error messages
2. Verify all references are assigned in Inspector
3. Ensure scene names match exactly
4. Check that all required scenes are in Build Settings
