using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RedAlert.CI
{
    /// <summary>
    /// CI Build methods for Unity 6.1 automated builds
    /// Optimized for Unity 6000.1.14f1 with WebGL improvements
    /// </summary>
    public static class CIBuild
    {
        /// <summary>
        /// Build WebGL for Unity 6.1 production deployment
        /// </summary>
        public static void BuildWebGL()
        {
            try
            {
                Debug.Log("Starting Unity 6.1 WebGL build for CI/CD...");
                
                // Configure Unity 6.1 WebGL build settings
                string buildPath = "build/WebGL";
                BuildOptions buildOptions = BuildOptions.None;
                
                // Unity 6.1 WebGL optimizations
                ConfigureWebGLSettings();
                
                // Set development build for debugging if needed
                if (EditorUserBuildSettings.development)
                {
                    buildOptions |= BuildOptions.Development;
                    Debug.Log("Development build enabled");
                }
                
                // Get scenes from build settings
                string[] scenes = GetEnabledScenes();
                
                Debug.Log($"Unity Version: {Application.unityVersion}");
                Debug.Log($"Building to: {buildPath}");
                Debug.Log($"Scenes: {string.Join(", ", scenes)}");
                Debug.Log($"Target Platform: {EditorUserBuildSettings.activeBuildTarget}");
                
                // Ensure build directory exists
                Directory.CreateDirectory(buildPath);
                
                // Build the player
                BuildReport report = BuildPipeline.BuildPlayer(
                    scenes,
                    buildPath,
                    BuildTarget.WebGL,
                    buildOptions
                );
                
                // Check build result
                if (report.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"✅ Unity 6.1 WebGL build succeeded!");
                    Debug.Log($"📁 Output: {buildPath}");
                    Debug.Log($"📊 Build size: {report.summary.totalSize} bytes");
                    Debug.Log($"⏱️ Build time: {report.summary.totalTime}");
                    
                    // Log Unity 6.1 WebGL specifics
                    LogWebGLBuildInfo(buildPath);
                    
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError($"❌ Unity 6.1 WebGL build failed with result: {report.summary.result}");
                    LogBuildErrors(report);
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"💥 Unity 6.1 build exception: {e.Message}");
                Debug.LogError($"📋 Stack trace: {e.StackTrace}");
                EditorApplication.Exit(1);
            }
        }
        
        /// <summary>
        /// Configure Unity 6.1 WebGL specific settings
        /// </summary>
        private static void ConfigureWebGLSettings()
        {
            Debug.Log("Configuring Unity 6.1 WebGL settings...");
            
            // Ensure WebGL is the active build target
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("Switching to WebGL build target...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }
            
            // Unity 6.1 WebGL optimizations
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.debugSymbols = false;
            
            // Unity 6.1 improved WebGL threading
            PlayerSettings.WebGL.threadsSupport = false; // Enable if needed and hosting supports COOP/COEP
            
            // Memory and performance settings
            PlayerSettings.WebGL.memorySize = 256; // MB, adjust as needed
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            
            // Code stripping for smaller builds
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.managedStrippingLevel = ManagedStrippingLevel.High;
            
            Debug.Log("✅ Unity 6.1 WebGL settings configured");
        }
        
        /// <summary>
        /// Get all enabled scenes from build settings
        /// </summary>
        private static string[] GetEnabledScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }
            
            if (scenes.Count == 0)
            {
                Debug.LogWarning("No scenes enabled in build settings. Adding current scene.");
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                if (!string.IsNullOrEmpty(currentScene))
                {
                    scenes.Add(currentScene);
                }
                else
                {
                    Debug.LogError("No current scene available for build!");
                }
            }
            
            return scenes.ToArray();
        }
        
        /// <summary>
        /// Log Unity 6.1 WebGL build information
        /// </summary>
        private static void LogWebGLBuildInfo(string buildPath)
        {
            try
            {
                string buildFolder = Path.Combine(buildPath, "Build");
                if (Directory.Exists(buildFolder))
                {
                    var files = Directory.GetFiles(buildFolder, "*", SearchOption.TopDirectoryOnly);
                    Debug.Log($"📁 WebGL build files ({files.Length}):");
                    
                    foreach (string file in files)
                    {
                        FileInfo info = new FileInfo(file);
                        Debug.Log($"  📄 {Path.GetFileName(file)}: {info.Length / 1024}KB");
                    }
                }
                
                // Check for Unity 6.1 WebGL specific files
                string wasmFile = Path.Combine(buildFolder, "*.wasm");
                if (Directory.GetFiles(buildFolder, "*.wasm").Length > 0)
                {
                    Debug.Log("✅ Unity 6.1 WebAssembly build detected");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not log build info: {e.Message}");
            }
        }
        
        /// <summary>
        /// Log detailed build errors for debugging
        /// </summary>
        private static void LogBuildErrors(BuildReport report)
        {
            Debug.LogError("=== BUILD ERRORS ===");
            
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        Debug.LogError($"🔥 {step.name}: {message.content}");
                    }
                    else if (message.type == LogType.Warning)
                    {
                        Debug.LogWarning($"⚠️ {step.name}: {message.content}");
                    }
                }
            }
            
            Debug.LogError("=== END BUILD ERRORS ===");
        }
    }
}