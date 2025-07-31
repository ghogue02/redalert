using UnityEngine;
using UnityEngine.AI;
using RedAlert.Build;
using RedAlert.Economy;
using RedAlert.Units;
using System.Collections.Generic;

namespace RedAlert.AI
{
    /// <summary>
    /// Sets up the AI opponent system including base buildings, economy, and the StandardBot AI controller.
    /// Creates a functional AI that can build, gather resources, and attack the player.
    /// </summary>
    public class AISystemSpawner : MonoBehaviour
    {
        [Header("AI Configuration")]
        [SerializeField] private int _aiTeamId = 2;
        [SerializeField] private bool _spawnOnStart = true;
        
        [Header("AI Base Layout")]
        [SerializeField] private Vector3 _aiBaseCenter = new Vector3(15f, 0f, 15f);
        [SerializeField] private Vector2 _baseAreaSize = new Vector2(20f, 20f);
        [SerializeField] private float _factorySpacing = 8f;
        [SerializeField] private float _refinerySpacing = 10f;
        
        [Header("AI Units")]
        [SerializeField] private int _initialUnits = 3;
        [SerializeField] private GameObject _basicUnitPrefab;
        
        [Header("AI Buildings")]
        [SerializeField] private int _factoriesToSpawn = 1;
        [SerializeField] private int _refineriesToSpawn = 1;
        [SerializeField] private int _harvestersToSpawn = 2;
        
        [Header("AI Behavior")]
        [SerializeField] private int _armyValueThreshold = 5;
        [SerializeField] private Vector2 _waveWindowSeconds = new Vector2(120f, 180f);
        [SerializeField] private float _retreatHpFraction = 0.4f;
        
        [Header("Layers")]
        [SerializeField] private int _buildingsLayer = 9;
        [SerializeField] private int _unitsLayer = 8;
        
        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        
        private List<GameObject> _aiBuildings = new List<GameObject>();
        private List<GameObject> _aiUnits = new List<GameObject>();
        private PlayerEconomy _aiEconomy;
        private StandardBot _standardBot;
        private BuildQueue _aiBuildQueue;
        
        private void Start()
        {
            if (_spawnOnStart)
            {
                SpawnAISystem();
            }
        }
        
        /// <summary>
        /// Spawn the complete AI system
        /// </summary>
        public void SpawnAISystem()
        {
            ClearExistingAI();
            
            // Create AI economy
            CreateAIEconomy();
            
            // Spawn AI base buildings
            SpawnAIBuildings();
            
            // Spawn initial AI units
            SpawnAIUnits();
            
            // Set up StandardBot AI controller
            SetupStandardBot();
            
            Debug.Log($"AISystemSpawner: Created AI system for team {_aiTeamId}");
        }
        
        private void CreateAIEconomy()
        {
            GameObject economyObject = new GameObject("AI_PlayerEconomy");
            economyObject.transform.parent = transform;
            
            _aiEconomy = economyObject.AddComponent<PlayerEconomy>();
            
            // Give AI some starting resources
            _aiEconomy.SetCrystalite(2000);
        }
        
        private void SpawnAIBuildings()
        {
            // Spawn AI refineries
            for (int i = 0; i < _refineriesToSpawn; i++)
            {
                Vector3 refineryPos = GetAIBuildingPosition(i, "refinery");
                GameObject refinery = CreateAIRefinery(refineryPos, $"AI_Refinery_{i + 1}");
                _aiBuildings.Add(refinery);
            }
            
            // Spawn AI factories
            for (int i = 0; i < _factoriesToSpawn; i++)
            {
                Vector3 factoryPos = GetAIBuildingPosition(i, "factory");
                GameObject factory = CreateAIFactory(factoryPos, $"AI_Factory_{i + 1}");
                _aiBuildings.Add(factory);
                
                // Keep reference to the first factory's build queue for the AI
                if (i == 0)
                {
                    _aiBuildQueue = factory.GetComponent<BuildQueue>();
                }
            }
            
            // Spawn AI harvesters
            for (int i = 0; i < _harvestersToSpawn; i++)
            {
                Vector3 harvesterPos = GetAIUnitPosition();
                GameObject harvester = CreateAIHarvester(harvesterPos, $"AI_Harvester_{i + 1}");
                _aiUnits.Add(harvester);
            }
        }
        
        private void SpawnAIUnits()
        {
            // Create basic unit prefab if not assigned
            if (_basicUnitPrefab == null)
            {
                _basicUnitPrefab = CreateBasicUnitPrefab();
            }
            
            // Spawn initial army units
            for (int i = 0; i < _initialUnits; i++)
            {
                Vector3 unitPos = GetAIUnitPosition();
                GameObject unit = CreateAIUnit(unitPos, $"AI_Unit_{i + 1}");
                _aiUnits.Add(unit);
            }
        }
        
