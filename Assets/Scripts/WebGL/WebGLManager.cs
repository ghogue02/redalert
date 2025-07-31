using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedAlert.WebGL
{
    /// <summary>
    /// WebGL-specific manager handling loading screens, browser compatibility,
    /// and mobile optimizations for Red Alert RTS.
    /// </summary>
    public class WebGLManager : MonoBehaviour
    {
        [Header("Loading Screen")]
        [SerializeField] private GameObject _loadingScreen;
        [SerializeField] private UnityEngine.UI.Slider _loadingBar;
        [SerializeField] private UnityEngine.UI.Text _loadingText;
        [SerializeField] private float _minLoadingTime = 2f;
        
        [Header("Browser Compatibility")]
        [SerializeField] private bool _detectMobile = true;
        [SerializeField] private bool _detectLowEnd = true;
        [SerializeField] private string[] _supportedBrowsers = { "Chrome", "Firefox", "Safari", "Edge" };
        
        [Header("Performance Scaling")]
        [SerializeField] private float _mobileScaleFactor = 0.7f;
        [SerializeField] private int _maxTextureQualityMobile = 2;
        [SerializeField] private bool _disableShadowsOnMobile = true;
        
        private bool _isMobile;
        private bool _isLowEndDevice;
        private string _browserName;
        private float _loadingStartTime;
        
        public bool IsMobile => _isMobile;
        public bool IsLowEndDevice => _isLowEndDevice;
        public string BrowserName => _browserName;
        
        private void Awake()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                InitializeWebGL();
            }
            
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                StartCoroutine(InitialLoadingSequence());
            }
        }
        
        private void InitializeWebGL()
        {
            Debug.Log("[WebGLManager] Initializing WebGL optimizations...");
            
            // Detect device and browser capabilities
            DetectDeviceCapabilities();
            DetectBrowserCompatibility();
            
            // Apply WebGL-specific settings
            ApplyWebGLSettings();
            
            // Set target frame rate for consistent performance
            Application.targetFrameRate = _isMobile ? 30 : 60;
            
            Debug.Log($"[WebGLManager] WebGL initialized. Mobile: {_isMobile}, Browser: {_browserName}");
        }
        
        private void DetectDeviceCapabilities()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Detect mobile using JavaScript
            _isMobile = IsMobileDevice();
            
            // Detect low-end device based on various factors
            _isLowEndDevice = DetectLowEndDevice();
#else
            // Editor simulation
            _isMobile = SystemInfo.deviceType == DeviceType.Handheld;
            _isLowEndDevice = SystemInfo.systemMemorySize < 4096; // Less than 4GB RAM
#endif
            
            Debug.Log($"[WebGLManager] Device detection - Mobile: {_isMobile}, Low-end: {_isLowEndDevice}");
        }
        
        private void DetectBrowserCompatibility()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _browserName = GetBrowserName();
#else
            _browserName = "Editor";
#endif
            
            bool isSupported = false;
            foreach (string browser in _supportedBrowsers)
            {
                if (_browserName.Contains(browser))
                {
                    isSupported = true;
                    break;
                }
            }
            
            if (!isSupported && Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Debug.LogWarning($"[WebGLManager] Browser '{_browserName}' may have compatibility issues.");
                ShowBrowserWarning();
            }
        }
        
        private void ApplyWebGLSettings()
        {
            // Screen resolution settings
            if (_isMobile)
            {
                int targetWidth = Mathf.RoundToInt(Screen.width * _mobileScaleFactor);
                int targetHeight = Mathf.RoundToInt(Screen.height * _mobileScaleFactor);
                Screen.SetResolution(targetWidth, targetHeight, false);
            }
            
            // Quality settings based on device
            if (_isMobile || _isLowEndDevice)
            {
                QualitySettings.SetQualityLevel(1); // Low quality
                QualitySettings.masterTextureLimit = _maxTextureQualityMobile;
                
                if (_disableShadowsOnMobile)
                {
                    QualitySettings.shadows = ShadowQuality.Disable;
                }
                
                // Reduce particle limits
                QualitySettings.particleRaycastBudget = 8;
                QualitySettings.pixelLightCount = 1;
                
                // Disable expensive features
                QualitySettings.softParticles = false;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.antiAliasing = 0;
            }
            else
            {
                QualitySettings.SetQualityLevel(2); // Medium quality for desktop
            }
            
            // WebGL-specific optimizations
            QualitySettings.asyncUploadTimeSlice = 2;
            QualitySettings.asyncUploadBufferSize = 4;
            
            // Memory management
            if (_isLowEndDevice)
            {
                // More aggressive memory management for low-end devices
                QualitySettings.masterTextureLimit = 2;
                QualitySettings.maximumLODLevel = 1;
            }
        }
        
        private IEnumerator InitialLoadingSequence()
        {
            _loadingStartTime = Time.realtimeSinceStartup;
            
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(true);
            }
            
            // Simulate initial loading steps
            yield return StartCoroutine(LoadingStep("Initializing...", 0.1f, 1f));
            yield return StartCoroutine(LoadingStep("Loading assets...", 0.3f, 1.5f));
            yield return StartCoroutine(LoadingStep("Optimizing for your device...", 0.6f, 1f));
            yield return StartCoroutine(LoadingStep("Starting game...", 0.9f, 0.5f));
            
            // Ensure minimum loading time for better UX
            float elapsedTime = Time.realtimeSinceStartup - _loadingStartTime;
            if (elapsedTime < _minLoadingTime)
            {
                yield return new WaitForSeconds(_minLoadingTime - elapsedTime);
            }
            
            // Complete loading
            if (_loadingBar != null) _loadingBar.value = 1f;
            if (_loadingText != null) _loadingText.text = "Ready!";
            
            yield return new WaitForSeconds(0.5f);
            
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(false);
            }
        }
        
        private IEnumerator LoadingStep(string stepText, float targetProgress, float duration)
        {
            if (_loadingText != null) _loadingText.text = stepText;
            
            float startProgress = _loadingBar != null ? _loadingBar.value : 0f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Lerp(startProgress, targetProgress, elapsed / duration);
                
                if (_loadingBar != null)
                {
                    _loadingBar.value = progress;
                }
                
                yield return null;
            }
        }
        
        private void ShowBrowserWarning()
        {
            // In a real implementation, you'd show a UI warning
            Debug.LogWarning("Your browser may not fully support this game. For the best experience, please use Chrome, Firefox, Safari, or Edge.");
        }
        
        // JavaScript interface methods (would be implemented with jslib plugins)
        private bool IsMobileDevice()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // This would call JavaScript to detect mobile
            // For now, use Unity's built-in detection
            return SystemInfo.deviceType == DeviceType.Handheld;
