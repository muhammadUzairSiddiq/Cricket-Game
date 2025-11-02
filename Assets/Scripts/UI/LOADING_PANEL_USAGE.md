# Loading Panel Manager - Usage Guide

## Overview
Professional, reusable loading panel system with smooth radial fill animations. Perfect for scene transitions, loading states, and screen fades.

## Setup Instructions

1. **Assign Loading Panel Reference:**
   - In Unity, find the `LoadingPanelManager` component (it will auto-create if needed)
   - Assign your `Loading Panel` GameObject's Image component to the `Loading Panel Image` field in the Inspector

2. **Panel Configuration (Already Done):**
   - ✅ Image Type: `Filled`
   - ✅ Fill Method: `Radial 90`
   - ✅ Fill Origin: `BottomLeft`
   - ✅ Color: `Black`
   - ✅ Full Screen Coverage: Automatic

## Usage Examples

### Basic Usage - Show Loading Panel
```csharp
// Black out the screen
LoadingPanelManager.Show();

// Reveal the screen
LoadingPanelManager.Hide();
```

### With Custom Duration
```csharp
// Show with 1 second animation
LoadingPanelManager.Show(1f);

// Hide with 0.3 second animation
LoadingPanelManager.Hide(0.3f);
```

### With Callbacks (Perfect for State Machine Integration)
```csharp
// Show panel, then load new scene
LoadingPanelManager.Show(0.5f, () => {
    SceneManager.LoadScene("NextScene");
    LoadingPanelManager.Hide(0.5f); // Reveal after loading
});
```

### State Machine Integration Example
```csharp
public void TransitionToNextState()
{
    // Black out screen
    LoadingPanelManager.Show(0.4f, () => {
        // Perform state transition while screen is black
        ChangeState();
        
        // Reveal screen after transition
        LoadingPanelManager.Hide(0.4f, () => {
            OnTransitionComplete();
        });
    });
}
```

### Check Visibility
```csharp
if (LoadingPanelManager.IsVisible())
{
    Debug.Log("Screen is currently blacked out");
}
```

### Instant Show/Hide (No Animation)
```csharp
// Instantly black out (useful for immediate transitions)
LoadingPanelManager.ShowInstant();

// Instantly reveal
LoadingPanelManager.HideInstant();
```

### Toggle
```csharp
// Toggle between visible and hidden
LoadingPanelManager.Toggle();
```

## Features

✅ **Singleton Pattern** - Call from anywhere, no references needed  
✅ **Smooth Animations** - Customizable duration and curves  
✅ **Callback Support** - Perfect for state machines and async operations  
✅ **Auto-Setup** - Automatically finds and configures Loading Panel  
✅ **Full Screen Coverage** - Ensures panel covers entire screen  
✅ **Professional** - Production-ready, reusable code  

## Integration Tips

1. **Scene Transitions:** Use Show() before loading, Hide() after
2. **State Machine:** Show panel before state change, hide after
3. **Loading Screens:** Show while loading assets, hide when complete
4. **Game Pauses:** Instant show/hide for immediate pause transitions

## Customization

Edit the `LoadingPanelManager` component in Inspector to adjust:
- **Show Duration:** How long it takes to black out (default: 0.5s)
- **Hide Duration:** How long it takes to reveal (default: 0.5s)
- **Animation Curves:** Customize the animation feel
- **Auto Find Panel:** Enable to auto-detect Loading Panel in scene

