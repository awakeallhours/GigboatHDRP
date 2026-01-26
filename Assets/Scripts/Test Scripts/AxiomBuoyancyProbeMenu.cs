using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class AxiomBuoyancyProbeMenu
{
    // ========================================================================
    // SHARED PROBE INSTANTIATION METHOD
    // ========================================================================
    private static Transform CreateProbeObject(
        Vector3 pos,
        Transform parent,
        float radius,
        List<Transform> probeObjects)
    {
        var probeGO = new GameObject("Probe");
        probeGO.transform.SetParent(parent, false);
        probeGO.transform.position = pos;

        var col = probeGO.AddComponent<SphereCollider>();

        float scale = probeGO.transform.lossyScale.x;
        col.radius = radius / (scale == 0 ? 1 : scale);

        col.isTrigger = true;

        probeGO.layer = LayerMask.NameToLayer("Ignore Raycast");

        probeObjects.Add(probeGO.transform);
        return probeGO.transform;
    }


    // ========================================================================
    // MESH VERSION
    // ========================================================================
    [MenuItem("Axiom/Generate Probes (Mesh)")]
    public static void GenerateMeshProbes()
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

        var hullRenderer = vessel.HullRenderer;
        var meshFilter = hullRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning(
                $"Axiom: Vessel '{vessel.name}' hullRenderer has no MeshFilter/sharedMesh. Cannot generate mesh-based probes."
            );
            return;
        }

        Bounds bounds = vessel.GetHullBounds();

        // ------------------------------------------------------------
        // Validate probe density settings
        // ------------------------------------------------------------
        if (vessel.BeamCount < 1 || vessel.LengthCount < 1)
        {
            Debug.LogWarning(
                $"Axiom: Vessel '{vessel.name}' has invalid probe settings. BeamCount and LengthCount must be ≥ 1."
            );
            return;
        }

        // ------------------------------------------------------------
        // Ensure MeshCollider (reuse if present, otherwise add temporarily)
        // ------------------------------------------------------------
        MeshCollider existingCollider = hullRenderer.GetComponent<MeshCollider>();
        bool addedCollider = false;

        MeshCollider mc = existingCollider;
        if (mc == null)
        {
            mc = hullRenderer.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = meshFilter.sharedMesh;
            mc.convex = false;
            mc.isTrigger = true;
            addedCollider = true;
        }

        // ------------------------------------------------------------
        // Generate mesh-based probe positions
        // ------------------------------------------------------------
        var result = AxiomAutoBuoyancyProbeGenerator.GenerateMeshBased(
            mc,
            bounds,
            vessel.BeamCount,
            vessel.LengthCount,
            vessel
        );

        // ------------------------------------------------------------
        // NEW: Filter keel probes against side probes
        // ------------------------------------------------------------
        FilterKeelProbesAgainstSideProbes(ref result.keelProbes, result.sideProbes);


        // ------------------------------------------------------------
        // Remove temporary MeshCollider if we added it
        // ------------------------------------------------------------
        if (addedCollider)
        {
            Object.DestroyImmediate(mc);
        }

        // ------------------------------------------------------------
        // Clear old probes + rebuild hierarchy
        // ------------------------------------------------------------
        vessel.ClearProbes();
        vessel.EnsureProbeHierarchy();

        var probeObjects = new List<Transform>();

        // ------------------------------------------------------------
        // Instantiate keel probes
        // ------------------------------------------------------------
        foreach (var pos in result.keelProbes)
            CreateProbeObject(pos, vessel.KeelProbeRoot, vessel.ProbeRadius, probeObjects);

        // ------------------------------------------------------------
        // Instantiate side probes
        // ------------------------------------------------------------
        foreach (var pos in result.sideProbes)
            CreateProbeObject(pos, vessel.SideProbeRoot, vessel.ProbeRadius, probeObjects);

        // ------------------------------------------------------------
        // Instantiate deck probes
        // ------------------------------------------------------------
        foreach (var pos in result.deckProbes)
            CreateProbeObject(pos, vessel.DeckProbeRoot, vessel.ProbeRadius, probeObjects);

        // ------------------------------------------------------------
        // Assign probes to vessel
        // ------------------------------------------------------------
        vessel.SetProbeObjects(probeObjects);

        Debug.Log(
            $"Axiom: Generated {probeObjects.Count} MESH-BASED probes for vessel '{vessel.name}' " +
            $"(Keel: {result.keelProbes.Count}, Side: {result.sideProbes.Count}, Deck: {result.deckProbes.Count})."
        );

        EditorUtility.SetDirty(vessel);
    }

    private static void FilterKeelProbesAgainstSideProbes(
    ref List<Vector3> keel,
    List<Vector3> side)
    {
        if (side.Count == 0 || keel.Count == 0)
            return;

        List<Vector3> filtered = new List<Vector3>();

        foreach (var kp in keel)
        {
            float bestDist = float.MaxValue;
            float nearestSideY = float.MaxValue;

            foreach (var sp in side)
            {
                float dz = Mathf.Abs(sp.z - kp.z);
                if (dz < bestDist)
                {
                    bestDist = dz;
                    nearestSideY = sp.y;
                }
            }

            if (kp.y <= nearestSideY)
                filtered.Add(kp);
        }

        keel = filtered;
    }
}