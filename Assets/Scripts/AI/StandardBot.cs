using System;
using System.Collections.Generic;
using UnityEngine;
using RedAlert.Core;
using RedAlert.Build;
using RedAlert.Economy;
using RedAlert.Units;

namespace RedAlert.AI
{
    /// <summary>
    /// Week 3: Standard AI bot. Deterministic build pads, 4 Hz tick, no cheating.
    /// States: Bootstrap → Economy → TechProduction → Attack → Regroup
    /// Includes simple scaling hooks for balance pass.
    /// </summary>
    public class StandardBot : MonoBehaviour, UpdateDriver.ISlowTick
    {
        public enum State
        {
            Bootstrap,
            Economy,
            TechProduction,
            Attack,
            Regroup
        }

        [Serializable]
        public struct BuildPad
        {
            public Vector3 position;
            public Vector2 footprint; // meters (x,z)
            public GameObject prefab;
        }

        [Serializable]
        public struct TargetComposition
        {
            [Range(0, 1f)] public float infantry; // reserved for future
            [Range(0, 1f)] public float vehicles;
            [Range(0, 1f)] public float air; // reserved for future
        }

        [Header("Ownership/Systems")]
        [SerializeField] private int _teamId = 1; // AI team
        [SerializeField] private PlayerEconomy _economy;
        [SerializeField] private PlacementValidator _placement;
        [SerializeField] private Transform _enemyHQ; // target to attack-move
        [SerializeField] private CommandSystem _commandSystem;

        [Header("Build")]
        [Tooltip("Pads used for deterministic, non-overlapping placement.")]
        [SerializeField] private BuildPad[] _pads;
        [Tooltip("Factory build queue for unit production.")]
        [SerializeField] private BuildQueue _factoryQueue;
        [Tooltip("Basic unit prefab for waves (vehicle).")]
        [SerializeField] private GameObject _basicUnitPrefab;
        [SerializeField] private int _basicUnitCost = PlacementRules.CostBasicVehicle;
        [SerializeField] private float _basicUnitTime = PlacementRules.TimeBasicVehicle;

        [Header("Wave Logic")]
        [Tooltip("Army value threshold to launch attack.")]
        [SerializeField] private int _armyValueThreshold = 4;
        [Tooltip("Fallback timer window for periodic waves (seconds).")]
        [SerializeField] private Vector2 _waveWindowSeconds = new Vector2(150f, 180f); // ~2.5–3.0 min window
        [Tooltip("Retreat when squad HP drops under this fraction of initial rollout HP.")]
        [SerializeField] private float _retreatHpFraction = 0.35f;

        [Header("Composition")]
        [SerializeField] private TargetComposition _targetComp = new TargetComposition { vehicles = 1f };

        [Header("Scaling (simple)")]
        [SerializeField, Tooltip("Multiplies wave threshold units")] private float _waveThresholdScale = 1f;
        [SerializeField, Tooltip("Multiplies wave window seconds (both min/max)")] private float _waveWindowScale = 1f;

        // Internal state
        private State _state = State.Bootstrap;
        private float _nextWaveTime;
        private int _nextPadIndex;
        private readonly List<Damageable> _squad = new List<Damageable>(64);
        private float _rolloutHp; // HP at wave start

        private void ApplyScaling()
        {
            _armyValueThreshold = Mathf.Max(1, Mathf.RoundToInt(_armyValueThreshold * _waveThresholdScale));
            _waveWindowSeconds = new Vector2(_waveWindowSeconds.x * _waveWindowScale, _waveWindowSeconds.y * _waveWindowScale);
        }
        private readonly Collider[] _overlapTemp = new Collider[64];
        private float _lastCommandAt; // stagger orders
        private const float CommandCadence = 0.25f; // seconds between bulk orders

        private static readonly List<ISelectable> _selBuffer = new List<ISelectable>(64);

        private void OnEnable()
        {
            if (_economy == null) _economy = FindObjectOfType<PlayerEconomy>();
            if (_placement == null) _placement = FindObjectOfType<PlacementValidator>();
            if (_commandSystem == null) _commandSystem = FindObjectOfType<CommandSystem>();
            if (_factoryQueue == null) _factoryQueue = FindObjectOfType<BuildQueue>();
            UpdateDriver.Register(this);
            ScheduleNextWave();
        }

        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
        }

