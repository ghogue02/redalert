using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedAlert.Core;
using RedAlert.Units;
using RedAlert.Economy;
using RedAlert.Build;
using RedAlert.UI;
using RedAlert.Performance;
using RedAlert.Audio;

namespace RedAlert.Testing
{
    /// <summary>
    /// Comprehensive integration test manager for Red Alert RTS.
    /// Validates all systems work together correctly and identifies potential issues.
    /// </summary>
    public class IntegrationTestManager : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runTestsOnStart = false;
        [SerializeField] private bool _logVerbose = true;
        [SerializeField] private float _testTimeout = 30f;
        [SerializeField] private bool _runPerformanceTests = true;
        
        [Header("Test Scenarios")]
        [SerializeField] private bool _testBasicGameplay = true;
        [SerializeField] private bool _testEconomySystem = true;
        [SerializeField] private bool _testBuildingSystem = true;
        [SerializeField] private bool _testCombatSystem = true;
        [SerializeField] private bool _testUISystem = true;
        [SerializeField] private bool _testAudioSystem = true;
        [SerializeField] private bool _testPerformanceSystem = true;
        
        private readonly List<TestResult> _testResults = new List<TestResult>();
        private int _totalTests = 0;
        private int _passedTests = 0;
        private int _failedTests = 0;
        private bool _isRunningTests = false;
        
        public bool IsRunningTests => _isRunningTests;
        public List<TestResult> TestResults => new List<TestResult>(_testResults);
        public float TestPassRate => _totalTests > 0 ? (float)_passedTests / _totalTests : 0f;
        
        private void Start()
        {
            if (_runTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }
        
        public void RunTests()
        {
            if (!_isRunningTests)
            {
                StartCoroutine(RunAllTests());
            }
        }
        
        private IEnumerator RunAllTests()
        {
            _isRunningTests = true;
            _testResults.Clear();
            _totalTests = 0;
            _passedTests = 0;
            _failedTests = 0;
            
            LogInfo("=== Red Alert RTS Integration Tests Started ===");
            
            // Core system validation
            yield return StartCoroutine(TestCoreServices());
            
            // Basic gameplay tests
            if (_testBasicGameplay)
            {
                yield return StartCoroutine(TestBasicGameplayFlow());
            }
            
            // Economy system tests
            if (_testEconomySystem)
            {
                yield return StartCoroutine(TestEconomySystem());
            }
            
            // Building system tests
            if (_testBuildingSystem)
            {
                yield return StartCoroutine(TestBuildingSystem());
            }
            
            // Combat system tests
            if (_testCombatSystem)
            {
                yield return StartCoroutine(TestCombatSystem());
            }
            
            // UI system tests
            if (_testUISystem)
            {
                yield return StartCoroutine(TestUISystem());
            }
            
            // Audio system tests
            if (_testAudioSystem)
            {
                yield return StartCoroutine(TestAudioSystem());
            }
            
            // Performance tests
            if (_testPerformanceSystem && _runPerformanceTests)
            {
                yield return StartCoroutine(TestPerformanceSystem());
            }
            
            // WebGL specific tests
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                yield return StartCoroutine(TestWebGLCompatibility());
            }
            
            // Generate final report
            GenerateFinalReport();
            
            _isRunningTests = false;
            LogInfo("=== Integration Tests Completed ===");
        }
        
