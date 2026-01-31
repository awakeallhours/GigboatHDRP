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

        if (keelRowHeights.Count == 0)
            return result;

        float keelTopY = keelRowHeights.Values.Max();

        // =================================================================
        // DECK DETECTION (DOWNWARD RAYS)
        // =================================================================
        float deckCutoffY = vessel.AutoDeckDetection
            ? DetectDeckHeight(mc, rendererBounds, beamCount, lengthCount, mask, keelTopY, vessel)
            : vessel.ManualDeckHeight;

        if (deckCutoffY <= keelTopY)
            return result;

        // =================================================================
        // SIDE + DECK PROBES
        // =================================================================
        GenerateSideAndDeckProbes(
            mc,
            rendererBounds,
            lengthCount,
            vessel,
            keelTopY,
            deckCutoffY,
            out result.sideProbes,
            out result.deckProbes
        );

        return result;
    }

    // =====================================================================
    // DECK DETECTION (DOWNWARD RAYS)
    // =====================================================================
    private static float DetectDeckHeight(
        MeshCollider mc,
        Bounds rendererBounds,
        int beamCount,
        int lengthCount,
        int mask,
        float keelTopY,
        AxiomBuoyancyVessel vessel)
    {
        Bounds local = mc.sharedMesh.bounds;
        Vector3 colliderMin = mc.transform.TransformPoint(local.min);
        Vector3 colliderMax = mc.transform.TransformPoint(local.max);

        float hullWidth = colliderMax.x - colliderMin.x;
        float hullLength = colliderMax.z - colliderMin.z;

        float minY = rendererBounds.min.y;
        float maxY = rendererBounds.max.y;
        float hullHeight = maxY - minY;

        float beamSpacing = hullWidth / (beamCount + 1);
        float lengthSpacing = hullLength / (lengthCount + 1);

        List<float> deckSamples = new List<float>();

        float rayStartY = maxY + hullHeight * 0.5f;
        float rayDistance = hullHeight * 2f;
        float ignoreBand = Mathf.Max(0.5f, hullHeight * 0.05f);

        for (int bx = 1; bx <= beamCount; bx++)
        {
            float x = colliderMin.x + beamSpacing * bx;

            for (int lz = 1; lz <= lengthCount; lz++)
            {
                float z = colliderMin.z + lengthSpacing * lz;

                Vector3 originTop = new Vector3(x, rayStartY, z);

                if (!Physics.Raycast(originTop, Vector3.down, out RaycastHit topHit, rayDistance, mask))
                    continue;

                if (!topHit.collider.transform.IsChildOf(mc.transform))
                    continue;

                float topY = topHit.point.y;

                float secondStartY = topY - ignoreBand;
                if (secondStartY <= keelTopY)
                {
                    deckSamples.Add(topY);
                    continue;
                }

                Vector3 originSecond = new Vector3(x, secondStartY, z);
                float secondDistance = secondStartY - (keelTopY - 1f);

                if (Physics.Raycast(originSecond, Vector3.down, out RaycastHit deckHit, secondDistance, mask) &&
                    deckHit.collider.transform.IsChildOf(mc.transform))
                {
                    deckSamples.Add(deckHit.point.y);
                }
                else
                {
                    deckSamples.Add(topY);
                }
            }
        }

        if (deckSamples.Count == 0)
            return Mathf.Lerp(keelTopY, maxY, 0.7f);

        deckSamples.Sort();

        float deckY =
            vessel.DeckMode == DeckDetectionMode.Average
            ? deckSamples.Average()
            : (deckSamples.Count % 2 == 1
                ? deckSamples[deckSamples.Count / 2]
                : 0.5f * (deckSamples[deckSamples.Count / 2 - 1] + deckSamples[deckSamples.Count / 2]));

        deckY = Mathf.Max(deckY, keelTopY + hullHeight * 0.1f);
        deckY = Mathf.Min(deckY, maxY);

        return deckY;
    }

    // =====================================================================
    // SIDE + DECK PROBES
    // =====================================================================
    private static void GenerateSideAndDeckProbes(
        MeshCollider mc,
        Bounds rendererBounds,
        int lengthCount,
        AxiomBuoyancyVessel vessel,
        float keelTopY,
        float deckCutoffY,
        out List<Vector3> sideProbes,
        out List<Vector3> deckProbes)
    {
        sideProbes = new List<Vector3>();
        deckProbes = new List<Vector3>();

        Bounds local = mc.sharedMesh.bounds;
        Vector3 colliderMin = mc.transform.TransformPoint(local.min);
        Vector3 colliderMax = mc.transform.TransformPoint(local.max);

        float hullWidth = colliderMax.x - colliderMin.x;

        float minY = rendererBounds.min.y;
        float maxY = rendererBounds.max.y;
        float hullHeight = maxY - minY;

        // ------------------------------------------------------------
        // SIDE ROW HEIGHTS
        // ------------------------------------------------------------
        List<float> sideRowHeights = new List<float>();

        if (vessel.OverrideSideLayers)
        {
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
            float spacing = vessel.VerticalLayerResolution;
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

                Vector3 portOrigin = new Vector3(colliderMin.x - sideOffset, rowY, z);

                if (Physics.Raycast(portOrigin, inwardPort, out RaycastHit hitP, maxRaycastDistance, mask))
                {
                    if (hitP.collider.transform.IsChildOf(mc.transform) &&
                        hitP.point.y <= deckCutoffY)
                        sideProbes.Add(hitP.point);
                }

                Vector3 starOrigin = new Vector3(colliderMax.x + sideOffset, rowY, z);

                if (Physics.Raycast(starOrigin, inwardStar, out RaycastHit hitS, maxRaycastDistance, mask))
                {
                    if (hitS.collider.transform.IsChildOf(mc.transform) &&
                        hitS.point.y <= deckCutoffY)
                        sideProbes.Add(hitS.point);
                }
            }
        }

        // ------------------------------------------------------------
        // DECK PROBES (one row at deck cutoff)
        // ------------------------------------------------------------
        float deckRowY = deckCutoffY;

        for (int lz = 1; lz <= lengthCount; lz++)
        {
            float z = colliderMin.z + lengthSpacing * lz;

            Vector3 portOrigin = new Vector3(colliderMin.x - sideOffset, deckRowY, z);

            if (Physics.Raycast(portOrigin, inwardPort, out RaycastHit hitP, maxRaycastDistance, mask))
            {
                if (hitP.collider.transform.IsChildOf(mc.transform))
                    deckProbes.Add(hitP.point);
            }

            Vector3 starOrigin = new Vector3(colliderMax.x + sideOffset, deckRowY, z);

            if (Physics.Raycast(starOrigin, inwardStar, out RaycastHit hitS, maxRaycastDistance, mask))
            {
                if (hitS.collider.transform.IsChildOf(mc.transform))
                    deckProbes.Add(hitS.point);
            }
        }
    }
}