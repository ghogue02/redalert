using System;
using UnityEngine;

namespace RedAlert.Economy
{
    /// <summary>
    /// Week 1: Tracks Crystalite and exposes an event for HUD binding.
    /// </summary>
    public class PlayerEconomy : MonoBehaviour
    {
        [SerializeField] private int _crystalite = 500;

        public int Crystalite => _crystalite;

        /// <summary>Raised when Crystalite changes. Arg: current amount.</summary>
        public event Action<int> OnCrystaliteChanged;

        private void OnEnable()
        {
            // Emit initial value for UI binding on scene bootstrap.
            OnCrystaliteChanged?.Invoke(_crystalite);
        }

        public void SetCrystalite(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount == _crystalite) return;
            _crystalite = amount;
            OnCrystaliteChanged?.Invoke(_crystalite);
        }

        public void AddCrystalite(int delta)
        {
            if (delta == 0) return;
            SetCrystalite(_crystalite + delta);
        }

        // Debug helper for editor tweaks
        public void DebugSetCrystalite(int amount) => SetCrystalite(amount);
    }
}