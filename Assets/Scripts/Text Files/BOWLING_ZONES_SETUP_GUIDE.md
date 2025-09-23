# 🏏 Bowling Zones & Individual Settings Setup Guide

## ✅ **NEW: Visual Bowling Zones & Individual Settings!**

The system now includes **visual bowling zones** and **individual settings** for each bowling length type!

## 🎯 **Visual Bowling Zones**

The system automatically creates **highlighted areas** on the pitch to show different bowling lengths:

- **🔴 Yorker Zone** (0-10% of pitch) - Red highlight
- **🟡 Full Length Zone** (10-30% of pitch) - Yellow highlight  
- **🟢 Good Length Zone** (30-50% of pitch) - Green highlight
- **🔵 Short Length Zone** (50-70% of pitch) - Blue highlight
- **🟣 Bouncer Zone** (70-100% of pitch) - Purple highlight

## ⚙️ **Individual Bowling Settings**

Each bowling length now has its own **customizable settings**:

### **🔴 Yorker Settings:**
- **Speed Range**: 12-15 m/s
- **Arc Range**: 1.5-2.0m
- **Bounce Range**: 0.8-1.2
- **X Rotation**: 0° (no rotation)

### **🟡 Full Length Settings:**
- **Speed Range**: 10-13 m/s
- **Arc Range**: 1.0-1.5m
- **Bounce Range**: 0.6-0.9
- **X Rotation**: 1° (slight rotation)

### **🟢 Good Length Settings:**
- **Speed Range**: 8-11 m/s
- **Arc Range**: 1.2-1.8m
- **Bounce Range**: 0.5-0.8
- **X Rotation**: 5° (moderate rotation)

### **🔵 Short Length Settings:**
- **Speed Range**: 6-9 m/s
- **Arc Range**: 1.5-2.2m
- **Bounce Range**: 0.3-0.6
- **X Rotation**: 10° (higher rotation)

### **🟣 Bouncer Settings:**
- **Speed Range**: 4-7 m/s
- **Arc Range**: 1.0-1.5m
- **Bounce Range**: 0.2-0.4
- **X Rotation**: 25° (maximum rotation)

## 🎮 **How the System Works:**

### **1. Automatic Detection:**
- **Move your target** to different zones on the pitch
- **System automatically detects** which bowling length you're targeting
- **Settings adjust automatically** based on target position

### **2. Visual Feedback:**
- **Colored zones** show exactly where each bowling length is
- **Zone labels** display the bowling length name
- **Real-time updates** as you move the target

### **3. Dynamic Adjustments:**
- **Ball speed** changes based on target zone
- **Arc height** adjusts for realistic trajectory
- **Bounce force** varies for different lengths
- **Spawn point rotation** changes for different bowling angles

## 🛠️ **Setup Instructions:**

### **Step 1: Assign References**
1. Select your **ContinuousBowlingTest_WithBounce** GameObject
2. Assign these references:
   - **Umpire Wicket**: Wicket at bowler's end
   - **Batsman Wicket**: Wicket at batsman's end
   - **Target**: Your target GameObject

### **Step 2: Configure Settings (Optional)**
1. **Adjust individual settings** for each bowling length in the Inspector
2. **Customize speed ranges** for different bowling types
3. **Modify arc heights** for realistic trajectories
4. **Set bounce forces** for different ball behaviors
5. **Configure rotations** for different bowling angles

### **Step 3: Enable Zone Visualization**
1. Make sure **"Show Bowling Zones"** is enabled
2. **Assign custom materials** for each zone (optional)
3. **Adjust zone width and height** as needed

## 🎯 **How to Use:**

1. **Press S** → Creates new ball with current zone settings
2. **Move target** → Watch zones highlight and settings change
3. **Press SPACE** → Bowl with zone-specific settings
4. **Watch console** → See which bowling length is detected

## 🎨 **Zone Materials (Optional):**

You can assign custom materials for each zone:
- **Yorker Zone Material**: Red transparent material
- **Full Length Zone Material**: Yellow transparent material
- **Good Length Zone Material**: Green transparent material
- **Short Length Zone Material**: Blue transparent material
- **Bouncer Zone Material**: Purple transparent material

## 🔧 **Customization Options:**

### **Zone Visualization:**
- **Show Bowling Zones**: Enable/disable zone display
- **Zone Height**: Height of the zone planes
- **Zone Width**: Width of the zone planes

### **Individual Settings:**
- **Speed Ranges**: Min/Max speed for each bowling type
- **Arc Ranges**: Min/Max arc height for each bowling type
- **Bounce Ranges**: Min/Max bounce force for each bowling type
- **Rotation Values**: X-axis rotation for spawn point

## 🎉 **Expected Results:**

- **Visual zones** clearly show bowling length areas
- **Automatic detection** of bowling length based on target position
- **Individual settings** for each bowling type
- **Realistic bowling variations** with different speeds, arcs, and bounces
- **Dynamic spawn point rotation** for different bowling angles

The system now provides **complete visual feedback** and **individual control** over each bowling length type! 🏏⚡🎯