        private Vector3 GetAIBuildingPosition(int index, string buildingType)
        {
            Vector3 basePos = _aiBaseCenter;
            
            if (buildingType == "refinery")
            {
                basePos.x += (index - (_refineriesToSpawn - 1) * 0.5f) * _refinerySpacing;
            }
            else if (buildingType == "factory")
            {
                basePos.z += (index - (_factoriesToSpawn - 1) * 0.5f) * _factorySpacing;
                basePos.x -= 5f; // Offset from refineries
            }
            
            // Sample NavMesh to ensure valid position
            if (NavMesh.SamplePosition(basePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return basePos;
        }
        
        private Vector3 GetAIUnitPosition()
        {
            // Random position near the AI base
            Vector3 randomOffset = new Vector3(
                Random.Range(-_baseAreaSize.x / 2f, _baseAreaSize.x / 2f),
                0f,
                Random.Range(-_baseAreaSize.y / 2f, _baseAreaSize.y / 2f)
            );
            
            Vector3 spawnPos = _aiBaseCenter + randomOffset;
            
            // Sample NavMesh to ensure valid position
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return _aiBaseCenter;
        }
        
        private GameObject CreateAIRefinery(Vector3 position, string buildingName)
        {
            // Create refinery (similar to EconomySpawner but for AI team)
            GameObject refinery = new GameObject(buildingName);
            refinery.transform.position = position;
            refinery.transform.parent = transform;
            refinery.layer = _buildingsLayer;
            
            // Add visual representation
            CreateBuildingVisual(refinery, new Vector3(4f, 3f, 4f), new Color(0.6f, 0.2f, 0.2f, 1f)); // Red for AI
            
            // Add physics
            BoxCollider trigger = refinery.AddComponent<BoxCollider>();
            trigger.size = new Vector3(6f, 4f, 6f);
            trigger.isTrigger = true;
            
            // Add dock points
            Transform[] dockPoints = CreateDockPoints(refinery);
            
            // Add team identification
            Team team = refinery.AddComponent<Team>();
            team.SetTeam(_aiTeamId);
            
            // Add health system
            Damageable damageable = refinery.AddComponent<Damageable>();
            
            // Add Refinery component
            Refinery refineryComponent = refinery.AddComponent<Refinery>();
            
            // Set references using reflection
            var dockPointsField = typeof(Refinery).GetField("dockPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            dockPointsField?.SetValue(refineryComponent, dockPoints);
            
            var economyField = typeof(Refinery).GetField("_ownerEconomy", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            economyField?.SetValue(refineryComponent, _aiEconomy);
            
            return refinery;
        }
        
        private GameObject CreateAIFactory(Vector3 position, string buildingName)
        {
            // Create factory for AI
            GameObject factory = new GameObject(buildingName);
            factory.transform.position = position;
            factory.transform.parent = transform;
            factory.layer = _buildingsLayer;
            
            // Add visual representation
            CreateBuildingVisual(factory, new Vector3(4f, 3f, 4f), new Color(0.6f, 0.2f, 0.6f, 1f)); // Purple for AI factory
            
            // Add physics
            BoxCollider collider = factory.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 3f, 4f);
            
            // Create exit and rally points
            Transform exitPoint = CreateExitPoint(factory);
            Transform rallyPoint = CreateRallyPoint(factory);
            
            // Add team identification
            Team team = factory.AddComponent<Team>();
            team.SetTeam(_aiTeamId);
            
            // Add health system
            Damageable damageable = factory.AddComponent<Damageable>();
            
            // Add BuildQueue component
            BuildQueue buildQueue = factory.AddComponent<BuildQueue>();
            
            // Set references using reflection
            var economyField = typeof(BuildQueue).GetField("_economy", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            economyField?.SetValue(buildQueue, _aiEconomy);
            
            var exitField = typeof(BuildQueue).GetField("_exitPoint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            exitField?.SetValue(buildQueue, exitPoint);
            
            var rallyField = typeof(BuildQueue).GetField("_rallyPoint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rallyField?.SetValue(buildQueue, rallyPoint);
            
            return factory;
        }
        
        private GameObject CreateAIHarvester(Vector3 position, string unitName)
        {
            // Create harvester for AI (similar to EconomySpawner but for AI team)
            GameObject harvester = new GameObject(unitName);
            harvester.transform.position = position;
            harvester.transform.parent = transform;
            harvester.layer = _unitsLayer;
            
            // Add visual representation  
            CreateUnitVisual(harvester, new Vector3(1.5f, 1f, 2f), new Color(1f, 0.4f, 0.2f, 1f)); // Orange for AI harvester
            
            // Add required components
            AddHarvesterComponents(harvester);
            
            return harvester;
        }
        
        private GameObject CreateAIUnit(Vector3 position, string unitName)
        {
            // Create basic combat unit for AI
            GameObject unit = new GameObject(unitName);
            unit.transform.position = position;
            unit.transform.parent = transform;
            unit.layer = _unitsLayer;
            
            // Add visual representation
            CreateUnitVisual(unit, new Vector3(1f, 1f, 1f), new Color(1f, 0.2f, 0.2f, 1f)); // Red for AI units
            
            // Add required components
            AddBasicUnitComponents(unit);
            
            return unit;
        }
        
        private void CreateBuildingVisual(GameObject building, Vector3 scale, Color color)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.parent = building.transform;
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = scale;
            body.name = "BuildingBody";
            
            // Remove default collider
            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null) DestroyImmediate(bodyCollider);
            
            // Set material
            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.SetFloat("_Metallic", 0.3f);
                mat.SetFloat("_Smoothness", 0.2f);
                renderer.material = mat;
            }
        }
        
        private void CreateUnitVisual(GameObject unit, Vector3 scale, Color color)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.parent = unit.transform;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = scale;
            visual.name = "UnitVisual";
            
            // Remove default collider
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null) DestroyImmediate(visualCollider);
            
            // Set material
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }
        }
        
