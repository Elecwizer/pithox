# Phase 3 Final Report — PITHOX (3D Vertical Slice)

## Team 7
- Abdulmohsen — Team Lead, Music Composer, Artist  
- Oukba — Combat Designer, Programmer  
- Ali — Gameplay Designer, Programmer  
- Hassan — Programmer, Enemy Designer  

---

# 1. Final Build Summary

## Build Information
- Engine: Unity
- Platform: PC 

## How to Run
1. Download the build folder
2. Extract the `.zip` file
3. Use Unity version 6000.4.5f1
4. Go to Scenes folder and open the Gameplay scene

## Controls
- Light attack: left mouse click / R1
- Heavy attack: right click / R2
- Move: WASD / left joystick
- Direction: cursor / right joystick
- Skills (learned through level up): E & Q / L1 & L2
- Pick up: space / X
- Dash: shift / O

---

# 2. Final Playable Experience

## Start → End Gameplay Flow

### Start Boundary
- Player spawns beside **PITHOX** in the center of the moonlit arena.

### Core Gameplay Loop
The final gameplay loop is fully implemented:

**Fight enemies → Collect tombstones → Carry offerings → Deliver to PITHOX → Gain upgrades → Survive stronger waves → Defeat the Night Boss**

### End Boundary (Victory)
- Defeating the Night Boss triggers:
  - Results screen
  - Survival time
  - Highest combo
  - Final score statistics

### Failure Condition
- Player HP reaches 0
- Defeat screen appears
- Restart option available immediately

---

# 3. Final Systems Implementation

## Core Gameplay Systems Engineering
- Responsive player movement and combat
- Stable game-state transitions
- Fully connected win/fail flow
- Upgrade progression integrated into gameplay loop
- Combo and scoring systems implemented

## Physics & Collision Systems
- Reliable hit detection using Unity colliders
- Stable enemy/player collision handling
- Deterministic combat interactions
- No major clipping or overlap issues observed in final testing

## AI Behavior Design
Enemy and boss AI use state-driven logic:

### Enemy States
- Idle
- Chase
- Attack
- Stagger
- Death

### Boss States
- Intro
- Chase
- Attack Phase 1
- Attack Phase 2
- Enraged State
- Death

Enemy telegraphs and attack timing were improved for gameplay readability and fairness.

---

# 4. Visual Production Pass

## Environment & Lighting
- Moonlit forest arena designed for high readability
- nighttime raining used to reinforce atmosphere
- Central PITHOX structure illuminated for player guidance
- Ambient lighting added for depth

## Materials & Shaders
- Stylized materials applied across environment and enemies
- Emissive materials used for:
  - PITHOX glow
  - Enemy attack telegraphs
  - Upgrade feedback

## VFX
Implemented visual effects include:
- Attack hit sparks
- Enemy death particles
- Offering effects
- Boss attack effects
- Screen shake and impact flashes

## Animation
- Player movement and attack animations polished
- Enemy attack telegraphs improved
- Boss animations synced with attack timing

## Camera Design
- Smooth top-down tracking camera
- Camera damping implemented for smoother movement
- Minor camera shake added for combat feedback

## Post-Processing
Post-processing stack includes:
- Bloom
- Color grading
- Vignette
- Ambient occlusion

The final visual pass significantly improved scene readability and gameplay feedback.

---

# 5. Audio Production Pass

## Music Integration
- Dynamic background music implemented
- Boss encounter includes dedicated music track
- Audio transitions smoothly between gameplay states

## Sound Effects
Implemented SFX include:
- Combat hits
- Enemy death sounds
- Tombstone pickup
- Offering interactions
- UI interactions
- Boss attacks

## Audio Mixing
- Music and SFX balanced for gameplay clarity
- Combat sounds prioritized during intense encounters
- Volume normalization applied across systems

## Spatial / 3D Audio
Spatial audio was used selectively for:
- Enemy attack positioning
- Boss attacks
- Offering interactions near PITHOX

This improved player awareness during crowded combat situations.

---

# 6. UI / UX Feedback Pass

## HUD Systems
Final HUD includes:
- Player HP
- Combo counter
- Current score
- Upgrade prompts

## Feedback Improvements
- Stronger hit feedback added
- Damage flashes improved readability
- Upgrade selection flow streamlined
- Clearer interaction prompts added

