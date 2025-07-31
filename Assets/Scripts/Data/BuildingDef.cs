using UnityEngine;

namespace RedAlert.Data
{
    /// <summary>
    /// Definition for a building type with minimal serializable fields.
    /// </summary>
    [CreateAssetMenu(menuName = "RedAlert/Data/BuildingDef", fileName = "BuildingDef")]
    public class BuildingDef : ScriptableObject
    {
        [SerializeField] public string Id;
        [SerializeField] public string DisplayName;
        [SerializeField] public float MaxHealth = 500f;
        [SerializeField] public Vector2Int Footprint = new Vector2Int(2, 2);

        // TODO: Extend with placement categories, power usage, prerequisites.
    }
}