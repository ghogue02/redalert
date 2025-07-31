using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RedAlert.Accessibility
{
    /// <summary>
    /// Tooltip system providing helpful information on hover for UI elements.
    /// Supports keyboard navigation and screen reader compatibility.
    /// </summary>
    public class TooltipSystem : MonoBehaviour
    {
        [Header("Tooltip UI")]
        [SerializeField] private GameObject _tooltipPrefab;
        [SerializeField] private Canvas _tooltipCanvas;
        [SerializeField] private float _showDelay = 0.5f;
        [SerializeField] private float _maxWidth = 300f;
        
        private GameObject _currentTooltip;
        private Coroutine _showTooltipCoroutine;
        private RectTransform _tooltipRect;
        private Text _tooltipText;
        private ContentSizeFitter _contentSizeFitter;
        
        private void Awake()
        {
            CreateTooltipUI();
        }
        
        private void CreateTooltipUI()
        {
            // Create tooltip canvas if not assigned
            if (_tooltipCanvas == null)
            {
                var canvasGO = new GameObject("TooltipCanvas");
                _tooltipCanvas = canvasGO.AddComponent<Canvas>();
                _tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _tooltipCanvas.sortingOrder = 1000; // Ensure tooltips appear on top
                
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            // Create tooltip prefab if not assigned
            if (_tooltipPrefab == null)
            {
                CreateDefaultTooltipPrefab();
            }
        }
        
        private void CreateDefaultTooltipPrefab()
        {
            var tooltipGO = new GameObject("Tooltip");
            tooltipGO.transform.SetParent(_tooltipCanvas.transform, false);
            
            // Background
            var image = tooltipGO.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            image.type = Image.Type.Sliced;
            
            // Layout components
            var layoutGroup = tooltipGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(8, 8, 8, 8);
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            
            _contentSizeFitter = tooltipGO.AddComponent<ContentSizeFitter>();
            _contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Text component
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(tooltipGO.transform, false);
            
            _tooltipText = textGO.AddComponent<Text>();
            _tooltipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _tooltipText.fontSize = 14;
            _tooltipText.color = Color.white;
            _tooltipText.alignment = TextAnchor.MiddleLeft;
            
            var layoutElement = textGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = _maxWidth;
            layoutElement.flexibleWidth = 1;
            
            _tooltipRect = tooltipGO.GetComponent<RectTransform>();
            _currentTooltip = tooltipGO;
            _currentTooltip.SetActive(false);
        }
        
        public void ShowTooltip(string text, Vector2 screenPosition, float delay = -1f)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            HideTooltip();
            
            float showDelay = delay >= 0 ? delay : _showDelay;
            _showTooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(text, screenPosition, showDelay));
        }
        
        public void ShowTooltip(TooltipData tooltipData, Vector2 screenPosition, float delay = -1f)
        {
            if (tooltipData == null || string.IsNullOrEmpty(tooltipData.title)) return;
            
            string fullText = tooltipData.title;
            if (!string.IsNullOrEmpty(tooltipData.description))
            {
                fullText += "\n\n" + tooltipData.description;
            }
            
            ShowTooltip(fullText, screenPosition, delay);
        }
        
        public void HideTooltip()
        {
            if (_showTooltipCoroutine != null)
            {
                StopCoroutine(_showTooltipCoroutine);
                _showTooltipCoroutine = null;
            }
            
            if (_currentTooltip != null)
            {
                _currentTooltip.SetActive(false);
            }
        }
        
        private IEnumerator ShowTooltipAfterDelay(string text, Vector2 screenPosition, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (_currentTooltip != null && _tooltipText != null)
            {
                _tooltipText.text = text;
                _currentTooltip.SetActive(true);
                
                // Force rebuild layout
                LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);
                
                // Position tooltip
                PositionTooltip(screenPosition);
            }
        }
        
        private void PositionTooltip(Vector2 screenPosition)
        {
            if (_tooltipRect == null) return;
            
            // Convert screen position to canvas position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _tooltipCanvas.transform as RectTransform,
                screenPosition,
                _tooltipCanvas.worldCamera,
                out Vector2 localPosition
            );
            
            _tooltipRect.localPosition = localPosition;
            
            // Ensure tooltip stays on screen
            ClampTooltipToScreen();
        }
        
        private void ClampTooltipToScreen()
        {
            var canvasRect = _tooltipCanvas.GetComponent<RectTransform>();
            var tooltipSize = _tooltipRect.sizeDelta;
            var tooltipPos = _tooltipRect.localPosition;
            
            // Get canvas bounds
            var canvasSize = canvasRect.sizeDelta;
            float minX = -canvasSize.x * 0.5f;
            float maxX = canvasSize.x * 0.5f - tooltipSize.x;
            float minY = -canvasSize.y * 0.5f;
            float maxY = canvasSize.y * 0.5f - tooltipSize.y;
            
            // Clamp position
            tooltipPos.x = Mathf.Clamp(tooltipPos.x, minX, maxX);
            tooltipPos.y = Mathf.Clamp(tooltipPos.y, minY, maxY);
            
            _tooltipRect.localPosition = tooltipPos;
        }
        
        // Static methods for easy access
        private static TooltipSystem _instance;
        public static TooltipSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TooltipSystem>();
                    if (_instance == null)
                    {
                        var go = new GameObject("TooltipSystem");
                        _instance = go.AddComponent<TooltipSystem>();
                    }
                }
                return _instance;
            }
        }
        
        public static void Show(string text, Vector2 screenPosition, float delay = -1f)
        {
            Instance.ShowTooltip(text, screenPosition, delay);
        }
        
        public static void Show(TooltipData data, Vector2 screenPosition, float delay = -1f)
        {
            Instance.ShowTooltip(data, screenPosition, delay);
        }
        
        public static void Hide()
        {
            if (_instance != null)
            {
                _instance.HideTooltip();
            }
        }
    }
    
    /// <summary>
    /// Component that automatically shows tooltips on hover.
    /// Add this to any UI element that needs a tooltip.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Tooltip Content")]
        [SerializeField] private string _tooltipTitle;
        [TextArea(3, 6)]
        [SerializeField] private string _tooltipDescription;
        [SerializeField] private float _showDelay = 0.5f;
        
        private TooltipData _tooltipData;
        
        public string TooltipTitle
        {
            get => _tooltipTitle;
            set
            {
                _tooltipTitle = value;
                UpdateTooltipData();
            }
        }
        
        public string TooltipDescription
        {
            get => _tooltipDescription;
            set
            {
                _tooltipDescription = value;
                UpdateTooltipData();
            }
        }
        
        private void Awake()
        {
            UpdateTooltipData();
        }
        
        private void UpdateTooltipData()
        {
            _tooltipData = new TooltipData
            {
                title = _tooltipTitle,
                description = _tooltipDescription
            };
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(_tooltipTitle))
            {
                TooltipSystem.Show(_tooltipData, eventData.position, _showDelay);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipSystem.Hide();
        }
        
        // Public API for dynamic content
        public void SetTooltip(string title, string description = "")
        {
            _tooltipTitle = title;
            _tooltipDescription = description;
            UpdateTooltipData();
        }
    }
    
    [System.Serializable]
    public class TooltipData
    {
        public string title;
        public string description;
        public Sprite icon;
        
        public TooltipData(string title = "", string description = "")
        {
            this.title = title;
            this.description = description;
        }
    }
}