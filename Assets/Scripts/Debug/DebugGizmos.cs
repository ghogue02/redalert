using UnityEngine;

namespace RedAlert.Debug
{
    /// <summary>
    /// Draws simple gizmos for debugging in the Scene view. Minimal placeholder.
    /// </summary>
    public class DebugGizmos : MonoBehaviour
    {
        // TODO: Add toggles and safer drawing guards.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}