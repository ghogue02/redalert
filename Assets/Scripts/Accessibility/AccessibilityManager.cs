using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RedAlert.UI;

namespace RedAlert.Accessibility
{
    /// <summary>
    /// Accessibility manager providing colorblind-safe UI, keyboard shortcuts,
    /// tooltips, and other accessibility features for Red Alert RTS.
    /// </summary>
    public class AccessibilityManager : MonoBehaviour
    {
        [Header("Colorblind Support")]
        [SerializeField] private ColorblindType _colorblindMode = ColorblindType.None;
        [SerializeField] private bool _usePatterns = false;
        [SerializeField] private bool _useHighContrast = false;
        
        [Header("UI Accessibility")]
        [SerializeField] private float _tooltipDelay = 0.5f;
        [SerializeField] private bool _enableKeyboardNavigation = true;
        [SerializeField] private float _uiScaleFactor = 1f;
        [SerializeField] private bool _enableScreenReader = false;
        
        [Header("Visual Indicators")]
        [SerializeField] private bool _enableSelectionIndicators = true;
        [SerializeField] private bool _enableHealthBarAlways = false;
        [SerializeField] private bool _enableUnitOutlines = true;
        
        private readonly Dictionary<ColorblindType, ColorPalette> _colorPalettes = new Dictionary<ColorblindType, ColorPalette>();
        private TooltipSystem _tooltipSystem;
        private KeyboardNavigationManager _keyboardNav;
        
        public ColorblindType ColorblindMode
        {
            get => _colorblindMode;
            set
            {
                _colorblindMode = value;
                ApplyColorblindSettings();
            }
        }
        
        public bool UseHighContrast
        {
            get => _useHighContrast;
            set
            {
                _useHighContrast = value;
                ApplyColorblindSettings();
            }
        }
        
        public float UIScaleFactor
        {
            get => _uiScaleFactor;
            set
            {
                _uiScaleFactor = Mathf.Clamp(value, 0.8f, 2f);
                ApplyUIScaling();
            }
        }
        
        private void Awake()
        {
            InitializeColorPalettes();
            InitializeAccessibilityFeatures();
        }
        
        private void Start()
        {
            ApplyAccessibilitySettings();
        }
        
        private void InitializeColorPalettes()
        {
            // Normal vision
            _colorPalettes[ColorblindType.None] = new ColorPalette
            {
                playerColor = new Color(0.2f, 0.6f, 1f), // Blue
                enemyColor = new Color(1f, 0.3f, 0.2f),  // Red
                neutralColor = Color.yellow,
                healthyColor = Color.green,
                damagedColor = Color.yellow,
                criticalColor = Color.red
            };
            
            // Protanopia (red-blind)
            _colorPalettes[ColorblindType.Protanopia] = new ColorPalette
            {
                playerColor = new Color(0.2f, 0.6f, 1f), // Blue (unchanged)
                enemyColor = new Color(0.8f, 0.6f, 0f),  // Orange/brown instead of red
                neutralColor = new Color(1f, 1f, 0.2f),  // Bright yellow
                healthyColor = new Color(0f, 0.8f, 1f),  // Cyan instead of green
                damagedColor = new Color(1f, 0.8f, 0f),  // Orange
                criticalColor = new Color(0.6f, 0.4f, 0f) // Dark orange
            };
            
            // Deuteranopia (green-blind)
            _colorPalettes[ColorblindType.Deuteranopia] = new ColorPalette
            {
                playerColor = new Color(0.2f, 0.6f, 1f), // Blue (unchanged)
                enemyColor = new Color(1f, 0.3f, 0.2f),  // Red (unchanged)
                neutralColor = new Color(1f, 1f, 0.2f),  // Bright yellow
                healthyColor = new Color(0f, 0.8f, 1f),  // Cyan instead of green
                damagedColor = new Color(1f, 0.8f, 0f),  // Orange
                criticalColor = new Color(0.8f, 0.2f, 0.2f) // Dark red
            };
            
            // Tritanopia (blue-blind)
            _colorPalettes[ColorblindType.Tritanopia] = new ColorPalette
            {
                playerColor = new Color(0f, 0.8f, 0.2f),  // Green instead of blue
                enemyColor = new Color(1f, 0.3f, 0.6f),   // Pink/magenta instead of red
                neutralColor = new Color(0.8f, 0.8f, 0.8f), // Light gray
                healthyColor = Color.green,               // Green (unchanged)
                damagedColor = new Color(1f, 0.6f, 0f),   // Orange
                criticalColor = new Color(0.8f, 0f, 0.4f) // Dark pink
            };
        }
        
        private void InitializeAccessibilityFeatures()
        {
            // Initialize tooltip system
            _tooltipSystem = GetComponent<TooltipSystem>();
            if (_tooltipSystem == null)
            {
                _tooltipSystem = gameObject.AddComponent<TooltipSystem>();
            }
            
            // Initialize keyboard navigation
            if (_enableKeyboardNavigation)
            {
                _keyboardNav = GetComponent<KeyboardNavigationManager>();
                if (_keyboardNav == null)
                {
                    _keyboardNav = gameObject.AddComponent<KeyboardNavigationManager>();
                }
            }
        }
        
