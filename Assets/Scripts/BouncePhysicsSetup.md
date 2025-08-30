# Cricket Ball Bounce Physics Setup Guide

## Overview
This guide explains how to set up realistic cricket ball bounce physics when the ball hits the target. The ball will now bounce naturally like a real cricket ball, with multiple bounces that gradually decrease in height.

## New Scripts

### 1. **ContinuousBowlingTest_WithBounce.cs** - Main Script with Bounce Physics
- **Replaces**: `ContinuousBowlingTest_Fixed.cs`
- **Features**: Realistic cricket ball bouncing, bounce tracking, and physics

### 2. **CricketBallBounce.cs** - Bounce Detection Component
- **Automatically added** to the ball
- **Detects** when ball hits target
- **Triggers** bounce physics

## Setup Instructions

### Step 1: Replace the Script
1. Remove the old `ContinuousBowlingTest_Fixed` component
2. Add the new `ContinuousBowlingTest_WithBounce` component
3. Assign the same references (Ball, Target, Ball Spawn Point)

### Step 2: Configure Bounce Physics
In the inspector, adjust these new settings:

#### **Bounce Physics Settings**
- **Bounce Force**: 0.4 (40% of impact velocity - realistic cricket bounce)
- **Bounce Friction**: 0.7 (70% of velocity preserved after bounce)
- **Max Bounces**: 2 (maximum number of bounces before stopping)

#### **Recommended Values**
- **Bounce Force**: 0.3 - 0.5 (30% - 50% for realistic cricket)
- **Bounce Friction**: 0.6 - 0.8 (60% - 80% for natural deceleration)
- **Max Bounces**: 2 - 3 (typical cricket ball behavior)

## How Bounce Physics Works

### 1. **Bounce Detection**
- Ball automatically detects when it's near the target
- Triggers bounce when moving downward with sufficient velocity
- Uses smart detection to avoid false bounces

### 2. **Realistic Bounce Physics**
- **First Bounce**: Ball bounces with 40% of impact velocity
- **Second Bounce**: Ball bounces with reduced velocity (friction applied)
- **Natural Deceleration**: Each bounce reduces the ball's energy
- **Realistic Height**: Bounces get progressively lower

### 3. **Bounce Behavior**
- Ball bounces 2-3 times naturally
- Each bounce reduces height and velocity
- Ball eventually settles on the target
- **3-second wait** starts after ball settles

## Visual Feedback

### **Gizmos in Scene View**
- **Red Line**: Ball trajectory to target
- **Green Sphere**: Target position
- **Blue Sphere**: Ball's original position
- **Yellow Sphere**: Last bounce position (when bouncing)

### **Console Logs**
- Bounce detection messages
- Bounce velocity calculations
- Physics application confirmations

## Advanced Customization

### **Adjust Bounce Height**
- **Lower Bounce Force** (0.2 - 0.3): Ball bounces very little
- **Higher Bounce Force** (0.5 - 0.7): Ball bounces more dramatically

### **Adjust Bounce Count**
- **Lower Max Bounces** (1): Ball stops after first bounce
- **Higher Max Bounces** (3-4): Ball bounces more times

### **Adjust Bounce Deceleration**
- **Lower Bounce Friction** (0.5): Ball loses energy quickly
- **Higher Bounce Friction** (0.8): Ball maintains energy longer

## Troubleshooting

### **Ball Doesn't Bounce**
- Check if `CricketBallBounce` component is on the ball
- Ensure `useRealisticPhysics` is enabled
- Verify target is assigned correctly

### **Ball Bounces Too High**
- Reduce `Bounce Force` to 0.3 or lower
- Check if `arcHeight` is too high

### **Ball Bounces Too Many Times**
- Reduce `Max Bounces` to 1 or 2
- Increase `Bounce Friction` to 0.8

### **Ball Doesn't Settle**
- Ensure `Max Bounces` is set correctly
- Check bounce detection radius in `CricketBallBounce` component

## Example Settings for Different Bowling Styles

### **Fast Bowling (Realistic)**
- Bounce Force: 0.4
- Bounce Friction: 0.7
- Max Bounces: 2

### **Spin Bowling (Lower Bounce)**
- Bounce Force: 0.3
- Bounce Friction: 0.6
- Max Bounces: 2

### **Yorker (Minimal Bounce)**
- Bounce Force: 0.2
- Bounce Friction: 0.5
- Max Bounces: 1

## Testing the System

1. **Press Play** in Unity
2. **Press Space** to start bowling
3. **Watch the ball**:
   - Lands on target
   - Bounces naturally 2-3 times
   - Gradually settles
   - Waits 3 seconds
   - Returns to original position
4. **Press Escape** to stop

## Physics Simulation

The system now provides:
- ✅ **Realistic cricket ball bouncing**
- ✅ **Natural energy loss** with each bounce
- ✅ **Progressive height reduction**
- ✅ **Smart bounce detection**
- ✅ **Configurable bounce behavior**
- ✅ **Visual feedback** and debugging

This creates a much more authentic cricket experience where the ball behaves like a real cricket ball hitting the pitch!
