using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pure utility class responsible for generating buoyancy probe positions
/// from a vessel's bounding box. Produces a deterministic 3D probe lattice:
///
/// • Keel layer      – baseline buoyancy sampling
/// • Side layer      – vertical offset up the hull
/// • Beam‑offset     – horizontal offset toward port/starboard
///
/// This class contains no Unity scene dependencies and performs no allocation
/// outside the returned list. All behaviour is explicit and parameter‑driven.
/// </summary>
public static class AxiomAutoBuoyancyProbeGenerator
{
    [System.Serializable]
    public struct BoundingBoxSettings
    {
        public int beamCount;
        public int lengthCount;
        public float radius;
        public float sideHeightFraction;
        public float sideBeamFraction;
    }

    public static List<Vector3> GenerateBoundingBox(Bounds bounds, BoundingBoxSettings settings)
    {
        var probes = new List<Vector3>();

        if (settings.beamCount < 1 || settings.lengthCount < 1)
            return probes;

        float beamSpacing = bounds.size.x / (settings.beamCount + 1);
        float lengthSpacing = bounds.size.z / (settings.lengthCount + 1);

        float keelY = bounds.min.y;
        float deckY = bounds.max.y;
        float hullHeight = deckY - keelY;

        float sideY = keelY + hullHeight * Mathf.Clamp01(settings.sideHeightFraction);

        float halfBeam = bounds.size.x * 0.5f;
        float sideXOffset = halfBeam * Mathf.Clamp01(settings.sideBeamFraction);
        float centerX = bounds.center.x;

        // ------------------------------------------------------------
        // Keel layer (baseline)
        // ------------------------------------------------------------
        for (int bx = 1; bx <= settings.beamCount; bx++)
        {
            float x = bounds.min.x + beamSpacing * bx;

            for (int lz = 1; lz <= settings.lengthCount; lz++)
            {
                float z = bounds.min.z + lengthSpacing * lz;
                probes.Add(new Vector3(x, keelY, z));
            }
        }

        // ------------------------------------------------------------
        // Vertical side layer (same X/Z grid, raised Y)
        // ------------------------------------------------------------
        for (int bx = 1; bx <= settings.beamCount; bx++)
        {
            float x = bounds.min.x + beamSpacing * bx;

            for (int lz = 1; lz <= settings.lengthCount; lz++)
            {
                float z = bounds.min.z + lengthSpacing * lz;
                probes.Add(new Vector3(x, sideY, z));
            }
        }

        // ------------------------------------------------------------
        // Horizontal beam‑offset layer (port + starboard)
        // NOW MOVED UP TO sideY
        // ------------------------------------------------------------
        for (int lz = 1; lz <= settings.lengthCount; lz++)
        {
            float z = bounds.min.z + lengthSpacing * lz;

            probes.Add(new Vector3(centerX - sideXOffset, sideY, z)); // moved up
            probes.Add(new Vector3(centerX + sideXOffset, sideY, z)); // moved up
        }

        return probes;
    }
}