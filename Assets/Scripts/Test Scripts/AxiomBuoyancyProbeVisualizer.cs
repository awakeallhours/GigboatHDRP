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

    private void OnDrawGizmos()
    {
        if (vessel == null)
            return;

        var probeObjects = vessel.ProbeObjects;
        var settings = vessel.ProbeSettings;

        // Added guard: prevents rare null-ref if probes are cleared in edit mode
        if (probeObjects == null || probeObjects.Count == 0)
            return;

        Gizmos.color = Color.cyan;

        foreach (var probe in probeObjects)
        {
            if (probe != null)
                Gizmos.DrawSphere(probe.position, settings.radius);
        }
    }
}