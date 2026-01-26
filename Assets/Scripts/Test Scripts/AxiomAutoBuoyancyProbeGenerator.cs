using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Analytics;

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

        float keelCutoffY = minY + Mathf.Min(0.5f, hullHeight * 0.25f);

        for (int bx = 1; bx <= beamCount; bx++)
        {
            float x = colliderMin.x + beamSpacing * bx;

            for (int lz = 1; lz <= lengthCount; lz++)
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
            out result.sideProbes,
            out result.deckProbes
        );

        return result;
    }

    // =====================================================================
    // SIDE + DECK PROBES
    // =====================================================================
    private static void GenerateSideAndDeckProbes(
        MeshCollider mc,
        Bounds rendererBounds,
        int lengthCount,
        AxiomBuoyancyVessel vessel,
        out List<Vector3> sideProbes,
        out List<Vector3> deckProbes)
    {
        sideProbes = new List<Vector3>();
        deckProbes = new List<Vector3>();

        // ------------------------------------------------------------
        // COLLIDER BOUNDS FOR X/Z
        // ------------------------------------------------------------
        Bounds local = mc.sharedMesh.bounds;
        Vector3 colliderMin = mc.transform.TransformPoint(local.min);
        Vector3 colliderMax = mc.transform.TransformPoint(local.max);

        float hullWidth = colliderMax.x - colliderMin.x;

        // ------------------------------------------------------------
        // RENDERER BOUNDS FOR Y
        // ------------------------------------------------------------
        float minY = rendererBounds.min.y;
        float maxY = rendererBounds.max.y;
        float hullHeight = maxY - minY;

        // ------------------------------------------------------------
        // VERTICAL LAYERS (AUTO + MANUAL OVERRIDE)
        // ------------------------------------------------------------
        int verticalLayers;

        if (vessel.OverrideSideLayers)
        {
            verticalLayers = Mathf.Max(1, vessel.ManualSideLayers);
        }
        else
        {
            float desiredVerticalResolution = 0.75f;
            float scaleFactor = mc.transform.lossyScale.y;
            float scaledResolution = desiredVerticalResolution * scaleFactor;

            verticalLayers = Mathf.Clamp(
                Mathf.RoundToInt(hullHeight / scaledResolution),
                1, 6
            );
        }

        float topFrac = .40f;
        float bottomFrac = 0.05f;

        float deckCutoffY = maxY - Mathf.Min(0.03f, hullHeight * 0.1f);

        int mask = 1 << mc.gameObject.layer;

        float lengthSpacing = (colliderMax.z - colliderMin.z) / (lengthCount + 1);

        float maxRaycastDistance = hullWidth * 3f;
        float sideOffset = hullWidth * 0.25f;

        Vector3 inwardPort = mc.transform.right;
        Vector3 inwardStar = -mc.transform.right;

        // ------------------------------------------------------------
        // MAIN LOOP
        // ------------------------------------------------------------
        for (int v = 0; v < verticalLayers; v++)
        {
            float t = (verticalLayers == 1) ? 0.5f : (float)v / (verticalLayers - 1);
            float frac = Mathf.Lerp(topFrac, bottomFrac, t); // top → bottom

            float y = Mathf.Lerp(minY, maxY, frac);

            for (int lz = 1; lz <= lengthCount; lz++)
            {
                float z = colliderMin.z + lengthSpacing * lz;

                // PORT
                Vector3 portOrigin = new Vector3(colliderMin.x - sideOffset, y, z);

                if (Physics.Raycast(portOrigin, inwardPort, out RaycastHit hitP, maxRaycastDistance, mask))
                {
                    if (hitP.collider != null && hitP.collider.transform.IsChildOf(mc.transform))
                    {
                        if (hitP.point.y >= deckCutoffY)
                        
                            deckProbes.Add(hitP.point);
                        else                                            
                            sideProbes.Add(hitP.point);
                    }
                }

                // STARBOARD
                Vector3 starOrigin = new Vector3(colliderMax.x + sideOffset, y, z);

                if (Physics.Raycast(starOrigin, inwardStar, out RaycastHit hitS, maxRaycastDistance, mask))
                {
                    if (hitS.collider != null && hitS.collider.transform.IsChildOf(mc.transform))
                    {
                        if (hitS.point.y >= deckCutoffY)
                            
                            deckProbes.Add(hitS.point);
                        else                                        
                            sideProbes.Add(hitS.point);
                    }
                }
            }
        }
    }
}