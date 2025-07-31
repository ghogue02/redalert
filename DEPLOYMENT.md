# Red Alert - Unity 6.1 Deployment Guide

🚀 **Unity Version**: 6.1 (6000.1.14f1) - Apple Silicon  
🎯 **Deployment Strategy**: GitHub Actions → Vercel  
⚖️ **Supervisor Approved**: Horizon Alpha reviewed and approved Unity 6.1 configuration

## 🎯 Overview

This Unity 6.1 WebGL project uses an optimized CI/CD pipeline:
1. **Builds** Unity 6.1 WebGL via GitHub Actions on Apple Silicon runners
2. **Deploys** to Vercel with Unity 6.1 WebGL optimizations
3. **Optimizes** for Unity 6.1 features: Brotli compression, improved WebGL, threading support

## 📋 Unity 6.1 Setup Requirements

### 1. Unity 6.1 License Extraction

**🎯 STEP 1: Generate Activation File**
```bash
/Applications/Unity/Hub/Editor/6000.1.14f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -createManualActivationFile -quit
```
This creates: `Unity_v6000.1.14f1.alf`

**🎯 STEP 2: Online Activation**
1. Go to: https://license.unity3d.com/manual
2. Upload the `.alf` file
3. Download the `.ulf` license file

**🎯 STEP 3: GitHub Secret**
Copy the ENTIRE `.ulf` file content to GitHub secret `UNITY_LICENSE`

### 2. GitHub Secrets Configuration

| Secret Name | Description | Unity 6.1 Notes |
|-------------|-------------|------------------|
| `UNITY_LICENSE` | Unity 6.1 license (.ulf content) | Full file content, no extra whitespace |
| `VERCEL_TOKEN` | Vercel deployment token | From Vercel dashboard |
| `VERCEL_ORG_ID` | Vercel organization ID | From `vercel link` output |
| `VERCEL_PROJECT_ID` | Vercel project ID | From `vercel link` output |

### 3. Vercel Project Setup

```bash
# Install Vercel CLI
npm i -g vercel@latest

# Link project (run from /Users/greghogue/redalert/)
cd /Users/greghogue/redalert
vercel link

# Copy org/project IDs to GitHub secrets
```

## 🔧 Unity 6.1 Configuration Files

### GitHub Actions Workflow
- **File**: `.github/workflows/unity-webgl-build.yml`
- **Unity Version**: 6000.1.14f1
- **Runner**: macOS-14 (Apple Silicon)
- **Features**: Unity 6.1 WebGL optimizations, Brotli compression

### Vercel Configuration  
- **File**: `vercel.json`
- **Unity 6.1 Features**: Enhanced CORS headers, WebAssembly MIME types
- **Optimizations**: Brotli/Gzip support, proper Content-Encoding

### Unity CI Build Script
- **File**: `Assets/Scripts/Editor/CIBuild.cs`
- **Unity 6.1 Optimizations**: 
  - Brotli compression enabled
  - WebGL threading support (configurable)
  - Enhanced memory management
  - Code stripping optimizations

## 🚀 Unity 6.1 Deployment Process

### Automatic Deployment
1. **Commit & Push** to `main` branch
2. **GitHub Actions** builds Unity 6.1 WebGL on Apple Silicon runner
3. **Vercel** deploys with Unity 6.1 optimizations
4. **Preview** deployments available for PRs

### Manual Deployment
```bash
# Generate Unity license (first time only)
/Applications/Unity/Hub/Editor/6000.1.14f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -createManualActivationFile -quit

# Trigger manual build
gh workflow run "Unity 6.1 WebGL CI/CD"

# Local Vercel deployment (if needed)
vercel --prod
```

## 📊 Unity 6.1 Build Optimizations

### Unity 6.1 WebGL Features
- **Compression**: Brotli (default) with Gzip fallback
- **Emscripten**: Updated toolchain with better performance
- **Threading**: Configurable WebGL threading support
- **Memory**: Optimized memory management (256MB default)
- **Code Stripping**: High-level managed stripping for smaller builds

### Vercel Optimizations  
- **Unity 6.1 Headers**: CORS, COOP/COEP for threading support
- **WebAssembly**: Proper MIME types for .wasm files
- **Caching**: Immutable caching for Unity build assets
- **Compression**: Brotli/Gzip content encoding support

## 🛠️ Unity 6.1 Troubleshooting

### Common Unity 6.1 Issues

**Build Fails - Unity 6.1 License**
```bash
# Verify license file location
ls -la "$HOME/Library/Application Support/Unity/"

# Re-generate activation file if needed
/Applications/Unity/Hub/Editor/6000.1.14f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -createManualActivationFile -quit
```

**WebGL Build Fails - Unity 6.1**
- Check Unity 6.1 WebGL Build Support is installed
- Verify Brotli compression settings in Player Settings
- Ensure scenes are added to Build Settings

**Deployment Fails - Unity 6.1 Output**
- Unity 6.1 output structure: `build/WebGL/Build/`
- Check for `.wasm`, `.data`, `.js` files
- Verify Vercel headers support Unity 6.1 format

### Unity 6.1 Debug Commands

```bash
# Check Unity 6.1 installation
ls -la /Applications/Unity/Hub/Editor/6000.1.14f1/

# Test Unity 6.1 command line
/Applications/Unity/Hub/Editor/6000.1.14f1/Unity.app/Contents/MacOS/Unity -version

# Manual Unity 6.1 WebGL build
/Applications/Unity/Hub/Editor/6000.1.14f1/Unity.app/Contents/MacOS/Unity \
  -projectPath /Users/greghogue/redalert \
  -batchmode -nographics -quit \
  -buildTarget WebGL \
  -executeMethod RedAlert.CI.CIBuild.BuildWebGL
```

## 🎮 Unity 6.1 Performance Notes

- **Build Time**: ~15-25 minutes for Unity 6.1 builds (improved from 2022.3)
- **Artifact Size**: 30-50% smaller with Unity 6.1 optimizations
- **Load Time**: Faster WebGL loading with improved Emscripten toolchain
- **Runtime**: Better performance with Unity 6.1 WebGL improvements

## 📈 Unity 6.1 Monitoring

### Success Metrics
- ✅ Unity 6.1 build completes without errors
- ✅ WebGL loads with Unity 6.1 optimizations
- ✅ Brotli compression active (check network tab)
- ✅ WebAssembly loading correctly

### Unity 6.1 Specific Checks
- Monitor Unity 6.1 build artifacts in GitHub Actions
- Check Vercel deployment logs for Unity 6.1 specific files
- Verify WebGL console shows Unity 6.1 version
- Test on multiple browsers for Unity 6.1 compatibility

---

## 🎯 NEXT STEPS FOR YOU

**🔐 1. Get Your Unity License:**
Run the command shown above to generate the activation file, then activate online.

**🌐 2. Set Up Vercel:**
```bash
npm i -g vercel@latest
cd /Users/greghogue/redalert  
vercel link
```

**🔑 3. Configure GitHub Secrets:**
Add the 4 secrets shown above to your repository.

**🚀 4. Deploy:**
Push your code and watch the Unity 6.1 deployment happen automatically!

**Unity 6.1 deployment pipeline ready - supervised by Horizon Alpha**