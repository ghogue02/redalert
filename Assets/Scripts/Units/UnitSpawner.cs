using UnityEngine;
using UnityEngine.AI;
using RedAlert.Combat;
using System.Collections.Generic;

namespace RedAlert.Units
{
    /// <summary>
    /// Spawns units for testing and initial scene setup.
    /// Creates basic units with all required components for RTS gameplay.
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        [Header("Unit Configuration")]
        [SerializeField] private int _playerUnitsToSpawn = 5;
        [SerializeField] private int _enemyUnitsToSpawn = 3;
        [SerializeField] private Vector2 _spawnAreaSize = new Vector2(20f, 20f);
        [SerializeField] private Vector3 _playerSpawnCenter = new Vector3(-10f, 0f, -10f);
        [SerializeField] private Vector3 _enemySpawnCenter = new Vector3(10f, 0f, 10f);
        
        [Header("Unit Stats")]
        [SerializeField] private float _unitHealth = 100f;
        [SerializeField] private float _unitSpeed = 3.5f;
        [SerializeField] private float _unitAttackRange = 8f;
        
        [Header("Layers")]
        [SerializeField] private int _unitsLayer = 8;
        [SerializeField] private int _playerTeam = 1;
        [SerializeField] private int _enemyTeam = 2;
        
        [Header("Debug")]
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField] private bool _showGizmos = true;
        
        private List<GameObject> _spawnedUnits = new List<GameObject>();
        
        private void Start()
        {
            if (_spawnOnStart)
            {
                SpawnUnits();
            }
        }
        
        /// <summary>
        /// Spawn all units for both teams
        /// </summary>
        public void SpawnUnits()
        {
            ClearExistingUnits();
            
            // Spawn player units
            for (int i = 0; i < _playerUnitsToSpawn; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPosition(_playerSpawnCenter, _spawnAreaSize);
                GameObject unit = CreateBasicUnit(spawnPos, _playerTeam, $"Player_Unit_{i + 1}");
                _spawnedUnits.Add(unit);
            }
            
            // Spawn enemy units
            for (int i = 0; i < _enemyUnitsToSpawn; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPosition(_enemySpawnCenter, _spawnAreaSize);
                GameObject unit = CreateBasicUnit(spawnPos, _enemyTeam, $"Enemy_Unit_{i + 1}");
                _spawnedUnits.Add(unit);
            }
            
            Debug.Log($"UnitSpawner: Spawned {_playerUnitsToSpawn} player units and {_enemyUnitsToSpawn} enemy units");
        }
        
        private Vector3 GetRandomSpawnPosition(Vector3 center, Vector2 area)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-area.x / 2f, area.x / 2f),
                0f,
                Random.Range(-area.y / 2f, area.y / 2f)
            );
            
            Vector3 spawnPos = center + randomOffset;
            
            // Sample NavMesh to ensure valid spawn position
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return center; // Fallback to center if no valid position found
        }
        
        private GameObject CreateBasicUnit(Vector3 position, int teamId, string unitName)
        {
            // Create unit GameObject
            GameObject unit = new GameObject(unitName);
            unit.transform.position = position;
            unit.transform.parent = transform;
            unit.layer = _unitsLayer;
            
            // Add visual representation
            CreateUnitVisual(unit, teamId);
            
            // Add required components
            AddBasicUnitComponents(unit, teamId);
            
            return unit;
        }
        
        private void CreateUnitVisual(GameObject unit, int teamId)
        {
            // Create main body
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.parent = unit.transform;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(1f, 1f, 1f);
            visual.name = "UnitVisual";
            
            // Set team color
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                
                // Different colors for different teams
                switch (teamId)
                {
                    case 1: // Player - Blue
                        mat.color = new Color(0.2f, 0.4f, 1f, 1f);
                        break;
                    case 2: // Enemy - Red
                        mat.color = new Color(1f, 0.2f, 0.2f, 1f);
                        break;
                    default: // Neutral - Gray
                        mat.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                        break;
                }
                
                renderer.material = mat;
            }
            
            // Add weapon indicator (small cube on top)
            GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.transform.parent = visual.transform;
            weapon.transform.localPosition = new Vector3(0f, 0.7f, 0.3f);
            weapon.transform.localScale = new Vector3(0.2f, 0.2f, 0.6f);
            weapon.name = "WeaponIndicator";
            
            // Remove unnecessary colliders from visual components
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null) DestroyImmediate(visualCollider);
            
            Collider weaponCollider = weapon.GetComponent<Collider>();
            if (weaponCollider != null) DestroyImmediate(weaponCollider);
        }
        
        private void AddBasicUnitComponents(GameObject unit, int teamId)
        {
            // Add physics
            Rigidbody rb = unit.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.drag = 5f;
            rb.angularDrag = 5f;
            rb.freezeRotation = true;
            
            // Add collider for selection and physics
            CapsuleCollider collider = unit.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            
            // Add NavMeshAgent for movement
            NavMeshAgent agent = unit.AddComponent<NavMeshAgent>();
            agent.speed = _unitSpeed;
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
            
            // Add team identification
            Team team = unit.AddComponent<Team>();
            team.SetTeam(teamId);
            
            // Add selection system
            SelectableFlag selectable = unit.AddComponent<SelectableFlag>();
            
            // Add main unit controller
            BasicUnit basicUnit = unit.AddComponent<BasicUnit>();
            
            // Add weapon system
            WeaponController weaponController = unit.AddComponent<WeaponController>();
            HitscanWeapon hitscanWeapon = unit.AddComponent<HitscanWeapon>();
            
            // Configure targeting agent if present
            TargetingAgent targeting = unit.AddComponent<TargetingAgent>();
        }
        
        /// <summary>
        /// Clear all existing spawned units
        /// </summary>
        public void ClearExistingUnits()
        {
            foreach (var unit in _spawnedUnits)
            {
                if (unit != null)
                {
                    DestroyImmediate(unit);
                }
            }
            _spawnedUnits.Clear();
        }
        
        /// <summary>
        /// Get all spawned units
        /// </summary>
        public List<GameObject> GetSpawnedUnits()
        {
            return new List<GameObject>(_spawnedUnits);
        }
        
        /// <summary>
        /// Spawn a single unit at a specific position
        /// </summary>
        public GameObject SpawnUnit(Vector3 position, int teamId, string unitName = "Unit")
        {
            GameObject unit = CreateBasicUnit(position, teamId, unitName);
            _spawnedUnits.Add(unit);
            return unit;
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            // Draw player spawn area
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(_playerSpawnCenter, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.y));
            
            // Draw enemy spawn area
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_enemySpawnCenter, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.y));
        }
    }
}