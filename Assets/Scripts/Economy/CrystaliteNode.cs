using UnityEngine;
using RedAlert.Core;

namespace RedAlert.Economy
{
    /// <summary>
    /// Crystalite node with reservation and mining at 4 Hz via UpdateDriver.
    /// </summary>
    public class CrystaliteNode : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("Node")]
        [SerializeField] private int capacity = 5000;
        [SerializeField] private int yieldPerSecond = 40;
        [SerializeField] private int reservePerMiner = 40;

        private int _reserved; // amount reserved but not yet mined
        public int Remaining => Mathf.Max(0, capacity - _reserved);
        public bool IsDepleted => capacity <= 0;

        private void OnEnable()
        {
            UpdateDriver.Register(this);
        }

        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
        }

        /// <summary>Attempt to reserve amount for a miner. Returns granted amount.</summary>
        public int TryReserve(int amount)
        {
            if (capacity <= 0) return 0;
            int grant = Mathf.Clamp(amount, 0, capacity - _reserved);
            _reserved += grant;
            return grant;
        }

        /// <summary>Commit mined amount from capacity (consumes reservation window if any).</summary>
        public int MineTick(int amount)
        {
            if (capacity <= 0) return 0;
            int mine = Mathf.Clamp(amount, 0, capacity);
            capacity -= mine;
            if (_reserved >= mine) _reserved -= mine;
            if (capacity == 0)
            {
                EventBus.PublishNodeDepleted();
            }
            return mine;
        }

        public void SlowTick()
        {
            // Node itself is passive; miners call MineTick. Kept to allow future regen logic.
        }
    }
}