        public void SlowTick()
        {
            switch (_state)
            {
                case State.Bootstrap:
                    TickBootstrap();
                    break;
                case State.Economy:
                    TickEconomy();
                    break;
                case State.TechProduction:
                    TickTechProduction();
                    break;
                case State.Attack:
                    TickAttack();
                    break;
                case State.Regroup:
                    TickRegroup();
                    break;
            }
        }

        private void TickBootstrap()
        {
            // Ensure scene references
            if (_enemyHQ == null)
            {
                // Try to find an opposing HQ by tag or fallback to origin; keep deterministic
                var hq = GameObject.FindWithTag("EnemyHQ");
                _enemyHQ = hq != null ? hq.transform : null;
            }

            // Move to Economy to ensure 1-2 harvesters: assume harvesters are pre-placed or produced by a refinery/factory flow.
            _state = State.Economy;
        }

        private void TickEconomy()
        {
            // Keep 1–2 harvesters target (simplified): if there is a refinery queue in pads, we skip and proceed.
            // For this vertical slice we assume harvesters exist or are produced elsewhere.
            // Transition deterministically after a short bootstrap period to Tech/Production.
            _state = State.TechProduction;
        }

        private void TickTechProduction()
        {
            // Place buildings using pads if unfilled. We validate and place if possible.
            TryPlaceNextPad();

            // Queue unit production at factory
            TryQueueBasicUnit();

            // Check whether we have enough army or time to attack
            if (ArmyValue() >= _armyValueThreshold || Time.time >= _nextWaveTime)
            {
                AssembleSquad();
                IssueAttackOrders();
                _state = State.Attack;
            }
        }

        private void TickAttack()
        {
            // Monitor squad HP to decide retreat/regroup
            float hpNow = SquadHp();
            if (_rolloutHp > 1f && hpNow / _rolloutHp <= _retreatHpFraction)
            {
                IssueRetreat();
                _state = State.Regroup;
                return;
            }

            // Stagger re-issue attack-move rarely to avoid spamming pathing (only if far off)
            // For simplicity, do not re-issue unless we regroup or a new wave starts.
            // If all units died, regroup
            if (_squad.Count == 0 || !AnyAlive(_squad))
            {
                _state = State.Regroup;
            }
        }

        private void TickRegroup()
        {
            // Simple regroup: wait until timer for next wave and ensure some units available
            if (ArmyValue() >= Mathf.Max(2, _armyValueThreshold / 2) || Time.time >= _nextWaveTime)
            {
                AssembleSquad();
                IssueAttackOrders();
                _state = State.Attack;
            }
        }

        private void TryPlaceNextPad()
        {
            if (_pads == null || _pads.Length == 0) return;
            if (_nextPadIndex >= _pads.Length) return;

            var pad = _pads[_nextPadIndex];
            if (pad.prefab == null || _placement == null) { _nextPadIndex++; return; }

            // Validate
            if (_placement.Validate(pad.position, pad.footprint, out var corrected, out _))
            {
                // Instantiate building deterministically
                var go = Instantiate(pad.prefab, corrected, Quaternion.identity);
                // Assign team if present
                if (go.TryGetComponent<Team>(out var team)) team.DebugSetTeam(_teamId);
                _nextPadIndex++;
            }
            // else: skip and retry next tick (do nothing)
        }

        private void TryQueueBasicUnit()
        {
            if (_factoryQueue == null || _basicUnitPrefab == null) return;

            var item = new BuildQueue.BuildItem
            {
                Id = "BasicVehicle",
                Prefab = _basicUnitPrefab,
                Cost = _basicUnitCost,
                BuildTimeSeconds = _basicUnitTime
            };

            // If insufficient, BuildQueue will publish event; we just attempt sparingly
            // Throttle enqueue attempts so we don't spam reserves: try only when queue empty or small
            var q = _factoryQueue.Queue;
            if (q.Count < 3)
            {
                _factoryQueue.Enqueue(item);
            }
        }

        private int ArmyValue()
        {
            // Count owned units near factory area or globally for simplicity
            int count = 0;
            // Optionally, scan by Team across scene using physics or tag; here we do a cheap global search by tag "Unit"
            // To keep non-alloc, avoid GameObject.FindGameObjectsWithTag; rely on spawn buffer later if needed.
            // For now, approximate by current assembled squad size plus some leeway.
            count = _squad.Count;
            // If squad not built yet, sample around factory exit position
            if (count < 1 && _factoryQueue != null)
            {
                var pos = _factoryQueue.transform.position;
                int c = Physics.OverlapSphereNonAlloc(pos, 20f, _overlapTemp, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < c; i++)
                {
                    var go = _overlapTemp[i].attachedRigidbody ? _overlapTemp[i].attachedRigidbody.gameObject : _overlapTemp[i].gameObject;
                    if (go.TryGetComponent<Team>(out var t) && t.TeamId == _teamId && go.TryGetComponent<Damageable>(out _))
                        count++;
                }
            }
            return count;
        }

