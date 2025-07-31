using UnityEngine;

namespace RedAlert.Data
{
    /// <summary>
    /// Defines the color palette used by a faction for units, UI accents, and minimap.
    /// </summary>
    [CreateAssetMenu(menuName = "RedAlert/Data/FactionPalette", fileName = "FactionPalette")]
    public class FactionPalette : ScriptableObject
    {
        [SerializeField] public Color Primary = Color.red;
        [SerializeField] public Color Secondary = Color.black;
        [SerializeField] public Color Accent = Color.white;
    }
}