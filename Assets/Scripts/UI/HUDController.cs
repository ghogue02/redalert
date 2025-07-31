using UnityEngine;
using RedAlert.Economy;

namespace RedAlert.UI
{
    /// <summary>
    /// Week 1: Wires PlayerEconomy to ResourcePanel to show Crystalite.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private PlayerEconomy _playerEconomy;
        [SerializeField] private ResourcePanel _resourcePanel;

        private void Awake()
        {
            if (_playerEconomy == null) _playerEconomy = FindObjectOfType<PlayerEconomy>();
            if (_resourcePanel == null) _resourcePanel = FindObjectOfType<ResourcePanel>();
        }

        private void OnEnable()
        {
            if (_playerEconomy != null)
            {
                _playerEconomy.OnCrystaliteChanged += OnCrystaliteChanged;
                // Make sure initial value is displayed (PlayerEconomy also fires in OnEnable)
                OnCrystaliteChanged(_playerEconomy.Crystalite);
            }
        }

        private void OnDisable()
        {
            if (_playerEconomy != null)
            {
                _playerEconomy.OnCrystaliteChanged -= OnCrystaliteChanged;
            }
        }

        private void OnCrystaliteChanged(int amount)
        {
            if (_resourcePanel != null)
            {
                _resourcePanel.SetValue(amount);
            }
        }
    }
}