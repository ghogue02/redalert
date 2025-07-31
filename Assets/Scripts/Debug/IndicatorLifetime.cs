using UnityEngine;
using RedAlert.Core;

namespace RedAlert.Debug
{
    /// <summary>
    /// Minimal pooled indicator lifetime controller.
    /// Requires PoolService in scene. No coroutines to avoid allocs.
    /// Call Arm(seconds) after spawn; Update() counts down and returns to pool.
    /// </summary>
    public class IndicatorLifetime : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.6f;

        private float _t;
        private bool _armed;
        private PoolService _pool;

        private void Awake()
        {
            _pool = FindObjectOfType<PoolService>();
        }

        public void Arm(float seconds)
        {
            _duration = seconds > 0f ? seconds : _duration;
            _t = _duration;
            _armed = true;
            // ensure active visual
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_armed) return;
            _t -= Time.deltaTime;
            if (_t <= 0f)
            {
                _armed = false;
                // Return to pool without Destroy to avoid GC pressure
                if (_pool != null)
                {
                    _pool.Return(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}