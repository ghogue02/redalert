# Unity 6.1 Local Build Guide

🎯 **Solution**: Build locally with Unity Personal license, deploy via GitHub Actions  
⚖️ **Supervisor Approved**: Horizon Alpha confirmed this approach for Unity Personal license constraints

## 🚨 Unity Personal License Update

**⚠️ Issue**: Unity no longer supports manual activation for Personal licenses in CI/CD  
**✅ Solution**: Build locally where license is already activated, deploy artifacts via CI

## 🔧 Local Build Process

### **Step 1: Open Unity Project**
```bash
# Navigate to project directory
cd /Volumes/LaCie/redalert

# Open in Unity 6.1
open -a "/Applications/Unity/Hub/Editor/6000.1.14f1/Unity.app" .
```

### **Step 2: Configure WebGL Build Settings**
1. **File > Build Settings**
2. **Platform**: Select "WebGL" 
3. **Switch Platform** (if not already selected)
4. **Player Settings**:
   - **Compression Format**: Brotli
   - **Decompression Fallback**: Enabled
   - **Data Caching**: Enabled
   - **Memory Size**: 256 MB

### **Step 3: Add Scenes to Build**
1. **Build Settings > Add Open Scenes** (or drag scenes from Project)
2. Ensure at least one scene is checked
3. Recommended scenes:
   - `Assets/Scenes/Bootstrap.unity` (index 0)
   - `Assets/Scenes/MainMenu.unity`
   - `Assets/Scenes/Skirmish_Graybox.unity`

### **Step 4: Build WebGL**
1. **Build Settings > Build**
2. **Select Folder**: Choose `build/WebGL` (create if needed)
3. **Click "Build"**
4. Wait for Unity 6.1 to build (15-25 minutes typical)

### **Step 5: Verify Build Output**
```bash
# Check build output
ls -la build/WebGL/

# Should contain:
# - index.html
# - Build/ directory
# - TemplateData/ directory
```

### **Step 6: Commit and Deploy**
```bash
# Add build files to git
git add build/WebGL/

# Commit the build
git commit -m "Add Unity 6.1 WebGL build

- Built with Unity 6000.1.14f1 Personal license
- Brotli compression enabled
- Optimized for Vercel deployment
- Ready for automated deployment via GitHub Actions"

# Push to trigger deployment
git push origin main
```

## 🚀 Automated Deployment

Once you push the `build/WebGL/` directory:

1. **GitHub Actions** detects WebGL build changes
2. **Vercel deployment** happens automatically  
3. **Game goes live** at your Vercel URL

## 📁 Directory Structure

```
redalert/
├── build/
│   └── WebGL/           # ← Build output goes here
│       ├── index.html
│       ├── Build/
│       │   ├── *.wasm
│       │   ├── *.data
│       │   └── *.js
│       └── TemplateData/
├── Assets/
├── .github/workflows/
│   └── deploy-prebuilt.yml  # ← New deployment workflow
└── ...
```

## 🛠️ Troubleshooting

### **Build Fails**
- Check Unity Console for errors
- Ensure WebGL Build Support is installed
- Verify scenes are properly configured

### **Large Build Size**
- Check compression settings (Brotli recommended)
- Review assets for optimization opportunities
- Consider code stripping settings

### **Deployment Fails**
- Verify `build/WebGL/index.html` exists
- Check GitHub Actions logs
- Ensure Vercel secrets are configured

## 🎯 Benefits of This Approach

✅ **License Compliant**: Uses your activated Unity Personal license  
✅ **Fast Deployment**: No CI build time, just artifact deployment  
✅ **Reliable**: No CI license activation failures  
✅ **Quality Control**: You can test builds locally before deployment  
✅ **Cost Effective**: Minimal CI/CD resource usage

## 📋 Next Steps

1. **Build Unity WebGL** locally using steps above
2. **Configure Vercel secrets** in GitHub repository
3. **Push build artifacts** to trigger deployment
4. **Access your game** at the Vercel deployment URL

---

**🎮 Unity 6.1 game deployment without CI license headaches!**  
**⚖️ Supervisor-approved solution for Unity Personal license constraints**