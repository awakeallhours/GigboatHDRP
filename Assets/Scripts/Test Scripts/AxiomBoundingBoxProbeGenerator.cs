using UnityEngine;
using System.Collections.Generic;

public static class AxiomBoundingBoxProbeGenerator
{
    public struct ProbeSettings
    {
        public int countX;     // number of probes across beam
        public int countZ;     // number of probes along length
        public float radius;   // probe radius
    }

    public static List<Vector3> Generate(Bounds bounds, ProbeSettings settings)
    {
        var probes = new List<Vector3>();

        // Safety checks
        if (settings.countX < 1 || settings.countZ < 1)
            return probes;

        // Calculate spacing
        float stepX = bounds.size.x / (settings.countX + 1);
        float stepZ = bounds.size.z / (settings.countZ + 1);

        // Generate grid
        for (int ix = 1; ix <= settings.countX; ix++)
        {
            for (int iz = 1; iz <= settings.countZ; iz++)
            {
                float x = bounds.min.x + stepX * ix;
                float z = bounds.min.z + stepZ * iz;

                // Probes sit at the bottom of the bounds (y = min)
                float y = bounds.min.y;

                probes.Add(new Vector3(x, y, z));
            }
        }

        return probes;
    }
}