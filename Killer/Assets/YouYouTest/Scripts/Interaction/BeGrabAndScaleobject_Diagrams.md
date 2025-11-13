# BeGrabAndScaleobject.cs - Visual Diagrams & Flow Charts

## 1. System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Unity VR Application                      │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                      EditorPlayer                            │
│  ┌──────────────┐                    ┌──────────────┐       │
│  │  Left Hand   │                    │  Right Hand  │       │
│  │  Transform   │                    │  Transform   │       │
│  └──────────────┘                    └──────────────┘       │
│         │                                     │              │
│         └─────────────┬───────────────────────┘              │
└───────────────────────┼──────────────────────────────────────┘
                        │ Trigger Press/Release
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                  IGrabable Interface                         │
│  • OnGrabbed(handTransform)                                  │
│  • OnReleased(handTransform)                                 │
│  • UnifiedGrab(handTransform)                                │
└─────────────────────────────────────────────────────────────┘
                        │ implements
                        ▼
┌─────────────────────────────────────────────────────────────┐
│            BeGrabAndScaleobject (This Script)                │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Grab System          │  Scaling System                │  │
│  │  • Primary Hand       │  • Two-Hand Detection         │  │
│  │  • Secondary Hand     │  • Axis Selection             │  │
│  │  • Offset Tracking    │  • Distance Calculation       │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Indirect Grab        │  Command System               │  │
│  │  • Middle Position    │  • ScaleCommand               │  │
│  │  • Smooth Follow      │  • Undo/Redo Support          │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                  CommandHistory                              │
│  • ExecuteCommand(command)                                   │
│  • Undo() / Redo()                                           │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. State Machine Diagram

```
     ╔═══════════════╗
     ║  NOT GRABBED  ║ ◄────────────────┐
     ╚═══════════════╝                  │
            │                           │
            │ One hand grabs            │ Both hands release
            │ (OnGrabbed)               │ (OnReleased)
            ▼                           │
     ╔═══════════════╗                  │
     ║ SINGLE HAND   ║                  │
     ║   GRABBED     ║ ◄────────┐       │
     ╚═══════════════╝          │       │
            │                   │       │
            │ Second hand       │ One   │
            │ grabs             │ hand  │
            │ (OnGrabbed)       │ drops │
            ▼                   │       │
     ╔═══════════════╗          │       │
     ║   TWO HAND    ║──────────┘       │
     ║    SCALING    ║──────────────────┘
     ╚═══════════════╝

Legend:
  ╔═══╗ = State
  ───▶ = Transition
  ◄─── = Return Transition
```

---

## 3. Update() Method Flow Chart

```
┌─────────────────────────┐
│   Update() Called       │
└───────────┬─────────────┘
            │
            ▼
      ┌─────────────┐
      │ Indirect    │ YES
      │ Grab Active?├────────────────────────────────────────┐
      └─────┬───────┘                                        │
            │ NO                                             │
            ▼                                                │
      ┌─────────────┐                                        │
      │ Check Two   │                                        │
      │ Hand State  │                                        │
      └─────┬───────┘                                        │
            │                                                │
      ┌─────▼────────┐                                       │
      │ Entering     │ YES                                   │
      │ Two Hand? ───┼──────┐                                │
      └──────┬───────┘      │                                │
            │ NO            │                                │
            │               ▼                                │
            │         ┌─────────────┐                        │
            │         │ Record Base │                        │
            │         │ Scale &     │                        │
            │         │ Distance    │                        │
            │         └─────┬───────┘                        │
            │               │                                │
            │               ▼                                │
            │         ┌─────────────┐                        │
            │         │ Create Scale│                        │
            │         │ Command     │                        │
            │         └─────────────┘                        │
            │                                                │
      ┌─────▼────────┐                                       │
      │ Exiting      │ YES                                   │
      │ Two Hand? ───┼──────┐                                │
      └──────┬───────┘      │                                │
            │ NO            │                                │
            │               ▼                                │
            │         ┌─────────────┐                        │
            │         │ Complete    │                        │
            │         │ Scale       │                        │
            │         │ Command     │                        │
            │         └─────────────┘                        │
            │                                                │
      ┌─────▼────────┐                                       │
      │ Calculate    │                                       │
      │ Target       │                                       │
      │ Position &   │                                       │
      │ Rotation     │                                       │
      └─────┬────────┘                                       │
            │                                                │
      ┌─────▼────────┐                                       │
      │ Apply Smooth │                                       │
      │ Interpolation│                                       │
      └─────┬────────┘                                       │
            │                                                │
      ┌─────▼────────┐                                       │
      │ Two Hand     │ YES                                   │
      │ Scaling?  ───┼──────┐                                │
      └──────┬───────┘      │                                │
            │ NO            │                                │
            │               ▼                                │
            │         ┌─────────────┐                        │
            │         │ Perform     │                        │
            │         │ Single Axis │                        │
            │         │ Scaling     │                        │
            │         └─────────────┘                        │
            │                                                │
            └────────────────────────────────────────────────┤
                                                             │
      ┌──────────────────────────────────────────────────────┘
      │ Indirect Grab Path
      ▼
┌─────────────────────────┐
│ Calculate Middle Alpha  │
│ (Exponential Decay)     │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ Update Middle Position  │
│ (Smooth Follow Hand)    │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ Update Middle Rotation  │
│ (Y-Axis Only)           │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ Set Object Transform    │
│ (Fixed Offset from      │
│  Middle)                │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ Return (Skip Normal     │
│ Processing)             │
└─────────────────────────┘
```

