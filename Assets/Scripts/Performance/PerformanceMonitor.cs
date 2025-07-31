using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using RedAlert.Core;

namespace RedAlert.Performance
{
    /// <summary>
    /// Performance monitoring system for Red Alert RTS.
    /// Tracks FPS, memory usage, and automatically adjusts quality settings for WebGL.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("Performance Targets")]
        [SerializeField] private float _targetFPS = 30f;
        [SerializeField] private float _minAcceptableFPS = 20f;
        [SerializeField] private long _maxMemoryMB = 512;
        
        [Header("Monitoring Settings")]
        [SerializeField] private bool _enableAutoOptimization = true;
        [SerializeField] private float _optimizationDelay = 5f;
        [SerializeField] private bool _showDebugOverlay = false;
        
        [Header("Quality Adjustment")]
        [SerializeField] private int _maxQualityLevel = 5;
        [SerializeField] private int _minQualityLevel = 0;
        
        // Performance metrics
        private float _currentFPS;
        private float _averageFPS;
        private long _currentMemoryMB;
        private float _frameTime;
        private int _currentQualityLevel;
        
        // Monitoring state
        private readonly Queue<float> _fpsHistory = new Queue<float>();
        private const int FPS_HISTORY_SIZE = 30; // 30 samples for average
        private float _lastOptimizationTime;
        private bool _isOptimizing;
        
        // Performance statistics
        private int _totalFrames;
        private float _totalTime;
        private float _minFPS = float.MaxValue;
        private float _maxFPS = 0f;
        
        // Auto-optimization state
        private int _consecutiveLowFPSFrames;
        private const int LOW_FPS_THRESHOLD_FRAMES = 30;
        
        public float CurrentFPS => _currentFPS;
        public float AverageFPS => _averageFPS;
        public long CurrentMemoryMB => _currentMemoryMB;
        public float FrameTime => _frameTime;
        public bool IsPerformingWell => _currentFPS >= _minAcceptableFPS;
        
