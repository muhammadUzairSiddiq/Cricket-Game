# Gameplay State Machine Setup Guide

## Overview
Flexible, scalable state machine system for smooth gameplay flow. Frame-efficient with loading panel transitions.

## State Flow
1. **IntroCam** → Plays intro animation (3s), bowler instantiated in background
2. **PitchCam** → User drags target + selects speed (15s timer)
   - Success → **CameraFollow**
   - Timeout → **Failed** (shows "TIMEOUT")
3. **CameraFollow** → Camera follows bowler, wait for Space key
4. **Bowling** → Bowler bowls (same as current functionality)

## Setup Instructions

### Step 1: Add Components to Scene

1. **Create State Machine GameObject:**
   - Create empty GameObject named "GameplayStateMachine"
   - Add `GameStateMachine` component
   - Add `GameplayStateManager` component

2. **Add State Components:**
   - Add all state scripts to the same GameObject OR create separate GameObjects for each:
     - `IntroCamState`
     - `PitchCamState`
     - `FailedState`
     - `CameraFollowState`
     - `BowlingState`

### Step 2: Configure State Machine

**On GameStateMachine component:**
- ✅ Enable "Use Loading Panel Transitions" (for smooth transitions)
- Set "Transition Duration" (default: 0.4s)
- Enable "Show Debug Logs" for testing

### Step 3: Configure Each State

**IntroCamState:**
- Assign `Intro Cam` to "Intro Cam" field
- Assign bowler prefab to "Bowler Prefab"
- Assign spawn point to "Bowler Spawn Point"
- Set "Intro Duration" (default: 3s)

**PitchCamState:**
- Assign `Pitch Cam` to "Pitch Cam" field
- Assign `SpeedController` component to "Speed Controller"
- Assign `TargetDragger` component to "Target Dragger"
- Set "Time Limit" (default: 15s)

**FailedState:**
- Create UI Text for timeout message
- Assign TextMeshProUGUI component to "Timeout Text"
- Set "Display Duration" (default: 3s)

**CameraFollowState:**
- Assign `Bowler Follow Cam` to "Follow Cam" field
- Assign `BowlerFollowCamera` component to "Bowler Follow Camera"

**BowlingState:**
- Assign `BowlingController` component to "Bowling Controller"

### Step 4: Configure GameplayStateManager

**On GameplayStateManager component:**
- Assign `GameStateMachine` (auto-finds if on same GameObject)
- Assign all state components (auto-finds if not assigned)
- Set "Initial State Name" = "IntroCam"

### Step 5: Verify Camera Setup

- Ensure only one camera is active at a time per state
- Intro Cam: Active in IntroCam state
- Pitch Cam: Active in PitchCam state  
- Bowler Follow Cam: Active in CameraFollow and Bowling states

## Key Features

✅ **Flexible:** Easy to add/remove/replace states
✅ **Smooth:** Loading panel transitions between states
✅ **Frame-efficient:** Only active state updates
✅ **Scalable:** Clean interface-based design

## Adding New States

1. Create new class implementing `IGameState`
2. Implement `OnEnter()`, `OnUpdate()`, `OnExit()`
3. Register in `GameplayStateManager.RegisterStates()`
4. Transition using `stateMachine.TransitionToState("StateName")`

## Speed Bar Fix

- Speed bar now only stops when tapped on the slider area (not anywhere)
- `SpeedController.IsSpeedSelected` property indicates if user selected speed
- Used by PitchCamState to detect success condition

## Space Key

- Changed from P key to Space key for bowling
- Used in CameraFollowState to transition to Bowling
- Used in BowlerFollowCamera to resume following

## Troubleshooting

**States not transitioning:**
- Check state names match exactly
- Verify states are registered (check console logs)
- Ensure GameplayStateManager is in scene

**Camera issues:**
- Ensure cameras are assigned in state components
- Check camera activation/deactivation in OnEnter/OnExit

**Speed bar not stopping:**
- Verify `stopOnAnyTap` is enabled in SpeedController
- Check slider has EventTrigger component
- Ensure slider is interactable

