using UnityEngine;

/// <summary>
/// Draws gizmo spheres for the probe objects stored on an
/// <see cref="AxiomBuoyancyVessel"/>. This component is purely visual and
/// does not affect buoyancy behaviour.
/// </summary>
public class AxiomBuoyancyProbeVisualizer : MonoBehaviour
{
    [Header("Vessel Reference")]
    [Tooltip("The vessel whose probe objects will be visualized.")]
    [SerializeField] private AxiomBuoyancyVessel vessel;

    /// <summary>
    /// The vessel whose probes are being visualized.
    /// </summary>
    public AxiomBuoyancyVessel Vessel => vessel;

    [Header("Visual Settings")]
    [Tooltip("Base radius of gizmo spheres before scaling.")]
    [SerializeField] private float baseRadius = 0.1f;

    private void OnDrawGizmos()
    {
        if (vessel == null)
            return;

        var probeObjects = vessel.ProbeObjects;

        if (probeObjects == null || probeObjects.Count == 0)
            return;

        foreach (var probe in probeObjects)
        {
            if (probe == null)
                continue;

            // Determine colour based on parent transform
            if (probe.parent == vessel.KeelProbeRoot)
                Gizmos.color = Color.blue;      // Keel probes
            else if (probe.parent == vessel.SideProbeRoot)
                Gizmos.color = Color.green;     // Side probes
            else if (probe.parent == vessel.DeckProbeRoot)
                Gizmos.color = Color.yellow;    // Deck probes
            else
                Gizmos.color = Color.magenta;   // Unclassified / unexpected

            float scale = probe.lossyScale.x;
            float radius = baseRadius / (scale == 0 ? 1 : scale);

            Gizmos.DrawSphere(probe.position, radius);
        }
    }
}