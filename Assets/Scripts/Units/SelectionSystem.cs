using System.Collections.Generic;
using UnityEngine;

namespace RedAlert.Units
{
    /// <summary>
    /// Week 1: Minimal marquee selection using Physics non-alloc queries on "Units" layer.
    /// Uses a simple ISelectable interface/component to flag selection and optionally set a material index.
    /// Input bridging via Input.GetMouseButton (temporary). TODO(Week2): migrate to Input System action maps.
    /// Performance: avoids GC allocs by caching small arrays and lists.
    /// </summary>
    public class SelectionSystem : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private string unitsLayerName = "Units";
        [SerializeField] private LayerMask groundMask = -1; // Assign to ground/static colliders for ray hit

        [Header("Picking")]
        [SerializeField] private float pickMaxDistance = 1000f;
        [SerializeField] private int maxHits = 8;

        [Header("Marquee")]
        [SerializeField] private float dragThresholdPixels = 4f;
        [Tooltip("UI draw cadence in seconds; leave > 0 to throttle debug/overlay draw updates.")]
        [SerializeField] private float marqueeUiTick = 0.033f; // ~30Hz
        private float _nextUiTick;

        private int _unitsLayer;
        private readonly RaycastHit[] _rayHits = new RaycastHit[8];
        private readonly Collider[] _overlapHits = new Collider[64]; // selection volume sampling
        private Vector2 _mouseDownPos;
        private bool _dragging;
        private Camera _cam;

        private static readonly List<ISelectable> Selected = new List<ISelectable>(64);
        private static readonly List<ISelectable> TempFound = new List<ISelectable>(128);

