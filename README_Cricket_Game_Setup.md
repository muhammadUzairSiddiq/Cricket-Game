# Cricket Game Prototype - Complete Setup Guide

## 🏏 Overview
This is a comprehensive cricket game system designed for Android mobile with realistic bat movement, smooth camera following, and optimized touch controls. The system provides the best possible gameplay experience with 360-degree bat movement that mimics real cricket shots.

## ✨ Features

### 🎯 **Enhanced Bat Controller**
- **360-degree movement** following mouse/touch input
- **Realistic rotation** with proper physics
- **Smooth interpolation** for natural movement
- **Shot type recognition** (Defensive, Drive, Pull)
- **Mobile-optimized** touch sensitivity

### 📹 **Cricket Camera Controller**
- **Backside view** (behind the batsman)
- **Smooth following** with configurable speed
- **Height and distance limits** for optimal viewing
- **Camera shake effects** for impact
- **Zoom functionality**

### 📱 **Mobile Input Handler**
- **Touch-optimized** controls
- **Gesture recognition** (swipe detection)
- **Smooth input processing** with dead zones
- **Visual touch indicators**
- **Configurable sensitivity**

### 🛠️ **Auto-Setup System**
- **One-click setup** for all components
- **Automatic component detection**
- **Physics configuration**
- **Input system integration**

## 🚀 Quick Setup (Recommended)

### Step 1: Add Setup Script
1. Create an empty GameObject in your scene
2. Name it "CricketGameSetup"
3. Add the `CricketGameSetup.cs` script to it

### Step 2: Run Setup
1. **Right-click** on the CricketGameSetup GameObject in the Hierarchy
2. Select **"Setup Cricket Game"** from the context menu
3. Check the Console for setup messages
4. Your bat should now respond to mouse/touch input
5. Camera should be positioned behind the bat

## 🔧 Manual Setup (Advanced)

### Context Menu Options
The `CricketGameSetup` script provides two context menu options:
- **"Setup Cricket Game"**: Configures all components automatically
- **"Reset Setup"**: Removes all added components for a clean slate

To access these:
1. Right-click on the CricketGameSetup GameObject in the Hierarchy
2. Select the desired option from the context menu

### 1. Bat Setup
```csharp
// Add to your bat GameObject:
- EnhancedBatController component
- Rigidbody (configured automatically)
- Tag as "Player"
```

### 2. Camera Setup
```csharp
// Add to your Main Camera:
- CricketCameraController component
- Set target to bat GameObject
- Configure offset and smooth speed
```

### 3. Input Setup
```csharp
// Add to your bat GameObject:
- MobileInputHandler component
- PlayerInput component (if using new Input System)
- Configure touch sensitivity and dead zones
```

## 📱 Mobile Optimization

### Touch Settings
- **Touch Sensitivity**: 1.5 (default) - Adjust for your needs
- **Dead Zone**: 0.05 (default) - Prevents accidental input
- **Smoothing**: Enabled by default for smooth movement
- **Gesture Recognition**: Swipe detection for shot types

### Performance Tips
- Use `RigidbodyInterpolation.Interpolate` for smooth movement
- Enable `Continuous` collision detection
- Optimize camera following with appropriate smooth speeds

## 🎮 Input Controls

### Mouse (PC)
- **Left Click + Drag**: Control bat rotation
- **Movement**: Bat follows mouse cursor position
- **Release**: Bat returns to center position

### Touch (Mobile)
- **Touch + Drag**: Control bat rotation
- **Swipe Up**: Drive shot
- **Swipe Down**: Defensive shot
- **Swipe Right**: Pull shot
- **Release**: Bat returns to center position

## ⚙️ Configuration Options

### Bat Movement
```csharp
rotationSpeed = 3f;        // How fast bat responds to input
returnSpeed = 8f;          // How fast bat returns to center
maxRotationAngle = 60f;    // Maximum rotation angle
smoothDamping = 10f;       // Smoothing factor for movement
```

### Camera Settings
```csharp
cameraOffset = (0, 2.5f, -8f);  // Camera position relative to bat
smoothSpeed = 5f;                // How fast camera follows
rotationSmoothSpeed = 8f;        // How fast camera rotates
```

### Mobile Settings
```csharp
touchSensitivity = 1.5f;         // Touch input sensitivity
deadZone = 0.05f;                // Input dead zone
enableSmoothing = true;           // Enable input smoothing
```

## 🎯 Shot Types

### Defensive Shot
- **Trigger**: Swipe down or small downward movement
- **Angle**: 15° (configurable)
- **Use**: Defensive play, blocking

### Drive Shot
- **Trigger**: Swipe up or upward movement
- **Angle**: 45° (configurable)
- **Use**: Attacking play, driving through covers

### Pull Shot
- **Trigger**: Swipe right or rightward movement
- **Angle**: 60° (configurable)
- **Use**: Pulling to leg side

## 🔍 Troubleshooting

### Common Issues

#### Bat Not Moving
- Check if `EnhancedBatController` is attached
- Verify Rigidbody is present and configured
- Check Console for error messages

#### Camera Not Following
- Ensure `CricketCameraController` is on Main Camera
- Check if target is assigned correctly
- Verify camera offset values

#### Touch Not Working
- Check if `MobileInputHandler` is attached
- Verify touch sensitivity settings
- Test on actual mobile device (not just Unity Remote)

#### Input System Errors
- Ensure Input Actions asset is assigned
- Check if PlayerInput component is configured
- Verify action map names match

### Debug Mode
Enable debug mode in `CricketGameSetup` to see detailed setup information in the Console.

## 📁 File Structure
```
Assets/Scripts/
├── EnhancedBatController.cs      # Main bat movement controller
├── CricketCameraController.cs    # Camera following and positioning
├── MobileInputHandler.cs         # Touch input processing
├── CricketGameSetup.cs           # Auto-setup system
└── README_Cricket_Game_Setup.md  # This file
```

## 🎮 Gameplay Tips

### For Best Experience
1. **Start with default settings** and adjust gradually
2. **Test on actual mobile device** for touch optimization
3. **Use smooth camera following** for immersive feel
4. **Adjust touch sensitivity** based on device performance
5. **Enable touch visualization** during development

### Performance Optimization
1. **Use appropriate smooth speeds** (not too fast, not too slow)
2. **Limit maximum rotation angles** for realistic movement
3. **Optimize camera update frequency** if needed
4. **Test on target device** for mobile optimization

## 🔄 Updates and Maintenance

### Adding New Features
- Extend `EnhancedBatController` for new shot types
- Add new camera behaviors to `CricketCameraController`
- Implement additional input methods in `MobileInputHandler`

### Customization
- Modify shot angles and speeds
- Adjust camera positioning and behavior
- Customize touch sensitivity and dead zones
- Add new gesture recognition patterns

## 📞 Support

If you encounter issues:
1. Check the Console for error messages
2. Verify all components are properly attached
3. Test with default settings first
4. Check this README for troubleshooting steps

## 🎯 Next Steps

After setting up the basic system:
1. **Add ball physics** and collision detection
2. **Implement scoring system** and game logic
3. **Add sound effects** and visual feedback
4. **Create UI elements** for mobile interface
5. **Add animations** for bat and player movement

---

**Happy Cricket Gaming! 🏏⚾**

*This system is designed to provide the foundation for a professional-quality cricket game. Customize and extend it to match your specific game requirements.*
