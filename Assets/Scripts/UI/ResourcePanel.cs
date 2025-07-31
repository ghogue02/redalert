using UnityEngine;
using UnityEngine.UI;

namespace RedAlert.UI
{
    /// <summary>
    /// Resource panel with a simple color pulse on delta.
    /// </summary>
    public class ResourcePanel : MonoBehaviour
    {
        [SerializeField] private Text _crystaliteText;
        [SerializeField] private Color _pulseGain = new Color(0.5f, 1f, 0.5f, 1f);
        [SerializeField] private Color _pulseLoss = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private float _pulseTime = 0.15f;

        private int _lastValue;
        private float _pulseUntil;
        private Color _baseColor;

        private void Awake()
        {
            if (_crystaliteText == null) _crystaliteText = GetComponentInChildren<Text>();
            if (_crystaliteText != null) _baseColor = _crystaliteText.color;
        }

        private void Update()
        {
            if (_crystaliteText == null) return;
            if (Time.unscaledTime > _pulseUntil && _crystaliteText.color != _baseColor)
            {
                _crystaliteText.color = _baseColor; // snap back, minimal and allocation-free
            }
        }

        public void SetValue(int amount)
        {
            if (_crystaliteText == null) return;

            int delta = amount - _lastValue;
            _lastValue = amount;

            _crystaliteText.text = amount.ToString();

            if (delta != 0)
            {
                _crystaliteText.color = delta > 0 ? _pulseGain : _pulseLoss;
                _pulseUntil = Time.unscaledTime + _pulseTime;
            }
        }
    }
}