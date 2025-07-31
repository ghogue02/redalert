using UnityEngine;

namespace RedAlert.Data
{
    /// <summary>
    /// Tag-style classification for armor categories (e.g., Light, Heavy, Structure).
    /// Implement as a ScriptableObject to keep data-driven and easily extensible.
    /// </summary>
    [CreateAssetMenu(menuName = "RedAlert/Data/ArmorTag", fileName = "ArmorTag")]
    public class ArmorTag : ScriptableObject
    {
        [SerializeField] public string Id;
        [SerializeField] public string DisplayName;
    }
}