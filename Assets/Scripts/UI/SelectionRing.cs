using UnityEngine;
using RedAlert.Units;

namespace RedAlert.UI
{
    /// <summary>
    /// Visual selection ring that appears around selected units.
    /// Supports team colors, animated effects, and automatic sizing based on unit bounds.
    /// </summary>
    public class SelectionRing : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private GameObject _ringPrefab;
        [SerializeField] private Material _friendlyMaterial;
        [SerializeField] private Material _enemyMaterial;
        [SerializeField] private Material _neutralMaterial;
        
        [Header("Animation")]
        [SerializeField] private bool _animate = true;
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _rotationSpeed = 30f;
        [SerializeField] private float _minScale = 0.9f;
        [SerializeField] private float _maxScale = 1.1f;
        
        private GameObject _ringInstance;
        private Renderer _ringRenderer;
        private bool _isSelected;
        private Team _unitTeam;
        private float _baseScale = 1f;
        private MaterialPropertyBlock _materialPropertyBlock;
        
        private void Awake()
        {
            _unitTeam = GetComponent<Team>();
            _materialPropertyBlock = new MaterialPropertyBlock();
            
            // Auto-detect unit bounds for ring scaling
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                _baseScale = Mathf.Max(collider.bounds.size.x, collider.bounds.size.z) * 0.6f;
            }
            
            CreateSelectionRing();
            SetVisible(false);
        }
        
        private void CreateSelectionRing()
        {
            if (_ringPrefab != null)
            {
                _ringInstance = Instantiate(_ringPrefab, transform);
                _ringInstance.transform.localPosition = Vector3.zero;
                _ringInstance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Create default ring using Unity primitives
                _ringInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _ringInstance.name = "SelectionRing";
                _ringInstance.transform.SetParent(transform);
                _ringInstance.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                _ringInstance.transform.localRotation = Quaternion.identity;
                _ringInstance.transform.localScale = new Vector3(_baseScale, 0.1f, _baseScale);
                
                // Remove collider from ring
                var collider = _ringInstance.GetComponent<Collider>();
                if (collider != null)
                    DestroyImmediate(collider);
            }
            
            _ringRenderer = _ringInstance.GetComponent<Renderer>();
            if (_ringRenderer != null)
            {
                SetRingMaterial();
            }
        }
        
        private void SetRingMaterial()
        {
            if (_ringRenderer == null || _unitTeam == null) return;
            
            Material materialToUse = _neutralMaterial;
            
            // Determine material based on team relationship
            if (_unitTeam.TeamId == 1) // Player team
            {
                materialToUse = _friendlyMaterial;
            }
            else if (_unitTeam.TeamId > 1) // Enemy teams
            {
                materialToUse = _enemyMaterial;
            }
            
            if (materialToUse != null)
            {
                _ringRenderer.material = materialToUse;
            }
            else
            {
                // Fallback: use team colors
                Color teamColor = GetTeamColor();
                _ringRenderer.GetPropertyBlock(_materialPropertyBlock);
                _materialPropertyBlock.SetColor("_Color", teamColor);
                _materialPropertyBlock.SetColor("_BaseColor", teamColor); // URP compatibility
                _ringRenderer.SetPropertyBlock(_materialPropertyBlock);
            }
        }
        
        private Color GetTeamColor()
        {
            if (_unitTeam == null) return Color.white;
            
            switch (_unitTeam.TeamId)
            {
                case 1: return Color.blue;   // Player
                case 2: return Color.red;    // Enemy 1
                case 3: return Color.yellow; // Enemy 2
                case 4: return Color.green;  // Enemy 3
                default: return Color.gray;  // Neutral
            }
        }
        
        private void Update()
        {
            if (!_isSelected || _ringInstance == null) return;
            
            if (_animate)
            {
                // Pulse animation
                float pulse = Mathf.Lerp(_minScale, _maxScale, 
                    (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f);
                
                // Rotation animation
                float rotation = Time.time * _rotationSpeed;
                
                _ringInstance.transform.localScale = new Vector3(
                    _baseScale * pulse, 
                    _ringInstance.transform.localScale.y, 
                    _baseScale * pulse
                );
                
                _ringInstance.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
            }
        }
        
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            SetVisible(selected);
        }
        
        public void SetVisible(bool visible)
        {
            if (_ringInstance != null)
            {
                _ringInstance.SetActive(visible);
            }
        }
        
        public void SetRingColor(Color color)
        {
            if (_ringRenderer != null && _materialPropertyBlock != null)
            {
                _ringRenderer.GetPropertyBlock(_materialPropertyBlock);
                _materialPropertyBlock.SetColor("_Color", color);
                _materialPropertyBlock.SetColor("_BaseColor", color);
                _ringRenderer.SetPropertyBlock(_materialPropertyBlock);
            }
        }
        
        public void SetRingScale(float scale)
        {
            _baseScale = scale;
            if (_ringInstance != null)
            {
                _ringInstance.transform.localScale = new Vector3(scale, 
                    _ringInstance.transform.localScale.y, scale);
            }
        }
        
        private void OnDestroy()
        {
            if (_ringInstance != null)
            {
                DestroyImmediate(_ringInstance);
            }
        }
        
        // Integration with SelectionSystem
        private void OnEnable()
        {
            var selectable = GetComponent<ISelectable>();
            if (selectable != null)
            {
                // The SelectableFlag component will call SetSelected via ISelectable interface
            }
        }
    }
}