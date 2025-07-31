# Red Alert — Unity Project Skeleton

This repository contains the initial Unity project scaffold matching the agreed vertical slice and technical architecture. It includes the folder layout, assembly definitions, script stubs, and documentation to allow engineers to open in Unity 2022.3.40f1 LTS and begin implementation.

Note: This repo does not include a generated Unity project (no Library/ProjectSettings). Open in Unity, let it generate, then assign URP and packages per Docs/Setup.md.

## Requirements
- Unity 2022.3.40f1 LTS
- Git LFS (recommended for future binary assets such as textures, audio)

## Folder Structure
- Assets/
  - Scripts/{Core, Data, Economy, Build, Units, Combat, Pathing, AI, UI, Debug}
  - Prefabs/{Units, Buildings, Resources, FX, UI}
  - Art/Placeholders
  - Materials/Placeholder
  - Shaders
  - Scenes
  - Resources/Data
  - UI/{Sprites, Layouts}
  - VFX
  - Audio
- Docs/
- CI/

All .asmdef modules are placed under Assets/Scripts/<Module>.

## Quick Start (after cloning)
1) Open the repository folder in Unity 2022.3.40f1 LTS. Unity will create Library and ProjectSettings.
2) Packages:
   - Open Package Manager. Confirm URP and other baseline packages as per Docs/Setup.md.
3) URP Setup:
   - Create or import a URP Pipeline Asset and assign it in Project Settings > Graphics (and Quality).
4) Scenes:
   - See Assets/Scenes/README.md for the three initial scenes:
     - Bootstrap.unity
     - MainMenu.unity
     - Skirmish_Graybox.unity
   - Create them in the editor and save to Assets/Scenes.
5) Script Assemblies:
   - The .asmdef files are included. Unity will compile stubs. If you add new references, update .asmdef accordingly.
6) Initial Objects (editor actions):
   - In Bootstrap scene, create an empty GameObject "Bootstrap" and add RedAlert.Core.SceneBootstrap.
   - Optionally create an empty "Systems" GameObject and add RedAlert.Core.UpdateDriver.
   - UI scene objects can reference RedAlert.UI.HUDController and panel components later.
7) Builds:
   - See Docs/Setup.md for WebGL build steps.
   - See CI/Build.md for batchmode build templates.

## Modules Overview
- RedAlert.Core — base services, scene bootstrap, event bus
- RedAlert.Data — ScriptableObject definitions for units, buildings, weapons, costs
- RedAlert.Pathing — path utilities/services
- RedAlert.Economy — resource economy systems and agents
- RedAlert.Build — placement and building systems
- RedAlert.Units — selection, commands, locomotion, damageables, teams
- RedAlert.Combat — weapon controllers, projectiles, hitscan
- RedAlert.UI — HUD, panels, minimap controller
- RedAlert.AI — standard bot, build order scripting
- RedAlert.Debug — gizmos, cheats, profiling toggles

## Conventions
- Namespaces: RedAlert.<Module>
- Filenames match class names
- Minimal stubs only; TODO markers indicate where implementation will follow
- No gameplay logic here; ensure clean compile

## License
MIT by default. See LICENSE.