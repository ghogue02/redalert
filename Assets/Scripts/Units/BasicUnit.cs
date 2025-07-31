using UnityEngine;
using RedAlert.Core;

namespace RedAlert.Units
{
    /// <summary>
    /// Basic RTS unit that combines all core functionality:
    /// - Selectable behavior
    /// - Movement via LocomotionAgent
    /// - Combat capabilities via Damageable and WeaponController
    /// - Team identification
    /// - Attack-move tactical mode
    /// </summary>
    [RequireComponent(typeof(LocomotionAgent))]
    [RequireComponent(typeof(Damageable))]
    [RequireComponent(typeof(Team))]
    [RequireComponent(typeof(SelectableFlag))]
    public class BasicUnit : MonoBehaviour, ITacticalMode, IMinimapIconProvider
    {
        [Header("Unit Configuration")]
        [SerializeField] private string _unitName = "Basic Unit";
        [SerializeField] private UnitType _unitType = UnitType.Infantry;
        
        [Header("Combat")]
        [SerializeField] private float _attackRange = 8f;
        [SerializeField] private float _attackCooldown = 1.5f;
        [SerializeField] private LayerMask _enemyLayers = -1;
        
        [Header("Tactical Mode")]
        [SerializeField] private bool _attackMoveEnabled = false;
        [SerializeField] private float _engagementRange = 10f;
        
        [Header("Minimap")]
        [SerializeField] private Color _minimapColor = Color.blue;
        [SerializeField] private float _minimapSize = 3f;
        
        // Cached components
        private LocomotionAgent _locomotion;
        private Damageable _damageable;
        private WeaponController _weaponController;
        private Team _team;
        private SelectableFlag _selectable;
        
        // State
        private Transform _currentTarget;
        private Vector3 _originalDestination;
        private float _lastAttackTime;
        private bool _isEngaging;
        
        public enum UnitType
        {
            Infantry,
            Vehicle,
            Aircraft,
            Building
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheComponents();
        }
        
        private void Start()
        {
            InitializeUnit();
        }
        
        private void Update()
        {
            if (_attackMoveEnabled)
            {
                UpdateAttackMove();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void CacheComponents()
        {
            _locomotion = GetComponent<LocomotionAgent>();
            _damageable = GetComponent<Damageable>();
            _weaponController = GetComponent<WeaponController>();
            _team = GetComponent<Team>();
            _selectable = GetComponent<SelectableFlag>();
        }
        
        private void InitializeUnit()
        {
            // Subscribe to damage events
            if (_damageable != null)
            {
                _damageable.OnDeath += HandleDeath;
                _damageable.OnDamaged += HandleDamaged;
            }
            
            // Set default team if not set
            if (_team != null && _team.TeamId == 0)
            {
                _team.SetTeam(1); // Default to player team
            }
        }
        
        #endregion
        
        #region Combat System
        
        private void UpdateAttackMove()
        {
            if (!_locomotion.IsMoving && !_isEngaging)
            {
                // Look for enemies in engagement range
                Transform nearestEnemy = FindNearestEnemy();
                if (nearestEnemy != null)
                {
                    EngageTarget(nearestEnemy);
                }
            }
            else if (_isEngaging && _currentTarget != null)
            {
                // Check if target is still valid and in range
                float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
                
                if (distanceToTarget <= _attackRange)
                {
                    // Attack if cooldown is ready
                    if (Time.time >= _lastAttackTime + _attackCooldown)
                    {
                        AttackTarget(_currentTarget);
                        _lastAttackTime = Time.time;
                    }
                }
                else if (distanceToTarget > _engagementRange)
                {
                    // Target moved too far, disengage
                    DisengageTarget();
                }
                else
                {
                    // Move closer to target
                    _locomotion.SetDestination(_currentTarget.position);
                }
            }
        }
        
        private Transform FindNearestEnemy()
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, _engagementRange, _enemyLayers);
            Transform nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var collider in nearbyColliders)
            {
                Team enemyTeam = collider.GetComponent<Team>();
                if (enemyTeam != null && enemyTeam.TeamId != _team.TeamId)
                {
                    Damageable enemyDamageable = collider.GetComponent<Damageable>();
                    if (enemyDamageable != null && !enemyDamageable.IsDead)
                    {
                        float distance = Vector3.Distance(transform.position, collider.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearest = collider.transform;
                        }
                    }
                }
            }
            
            return nearest;
        }
        
        private void EngageTarget(Transform target)
        {
            _currentTarget = target;
            _isEngaging = true;
            _originalDestination = transform.position;
            
            // Move to attack range
            _locomotion.SetDestination(target.position);
        }
        
        private void DisengageTarget()
        {
            _currentTarget = null;
            _isEngaging = false;
            
            // Resume original movement if we were moving
            if (_originalDestination != transform.position)
            {
                _locomotion.SetDestination(_originalDestination);
            }
        }
        
        private void AttackTarget(Transform target)
        {
            if (_weaponController != null)
            {
                _weaponController.TryFire(target.position);
            }
            else
            {
                // Direct damage fallback
                Damageable targetDamageable = target.GetComponent<Damageable>();
                if (targetDamageable != null)
                {
                    targetDamageable.TakeDamage(25f, transform.position);
                }
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleDeath()
        {
            // Play death effects, disable components, etc.
            DisengageTarget();
            
            // Notify systems
            EventBus.PublishUnitDied(gameObject);
            
            // Schedule destruction
            Destroy(gameObject, 2f);
        }
        
        private void HandleDamaged(float damage, Vector3 damageSource)
        {
            // Could play hit effects, damage indicators, etc.
            // For now, just log
            Debug.Log($"{_unitName} took {damage} damage from {damageSource}");
        }
        
        #endregion
        
        #region ITacticalMode Implementation
        
        public void SetAttackMove(bool enabled)
        {
            _attackMoveEnabled = enabled;
            
            if (!enabled)
            {
                DisengageTarget();
            }
        }
        
        #endregion
        
        #region IMinimapIconProvider Implementation
        
        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }
        
        public Color GetMinimapColor()
        {
            return _minimapColor;
        }
        
        public float GetMinimapSize()
        {
            return _minimapSize;
        }
        
        public bool ShouldShowOnMinimap()
        {
            return !_damageable.IsDead;
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get unit information for UI display
        /// </summary>
        public string GetUnitName() => _unitName;
        public UnitType GetUnitType() => _unitType;
        public float GetHealth() => _damageable?.Health ?? 0f;
        public float GetMaxHealth() => _damageable?.MaxHealth ?? 100f;
        public bool IsSelected() => _selectable?.IsSelected ?? false;
        public Team GetTeam() => _team;
        
        /// <summary>
        /// Force the unit to attack a specific target
        /// </summary>
        public void ForceAttack(Transform target)
        {
            if (target != null)
            {
                EngageTarget(target);
            }
        }
        
        /// <summary>
        /// Stop all current actions
        /// </summary>
        public void Stop()
        {
            DisengageTarget();
            _locomotion.SetDestination(transform.position);
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
            
            // Draw engagement range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _engagementRange);
            
            // Draw current target
            if (_currentTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
        
        #endregion
    }
}