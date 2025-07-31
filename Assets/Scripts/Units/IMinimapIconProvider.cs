using UnityEngine;
using UnityEngine.UI;

namespace RedAlert.Units
{
    /// <summary>
    /// Providers attach to units/structures to expose minimap info without allocations.
    /// Implementors should Register/Unregister with MinimapController on enable/disable.
    /// </summary>
    public interface IMinimapIconProvider
    {
        Vector3 GetWorldPosition();
        Sprite GetIconShape();
        Color GetFactionColor();
        bool IsStructure();
        bool IsSelected();
    }
}