        private Transform[] CreateDockPoints(GameObject building)
        {
            List<Transform> dockPoints = new List<Transform>();
            
            Vector3[] offsets = {
                new Vector3(3.5f, 0f, 0f),
                new Vector3(-3.5f, 0f, 0f),
                new Vector3(0f, 0f, 3.5f),
                new Vector3(0f, 0f, -3.5f)
            };
            
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject dockPoint = new GameObject($"DockPoint_{i}");
                dockPoint.transform.parent = building.transform;
                dockPoint.transform.localPosition = offsets[i];
                dockPoints.Add(dockPoint.transform);
            }
            
            return dockPoints.ToArray();
        }
        
        private Transform CreateExitPoint(GameObject factory)
        {
            GameObject exitObject = new GameObject("Exit");
            exitObject.transform.parent = factory.transform;
            exitObject.transform.localPosition = new Vector3(0f, 0f, 2.5f);
            return exitObject.transform;
        }
        
        private Transform CreateRallyPoint(GameObject factory)
        {
            GameObject rallyObject = new GameObject("Rally");
            rallyObject.transform.parent = factory.transform;
            rallyObject.transform.localPosition = new Vector3(0f, 0f, 5f);
            return rallyObject.transform;
        }
        
        private void AddHarvesterComponents(GameObject harvester)
        {
            // Add physics
            Rigidbody rb = harvester.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.drag = 5f;
            rb.angularDrag = 5f;
            rb.freezeRotation = true;
            
            // Add collider
            BoxCollider collider = harvester.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.5f, 1f, 2f);
            
            // Add NavMeshAgent
            NavMeshAgent agent = harvester.AddComponent<NavMeshAgent>();
            agent.speed = 4f;
            agent.acceleration = 6f;
            agent.angularSpeed = 180f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            
            // Add LocomotionAgent wrapper
            LocomotionAgent locomotion = harvester.AddComponent<LocomotionAgent>();
            
            // Add health system
            Damageable damageable = harvester.AddComponent<Damageable>();
            
            // Add team identification
            Team team = harvester.AddComponent<Team>();
            team.SetTeam(_aiTeamId);
            
            // Add selection system
            SelectableFlag selectable = harvester.AddComponent<SelectableFlag>();
            