---

## 4. Single-Axis Scaling Algorithm Flow

```
┌─────────────────────────────────────────────────────────┐
│         PerformSingleAxisScaling()                      │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
        ┌────────────────────────┐
        │ Get Vector Between     │
        │ Left and Right Hand    │
        └────────┬───────────────┘
                 │
                 ▼
        ┌────────────────────────┐
        │ Calculate Angle with   │
        │ X, Y, Z Axes           │
        │                        │
        │ angleX = min(θ, 180-θ) │
        │ angleY = min(θ, 180-θ) │
        │ angleZ = min(θ, 180-θ) │
        └────────┬───────────────┘
                 │
                 ▼
        ┌────────────────────────┐
        │ Find Minimum Angle     │
        └────┬────────┬──────────┘
             │        │
     ┌───────┘        └────────┐
     │                         │
     ▼                         ▼
┌─────────┐  ┌─────────┐  ┌─────────┐
│ angleX  │  │ angleY  │  │ angleZ  │
│   min   │  │   min   │  │   min   │
└────┬────┘  └────┬────┘  └────┬────┘
     │            │            │
     ▼            ▼            ▼
  [X-Axis]    [Y-Axis]    [Z-Axis]
     │            │            │
     └────────┬───┴────────────┘
              │
              ▼
     ┌────────────────────────┐
     │ Is New Gesture OR      │
     │ Axis Changed?          │
     └────┬──────────┬────────┘
          │ YES      │ NO
          ▼          │
     ┌────────────┐  │
     │ Record:    │  │
     │ • Scale    │  │
     │ • Distance │  │
     └────┬───────┘  │
          │          │
          └────┬─────┘
               │
               ▼
     ┌─────────────────────────┐
     │ Calculate Scale Rate:   │
     │ rate = current / record │
     └─────────┬───────────────┘
               │
               ▼
     ┌─────────────────────────┐
     │ Apply Scale to Selected │
     │ Axis Only:              │
     │                         │
     │ if X: (r*s, y, z)       │
     │ if Y: (x, r*s, z)       │
     │ if Z: (x, y, r*s)       │
     │                         │
     │ r = rate, s = recorded  │
     └─────────────────────────┘
```

---

## 5. Indirect Grab Two-Layer System