        private void ApplyAccessibilitySettings()
        {
            ApplyColorblindSettings();
            ApplyUIScaling();
            ApplyVisualIndicators();
        }
        
        private void ApplyColorblindSettings()
        {
            if (!_colorPalettes.ContainsKey(_colorblindMode)) return;
            
            var palette = _colorPalettes[_colorblindMode];
            
            // Apply to team colors
            UpdateTeamColors(palette);
            
            // Apply to UI elements
            UpdateUIColors(palette);
            
            // Apply to health bars
            UpdateHealthBarColors(palette);
            
            // Apply high contrast if enabled
            if (_useHighContrast)
            {
                ApplyHighContrast();
            }
        }
        
        private void UpdateTeamColors(ColorPalette palette)
        {
            // Update all selection rings
            var selectionRings = FindObjectsOfType<SelectionRing>();
            foreach (var ring in selectionRings)
            {
                var team = ring.GetComponent<RedAlert.Units.Team>();
                if (team != null)
                {
                    Color teamColor = team.TeamId == 1 ? palette.playerColor : palette.enemyColor;
                    ring.SetRingColor(teamColor);
                }
            }
            
            // Update minimap colors
            var minimapController = FindObjectOfType<MinimapController>();
            if (minimapController != null)
            {
                // Minimap color updates would be implemented here
            }
        }
        
        private void UpdateUIColors(ColorPalette palette)
        {
            // Update UI button colors and highlights
            var buttons = FindObjectsOfType<Button>();
            foreach (var button in buttons)
            {
                var colors = button.colors;
                if (_useHighContrast)
                {
                    colors.normalColor = Color.white;
                    colors.highlightedColor = palette.playerColor;
                    colors.pressedColor = palette.enemyColor;
                }
                button.colors = colors;
            }
        }
        
        private void UpdateHealthBarColors(ColorPalette palette)
        {
            var healthBars = FindObjectsOfType<HealthBarController>();
            foreach (var healthBar in healthBars)
            {
                // Health bar color updates would be implemented here
                // This would require exposing color properties on HealthBarController
            }
        }
        
        private void ApplyHighContrast()
        {
            // Increase contrast for better visibility
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                var canvasGroup = canvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
                }
                
                // Increase alpha for better visibility
                canvasGroup.alpha = Mathf.Min(1f, canvasGroup.alpha * 1.2f);
            }
        }
        
        private void ApplyUIScaling()
        {
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.scaleFactor *= _uiScaleFactor;
                }
            }
        }
        
        private void ApplyVisualIndicators()
        {
            // Enable/disable selection indicators
            var selectionRings = FindObjectsOfType<SelectionRing>();
            foreach (var ring in selectionRings)
            {
                ring.gameObject.SetActive(_enableSelectionIndicators);
            }
            
            // Enable/disable always-on health bars
            var healthBars = FindObjectsOfType<HealthBarController>();
            foreach (var healthBar in healthBars)
            {
                // This would require exposing the hideWhenFull property
            }
        }
        
        // Public API for runtime accessibility changes
        public void SetColorblindMode(ColorblindType mode)
        {
            ColorblindMode = mode;
        }
        
        public void ToggleHighContrast()
        {
            UseHighContrast = !UseHighContrast;
        }
        
        public void SetUIScale(float scale)
        {
            UIScaleFactor = scale;
        }
        
        public void ToggleKeyboardNavigation()
        {
            _enableKeyboardNavigation = !_enableKeyboardNavigation;
            
            if (_keyboardNav != null)
            {
                _keyboardNav.enabled = _enableKeyboardNavigation;
            }
        }
        
        public void EnableScreenReaderSupport(bool enable)
        {
            _enableScreenReader = enable;
            // Screen reader integration would be implemented here
        }
        
        // Accessibility info for screen readers
        public string GetUIElementDescription(GameObject uiElement)
        {
            // Generate description for screen readers
            var button = uiElement.GetComponent<Button>();
            if (button != null)
            {
                var text = button.GetComponentInChildren<Text>();
                return text != null ? $"Button: {text.text}" : "Button";
            }
            
            var slider = uiElement.GetComponent<Slider>();
            if (slider != null)
            {
                return $"Slider: {slider.value:F1} out of {slider.maxValue:F1}";
            }
            
            return uiElement.name;
        }
    }
    
    public enum ColorblindType
    {
        None,        // Normal vision
        Protanopia,  // Red-blind
        Deuteranopia, // Green-blind
        Tritanopia   // Blue-blind
    }
    
    [System.Serializable]
    public struct ColorPalette
    {
        public Color playerColor;
        public Color enemyColor;
        public Color neutralColor;
        public Color healthyColor;
        public Color damagedColor;
        public Color criticalColor;
    }
}