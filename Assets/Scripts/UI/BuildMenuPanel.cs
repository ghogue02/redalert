using UnityEngine;
using UnityEngine.UI;
using RedAlert.Build;
using RedAlert.Economy;

namespace RedAlert.UI
{
    /// <summary>
    /// Minimal UGUI panel with buttons to place buildings via BuildPlacementController.
    /// Costs from PlacementRules; buttons disabled during active placement or insufficient funds.
    /// </summary>
    public class BuildMenuPanel : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _btnRefinery;
        [SerializeField] private Button _btnFactory;

        [Header("Cost Labels (optional)")]
        [SerializeField] private Text _refineryCostText;
        [SerializeField] private Text _factoryCostText;

        [Header("Refs")]
        [SerializeField] private BuildPlacementController _builder;
        [SerializeField] private PlayerEconomy _economy;

        private void Awake()
        {
            if (_builder == null) _builder = FindObjectOfType<BuildPlacementController>();
            if (_economy == null) _economy = FindObjectOfType<PlayerEconomy>();
        }

        private void OnEnable()
        {
            if (_refineryCostText != null) _refineryCostText.text = PlacementRules.CostRefinery.ToString();
            if (_factoryCostText != null) _factoryCostText.text = PlacementRules.CostFactory.ToString();

            if (_economy != null)
            {
                _economy.OnCrystaliteChanged += OnCrystaliteChanged;
                OnCrystaliteChanged(_economy.Crystalite);
            }

            if (_btnRefinery != null) _btnRefinery.onClick.AddListener(OnClickRefinery);
            if (_btnFactory != null) _btnFactory.onClick.AddListener(OnClickFactory);
        }

        private void OnDisable()
        {
            if (_economy != null) _economy.OnCrystaliteChanged -= OnCrystaliteChanged;
            if (_btnRefinery != null) _btnRefinery.onClick.RemoveListener(OnClickRefinery);
            if (_btnFactory != null) _btnFactory.onClick.RemoveListener(OnClickFactory);
        }

        private void Update()
        {
            bool active = _builder != null && _builder.IsActive();
            UpdateInteractable(!active);
        }

        private void OnCrystaliteChanged(int amount)
        {
            UpdateInteractable(_builder == null || !_builder.IsActive());
        }

        private void UpdateInteractable(bool allow)
        {
            int funds = _economy != null ? _economy.Crystalite : 0;
            if (_btnRefinery != null) _btnRefinery.interactable = allow && funds >= PlacementRules.CostRefinery;
            if (_btnFactory != null) _btnFactory.interactable = allow && funds >= PlacementRules.CostFactory;
        }

        private void OnClickRefinery()
        {
            if (_builder != null) _builder.BeginPlacement(BuildPlacementController.BuildType.Refinery);
        }

        private void OnClickFactory()
        {
            if (_builder != null) _builder.BeginPlacement(BuildPlacementController.BuildType.Factory);
        }
    }
}