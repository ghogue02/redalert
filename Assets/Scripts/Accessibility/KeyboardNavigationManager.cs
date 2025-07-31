using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RedAlert.Units;
using RedAlert.Build;

namespace RedAlert.Accessibility
{
    /// <summary>
    /// Keyboard navigation manager providing comprehensive keyboard shortcuts
    /// and navigation for Red Alert RTS accessibility.
    /// </summary>
    public class KeyboardNavigationManager : MonoBehaviour
    {
        [Header("Keyboard Shortcuts")]
        [SerializeField] private KeyCode _selectAllKey = KeyCode.A;
        [SerializeField] private KeyCode _attackMoveKey = KeyCode.A;
        [SerializeField] private KeyCode _stopKey = KeyCode.S;
        [SerializeField] private KeyCode _holdPositionKey = KeyCode.H;
        [SerializeField] private KeyCode _patrolKey = KeyCode.P;
        
        [Header("Building Shortcuts")]
        [SerializeField] private KeyCode _buildRefineryKey = KeyCode.R;
        [SerializeField] private KeyCode _buildFactoryKey = KeyCode.F;
        [SerializeField] private KeyCode _buildBarracksKey = KeyCode.B;
        [SerializeField] private KeyCode _cancelBuildKey = KeyCode.Escape;
        
        [Header("Camera Controls")]
        [SerializeField] private KeyCode _centerCameraKey = KeyCode.Space;
        [SerializeField] private float _keyboardCameraSpeed = 10f;
        [SerializeField] private float _cameraEdgeScrollSpeed = 5f;
        
        [Header("UI Navigation")]
        [SerializeField] private bool _enableTabNavigation = true;
        [SerializeField] private bool _enableArrowKeyNavigation = true;
        
        private SelectionSystem _selectionSystem;
        private CommandSystem _commandSystem;
        private BuildPlacementController _buildController;
        private Camera _mainCamera;
        private BuildMenuPanel _buildMenu;
        
        // UI Navigation state
        private List<Selectable> _navigableElements = new List<Selectable>();
        private int _currentUIIndex = -1;
        private bool _uiNavigationMode = false;
        
        // Keyboard shortcuts help
        private readonly Dictionary<KeyCode, string> _shortcutDescriptions = new Dictionary<KeyCode, string>();
        
        private void Awake()
        {
            InitializeReferences();
            InitializeShortcutDescriptions();
        }
        
        private void InitializeReferences()
        {
            _selectionSystem = FindObjectOfType<SelectionSystem>();
            _commandSystem = FindObjectOfType<CommandSystem>();
            _buildController = FindObjectOfType<BuildPlacementController>();
            _buildMenu = FindObjectOfType<BuildMenuPanel>();
            _mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        }
        
        private void InitializeShortcutDescriptions()
        {
            _shortcutDescriptions[_selectAllKey] = "Select All Units (Ctrl+A)";
            _shortcutDescriptions[_attackMoveKey] = "Attack Move (A)";
            _shortcutDescriptions[_stopKey] = "Stop (S)";
            _shortcutDescriptions[_holdPositionKey] = "Hold Position (H)";
            _shortcutDescriptions[_patrolKey] = "Patrol (P)";
            _shortcutDescriptions[_buildRefineryKey] = "Build Refinery (R)";
            _shortcutDescriptions[_buildFactoryKey] = "Build Factory (F)";
            _shortcutDescriptions[_buildBarracksKey] = "Build Barracks (B)";
            _shortcutDescriptions[_centerCameraKey] = "Center Camera on Selection (Space)";
            _shortcutDescriptions[KeyCode.Tab] = "UI Navigation Mode (Tab)";
            _shortcutDescriptions[KeyCode.F1] = "Show Keyboard Shortcuts (F1)";
        }
        
        private void Update()
        {
            HandleKeyboardInput();
            HandleCameraMovement();
            HandleUINavigation();
        }
        
        private void HandleKeyboardInput()
        {
            // Help system
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ShowKeyboardShortcuts();
            }
            
            // UI Navigation toggle
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleUINavigationMode();
            }
            
            // Skip game controls if in UI navigation mode
            if (_uiNavigationMode) return;
            
            // Unit selection shortcuts
            HandleSelectionShortcuts();
            
            // Unit command shortcuts
            HandleCommandShortcuts();
            
            // Building shortcuts
            HandleBuildingShortcuts();
            
