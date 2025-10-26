# Wicket Breaking System Setup Guide

## Quick Setup Instructions

### 1. **Add WicketBreakingSystem to Your Wicket**
- Select your `Batsmen Wicket` GameObject in the Hierarchy
- Add Component → `WicketBreakingSystem`
- The script will automatically detect:
  - `wicket bails 02` and `bails top` as bails
  - `Cylinder.010` and other stump objects as stumps

### 2. **Add BallWicketCollision to Your Ball**
- Select your `BALL(Clone)` GameObject
- Add Component → `BallWicketCollision`
- Configure settings:
  - **Min Hit Velocity**: `2` (minimum speed to break wicket)
  - **Collision Radius**: `0.3` (ball collision size)

### 3. **Configure Wicket Breaking Settings**
In the `WicketBreakingSystem` component:
- **Break Force**: `10` (how hard pieces fly off)
- **Break Torque**: `5` (rotation effect)
- **Break Delay**: `0.1` (delay between bails and stumps)
- **Bail Lifetime**: `3` (how long bails stay)
- **Stump Lifetime**: `5` (how long stumps stay)

### 4. **Optional: Add Effects**
- **Break Effect Prefab**: Assign a particle effect prefab
- **Break Sound**: Assign an audio clip for breaking sound

## How It Works

1. **Ball hits wicket** → `BallWicketCollision` detects collision
2. **Velocity check** → Only breaks if ball is moving fast enough
3. **Bails break first** → Fly off with physics forces
4. **Stumps break after delay** → Fall over realistically
5. **Pieces auto-destroy** → Clean up after specified time

## Customization Options

### **Force Settings**
- Increase `Break Force` for more dramatic breaking
- Adjust `Break Torque` for more/less rotation
- Modify `Break Delay` for timing control

### **Physics Settings**
- Enable/disable gravity on broken pieces
- Adjust mass of bails vs stumps
- Control lifetime of broken pieces

### **Visual Effects**
- Add particle systems for dust/splinters
- Include sound effects for impact
- Use different materials for broken pieces

## Testing

1. **Play the scene**
2. **Bowl the ball** towards the wicket
3. **Watch the wicket break** when ball hits with sufficient velocity
4. **Adjust settings** as needed for desired effect

## Troubleshooting

- **Wicket not breaking**: Check if ball has enough velocity
- **Pieces not flying**: Increase Break Force
- **Too dramatic**: Reduce Break Force and Break Torque
- **Not detecting collision**: Ensure wicket has colliders enabled
