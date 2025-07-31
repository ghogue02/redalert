using UnityEngine;

namespace RedAlert.Data
{
    /// <summary>
    /// Definition for a Crystalite resource node type and yield parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "RedAlert/Data/CrystaliteNodeDef", fileName = "CrystaliteNodeDef")]
    public class CrystaliteNodeDef : ScriptableObject
    {
        [SerializeField] public string Id;
        [SerializeField] public int InitialAmount = 5000;
        [SerializeField] public float HarvestRatePerSecond = 5f;
        [SerializeField] public Color MinimapColor = new Color(0.2f, 0.9f, 1f, 1f);
    }
}