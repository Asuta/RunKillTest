# BeGrabAndScaleobject.cs - English Summary

## Quick Overview

This is a Unity VR interaction script that allows users to grab and scale 3D objects using VR controllers. It supports both one-handed and two-handed manipulation with smooth interpolation and intelligent single-axis scaling.

## Core Features

### 1. **One-Handed Grab**
- Grab object with one hand
- Object follows hand position and rotation
- Maintains relative offset from when grabbed

### 2. **Two-Handed Scaling**
- When a second hand grabs the object, it enters scaling mode
- Object scales based on the distance between two hands
- Intelligent single-axis scaling (X, Y, or Z)

### 3. **Single-Axis Scaling**
The system automatically determines which axis to scale based on the direction between your hands:
- If hands are aligned with X-axis → scales along X
- If hands are aligned with Y-axis → scales along Y  
- If hands are aligned with Z-axis → scales along Z

This creates an intuitive "stretch in the direction you pull" behavior.

### 4. **Indirect Grab Mode**
Uses a two-layer interpolation system:
- Layer 1: Middle data smoothly follows hand (with interpolation)
- Layer 2: Object maintains fixed offset from middle data (no interpolation)
- Result: Smoother grab with less jitter

### 5. **Command Pattern Integration**
- All scaling operations are recorded as commands
- Supports undo/redo functionality
- Integrates with a command history system

---

## How It Works

### State Machine

```
[Not Grabbed] 
    ↓ (one hand grabs)
[Single Hand Grabbed]
    ↓ (second hand grabs)
[Two Hand Scaling]
    ↓ (one hand releases)
[Single Hand Grabbed] or [Not Grabbed]
```

### Key Algorithms

#### 1. Smooth Follow (Exponential Decay)
```csharp
alpha = 1 - Exp(-speed * deltaTime)
position = Lerp(current, target, alpha)
```
- Frame-rate independent
- Natural motion curve
- Automatically slows down as it approaches target

#### 2. Axis Selection (Angle Minimization)
```csharp
angleX = Angle(handVector, transform.right)
angleY = Angle(handVector, transform.up)
angleZ = Angle(handVector, transform.forward)

// Choose axis with smallest angle
if (angleX < angleY && angleX < angleZ)
    scaleAxis = X
```

#### 3. Relative Scaling
```csharp
scaleRate = currentDistance / initialDistance
newScale = baseScale * scaleRate
```
- Avoids cumulative errors
- Accurate even when switching axes

---

## Code Structure

### Main Components

1. **Grab State Management**
   - `primaryHand` - First hand that grabbed
   - `secondaryHand` - Second hand (if any)
   - `isGrabbed` - Overall grab state

2. **Indirect Grab System**
   - `indirectTarget` - The hand to follow
   - `middlePosition/Rotation` - Interpolation buffer
   - `indirectGrabOffset` - Fixed offset from buffer

3. **Scaling System**
   - `isTwoHandScaling` - Whether in scaling mode
   - `initialHandsDistance` - Distance when scaling started
   - `scaleAxisData` - Current scaling axis info

4. **Command System**
   - `currentScaleCommand` - Active command for undo/redo
   - Integrates with `CommandHistory`

### Key Methods

- `Update()` - Main loop, handles following and scaling
- `OnGrabbed()` - Called when a hand grabs
- `OnReleased()` - Called when a hand releases
- `PerformSingleAxisScaling()` - Executes axis-aligned scaling
- `StartIndirectGrab()` - Enables smooth indirect grab
- `CreateScaleCommand()` - Records state for undo

---

## Technical Details

### Why Single-Axis Scaling?

Traditional two-hand scaling often scales all axes uniformly (xyz all change together). This script implements single-axis scaling, which:

✅ **More Intuitive**: Pull left-right → X scales, Pull up-down → Y scales
✅ **More Control**: Can adjust length/width/height independently
✅ **Less Frustration**: Easier to make precise adjustments

### Why Indirect Grab?

Direct grab can be jittery because:
- VR tracking has noise
- Direct 1:1 mapping amplifies small movements
- Sudden tracking losses cause jumps

Indirect grab solves this by:
- Smoothing hand movements first (middle layer)
- Then applying object offset (stays rigid relative to smoothed hand)
- Result: Object feels "solid" and less shaky

---

## Integration Points

### Required Components