        private IEnumerator TestCoreServices()
        {
            LogInfo("Testing Core Services...");
            
            // Test UpdateDriver
            var updateDriver = FindObjectOfType<UpdateDriver>();
            RecordTest("UpdateDriver exists", updateDriver != null);
            
            // Test EventBus
            bool eventBusWorks = TestEventBus();
            RecordTest("EventBus functional", eventBusWorks);
            
            // Test PoolService
            var poolService = PoolService.Instance;
            RecordTest("PoolService exists", poolService != null);
            
            if (poolService != null)
            {
                // Test pool registration and retrieval
                bool poolWorks = TestPoolService(poolService);
                RecordTest("PoolService functional", poolWorks);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        private IEnumerator TestBasicGameplayFlow()
        {
            LogInfo("Testing Basic Gameplay Flow...");
            
            // Test scene setup
            var gameStateManager = FindObjectOfType<GameStateManager>();
            RecordTest("GameStateManager exists", gameStateManager != null);
            
            var camera = Camera.main;
            RecordTest("Main camera exists", camera != null);
            
            var selectionSystem = FindObjectOfType<SelectionSystem>();
            RecordTest("SelectionSystem exists", selectionSystem != null);
            
            var commandSystem = FindObjectOfType<CommandSystem>();
            RecordTest("CommandSystem exists", commandSystem != null);
            
            // Test basic game loop
            yield return StartCoroutine(TestGameLoopIntegration());
            
            yield return new WaitForSeconds(0.1f);
        }
        
        private IEnumerator TestEconomySystem()
        {
            LogInfo("Testing Economy System...");
            
            var playerEconomy = FindObjectOfType<PlayerEconomy>();
            RecordTest("PlayerEconomy exists", playerEconomy != null);
            
            if (playerEconomy != null)
            {
                int initialResources = playerEconomy.Crystalite;
                
                // Test resource changes
                playerEconomy.AddCrystalite(100);
                bool resourcesIncreased = playerEconomy.Crystalite == initialResources + 100;
                RecordTest("Resource addition works", resourcesIncreased);
                
                playerEconomy.AddCrystalite(-50);
                bool resourcesDecreased = playerEconomy.Crystalite == initialResources + 50;
                RecordTest("Resource subtraction works", resourcesDecreased);
                
                // Test resource events
                bool eventFired = false;
                System.Action<int> testHandler = (amount) => eventFired = true;
                playerEconomy.OnCrystaliteChanged += testHandler;
                playerEconomy.AddCrystalite(1);
                playerEconomy.OnCrystaliteChanged -= testHandler;
                RecordTest("Resource change events work", eventFired);
            }
            
            // Test resource nodes
            var resourceNodes = FindObjectsOfType<RedAlert.Economy.CrystaliteNode>();
            RecordTest("Resource nodes exist", resourceNodes.Length > 0);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        private IEnumerator TestBuildingSystem()
        {
            LogInfo("Testing Building System...");
            
            var buildPlacement = FindObjectOfType<BuildPlacementController>();
            RecordTest("BuildPlacementController exists", buildPlacement != null);
            
            var buildMenu = FindObjectOfType<BuildMenuPanel>();
            RecordTest("BuildMenuPanel exists", buildMenu != null);
            
            if (buildPlacement != null)
            {
                // Test building placement
                bool canStartPlacement = buildPlacement.BeginPlacement(BuildPlacementController.BuildType.Refinery);
                RecordTest("Can start building placement", canStartPlacement);
                
                if (canStartPlacement)
                {
                    bool isActive = buildPlacement.IsActive();
                    RecordTest("Building placement is active", isActive);
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        private IEnumerator TestCombatSystem()
        {
            LogInfo("Testing Combat System...");
            
            // Find or spawn test units
            var units = FindObjectsOfType<Damageable>();
            RecordTest("Damageable units exist", units.Length > 0);
            
            if (units.Length > 0)
            {
                var testUnit = units[0];
                float initialHealth = testUnit.Health;
                
                // Test damage application
                testUnit.ApplyDamage(10f, "SmallArms");
                bool damageApplied = testUnit.Health < initialHealth;
                RecordTest("Damage application works", damageApplied);
                
                // Test armor system
                float healthAfterArmor = testUnit.Health;
                testUnit.ApplyDamage(10f, "AntiArmor");
                bool armorSystemWorks = testUnit.Health != healthAfterArmor - 10f; // Should be different due to armor
                RecordTest("Armor system works", armorSystemWorks);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        private IEnumerator TestUISystem()
        {
            LogInfo("Testing UI System...");
            
            var hudController = FindObjectOfType<HUDController>();
            RecordTest("HUDController exists", hudController != null);
            
            var resourcePanel = FindObjectOfType<ResourcePanel>();
            RecordTest("ResourcePanel exists", resourcePanel != null);
            
            var selectionPanel = FindObjectOfType<SelectionPanel>();
            RecordTest("SelectionPanel exists", selectionPanel != null);
            
            var minimapController = FindObjectOfType<MinimapController>();
            RecordTest("MinimapController exists", minimapController != null);
            
            // Test UI responsiveness
            var canvas = FindObjectOfType<Canvas>();
            RecordTest("UI Canvas exists", canvas != null);
            
            // Test tooltip system
            var tooltipSystem = FindObjectOfType<RedAlert.Accessibility.TooltipSystem>();
            bool tooltipExists = tooltipSystem != null;
            RecordTest("Tooltip system exists", tooltipExists);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        private IEnumerator TestAudioSystem()
        {
            LogInfo("Testing Audio System...");
            
            var audioSystem = AudioSystem.Instance;
            RecordTest("AudioSystem exists", audioSystem != null);
            
            if (audioSystem != null)
            {
                // Test volume controls
                float originalVolume = audioSystem.MasterVolume;
                audioSystem.MasterVolume = 0.5f;
                bool volumeChanged = Mathf.Approximately(audioSystem.MasterVolume, 0.5f);
                RecordTest("Volume control works", volumeChanged);
                
                // Restore original volume
                audioSystem.MasterVolume = originalVolume;
            }
            
            // Test audio sources
            var audioSources = FindObjectsOfType<AudioSource>();
            RecordTest("Audio sources exist", audioSources.Length > 0);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        private IEnumerator TestPerformanceSystem()
        {
            LogInfo("Testing Performance System...");
            
            var performanceMonitor = FindObjectOfType<PerformanceMonitor>();
            RecordTest("PerformanceMonitor exists", performanceMonitor != null);
            
            var assetOptimizer = FindObjectOfType<AssetOptimizer>();
            RecordTest("AssetOptimizer exists", assetOptimizer != null);
            
            // Test performance metrics
            if (performanceMonitor != null)
            {
                float fps = performanceMonitor.CurrentFPS;
                bool fpsReasonable = fps > 10f && fps < 200f; // Sanity check
                RecordTest("FPS measurement reasonable", fpsReasonable);
                
                long memory = performanceMonitor.CurrentMemoryMB;
                bool memoryReasonable = memory > 0 && memory < 4096; // Less than 4GB
                RecordTest("Memory measurement reasonable", memoryReasonable);
            }
            
            yield return new WaitForSeconds(1f); // Allow time for performance measurement
        }
        
        private IEnumerator TestWebGLCompatibility()
        {
            LogInfo("Testing WebGL Compatibility...");
            
            var webglManager = FindObjectOfType<RedAlert.WebGL.WebGLManager>();
            RecordTest("WebGLManager exists", webglManager != null);
            
            // Test WebGL specific features
            bool supportsWebGL = SystemInfo.supportsInstancing;
            RecordTest("GPU instancing supported", supportsWebGL);
            
            bool hasWebGLOptimizations = QualitySettings.GetQualityLevel() <= 2;
            RecordTest("WebGL quality optimizations applied", hasWebGLOptimizations);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        private IEnumerator TestGameLoopIntegration()
        {
            LogInfo("Testing Game Loop Integration...");
            
            // Test that all major systems can work together
            float startTime = Time.time;
            int frameCount = 0;
            bool stableFramerate = true;
            
            while (Time.time - startTime < 2f) // Test for 2 seconds
            {
                frameCount++;
                
                // Check for frame drops or freezes
                if (Time.unscaledDeltaTime > 0.1f) // Frame took longer than 100ms
                {
                    stableFramerate = false;
                }
                
                yield return null;
            }
            
            float averageFPS = frameCount / 2f;
            RecordTest("Game loop stable", stableFramerate && averageFPS > 15f);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Helper methods
        private bool TestEventBus()
        {
            bool eventReceived = false;
            
            System.Action<GameObject> testHandler = (obj) => eventReceived = true;
            EventBus.OnUnitDeath += testHandler;
            
            // Create a test GameObject and trigger the event
            var testObj = new GameObject("TestUnit");
            EventBus.PublishUnitDeath(testObj);
            
            EventBus.OnUnitDeath -= testHandler;
            Destroy(testObj);
            
            return eventReceived;
        }
        
        private bool TestPoolService(PoolService poolService)
        {
            // Create a simple test prefab
            var testPrefab = new GameObject("TestPoolObject");
            
            try
            {
                // Register a pool
                poolService.RegisterPool("TestPool", testPrefab, 5);
                
                // Get an object from pool
                var pooledObj = poolService.Get("TestPool");
                bool gotObject = pooledObj != null;
                
                if (gotObject)
                {
                    // Return object to pool
                    poolService.Release(pooledObj);
                }
                
                return gotObject;
            }
            catch (System.Exception e)
            {
                LogError($"PoolService test failed: {e.Message}");
                return false;
            }
            finally
            {
                if (testPrefab != null)
                    Destroy(testPrefab);
            }
        }
        
        private void RecordTest(string testName, bool passed)
        {
            var result = new TestResult
            {
                testName = testName,
                passed = passed,
                timestamp = System.DateTime.Now
            };
            
            _testResults.Add(result);
            _totalTests++;
            
            if (passed)
            {
                _passedTests++;
                if (_logVerbose) LogInfo($"✓ {testName}");
            }
            else
            {
                _failedTests++;
                LogError($"✗ {testName}");
            }
        }
        
        private void GenerateFinalReport()
        {
            string report = "\n=== Integration Test Report ===\n";
            report += $"Total Tests: {_totalTests}\n";
            report += $"Passed: {_passedTests}\n";
            report += $"Failed: {_failedTests}\n";
            report += $"Pass Rate: {TestPassRate:P1}\n\n";
            
            if (_failedTests > 0)
            {
                report += "Failed Tests:\n";
                foreach (var result in _testResults)
                {
                    if (!result.passed)
                    {
                        report += $"  - {result.testName}\n";
                    }
                }
                report += "\n";
            }
            
            report += "System Status:\n";
            report += $"  Core Services: {GetSystemStatus("Core")}\n";
            report += $"  Economy: {GetSystemStatus("Economy")}\n";
            report += $"  Building: {GetSystemStatus("Building")}\n";
            report += $"  Combat: {GetSystemStatus("Combat")}\n";
            report += $"  UI: {GetSystemStatus("UI")}\n";
            report += $"  Audio: {GetSystemStatus("Audio")}\n";
            report += $"  Performance: {GetSystemStatus("Performance")}\n";
            
            if (TestPassRate >= 0.9f)
            {
                report += "\n🎉 All systems operational! Game ready for deployment.";
            }
            else if (TestPassRate >= 0.7f)
            {
                report += "\n⚠️ Some issues detected. Review failed tests before deployment.";
            }
            else
            {
                report += "\n❌ Critical issues detected. Game not ready for deployment.";
            }
            
            Debug.Log(report);
        }
        
        private string GetSystemStatus(string systemName)
        {
            var systemTests = _testResults.FindAll(r => r.testName.ToLower().Contains(systemName.ToLower()));
            if (systemTests.Count == 0) return "Not Tested";
            
            int passed = systemTests.FindAll(r => r.passed).Count;
            float passRate = (float)passed / systemTests.Count;
            
            if (passRate >= 0.9f) return "✓ Operational";
            if (passRate >= 0.7f) return "⚠ Issues Detected";
            return "❌ Critical Issues";
        }
        
        private void LogInfo(string message)
        {
            Debug.Log($"[IntegrationTest] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[IntegrationTest] {message}");
        }
        
        // Public API
        public void RunSpecificTest(string testCategory)
        {
            StartCoroutine(RunSpecificTestCoroutine(testCategory));
        }
        
        private IEnumerator RunSpecificTestCoroutine(string testCategory)
        {
            _isRunningTests = true;
            
            switch (testCategory.ToLower())
            {
                case "core":
                    yield return StartCoroutine(TestCoreServices());
                    break;
                case "economy":
                    yield return StartCoroutine(TestEconomySystem());
                    break;
                case "building":
                    yield return StartCoroutine(TestBuildingSystem());
                    break;
                case "combat":
                    yield return StartCoroutine(TestCombatSystem());
                    break;
                case "ui":
                    yield return StartCoroutine(TestUISystem());
                    break;
                case "audio":
                    yield return StartCoroutine(TestAudioSystem());
                    break;
                case "performance":
                    yield return StartCoroutine(TestPerformanceSystem());
                    break;
            }
            
            _isRunningTests = false;
        }
    }
    
    [System.Serializable]
    public class TestResult
    {
        public string testName;
        public bool passed;
        public System.DateTime timestamp;
        public string errorMessage;
    }
}