            // Add harvester-specific component
            HarvesterAgent harvesterAgent = harvester.AddComponent<HarvesterAgent>();
        }
        
        private void AddBasicUnitComponents(GameObject unit)
        {
            // Add physics
            Rigidbody rb = unit.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.drag = 5f;
            rb.angularDrag = 5f;
            rb.freezeRotation = true;
            
            // Add collider
            CapsuleCollider collider = unit.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            
            // Add NavMeshAgent
            NavMeshAgent agent = unit.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.acceleration = 8f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.1f;
            agent.autoBraking = true;
            
            // Add LocomotionAgent wrapper
            LocomotionAgent locomotion = unit.AddComponent<LocomotionAgent>();
            
            // Add health system
            Damageable damageable = unit.AddComponent<Damageable>();
            
            // Add weapon system
            WeaponController weaponController = unit.AddComponent<WeaponController>();
            HitscanWeapon hitscanWeapon = unit.AddComponent<HitscanWeapon>();
            
            // Add team identification
            Team team = unit.AddComponent<Team>();
            team.SetTeam(_aiTeamId);
            
            // Add selection system
            SelectableFlag selectable = unit.AddComponent<SelectableFlag>();
            
            // Add targeting agent
            TargetingAgent targeting = unit.AddComponent<TargetingAgent>();
            
            // Add main unit controller
            BasicUnit basicUnit = unit.AddComponent<BasicUnit>();
        }
        
        private GameObject CreateBasicUnitPrefab()
        {
            // Create a prefab for AI basic units
            GameObject prefab = new GameObject("AI_BasicUnitPrefab");
            prefab.SetActive(false);
            
            // Add visual
            CreateUnitVisual(prefab, Vector3.one, new Color(1f, 0.2f, 0.2f, 1f));
            
            // Add unit components
            prefab.layer = _unitsLayer;
            AddBasicUnitComponents(prefab);
            
            return prefab;
        }
        
        private void SetupStandardBot()
        {
            // Create StandardBot controller
            GameObject botObject = new GameObject("AI_StandardBot");
            botObject.transform.parent = transform;
            
            _standardBot = botObject.AddComponent<StandardBot>();
            
            // Configure StandardBot using reflection (since fields are private)
            var teamIdField = typeof(StandardBot).GetField("_teamId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            teamIdField?.SetValue(_standardBot, _aiTeamId);
            
            var economyField = typeof(StandardBot).GetField("_economy", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            economyField?.SetValue(_standardBot, _aiEconomy);
            
            var factoryQueueField = typeof(StandardBot).GetField("_factoryQueue", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            factoryQueueField?.SetValue(_standardBot, _aiBuildQueue);
            
            var basicUnitPrefabField = typeof(StandardBot).GetField("_basicUnitPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            basicUnitPrefabField?.SetValue(_standardBot, _basicUnitPrefab);
            
            var armyValueThresholdField = typeof(StandardBot).GetField("_armyValueThreshold", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            armyValueThresholdField?.SetValue(_standardBot, _armyValueThreshold);
            
            var waveWindowSecondsField = typeof(StandardBot).GetField("_waveWindowSeconds", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            waveWindowSecondsField?.SetValue(_standardBot, _waveWindowSeconds);
            
            var retreatHpFractionField = typeof(StandardBot).GetField("_retreatHpFraction", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            retreatHpFractionField?.SetValue(_standardBot, _retreatHpFraction);
            
            // Find player HQ as enemy target
            var playerEconomy = FindObjectOfType<PlayerEconomy>();
            if (playerEconomy != null)
            {
                var enemyHQField = typeof(StandardBot).GetField("_enemyHQ", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                enemyHQField?.SetValue(_standardBot, playerEconomy.transform);
            }
            
            // Find command system
            var commandSystem = FindObjectOfType<CommandSystem>();
            if (commandSystem != null)
            {
                var commandSystemField = typeof(StandardBot).GetField("_commandSystem", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                commandSystemField?.SetValue(_standardBot, commandSystem);
            }
        }
        
        /// <summary>
        /// Clear all existing AI buildings and units
        /// </summary>
        public void ClearExistingAI()
        {
            foreach (var building in _aiBuildings)
            {
                if (building != null)
                {
                    DestroyImmediate(building);
                }
            }
            _aiBuildings.Clear();
            
            foreach (var unit in _aiUnits)
            {
                if (unit != null)
                {
                    DestroyImmediate(unit);
                }
            }
            _aiUnits.Clear();
            
            if (_standardBot != null)
            {
                DestroyImmediate(_standardBot.gameObject);
                _standardBot = null;
            }
            
            if (_aiEconomy != null)
            {
                DestroyImmediate(_aiEconomy.gameObject);
                _aiEconomy = null;
            }
        }
        
        /// <summary>
        /// Get the AI economy reference
        /// </summary>
        public PlayerEconomy GetAIEconomy()
        {
            return _aiEconomy;
        }
        
        /// <summary>
        /// Get the StandardBot reference
        /// </summary>
        public StandardBot GetStandardBot()
        {
            return _standardBot;
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            // Draw AI base area
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_aiBaseCenter, new Vector3(_baseAreaSize.x, 1f, _baseAreaSize.y));
            
            // Draw AI building positions
            Gizmos.color = Color.magenta;
            for (int i = 0; i < _factoriesToSpawn; i++)
            {
                Vector3 factoryPos = GetAIBuildingPosition(i, "factory");
                Gizmos.DrawWireCube(factoryPos, new Vector3(4f, 3f, 4f));
            }
            
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _refineriesToSpawn; i++)
            {
                Vector3 refineryPos = GetAIBuildingPosition(i, "refinery");
                Gizmos.DrawWireCube(refineryPos, new Vector3(4f, 3f, 4f));
            }
        }
    }
}