using UnityEngine;
using RedAlert.Units;

namespace RedAlert.Combat
{
    /// <summary>
    /// Allocation-free hitscan using RaycastNonAlloc. Applies damage to Damageable.
    /// </summary>
    public class HitscanWeapon : MonoBehaviour
    {
        [SerializeField] private float _damagePerShot = 10f;
        [SerializeField] private float _maxDistance = 20f;
        [SerializeField] private LayerMask _hitMask = -1;
        [SerializeField] private string _weaponTagVsArmor = "SmallArms"; // used by Damageable multipliers

        private readonly RaycastHit[] _hits = new RaycastHit[4];

        public void Fire(Vector3 targetPoint)
        {
            Vector3 origin = transform.position;
            Vector3 dir = (targetPoint - origin);
            float dist = dir.magnitude;
            if (dist < 0.001f) return;
            if (dist > _maxDistance) dist = _maxDistance;
            dir /= Mathf.Max(0.0001f, dir.magnitude);

            int count = Physics.RaycastNonAlloc(origin, dir, _hits, dist, _hitMask, QueryTriggerInteraction.Ignore);
            if (count <= 0) return;

            // Take first valid Damageable in order
            for (int i = 0; i < count; i++)
            {
                var go = _hits[i].collider.attachedRigidbody ? _hits[i].collider.attachedRigidbody.gameObject : _hits[i].collider.gameObject;
                if (go != null && go.TryGetComponent<Damageable>(out var dmg))
                {
                    dmg.ApplyDamage(_damagePerShot, _weaponTagVsArmor);
                    break;
                }
            }
        }

        // Backward compatibility
        public void DebugFire(Vector3 targetPoint) => Fire(targetPoint);
    }
}