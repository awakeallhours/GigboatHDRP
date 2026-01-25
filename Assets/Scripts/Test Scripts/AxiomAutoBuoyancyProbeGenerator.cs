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

    public static ProbeGenerationResult GenerateMeshBased(
    MeshCollider mc,
    Bounds rendererBounds,
    BoundingBoxSettings settings)
    {
        var result = new ProbeGenerationResult
        {
            keelProbes = new List<Vector3>(),
            sideProbes = new List<Vector3>(),
            deckProbes = new List<Vector3>()
        };

        if (settings.beamCount < 1 || settings.lengthCount < 1)
            return result;

        // Use the ACTUAL mesh bounds, not renderer bounds
        Bounds local = mc.sharedMesh.bounds;

        Vector3 worldMin = mc.transform.TransformPoint(local.min);
        Vector3 worldMax = mc.transform.TransformPoint(local.max);

        float beamSpacing = (worldMax.x - worldMin.x) / (settings.beamCount + 1);
        float lengthSpacing = (worldMax.z - worldMin.z) / (settings.lengthCount + 1);

        // Use renderer bounds for Y because they are world-aligned
        float castHeight = rendererBounds.max.y + 5f;
        int mask = 1 << mc.gameObject.layer;

        for (int bx = 1; bx <= settings.beamCount; bx++)
        {
            float x = worldMin.x + beamSpacing * bx;

            for (int lz = 1; lz <= settings.lengthCount; lz++)
            {
                float z = worldMin.z + lengthSpacing * lz;

                Vector3 origin = new Vector3(x, rendererBounds.min.y - 5f, z);


                if (Physics.Raycast(origin, Vector3.up, out RaycastHit hit, 1000f, mask))
                {
                    Vector3 p = hit.point;

                    Debug.DrawLine(origin, hit.point, Color.red, 5f);

                    // Classification unchanged
                    float normalizedHeight = Mathf.InverseLerp(worldMin.y, worldMax.y, p.y);

                    float hullHeight = worldMax.y - worldMin.y;
                    float keelCutoffY = worldMin.y + Mathf.Min(0.5f, hullHeight * 0.1f);

                    if (p.y <= keelCutoffY)
                        result.keelProbes.Add(p);
                    else
                        result.sideProbes.Add(p);
                }
            }
        }

        return result;
    }
}