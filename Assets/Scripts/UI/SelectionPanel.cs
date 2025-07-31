using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RedAlert.Units;

namespace RedAlert.UI
{
    /// <summary>
    /// Shows current unit selection details with portraits, health bars, and names.
    /// </summary>
    public class SelectionPanel : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject _selectionInfoPanel;
        [SerializeField] private Text _selectionCountText;
        [SerializeField] private Transform _portraitContainer;
        [SerializeField] private GameObject _portraitPrefab;
        
        [Header("Single Unit Display")]
        [SerializeField] private GameObject _singleUnitPanel;
        [SerializeField] private Text _unitNameText;
        [SerializeField] private Slider _healthBar;
        [SerializeField] private Text _healthText;
        [SerializeField] private Image _unitPortrait;

        private SelectionSystem _selectionSystem;
        private readonly List<GameObject> _portraitInstances = new List<GameObject>();

        private void Awake()
        {
            if (_selectionSystem == null) _selectionSystem = FindObjectOfType<SelectionSystem>();
            
            // Initialize UI state
            if (_selectionInfoPanel != null) _selectionInfoPanel.SetActive(false);
            if (_singleUnitPanel != null) _singleUnitPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (_selectionSystem != null)
            {
                _selectionSystem.OnSelectionChanged += OnSelectionChanged;
            }
        }

        private void OnDisable()
        {
            if (_selectionSystem != null)
            {
                _selectionSystem.OnSelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnSelectionChanged(List<BasicUnit> selectedUnits)
        {
            RefreshSelectionDisplay(selectedUnits);
        }

        private void RefreshSelectionDisplay(List<BasicUnit> selectedUnits)
        {
            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                // No selection
                if (_selectionInfoPanel != null) _selectionInfoPanel.SetActive(false);
                if (_singleUnitPanel != null) _singleUnitPanel.SetActive(false);
                return;
            }

            if (selectedUnits.Count == 1)
            {
                // Single unit selection
                ShowSingleUnit(selectedUnits[0]);
            }
            else
            {
                // Multiple units selection
                ShowMultipleUnits(selectedUnits);
            }
        }

        private void ShowSingleUnit(BasicUnit unit)
        {
            if (_singleUnitPanel != null) _singleUnitPanel.SetActive(true);
            if (_selectionInfoPanel != null) _selectionInfoPanel.SetActive(false);

            if (unit == null) return;

            // Update unit name
            if (_unitNameText != null)
            {
                string unitName = unit.name;
                // Clean up name (remove "(Clone)" suffix)
                if (unitName.EndsWith("(Clone)"))
                    unitName = unitName.Substring(0, unitName.Length - 7);
                _unitNameText.text = unitName;
            }

            // Update health bar
            var damageable = unit.GetComponent<Damageable>();
            if (damageable != null && _healthBar != null)
            {
                float healthPercent = damageable.Health / damageable.MaxHealth;
                _healthBar.value = healthPercent;
                
                if (_healthText != null)
                {
                    _healthText.text = $"{Mathf.CeilToInt(damageable.Health)}/{Mathf.CeilToInt(damageable.MaxHealth)}";
                }
            }

            // TODO: Set unit portrait sprite based on unit type
            if (_unitPortrait != null)
            {
                // For now, use a default color based on team
                var team = unit.GetComponent<Team>();
                if (team != null)
                {
                    _unitPortrait.color = team.TeamId == 1 ? Color.blue : Color.red;
                }
            }
        }

        private void ShowMultipleUnits(List<BasicUnit> units)
        {
            if (_selectionInfoPanel != null) _selectionInfoPanel.SetActive(true);
            if (_singleUnitPanel != null) _singleUnitPanel.SetActive(false);

            // Update selection count
            if (_selectionCountText != null)
            {
                _selectionCountText.text = $"{units.Count} Units Selected";
            }

            // Clear existing portraits
            ClearPortraits();

            // Create portraits for each unit (limit to reasonable number)
            int maxPortraits = 12;
            for (int i = 0; i < Mathf.Min(units.Count, maxPortraits); i++)
            {
                CreatePortrait(units[i]);
            }
        }

        private void CreatePortrait(BasicUnit unit)
        {
            if (_portraitPrefab == null || _portraitContainer == null) return;

            var portraitGO = Instantiate(_portraitPrefab, _portraitContainer);
            _portraitInstances.Add(portraitGO);

            // Set up portrait (simplified for now)
            var image = portraitGO.GetComponent<Image>();
            if (image != null)
            {
                var team = unit.GetComponent<Team>();
                if (team != null)
                {
                    image.color = team.TeamId == 1 ? Color.blue : Color.red;
                }
            }

            // Add health bar to portrait if available
            var healthBar = portraitGO.GetComponentInChildren<Slider>();
            if (healthBar != null)
            {
                var damageable = unit.GetComponent<Damageable>();
                if (damageable != null)
                {
                    healthBar.value = damageable.Health / damageable.MaxHealth;
                }
            }
        }

        private void ClearPortraits()
        {
            foreach (var portrait in _portraitInstances)
            {
                if (portrait != null)
                    DestroyImmediate(portrait);
            }
            _portraitInstances.Clear();
        }

        private void Update()
        {
            // Update health bars in real-time for single unit selection
            if (_singleUnitPanel != null && _singleUnitPanel.activeInHierarchy)
            {
                if (_selectionSystem != null && _selectionSystem.SelectedUnits.Count == 1)
                {
                    var unit = _selectionSystem.SelectedUnits[0];
                    var damageable = unit.GetComponent<Damageable>();
                    if (damageable != null && _healthBar != null)
                    {
                        float healthPercent = damageable.Health / damageable.MaxHealth;
                        _healthBar.value = healthPercent;
                        
                        if (_healthText != null)
                        {
                            _healthText.text = $"{Mathf.CeilToInt(damageable.Health)}/{Mathf.CeilToInt(damageable.MaxHealth)}";
                        }
                    }
                }
            }
        }

        public void DebugRefresh()
        {
            if (_selectionSystem != null)
            {
                RefreshSelectionDisplay(_selectionSystem.SelectedUnits);
            }
        }
    }
}