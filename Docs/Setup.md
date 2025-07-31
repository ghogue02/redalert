# Project Setup — Unity 2022.3.40f1 LTS

This document pins configuration selections and provides a step-by-step checklist to get the project compiling and to produce the first Dev WebGL build.

## Prerequisites
- Unity 2022.3.40f1 LTS
- WebGL Build Support module installed with Unity Hub
- Git (and Git LFS recommended)

## Packages (baseline)
Use Window > Package Manager to confirm or add:
- Universal RP (URP) — latest compatible with 2022.3
- Input System (optional if using)
- TextMeshPro (built-in)
- Post Processing (if needed by URP profile)
- Cinemachine (optional, if planned)
- Addressables (optional)

Note: Package lock/manifest placeholders will be created by Unity on first open. Keep packages minimal at start.

## Graphics Pipeline (URP)
1) Create URP Asset:
   - Right click in Project window: Create > Rendering > URP > UniversalRenderPipelineAsset
   - Name: URP_Asset
2) Assign URP:
   - Edit > Project Settings > Graphics: Set Scriptable Render Pipeline Settings to URP_Asset
   - Edit > Project Settings > Quality: Assign URP_Asset for each quality level
3) Optional: Create a URP Renderer asset if needed for future features.

## Project Folder Layout (reference)
Assets/
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

## Initial Scenes (create in editor)
Create three scenes in Assets/Scenes:
- Bootstrap.unity
- MainMenu.unity
- Skirmish_Graybox.unity

Follow Assets/Scenes/README.md for intended content. Add them to Build Settings (File > Build Settings > Scenes In Build).

## Assigning Script Components
- In Bootstrap.unity:
  - Create empty GameObject "Bootstrap" and add RedAlert.Core.SceneBootstrap
  - Create empty GameObject "Systems" and add RedAlert.Core.UpdateDriver
- UI scaffolding:
  - When ready, add RedAlert.UI.HUDController to a UI root canvas and reference panel components.

## Define Symbols (optional)
Project Settings > Player > Other Settings > Scripting Define Symbols:
- You may add UNITY_2022_3 specific constraints in asmdefs, but not necessary. Keep empty initially.

## Version Control Notes
- Library/, Temp/, Logs/, Obj/, Build/, UserSettings/ should be ignored (see .gitignore)
- Commit Assets/, Packages/, ProjectSettings/ after Unity initializes the project

## First Dev WebGL Build — Checklist
1) Open Build Settings: File > Build Settings
2) Select WebGL platform and click "Switch Platform"
3) Add Scenes:
   - Ensure Bootstrap, MainMenu, Skirmish_Graybox are in "Scenes In Build" (Bootstrap at index 0)
4) Player Settings:
   - Company/Product name: As desired
   - Color Space: Linear (recommended)
   - API Compatibility Level: .NET 4.x
   - Compression Format: Gzip or Brotli (as preferred)
5) URP Verify:
   - Ensure URP_Asset is assigned in Graphics and Quality settings
6) Addressables (if used): Disable for first build or ensure clean default settings
7) Build:
   - Choose output folder: Build/WebGLDev/
   - Click "Build"
8) Verify Artifacts:
   - Output should include index.html, Build/ and TemplateData/ (or StreamingAssets) under Build/WebGLDev/
9) Run locally (optional):
   - Use a simple HTTP server to serve the build folder (WebGL requires HTTP server; file:// may fail)

## Troubleshooting
- Pink materials: URP not assigned — revisit Graphics/Quality settings
- Compilation errors: Ensure all asmdefs are present and scripts compile; no gameplay logic implemented yet
- WebGL build errors: Confirm WebGL module is installed, remove unused packages if linker runs out of memory