            // Camera shortcuts
            HandleCameraShortcuts();
        }
        
        private void HandleSelectionShortcuts()
        {
            // Select all units
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(_selectAllKey))
            {
                SelectAllUnits();
            }
            
            // Select units by type (double-click simulation)
            if (Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKeyDown(_selectAllKey))
            {
                SelectSimilarUnits();
            }
            
            // Control groups (1-9)
            for (int i = 1; i <= 9; i++)
            {
                KeyCode numberKey = (KeyCode)(KeyCode.Alpha1 + i - 1);
                
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(numberKey))
                {
                    // Save control group
                    SaveControlGroup(i);
                }
                else if (Input.GetKeyDown(numberKey))
                {
                    // Select control group
                    SelectControlGroup(i);
                }
            }
        }
        
        private void HandleCommandShortcuts()
        {
            if (_selectionSystem == null || _commandSystem == null) return;
            
            var selectedUnits = _selectionSystem.SelectedUnits;
            if (selectedUnits.Count == 0) return;
            
            // Stop command
            if (Input.GetKeyDown(_stopKey))
            {
                IssueStopCommand();
            }
            
            // Hold position
            if (Input.GetKeyDown(_holdPositionKey))
            {
                IssueHoldPositionCommand();
            }
            
            // Attack move
            if (Input.GetKeyDown(_attackMoveKey) && !Input.GetKey(KeyCode.LeftControl))
            {
                // Set attack move mode - next click will be attack move
                ShowAttackMoveIndicator();
            }
            
            // Patrol
            if (Input.GetKeyDown(_patrolKey))
            {
                // Set patrol mode - next click will start patrol
                ShowPatrolIndicator();
            }
        }
        
        private void HandleBuildingShortcuts()
        {
            if (_buildController == null) return;
            
            // Cancel current building
            if (Input.GetKeyDown(_cancelBuildKey))
            {
                CancelBuilding();
            }
            
            // Quick build shortcuts
            if (Input.GetKeyDown(_buildRefineryKey))
            {
                StartBuilding(BuildPlacementController.BuildType.Refinery);
            }
            
            if (Input.GetKeyDown(_buildFactoryKey))
            {
                StartBuilding(BuildPlacementController.BuildType.Factory);
            }
            
            if (Input.GetKeyDown(_buildBarracksKey))
            {
                StartBuilding(BuildPlacementController.BuildType.Barracks);
            }
        }
        
        private void HandleCameraShortcuts()
        {
            if (_mainCamera == null) return;
            
            // Center camera on selection
            if (Input.GetKeyDown(_centerCameraKey))
            {
                CenterCameraOnSelection();
            }
            
            // Camera bookmarks (F2-F5)
            for (int i = 2; i <= 5; i++)
            {
                KeyCode fKey = (KeyCode)(KeyCode.F2 + i - 2);
                
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(fKey))
                {
                    // Save camera position
                    SaveCameraBookmark(i - 1);
                }
                else if (Input.GetKeyDown(fKey))
                {
                    // Go to camera bookmark
                    GoToCameraBookmark(i - 1);
                }
            }
        }
        
        private void HandleCameraMovement()
        {
            if (_mainCamera == null || _uiNavigationMode) return;
            
            Vector3 movement = Vector3.zero;
            
            // WASD camera movement
            if (Input.GetKey(KeyCode.W)) movement += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) movement += Vector3.back;
            if (Input.GetKey(KeyCode.A)) movement += Vector3.left;
            if (Input.GetKey(KeyCode.D)) movement += Vector3.right;
            
            // Arrow keys camera movement
            if (Input.GetKey(KeyCode.UpArrow)) movement += Vector3.forward;
            if (Input.GetKey(KeyCode.DownArrow)) movement += Vector3.back;
            if (Input.GetKey(KeyCode.LeftArrow)) movement += Vector3.left;
            if (Input.GetKey(KeyCode.RightArrow)) movement += Vector3.right;
            
            // Edge scrolling (mouse near screen edges)
            Vector3 mousePos = Input.mousePosition;
            float edgeSize = 20f;
            
            if (mousePos.x < edgeSize) movement += Vector3.left;
            if (mousePos.x > Screen.width - edgeSize) movement += Vector3.right;
            if (mousePos.y < edgeSize) movement += Vector3.back;
            if (mousePos.y > Screen.height - edgeSize) movement += Vector3.forward;
            
            if (movement != Vector3.zero)
            {
                float speed = Input.GetKey(KeyCode.LeftShift) ? _keyboardCameraSpeed * 2f : _keyboardCameraSpeed;
                _mainCamera.transform.Translate(movement.normalized * speed * Time.deltaTime, Space.World);
            }
        }
        
        private void HandleUINavigation()
        {
            if (!_uiNavigationMode) return;
            
            // Update navigable elements
            RefreshNavigableElements();
            
            // Tab navigation
            if (_enableTabNavigation)
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                        NavigatePrevious();
                    else
                        NavigateNext();
                }
            }
            
            // Arrow key navigation
            if (_enableArrowKeyNavigation)
            {
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))
                    NavigateNext();
                
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
                    NavigatePrevious();
            }
            
            // Enter to activate
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ActivateCurrentUIElement();
            }
            
            // Escape to exit UI navigation
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitUINavigationMode();
            }
        }
        
        // Implementation of specific commands
        private void SelectAllUnits()
        {
            // Implementation would select all player units
            Debug.Log("[Keyboard] Select All Units");
        }
        
        private void SelectSimilarUnits()
        {
            // Implementation would select all units of the same type as current selection
            Debug.Log("[Keyboard] Select Similar Units");
        }
        
        private void SaveControlGroup(int groupNumber)
        {
            // Implementation would save current selection to control group
            Debug.Log($"[Keyboard] Save Control Group {groupNumber}");
        }
        
        private void SelectControlGroup(int groupNumber)
        {
            // Implementation would select stored control group
            Debug.Log($"[Keyboard] Select Control Group {groupNumber}");
        }
        
        private void IssueStopCommand()
        {
            Debug.Log("[Keyboard] Stop Command");
        }
        
        private void IssueHoldPositionCommand()
        {
            Debug.Log("[Keyboard] Hold Position Command");
        }
        
        private void ShowAttackMoveIndicator()
        {
            Debug.Log("[Keyboard] Attack Move Mode");
        }
        
        private void ShowPatrolIndicator()
        {
            Debug.Log("[Keyboard] Patrol Mode");
        }
        
        private void StartBuilding(BuildPlacementController.BuildType buildType)
        {
            if (_buildController != null)
            {
                _buildController.BeginPlacement(buildType);
                Debug.Log($"[Keyboard] Start Building {buildType}");
            }
        }
        
        private void CancelBuilding()
        {
            if (_buildController != null && _buildController.IsActive())
            {
                // Cancel current building placement
                Debug.Log("[Keyboard] Cancel Building");
            }
        }
        
        private void CenterCameraOnSelection()
        {
            if (_selectionSystem != null && _selectionSystem.SelectedUnits.Count > 0)
            {
                Vector3 centerPos = Vector3.zero;
                foreach (var unit in _selectionSystem.SelectedUnits)
                {
                    centerPos += unit.transform.position;
                }
                centerPos /= _selectionSystem.SelectedUnits.Count;
                
                var cameraPos = _mainCamera.transform.position;
                cameraPos.x = centerPos.x;
                cameraPos.z = centerPos.z;
                _mainCamera.transform.position = cameraPos;
                
                Debug.Log("[Keyboard] Center Camera on Selection");
            }
        }
        
        private readonly Vector3[] _cameraBookmarks = new Vector3[4];
        
        private void SaveCameraBookmark(int index)
        {
            if (index >= 0 && index < _cameraBookmarks.Length)
            {
                _cameraBookmarks[index] = _mainCamera.transform.position;
                Debug.Log($"[Keyboard] Save Camera Bookmark {index + 2}");
            }
        }
        
        private void GoToCameraBookmark(int index)
        {
            if (index >= 0 && index < _cameraBookmarks.Length && _cameraBookmarks[index] != Vector3.zero)
            {
                _mainCamera.transform.position = _cameraBookmarks[index];
                Debug.Log($"[Keyboard] Go to Camera Bookmark {index + 2}");
            }
        }
        
        // UI Navigation methods
        private void ToggleUINavigationMode()
        {
            _uiNavigationMode = !_uiNavigationMode;
            
            if (_uiNavigationMode)
            {
                EnterUINavigationMode();
            }
            else
            {
                ExitUINavigationMode();
            }
        }
        
        private void EnterUINavigationMode()
        {
            _uiNavigationMode = true;
            RefreshNavigableElements();
            
            if (_navigableElements.Count > 0)
            {
                _currentUIIndex = 0;
                HighlightCurrentUIElement();
            }
            
            Debug.Log("[Keyboard] Entered UI Navigation Mode");
        }
        
        private void ExitUINavigationMode()
        {
            _uiNavigationMode = false;
            _currentUIIndex = -1;
            ClearUIHighlight();
            
            Debug.Log("[Keyboard] Exited UI Navigation Mode");
        }
        
        private void RefreshNavigableElements()
        {
            _navigableElements.Clear();
            var allSelectables = FindObjectsOfType<Selectable>();
            
            foreach (var selectable in allSelectables)
            {
                if (selectable.IsInteractable() && selectable.gameObject.activeInHierarchy)
                {
                    _navigableElements.Add(selectable);
                }
            }
        }
        
        private void NavigateNext()
        {
            if (_navigableElements.Count == 0) return;
            
            _currentUIIndex = (_currentUIIndex + 1) % _navigableElements.Count;
            HighlightCurrentUIElement();
        }
        
        private void NavigatePrevious()
        {
            if (_navigableElements.Count == 0) return;
            
            _currentUIIndex--;
            if (_currentUIIndex < 0) _currentUIIndex = _navigableElements.Count - 1;
            HighlightCurrentUIElement();
        }
        
        private void HighlightCurrentUIElement()
        {
            ClearUIHighlight();
            
            if (_currentUIIndex >= 0 && _currentUIIndex < _navigableElements.Count)
            {
                var element = _navigableElements[_currentUIIndex];
                element.Select();
                
                // Add visual highlight
                var outline = element.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = element.gameObject.AddComponent<Outline>();
                    outline.effectColor = Color.yellow;
                    outline.effectDistance = new Vector2(2, 2);
                }
                outline.enabled = true;
            }
        }
        
        private void ClearUIHighlight()
        {
            foreach (var element in _navigableElements)
            {
                if (element != null)
                {
                    var outline = element.GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.enabled = false;
                    }
                }
            }
        }
        
        private void ActivateCurrentUIElement()
        {
            if (_currentUIIndex >= 0 && _currentUIIndex < _navigableElements.Count)
            {
                var element = _navigableElements[_currentUIIndex];
                
                if (element is Button button)
                {
                    button.onClick.Invoke();
                }
                else if (element is Toggle toggle)
                {
                    toggle.isOn = !toggle.isOn;
                }
                else if (element is Slider slider)
                {
                    // For sliders, we could implement increment/decrement
                }
                
                Debug.Log($"[Keyboard] Activated UI Element: {element.name}");
            }
        }
        
        private void ShowKeyboardShortcuts()
        {
            string shortcuts = "Red Alert RTS - Keyboard Shortcuts:\n\n";
            
            foreach (var kvp in _shortcutDescriptions)
            {
                shortcuts += $"{kvp.Key}: {kvp.Value}\n";
            }
            
            shortcuts += "\nCamera Movement:\n";
            shortcuts += "WASD or Arrow Keys: Move Camera\n";
            shortcuts += "Shift + Movement: Fast Camera\n";
            shortcuts += "Mouse Edge Scrolling: Move Camera\n";
            
            shortcuts += "\nControl Groups:\n";
            shortcuts += "Ctrl + 1-9: Save Control Group\n";
            shortcuts += "1-9: Select Control Group\n";
            
            shortcuts += "\nCamera Bookmarks:\n";
            shortcuts += "Ctrl + F2-F5: Save Camera Position\n";
            shortcuts += "F2-F5: Go to Camera Position\n";
            
            Debug.Log(shortcuts);
            
            // In a real implementation, you'd show this in a UI panel
        }
        
        // Public API
        public void SetShortcut(string action, KeyCode key)
        {
            // Allow runtime customization of shortcuts
            switch (action.ToLower())
            {
                case "selectall": _selectAllKey = key; break;
                case "attackmove": _attackMoveKey = key; break;
                case "stop": _stopKey = key; break;
                case "holdposition": _holdPositionKey = key; break;
                case "patrol": _patrolKey = key; break;
            }
        }
        
        public Dictionary<string, KeyCode> GetCurrentShortcuts()
        {
            return new Dictionary<string, KeyCode>
            {
                { "SelectAll", _selectAllKey },
                { "AttackMove", _attackMoveKey },
                { "Stop", _stopKey },
                { "HoldPosition", _holdPositionKey },
                { "Patrol", _patrolKey },
                { "BuildRefinery", _buildRefineryKey },
                { "BuildFactory", _buildFactoryKey },
                { "BuildBarracks", _buildBarracksKey }
            };
        }
    }
}