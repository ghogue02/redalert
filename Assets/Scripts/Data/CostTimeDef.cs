using UnityEngine;

namespace RedAlert.Data
{
    /// <summary>
    /// Minimal cost and build time definition for constructing units/buildings.
    /// </summary>
    [CreateAssetMenu(menuName = "RedAlert/Data/CostTimeDef", fileName = "CostTimeDef")]
    public class CostTimeDef : ScriptableObject
    {
        [SerializeField] public int CrystaliteCost = 100;
        [SerializeField] public float BuildTimeSeconds = 10f;
    }
}