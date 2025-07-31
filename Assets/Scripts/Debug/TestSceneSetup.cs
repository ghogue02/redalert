using UnityEngine;
using UnityEngine.SceneManagement;
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
    /// Automated test scene setup for RTS subsystem validation
    /// Creates necessary GameObjects and components for testing
    /// </summary>
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private TestSceneType sceneType = TestSceneType.FullIntegration;
        
        [Header("Test Prefabs")]
        [SerializeField] private GameObject[] testPrefabs;
        
        [Header("Test Configuration")]
        [SerializeField] private Vector3 testAreaCenter = Vector3.zero;
        [SerializeField] private Vector3 testAreaSize = new Vector3(50, 10, 50);
        [SerializeField] private int testUnitCount = 5;
        [SerializeField] private int testNodeCount = 3;
        
        public enum TestSceneType
        {
            CoreSystems,
            EconomyOnly,
            BuildOnly,
            UnitsOnly,
            CombatOnly,
            AIOnly,
            UIOnly,
            FullIntegration
        }
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupTestScene();
            }
        }
        
        [ContextMenu("Setup Test Scene")]
        public void SetupTestScene()
        {
            Debug.Log($"Setting up test scene: {sceneType}");
            
            // Clear existing test objects
            ClearTestObjects();
            
            // Setup based on scene type
            switch (sceneType)
            {
                case TestSceneType.CoreSystems:
                    SetupCoreSystemsTest();
                    break;
                case TestSceneType.EconomyOnly:
                    SetupEconomyTest();
                    break;
                case TestSceneType.BuildOnly:
                    SetupBuildTest();
                    break;
                case TestSceneType.UnitsOnly:
                    SetupUnitsTest();
                    break;
                case TestSceneType.CombatOnly:
                    SetupCombatTest();
                    break;
                case TestSceneType.AIOnly:
                    SetupAITest();
                    break;
                case TestSceneType.UIOnly:
                    SetupUITest();
                    break;
                case TestSceneType.FullIntegration:
                    SetupFullIntegrationTest();
                    break;
            }
            
            Debug.Log("Test scene setup complete!");
        }
        
        private void ClearTestObjects()
        {
            // Remove any existing test objects
            var testObjects = GameObject.FindGameObjectsWithTag("TestObject");
            for (int i = testObjects.Length - 1; i >= 0; i--)
            {
                DestroyImmediate(testObjects[i]);
            }
        }
        
        private void SetupCoreSystemsTest()
        {
            Debug.Log("Setting up Core Systems test...");
            
            // Create core systems container
            var coreContainer = CreateTestObject("CoreSystems");
            
            // Add SceneBootstrap
            var bootstrap = coreContainer.AddComponent<SceneBootstrap>();
            
            // Add UpdateDriver
            var updateDriver = coreContainer.AddComponent<UpdateDriver>();
            
            // Add EventBus
            var eventBus = coreContainer.AddComponent<EventBus>();
            
            // Add GameModeController
            var gameModeController = coreContainer.AddComponent<GameModeController>();
            
            // Add Performance Validator
            var perfValidator = coreContainer.AddComponent<WebGLPerformanceValidator>();
            
            // Add System Validator
            var systemValidator = coreContainer.AddComponent<RTSSystemValidator>();
        }
        
        private void SetupEconomyTest()
        {
            Debug.Log("Setting up Economy test...");
            
            // Setup core systems first
            SetupCoreSystemsTest();
            
            // Create economy container
            var economyContainer = CreateTestObject("EconomySystem");
            
            // Add PlayerEconomy
            var playerEconomy = economyContainer.AddComponent<PlayerEconomy>();
            
            // Create test crystalite nodes
            for (int i = 0; i < testNodeCount; i++)
            {
                var nodeObj = CreateTestObject($"CrystaliteNode_{i}");
                nodeObj.transform.position = testAreaCenter + new Vector3(
                    Random.Range(-testAreaSize.x * 0.5f, testAreaSize.x * 0.5f),
                    0,
                    Random.Range(-testAreaSize.z * 0.5f, testAreaSize.z * 0.5f)
                );
                
                var node = nodeObj.AddComponent<CrystaliteNode>();
                
                // Add collider for harvester detection
                var collider = nodeObj.AddComponent<SphereCollider>();
                collider.radius = 5f;
                collider.isTrigger = false;
                
                // Visual representation
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(nodeObj.transform);
                cube.transform.localPosition = Vector3.zero;
                cube.GetComponent<Renderer>().material.color = Color.cyan;
                DestroyImmediate(cube.GetComponent<Collider>());
            }
            
            // Create test refinery
            var refineryObj = CreateTestObject("TestRefinery");
            refineryObj.transform.position = testAreaCenter + Vector3.right * 10;
            var refinery = refineryObj.AddComponent<Refinery>();
            
            // Add dock points
            var dockParent = new GameObject("DockPoints");
            dockParent.transform.SetParent(refineryObj.transform);
            
            for (int i = 0; i < 2; i++)
            {
                var dock = new GameObject($"Dock_{i}");
                dock.transform.SetParent(dockParent.transform);
                dock.transform.localPosition = new Vector3(i * 3, 0, 3);
            }
            
            // Add trigger collider
            var triggerCollider = refineryObj.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(8, 4, 8);
            
            // Visual representation
            var refineryCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            refineryCube.transform.SetParent(refineryObj.transform);
            refineryCube.transform.localPosition = Vector3.zero;
            refineryCube.transform.localScale = Vector3.one * 3;
            refineryCube.GetComponent<Renderer>().material.color = Color.green;
            DestroyImmediate(refineryCube.GetComponent<Collider>());
            
            // Create test harvester
            var harvesterObj = CreateTestObject("TestHarvester");
            harvesterObj.transform.position = testAreaCenter + Vector3.left * 5;
            harvesterObj.layer = LayerMask.NameToLayer("Units");
            
            var harvester = harvesterObj.AddComponent<HarvesterAgent>();
            var locomotion = harvesterObj.AddComponent<LocomotionAgent>();
            
            // Add NavMeshAgent
            var navAgent = harvesterObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            
            // Add collider
            var harvesterCollider = harvesterObj.AddComponent<CapsuleCollider>();
            
            // Visual representation
            var harvesterCube = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            harvesterCube.transform.SetParent(harvesterObj.transform);
            harvesterCube.transform.localPosition = Vector3.zero;
            harvesterCube.GetComponent<Renderer>().material.color = Color.yellow;
            DestroyImmediate(harvesterCube.GetComponent<Collider>());
        }
        
        private void SetupBuildTest()
        {
            Debug.Log("Setting up Build test...");
            
            // Setup core and economy systems
            SetupEconomyTest();
            
            // Create build container
            var buildContainer = CreateTestObject("BuildSystem");
            
            // Add PlacementValidator
            var validator = buildContainer.AddComponent<PlacementValidator>();
            
            // Add BuildPlacementController
            var buildController = buildContainer.AddComponent<BuildPlacementController>();
            
            // Create test factory with BuildQueue
            var factoryObj = CreateTestObject("TestFactory");
            factoryObj.transform.position = testAreaCenter + Vector3.forward * 10;
            
            var buildQueue = factoryObj.AddComponent<BuildQueue>();
            
            // Add exit and rally points
            var exitObj = new GameObject("Exit");
            exitObj.transform.SetParent(factoryObj.transform);
            exitObj.transform.localPosition = Vector3.forward * 3;
            
            var rallyObj = new GameObject("Rally");
            rallyObj.transform.SetParent(factoryObj.transform);
            rallyObj.transform.localPosition = Vector3.forward * 6;
            
            buildQueue.SetExit(exitObj.transform);
            buildQueue.SetRally(rallyObj.transform);
            
            // Visual representation
            var factoryCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            factoryCube.transform.SetParent(factoryObj.transform);
            factoryCube.transform.localPosition = Vector3.zero;
            factoryCube.transform.localScale = new Vector3(4, 2, 4);
            factoryCube.GetComponent<Renderer>().material.color = Color.blue;
            DestroyImmediate(factoryCube.GetComponent<Collider>());
        }
        
        private void SetupUnitsTest()
        {
            Debug.Log("Setting up Units test...");
            
            // Setup core systems
            SetupCoreSystemsTest();
            
            // Create units container
            var unitsContainer = CreateTestObject("UnitsSystem");
            
            // Add SelectionSystem
            var selectionSystem = unitsContainer.AddComponent<SelectionSystem>();
            
            // Add CommandSystem
            var commandSystem = unitsContainer.AddComponent<CommandSystem>();
            
            // Create test units
            for (int i = 0; i < testUnitCount; i++)
            {
                var unitObj = CreateTestObject($"TestUnit_{i}");
                unitObj.transform.position = testAreaCenter + new Vector3(
                    Random.Range(-10f, 10f),
                    0,
                    Random.Range(-10f, 10f)
                );
                unitObj.layer = LayerMask.NameToLayer("Units");
                
                // Add unit components
                var selectable = unitObj.AddComponent<SelectableFlag>();
                var locomotion = unitObj.AddComponent<LocomotionAgent>();
                var damageable = unitObj.AddComponent<Damageable>();
                var team = unitObj.AddComponent<Team>();
                team.DebugSetTeam(0); // Player team
                
                // Add NavMeshAgent
                var navAgent = unitObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
                
                // Add collider
                var collider = unitObj.AddComponent<CapsuleCollider>();
                
                // Visual representation
                var unitCube = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                unitCube.transform.SetParent(unitObj.transform);
                unitCube.transform.localPosition = Vector3.zero;
                unitCube.GetComponent<Renderer>().material.color = Color.white;
                DestroyImmediate(unitCube.GetComponent<Collider>());
            }
        }
        
        private void SetupCombatTest()
        {
            Debug.Log("Setting up Combat test...");
            
            // Setup units test first
            SetupUnitsTest();
            
            // Add weapons to some units
            var units = GameObject.FindGameObjectsWithTag("TestObject");
            int weaponCount = 0;
            
            foreach (var unitObj in units)
            {
                if (unitObj.name.StartsWith("TestUnit_") && weaponCount < 2)
                {
                    // Add weapon components
                    var weaponController = unitObj.AddComponent<WeaponController>();
                    var hitscanWeapon = unitObj.AddComponent<HitscanWeapon>();
                    
                    weaponCount++;
                }
            }
            
            // Create enemy units
            for (int i = 0; i < 2; i++)
            {
                var enemyObj = CreateTestObject($"EnemyUnit_{i}");
                enemyObj.transform.position = testAreaCenter + new Vector3(20 + i * 3, 0, 0);
                enemyObj.layer = LayerMask.NameToLayer("Units");
                
                var damageable = enemyObj.AddComponent<Damageable>();
                var team = enemyObj.AddComponent<Team>();
                team.DebugSetTeam(1); // Enemy team
                
                // Add collider
                var collider = enemyObj.AddComponent<CapsuleCollider>();
                
                // Visual representation
                var enemyCube = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyCube.transform.SetParent(enemyObj.transform);
                enemyCube.transform.localPosition = Vector3.zero;
                enemyCube.GetComponent<Renderer>().material.color = Color.red;
                DestroyImmediate(enemyCube.GetComponent<Collider>());
            }
        }
        
        private void SetupAITest()
        {
            Debug.Log("Setting up AI test...");
            
            // Setup build and combat tests
            SetupCombatTest();
            
            // Create AI container
            var aiContainer = CreateTestObject("AISystem");
            
            // Add StandardBot
            var aiBot = aiContainer.AddComponent<StandardBot>();
            
            // Add BuildOrderScript
            var buildOrder = aiContainer.AddComponent<BuildOrderScript>();
            
            // Set enemy HQ for AI targeting
            var enemyHQ = CreateTestObject("EnemyHQ");
            enemyHQ.transform.position = testAreaCenter + new Vector3(30, 0, 0);
            enemyHQ.tag = "EnemyHQ";
            
            // Visual representation
            var hqCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hqCube.transform.SetParent(enemyHQ.transform);
            hqCube.transform.localPosition = Vector3.zero;
            hqCube.transform.localScale = Vector3.one * 5;
            hqCube.GetComponent<Renderer>().material.color = Color.magenta;
        }
        
        private void SetupUITest()
        {
            Debug.Log("Setting up UI test...");
            
            // Setup economy test for UI binding
            SetupEconomyTest();
            
            // Create UI Canvas
            var canvasObj = new GameObject("TestCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add HUDController
            var hudController = canvasObj.AddComponent<HUDController>();
            
            // Create ResourcePanel
            var resourcePanelObj = new GameObject("ResourcePanel");
            resourcePanelObj.transform.SetParent(canvasObj.transform);
            var resourcePanel = resourcePanelObj.AddComponent<ResourcePanel>();
            
            // Create MinimapController
            var minimapContainer = CreateTestObject("MinimapSystem");
            var minimapController = minimapContainer.AddComponent<MinimapController>();
            
            // Create BuildMenuPanel
            var buildMenuObj = new GameObject("BuildMenuPanel");
            buildMenuObj.transform.SetParent(canvasObj.transform);
            var buildMenu = buildMenuObj.AddComponent<BuildMenuPanel>();
        }
        
        private void SetupFullIntegrationTest()
        {
            Debug.Log("Setting up Full Integration test...");
            
            // Setup all systems
            SetupCoreSystemsTest();
            SetupEconomyTest();
            SetupBuildTest();
            SetupUnitsTest();
            SetupCombatTest();
            SetupAITest();
            SetupUITest();
            
            // Create ground plane for NavMesh
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = testAreaCenter;
            ground.transform.localScale = Vector3.one * 10;
            ground.GetComponent<Renderer>().material.color = Color.gray;
            
            // Add NavMesh Surface
            var navMeshSurface = ground.AddComponent<UnityEngine.AI.NavMeshSurface>();
            navMeshSurface.BuildNavMesh();
            
            Debug.Log("Full integration test scene ready!");
        }
        
        private GameObject CreateTestObject(string name)
        {
            var obj = new GameObject(name);
            obj.tag = "TestObject";
            return obj;
        }
        
        [ContextMenu("Generate Performance Report")]
        public void GeneratePerformanceReport()
        {
            var perfValidator = FindObjectOfType<WebGLPerformanceValidator>();
            if (perfValidator != null)
            {
                var metrics = perfValidator.GetCurrentMetrics();
                Debug.Log($"=== Performance Report ===");
                Debug.Log($"Frame Time: {metrics.frameTimeMs:F2}ms");
                Debug.Log($"Memory: {metrics.memoryUsageMB:F1}MB");
                Debug.Log($"Draw Calls: {metrics.drawCalls}");
                Debug.Log($"Performance Within Budgets: {perfValidator.IsPerformanceWithinBudgets()}");
            }
        }
        
        [ContextMenu("Run System Validation")]
        public void RunSystemValidation()
        {
            var systemValidator = FindObjectOfType<RTSSystemValidator>();
            if (systemValidator != null)
            {
                systemValidator.StartValidation();
            }
            else
            {
                Debug.LogWarning("No RTSSystemValidator found in scene!");
            }
        }
    }
}