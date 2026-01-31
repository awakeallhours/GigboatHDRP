using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class AxiomAutoBuoyancyProbeGenerator
{
    public struct ProbeGenerationResult
    {
        public List<Vector3> keelProbes;
        public List<Vector3> sideProbes;
        public List<Vector3> deckProbes;
    }

    // =====================================================================
    // PUBLIC ENTRY POINT
    // =====================================================================
    public static ProbeGenerationResult GenerateMeshBased(
        MeshCollider mc,
        Bounds rendererBounds,
        int beamCount,
        int lengthCount,
        AxiomBuoyancyVessel vessel)
    {
        var result = new ProbeGenerationResult
        {
            keelProbes = new List<Vector3>(),
            sideProbes = new List<Vector3>(),
            deckProbes = new List<Vector3>()
        };

        if (mc == null || mc.sharedMesh == null)
            return result;

        if (beamCount < 1 || lengthCount < 1)
            return result;

        // ------------------------------------------------------------
        // COLLIDER BOUNDS FOR X/Z
        // ------------------------------------------------------------
        Bounds local = mc.sharedMesh.bounds;
        Vector3 colliderMin = mc.transform.TransformPoint(local.min);
        Vector3 colliderMax = mc.transform.TransformPoint(local.max);

        // ------------------------------------------------------------
        // RENDERER BOUNDS FOR Y
        // ------------------------------------------------------------
        float minY = rendererBounds.min.y;
        float maxY = rendererBounds.max.y;
        float hullHeight = maxY - minY;

        int mask = 1 << mc.gameObject.layer;

        // =================================================================
        // KEEL PROBES (UPWARD RAYS)
        // =================================================================
        float beamSpacing = (colliderMax.x - colliderMin.x) / (beamCount + 1);
        float lengthSpacing = (colliderMax.z - colliderMin.z) / (lengthCount + 1);

        float keelCutoffY = minY + hullHeight * vessel.KeelCutoffHeightFraction;

        // Track keel row heights by Z index
        Dictionary<int, float> keelRowHeights = new Dictionary<int, float>();

        for (int bx = 1; bx <= beamCount; bx++)
        {
            float x = colliderMin.x + beamSpacing * bx;

            for (int lz = 0; lz <= lengthCount + 1; lz++)
            {
                float z = colliderMin.z + lengthSpacing * lz;

                Vector3 origin = new Vector3(x, rendererBounds.min.y - 5f, z);

                if (Physics.Raycast(origin, Vector3.up, out RaycastHit hit, 1000f, mask))
                {
                    if (hit.collider != null &&
                        hit.collider.transform.IsChildOf(mc.transform) &&
                        hit.point.y <= keelCutoffY)
                    {
                        result.keelProbes.Add(hit.point);

                        if (!keelRowHeights.ContainsKey(lz))
                            keelRowHeights[lz] = hit.point.y;
                    }
                }
            }
        }

        // =================================================================
        // SIDE + DECK PROBES
        // =================================================================
        GenerateSideAndDeckProbes(
            mc,
            rendererBounds,
            lengthCount,
            vessel,
            keelRowHeights,
            out result.sideProbes,
            out result.deckProbes
        );

        return result;
    }

    // =====================================================================
    // SIDE + DECK PROBES (AUTO + OVERRIDE + SINGLE DECK ROW)
    // =====================================================================
    private static void GenerateSideAndDeckProbes(
        MeshCollider mc,
        Bounds rendererBounds,
        int lengthCount,
        AxiomBuoyancyVessel vessel,
        Dictionary<int, float> keelRowHeights,
        out List<Vector3> sideProbes,
        out List<Vector3> deckProbes)
    {
        sideProbes = new List<Vector3>();
        deckProbes = new List<Vector3>();

        if (keelRowHeights.Count == 0)
            return;

        // ------------------------------------------------------------
        // BOUNDS
        // ------------------------------------------------------------
        Bounds local = mc.sharedMesh.bounds;
        Vector3 colliderMin = mc.transform.TransformPoint(local.min);
        Vector3 colliderMax = mc.transform.TransformPoint(local.max);

        float hullWidth = colliderMax.x - colliderMin.x;

        float minY = rendererBounds.min.y;
        float maxY = rendererBounds.max.y;
        float hullHeight = maxY - minY;

        // ------------------------------------------------------------
        // DECK CUTOFF
        // ------------------------------------------------------------
        float deckCutoffY = maxY - Mathf.Min(
            vessel.DeckRegionAbsolute,
            hullHeight * vessel.DeckRegionHeightFraction
        );

        // ------------------------------------------------------------
        // KEEL TOP
        // ------------------------------------------------------------
        float keelTopY = keelRowHeights.Values.Max();

        if (deckCutoffY <= keelTopY)
            return;

        // ------------------------------------------------------------
        // SIDE ROW HEIGHTS
        // ------------------------------------------------------------
        List<float> sideRowHeights = new List<float>();

        if (vessel.OverrideSideLayers)
        {
            // MANUAL MODE — evenly spaced rows
            int N = Mathf.Max(1, vessel.ManualSideLayers);

            for (int i = 0; i < N; i++)
            {
                float t = (i + 1f) / (N + 1f);
                float y = Mathf.Lerp(keelTopY, deckCutoffY, t);
                sideRowHeights.Add(y);
            }
        }
        else
        {
            // AUTO MODE — spacing based on hull height
            float spacing = hullHeight * 0.05f; // 5% of hull height

            float y = keelTopY + spacing;
            int safety = 0;

            while (y < deckCutoffY && safety < 200)
            {
                sideRowHeights.Add(y);
                y += spacing;
                safety++;
            }
        }

        // ------------------------------------------------------------
        // DECK ROW HEIGHT (always exactly one)
        // ------------------------------------------------------------
        float deckRowY = deckCutoffY;

        // ------------------------------------------------------------
        // RAYCAST SETUP
        // ------------------------------------------------------------
        int mask = 1 << mc.gameObject.layer;
        float lengthSpacing = (colliderMax.z - colliderMin.z) / (lengthCount + 1);
        float maxRaycastDistance = hullWidth * vessel.SideRaycastDistanceMultiplier;
        float sideOffset = hullWidth * vessel.SideOffsetWidthFraction;

        Vector3 inwardPort = mc.transform.right;
        Vector3 inwardStar = -mc.transform.right;

        // ------------------------------------------------------------
        // SIDE PROBES
        // ------------------------------------------------------------
        foreach (float rowY in sideRowHeights)
        {
            for (int lz = 1; lz <= lengthCount; lz++)
            {
                float z = colliderMin.z + lengthSpacing * lz;

                // PORT
                Vector3 portOrigin = new Vector3(colliderMin.x - sideOffset, rowY, z);

                if (Physics.Raycast(portOrigin, inwardPort, out RaycastHit hitP, maxRaycastDistance, mask))
                {
                    if (hitP.collider != null && hitP.collider.transform.IsChildOf(mc.transform))
                        sideProbes.Add(hitP.point);
                }

                // STARBOARD
                Vector3 starOrigin = new Vector3(colliderMax.x + sideOffset, rowY, z);

                if (Physics.Raycast(starOrigin, inwardStar, out RaycastHit hitS, maxRaycastDistance, mask))
                {
                    if (hitS.collider != null && hitS.collider.transform.IsChildOf(mc.transform))
                        sideProbes.Add(hitS.point);
                }
            }
        }

        // ------------------------------------------------------------
        // DECK PROBES (one row at deck cutoff)
        // ------------------------------------------------------------
        for (int lz = 1; lz <= lengthCount; lz++)
        {
            float z = colliderMin.z + lengthSpacing * lz;

            // PORT
            Vector3 portOrigin = new Vector3(colliderMin.x - sideOffset, deckRowY, z);

            if (Physics.Raycast(portOrigin, inwardPort, out RaycastHit hitP, maxRaycastDistance, mask))
            {
                if (hitP.collider != null && hitP.collider.transform.IsChildOf(mc.transform))
                    deckProbes.Add(hitP.point);
            }

            // STARBOARD
            Vector3 starOrigin = new Vector3(colliderMax.x + sideOffset, deckRowY, z);

            if (Physics.Raycast(starOrigin, inwardStar, out RaycastHit hitS, maxRaycastDistance, mask))
            {
                if (hitS.collider != null && hitS.collider.transform.IsChildOf(mc.transform))
                    deckProbes.Add(hitS.point);
            }
        }
    }
}