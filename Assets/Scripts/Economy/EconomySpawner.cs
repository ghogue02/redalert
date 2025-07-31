using UnityEngine;
using UnityEngine.AI;
using RedAlert.Units;
using System.Collections.Generic;

namespace RedAlert.Economy
{
    /// <summary>
    /// Spawns economy buildings (refineries) and harvester units to create a functional resource gathering system.
    /// Integrates with PlayerEconomy for resource flow and NavMesh for movement.
    /// </summary>
    public class EconomySpawner : MonoBehaviour
    {
        [Header("Economy Configuration")]
        [SerializeField] private PlayerEconomy _playerEconomy;
        [SerializeField] private int _harvestersToSpawn = 2;
        [SerializeField] private int _refineriesToSpawn = 1;
        
        [Header("Spawning Positions")]
        [SerializeField] private Vector3 _playerBaseCenter = new Vector3(-15f, 0f, -15f);
        [SerializeField] private Vector2 _baseAreaSize = new Vector2(10f, 10f);
        [SerializeField] private float _refinerySpacing = 8f;
        
        [Header("Unit Configuration")]
        [SerializeField] private float _harvesterSpeed = 4f;
        [SerializeField] private int _harvesterCapacity = 200;
        [SerializeField] private int _harvesterMineRate = 40;
        [SerializeField] private int _harvesterUnloadRate = 200;
        
        [Header("Layers")]
        [SerializeField] private int _buildingsLayer = 9;
        [SerializeField] private int _unitsLayer = 8;
        [SerializeField] private int _playerTeam = 1;
        
        [Header("Debug")]
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField] private bool _showGizmos = true;
        
        private List<GameObject> _spawnedBuildings = new List<GameObject>();
        private List<GameObject> _spawnedHarvesters = new List<GameObject>();
        
        private void Start()
        {
            if (_spawnOnStart)
            {
                SpawnEconomySystem();
            }
        }
        
        /// <summary>
        /// Spawn the complete economy system: refineries and harvesters
        /// </summary>
        public void SpawnEconomySystem()
        {
            ClearExistingBuildings();
            
            // Ensure we have a PlayerEconomy
            if (_playerEconomy == null)
            {
                _playerEconomy = FindObjectOfType<PlayerEconomy>();
                if (_playerEconomy == null)
                {
                    _playerEconomy = CreatePlayerEconomy();
                }
            }
            
            // Spawn refineries first
            List<Refinery> refineries = new List<Refinery>();
            for (int i = 0; i < _refineriesToSpawn; i++)
            {
                Vector3 refineryPos = GetRefinerySpawnPosition(i);
                GameObject refinery = CreateRefinery(refineryPos, $"PlayerRefinery_{i + 1}");
                refineries.Add(refinery.GetComponent<Refinery>());
                _spawnedBuildings.Add(refinery);
            }
            
            // Then spawn harvesters
            for (int i = 0; i < _harvestersToSpawn; i++)
            {
                Vector3 harvesterPos = GetHarvesterSpawnPosition();
                GameObject harvester = CreateHarvester(harvesterPos, $"PlayerHarvester_{i + 1}");
                _spawnedHarvesters.Add(harvester);
            }
            
            Debug.Log($"EconomySpawner: Created economy system with {_refineriesToSpawn} refineries and {_harvestersToSpawn} harvesters");
        }
        
        private PlayerEconomy CreatePlayerEconomy()
        {
            GameObject economyObject = new GameObject("PlayerEconomy");
            economyObject.transform.parent = transform;
            return economyObject.AddComponent<PlayerEconomy>();
        }
        
