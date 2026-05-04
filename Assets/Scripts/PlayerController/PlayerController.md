# Player Movement Controller

This script controls a first-person player using Unity’s `CharacterController`.

It includes:

- Walking
- Sprinting
- Crouching
- Crawling
- Jumping
- Gravity
- Smooth acceleration
- Smooth camera height changes
- Head bob
- Sprint FOV effect
- Safe stand-up collision checks

---

## What You Can Edit

### References

These connect the script to objects in your scene.

| Setting | What it does |
|---|---|
| `Controller` | The player’s `CharacterController` component |
| `Player Camera` | The camera used for first-person view |
| `Camera Component` | The actual `Camera` component for FOV effects |

---

## Movement Speeds

These control how fast the player moves.

| Setting | What it does |
|---|---|
| `Walk Speed` | Normal movement speed |
| `Sprint Speed` | Speed while sprinting |
| `Crouch Speed` | Speed while crouching |
| `Crawl Speed` | Speed while crawling |

Lower values make the game feel slower and more stealthy.  
Higher values make it feel more action-based.

---

## Acceleration

These control how smooth or responsive movement feels.

| Setting | What it does |
|---|---|
| `Grounded Acceleration` | How fast the player reaches full speed on the ground |
| `Grounded Deceleration` | How fast the player stops on the ground |
| `Air Acceleration` | How much control the player has in the air |
| `Air Deceleration` | How fast the player slows down in the air |
| `Air Control Multiplier` | Reduces movement control while jumping/falling |
| `Direction Change Boost` | Makes turning in the opposite direction feel sharper |

Higher acceleration = snappier movement.  
Lower acceleration = heavier, smoother movement.

---

## Jump & Gravity

These control jumping and falling.

| Setting | What it does |
|---|---|
| `Jump Height` | How high the player jumps |
| `Gravity` | How strongly the player is pulled down |
| `Grounded Stick Force` | Keeps the player attached to the ground |
| `Coyote Time` | Allows jumping shortly after leaving the ground |
| `Jump Buffer Time` | Allows a jump input just before landing |
| `Fall Multiplier` | Makes falling feel heavier |
| `Held Jump Repeat Delay` | Delay between repeated jumps when Space is held |

For a realistic stealth game, use a lower jump height.  
For an action game, use a higher jump height.

---

## Stance Heights

These control how tall the player is in each stance.

| Setting | What it does |
|---|---|
| `Standing Height` | Player height when standing |
| `Crouch Height` | Player height when crouching |
| `Crawl Height` | Player height when crawling |
| `Stance Transition Speed` | How quickly the player changes height |
| `Eye Level Ratio` | Camera height compared to body height |

A lower crouch/crawl height lets the player fit under smaller spaces.

---

## Sprint System

These control sprint stamina and sprint feel.

| Setting | What it does |
|---|---|
| `Sprint Build Time` | How long it takes to reach full sprint speed |
| `Max Sprint Duration` | How long the player can sprint |
| `Sprint Recovery Time` | How long sprint stamina takes to recover |
| `Exhausted Speed Multiplier` | How slow the player becomes when tired |

Lower sprint duration makes sprinting feel more limited.  
Higher recovery time makes stamina more punishing.

---

## Camera Polish

These settings make movement feel more polished.

| Setting | What it does |
|---|---|
| `Enable Head Bob` | Turns camera bobbing on/off |
| `Walk Bob Amount` | Camera movement while walking |
| `Sprint Bob Amount` | Camera movement while sprinting |
| `Crouch Bob Amount` | Camera movement while crouching/crawling |
| `Bob Frequency` | Speed of the camera bob |
| `Enable FOV Kick` | Turns sprint FOV effect on/off |
| `Normal FOV` | Camera FOV normally |
| `Sprint FOV` | Camera FOV while sprinting |
| `FOV Smooth Speed` | How smoothly the FOV changes |

Small camera effects make the game feel more professional.  
Too much head bob can make players uncomfortable.

---

## Collision Checks

These stop the player from standing up inside ceilings or objects.

| Setting | What it does |
|---|---|
| `Obstruction Mask` | Layers that block standing up |
| `Stand Check Radius Padding` | Makes the stand-up check slightly smaller |
| `Stand Check Floor Offset` | Prevents the floor from blocking stand-up |

Make sure the `Obstruction Mask` does **not** include the Player layer.

---

## Controls

| Key | Action |
|---|---|
| `WASD` | Move |
| `Shift` | Sprint |
| `Space` | Jump |
| `C` | Crouch / Stand |
| `X` | Crawl / Stand |

---

## Recommended Stealth Settings

Good starting values for a slower stealth game:

```text
Walk Speed: 5.5
Sprint Speed: 10
Crouch Speed: 2.5
Crawl Speed: 1.4
Jump Height: 1.5
Gravity: -30
