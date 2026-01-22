using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Editor menu utilities for generating buoyancy probes for a selected vessel.
/// This class invokes
/// <see cref="AxiomAutoBuoyancyProbeGenerator.GenerateBoundingBox(Bounds, AxiomAutoBuoyancyProbeGenerator.BoundingBoxSettings)"/>
/// and instantiates probe GameObjects under the vessel's probe hierarchy.
/// </summary>
public static class AxiomBuoyancyProbeMenu
{
    [MenuItem("Axiom/Generate Probes (Bounding Box)")]
    public static void GenerateBoundingBoxProbes()
    {
        // ------------------------------------------------------------
        // Validate selection
        // ------------------------------------------------------------
        var vessel = Selection.activeGameObject?.GetComponent<AxiomBuoyancyVessel>();
        if (vessel == null)
        {
            Debug.LogWarning("Axiom: Select a GameObject containing an AxiomBuoyancyVessel component.");
            return;
        }

        // ------------------------------------------------------------
        // Validate hull reference
        // ------------------------------------------------------------
        if (!vessel.HasValidHull)
        {
            Debug.LogWarning(
                $"Axiom: Vessel '{vessel.name}' has no hullRenderer assigned. Cannot generate probes."
            );
            return;
        }

        Bounds bounds = vessel.GetHullBounds();

        // ------------------------------------------------------------
        // Validate probe settings
        // ------------------------------------------------------------
        var settings = vessel.ProbeSettings;
        if (settings.beamCount < 1 || settings.lengthCount < 1)
        {
            Debug.LogWarning(
                $"Axiom: Vessel '{vessel.name}' has invalid probe settings. BeamCount and LengthCount must be ≥ 1."
            );
            return;
        }

        // ------------------------------------------------------------
        // Generate probe positions
        // ------------------------------------------------------------
        var positions = AxiomAutoBuoyancyProbeGenerator.GenerateBoundingBox(bounds, settings);

        // ------------------------------------------------------------
        // Clear old probes + rebuild hierarchy
        // ------------------------------------------------------------
        vessel.ClearProbes();
        vessel.EnsureProbeHierarchy();

        var probeObjects = new List<Transform>();

        // ------------------------------------------------------------
        // Instantiate probe GameObjects
        // ------------------------------------------------------------
        foreach (var pos in positions)
        {
            // Decide parent based on Y height
            Transform parent =
                Mathf.Approximately(pos.y, bounds.min.y)
                ? vessel.KeelProbeRoot
                : vessel.SideProbeRoot;

            var probeGO = new GameObject("Probe");
            probeGO.transform.SetParent(parent, false);
            probeGO.transform.position = pos;

            // Add tiny trigger collider for Scene selection
            var col = probeGO.AddComponent<SphereCollider>();
            col.radius = 0.05f;
            col.isTrigger = true;

            // Put on Ignore Raycast layer to avoid gameplay interference
            probeGO.layer = LayerMask.NameToLayer("Ignore Raycast");

            probeObjects.Add(probeGO.transform);
        }

        // ------------------------------------------------------------
        // Assign probes to vessel
        // ------------------------------------------------------------
        vessel.SetProbeObjects(probeObjects);

        Debug.Log($"Axiom: Generated {probeObjects.Count} probe objects for vessel '{vessel.name}'.");
    }
}