## Accessibility & Readability
- UI contrast improved
- Font readability increased
- Important gameplay elements highlighted visually

---

# 7. Performance & Optimization Report

## Target Hardware
- Windows PC
- Mid-range GPU
- 1080p resolution

## Final Performance Metrics

| Scenario | Average FPS |
|---|---|
| Normal gameplay | 60 FPS |
| Heavy enemy waves | 56–60 FPS |
| Boss fight | 55–60 FPS |

## Profiling & Bottlenecks Identified

### Major Bottlenecks
| Issue | Cause | Fix Applied |
|---|---|---|
| Enemy spawn spikes | Frequent instantiation | Object pooling |
| Particle overload | Excess VFX emission | Reduced particle count |
| UI update overhead | Constant updates | Event-driven UI refresh |

## Optimization Techniques Applied
- Object pooling for enemies and particles
- Reduced unnecessary Update() calls
- Optimized particle systems
- Simplified collision checks
- Reduced overdraw in environment assets

## Stability Results
- No major crashes during final testing
- Stable framerate maintained during full runs
- Memory usage remained consistent over extended sessions

---

# 8. Bug Triage & Stability Log

| Issue | Severity | Resolution |
|---|---|---|
| Enemy pathing overlap | Medium | Fixed enemy spacing logic |
| Tombstone interaction inconsistency | Medium | Adjusted interaction radius |
| Boss attack hitbox mismatch | High | Corrected collider timing |
| UI overlap on small resolutions | Low | Responsive scaling added |
| Audio clipping during combat | Medium | Rebalanced audio mix |

---

# 9. Repository & Production Workflow

## GitHub Workflow
- Kanban boards used for milestone tracking
- Tasks organized by Phase 2 and Phase 3
- Iterative workflow maintained throughout development

## Repository Cleanup
- Project folders reorganized
- Unused assets removed
- README updated with:
  - Controls
  - Build instructions
  - Team roles
  - Project summary

## Release Management
- Final release candidate tagged
- Backup build archived
- Known issues documented

---

# 10. Final Presentation Package

## Live Demo Plan
1. Introduce gameplay concept
2. Demonstrate:
   - Combat
   - Tombstone mechanic
   - Upgrade system
   - Boss encounter
3. Showcase win/lose flow
4. Discuss technical implementation
5. Present optimization results

## Fallback Demo Video
- Recorded full gameplay session included
- Demonstrates complete run from start to finish

---

# 11. Team Postmortem

## What Worked Well
- Strong communication between programmers and artist
- Core gameplay loop became fun early in development
- Iterative playtesting improved combat feel significantly
- Kanban workflow helped task visibility and organization

## Challenges Faced
- Balancing enemy difficulty took longer than expected
- Boss implementation required multiple redesigns
- UI polish was delayed until late production

## Lessons Learned
- Early prototyping helped validate the core loop quickly
- Playtesting feedback was critical for balancing
- Optimization should begin earlier in development

## What We Would Add Next
Given more time, future improvements would include:
- Additional enemy types
- More upgrade paths
- Multiple nights/stages
- Expanded boss mechanics
- Settings and accessibility menu
- Steam integration and save system

---

# 12. Competency Coverage

## Core Gameplay Systems Engineering
- Fully implemented player controls, combat, progression, and state flow

## Physics & Collision Systems
- Stable and reliable interaction systems implemented across all gameplay elements

## AI Behavior Design
- State-driven enemy and boss behaviors with readable telegraphs

## Real-Time Graphics Pipeline
- Lighting, materials, shaders, and rendering configured for readability and atmosphere

## Technical Art & Polish
- VFX, animation, camera feedback, and post-processing polished for final presentation

## Audio Systems Design
- Event-driven SFX and layered music integrated with gameplay states

## UI/UX Feedback
- HUD and gameplay feedback systems improved for readability and player clarity

## Performance Optimization & Debugging
- Profiling, optimization, and bug triage documented and applied successfully

## Production & Collaboration Practices
- GitHub workflow, Kanban tracking, milestone planning, and iterative delivery maintained throughout development

---

# Conclusion

PITHOX successfully delivers a polished and technically defensible 3D vertical slice experience.

The final build contains:
- A complete gameplay loop
- Stable combat and progression systems
- Polished visuals and audio
- Optimized and testable gameplay
- A professional production workflow
