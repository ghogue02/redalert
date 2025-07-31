using UnityEngine;
using RedAlert.Units;
using RedAlert.Core;

namespace RedAlert.Economy
{
    /// <summary>
    /// Harvester FSM: Idle → SeekNode → MoveToNode → Mine → MoveToRefinery → Dock/Unload → Repeat.
    /// Uses 4 Hz ticks and non-alloc physics where applicable.
    /// </summary>
    [RequireComponent(typeof(LocomotionAgent))]
    public class HarvesterAgent : MonoBehaviour, UpdateDriver.ISlowTick
    {
        private enum State
        {
            Idle, SeekNode, MoveToNode, Mine, MoveToRefinery, Docking, Unloading
        }

        [Header("Harvest")]
        [SerializeField] private int carryCapacity = 200;
        [SerializeField] private int mineRatePerSec = 40;
        [SerializeField] private float mineRadius = 2.0f;

        [Header("Unload")]
        [SerializeField] private int unloadRatePerSec = 200;

        [Header("Refs")]
        [SerializeField] private LayerMask nodeMask = -1;
        [SerializeField] private LayerMask refineryMask = -1;

        private LocomotionAgent _loco;
        private State _state;
        private int _carried;
        private CrystaliteNode _node;
        private Refinery _refinery;
        private Vector3 _dockPos;
        private int _dockIndex = -1;
        private readonly Collider[] _overlaps = new Collider[8];

        public int Carried => _carried;
        public int CarryCapacity => carryCapacity;

        public bool HasDock => _dockIndex >= 0;
        public int DockIndex => _dockIndex;

        private void Awake()
        {
            _loco = GetComponent<LocomotionAgent>();
        }

        private void OnEnable()
        {
            UpdateDriver.Register(this);
            _state = State.Idle;
        }

        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
        }

        public void AssignDock(Refinery r, int index, Vector3 pos)
        {
            _refinery = r;
            _dockIndex = index;
            _dockPos = pos;
        }

        public void ClearDock()
        {
            _dockIndex = -1;
        }

        public void SlowTick()
        {
            switch (_state)
            {
                case State.Idle:
                    if (_carried >= carryCapacity * 0.5f)
                    {
                        _state = State.MoveToRefinery;
                    }
                    else
                    {
                        _state = State.SeekNode;
                    }
                    break;

                case State.SeekNode:
                    _node = FindNearestNode();
                    if (_node != null && !_node.IsDepleted)
                    {
                        _loco.SetDestination(_node.transform.position);
                        _state = State.MoveToNode;
                    }
                    else
                    {
                        _state = State.Idle;
                    }
                    break;

                case State.MoveToNode:
                    if (_node == null || _node.IsDepleted)
                    {
                        _state = State.SeekNode;
                        break;
                    }
                    if ((transform.position - _node.transform.position).sqrMagnitude <= (mineRadius * mineRadius))
                    {
                        _state = State.Mine;
                    }
                    break;

                case State.Mine:
                    if (_node == null || _node.IsDepleted)
                    {
                        _state = State.SeekNode;
                        break;
                    }
                    // Reserve a chunk then mine
                    int reserve = _node.TryReserve(mineRatePerSec / 4); // 4Hz slice
                    if (reserve > 0)
                    {
                        int mined = _node.MineTick(reserve);
                        _carried = Mathf.Min(carryCapacity, _carried + mined);
                    }

                    if (_carried >= carryCapacity || _node.IsDepleted)
                    {
                        _state = State.MoveToRefinery;
                    }
                    break;

                case State.MoveToRefinery:
                    // find nearby refinery trigger and move to it (basic: pick nearest overlap)
                    var r = FindNearestRefinery();
                    if (r != null)
                    {
                        _refinery = r;
                        _loco.SetDestination(r.transform.position);
                        _state = State.Docking;
                    }
                    else
                    {
                        // stay searching
                    }
                    break;

                case State.Docking:
                    if (_refinery == null)
                    {
                        _state = State.MoveToRefinery;
                        break;
                    }
                    // When trigger assigns dock, we move to dock position
                    if (HasDock)
                    {
                        _loco.SetDestination(_dockPos);
                        if ((transform.position - _dockPos).sqrMagnitude < 0.25f)
                        {
                            _state = State.Unloading;
                        }
                    }
                    break;

                case State.Unloading:
                    if (_refinery == null)
                    {
                        _state = State.MoveToRefinery;
                        break;
                    }
                    if (_carried > 0)
                    {
                        int unload = Mathf.Min(_carried, unloadRatePerSec / 4);
                        _refinery.CommitUnload(unload);
                        _carried -= unload;
                    }
                    else
                    {
                        // done, undock and repeat
                        _refinery.ReleaseDock(this);
                        _refinery = null;
                        _state = State.SeekNode;
                    }
                    break;
            }
        }

        private CrystaliteNode FindNearestNode()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 20f, _overlaps, nodeMask, QueryTriggerInteraction.Ignore);
            CrystaliteNode best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (_overlaps[i] != null && _overlaps[i].TryGetComponent<CrystaliteNode>(out var n))
                {
                    if (n.IsDepleted) continue;
                    float d = (n.transform.position - transform.position).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = n; }
                }
            }
            return best;
        }

        private Refinery FindNearestRefinery()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 30f, _overlaps, refineryMask, QueryTriggerInteraction.Collide);
            Refinery best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (_overlaps[i] != null && _overlaps[i].TryGetComponent<Refinery>(out var r))
                {
                    float d = (r.transform.position - transform.position).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = r; }
                }
            }
            return best;
        }

        // Debug setter retained
        public void DebugSetCarried(int amount)
        {
            _carried = Mathf.Clamp(amount, 0, carryCapacity);
        }
    }
}