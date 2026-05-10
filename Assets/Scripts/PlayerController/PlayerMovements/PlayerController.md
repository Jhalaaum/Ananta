# First-Person Character Controller

A clean Unity first-person character controller with movement, jumping, sprint stamina, crouching, crawling, camera polish, and leaning support.

Built using Unity’s `CharacterController` and the new `Input System`.

## Features

### Player Movement
- WASD movement
- Smooth acceleration and deceleration
- Better direction-change response
- Air movement control
- Gravity and falling polish

### Jumping
- Space to jump
- Coyote time support
- Jump buffering
- Held-jump repeat delay

### Sprint System
- Hold Shift to sprint
- Sprint builds up smoothly
- Sprint energy drains over time
- Exhaustion system
- Smooth sprint slowdown before recovery
- Delayed stamina recovery after sprinting

### Stance System
- Press `C` to crouch
- Press `X` to crawl
- Smooth height transitions
- Prevents standing up when blocked by ceilings or obstacles

### Camera System
- Mouse look
- Camera pitch clamp
- Smooth head bob
- Sprint FOV kick
- Camera height adjusts based on stance

### Leaning
- Press `Q` to lean left
- Press `E` to lean right
- Camera tilts while leaning
- Camera physically moves sideways
- SphereCast collision check prevents leaning through walls

## Scripts

### `PlayerMovement.cs`

Handles most of the character logic:

- Movement
- Sprinting
- Jumping
- Gravity
- Crouch/crawl
- Camera height
- Head bob
- FOV kick
- Lean position movement
- Lean collision checks

### `MouseLook.cs`

Handles camera rotation:

- Mouse X rotates the player body
- Mouse Y rotates the camera up/down
- Q/E apply camera roll for leaning

The sideways lean movement is handled in `PlayerMovement.cs`, while the visual camera tilt is handled in `MouseLook.cs`.

## Unity Setup

Use this hierarchy:

```text
Player
├── CharacterController
└── Main Camera


---

### Setup Steps

#### 1. Player Object
Attach: PlayerMovement.cs

Assign: Controller → CharacterController, Player Camera → Main Camera (Transform), Camera Component → Main Camera (Camera)


---

#### 2. Main Camera
Attach: MouseLook.cs

Assign: Player Body → Player Transform


---

## 🎮 Controls

| Action        | Key |
|--------------|-----|
| Move         | WASD |
| Look         | Mouse |
| Jump         | Space |
| Sprint       | Shift |
| Crouch       | C |
| Crawl        | X |
| Lean Left    | Q |
| Lean Right   | E |

---

## ⚙️ Layer Setup (IMPORTANT)

Create a **Player layer** and assign it to the player.

### These masks:
- `Obstruction Mask`
- `Lean Obstacle Mask`

👉 Should include:
Ground / Environment / World

👉 Should NOT include:
Player

This prevents:
- Self-collision during lean
- Failed stand-up checks

---

## 🔧 Recommended Settings
Walk Speed: 6
Sprint Speed: 18
Crouch Speed: 3
Crawl Speed: 2

Sprint Build Time: 4
Max Sprint Duration: 6
Sprint Recovery Time: 6
Sprint Slowdown Time: 4

Lean Distance: 0.7
Lean Speed: 10
Lean Check Radius: 0.2

Normal FOV: 70
Sprint FOV: 78

---

## 🧠 How Leaning Works

Leaning is split into two systems:
PlayerMovement.cs → camera sideways movement
MouseLook.cs → camera tilt (visual roll)

✔ Prevents conflicts with:
- Head bob
- Camera height
- Crouch / crawl
- FOV effects

---

## 💡 Notes

This controller is:
- Clean
- Expandable
- Good for prototypes → can scale toward AA-level systems

---

## 🔮 Possible Improvements

- Input System Actions (instead of raw keyboard)
- Stamina UI bar
- Footstep sounds
- Camera shake / recoil
- Controller (gamepad) support
- Animation system integration

---

## 📌 Summary

A solid, production-style base for:
- Stealth games
- FPS prototypes
- Exploration systems
