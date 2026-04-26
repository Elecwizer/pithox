# Phase 2 Checkpoint — PITHOX (3D Vertical Slice)

## Team 7
- Abdulmohsen — Team Lead, Music Composer, Artist  
- Oukba — Combat Designer, Programmer  
- Ali — Gameplay Designer, Programmer  
- Hassan — Programmer, Enemy Designer  

---

# 1. Playable Slice Summary

## Start → End Flow

The current vertical slice implements a **complete end-to-end gameplay loop**:

### Start Boundary
- Player spawns at the center of the arena near **PITHOX**.
- Player presses **Start** → timer begins.
- **Wave 1 enemies spawn** from arena edges.

### Core Loop (Implemented)
- Player fights enemies using manual combat.
- Enemies drop **tombstones** upon death.
- Player picks up tombstones (movement is slowed).
- Player delivers tombstones to **PITHOX**.
- Player selects an **upgrade** from a choice panel.
- Loop repeats with increasing difficulty.

### End Boundary (Success)
- Night ends → **Results Screen displayed**
  - Time survived
  - Highest combo
  - Total score

### Failure Condition
- Player HP reaches 0 → **Defeat Screen**
- Option to restart the run

---

## Core Loop Status

The core loop is:
- Fully connected  
- Playable from start to end  
- Stable under normal play conditions  

---

## Win / Fail Logic

| State | Condition | Result |
|------|----------|--------|
| Win | Night ends | Results screen |
| Fail | Player HP = 0 | Defeat screen + restart |

---

## Known Limitations

- Limited enemy variety (3 types implemented)
- No level up
- No Boss
- No Upgrades 
- UI visuals are functional but not fully polished
- Audio is first-pass only
- No settings panel

---

# 2. Systems Implementation Status

## Player Controls & Interaction
- WASD movement with directional facing
- Responsive movement system
- HP and damage handling implemented
- Interaction with tombstones and PITHOX works correctly

## Physics & Collision
- Player, enemy, and attack collisions implemented using Unity colliders
- Hit detection is consistent and reliable
- No major clipping or missed hit issues observed

## Combat System
- Manual attack system with cooldown
- Combo system increases with consecutive hits
- Enemy knockback and hit feedback implemented

## AI Behavior System
- Enemies use **state-based behavior**:
  - Idle → Chase → Attack → Death
- Boss uses:
  - Multiple attack phases
  - Telegraph-based attacks for readability

## Game State Flow
- Start → Gameplay → Defeat waves → Night ends (Win/Fail) fully implemented
- Game states are stable and transition correctly

---

# 3. UI + Audio First Pass

## UI Systems
- Player HUD:
  - HP display
  - Timer
  - Combo counter
- Win/Lose result screens implemented

## Audio Systems
- Background music (looped)
- Attack and hit sound effects
- Enemy death sounds
- Tombstone pickup and offering sounds

---

# 4. Playtest Report

## Test Setup
- Internal team playtesting sessions conducted

---

## Key Findings

### Gameplay
- Tombstone mechanic created meaningful risk/reward decisions
- Combat felt responsive but slightly repetitive (not enough variety yet)

### Difficulty
- Early waves: too easy
- Mid-game: still kinda easy but noticable difference
- Late at night: slightly challenging

### UI
- We need to add more UI elements other than player HP for player feedback

### Controls
- Movement felt smooth
- Might need clearer attack feedback 

---

## Issues Identified

| Issue | Severity | Status |
|------|--------|--------|
| Enemy spawn rate too low early | Medium | Fixed |
| Need More variety | High | Will be added in phase 3 |
| Tombstone not always visible | Medium | Fixed |
| UI readability issues | High | Needs improvement by phase 3 |

---

# Conclusion

The Phase 2 vertical slice successfully delivers a **fully playable, end-to-end gameplay concept experience** with all core systems implemented and integrated.

The project is stable, testable, and ready for **polish and optimization in Phase 3**.