        private void Start()
        {
            _currentQualityLevel = QualitySettings.GetQualityLevel();
            _lastOptimizationTime = Time.time;
            
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // Apply initial WebGL optimizations
                ApplyWebGLOptimizations();
            }
        }
        
        private void OnEnable()
        {
            UpdateDriver.Register(this);
        }
        
        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
        }
        
        private void Update()
        {
            UpdatePerformanceMetrics();
            
            if (_showDebugOverlay)
            {
                DrawDebugOverlay();
            }
        }
        
        public void SlowTick()
        {
            // Check if auto-optimization is needed
            if (_enableAutoOptimization && !_isOptimizing)
            {
                CheckForOptimizationNeeds();
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            // Calculate FPS
            _frameTime = Time.unscaledDeltaTime;
            _currentFPS = 1f / _frameTime;
            
            // Update FPS history for averaging
            _fpsHistory.Enqueue(_currentFPS);
            if (_fpsHistory.Count > FPS_HISTORY_SIZE)
            {
                _fpsHistory.Dequeue();
            }
            
            // Calculate average FPS
            float fpsSum = 0f;
            foreach (float fps in _fpsHistory)
            {
                fpsSum += fps;
            }
            _averageFPS = fpsSum / _fpsHistory.Count;
            
            // Update statistics
            _totalFrames++;
            _totalTime += _frameTime;
            _minFPS = Mathf.Min(_minFPS, _currentFPS);
            _maxFPS = Mathf.Max(_maxFPS, _currentFPS);
            
            // Update memory usage
            _currentMemoryMB = Profiler.GetTotalAllocatedMemory(false) / (1024 * 1024);
            
            // Track consecutive low FPS frames
            if (_currentFPS < _minAcceptableFPS)
            {
                _consecutiveLowFPSFrames++;
            }
            else
            {
                _consecutiveLowFPSFrames = 0;
            }
        }
        
        private void CheckForOptimizationNeeds()
        {
            if (Time.time - _lastOptimizationTime < _optimizationDelay) return;
            
            bool needsOptimization = false;
            string reason = "";
            
            // Check FPS performance
            if (_averageFPS < _minAcceptableFPS || _consecutiveLowFPSFrames >= LOW_FPS_THRESHOLD_FRAMES)
            {
                needsOptimization = true;
                reason = $"Low FPS: {_averageFPS:F1} (target: {_targetFPS:F1})";
            }
            
            // Check memory usage
            if (_currentMemoryMB > _maxMemoryMB)
            {
                needsOptimization = true;
                reason += $" High memory: {_currentMemoryMB}MB (max: {_maxMemoryMB}MB)";
            }
            
            if (needsOptimization)
            {
                StartCoroutine(OptimizePerformance(reason));
            }
        }
        
        private IEnumerator OptimizePerformance(string reason)
        {
            _isOptimizing = true;
            _lastOptimizationTime = Time.time;
            
            Debug.Log($"[PerformanceMonitor] Starting optimization: {reason}");
            
            // Try quality reduction first
            if (_currentQualityLevel > _minQualityLevel)
            {
                _currentQualityLevel = Mathf.Max(_minQualityLevel, _currentQualityLevel - 1);
                QualitySettings.SetQualityLevel(_currentQualityLevel);
                Debug.Log($"[PerformanceMonitor] Reduced quality level to {_currentQualityLevel}");
                
                yield return new WaitForSeconds(2f); // Wait to see effect
            }
            
            // If still having issues, apply more aggressive optimizations
            if (_averageFPS < _minAcceptableFPS)
            {
                ApplyAggressiveOptimizations();
                yield return new WaitForSeconds(2f);
            }
            
            // Force garbage collection to free memory
            if (_currentMemoryMB > _maxMemoryMB)
            {
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
                yield return new WaitForSeconds(1f);
            }
            
            _isOptimizing = false;
            Debug.Log($"[PerformanceMonitor] Optimization complete. New FPS: {_averageFPS:F1}");
        }
        
        private void ApplyWebGLOptimizations()
        {
            Debug.Log("[PerformanceMonitor] Applying WebGL-specific optimizations...");
            
            // Disable expensive features for WebGL
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 25f;
            
            // Reduce particle system limits
            QualitySettings.particleRaycastBudget = 16;
            
            // Disable anisotropic filtering
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            
            // Reduce texture quality
            QualitySettings.masterTextureLimit = 1;
            
            // Disable soft particles
            QualitySettings.softParticles = false;
            
            // Set reasonable quality level
            _currentQualityLevel = Mathf.Min(2, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(_currentQualityLevel);
            
            Debug.Log("[PerformanceMonitor] WebGL optimizations applied.");
        }
        
        private void ApplyAggressiveOptimizations()
        {
            Debug.Log("[PerformanceMonitor] Applying aggressive performance optimizations...");
            
            // Reduce LOD bias for more aggressive LOD switching
            QualitySettings.lodBias = 0.7f;
            
            // Reduce shadow distance further
            QualitySettings.shadowDistance = 15f;
            
            // Disable realtime reflections
            QualitySettings.realtimeReflectionProbes = false;
            
            // Reduce maximum LOD level
            QualitySettings.maximumLODLevel = 1;
            
            // Reduce pixel light count
            QualitySettings.pixelLightCount = 1;
            
            // Find and optimize renderers
            OptimizeActiveRenderers();
        }
        
        private void OptimizeActiveRenderers()
        {
            var assetOptimizer = FindObjectOfType<AssetOptimizer>();
            if (assetOptimizer != null)
            {
                var renderers = FindObjectsOfType<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.isVisible)
                    {
                        assetOptimizer.OptimizeRenderer(renderer);
                    }
                }
            }
        }
        
        private void DrawDebugOverlay()
        {
            // This would typically use OnGUI, but for production, 
            // you'd want to use a proper UI system
        }
        
        // Public API
        public void SetTargetFPS(float targetFPS)
        {
            _targetFPS = targetFPS;
            Application.targetFrameRate = Mathf.RoundToInt(targetFPS);
        }
        
        public void SetAutoOptimization(bool enabled)
        {
            _enableAutoOptimization = enabled;
        }
        
        public void ForceOptimization()
        {
            if (!_isOptimizing)
            {
                StartCoroutine(OptimizePerformance("Manual optimization"));
            }
        }
        
        public PerformanceStats GetPerformanceStats()
        {
            return new PerformanceStats
            {
                currentFPS = _currentFPS,
                averageFPS = _averageFPS,
                minFPS = _minFPS,
                maxFPS = _maxFPS,
                frameTime = _frameTime,
                memoryUsageMB = _currentMemoryMB,
                totalFrames = _totalFrames,
                totalTime = _totalTime,
                qualityLevel = _currentQualityLevel
            };
        }
    }
    
    [System.Serializable]
    public struct PerformanceStats
    {
        public float currentFPS;
        public float averageFPS;
        public float minFPS;
        public float maxFPS;
        public float frameTime;
        public long memoryUsageMB;
        public int totalFrames;
        public float totalTime;
        public int qualityLevel;
    }
}