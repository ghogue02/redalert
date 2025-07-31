using UnityEngine;
using UnityEngine.AI;
using RedAlert.Pathing;

namespace RedAlert.Units
{
    /// <summary>
    /// Week 1: Thin wrapper over NavMeshAgent. Provides SetDestination and IsMoving.
    /// WebGL safety: if PathService available, defers to it to enqueue path set to avoid spikes.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class LocomotionAgent : MonoBehaviour
    {
        [Header("Agent Defaults")]
        [SerializeField] private float _speed = 3.5f;
        [SerializeField] private float _acceleration = 8f;
        [SerializeField] private float _angularSpeed = 360f;
        [SerializeField] private float _stoppingDistance = 0.1f;
        [SerializeField] private bool _autoBraking = true;

        private NavMeshAgent _agent;
        private PathService _pathService; // optional

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _speed;
            _agent.acceleration = _acceleration;
            _agent.angularSpeed = _angularSpeed;
            _agent.stoppingDistance = _stoppingDistance;
            _agent.autoBraking = _autoBraking;

            // Optional: find PathService in scene; if present we enqueue to it.
            _pathService = FindObjectOfType<PathService>();
        }

        public bool IsMoving
        {
            get
            {
                if (_agent.pathPending) return true;
                if (_agent.remainingDistance > _agent.stoppingDistance) return true;
                return !_agent.hasPath || _agent.velocity.sqrMagnitude <= 0.0001f ? false : true;
            }
        }

        /// <summary>
        /// Safe set destination; if PathService present, use its enqueue to reduce spikes on WebGL.
        /// TODO(Week2): centralize all path requests through PathService with batching/cadence.
        /// </summary>
        public void SetDestination(Vector3 worldPos)
        {
            if (_pathService != null)
            {
                // Stub-friendly: PathService has an EnqueueSetDestination, or we simulate via action.
                // Using a safe enqueue if available; otherwise fall back immediately.
                if (!_pathService.TryEnqueueSetDestination(_agent, worldPos))
                {
                    _agent.SetDestination(worldPos);
                }
            }
            else
            {
                _agent.SetDestination(worldPos);
            }
        }

        // Debug helper kept for parity with previous placeholder
        public void DebugWarp(Vector3 worldPos)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.Warp(worldPos);
            }
            else
            {
                transform.position = worldPos;
            }
        }
    }
}