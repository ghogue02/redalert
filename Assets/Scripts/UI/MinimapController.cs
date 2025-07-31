using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RedAlert.Core;
using RedAlert.Units;

namespace RedAlert.UI
{
    public class MinimapController : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("UI")]
        [SerializeField] private RectTransform minimapRect;
        [SerializeField] private RectTransform blipParent;
        [SerializeField] private Image blipPrototype; // disabled template; uses atlas sprite
        [SerializeField] private Camera worldCamera;

        [Header("World Bounds")]
        [SerializeField] private Vector2 worldMin = new Vector2(-100, -100);
        [SerializeField] private Vector2 worldMax = new Vector2(100, 100);

        [Header("Input")]
        [SerializeField] private CommandSystem commandSystem;

        private readonly List<IMinimapIconProvider> _providers = new List<IMinimapIconProvider>(256);
        private readonly List<Image> _blips = new List<Image>(256);
        private readonly List<Image> _pool = new List<Image>(256);

        private void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (commandSystem == null) commandSystem = FindObjectOfType<CommandSystem>();
            if (blipPrototype != null) blipPrototype.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            UpdateDriver.Register(this);
        }

        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
        }

        public void Register(IMinimapIconProvider p)
        {
            if (p == null) return;
            if (!_providers.Contains(p)) _providers.Add(p);
        }

        public void Unregister(IMinimapIconProvider p)
        {
            int idx = _providers.IndexOf(p);
            if (idx >= 0)
            {
                _providers[idx] = _providers[_providers.Count - 1];
                _providers.RemoveAt(_providers.Count - 1);
            }
        }

        public void SlowTick()
        {
            // 10 Hz is acceptable: driver is 4 Hz; we refresh per slow tick and allow camera rect per-frame in Update
            RefreshBlips();
        }

        private void Update()
        {
            HandleMouseInput();
            // Camera box draw optional; TODO: add simple rect overlay if needed
        }

        private void HandleMouseInput()
        {
            if (minimapRect == null || worldCamera == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(minimapRect, Input.mousePosition))
                {
                    Vector3 world = ScreenToWorldOnMinimap(Input.mousePosition);
                    PanCameraTo(world);
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(minimapRect, Input.mousePosition))
                {
                    Vector3 world = ScreenToWorldOnMinimap(Input.mousePosition);
                    if (commandSystem != null)
                    {
                        // Reuse latch state indirectly: choose attack-move if A held at the moment
                        if (Input.GetKey(KeyCode.A)) commandSystem.IssueAttackMove(world);
                        else commandSystem.IssueMove(world);
                    }
                }
            }
        }

        private Vector3 ScreenToWorldOnMinimap(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapRect, screenPos, null, out var local);
            var rect = minimapRect.rect;
            float nx = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
            float ny = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);

            float wx = Mathf.Lerp(worldMin.x, worldMax.x, nx);
            float wz = Mathf.Lerp(worldMin.y, worldMax.y, ny);
            return new Vector3(wx, 0f, wz);
        }

        private void PanCameraTo(Vector3 world)
        {
            if (worldCamera == null) return;
            // Basic recenter: maintain current camera height and yaw
            var cam = worldCamera.transform;
            Vector3 target = new Vector3(world.x, cam.position.y, world.z);
            // Clamp to bounds
            target.x = Mathf.Clamp(target.x, worldMin.x, worldMax.x);
            target.z = Mathf.Clamp(target.z, worldMin.y, worldMax.y);
            cam.position = target;
            // TODO: optional easing over 100–200 ms
        }

        private void RefreshBlips()
        {
            // Ensure capacity
            EnsurePool(_providers.Count);

            // Activate needed blips and position them
            int needed = _providers.Count;
            for (int i = 0; i < needed; i++)
            {
                var p = _providers[i];
                if (p == null) continue;
                var img = GetOrCreateBlip(i);
                Vector3 wp = p.GetWorldPosition();
                Vector2 uv = WorldToMinimap(wp);
                SetAnchored(img.rectTransform, uv);
                img.color = p.GetFactionColor();
                img.sprite = p.GetIconShape();
                img.enabled = true;
            }

            // Disable extras
            for (int i = needed; i < _blips.Count; i++)
            {
                if (_blips[i] != null) _blips[i].enabled = false;
            }
        }

        private Vector2 WorldToMinimap(Vector3 world)
        {
            float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, world.x);
            float ny = Mathf.InverseLerp(worldMin.y, worldMax.y, world.z);
            var rect = minimapRect.rect;
            float x = Mathf.Lerp(rect.xMin, rect.xMax, nx);
            float y = Mathf.Lerp(rect.yMin, rect.yMax, ny);
            return new Vector2(x, y);
        }

        private void SetAnchored(RectTransform rt, Vector2 localPoint)
        {
            rt.anchoredPosition = localPoint;
        }

        private void EnsurePool(int count)
        {
            if (blipParent == null || blipPrototype == null) return;
            // Grow pool if necessary
            while (_pool.Count + ActiveCount() < count)
            {
                var img = Instantiate(blipPrototype, blipParent);
                img.gameObject.SetActive(true);
                _pool.Add(img);
            }
        }

        private int ActiveCount()
        {
            int c = 0;
            for (int i = 0; i < _blips.Count; i++)
            {
                if (_blips[i] != null && _blips[i].enabled) c++;
            }
            return c;
        }

        private Image GetOrCreateBlip(int index)
        {
            // Ensure list size
            while (_blips.Count < index + 1) _blips.Add(null);
            if (_blips[index] == null || !_blips[index].gameObject.activeSelf)
            {
                if (_pool.Count > 0)
                {
                    int last = _pool.Count - 1;
                    _blips[index] = _pool[last];
                    _pool.RemoveAt(last);
                }
                else
                {
                    _blips[index] = Instantiate(blipPrototype, blipParent);
                    _blips[index].gameObject.SetActive(true);
                }
            }
            return _blips[index];
        }

        // Static registration entry points for providers
        public static void RegisterProvider(IMinimapIconProvider p)
        {
            var mm = FindObjectOfType<MinimapController>();
            if (mm != null) mm.Register(p);
        }
        public static void UnregisterProvider(IMinimapIconProvider p)
        {
            var mm = FindObjectOfType<MinimapController>();
            if (mm != null) mm.Unregister(p);
        }
    }
}