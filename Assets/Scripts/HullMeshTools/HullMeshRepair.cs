using System.Collections.Generic;
using UnityEngine;

public static class HullMeshRepair
{
    public static Mesh CloneMesh(Mesh source)
    {
        var m = new Mesh
        {
            vertices = source.vertices,
            triangles = source.triangles,
            normals = source.normals,
            uv = source.uv,
            tangents = source.tangents,
            colors = source.colors
        };
        m.name = source.name + "_Clone";
        return m;
    }

    public static void RemoveDegenerateTriangles(Mesh mesh, HullMeshValidator.ValidationResult validation)
    {
        if (validation.degenerateTriangles == null || validation.degenerateTriangles.Count == 0)
            return;

        var tris = new List<int>(mesh.triangles);
        int triCount = tris.Count / 3;

        var toRemove = new HashSet<int>(validation.degenerateTriangles);

        var newTris = new List<int>(tris.Count);
        for (int i = 0; i < triCount; i++)
        {
            if (toRemove.Contains(i))
                continue;

            newTris.Add(tris[i * 3 + 0]);
            newTris.Add(tris[i * 3 + 1]);
            newTris.Add(tris[i * 3 + 2]);
        }

        mesh.triangles = newTris.ToArray();
        mesh.RecalculateBounds();
    }

    public static void FixFlippedTriangles(Mesh mesh, HullMeshValidator.ValidationResult validation)
    {
        if (validation.flippedTriangles == null || validation.flippedTriangles.Count == 0)
            return;

        var tris = mesh.triangles;
        int triCount = tris.Length / 3;

        foreach (int triIndex in validation.flippedTriangles)
        {
            int baseIndex = triIndex * 3;

            // Safety guard: skip if triangle index is invalid
            if (baseIndex + 2 >= tris.Length)
                continue;

            int i0 = tris[baseIndex + 0];
            int i1 = tris[baseIndex + 1];
            int i2 = tris[baseIndex + 2];

            // Swap winding
            tris[baseIndex + 0] = i0;
            tris[baseIndex + 1] = i2;
            tris[baseIndex + 2] = i1;
        }

        mesh.triangles = tris;
        mesh.RecalculateBounds();
    }

    public static void WeldVertices(Mesh mesh, float tolerance = 1e-4f)
    {
        var verts = mesh.vertices;
        var tris = mesh.triangles;

        int vertCount = verts.Length;

        var map = new Dictionary<Vector3, int>(vertCount);
        var newVerts = new List<Vector3>();
        var remap = new int[vertCount];

        for (int i = 0; i < vertCount; i++)
        {
            Vector3 v = verts[i];

            // Quantize to tolerance
            Vector3 key = new Vector3(
                Mathf.Round(v.x / tolerance) * tolerance,
                Mathf.Round(v.y / tolerance) * tolerance,
                Mathf.Round(v.z / tolerance) * tolerance
            );

            if (!map.TryGetValue(key, out int newIndex))
            {
                newIndex = newVerts.Count;
                newVerts.Add(v);
                map[key] = newIndex;
            }

            remap[i] = newIndex;
        }

        for (int i = 0; i < tris.Length; i++)
            tris[i] = remap[tris[i]];

        mesh.vertices = newVerts.ToArray();
        mesh.triangles = tris;
        mesh.RecalculateBounds();
    }

    public static void RemoveNonManifoldEdges(Mesh mesh, HullMeshValidator.ValidationResult validation)
    {
        if (validation.nonManifoldEdges == null || validation.nonManifoldEdges.Count == 0)
            return;

        // Simple strategy: remove all triangles that use any non-manifold edge
        var tris = mesh.triangles;
        int triCount = tris.Length / 3;

        var badEdges = new HashSet<(int, int)>();
        foreach (var e in validation.nonManifoldEdges)
        {
            int a = Mathf.Min(e.Item1, e.Item2);
            int b = Mathf.Max(e.Item1, e.Item2);
            badEdges.Add((a, b));
        }

        bool UsesBadEdge(int i0, int i1)
        {
            int a = Mathf.Min(i0, i1);
            int b = Mathf.Max(i0, i1);
            return badEdges.Contains((a, b));
        }

        var newTris = new List<int>(tris.Length);

        for (int i = 0; i < triCount; i++)
        {
            int i0 = tris[i * 3 + 0];
            int i1 = tris[i * 3 + 1];
            int i2 = tris[i * 3 + 2];

            if (UsesBadEdge(i0, i1) || UsesBadEdge(i1, i2) || UsesBadEdge(i2, i0))
                continue;

            newTris.Add(i0);
            newTris.Add(i1);
            newTris.Add(i2);
        }

        mesh.triangles = newTris.ToArray();
        mesh.RecalculateBounds();
    }

    public static void RecalculateNormalsAndTangents(Mesh mesh)
    {
        mesh.RecalculateNormals();
        if (mesh.uv != null && mesh.uv.Length == mesh.vertexCount)
            mesh.RecalculateTangents();
    }
}