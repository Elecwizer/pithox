# Phase 1 Proposal — 3D Vertical Slice  
**Game:** PITHOX (3D adaptation)  
**Engine:** Unity 
**Platform:** PC (Steam)  
**Team:** Team 7  

**Team members & roles**
- **Abdulmohsen** — Team Lead, Music Composer, Artist  
- **Oukba** — Combat Designer, Programmer  
- **Ali** — Gameplay Designer, Programmer  
- **Hassan** — Programmer, Enemy Designer  

---

## 1) What are we building? (start → end, player experience, core loop)

### Scene summary (vertical slice)
**Scene name:** *Night 1*  
**Goal:** Survive escalating waves, deliver offerings at PITHOX to gain upgrades, and defeat the **Night Mini-Boss**.  
**Setting:** A **moonlit forest clearing** with **PITHOX at the center** (3D arena/diorama).

#### Start boundary
- Player spawns **at the center of the stage next to PITHOX**.
- On pressing start button, the timer begins and Wave 1 spawns at the arena edges.

#### End boundary (success)
- Mini-Boss defeated → results panel (time survived, highest combo and other stats).

#### Failure states
- Player HP reaches 0 → defeat screen with stats → restart.

### Player experience 
A fast, readable **manual-combat** survival arena. The player balances aggression and risk:
- Kill enemies to create space
- Grab a tombstone drop (movement penalty while carrying)
- Deliver it to PITHOX to earn **an upgrade choice**
- Get stronger in the same night and survive to the boss

### Core loop (in the slice)
**Fight → Tombstone drop → Carry (slow) → Offer at PITHOX → Choose upgrade → Repeat → Boss**

---

## 2) How will we build it? (scope, risks, tasks, workflow)

## Benchmark reference (scene video + deconstruction template)

### Benchmark videos
Hades II

Link:
[https://www.youtube.com/watch?v=91t0ha9x0AE](https://youtu.be/7m1qUqlZ_0g?si=TtU9ezge3KXf4-Kg&t=1166)

Vampire Survivors

Link:
https://youtu.be/wgYu6lLi6cE?si=GCSiB7QKdG6W5qmW&t=414

What we are copying (replicate):
- Clear start/end of a single-room encounter
- Readable enemy telegraphs and hit feedback
- “Fight → reward choice → continue pressure” rhythm

What we are simplifying (to fit semester scope):
- Fewer enemy variations (3 types + mini-boss)
- Simpler VFX and animation (placeholder-first)
- Smaller UI set (HP, cooldowns, upgrade choice only)

Systems to break down from the video (we will fill these for each new benchmark):
- Controls & camera
- Combat & hit detection
- Enemy AI behaviors & spawning
- UI feedback & prompts
- VFX / animation / audio cues
- Win/lose state flow
- Performance considerations (spawns, particles, pooling)