        private void AssembleSquad()
        {
            _squad.Clear();
            // Gather all friendly units in radius of factory as a staging point
            if (_factoryQueue == null) return;
            var pos = _factoryQueue.transform.position;
            int c = Physics.OverlapSphereNonAlloc(pos, 40f, _overlapTemp, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < c; i++)
            {
                var go = _overlapTemp[i].attachedRigidbody ? _overlapTemp[i].attachedRigidbody.gameObject : _overlapTemp[i].gameObject;
                if (go == null) continue;
                if (go.TryGetComponent<Team>(out var t) && t.TeamId == _teamId && go.TryGetComponent<Damageable>(out var hp))
                {
                    _squad.Add(hp);
                }
            }
            _rolloutHp = SquadHp();
        }

        private float SquadHp()
        {
            float hp = 0f;
            for (int i = 0; i < _squad.Count; i++)
            {
                var d = _squad[i];
                if (d != null) hp += d.CurrentHealth;
            }
            return hp;
        }

        private bool AnyAlive(List<Damageable> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].CurrentHealth > 0.01f) return true;
            }
            return false;
        }

        private void IssueAttackOrders()
        {
            if (_commandSystem == null || _enemyHQ == null) { ScheduleNextWave(); return; }
            if (Time.time - _lastCommandAt < CommandCadence) return;
            _lastCommandAt = Time.time;

            // Temporarily override selection buffer with our squad to reuse CommandSystem
            SelectionSystem_GetSelectedOverride(_squad);
            _commandSystem.IssueAttackMove(_enemyHQ.position);
            SelectionSystem_ClearOverride();

            ScheduleNextWave(); // set next fallback window
        }

        private void IssueRetreat()
        {
            if (_commandSystem == null) return;
            if (Time.time - _lastCommandAt < CommandCadence) return;
            _lastCommandAt = Time.time;

            // Retreat to factory position
            var fallback = _factoryQueue != null ? _factoryQueue.transform.position : transform.position;
            SelectionSystem_GetSelectedOverride(_squad);
            _commandSystem.IssueMove(fallback);
            SelectionSystem_ClearOverride();
        }

        private void ScheduleNextWave()
        {
            float min = _waveWindowSeconds.x;
            float max = _waveWindowSeconds.y;
            float w = Mathf.Lerp(min, max, 0.5f); // deterministic midpoint
            _nextWaveTime = Time.time + w;
        }

        // Selection override helpers (non-alloc): temporarily present our squad as "selected"
        private static readonly List<ISelectable> _prevSelectedBackup = new List<ISelectable>(64);
        private static bool _overrideActive;

        private void SelectionSystem_GetSelectedOverride(List<Damageable> squad)
        {
            if (_overrideActive) return;
            _overrideActive = true;
            // Backup previous selection
            SelectionSystem.GetSelectedNonAlloc(_prevSelectedBackup);
            // Build a temp list of ISelectable from squad; if units do not implement ISelectable, we adapt by adding their component as shim
            _selBuffer.Clear();
            for (int i = 0; i < squad.Count; i++)
            {
                var d = squad[i];
                if (d == null) continue;
                var go = d.gameObject;
                if (go.TryGetComponent<ISelectable>(out var s))
                {
                    _selBuffer.Add(s);
                }
            }
            // Apply by marking them selected
            for (int i = 0; i < _selBuffer.Count; i++)
            {
                _selBuffer[i].SetSelected(true);
            }
        }

        private void SelectionSystem_ClearOverride()
        {
            if (!_overrideActive) return;
            _overrideActive = false;
            // Clear our temp selections
            for (int i = 0; i < _selBuffer.Count; i++)
            {
                _selBuffer[i].SetSelected(false);
            }
            _selBuffer.Clear();

            // Restore previous selection visuals (best-effort)
            for (int i = 0; i < _prevSelectedBackup.Count; i++)
            {
                var s = _prevSelectedBackup[i];
                if (s != null) s.SetSelected(true);
            }
            _prevSelectedBackup.Clear();
        }
    }
}