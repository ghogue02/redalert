using UnityEngine;
using UnityEngine.UI;
using System.Text;
using RedAlert.Core;

namespace RedAlert.UI
{
    /// <summary>
    /// Alerts with debounce/coalesce per Week 3.
    /// UnderAttack: throttle per-source to <= 0.5s
    /// UnitReady: aggregate within 1s (placeholder for future)
    /// InsufficientResources: <= 1 / 1.5s cadence
    /// </summary>
    public class AlertsPanel : MonoBehaviour, RedAlert.Core.UpdateDriver.ISlowTick
    {
        [SerializeField] private Text _logText;

        // Cadences
        [SerializeField] private float _underAttackThrottle = 0.5f;
        [SerializeField] private float _insufficientThrottle = 1.5f;
        [SerializeField] private float _unitReadyWindow = 1.0f;

        private float _lastInsufficientAt;
        private float _unitReadyWindowEnd;
        private int _unitReadyCount;

        // Rolling small dictionary for UnderAttack timestamps without allocations
        private const int MaxAttackSources = 32;
        private GameObject[] _uaKeys = new GameObject[MaxAttackSources];
        private float[] _uaTimes = new float[MaxAttackSources];

        private readonly StringBuilder _sb = new StringBuilder(512);

        private void Awake()
        {
            if (_logText == null) _logText = GetComponentInChildren<Text>();
        }

        private void OnEnable()
        {
            EventBus.OnInsufficientResources += OnInsufficient;
            EventBus.OnUnderAttack += OnUnderAttack;
            UpdateDriver.Register(this);
        }

        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
            EventBus.OnInsufficientResources -= OnInsufficient;
            EventBus.OnUnderAttack -= OnUnderAttack;
        }

        public void SlowTick()
        {
            // Flush UnitReady aggregate if window elapsed
            if (_unitReadyCount > 0 && Time.time >= _unitReadyWindowEnd)
            {
                Append($"{_unitReadyCount} unit(s) ready");
                _unitReadyCount = 0;
            }
        }

        private void Append(string line)
        {
            if (_logText == null) return;
            _sb.Length = 0;
            _sb.Append('[').Append(Time.time.ToString("0.0")).Append("] ").Append(line).Append('\n');
            _logText.text += _sb.ToString();
        }

        private void OnInsufficient(int cost)
        {
            if (Time.time - _lastInsufficientAt < _insufficientThrottle) return;
            _lastInsufficientAt = Time.time;
            Append($"Insufficient Crystalite for cost {cost}");
        }

        private void OnUnderAttack(GameObject target)
        {
            if (target == null)
            {
                // Fallback to global throttle using slot 0
                if (Time.time - _uaTimes[0] < _underAttackThrottle) return;
                _uaTimes[0] = Time.time;
                Append("Under Attack!");
                return;
            }

            int idx = FindOrAllocateKey(target);
            if (idx < 0) idx = 0; // fallback slot if table full

            if (Time.time - _uaTimes[idx] < _underAttackThrottle) return;
            _uaTimes[idx] = Time.time;

            Append("Under Attack!");
        }

        // Placeholder API for unit completion aggregation; call this from production when item completes.
        public void NotifyUnitReady()
        {
            if (_unitReadyCount == 0)
            {
                _unitReadyCount = 1;
                _unitReadyWindowEnd = Time.time + _unitReadyWindow;
            }
            else
            {
                _unitReadyCount++;
            }
        }

        private int FindOrAllocateKey(GameObject k)
        {
            // linear scan small array
            int free = -1;
            for (int i = 0; i < _uaKeys.Length; i++)
            {
                if (_uaKeys[i] == k) return i;
                if (_uaKeys[i] == null && free < 0) free = i;
            }
            if (free >= 0)
            {
                _uaKeys[free] = k;
                _uaTimes[free] = 0f;
                return free;
            }
            return -1;
        }
    }
}