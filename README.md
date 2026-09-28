# Dragon Battle — Unity Technical Assessment

A 2.5D top-down dragon battle prototype featuring a player-controlled Red Dragon and an AI-controlled Black Dragon.

## Links
- Source Code: [https://github.com/bhartinderjoshi/Dragon-Game.git]
- Windows Build: [https://drive.google.com/drive/folders/1Sln2kyJFPjBnXsavomejrZSK1FIh2F_2?usp=sharing]
- Gameplay Video: [Video Link]

## Unity
**Unity 6.5 (6000.5.4f1)**

## Controls
- `WASD` — Movement
- `1` — Fire Attack
- `2` — Tail Attack
- `3` — Fly Attack

## Core Systems
- Modular `DragonController` base class
- `PlayerDragonController` for player input
- `DragonAIController` with hand-coded Finite State Machine
- AI states: `Idle`, `Chase`, `Attack`
- Distance-based ability selection
- Independent ability cooldowns
- `AbilityBase` with separate Fire, Tail, and Fly abilities
- Event-based health and ability/UI synchronization
- Procedural Bootstrap for runtime initialization
- Game Manager and UI Manager
- Health bars, cooldown UI, VFX, particles, sound, winner/defeat screen

## Development Process
Started with ideation and visual/gameplay references, followed by planning the architecture around a modular structure. The initial prototype was divided into State Machines, Base Classes, Game Manager, and UI Manager. VFX, particles, sound, and final UI were added during refinement.

## AI Usage
- **Antigravity — Gemini 3.7 Flash:** Prototyping, code generation, and refinement.
- **ChatGPT:** Ideation, planning, architecture, and problem solving.
- **Pinterest:** Visual references and inspiration.

Most of the initial code was AI-assisted, but the architecture and implementation were reviewed and controlled manually. Debugging and integration issues were tested and fixed manually.

# Development Process & Implemented Work

The project was developed iteratively, starting from a basic gameplay concept and gradually moving toward a more complete and polished playable prototype.

### 1. Initial Concept & Prototyping

* Started by defining the core gameplay idea and planning how the arena, dragons, combat, UI, and overall gameplay flow should work.
* Created an initial prototype to validate the basic gameplay concept before spending time on detailed assets and visual polish.
* Focused the early development stage on getting the core gameplay loop functional and testable.

### 2. Procedural Arena Prototype

* The first playable arena was created as a procedural environment to quickly test gameplay without depending on finalized environment assets.
* Used the procedural arena for initial gameplay testing, iteration, and validation of the core systems.
* This allowed the gameplay mechanics to be tested and adjusted before moving toward the final environment.

### 3. Initial Dragon Implementation

* Started with simple placeholder dragon models made from basic primitives such as cubes.
* Used these placeholder dragons to test movement, gameplay interactions, combat logic, and overall game flow.
* This helped separate gameplay implementation from the final visual asset integration.

### 4. UI Implementation

* Added and integrated the required gameplay UI.
* Connected the UI with the relevant gameplay systems and configured it to work with the prototype.
* Tested the UI alongside the gameplay systems to ensure the required information and interactions were available during gameplay.

### 5. Final Dragon Asset Integration

* After the core prototype and gameplay systems were working, replaced the basic placeholder dragons with the final dragon assets.
* Adapted the existing implementation to work with the new dragon models.
* Verified the integration of the final assets with the existing gameplay, combat, and interaction systems.

### 6. Dragon Animation Setup

* Set up the dragon animation system after integrating the final models.
* Added and configured the required animation states and connected them with the gameplay implementation.
* Worked on integrating animations with the relevant dragon actions because animation handling was not included in the initial code implementation.

### 7. Combat & Fire Attack

* Implemented and integrated the dragon's fire attack functionality.
* Tested the attack during gameplay and identified issues with the initial fire-attack implementation.
* Debugged and refined the related gameplay logic to make the attack function correctly within the overall combat system.

### 8. Particle & Visual Effects

* Added particle-based effects for combat and gameplay feedback.
* Experimented with different particle effects to improve the visual presentation of attacks.
* Identified areas where the particle effects could still be visually refined further.
* Prioritized functional gameplay and core implementation first due to the limited development time.

### 9. Environment & Arena Creation

* Created the main game environment and arena using Blender.
* Used Python scripting to procedurally create and configure parts of the environment.
* Used Kiro as an AI-assisted development tool while working on the Blender Python scripting workflow.
* Iterated on the environment structure to create a suitable arena for the gameplay prototype.

### 10. Environment Design & Visual References

* Used visual references and idea exploration from sources such as Pinterest, Gemini, and ChatGPT while developing the environment and overall visual direction.
* Used these references to explore suitable environmental elements, composition, and visual presentation.
* Combined the references with the technical requirements of the gameplay prototype.

### 11. Debugging & Integration

* Encountered and resolved multiple implementation and integration issues throughout development.
* Debugged problems involving gameplay logic, dragon integration, fire attacks, animations, UI, and visual effects.
* Iteratively tested different systems together instead of treating each feature as an isolated implementation.
* Refined the project as new systems and final assets were introduced.

### 12. Runtime & Implementation Refinement

* Removed unnecessary complexity encountered during the development process.
* Simplified parts of the implementation where possible to keep the project easier to maintain and iterate on.
* Focused development time primarily on gameplay functionality, system integration, debugging, and overall playability.

### 13. Development Priorities

* The majority of the available development time was dedicated to gameplay and core technical implementation.
* Priority was given to getting the complete gameplay flow working rather than spending excessive time on secondary visual polish.
* Additional visual improvements and optimization opportunities were identified during development for future refinement.

---

# Current State

* Core gameplay prototype implemented and tested.
* Procedural arena prototype created and used during the initial testing phase.
* Main gameplay arena/environment created in Blender using Python scripting.
* Basic placeholder dragon models successfully used for early gameplay testing.
* Final dragon assets integrated into the project.
* Dragon animations configured and connected to gameplay.
* Gameplay UI implemented and integrated.
* Dragon combat functionality implemented.
* Fire attack functionality implemented and debugged.
* Particle effects added for combat and visual feedback.
* Gameplay systems tested together after integrating the final assets.
* Multiple coding and integration issues addressed during development.
* Unnecessary implementation complexity reduced during refinement.
* AI-assisted tools including Kiro, Gemini, and ChatGPT were used for development assistance, debugging, ideation, and scripting support.
* Visual references were explored through Pinterest and other reference sources during environment development.
* The project currently has a functional gameplay foundation, with additional visual polish and optimization remaining.

---

# Future Improvements

### Visual Polish

* Further refine the combat particle effects to make attacks feel more impactful and visually consistent.
* Improve environmental visual details and overall arena presentation.
* Add additional visual feedback for important gameplay interactions.
* Improve transitions and presentation between different gameplay actions.

### Environment Optimization

* Optimize the polygon count of environment assets, particularly the trees and other high-poly elements.
* Reduce unnecessary geometry while maintaining the required visual quality.
* Review environment assets for opportunities to improve runtime performance.
* Optimize materials, meshes, and other environment elements where required.

### Combat Refinement

* Further polish the fire attack visuals and timing.
* Improve particle effects and attack feedback.
* Continue testing combat interactions across different gameplay situations.
* Refine the overall combat presentation to make attacks clearer to the player.

### Animation & Gameplay Polish

* Further tune dragon animation transitions.
* Improve synchronization between animations, gameplay actions, and visual effects.
* Perform additional gameplay testing to identify edge cases and integration issues.

### Performance & Code Refinement

* Perform an additional optimization pass after the environment and visual assets are finalized.
* Review runtime systems for unnecessary processing and further simplify where appropriate.
* Profile the project to identify potential performance bottlenecks.
* Continue improving code structure and maintainability as the project develops.

### Additional Testing

* Conduct more extensive playtesting of the complete gameplay loop.
* Test the interaction between the arena, dragons, UI, animations, combat, and effects under different gameplay conditions.
* Address any remaining bugs or inconsistencies discovered during extended testing.


## Assets
- Dragon: [https://assetstore.unity.com/packages/3d/characters/creatures/dragon-pbr-94333]

## Author
**Bhart Inder Joshi**
