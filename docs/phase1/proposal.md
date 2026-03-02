# Phase 1 Proposal — 3D Vertical Slice  
**Game:** PITHOX (3D adaptation)
**Engine:** Unity (C#) 
**Platform:** PC (Steam)   
**Team:** Team 7 

---

## 1) What are we building? (start → end, player experience, core loop)

### Scene summary (vertical slice)
**Scene name:** *Night 1*  
**Goal:** Survive escalating waves, level up some skills and defeat the **Night Mini-Boss**.  
**Setting:** A **moonlit forest clearing** with **PITHOX at the center** (now built as a 3D diorama/arena).
#### Start boundary (trigger)
- Player starts at the center of the stage right next to Pithox.
- On pressing start button, **Night begins** and monsters start approaching.

#### End boundary (success)
- Mini-Boss defeated → results panel (time survived, highest combo and other stats).

#### Failure states
- Player HP reaches 0 → defeat screen (restart run).

### Player experience (what it feels like)
A fast, readable **manual-combat** survival arena where the player takes risks: kill enemies → pick up a tombstone (movement penalty) → carry it to the pot under pressure → choose an upgrades on level up to get stronger.   

### Core loop (in the slice)
**Fight → Tombstone drop → Carry → Offer at PITHOX → Choose an upgrades → Repeat → Boss**   

---

## Benchmark reference (1 target scene video)
We will match the pacing + clarity of a single-room combat encounter with a mini-boss finish.

```text
Target benchmark scene video (watch & deconstruct):
https://www.youtube.com/watch?v=91t0ha9x0AE  (Hades — combat room encounter example)
