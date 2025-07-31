using UnityEngine;

namespace RedAlert.Data
{
    /// <summary>
    /// Definition for a weapon used by units or buildings.
    /// Minimal serializable fields to support early compile.
    /// </summary>
    [CreateAssetMenu(menuName = "RedAlert/Data/WeaponDef", fileName = "WeaponDef")]
    public class WeaponDef : ScriptableObject
    {
        [SerializeField] public string Id;
        [SerializeField] public string DisplayName;
        [SerializeField] public float Damage = 10f;
        [SerializeField] public float Range = 6f;
        [SerializeField] public float CooldownSeconds = 1.0f;
        [SerializeField] public ArmorTag PreferredTarget;

        // TODO: Extend with projectile/hitscan data and effects.
    }
}