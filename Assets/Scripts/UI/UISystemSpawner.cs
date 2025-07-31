using UnityEngine;
using UnityEngine.UI;
using RedAlert.Economy;
using RedAlert.Core;

namespace RedAlert.UI
{
    /// <summary>
    /// Creates the complete UI system for the RTS game including HUD, resource display, and other panels.
    /// Sets up Canvas, UI elements, and connects them to game systems.
    /// </summary>
    public class UISystemSpawner : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private bool _createUIOnStart = true;
        [SerializeField] private Font _defaultFont;
        
        [Header("HUD Layout")]
        [SerializeField] private int _hudHeight = 80;
        [SerializeField] private Color _hudBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        [SerializeField] private Color _textColor = Color.white;
        
        [Header("Resource Display")]
        [SerializeField] private int _resourceFontSize = 24;
        [SerializeField] private Vector2 _resourcePanelSize = new Vector2(200f, 50f);
        
        [Header("References")]
        [SerializeField] private PlayerEconomy _playerEconomy;
        [SerializeField] private GameStateManager _gameStateManager;
        
        private GameObject _uiRoot;
        private Canvas _mainCanvas;
        private HUDController _hudController;
        private ResourcePanel _resourcePanel;
        
        private void Start()
        {
            if (_createUIOnStart)
            {
                CreateUISystem();
            }
        }
        
        /// <summary>
        /// Create the complete UI system
        /// </summary>
        public void CreateUISystem()
        {
            if (_uiRoot != null)
            {
                DestroyImmediate(_uiRoot);
            }
            
            // Find references if not assigned
            if (_playerEconomy == null)
            {
                _playerEconomy = FindObjectOfType<PlayerEconomy>();
            }
            if (_gameStateManager == null)
            {
                _gameStateManager = FindObjectOfType<GameStateManager>();
            }
            
            // Create UI root
            CreateUIRoot();
            
            // Create main HUD
            CreateMainHUD();
            
            // Create resource panel
            CreateResourcePanel();
            
            // Create game state display
            CreateGameStateDisplay();
            
            // Set up HUD controller
            SetupHUDController();
            
            Debug.Log("UISystemSpawner: Created complete UI system");
        }
        
        private void CreateUIRoot()
        {
            _uiRoot = new GameObject("UI_Root");
            _uiRoot.transform.parent = transform;
            
            // Create main canvas
            _mainCanvas = _uiRoot.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _mainCanvas.sortingOrder = 100;
            
            // Add canvas scaler
            CanvasScaler scaler = _uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add graphics raycaster
            GraphicRaycaster raycaster = _uiRoot.AddComponent<GraphicRaycaster>();
        }
        
        private void CreateMainHUD()
        {
            // Create HUD background panel
            GameObject hudPanel = new GameObject("HUD_Panel");
            hudPanel.transform.SetParent(_mainCanvas.transform, false);
            
            // Set up rect transform for bottom of screen
            RectTransform hudRect = hudPanel.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 0f);
            hudRect.anchorMax = new Vector2(1f, 0f);
            hudRect.anchoredPosition = Vector2.zero;
            hudRect.sizeDelta = new Vector2(0f, _hudHeight);
            
