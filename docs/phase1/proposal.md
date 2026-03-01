# Phase 1 Proposal — 3D Vertical Slice  
**Game:** PITHOX / “The Whispering Pot” (3D adaptation) :contentReference[oaicite:0]{index=0} :contentReference[oaicite:1]{index=1}  
**Engine:** Unity (C#) :contentReference[oaicite:2]{index=2}  
**Platform:** PC (Steam)   
**Team:** Team 7 :contentReference[oaicite:4]{index=4}  

---

## 1) What are we building? (start → end, player experience, core loop)

### Scene summary (vertical slice)
**Scene name:** *Night 1 — The Clearing*  
**Goal:** Survive escalating waves, make at least **3 successful tombstone offerings**, and defeat the **Night Mini-Boss**.  
**Setting:** A **moonlit forest clearing** with **PITHOX at the center** (now built as a 3D diorama/arena). :contentReference[oaicite:5]{index=5}  

#### Start boundary (trigger)
- Player starts at the edge of the clearing at dusk.
- On entering the arena radius, **Night begins** and Wave 1 spawns.

#### End boundary (success)
- Mini-Boss defeated → short “calm” moment + results panel (time survived, offerings delivered, upgrades chosen).

#### Failure states
- Player HP reaches 0 → defeat screen (restart run).
- Optional: “Abandon run” via pause menu (counts as fail).

### Player experience (what it feels like)
A fast, readable **manual-combat** survival arena where the player takes risks: kill enemies → pick up a tombstone (movement penalty) → sprint it to the pot under pressure → choose one of two upgrades and immediately feel stronger.   

### Core loop (in the slice)
**Fight → Tombstone drop → Carry (slowed) → Offer at PITHOX → Choose 1 of 2 upgrades → Repeat → Boss**   

**Player verbs:** move, attack, dash, pick up/drop, offer, choose upgrade. :contentReference[oaicite:8]{index=8}  

---

## Benchmark reference (1 target scene video)
We will match the pacing + clarity of a single-room combat encounter with a mini-boss finish.

```text
Target benchmark scene video (watch & deconstruct):
https://www.youtube.com/watch?v=91t0ha9x0AE  (Hades — combat room encounter example)