        private Vector3 GetRefinerySpawnPosition(int index)
        {
            // Space refineries out horizontally
            Vector3 basePos = _playerBaseCenter;
            basePos.x += (index - (_refineriesToSpawn - 1) * 0.5f) * _refinerySpacing;
            
            // Sample NavMesh to ensure valid position
            if (NavMesh.SamplePosition(basePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return basePos;
        }
        
        private Vector3 GetHarvesterSpawnPosition()
        {
            // Random position near the base
            Vector3 randomOffset = new Vector3(
                Random.Range(-_baseAreaSize.x / 2f, _baseAreaSize.x / 2f),
                0f,
                Random.Range(-_baseAreaSize.y / 2f, _baseAreaSize.y / 2f)
            );
            
            Vector3 spawnPos = _playerBaseCenter + randomOffset;
            
            // Sample NavMesh to ensure valid position
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return _playerBaseCenter;
        }
        
        private GameObject CreateRefinery(Vector3 position, string buildingName)
        {
            // Create refinery GameObject
            GameObject refinery = new GameObject(buildingName);
            refinery.transform.position = position;
            refinery.transform.parent = transform;
            refinery.layer = _buildingsLayer;
            
            // Add visual representation
            CreateRefineryVisual(refinery);
            
            // Add physics
            BoxCollider trigger = refinery.AddComponent<BoxCollider>();
            trigger.size = new Vector3(6f, 4f, 6f);
            trigger.isTrigger = true;
            
            // Add dock points
            Transform[] dockPoints = CreateDockPoints(refinery);
            
            // Add Refinery component
            Refinery refineryComponent = refinery.AddComponent<Refinery>();
            
            // Use reflection to set the dock points (since the field is private)
            var dockPointsField = typeof(Refinery).GetField("dockPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            dockPointsField?.SetValue(refineryComponent, dockPoints);
            
            // Set the economy reference
            var economyField = typeof(Refinery).GetField("_ownerEconomy", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            economyField?.SetValue(refineryComponent, _playerEconomy);
            
            return refinery;
        }
        
        private void CreateRefineryVisual(GameObject refinery)
        {
            // Main building body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.parent = refinery.transform;
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(4f, 3f, 4f);
            body.name = "RefineryBody";
            
            // Remove collider (we have our own trigger)
            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null) DestroyImmediate(bodyCollider);
            
            // Set material
            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.6f, 0.3f, 1f); // Green for player refinery
                mat.SetFloat("_Metallic", 0.2f);
                mat.SetFloat("_Smoothness", 0.1f);
                renderer.material = mat;
            }
            
            // Add some detail elements
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chimney.transform.parent = body.transform;
            chimney.transform.localPosition = new Vector3(0.3f, 0.8f, 0.3f);
            chimney.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
            chimney.name = "Chimney";
            
            Collider chimneyCollider = chimney.GetComponent<Collider>();
            if (chimneyCollider != null) DestroyImmediate(chimneyCollider);
        }
        
        private Transform[] CreateDockPoints(GameObject refinery)
        {
            List<Transform> dockPoints = new List<Transform>();
            
            // Create 4 dock points around the refinery
            Vector3[] offsets = {
                new Vector3(3.5f, 0f, 0f),    // Right
                new Vector3(-3.5f, 0f, 0f),   // Left
                new Vector3(0f, 0f, 3.5f),    // Forward
                new Vector3(0f, 0f, -3.5f)    // Back
            };
            
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject dockPoint = new GameObject($"DockPoint_{i}");
                dockPoint.transform.parent = refinery.transform;
                dockPoint.transform.localPosition = offsets[i];
                dockPoints.Add(dockPoint.transform);
            }
            
            return dockPoints.ToArray();
        }
        
        private GameObject CreateHarvester(Vector3 position, string unitName)
        {
            // Create harvester GameObject
            GameObject harvester = new GameObject(unitName);
            harvester.transform.position = position;
            harvester.transform.parent = transform;
            harvester.layer = _unitsLayer;
            
            // Add visual representation
            CreateHarvesterVisual(harvester);
            
            // Add required components
            AddHarvesterComponents(harvester);
            
            return harvester;
        }
        