```
VR Hand Movement (Can be jittery)
    │
    │ Real-time tracking
    │
    ▼
┌───────────────────────────────┐
│      indirectTarget           │ ◄── Hand Transform
│   (Reference to Hand)         │
└───────────────────────────────┘
    │
    │ Smooth interpolation
    │ α = 1 - e^(-speed * Δt)
    │
    ▼
┌───────────────────────────────┐
│     Middle Layer              │
│  • middlePosition             │
│  • middleRotation             │
│  (Smoothed tracking)          │
└───────────────────────────────┘
    │
    │ Fixed offset (NO interpolation)
    │ position = middle + rotation * offset
    │
    ▼
┌───────────────────────────────┐
│     Object Transform          │ ◄── Final position
│  (Feels solid and stable)     │
└───────────────────────────────┘

Visual Comparison:

Direct Grab:
Hand ═══════════════════════▶ Object
     (Jitter transfers directly)

Indirect Grab:
Hand ──┬──┬──┬──▶ Middle ═══════▶ Object
       │  │  │     (Smooth)      (Stable)
   (Noisy input)
```

---

## 6. Grab Event Sequence Diagram

```
User      VR Controller    EditorPlayer    BeGrabAndScale    CommandHistory
 │             │                │                │                 │
 │ Press       │                │                │                 │
 │ Trigger     │                │                │                 │
 ├────────────▶│                │                │                 │
 │             │ Detect Press   │                │                 │
 │             ├───────────────▶│                │                 │
 │             │                │ OnGrabbed()    │                 │
 │             │                ├───────────────▶│                 │
 │             │                │                │ Set primaryHand │
 │             │                │                │ Record offset   │
 │             │                │                │                 │
 │ Press 2nd   │                │                │                 │
 │ Trigger     │                │                │                 │
 ├────────────▶│                │                │                 │
 │             │ Detect Press   │                │                 │
 │             ├───────────────▶│                │                 │
 │             │                │ OnGrabbed()    │                 │
 │             │                ├───────────────▶│                 │
 │             │                │                │ Set secondary   │
 │             │                │                │ Enter two-hand  │
 │             │                │                │                 │
 │             │                │                │ CreateCommand() │
 │             │                │                ├────────────────▶│
 │             │                │                │                 │ Store start
 │             │                │                │                 │ state
 │             │                │                │                 │
 │    [User moves hands - scaling happens in Update() loop]        │
 │             │                │                │                 │
 │ Release     │                │                │                 │
 │ Trigger     │                │                │                 │
 ├────────────▶│                │                │                 │
 │             │ Detect Release │                │                 │
 │             ├───────────────▶│                │                 │
 │             │                │ OnReleased()   │                 │
 │             │                ├───────────────▶│                 │
 │             │                │                │ Exit two-hand   │
 │             │                │                │                 │
 │             │                │                │CompleteCommand()│
 │             │                │                ├────────────────▶│
 │             │                │                │                 │ Execute &
 │             │                │                │                 │ add to stack
 │             │                │                │                 │
```

---

## 7. Axis Selection Visualization

```
Scenario 1: Pulling Along X-Axis
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        Y ↑
          │
          │    ┌───────────────┐
          │    │               │
          │    │   Object      │
          │    │               │
          │    └───────────────┘
          │
          └────────────────────────▶ X
         ╱
        ╱ Z

    Left Hand ◄─────────▶ Right Hand
              (X-aligned)

    angleX ≈ 0°  (MINIMUM)
    angleY ≈ 90°
    angleZ ≈ 90°
    
    → Scale X-Axis ✓


Scenario 2: Pulling Along Y-Axis
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        Y ↑ Right Hand
          │
          │    ┌───────────────┐
          │    │               │
          │    │   Object      │
          │    │               │
          │    └───────────────┘
          ↓ Left Hand
          └────────────────────────▶ X
         ╱
        ╱ Z

    angleX ≈ 90°
    angleY ≈ 0°  (MINIMUM)
    angleZ ≈ 90°
    
    → Scale Y-Axis ✓


Scenario 3: Pulling Along Z-Axis
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        Y ↑
          │
          │    ┌───────────────┐
          │    │               │
          │    │   Object      │
          │    │               │
          │    └───────────────┘
          │
          └────────────────────────▶ X
         ╱   ╱
        ╱   ╱ Left Hand
    Right  ╱
    Hand  ╱ (Z-aligned)
         Z

    angleX ≈ 90°
    angleY ≈ 90°
    angleZ ≈ 0°  (MINIMUM)
    
    → Scale Z-Axis ✓
```

