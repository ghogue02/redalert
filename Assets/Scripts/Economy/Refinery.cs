using UnityEngine;
using System.Collections.Generic;

namespace RedAlert.Economy
{
    /// <summary>
    /// Refinery docking with simple slot queue; harvesters unload into PlayerEconomy.
    /// </summary>
    public class Refinery : MonoBehaviour
    {
        [SerializeField] private PlayerEconomy _ownerEconomy;
        [SerializeField] private Transform[] dockPoints;

        private readonly Queue<int> _free = new Queue<int>(8);

        private void Awake()
        {
            // Initialize free slots
            if (dockPoints != null)
            {
                for (int i = 0; i < dockPoints.Length; i++) _free.Enqueue(i);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<HarvesterAgent>(out var harv)) return;
            TryAssignDock(harv);
        }

        private void OnTriggerExit(Collider other)
        {
            // If a harvester leaves while assigned, release its slot
            if (other.TryGetComponent<HarvesterAgent>(out var harv))
            {
                ReleaseDock(harv);
            }
        }

        public bool TryAssignDock(HarvesterAgent h)
        {
            if (h == null || dockPoints == null || _free.Count == 0) return false;
            if (h.HasDock) return true;
            int idx = _free.Dequeue();
            h.AssignDock(this, idx, dockPoints[idx].position);
            return true;
        }

        public void ReleaseDock(HarvesterAgent h)
        {
            if (h == null) return;
            if (h.HasDock && dockPoints != null)
            {
                int idx = h.DockIndex;
                h.ClearDock();
                _free.Enqueue(idx);
            }
        }

        public void CommitUnload(int amount)
        {
            if (_ownerEconomy != null && amount > 0)
            {
                _ownerEconomy.AddCrystalite(amount);
            }
        }
    }
}