#else
            return SystemInfo.deviceType == DeviceType.Handheld;
#endif
        }
        
        private bool DetectLowEndDevice()
        {
            // Heuristics for low-end device detection
            int memoryMB = SystemInfo.systemMemorySize;
            int vramMB = SystemInfo.graphicsMemorySize;
            string gpu = SystemInfo.graphicsDeviceName.ToLower();
            
            // Low memory
            if (memoryMB < 2048) return true;
            if (vramMB < 512) return true;
            
            // Integrated graphics (common patterns)
            if (gpu.Contains("intel") && (gpu.Contains("hd") || gpu.Contains("uhd"))) return true;
            if (gpu.Contains("adreno") && !gpu.Contains("6")) return true; // Older Adreno GPUs
            if (gpu.Contains("mali")) return true; // ARM Mali GPUs
            
            return false;
        }
        
        private string GetBrowserName()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // This would call JavaScript to get user agent
            // For now, return a placeholder
            return "WebGL";
#else
            return "Editor";
#endif
        }
        
        // Public API for dynamic quality adjustment
        public void AdjustQualityForPerformance(float averageFPS)
        {
            if (averageFPS < 20f && QualitySettings.GetQualityLevel() > 0)
            {
                // Reduce quality
                QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel() - 1);
                Debug.Log($"[WebGLManager] Reduced quality level due to low FPS ({averageFPS:F1})");
            }
            else if (averageFPS > 45f && QualitySettings.GetQualityLevel() < QualitySettings.names.Length - 1)
            {
                // Increase quality if performance allows
                QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel() + 1);
                Debug.Log($"[WebGLManager] Increased quality level due to good FPS ({averageFPS:F1})");
            }
        }
        
        public void ShowLoadingScreen(string message = "Loading...")
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(true);
                if (_loadingText != null) _loadingText.text = message;
                if (_loadingBar != null) _loadingBar.value = 0f;
            }
        }
        
        public void HideLoadingScreen()
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(false);
            }
        }
        
        public void UpdateLoadingProgress(float progress, string message = null)
        {
            if (_loadingBar != null) _loadingBar.value = progress;
            if (_loadingText != null && !string.IsNullOrEmpty(message)) _loadingText.text = message;
        }
        
        // Scene loading with loading screen
        public void LoadSceneAsync(string sceneName)
        {
            StartCoroutine(LoadSceneWithProgress(sceneName));
        }
        
        private IEnumerator LoadSceneWithProgress(string sceneName)
        {
            ShowLoadingScreen($"Loading {sceneName}...");
            
            var operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;
            
            while (operation.progress < 0.9f)
            {
                UpdateLoadingProgress(operation.progress, $"Loading {sceneName}...");
                yield return null;
            }
            
            UpdateLoadingProgress(0.9f, "Finalizing...");
            yield return new WaitForSeconds(0.5f);
            
            operation.allowSceneActivation = true;
            
            while (!operation.isDone)
            {
                yield return null;
            }
            
            HideLoadingScreen();
        }
    }
}