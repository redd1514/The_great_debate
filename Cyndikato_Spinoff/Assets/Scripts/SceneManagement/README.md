# Scene Management System

## Overview

This directory contains the Scene Flow Management system for handling smooth transitions between game scenes.

## Components

### SceneFlowManager.cs
**Purpose**: Central singleton manager for all scene transitions

**Key Features**:
- Singleton pattern for global access
- Async scene loading with loading screen
- Direct scene loading for quick transitions
- Memory management during transitions
- Error handling for missing scenes

**Usage**:
```csharp
// Load scene with loading screen
SceneFlowManager.Instance.LoadSceneWithLoading("CharacterSelect");

// Load scene directly (no loading screen)
SceneFlowManager.Instance.LoadSceneDirect("Menu");

// Helper methods
SceneFlowManager.Instance.LoadMainMenu();
SceneFlowManager.Instance.LoadCharacterSelect();
SceneFlowManager.Instance.LoadIntroScene();

// Get loading status
bool isLoading = SceneFlowManager.Instance.IsLoading();
float progress = SceneFlowManager.Instance.GetLoadingProgress();
```

**Configuration**:
- `introSceneName`: Name of the intro scene (default: "introScene")
- `loadingSceneName`: Name of the loading scene (default: "LoadingScene")
- `mainMenuSceneName`: Name of the main menu (default: "Menu")
- `characterSelectSceneName`: Name of character select (default: "CharacterSelect")
- `minimumLoadingTime`: Minimum seconds to show loading screen (default: 2.0f)

### LoadingScreenController.cs
**Purpose**: Manages the loading screen UI with progress bar and status text

**Key Features**:
- Realistic progress bar animation
- Multiple loading phases with automatic transitions
- Smooth progress updates
- Percentage display
- Customizable loading text

**Setup Requirements**:
1. Attach to GameObject in LoadingScene
2. Assign UI references in Inspector:
   - Progress Bar (UI Slider)
   - Loading Text (TextMeshProUGUI)
   - Percentage Text (TextMeshProUGUI)
3. Configure loading phases (optional)

**Configuration**:
- `loadingPhases`: Array of status messages shown during loading
- `progressSmoothSpeed`: Speed of progress bar animation (default: 2.0f)
- `phaseTransitionTime`: Time between phase transitions (default: 0.5f)

**Usage**:
```csharp
// Usually handled automatically, but can be controlled manually
LoadingScreenController controller = FindObjectOfType<LoadingScreenController>();
controller.SetCustomLoadingText("Loading custom content...");
controller.SetProgress(0.5f);
```

### IntroSceneController.cs
**Purpose**: Manages the intro/splash screen sequence

**Key Features**:
- Auto-play intro with configurable duration
- Skip functionality (keyboard, controller, mouse)
- Optional fade in/out effects
- Smooth transition to main menu via SceneFlowManager

**Setup Requirements**:
1. Attach to GameObject in introScene
2. Optional: Assign Canvas Group for fade effects
3. Optional: Assign Logo Image reference
4. Configure timing settings

**Configuration**:
- `introDuration`: Seconds to display intro (default: 3.0f)
- `allowSkip`: Enable skip functionality (default: true)
- `useFadeIn`: Enable fade in effect (default: true)
- `useFadeOut`: Enable fade out effect (default: true)
- `fadeInDuration`: Fade in seconds (default: 1.0f)
- `fadeOutDuration`: Fade out seconds (default: 1.0f)

**Skip Controls**:
- **Keyboard**: Space, Enter, Escape
- **Controller**: X button, Start button
- **Mouse**: Any click

**Usage**:
```csharp
// Manually skip intro
IntroSceneController controller = FindObjectOfType<IntroSceneController>();
controller.SkipIntro();
```

## Scene Flow

The typical scene flow is:

1. **introScene** → Shows logo/splash screen
   - IntroSceneController plays intro sequence
   - Auto-transitions or can be skipped
   
2. **LoadingScene** → Shows loading progress
   - LoadingScreenController displays progress
   - SceneFlowManager loads target scene in background
   
3. **Menu** → Main menu
   - Players can navigate menu options
   - Uses SceneFlowManager for transitions
   
4. **CharacterSelect** → Character selection
   - Loaded via SceneFlowManager with loading screen

## Integration

### Updating Existing Code

The system is designed to integrate easily with existing code:

**Before**:
```csharp
SceneManager.LoadScene("CharacterSelect");
```

**After**:
```csharp
if (SceneFlowManager.Instance != null)
{
    SceneFlowManager.Instance.LoadCharacterSelect();
}
else
{
    SceneManager.LoadScene("CharacterSelect"); // Fallback
}
```

### MainMenu Integration

The `Mainmenu.cs` script has been updated to use SceneFlowManager:
- Start Game button now uses smooth loading transitions
- Maintains all existing functionality
- Includes fallback for compatibility

## Build Settings

Ensure scenes are in Build Settings in this order:
1. Assets/Scenes/introScene.unity
2. Assets/Scenes/LoadingScene.unity
3. Assets/Scenes/Menu.unity
4. Assets/Scenes/play/CharacterSelect.unity
5. Other game scenes...

## Testing

### In Unity Editor
1. Open introScene
2. Press Play
3. Observe:
   - Intro plays (can be skipped)
   - Loading screen appears with progress
   - Main menu loads
   - Scene transitions work smoothly

### Debug Logs
The system outputs helpful debug messages:
- "SceneFlowManager initialized"
- "Starting scene transition to [SceneName]"
- "Scene transition to [SceneName] complete"
- "Intro scene started"
- "Intro skipped by player"

## Customization

### Custom Loading Phases
Edit the `loadingPhases` array in LoadingScreenController:
```csharp
public string[] loadingPhases = new string[]
{
    "Initializing...",
    "Loading Assets...",
    "Preparing Scene...",
    "Almost Ready..."
};
```

### Custom Scene Names
If your scenes have different names, update SceneFlowManager:
```csharp
public string introSceneName = "YourIntroScene";
public string loadingSceneName = "YourLoadingScene";
public string mainMenuSceneName = "YourMainMenu";
```

### Longer/Shorter Loading Times
Adjust `minimumLoadingTime` in SceneFlowManager:
```csharp
public float minimumLoadingTime = 2f; // Seconds
```

## Troubleshooting

**Problem**: SceneFlowManager not found
- **Solution**: It creates itself automatically. Ensure script is in project.

**Problem**: Loading screen doesn't show
- **Solution**: Check scene names match exactly in SceneFlowManager and Build Settings.

**Problem**: Progress bar doesn't update
- **Solution**: Verify LoadingScreenController references are assigned in Inspector.

**Problem**: Intro scene doesn't transition
- **Solution**: Check IntroSceneController is active and SceneFlowManager can access loading scene.

## Performance

- Scene loading is asynchronous (non-blocking)
- Memory is properly managed during transitions
- Minimum loading time ensures smooth experience
- Progress updates are throttled for performance

## Best Practices

1. Always use SceneFlowManager for scene transitions
2. Keep loading phases short and descriptive
3. Test skip functionality in intro scene
4. Ensure all scenes are in Build Settings
5. Use direct loading only for very quick transitions
6. Set appropriate minimumLoadingTime for your game

## Future Enhancements

Potential additions:
- Audio fade in/out during transitions
- Advanced transition effects (wipes, fades, etc.)
- Loading tips/hints display
- Save/load integration during transitions
- Asset bundle loading support