---

## 8. Command Pattern Flow

```
┌──────────────────────────────────────────────────────────┐
│                  Scaling Operation                        │
└────────────────────┬─────────────────────────────────────┘
                     │
          ┌──────────▼──────────┐
          │  Enter Two-Hand     │
          │  Scaling Mode       │
          └──────────┬──────────┘
                     │
          ┌──────────▼──────────┐
          │  CreateScaleCommand │
          └──────────┬──────────┘
                     │
                     │ Record Initial State
                     ▼
          ┌────────────────────────────┐
          │   ScaleCommand Object      │
          │  ┌──────────────────────┐  │
          │  │ _startScale          │  │
          │  │ _startPosition       │  │
          │  │ _startRotation       │  │
          │  │ _scaleAxis = ?       │  │
          │  └──────────────────────┘  │
          │                            │
          │  ┌──────────────────────┐  │
          │  │ _endScale   = null   │  │
          │  │ _endPosition = null  │  │
          │  │ _endRotation = null  │  │
          │  └──────────────────────┘  │
          └────────────┬───────────────┘
                       │
        [User performs scaling]
                       │
          ┌────────────▼───────────┐
          │  Exit Two-Hand         │
          │  Scaling Mode          │
          └────────────┬───────────┘
                       │
          ┌────────────▼───────────┐
          │  CompleteScaleCommand  │
          └────────────┬───────────┘
                       │
                       │ Record Final State
                       ▼
          ┌────────────────────────────┐
          │   ScaleCommand Object      │
          │  ┌──────────────────────┐  │
          │  │ _startScale   ✓      │  │
          │  │ _startPosition ✓     │  │
          │  │ _startRotation ✓     │  │
          │  │ _scaleAxis = X ✓     │  │
          │  └──────────────────────┘  │
          │                            │
          │  ┌──────────────────────┐  │
          │  │ _endScale     ✓      │  │
          │  │ _endPosition  ✓      │  │
          │  │ _endRotation  ✓      │  │
          │  └──────────────────────┘  │
          └────────────┬───────────────┘
                       │
          ┌────────────▼────────────────┐
          │ CommandHistory.Instance     │
          │ .ExecuteCommand(cmd)        │
          └────────────┬────────────────┘
                       │
                ┌──────┴──────┐
                │             │
         ┌──────▼──────┐      │
         │  cmd.Execute()│     │
         │  (Confirm)   │      │
         └─────────────┘      │
                              │
         ┌────────────────────▼─────┐
         │  Add to Undo Stack       │
         └──────────────────────────┘

Now user can:
  • Press Ctrl+Z → cmd.Undo()  → Restore _startScale
  • Press Ctrl+Y → cmd.Execute() → Apply _endScale
```

---

## 9. Data Flow Diagram

```
Input Layer
═══════════
    [VR Controllers]
         │
         ├─ Position
         ├─ Rotation
         └─ Trigger State
         │
         ▼
Processing Layer
════════════════
    [EditorPlayer]
         │
         ├─ leftHand Transform
         ├─ rightHand Transform
         └─ grabbedObject tracking
         │
         ▼
Interface Layer
═══════════════
    [IGrabable]
         │
         ├─ OnGrabbed()
         ├─ OnReleased()
         └─ UnifiedGrab()
         │
         ▼
Logic Layer
═══════════
    [BeGrabAndScaleobject]
         │
         ├─ State Management
         │   ├─ isGrabbed
         │   ├─ isTwoHandScaling
         │   └─ isIndirectGrabbing
         │
         ├─ Transform Calculation
         │   ├─ Position (with offset)
         │   ├─ Rotation (with offset)
         │   └─ Scale (axis-aligned)
         │
         └─ Smooth Interpolation
             ├─ Exponential decay
             └─ Frame-independent
         │
         ▼
Command Layer
═════════════
    [ScaleCommand]
         │
         ├─ Record start state
         ├─ Record end state
         └─ Execute/Undo
         │
         ▼
History Layer
═════════════
    [CommandHistory]
         │
         ├─ Undo stack
         ├─ Redo stack
         └─ Execute queue
         │
         ▼
Output Layer
════════════
    [GameObject Transform]
         │
         ├─ position
         ├─ rotation
         └─ localScale
```

