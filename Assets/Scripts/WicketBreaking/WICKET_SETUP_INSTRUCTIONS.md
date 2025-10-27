# 🎳 Wicket Breaking System - Setup Instructions

## Problem: Ball Passes Through Without Breaking

If the ball passes through the wicket without breaking, it means the `WicketBreakingSystem` component is NOT properly configured in your scene.

## ✅ Required Setup Steps

### Step 1: Select Your Wicket GameObject
In the Hierarchy, select the main wicket GameObject (e.g., "Wicket Batmen" or "Batsmen Wicket").

### Step 2: Add WicketBreakingSystem Component
1. Click "Add Component" in the Inspector
2. Search for "WicketBreakingSystem"
3. Add the component

### Step 3: Configure the Wicket Stumps
1. Expand the **Wicket Stumps** array in the Inspector
2. Set **Size** to `3` (or however many stumps you have)
3. Assign each stump GameObject:
   - Element 0: Your 1st stump
   - Element 1: Your Middle stump
   - Element 2: Your 3rd stump

### Step 4: Configure the Bails
1. Expand the **Wicket Bails** array in the Inspector
2. Set **Size** to `2` (for 2 bails)
3. Drag your bail GameObjects into the array

### Step 5: Configure Settings (Optional)
- **Break Force**: 10 (light hits)
- **Severe Break Force**: 20 (hard hits)
- **Break Torque**: 5
- **Speed For All Stumps Break**: 14 (if speed > 14, all stumps break)
- **Stump Lifetime**: 5 seconds (how long before reset)

## 🔍 Verification Checklist

- [ ] WicketBreakingSystem component is added
- [ ] All 3 stumps are assigned to Wicket Stumps array
- [ ] Both bails are assigned to Wicket Bails array
- [ ] Stumps have colliders enabled
- [ ] Ball has BallWicketCollision component
- [ ] Ball has a collider enabled

## ⚠️ Common Issues

1. **Wicket not breaking**: WicketBreakingSystem component missing
2. **Only one stump breaks**: Incorrect speed threshold (adjust `speedForAllStumpsBreak`)
3. **Stumps sinking**: BrokenStumpPhysics component handles this automatically
4. **Bails not assigned**: Check Wicket Bails array size and assignments

## 🎮 Test

After setup, bowl the ball at the wicket and it should break!