        private void Awake()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                _cam = FindObjectOfType<Camera>();
            }
            _unitsLayer = LayerMask.NameToLayer(unitsLayerName);
            if (_unitsLayer < 0)
            {
                Debug.LogWarning($"SelectionSystem: Units layer '{unitsLayerName}' not found. Selection will be disabled.");
            }
        }

        private void Update()
        {
            // Temporary bridging input; Week 2 will replace.
            // LMB: click or drag-marquee selection.
            if (Input.GetMouseButtonDown(0))
            {
                _mouseDownPos = Input.mousePosition;
                _dragging = false;
            }

            if (Input.GetMouseButton(0))
            {
                var delta = (Vector2)Input.mousePosition - _mouseDownPos;
                if (!_dragging && delta.sqrMagnitude >= dragThresholdPixels * dragThresholdPixels)
                {
                    _dragging = true;
                }

                if (_dragging && Time.unscaledTime >= _nextUiTick)
                {
                    // Optional: draw marquee rect using an existing UI sprite "selection-rect".
                    // To use: have a dedicated Image under a Screen Space - Overlay canvas, and update its anchored position/size here.
                    // Not implemented to avoid additional HUD setup at Week 1 scope.
                    _nextUiTick = Time.unscaledTime + marqueeUiTick;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (_dragging)
                {
                    PerformMarqueeSelect(_mouseDownPos, (Vector2)Input.mousePosition);
                }
                else
                {
                    PerformSingleSelect((Vector2)Input.mousePosition);
                }
                _dragging = false;
            }
        }

        private void PerformSingleSelect(Vector2 screenPos)
        {
            if (_unitsLayer < 0 || _cam == null) return;

            Ray ray = _cam.ScreenPointToRay(screenPos);
            int hitCount = Physics.RaycastNonAlloc(ray, _rayHits, pickMaxDistance, ~0, QueryTriggerInteraction.Ignore);

            ISelectable found = null;
            for (int i = 0; i < hitCount; i++)
            {
                var go = _rayHits[i].collider.attachedRigidbody ? _rayHits[i].collider.attachedRigidbody.gameObject : _rayHits[i].collider.gameObject;
                if (go.layer == _unitsLayer && go.TryGetComponent<ISelectable>(out var selectable))
                {
                    found = selectable;
                    break;
                }
            }

            ClearSelection();
            if (found != null)
            {
                Select(found);
            }
        }

        private void PerformMarqueeSelect(Vector2 screenA, Vector2 screenB)
        {
            if (_unitsLayer < 0 || _cam == null) return;

            // Build screen rect
            var min = Vector2.Min(screenA, screenB);
            var max = Vector2.Max(screenA, screenB);

            // Sample world frustum by projecting marquee corners to ground plane via raycasts.
            // For robustness without allocations, sample multiple points within the rect and OverlapSphere them.
            // This is a simplified approach: we test colliders within a few screen-sampled rays.
            TempFound.Clear();

            const int sampleGrid = 4; // 4x4 = 16 rays
            for (int y = 0; y < sampleGrid; y++)
            {
                float ty = (y + 0.5f) / sampleGrid;
                for (int x = 0; x < sampleGrid; x++)
                {
                    float tx = (x + 0.5f) / sampleGrid;
                    Vector2 sp = new Vector2(Mathf.Lerp(min.x, max.x, tx), Mathf.Lerp(min.y, max.y, ty));
                    Ray ray = _cam.ScreenPointToRay(sp);
                    int hitCount = Physics.RaycastNonAlloc(ray, _rayHits, pickMaxDistance, groundMask, QueryTriggerInteraction.Ignore);
                    if (hitCount > 0)
                    {
                        Vector3 p = _rayHits[0].point;
                        // Overlap small sphere to find units near this ground point.
                        int count = Physics.OverlapSphereNonAlloc(p, 1.0f, _overlapHits, 1 << _unitsLayer, QueryTriggerInteraction.Ignore);
                        for (int i = 0; i < count; i++)
                        {
                            var go = _overlapHits[i].attachedRigidbody ? _overlapHits[i].attachedRigidbody.gameObject : _overlapHits[i].gameObject;
                            if (go.TryGetComponent<ISelectable>(out var sel))
                            {
                                if (!TempFound.Contains(sel))
                                    TempFound.Add(sel);
                            }
                        }
                    }
                }
            }

            ClearSelection();
            for (int i = 0; i < TempFound.Count; i++)
            {
                Select(TempFound[i]);
            }
        }

        private static void ClearSelection()
        {
            for (int i = 0; i < Selected.Count; i++)
            {
                var s = Selected[i];
                if (s != null) s.SetSelected(false);
            }
            Selected.Clear();
        }

        private const int MaxSelection = 24;

        private static void Select(ISelectable s)
        {
            if (s == null) return;
            if (Selected.Count >= MaxSelection)
            {
                // Optional: alert via EventBus; kept minimal to avoid new features.
                return;
            }
            s.SetSelected(true);
            Selected.Add(s);
        }

        /// <summary>
        /// CommandSystem can query current selection.
        /// </summary>
        public static void GetSelectedNonAlloc(List<ISelectable> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < Selected.Count; i++)
            {
                if (Selected[i] != null) buffer.Add(Selected[i]);
            }
        }
    }

    /// <summary>
    /// Minimal selectable interface. Units implement this to participate in selection.
    /// Typical implementation toggles a material color index, outline, or indicator.
    /// </summary>
    public interface ISelectable
    {
        Transform transform { get; }
        void SetSelected(bool selected);
    }

    /// <summary>
    /// Simple component that implements ISelectable via a renderer material index toggle.
    /// No shader changes; assumes material supports color arrays or uses material index for highlight.
    /// </summary>
    public class SelectableFlag : MonoBehaviour, ISelectable
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private int _highlightMaterialIndex = -1; // -1 to skip index swap
        [SerializeField] private Color _highlightColor = Color.cyan;
        [SerializeField] private string _colorProperty = "_BaseColor"; // URP Lit default

        private MaterialPropertyBlock _mpb;
        private bool _isSelected;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        public void SetSelected(bool selected)
        {
            if (_renderer == null) return;
            if (_isSelected == selected) return;
            _isSelected = selected;

            // Apply simple color tint via MPB to avoid material instantiation.
            _renderer.GetPropertyBlock(_mpb, _highlightMaterialIndex);
            _mpb.SetColor(_colorProperty, selected ? _highlightColor : Color.white);
            _renderer.SetPropertyBlock(_mpb, _highlightMaterialIndex);
        }
    }
}