1. **EditorPlayer** - Manages hand tracking and input
2. **IGrabable** - Interface this class implements
3. **ScaleCommand** - Command pattern for undo/redo
4. **CommandHistory** - Manages command stack

### Events Flow

```
User Input (Trigger Press)
    ↓
EditorPlayer detects press
    ↓
Calls IGrabable.OnGrabbed()
    ↓
BeGrabAndScaleobject handles grab
    ↓
Update() loop runs continuously
    ↓
User Input (Trigger Release)
    ↓
Calls IGrabable.OnReleased()
```

---

## Usage Example

### In Unity Editor

1. Add `BeGrabAndScaleobject` component to a GameObject
2. Set `positionSmoothSpeed` (default: 10)
3. Set `rotationSmoothSpeed` (default: 15)
4. Optional: Enable `freezeYaxis` to lock Y-rotation
5. Ensure scene has `EditorPlayer` with hand references

### At Runtime

1. **Grab** - Point VR controller at object, pull trigger
2. **Move** - Move hand while holding trigger
3. **Scale** - Grab with second hand, move hands apart/together
4. **Release** - Release trigger to let go

---

## Performance Considerations

### Optimizations
✅ Uses `OverlapSphereNonAlloc` (assumed in EditorPlayer)
✅ Caches transform references
✅ Minimal per-frame allocations

### Potential Improvements
⚠️ `FindFirstObjectByType<EditorPlayer>()` is called multiple times
⚠️ Could cache EditorPlayer reference
⚠️ Could add min/max scale limits
⚠️ Could add axis switching threshold to prevent rapid changes

---

## Common Issues & Solutions

### Issue 1: Object jumps when grabbed
**Cause**: Offset not calculated correctly  
**Solution**: Ensure `OnGrabbed()` runs before first `Update()`

### Issue 2: Scaling too sensitive
**Cause**: Initial hand distance too small  
**Solution**: Add minimum distance threshold

### Issue 3: Axis keeps switching
**Cause**: Hands near 45-degree angle between axes  
**Solution**: Add hysteresis (time delay) before switching

### Issue 4: Can't undo scaling
**Cause**: Command not properly created/executed  
**Solution**: Check `CommandHistory.Instance` exists

---

## Architecture Patterns Used

1. **Component Pattern** - MonoBehaviour for Unity integration
2. **Interface Segregation** - IGrabable for polymorphism
3. **Command Pattern** - ScaleCommand for undo/redo
4. **State Machine** - Implicit states (not grabbed → single hand → two hands)
5. **Strategy Pattern** - Different scaling strategies (uniform vs single-axis)

---

## Comparison: Direct vs Indirect Grab

### Direct Grab
```
Hand Position → Object Position
(1:1 mapping, instant)
```
- Simple
- Can be jittery
- Harder to control

### Indirect Grab (This Script)
```
Hand Position → Middle Buffer (smoothed) → Object Position
(2-layer system)
```
- More complex
- Smoother
- Better control

---

## Related Files

- `BeGrabobject.cs` - Similar but without scaling
- `IGrabable.cs` - Interface definition
- `EditorPlayer.cs` - Hand controller
- `ScaleCommand.cs` - Command implementation
- `Scalelearn.md` - Development notes

---

## Future Enhancements

### Potential Features
- [ ] Uniform scaling mode toggle
- [ ] Scale limits (min/max)
- [ ] Haptic feedback on grab/release
- [ ] Visual feedback (outline/highlight)
- [ ] Multi-object scaling
- [ ] Snap-to-grid scaling
- [ ] Proportional lock (maintain aspect ratio)

### Code Quality
- [ ] Cache EditorPlayer reference
- [ ] Add XML documentation comments
- [ ] Unit tests for scaling logic
- [ ] Configurable axis switching threshold

---

## Conclusion

This script is a well-designed VR interaction system that balances:
- **Functionality** - Rich feature set
- **Usability** - Intuitive controls
- **Maintainability** - Clean code structure
- **Performance** - Efficient execution

It's suitable for:
- VR design/modeling applications
- Educational VR experiences
- Interactive VR games
- Architectural visualization

The single-axis scaling feature is particularly innovative and provides better user control than traditional uniform scaling.

---

**Version**: 1.0  
**Date**: 2025-11-13  
**Unity Version**: 2022.3+ with XR Interaction Toolkit
