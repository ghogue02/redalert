using System;
using UnityEngine;

namespace RedAlert.Core
{
    /// <summary>
    /// Lightweight static event hub for Week 2. Avoids allocations; simple C# events only.
    /// Replace with a robust bus later.
    /// </summary>
    public class EventBus : MonoBehaviour
    {
        // Economy
        public static event Action OnNodeDepleted;
        public static event Action<int> OnInsufficientResources; // missing amount or cost

        // Combat
        public static event Action<GameObject> OnUnderAttack; // target GO
        public static event Action<GameObject> OnUnitDeath;   // dead GO

        // Publish helpers (static, inline)
        public static void PublishNodeDepleted() => OnNodeDepleted?.Invoke();
        public static void PublishInsufficientResources(int cost) => OnInsufficientResources?.Invoke(cost);
        public static void PublishUnderAttack(GameObject target) => OnUnderAttack?.Invoke(target);
        public static void PublishUnitDeath(GameObject dead) => OnUnitDeath?.Invoke(dead);

        // Keep a MonoBehaviour to allow presence in scene; not strictly required.
        private void Awake() { }
    }
}