# CI Build Templates

This document provides Unity batchmode command templates and expected artifact paths. Adjust projectPath, build target, and method names as your pipeline requires.

Note: These templates assume Unity 2022.3.40f1 and that a Unity project exists at repository root (ProjectSettings/ present). For first-time CI runs, ensure Unity is opened once locally to generate ProjectSettings and Packages lock.

## CLI Batchmode Basics

- -batchmode: Run without UI
- -nographics: Headless mode (CI environments)
- -quit: Quit after executing method/build
- -projectPath: Absolute or relative path to the Unity project root
- -executeMethod: Static method entry point: Namespace.ClassName.Method
- -buildTarget: Target platform (e.g., WebGL, StandaloneOSX, StandaloneWindows64)
- -logFile: Write logs to file (optional; use - for stdout)
- -username/-password or -serial: For Pro/Plus activations (manage via CI secrets or ULF license approach)

## Example Build Methods (to implement later)
Create a static class like:
- Namespace: RedAlert.CI
- Class: BuildRunner
- Methods: BuildWebGL, BuildWindows, BuildMac

Example signature:
- public static void BuildWebGL()

These methods should:
1) Configure build options
2) Include enabled scenes from Build Settings
3) Build to the specified output folder
4) Exit with code 0 on success; throw on failure

## Command Templates

Replace UNITY_PATH with your Unity Editor executable path.

macOS:
- UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.40f1/Unity.app/Contents/MacOS/Unity"

Windows:
- UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.40f1\Editor\Unity.exe"

Linux (if applicable):
- UNITY_PATH="/opt/unity/Editor/Unity"

### WebGL (Dev)
macOS:
UNITY_PATH -batchmode -nographics -quit \
-projectPath "$(pwd)" \
-executeMethod RedAlert.CI.BuildRunner.BuildWebGL \
-buildTarget WebGL \
-logFile ./CI/logs/build_webgl.log

Output (expected):
- Build/WebGLDev/index.html
- Build/WebGLDev/Build/*
- Build/WebGLDev/TemplateData/* (or equivalent)

### Windows 64-bit (Standalone)
UNITY_PATH -batchmode -nographics -quit ^
-projectPath "%CD%" ^
-executeMethod RedAlert.CI.BuildRunner.BuildWindows ^
-buildTarget StandaloneWindows64 ^
-logFile .\CI\logs\build_win64.log

Output (expected):
- Build/Windows/RedAlert.exe
- Build/Windows/RedAlert_Data/*

### macOS (Standalone)
UNITY_PATH -batchmode -nographics -quit \
-projectPath "$(pwd)" \
-executeMethod RedAlert.CI.BuildRunner.BuildMac \
-buildTarget StandaloneOSX \
-logFile ./CI/logs/build_macos.log

Output (expected):
- Build/macOS/RedAlert.app

## Scenes and Build Index
Ensure the following scenes exist and are added in Build Settings:
- Assets/Scenes/Bootstrap.unity (index 0)
- Assets/Scenes/MainMenu.unity
- Assets/Scenes/Skirmish_Graybox.unity

## Artifacts and CI Collection
Archive or publish:
- Build/WebGLDev (for web pipelines)
- Build/Windows (zip)
- Build/macOS (zip)
- CI/logs/*.log

## Licensing/Activation
- Use Unity’s recommended CI activation flow (ULF license or Unity Build Server). Do not hardcode credentials in pipelines.
- Cache Library/ where appropriate to speed up builds.

## Example GitHub Actions Snippet (placeholder)
name: Build WebGL
on:
  workflow_dispatch:
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Unity - Builder (placeholder)
        run: |
          echo "Install Unity, activate license, then run batchmode."
          # UNITY_PATH="/opt/unity/Editor/Unity"
          # $UNITY_PATH -batchmode -nographics -quit \
          #   -projectPath "$(pwd)" \
          #   -executeMethod RedAlert.CI.BuildRunner.BuildWebGL \
          #   -buildTarget WebGL \
          #   -logFile ./CI/logs/build_webgl.log
      - name: Upload Artifact
        uses: actions/upload-artifact@v4
        with:
          name: WebGLDev
          path: Build/WebGLDev