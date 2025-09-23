# Test Scene Setup Guide

## Overview
This guide will help you set up a test scene for the continuous bowling system where a ball automatically bowls to a target, waits 3 seconds, returns to its original position, and repeats.

## Required GameObjects

### 1. Ball GameObject
- Create a sphere GameObject (or use your existing ball prefab)
- Name it "TestBall"
- Position it where you want the ball to start from
- Make sure it has a Rigidbody component (the script will configure it automatically)

### 2. Target GameObject
- Create a cube or sphere GameObject
- Name it "Target"
- Position it where you want the ball to land
- This will be your "pitch" area

### 3. Ball Spawn Point
- Create an empty GameObject
- Name it "BallSpawnPoint"
- Position it at the same location as your ball (or where you want the ball to return to)

## Script Setup

### 1. Add the Script
- Select any GameObject in your scene (or create an empty one)
- Add the `ContinuousBowlingTest` script component

### 2. Assign References
In the inspector, assign:
- **Ball**: Drag your TestBall GameObject
- **Target**: Drag your Target GameObject  
- **Ball Spawn Point**: Drag your BallSpawnPoint GameObject

### 3. Configure Settings
Adjust these values in the inspector:

#### Bowling Settings
- **Ball Speed**: 30 m/s (adjust for desired speed)
- **Arc Height**: 3m (how high the ball goes)
- **Return Speed**: 15 m/s (how fast ball returns)
- **Wait Time After Landing**: 3 seconds (as requested)

#### Physics
- **Gravity**: 9.81 m/s² (realistic cricket physics)
- **Use Realistic Physics**: true (for realistic ball movement)

#### Controls
- **Start Key**: Space (to start the test)
- **Stop Key**: Escape (to stop the test)

## How It Works

1. **Press Space** to start the continuous bowling loop
2. Ball automatically calculates trajectory to target
3. Ball follows realistic cricket ball physics
4. Ball lands on target and waits 3 seconds
5. Ball smoothly returns to original position
6. Process repeats automatically
7. **Press Escape** to stop the loop

## Features

- ✅ **Realistic Physics**: Uses proper cricket ball physics with gravity
- ✅ **Automatic Targeting**: Calculates perfect trajectory to target
- ✅ **3-Second Delay**: Waits exactly 3 seconds after landing
- ✅ **Smooth Return**: Ball smoothly returns to original position
- ✅ **Continuous Loop**: Automatically repeats the process
- ✅ **Visual Effects**: Trail renderer for ball movement
- ✅ **Easy Controls**: Space to start, Escape to stop

## Troubleshooting

### Ball doesn't move
- Check if all references are assigned in the inspector
- Ensure the ball has a Rigidbody component
- Check console for error messages

### Ball misses target
- Adjust Ball Speed and Arc Height values
- Make sure target is within reasonable distance
- Check if Use Realistic Physics is enabled

### Ball doesn't return
- Check if Ball Spawn Point is assigned
- Ensure Return Speed is not too low
- Check console for error messages

## Advanced Customization

You can modify the script to:
- Change the wait time (currently 3 seconds)
- Adjust physics parameters
- Add sound effects
- Change ball appearance
- Add multiple targets
- Implement different bowling styles

## Testing

1. Set up the scene as described above
2. Press Play in Unity
3. Press Space to start the test
4. Watch the ball bowl to target, wait, and return
5. Press Escape to stop when done
6. Use context menu options for additional testing
