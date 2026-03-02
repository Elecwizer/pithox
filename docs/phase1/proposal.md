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

### Scope
Must / Nice / NOT doing list:
- Must: one full night, enemies, upgrade system, night 1 boss, background music, visual feedback cues
- Nice: boss music, audio feedback ques, extra enemies, extra playstyle, secret end boss, enhanced visuals and UI
- NOT doing: main hub, weapons, extra nights, co-op

### production plans
Week 9: prototype of small arena with pithox in the middle that can get tombstones as offering with score being tracked
- Abdulmohsen: create initial assets
- Oukba: develop scoring and combo system
- Ali: develop player movement 
- Hassan: develop tombstones mechanic
  
Week 10: prototype of small arena with pithox in the middle that can get tombstones as offering with score being tracked
- Abdulmohsen: enhance the assets, add UI and some audio
- Oukba: start working on combat system
- Ali: develop enemies
- Hassan: help in enemies and mini boss development

Week 11: prototype of small arena with pithox in the middle that can get tombstones as offering with score being tracked
- Abdulmohsen: finish up initial assets for current enemies, mini boss arena, UI and audio
- Oukba: finish up combat system with help from play testing feedback
- Ali: finish up enemies and miniboss with help from play testing feedback
- Hassan: general help in lacking areas and proof test game for bugs or issues to be caught early

### Benchmark videos
Hades II

Link:
[https://www.youtube.com/watch?v=91t0ha9x0AE](https://youtu.be/7m1qUqlZ_0g?si=TtU9ezge3KXf4-Kg&t=1166)

Vampire Survivors

Link:
https://youtu.be/wgYu6lLi6cE?si=GCSiB7QKdG6W5qmW&t=414

What we are replicate:
- Clear start/end of a single-night encounter
- Readable enemy telegraphs and hit feedback
- “Fight → reward choice → continue pressure” rhythm

What we are simplifying (to fit semester scope):
- Fewer enemy variations 
- Simpler VFX and animation 
- Less detailed UI

Systems to break down from the videos:
- Controls & camera
- Combat & hit detection
- Enemy AI behaviors & spawning
- UI feedback & prompts
- VFX / animation / audio cues
- Win/lose state flow
- Performance considerations (spawns, particles, pooling)