        private void CreateHarvesterVisual(GameObject harvester)
        {
            // Main harvester body (more box-like for resource gathering)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.parent = harvester.transform;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(1.5f, 1f, 2f);
            visual.name = "HarvesterVisual";
            
            // Set harvester color (yellow/orange for industrial look)
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 0.8f, 0.2f, 1f); // Yellow-orange
                mat.SetFloat("_Metallic", 0.3f);
                mat.SetFloat("_Smoothness", 0.2f);
                renderer.material = mat;
            }
            
            // Add mining equipment indicator
            GameObject miningArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            miningArm.transform.parent = visual.transform;
            miningArm.transform.localPosition = new Vector3(0f, 0.3f, 1.2f);
            miningArm.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
            miningArm.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            miningArm.name = "MiningArm";
            
            // Remove unnecessary colliders from visual components
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null) DestroyImmediate(visualCollider);
            
            Collider armCollider = miningArm.GetComponent<Collider>();
            if (armCollider != null) DestroyImmediate(armCollider);
        }
        
        private void AddHarvesterComponents(GameObject harvester)
        {
            // Add physics
            Rigidbody rb = harvester.AddComponent<Rigidbody>();
            rb.mass = 2f; // Heavier than regular units
            rb.drag = 5f;
            rb.angularDrag = 5f;
            rb.freezeRotation = true;
            
            // Add collider
            BoxCollider collider = harvester.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.5f, 1f, 2f);
            
            // Add NavMeshAgent
            NavMeshAgent agent = harvester.AddComponent<NavMeshAgent>();
            agent.speed = _harvesterSpeed;
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
            team.SetTeam(_playerTeam);
            
            // Add selection system
            SelectableFlag selectable = harvester.AddComponent<SelectableFlag>();
            
            // Add harvester-specific component
            HarvesterAgent harvesterAgent = harvester.AddComponent<HarvesterAgent>();
            
            // Configure harvester settings via reflection (since fields are private)
            var capacityField = typeof(HarvesterAgent).GetField("carryCapacity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            capacityField?.SetValue(harvesterAgent, _harvesterCapacity);
            
            var mineRateField = typeof(HarvesterAgent).GetField("mineRatePerSec", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            mineRateField?.SetValue(harvesterAgent, _harvesterMineRate);
            
            var unloadRateField = typeof(HarvesterAgent).GetField("unloadRatePerSec", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            unloadRateField?.SetValue(harvesterAgent, _harvesterUnloadRate);
            
            // Set layer masks for finding nodes and refineries
            var nodeMaskField = typeof(HarvesterAgent).GetField("nodeMask", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nodeMaskField?.SetValue(harvesterAgent, 1 << _buildingsLayer); // Nodes are on buildings layer
            
            var refineryMaskField = typeof(HarvesterAgent).GetField("refineryMask", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            refineryMaskField?.SetValue(harvesterAgent, 1 << _buildingsLayer); // Refineries are on buildings layer
        }
        
        /// <summary>
        /// Clear all existing spawned buildings and harvesters
        /// </summary>
        public void ClearExistingBuildings()
        {
            foreach (var building in _spawnedBuildings)
            {
                if (building != null)
                {
                    DestroyImmediate(building);
                }
            }
            _spawnedBuildings.Clear();
            
            foreach (var harvester in _spawnedHarvesters)
            {
                if (harvester != null)
                {
                    DestroyImmediate(harvester);
                }
            }
            _spawnedHarvesters.Clear();
        }
        
        /// <summary>
        /// Get the player economy reference
        /// </summary>
        public PlayerEconomy GetPlayerEconomy()
        {
            return _playerEconomy;
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            // Draw base area
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_playerBaseCenter, new Vector3(_baseAreaSize.x, 1f, _baseAreaSize.y));
            
            // Draw refinery positions
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _refineriesToSpawn; i++)
            {
                Vector3 refineryPos = GetRefinerySpawnPosition(i);
                Gizmos.DrawWireCube(refineryPos, new Vector3(4f, 3f, 4f));
            }
        }
    }
}