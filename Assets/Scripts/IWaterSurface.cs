using UnityEngine;
public interface IWaterSurface
{
    /// <summary>
    /// Returns true if a water surface exists at this point.
    /// </summary>
    bool TryGetHeightAndNormal(
        Vector3 worldPosition,
        out float waterHeight,
        out Vector3 waterNormal
    );
}
