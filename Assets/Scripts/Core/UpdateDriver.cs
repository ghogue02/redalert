using System.Collections.Generic;
using UnityEngine;

namespace RedAlert.Core
{
    /// <summary>
    /// Central MonoBehaviour with low-frequency 4 Hz tick for economy/production/targeting.
    /// </summary>
    public class UpdateDriver : MonoBehaviour
    {
        public interface ISlowTick
        {
            // Called at ~4 Hz
            void SlowTick();
        }

        private static readonly List<ISlowTick> _slow = new List<ISlowTick>(64);
        private float _nextSlow;
        [SerializeField] private float _slowInterval = 0.25f; // 4 Hz

        public static void Register(ISlowTick t)
        {
            if (t == null) return;
            if (!_slow.Contains(t)) _slow.Add(t);
        }

        public static void Unregister(ISlowTick t)
        {
            if (t == null) return;
            int idx = _slow.IndexOf(t);
            if (idx >= 0)
            {
                int last = _slow.Count - 1;
                _slow[idx] = _slow[last];
                _slow.RemoveAt(last);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextSlow)
            {
                _nextSlow = Time.unscaledTime + _slowInterval;
                for (int i = 0; i < _slow.Count; i++)
                {
                    var t = _slow[i];
                    if (t != null) t.SlowTick();
                }
            }
        }
    }
}