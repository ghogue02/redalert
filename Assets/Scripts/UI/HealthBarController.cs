using UnityEngine;
using RedAlert.Units;

namespace RedAlert.UI
{
    /// <summary>
    /// World-space health bar that follows units and shows health/damage status.
    /// Automatically scales based on camera distance and fades when unit is at full health.
    /// </summary>
    public class HealthBarController : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Canvas _healthBarCanvas;
        [SerializeField] private UnityEngine.UI.Slider _healthSlider;
        [SerializeField] private UnityEngine.UI.Image _fillImage;
        [SerializeField] private UnityEngine.UI.Image _backgroundImage;
        
        [Header("Settings")]
        [SerializeField] private float _heightOffset = 2f;
        [SerializeField] private float _fadeDistance = 30f;
        [SerializeField] private bool _hideWhenFull = true;
        [SerializeField] private float _fadeOutTime = 2f;
        
        [Header("Colors")]
        [SerializeField] private Color _healthyColor = Color.green;
        [SerializeField] private Color _damagedColor = Color.yellow;
        [SerializeField] private Color _criticalColor = Color.red;
        
        private Damageable _damageable;
        private Camera _mainCamera;
        private CanvasGroup _canvasGroup;
        private float _lastDamageTime;
        private float _maxHealth;
        
        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
                _mainCamera = FindObjectOfType<Camera>();
                
            _damageable = GetComponentInParent<Damageable>();
            
            // Set up canvas
            if (_healthBarCanvas == null)
            {
                var canvasGO = new GameObject("HealthBarCanvas");
                canvasGO.transform.SetParent(transform);
                _healthBarCanvas = canvasGO.AddComponent<Canvas>();
                _healthBarCanvas.renderMode = RenderMode.WorldSpace;
                _healthBarCanvas.worldCamera = _mainCamera;
                
                _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
                
                // Create health bar UI
                CreateHealthBarUI();
            }
            
            if (_canvasGroup == null)
                _canvasGroup = _healthBarCanvas.GetComponent<CanvasGroup>();
            
            if (_damageable != null)
            {
                _maxHealth = _damageable.MaxHealth;
                _damageable.OnHealthChanged += OnHealthChanged;
                _damageable.OnDamaged += OnDamaged;
            }
            
            // Initially hidden
            if (_canvasGroup != null)
                _canvasGroup.alpha = _hideWhenFull ? 0f : 1f;
        }
        
        private void CreateHealthBarUI()
        {
            var canvasRect = _healthBarCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 0.3f);
            canvasRect.localScale = Vector3.one * 0.01f; // Scale down for world space
            
            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(_healthBarCanvas.transform, false);
            _backgroundImage = bgGO.AddComponent<UnityEngine.UI.Image>();
            _backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Slider
            var sliderGO = new GameObject("HealthSlider");
            sliderGO.transform.SetParent(_healthBarCanvas.transform, false);
            _healthSlider = sliderGO.AddComponent<UnityEngine.UI.Slider>();
            _healthSlider.minValue = 0f;
            _healthSlider.maxValue = 1f;
            _healthSlider.value = 1f;
            
            var sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
            
            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            _fillImage = fillGO.AddComponent<UnityEngine.UI.Image>();
            _fillImage.color = _healthyColor;
            _fillImage.type = UnityEngine.UI.Image.Type.Filled;
            
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            // Assign fill to slider
            _healthSlider.fillRect = fillRect;
        }
        
        private void OnDestroy()
        {
            if (_damageable != null)
            {
                _damageable.OnHealthChanged -= OnHealthChanged;
                _damageable.OnDamaged -= OnDamaged;
            }
        }
        
        private void Update()
        {
            if (_mainCamera == null || _healthBarCanvas == null) return;
            
            // Position health bar above unit
            var position = transform.position + Vector3.up * _heightOffset;
            _healthBarCanvas.transform.position = position;
            
            // Face camera
            _healthBarCanvas.transform.LookAt(_mainCamera.transform);
            _healthBarCanvas.transform.Rotate(0, 180, 0);
            
            // Handle fading
            if (_canvasGroup != null)
            {
                float targetAlpha = 0f;
                
                if (_damageable != null)
                {
                    float healthPercent = _damageable.Health / _maxHealth;
                    
                    // Show if damaged or recently took damage
                    bool shouldShow = !_hideWhenFull || 
                                      healthPercent < 1f || 
                                      Time.time - _lastDamageTime < _fadeOutTime;
                    
                    if (shouldShow)
                    {
                        // Distance-based fading
                        float distance = Vector3.Distance(_mainCamera.transform.position, position);
                        targetAlpha = Mathf.Clamp01(1f - (distance / _fadeDistance));
                    }
                }
                
                _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
            }
        }
        
        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            if (_healthSlider != null)
            {
                float healthPercent = currentHealth / maxHealth;
                _healthSlider.value = healthPercent;
                
                // Update color based on health percentage
                if (_fillImage != null)
                {
                    if (healthPercent > 0.7f)
                        _fillImage.color = _healthyColor;
                    else if (healthPercent > 0.3f)
                        _fillImage.color = _damagedColor;
                    else
                        _fillImage.color = _criticalColor;
                }
            }
        }
        
        private void OnDamaged(float damage, float currentHealth, float maxHealth)
        {
            _lastDamageTime = Time.time;
            
            // Show health bar immediately when taking damage
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }
        
        public void ForceShow(float duration = 3f)
        {
            _lastDamageTime = Time.time + duration - _fadeOutTime;
        }
        
        public void ForceHide()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }
    }
}