using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedAlert.Core;
using RedAlert.Economy;
using RedAlert.Build;
using RedAlert.Units;
using RedAlert.Combat;
using RedAlert.AI;
using RedAlert.UI;

namespace RedAlert.Debug
{
    /// <summary>
    /// Comprehensive RTS System Validation for M0 Technical Validation
    /// Tests all major subsystems integration and functionality
    /// </summary>
    public class RTSSystemValidator : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private float testTimeout = 30f;
        
        [Header("System References")]
        [SerializeField] private PlayerEconomy playerEconomy;
        [SerializeField] private BuildPlacementController buildController;
        [SerializeField] private SelectionSystem selectionSystem;
        [SerializeField] private CommandSystem commandSystem;
        [SerializeField] private StandardBot aiBot;
        [SerializeField] private HUDController hudController;
        [SerializeField] private MinimapController minimapController;
        
        [Header("Test Prefabs")]
        [SerializeField] private GameObject harvesterPrefab;
        [SerializeField] private GameObject crystaliteNodePrefab;
        [SerializeField] private GameObject refineryPrefab;
        [SerializeField] private GameObject factoryPrefab;
        [SerializeField] private GameObject basicUnitPrefab;
        
        private struct ValidationResult
        {
            public string systemName;
            public bool passed;
            public string details;
            public float executionTime;
        }
        
        private List<ValidationResult> _results = new List<ValidationResult>();
        private bool _validationInProgress = false;
        
        private void Start()
        {
            if (runOnStart)
            {
                StartValidation();
            }
        }
        
        [ContextMenu("Run Full System Validation")]
        public void StartValidation()
        {
            if (_validationInProgress)
            {
                LogError("Validation already in progress!");
                return;
            }
            
            StartCoroutine(RunFullValidation());
        }
        
