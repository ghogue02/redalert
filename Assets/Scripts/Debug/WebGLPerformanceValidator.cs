using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using RedAlert.Core;

namespace RedAlert.Debug
{
    /// <summary>
    /// WebGL Performance Validation Framework for Unity 6000.1
    /// Monitors frame time, memory usage, draw calls, and other WebGL-specific metrics
    /// </summary>
    public class WebGLPerformanceValidator : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("Performance Budgets")]
        [SerializeField] private float targetFrameTimeMs = 16.7f; // 60fps
        [SerializeField] private float maxMemoryMB = 256f;
        [SerializeField] private int maxDrawCalls = 1000;
        [SerializeField] private int maxSetPassCalls = 200;
        [SerializeField] private int maxTriangles = 100000;
        
        [Header("Monitoring")]
        [SerializeField] private bool enableContinuousMonitoring = true;
        [SerializeField] private float reportIntervalSeconds = 5f;
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool showOnScreenStats = true;
        
        [Header("Thresholds")]
        [SerializeField] private float frameTimeWarningMs = 20f;
        [SerializeField] private float frameTimeCriticalMs = 33f; // 30fps
        [SerializeField] private float memoryWarningMB = 200f;
        [SerializeField] private float memoryCriticalMB = 240f;
        
        private struct PerformanceMetrics
        {
            public float frameTimeMs;
            public float memoryUsageMB;
            public int drawCalls;
            public int setPassCalls;
            public int triangles;
            public int vertices;
            public float gpuTimeMs;
            public bool isValid;
        }
        
        private Queue<PerformanceMetrics> _metricsHistory = new Queue<PerformanceMetrics>();
        private float _nextReportTime;
        private PerformanceMetrics _currentMetrics;
        private PerformanceMetrics _averageMetrics;
        private int _maxHistorySize = 60; // 15 seconds at 4Hz
        
        // WebGL-specific monitoring
        private int _frameCount;
        private float _lastFrameTime;
        
        private void OnEnable()
        {
            UpdateDriver.Register(this);
            _nextReportTime = Time.time + reportIntervalSeconds;
        }
        
        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
        }
        
        private void Update()
        {
            // Collect per-frame metrics
            _frameCount++;
            _lastFrameTime = Time.unscaledDeltaTime * 1000f; // Convert to ms
        }
        
        public void SlowTick()
        {
            if (!enableContinuousMonitoring) return;
            
            // Collect performance metrics
            _currentMetrics = CollectMetrics();
            
            // Add to history
            _metricsHistory.Enqueue(_currentMetrics);
            if (_metricsHistory.Count > _maxHistorySize)
            {
                _metricsHistory.Dequeue();
            }
            
            // Calculate averages
            _averageMetrics = CalculateAverageMetrics();
            
            // Check for performance violations
            CheckPerformanceBudgets();
            
            // Periodic reporting
            if (Time.time >= _nextReportTime)
            {
                if (logToConsole)
                {
                    LogPerformanceReport();
                }
                _nextReportTime = Time.time + reportIntervalSeconds;
            }
        }
        
        private PerformanceMetrics CollectMetrics()
        {
            var metrics = new PerformanceMetrics();
            
            try
            {
                // Frame time
                metrics.frameTimeMs = _lastFrameTime;
                
                // Memory usage (WebGL-friendly approach)
#if !UNITY_WEBGL || UNITY_EDITOR
                metrics.memoryUsageMB = Profiler.GetTotalAllocatedMemory(Profiler.GetDefaultProfiler()) / (1024f * 1024f);
#else
                // WebGL fallback - use GC memory
                metrics.memoryUsageMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
#endif
                
                // Rendering stats
                UnityEngine.Rendering.FrameDebuggerUtility.GetRenderStatistics(out var stats);
                metrics.drawCalls = stats.drawCalls;
                metrics.setPassCalls = stats.setPassCalls;
                metrics.triangles = stats.triangles;
                metrics.vertices = stats.vertices;
                
                // GPU time (if available)
#if !UNITY_WEBGL || UNITY_EDITOR
                metrics.gpuTimeMs = Profiler.GetCounterValue(Profiler.GetDefaultProfiler(), "GPU Main Thread") / 1000000f;
#else
                metrics.gpuTimeMs = 0f; // Not available on WebGL
#endif
                
                metrics.isValid = true;
            }
            catch (System.Exception e)
            {
                if (logToConsole)
                {
                    UnityEngine.Debug.LogWarning($"WebGLPerformanceValidator: Failed to collect metrics: {e.Message}");
                }
                metrics.isValid = false;
            }
            
            return metrics;
        }
        
        private PerformanceMetrics CalculateAverageMetrics()
        {
            if (_metricsHistory.Count == 0) return default;
            
            var avg = new PerformanceMetrics();
            float count = 0f;
            
            foreach (var metrics in _metricsHistory)
            {
                if (!metrics.isValid) continue;
                
                avg.frameTimeMs += metrics.frameTimeMs;
                avg.memoryUsageMB += metrics.memoryUsageMB;
                avg.drawCalls += metrics.drawCalls;
                avg.setPassCalls += metrics.setPassCalls;
                avg.triangles += metrics.triangles;
                avg.vertices += metrics.vertices;
                avg.gpuTimeMs += metrics.gpuTimeMs;
                count++;
            }
            
            if (count > 0)
            {
                avg.frameTimeMs /= count;
                avg.memoryUsageMB /= count;
                avg.drawCalls = Mathf.RoundToInt(avg.drawCalls / count);
                avg.setPassCalls = Mathf.RoundToInt(avg.setPassCalls / count);
                avg.triangles = Mathf.RoundToInt(avg.triangles / count);
                avg.vertices = Mathf.RoundToInt(avg.vertices / count);
                avg.gpuTimeMs /= count;
                avg.isValid = true;
            }
            
            return avg;
        }
        
        private void CheckPerformanceBudgets()
        {
            if (!_currentMetrics.isValid) return;
            
            // Frame time budget check
            if (_currentMetrics.frameTimeMs > frameTimeCriticalMs)
            {
                LogPerformanceViolation("CRITICAL", $"Frame time {_currentMetrics.frameTimeMs:F2}ms exceeds critical threshold {frameTimeCriticalMs}ms");
            }
            else if (_currentMetrics.frameTimeMs > frameTimeWarningMs)
            {
                LogPerformanceViolation("WARNING", $"Frame time {_currentMetrics.frameTimeMs:F2}ms exceeds warning threshold {frameTimeWarningMs}ms");
            }
            
            // Memory budget check
            if (_currentMetrics.memoryUsageMB > memoryCriticalMB)
            {
                LogPerformanceViolation("CRITICAL", $"Memory usage {_currentMetrics.memoryUsageMB:F1}MB exceeds critical threshold {memoryCriticalMB}MB");
            }
            else if (_currentMetrics.memoryUsageMB > memoryWarningMB)
            {
                LogPerformanceViolation("WARNING", $"Memory usage {_currentMetrics.memoryUsageMB:F1}MB exceeds warning threshold {memoryWarningMB}MB");
            }
            
            // Draw call budget check
            if (_currentMetrics.drawCalls > maxDrawCalls)
            {
                LogPerformanceViolation("WARNING", $"Draw calls {_currentMetrics.drawCalls} exceeds budget {maxDrawCalls}");
            }
            
            // Set pass calls budget check
            if (_currentMetrics.setPassCalls > maxSetPassCalls)
            {
                LogPerformanceViolation("WARNING", $"SetPass calls {_currentMetrics.setPassCalls} exceeds budget {maxSetPassCalls}");
            }
            
            // Triangle budget check
            if (_currentMetrics.triangles > maxTriangles)
            {
                LogPerformanceViolation("WARNING", $"Triangle count {_currentMetrics.triangles} exceeds budget {maxTriangles}");
            }
        }
        
        private void LogPerformanceViolation(string severity, string message)
        {
            if (!logToConsole) return;
            
            string logMessage = $"[WebGL Performance {severity}] {message}";
            
            if (severity == "CRITICAL")
            {
                UnityEngine.Debug.LogError(logMessage);
            }
            else
            {
                UnityEngine.Debug.LogWarning(logMessage);
            }
        }
        
        private void LogPerformanceReport()
        {
            if (!_averageMetrics.isValid) return;
            
            UnityEngine.Debug.Log($"=== WebGL Performance Report (Unity {Application.unityVersion}) ===");
            UnityEngine.Debug.Log($"Frame Time: {_averageMetrics.frameTimeMs:F2}ms (Target: {targetFrameTimeMs:F2}ms)");
            UnityEngine.Debug.Log($"Memory: {_averageMetrics.memoryUsageMB:F1}MB (Budget: {maxMemoryMB:F1}MB)");
            UnityEngine.Debug.Log($"Draw Calls: {_averageMetrics.drawCalls} (Budget: {maxDrawCalls})");
            UnityEngine.Debug.Log($"SetPass Calls: {_averageMetrics.setPassCalls} (Budget: {maxSetPassCalls})");
            UnityEngine.Debug.Log($"Triangles: {_averageMetrics.triangles} (Budget: {maxTriangles})");
            
            // Performance status
            bool frameTimeOK = _averageMetrics.frameTimeMs <= targetFrameTimeMs;
            bool memoryOK = _averageMetrics.memoryUsageMB <= maxMemoryMB;
            bool drawCallsOK = _averageMetrics.drawCalls <= maxDrawCalls;
            
            string status = (frameTimeOK && memoryOK && drawCallsOK) ? "PASS" : "FAIL";
            UnityEngine.Debug.Log($"Overall Status: {status}");
            UnityEngine.Debug.Log("=== End Performance Report ===");
        }
        
        private void OnGUI()
        {
            if (!showOnScreenStats || !_currentMetrics.isValid) return;
            
            // Simple on-screen display
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUI.color = (_currentMetrics.frameTimeMs <= targetFrameTimeMs) ? Color.green : Color.red;
            GUILayout.Label($"Frame: {_currentMetrics.frameTimeMs:F1}ms");
            
            GUI.color = (_currentMetrics.memoryUsageMB <= maxMemoryMB) ? Color.green : Color.red;
            GUILayout.Label($"Memory: {_currentMetrics.memoryUsageMB:F1}MB");
            
            GUI.color = (_currentMetrics.drawCalls <= maxDrawCalls) ? Color.green : Color.red;
            GUILayout.Label($"Draw Calls: {_currentMetrics.drawCalls}");
            
            GUI.color = Color.white;
            GUILayout.Label($"Triangles: {_currentMetrics.triangles:N0}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        // Public API for testing
        public PerformanceMetrics GetCurrentMetrics() => _currentMetrics;
        public PerformanceMetrics GetAverageMetrics() => _averageMetrics;
        
        public bool IsPerformanceWithinBudgets()
        {
            return _averageMetrics.isValid &&
                   _averageMetrics.frameTimeMs <= targetFrameTimeMs &&
                   _averageMetrics.memoryUsageMB <= maxMemoryMB &&
                   _averageMetrics.drawCalls <= maxDrawCalls &&
                   _averageMetrics.setPassCalls <= maxSetPassCalls &&
                   _averageMetrics.triangles <= maxTriangles;
        }
        
        public void RunPerformanceTest(float durationSeconds, System.Action<bool> onComplete)
        {
            StartCoroutine(PerformanceTestCoroutine(durationSeconds, onComplete));
        }
        
        private System.Collections.IEnumerator PerformanceTestCoroutine(float duration, System.Action<bool> onComplete)
        {
            UnityEngine.Debug.Log($"Starting WebGL performance test for {duration} seconds...");
            
            _metricsHistory.Clear();
            float startTime = Time.time;
            
            while (Time.time - startTime < duration)
            {
                yield return new WaitForSeconds(0.25f); // 4Hz sampling
            }
            
            bool passed = IsPerformanceWithinBudgets();
            UnityEngine.Debug.Log($"WebGL Performance Test Result: {(passed ? "PASS" : "FAIL")}");
            
            onComplete?.Invoke(passed);
        }
    }
}