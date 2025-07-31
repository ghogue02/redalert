using UnityEngine;

namespace RedAlert.Combat
{
    /// <summary>
    /// Placeholder projectile with no physics or damage application yet.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 10f;

        private void Update()
        {
            // Placeholder: simple forward translate (visual only).
            transform.position += transform.forward * (_speed * Time.deltaTime);
        }
    }
}