---

## 10. Performance Profile

```
Per Frame Operations
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Update() Method:
├─ State checks                    [Very Fast]  ●
├─ Transform calculations          [Fast]       ●●
├─ Vector/Quaternion math          [Fast]       ●●
├─ Lerp/Slerp interpolations       [Fast]       ●●
└─ Angle calculations (if scaling) [Moderate]   ●●●

Occasional Operations:
├─ OnGrabbed()                     [Fast]       ●●
├─ OnReleased()                    [Fast]       ●●
├─ CreateScaleCommand()            [Fast]       ●●
└─ CompleteScaleCommand()          [Fast]       ●●

Expensive Operations (Avoid):
└─ FindFirstObjectByType()         [Slow]       ●●●●●
    ↑
    Currently called multiple times!
    Should cache reference.

Legend:
● = ~0.01ms
●● = ~0.1ms
●●● = ~1ms
●●●● = ~10ms
●●●●● = ~100ms
```

---

## 11. Memory Layout

```
BeGrabAndScaleobject Instance
┌─────────────────────────────────────────┐
│ Unity MonoBehaviour Base                │
├─────────────────────────────────────────┤
│ Configuration (Serialized)              │
│  • positionSmoothSpeed  : float         │ 4 bytes
│  • rotationSmoothSpeed  : float         │ 4 bytes
│  • freezeYaxis          : bool          │ 1 byte
├─────────────────────────────────────────┤
│ Grab State                              │
│  • isGrabbed            : bool          │ 1 byte
│  • primaryHand          : Transform     │ 8 bytes (ref)
│  • secondaryHand        : Transform     │ 8 bytes (ref)
│  • offsetFromPrimary    : Vector3       │ 12 bytes
│  • rotationOffsetFrom.. : Quaternion    │ 16 bytes
├─────────────────────────────────────────┤
│ Indirect Grab State                     │
│  • indirectTarget       : Transform     │ 8 bytes (ref)
│  • middlePosition       : Vector3       │ 12 bytes
│  • middleRotation       : Quaternion    │ 16 bytes
│  • indirectGrabOffset   : Vector3       │ 12 bytes
│  • indirectGrabRotOff.. : Quaternion    │ 16 bytes
│  • isIndirectGrabbing   : bool          │ 1 byte
│  • indirectRotationTar..: Transform     │ 8 bytes (ref)
├─────────────────────────────────────────┤
│ Scaling State                           │
│  • isTwoHandScaling     : bool          │ 1 byte
│  • initialHandsDistance : float         │ 4 bytes
│  • baseScale            : Vector3       │ 12 bytes
│  • twoHandRotationOff.. : Quaternion    │ 16 bytes
│  • isNewScaleGesture    : bool          │ 1 byte
│  • scaleAxisData        : ScaleAxisData │ 8 bytes (ref)
│  • recordScale          : float         │ 4 bytes
│  • recordHandDistance   : float         │ 4 bytes
│  • lastScaleAxis        : ScaleAxis?    │ 8 bytes
├─────────────────────────────────────────┤
│ Command State                           │
│  • currentScaleCommand  : ScaleCommand  │ 8 bytes (ref)
│  • isCommandActive      : bool          │ 1 byte
├─────────────────────────────────────────┤
│ References                              │
│  • editorPlayer         : EditorPlayer  │ 8 bytes (ref)
└─────────────────────────────────────────┘

Total: ~202 bytes + Unity overhead
(Excluding referenced objects)

Note: Actual memory usage is higher due to:
  • Unity's internal data structures
  • Referenced objects (Transform, EditorPlayer, etc.)
  • Managed heap allocations
```

---

## Summary

These diagrams illustrate:

1. **Architecture** - How components fit together
2. **State Machine** - Grab state transitions
3. **Flow Charts** - Algorithm execution paths
4. **Visualizations** - Spatial relationships
5. **Sequences** - Event timing
6. **Data Flow** - Information movement
7. **Performance** - Resource usage

Use these diagrams alongside the code to understand the complete system!

