using UnityEngine;

namespace RedAlert.Data
{
    /// <summary>
    /// Identifies the current balance/data version for telemetry and save/version checks.
    /// </summary>
    [CreateAssetMenu(menuName = "RedAlert/Data/BalanceVersion", fileName = "BalanceVersion")]
    public class BalanceVersion : ScriptableObject
    {
        [SerializeField] public string Version = "0.1.0";
        [SerializeField] public string Notes;
    }
}