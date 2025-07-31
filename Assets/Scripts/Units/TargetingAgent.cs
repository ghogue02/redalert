using UnityEngine;
using RedAlert.Core;

namespace RedAlert.Units
{
    /// <summary>
    /// Targeting cadence stub component at slow tick (4 Hz via UpdateDriver).
    /// Extracted from selection ownership to keep responsibilities clean. No behavior change.
    /// Non-alloc placeholder: prealloc overlap buffer; actual targeting to be implemented later.
    /// </summary>
    public class TargetingAgent : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [SerializeField] private float _range = 12f;
        [SerializeField] private LayerMask _targetMask = -1;

        private readonly Collider[] _overlaps = new Collider[32];

        private void OnEnable()
        {
            UpdateDriver.Register(this);
        }

        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
        }

        public void SlowTick()
        {
            // Placeholder for discovery with no allocations beyond prealloc buffer:
            // int count = Physics.OverlapSphereNonAlloc(transform.position, _range, _overlaps, _targetMask, QueryTriggerInteraction.Ignore);
            // Future: pick nearest hostile and notify weapon system.
        }
    }
}