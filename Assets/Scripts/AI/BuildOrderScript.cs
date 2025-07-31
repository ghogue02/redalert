using System;
using UnityEngine;

namespace RedAlert.AI
{
    /// <summary>
    /// Serialized build order steps for StandardBot. Kept simple and deterministic.
    /// </summary>
    public class BuildOrderScript : MonoBehaviour
    {
        [Serializable]
        public struct Step
        {
            public string Id;              // e.g., "Factory"
            public GameObject Prefab;      // optional reference (for validation/preview)
            public int CostHint;           // cost hint for scheduling
            public float TimeHint;         // seconds hint
            public string DependsOnId;     // optional dependency Id
        }

        [SerializeField] private Step[] _steps;

        public Step[] Steps => _steps;
    }
}