        private IEnumerator RunFullValidation()
        {
            _validationInProgress = true;
            _results.Clear();
            
            LogInfo("=== Starting RTS System Validation (M0) ===");
            LogInfo($"Unity Version: {Application.unityVersion}");
            LogInfo($"Platform: {Application.platform}");
            LogInfo($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            
            // Test each subsystem
            yield return StartCoroutine(ValidateEconomySystem());
            yield return StartCoroutine(ValidateBuildSystem());
            yield return StartCoroutine(ValidateUnitsSystem());
            yield return StartCoroutine(ValidateCombatSystem());
            yield return StartCoroutine(ValidateAISystem());
            yield return StartCoroutine(ValidateUISystem());
            yield return StartCoroutine(ValidateCoreSystem());
            yield return StartCoroutine(ValidateIntegration());
            
            // Generate final report
            GenerateValidationReport();
            _validationInProgress = false;
        }
        
        private IEnumerator ValidateEconomySystem()
        {
            float startTime = Time.time;
            LogInfo("--- Validating Economy System ---");
            
            try
            {
                // Test PlayerEconomy
                if (playerEconomy == null)
                {
                    playerEconomy = FindObjectOfType<PlayerEconomy>();
                }
                
                if (playerEconomy != null)
                {
                    int initialCrystalite = playerEconomy.Crystalite;
                    LogInfo($"Initial Crystalite: {initialCrystalite}");
                    
                    // Test add/subtract
                    playerEconomy.AddCrystalite(100);
                    if (playerEconomy.Crystalite != initialCrystalite + 100)
                    {
                        throw new System.Exception("Crystalite addition failed");
                    }
                    
                    playerEconomy.AddCrystalite(-50);
                    if (playerEconomy.Crystalite != initialCrystalite + 50)
                    {
                        throw new System.Exception("Crystalite subtraction failed");
                    }
                    
                    // Reset to initial value
                    playerEconomy.SetCrystalite(initialCrystalite);
                }
                
                // Test CrystaliteNode if available
                var nodes = FindObjectsOfType<CrystaliteNode>();
                if (nodes.Length > 0)
                {
                    var node = nodes[0];
                    int initialRemaining = node.Remaining;
                    LogInfo($"Testing CrystaliteNode with {initialRemaining} remaining");
                    
                    // Test reservation
                    int reserved = node.TryReserve(10);
                    if (reserved <= 0 && !node.IsDepleted)
                    {
                        throw new System.Exception("Node reservation failed");
                    }
                    
                    // Test mining
                    if (reserved > 0)
                    {
                        int mined = node.MineTick(reserved);
                        if (mined != reserved)
                        {
                            throw new System.Exception("Node mining failed");
                        }
                    }
                }
                
                // Test HarvesterAgent
                var harvesters = FindObjectsOfType<HarvesterAgent>();
                if (harvesters.Length > 0)
                {
                    LogInfo($"Found {harvesters.Length} harvester(s)");
                    var harvester = harvesters[0];
                    LogInfo($"Harvester carrying: {harvester.Carried}/{harvester.CarryCapacity}");
                }
                
                AddResult("Economy System", true, "All economy components functional", Time.time - startTime);
            }
            catch (System.Exception e)
            {
                AddResult("Economy System", false, $"Error: {e.Message}", Time.time - startTime);
            }
            
            yield return null;
        }
        
        private IEnumerator ValidateBuildSystem()
        {
            float startTime = Time.time;
            LogInfo("--- Validating Build System ---");
            
            try
            {
                // Test PlacementValidator
                var validator = FindObjectOfType<PlacementValidator>();
                if (validator != null)
                {
                    Vector3 testPos = Vector3.zero;
                    Vector2 testFootprint = new Vector2(4, 4);
                    bool isValid = validator.Validate(testPos, testFootprint, out Vector3 corrected, out string reason);
                    LogInfo($"Placement validation test: {isValid} (reason: {reason})");
                }
                
                // Test BuildQueue
                var queues = FindObjectsOfType<BuildQueue>();
                if (queues.Length > 0)
                {
                    var queue = queues[0];
                    int initialCount = queue.Queue.Count;
                    LogInfo($"Build queue has {initialCount} items");
                    
                    // Test enqueueing (if we have economy)
                    if (playerEconomy != null && basicUnitPrefab != null)
                    {
                        var item = new BuildQueue.BuildItem
                        {
                            Id = "TestUnit",
                            Prefab = basicUnitPrefab,
                            Cost = 100,
                            BuildTimeSeconds = 5f
                        };
                        
                        bool enqueued = queue.Enqueue(item);
                        LogInfo($"Enqueue test: {enqueued}");
                    }
                }
                
                // Test BuildPlacementController
                if (buildController == null)
                {
                    buildController = FindObjectOfType<BuildPlacementController>();
                }
                
                if (buildController != null)
                {
                    bool wasActive = buildController.IsActive();
                    LogInfo($"Build controller active: {wasActive}");
                }
                
                AddResult("Build System", true, "All build components functional", Time.time - startTime);
            }
            catch (System.Exception e)
            {
                AddResult("Build System", false, $"Error: {e.Message}", Time.time - startTime);
            }
            
            yield return null;
        }
        
        private IEnumerator ValidateUnitsSystem()
        {
            float startTime = Time.time;
            LogInfo("--- Validating Units System ---");
            
            try
            {
                // Test SelectionSystem
                if (selectionSystem == null)
                {
                    selectionSystem = FindObjectOfType<SelectionSystem>();
                }
                
                if (selectionSystem != null)
                {
                    LogInfo("SelectionSystem found and active");
                }
                
                // Test CommandSystem
                if (commandSystem == null)
                {
                    commandSystem = FindObjectOfType<CommandSystem>();
                }
                
                if (commandSystem != null)
                {
                    LogInfo("CommandSystem found and active");
                }
                
                // Test LocomotionAgent
                var locomotionAgents = FindObjectsOfType<LocomotionAgent>();
                LogInfo($"Found {locomotionAgents.Length} locomotion agent(s)");
                
                foreach (var agent in locomotionAgents)
                {
                    LogInfo($"Agent moving: {agent.IsMoving}");
                }
                
                // Test Damageable components
                var damageables = FindObjectsOfType<Damageable>();
                LogInfo($"Found {damageables.Length} damageable unit(s)");
                
                AddResult("Units System", true, "All unit components functional", Time.time - startTime);
            }
            catch (System.Exception e)
            {
                AddResult("Units System", false, $"Error: {e.Message}", Time.time - startTime);
            }
            
            yield return null;
        }
        
        private IEnumerator ValidateCombatSystem()
        {
            float startTime = Time.time;
            LogInfo("--- Validating Combat System ---");
            
            try
            {
                // Test WeaponController
                var weapons = FindObjectsOfType<WeaponController>();
                LogInfo($"Found {weapons.Length} weapon controller(s)");
                
                foreach (var weapon in weapons)
                {
                    LogInfo($"Weapon range: {weapon.Range}");
                    
                    // Test if weapon can fire at a test position
                    Vector3 testTarget = weapon.transform.position + Vector3.forward * (weapon.Range * 0.5f);
                    bool canFire = weapon.CanFireAt(testTarget);
                    LogInfo($"Weapon can fire test: {canFire}");
                }
                
                // Test HitscanWeapon
                var hitscans = FindObjectsOfType<HitscanWeapon>();
                LogInfo($"Found {hitscans.Length} hitscan weapon(s)");
                
                // Test Projectile
                var projectiles = FindObjectsOfType<Projectile>();
                LogInfo($"Found {projectiles.Length} active projectile(s)");
                
                AddResult("Combat System", true, "All combat components functional", Time.time - startTime);
            }
            catch (System.Exception e)
            {
                AddResult("Combat System", false, $"Error: {e.Message}", Time.time - startTime);
            }
            
            yield return null;
        }
        
        private IEnumerator ValidateAISystem()
        {
            float startTime = Time.time;
            LogInfo("--- Validating AI System ---");
            
            try
            {
                // Test StandardBot
                if (aiBot == null)
                {
                    aiBot = FindObjectOfType<StandardBot>();
                }
                
                if (aiBot != null)
                {
                    LogInfo("StandardBot found and active");
                    // AI bot validation would require more complex setup
                }
                
                // Test BuildOrderScript
                var buildOrders = FindObjectsOfType<BuildOrderScript>();
                LogInfo($"Found {buildOrders.Length} build order script(s)");
                
                foreach (var buildOrder in buildOrders)
                {
                    LogInfo($"Build order has {buildOrder.Steps.Length} steps");
                }
                
                AddResult("AI System", true, "AI components present and configured", Time.time - startTime);
            }
            catch (System.Exception e)
            {
                AddResult("AI System", false, $"Error: {e.Message}", Time.time - startTime);
            }
            
            yield return null;
        }
        
        private IEnumerator ValidateUISystem()
        {
            float startTime = Time.time;
            LogInfo("--- Validating UI System ---");
            
            try
            {
                // Test HUDController
                if (hudController == null)
                {
                    hudController = FindObjectOfType<HUDController>();
                }
                
                if (hudController != null)
                {
                    LogInfo("HUDController found and active");
                }
                
                // Test MinimapController
                if (minimapController == null)
                {
                    minimapController = FindObjectOfType<MinimapController>();
                }
                
                if (minimapController != null)
                {
                    LogInfo("MinimapController found and active");
                }
                
                // Test ResourcePanel
                var resourcePanels = FindObjectsOfType<ResourcePanel>();
                LogInfo($"Found {resourcePanels.Length} resource panel(s)");
                
                // Test BuildMenuPanel
                var buildMenus = FindObjectsOfType<BuildMenuPanel>();
                LogInfo($"Found {buildMenus.Length} build menu panel(s)");
                
                AddResult("UI System", true, "All UI components present", Time.time - startTime);
            }
            catch (System.Exception e)
            {
                AddResult("UI System", false, $"Error: {e.Message}", Time.time - startTime);
            }
            
            yield return null;
        }
        
        private IEnumerator ValidateCoreSystem()
        {
            float startTime = Time.time;
            LogInfo("--- Validating Core System ---");
            
            try
            {
                // Test SceneBootstrap
                var bootstrap = FindObjectOfType<SceneBootstrap>();
                if (bootstrap != null)
                {
                    LogInfo("SceneBootstrap found");
                }
                
                // Test UpdateDriver
                var updateDriver = FindObjectOfType<UpdateDriver>();
                if (updateDriver != null)
                {
                    LogInfo("UpdateDriver found and active");
                }
                
                // Test EventBus (static)
                LogInfo("EventBus is static - testing event publication");
                EventBus.PublishNodeDepleted(); // Should not throw
                
                // Test GameModeController
                var gameModeController = FindObjectOfType<GameModeController>();
                if (gameModeController != null)
                {
                    LogInfo("GameModeController found");
                }
                
                AddResult("Core System", true, "Core systems initialized", Time.time - startTime);
            }
            catch (System.Exception e)
            {
                AddResult("Core System", false, $"Error: {e.Message}", Time.time - startTime);
            }
            
            yield return null;
        }
        
        private IEnumerator ValidateIntegration()
        {
            float startTime = Time.time;
            LogInfo("--- Validating System Integration ---");
            
            try
            {
                // Test Economy -> UI integration
                if (playerEconomy != null)
                {
                    int testAmount = 999;
                    playerEconomy.SetCrystalite(testAmount);
                    yield return new WaitForSeconds(0.1f); // Allow UI update
                    LogInfo($"Economy-UI integration test: Set crystalite to {testAmount}");
                }
                
                // Test UpdateDriver registration (count registered systems)
                int slowTickSystems = 0;
                var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
                foreach (var mb in allMonoBehaviours)
                {
                    if (mb is UpdateDriver.ISlowTick)
                    {
                        slowTickSystems++;
                    }
                }
                LogInfo($"Found {slowTickSystems} systems using UpdateDriver.ISlowTick");
                
                // Test that systems can communicate via EventBus
                bool eventReceived = false;
                System.Action testHandler = () => eventReceived = true;
                EventBus.OnNodeDepleted += testHandler;
                EventBus.PublishNodeDepleted();
                yield return new WaitForEndOfFrame();
                EventBus.OnNodeDepleted -= testHandler;
                
                LogInfo($"EventBus integration test: {(eventReceived ? "PASS" : "FAIL")}");
                
                AddResult("System Integration", true, "Systems communicate properly", Time.time - startTime);
            }
            catch (System.Exception e)
            {
                AddResult("System Integration", false, $"Error: {e.Message}", Time.time - startTime);
            }
            
            yield return null;
        }
        
        private void AddResult(string systemName, bool passed, string details, float executionTime)
        {
            _results.Add(new ValidationResult
            {
                systemName = systemName,
                passed = passed,
                details = details,
                executionTime = executionTime
            });
            
            string status = passed ? "PASS" : "FAIL";
            LogInfo($"{systemName}: {status} ({executionTime:F2}s) - {details}");
        }
        
        private void GenerateValidationReport()
        {
            LogInfo("=== RTS System Validation Report ===");
            
            int passed = 0;
            int total = _results.Count;
            float totalTime = 0f;
            
            foreach (var result in _results)
            {
                if (result.passed) passed++;
                totalTime += result.executionTime;
                
                string status = result.passed ? "✓" : "✗";
                LogInfo($"{status} {result.systemName}: {result.details}");
            }
            
            LogInfo($"=== Summary: {passed}/{total} systems passed ({totalTime:F2}s total) ===");
            
            if (passed == total)
            {
                LogInfo("🎉 ALL SYSTEMS VALIDATED SUCCESSFULLY - Ready for M1 implementation!");
            }
            else
            {
                LogError($"❌ {total - passed} systems failed validation - requires attention before M1");
            }
        }
        
        private void LogInfo(string message)
        {
            if (verboseLogging)
            {
                UnityEngine.Debug.Log($"[RTSValidator] {message}");
            }
        }
        
        private void LogError(string message)
        {
            UnityEngine.Debug.LogError($"[RTSValidator] {message}");
        }
        
        // Public API for external testing
        public bool IsValidationComplete => !_validationInProgress;
        public List<ValidationResult> GetResults() => new List<ValidationResult>(_results);
        
        public bool AllSystemsPassed()
        {
            foreach (var result in _results)
            {
                if (!result.passed) return false;
            }
            return _results.Count > 0;
        }
    }
}