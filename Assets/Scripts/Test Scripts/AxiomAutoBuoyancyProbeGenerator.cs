using UnityEngine;
using System.Collections.Generic;

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

    public struct ProbeGenerationResult
    {
        public List<Vector3> keelProbes;
        public List<Vector3> sideProbes;
        public List<Vector3> deckProbes; // optional, currently unused
    }

    public static ProbeGenerationResult GenerateBoundingBox(Bounds bounds, BoundingBoxSettings settings)
    {
        var result = new ProbeGenerationResult
        {
            keelProbes = new List<Vector3>(),
            sideProbes = new List<Vector3>(),
            deckProbes = new List<Vector3>() // reserved for future use
        };

        if (settings.beamCount < 1 || settings.lengthCount < 1)
            return result;

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
        // Keel layer
        // ------------------------------------------------------------
        for (int bx = 1; bx <= settings.beamCount; bx++)
        {
            float x = bounds.min.x + beamSpacing * bx;

            for (int lz = 1; lz <= settings.lengthCount; lz++)
            {
                float z = bounds.min.z + lengthSpacing * lz;
                result.keelProbes.Add(new Vector3(x, keelY, z));
            }
        }

        // ------------------------------------------------------------
        // Vertical side layer
        // ------------------------------------------------------------
        for (int bx = 1; bx <= settings.beamCount; bx++)
        {
            float x = bounds.min.x + beamSpacing * bx;

            for (int lz = 1; lz <= settings.lengthCount; lz++)
            {
                float z = bounds.min.z + lengthSpacing * lz;
                result.sideProbes.Add(new Vector3(x, sideY, z));
            }
        }

        // ------------------------------------------------------------
        // Horizontal beam‑offset layer (port + starboard)
        // ------------------------------------------------------------
        for (int lz = 1; lz <= settings.lengthCount; lz++)
        {
            float z = bounds.min.z + lengthSpacing * lz;

            result.sideProbes.Add(new Vector3(centerX - sideXOffset, sideY, z));
            result.sideProbes.Add(new Vector3(centerX + sideXOffset, sideY, z));
        }

        return result;
    }
}