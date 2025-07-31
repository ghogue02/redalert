using UnityEngine;

namespace RedAlert.Combat
{
    /// <summary>
    /// Basic weapon controller with cooldown and range; uses a HitscanWeapon.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private HitscanWeapon _hitscan;
        [SerializeField] private float _range = 12f;
        [SerializeField] private float _cooldownSeconds = 0.5f;

        private float _nextFireTime;

        private void Awake()
        {
            if (_hitscan == null) _hitscan = GetComponentInChildren<HitscanWeapon>();
        }

        public float Range => _range;

        public bool CanFireAt(Vector3 targetPos)
        {
            if (Time.time < _nextFireTime) return false;
            if ((targetPos - transform.position).sqrMagnitude > _range * _range) return false;
            return _hitscan != null;
        }

        public void TryFireAt(Vector3 targetPos)
        {
            if (!CanFireAt(targetPos)) return;
            _hitscan.Fire(targetPos);
            _nextFireTime = Time.time + _cooldownSeconds;
        }
    }
}