            // Add background image
            Image hudImage = hudPanel.AddComponent<Image>();
            hudImage.color = _hudBackgroundColor;
            hudImage.type = Image.Type.Sliced;
        }
        
        private void CreateResourcePanel()
        {
            // Create resource panel container
            GameObject resourceContainer = new GameObject("Resource_Panel");
            resourceContainer.transform.SetParent(_mainCanvas.transform, false);
            
            // Position in top-left corner
            RectTransform containerRect = resourceContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(0f, 1f);
            containerRect.anchoredPosition = new Vector2(20f, -20f);
            containerRect.sizeDelta = _resourcePanelSize;
            
            // Add background
            Image containerImage = resourceContainer.AddComponent<Image>();
            containerImage.color = new Color(0f, 0f, 0f, 0.6f);
            
            // Create crystalite label
            GameObject crystaliteLabel = new GameObject("Crystalite_Label");
            crystaliteLabel.transform.SetParent(resourceContainer.transform, false);
            
            RectTransform labelRect = crystaliteLabel.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0.4f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            
            Text labelText = crystaliteLabel.AddComponent<Text>();
            labelText.text = "Credits:";
            labelText.font = _defaultFont != null ? _defaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = _resourceFontSize - 4;
            labelText.color = _textColor;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            // Create crystalite value text
            GameObject crystaliteValue = new GameObject("Crystalite_Value");
            crystaliteValue.transform.SetParent(resourceContainer.transform, false);
            
            RectTransform valueRect = crystaliteValue.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.4f, 0.5f);
            valueRect.anchorMax = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = Vector2.zero;
            
            Text valueText = crystaliteValue.AddComponent<Text>();
            valueText.text = "0";
            valueText.font = _defaultFont != null ? _defaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valueText.fontSize = _resourceFontSize;
            valueText.color = _textColor;
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.fontStyle = FontStyle.Bold;
            
            // Add ResourcePanel component
            _resourcePanel = resourceContainer.AddComponent<ResourcePanel>();
            
            // Set the text reference using reflection (since it's private)
            var crystaliteTextField = typeof(ResourcePanel).GetField("_crystaliteText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            crystaliteTextField?.SetValue(_resourcePanel, valueText);
        }
        
        private void CreateGameStateDisplay()
        {
            // Create game state panel in top-right
            GameObject statePanel = new GameObject("GameState_Panel");
            statePanel.transform.SetParent(_mainCanvas.transform, false);
            
            RectTransform stateRect = statePanel.AddComponent<RectTransform>();
            stateRect.anchorMin = new Vector2(1f, 1f);
            stateRect.anchorMax = new Vector2(1f, 1f);
            stateRect.anchoredPosition = new Vector2(-20f, -20f);
            stateRect.sizeDelta = new Vector2(200f, 100f);
            
            // Add background
            Image stateImage = statePanel.AddComponent<Image>();
            stateImage.color = new Color(0f, 0f, 0f, 0.6f);
            
            // Game time display
            GameObject timeDisplay = new GameObject("Game_Time");
            timeDisplay.transform.SetParent(statePanel.transform, false);
            
            RectTransform timeRect = timeDisplay.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0f, 0.7f);
            timeRect.anchorMax = new Vector2(1f, 1f);
            timeRect.anchoredPosition = Vector2.zero;
            timeRect.sizeDelta = Vector2.zero;
            
            Text timeText = timeDisplay.AddComponent<Text>();
            timeText.text = "Time: 0:00";
            timeText.font = _defaultFont != null ? _defaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timeText.fontSize = 16;
            timeText.color = _textColor;
            timeText.alignment = TextAnchor.MiddleCenter;
            
            // Game state display
            GameObject stateDisplay = new GameObject("Game_State");
            stateDisplay.transform.SetParent(statePanel.transform, false);
            
            RectTransform stateDisplayRect = stateDisplay.AddComponent<RectTransform>();
            stateDisplayRect.anchorMin = new Vector2(0f, 0.4f);
            stateDisplayRect.anchorMax = new Vector2(1f, 0.7f);
            stateDisplayRect.anchoredPosition = Vector2.zero;
            stateDisplayRect.sizeDelta = Vector2.zero;
            
            Text stateText = stateDisplay.AddComponent<Text>();
            stateText.text = "Loading...";
            stateText.font = _defaultFont != null ? _defaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stateText.fontSize = 18;
            stateText.color = Color.yellow;
            stateText.alignment = TextAnchor.MiddleCenter;
            stateText.fontStyle = FontStyle.Bold;
            
            // Unit count display
            GameObject unitDisplay = new GameObject("Unit_Count");
            unitDisplay.transform.SetParent(statePanel.transform, false);
            
            RectTransform unitRect = unitDisplay.AddComponent<RectTransform>();
            unitRect.anchorMin = new Vector2(0f, 0f);
            unitRect.anchorMax = new Vector2(1f, 0.4f);
            unitRect.anchoredPosition = Vector2.zero;
            unitRect.sizeDelta = Vector2.zero;
            
            Text unitText = unitDisplay.AddComponent<Text>();
            unitText.text = "Units: 0";
            unitText.font = _defaultFont != null ? _defaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            unitText.fontSize = 14;
            unitText.color = _textColor;
            unitText.alignment = TextAnchor.MiddleCenter;
            
            // Add game state updater component
            GameStateDisplay gameStateDisplay = statePanel.AddComponent<GameStateDisplay>();
            gameStateDisplay.Initialize(timeText, stateText, unitText, _gameStateManager);
        }
        
        private void SetupHUDController()
        {
            // Create HUD controller on the UI root
            _hudController = _uiRoot.AddComponent<HUDController>();
            
            // Set references using reflection (since fields are private)
            var economyField = typeof(HUDController).GetField("_playerEconomy", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            economyField?.SetValue(_hudController, _playerEconomy);
            
            var resourcePanelField = typeof(HUDController).GetField("_resourcePanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resourcePanelField?.SetValue(_hudController, _resourcePanel);
        }
        
        /// <summary>
        /// Clear the UI system
        /// </summary>
        public void ClearUI()
        {
            if (_uiRoot != null)
            {
                DestroyImmediate(_uiRoot);
                _uiRoot = null;
                _mainCanvas = null;
                _hudController = null;
                _resourcePanel = null;
            }
        }
        
        /// <summary>
        /// Get the main canvas for additional UI elements
        /// </summary>
        public Canvas GetMainCanvas()
        {
            return _mainCanvas;
        }
    }
    
    /// <summary>
    /// Helper component to update game state display
    /// </summary>
    public class GameStateDisplay : MonoBehaviour
    {
        private Text _timeText;
        private Text _stateText;
        private Text _unitText;
        private GameStateManager _gameStateManager;
        
        public void Initialize(Text timeText, Text stateText, Text unitText, GameStateManager gameStateManager)
        {
            _timeText = timeText;
            _stateText = stateText;
            _unitText = unitText;
            _gameStateManager = gameStateManager;
        }
        
        private void Update()
        {
            if (_gameStateManager == null) return;
            
            // Update time display
            if (_timeText != null)
            {
                float gameTime = _gameStateManager.GameTime;
                int minutes = Mathf.FloorToInt(gameTime / 60f);
                int seconds = Mathf.FloorToInt(gameTime % 60f);
                _timeText.text = $"Time: {minutes}:{seconds:00}";
            }
            
            // Update game state
            if (_stateText != null)
            {
                _stateText.text = _gameStateManager.CurrentState.ToString();
                
                // Color code by state
                switch (_gameStateManager.CurrentState)
                {
                    case GameStateManager.GameState.Loading:
                        _stateText.color = Color.yellow;
                        break;
                    case GameStateManager.GameState.Playing:
                        _stateText.color = Color.green;
                        break;
                    case GameStateManager.GameState.Victory:
                        _stateText.color = Color.cyan;
                        break;
                    case GameStateManager.GameState.Defeat:
                        _stateText.color = Color.red;
                        break;
                    case GameStateManager.GameState.Paused:
                        _stateText.color = Color.orange;
                        break;
                }
            }
            
            // Update unit count
            if (_unitText != null)
            {
                int playerUnits = _gameStateManager.GetTeamUnitCount(1);
                int enemyUnits = _gameStateManager.GetTeamUnitCount(2);
                _unitText.text = $"Units: {playerUnits} vs {enemyUnits}";
            }
        }
    }
}