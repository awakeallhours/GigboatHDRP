using System.Collections.Generic;
using UnityEngine;

public static class HullMeshValidator
{
    public struct ValidationResult
    {
        public bool degenerateOK;
        public bool manifoldOK;
        public bool normalsOK;
        public bool volumeOK;

        public float signedVolume;

        public List<(int, int)> boundaryEdges;
        public List<(int, int)> nonManifoldEdges;
        public List<int> degenerateTriangles;
        public List<int> flippedTriangles;

        public bool IsValid =>
            degenerateOK &&
            manifoldOK &&
            normalsOK &&
            volumeOK;
    }

    public static ValidationResult Validate(Mesh mesh)
    {
        var result = new ValidationResult
        {
            boundaryEdges = new List<(int, int)>(),
            nonManifoldEdges = new List<(int, int)>(),
            degenerateTriangles = new List<int>(),
            flippedTriangles = new List<int>(),

            degenerateOK = true,
            manifoldOK = true,
            normalsOK = true,
            volumeOK = true
        };

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        int triCount = tris.Length / 3;

        // ---------------------------------------------------------
        // 1. Degenerate triangles
        // ---------------------------------------------------------
        for (int i = 0; i < triCount; i++)
        {
            int i0 = tris[i * 3 + 0];
            int i1 = tris[i * 3 + 1];
            int i2 = tris[i * 3 + 2];

            Vector3 v0 = verts[i0];
            Vector3 v1 = verts[i1];
            Vector3 v2 = verts[i2];

            if (Vector3.Cross(v1 - v0, v2 - v0).sqrMagnitude < 1e-12f)
            {
                result.degenerateTriangles.Add(i);
                result.degenerateOK = false;
            }
        }

        // ---------------------------------------------------------
        // 2. Boundary / non-manifold edges
        // ---------------------------------------------------------
        var edgeCount = new Dictionary<(int, int), int>();

        void AddEdge(int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            if (!edgeCount.ContainsKey(key))
                edgeCount[key] = 0;
            edgeCount[key]++;
        }

        for (int i = 0; i < triCount; i++)
        {
            int a = tris[i * 3 + 0];
            int b = tris[i * 3 + 1];
            int c = tris[i * 3 + 2];

            AddEdge(a, b);
            AddEdge(b, c);
            AddEdge(c, a);
        }

        foreach (var kvp in edgeCount)
        {
            if (kvp.Value == 1)
            {
                result.boundaryEdges.Add(kvp.Key);
                result.manifoldOK = false;
            }
            else if (kvp.Value > 2)
            {
                result.nonManifoldEdges.Add(kvp.Key);
                result.manifoldOK = false;
            }
        }

        // ---------------------------------------------------------
        // 3. Normal consistency
        // ---------------------------------------------------------
        Vector3 refNormal = Vector3.zero;
        bool refSet = false;

        for (int i = 0; i < triCount; i++)
        {
            int i0 = tris[i * 3 + 0];
            int i1 = tris[i * 3 + 1];
            int i2 = tris[i * 3 + 2];

            Vector3 v0 = verts[i0];
            Vector3 v1 = verts[i1];
            Vector3 v2 = verts[i2];

            Vector3 n = Vector3.Cross(v1 - v0, v2 - v0);

            if (n.sqrMagnitude < 1e-12f)
                continue;

            if (!refSet)
            {
                refNormal = n.normalized;
                refSet = true;
            }
            else
            {
                if (Vector3.Dot(refNormal, n) < 0f)
                {
                    result.flippedTriangles.Add(i);
                    result.normalsOK = false;
                }
            }
        }

        // ---------------------------------------------------------
        // 4. Signed volume
        // ---------------------------------------------------------
        float vol = 0f;
        for (int i = 0; i < triCount; i++)
        {
            int i0 = tris[i * 3 + 0];
            int i1 = tris[i * 3 + 1];
            int i2 = tris[i * 3 + 2];

            vol += Vector3.Dot(verts[i0], Vector3.Cross(verts[i1], verts[i2])) / 6f;
        }

        result.signedVolume = vol;
        result.volumeOK = Mathf.Abs(vol) > 1e-6f;

        return result;
    }
}