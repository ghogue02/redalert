using UnityEngine;

namespace RedAlert.Data
{
    /// <summary>
    /// Definition for a unit type. Tuned baseline for Tier-1 pacing (90s tech-up).
    /// </summary>
    [CreateAssetMenu(menuName = "RedAlert/Data/UnitDef", fileName = "UnitDef")]
    public class UnitDef : ScriptableObject
    {
        [SerializeField] public string Id;
        [SerializeField] public string DisplayName;
        [SerializeField, Tooltip("HP tuned for ~5–7s TTK vs SmallArms under focus fire")] public float MaxHealth = 140f;
        [SerializeField] public float MoveSpeed = 3.5f;
        [SerializeField] public ArmorTag Armor;

        // TODO: Extend with weapon refs, build cost, roles per architecture.
    }
}