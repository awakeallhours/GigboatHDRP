using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class HullDebugVisualizer : MonoBehaviour
{
    [Header("Mesh Source")]
    public MeshFilter hullMeshFilter;
    public MeshCollider hullMeshCollider;
    public Mesh meshOverride;

    [Header("Display Options")]
    public bool showWireframe = true;
    public bool showBoundaryEdges = true;
    public bool showNonManifoldEdges = true;
    public bool showFlippedTriangles = true;
    public bool showDegenerateTriangles = true;

    [Header("Colors")]
    public Color wireColor = new Color(1f, 1f, 1f, 0.1f);
    public Color boundaryColor = Color.red;
    public Color nonManifoldColor = new Color(0.6f, 0f, 0.6f);
    public Color flippedColor = Color.blue;
    public Color degenerateColor = Color.yellow;

    private HullMeshValidator.ValidationResult validation;
    private Mesh mesh;

    private void OnValidate()
    {
        ResolveMesh();
        RunValidation();
    }

    private void OnDrawGizmos()
    {
        if (mesh == null)
            return;

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        // ---------------------------------------
        // Wireframe
        // ---------------------------------------
        if (showWireframe)
        {
            Gizmos.color = wireColor;
            for (int i = 0; i < tris.Length; i += 3)
            {
                DrawTri(verts, tris, i);
            }
        }

        // ---------------------------------------
        // Boundary edges
        // ---------------------------------------
        if (showBoundaryEdges)
        {
            Gizmos.color = boundaryColor;
            foreach (var e in validation.boundaryEdges)
                DrawEdge(verts, e.Item1, e.Item2);
        }

        // ---------------------------------------
        // Non-manifold edges
        // ---------------------------------------
        if (showNonManifoldEdges)
        {
            Gizmos.color = nonManifoldColor;
            foreach (var e in validation.nonManifoldEdges)
                DrawEdge(verts, e.Item1, e.Item2);
        }

        // ---------------------------------------
        // Flipped triangles
        // ---------------------------------------
        if (showFlippedTriangles)
        {
            Gizmos.color = flippedColor;
            foreach (int triIndex in validation.flippedTriangles)
                DrawTri(verts, tris, triIndex * 3);
        }

        // ---------------------------------------
        // Degenerate triangles
        // ---------------------------------------
        if (showDegenerateTriangles)
        {
            Gizmos.color = degenerateColor;
            foreach (int triIndex in validation.degenerateTriangles)
                DrawTri(verts, tris, triIndex * 3);
        }
    }

    // ---------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------
    private void ResolveMesh()
    {
        if (meshOverride != null)
            mesh = meshOverride;
        else if (hullMeshFilter != null && hullMeshFilter.sharedMesh != null)
            mesh = hullMeshFilter.sharedMesh;
        else if (hullMeshCollider != null && hullMeshCollider.sharedMesh != null)
            mesh = hullMeshCollider.sharedMesh;
        else
            mesh = null;
    }

    private void RunValidation()
    {
        if (mesh != null)
            validation = HullMeshValidator.Validate(mesh);
    }

    private void DrawEdge(Vector3[] verts, int i0, int i1)
    {
        Gizmos.DrawLine(
            transform.TransformPoint(verts[i0]),
            transform.TransformPoint(verts[i1])
        );
    }

    private void DrawTri(Vector3[] verts, int[] tris, int start)
    {
        int i0 = tris[start + 0];
        int i1 = tris[start + 1];
        int i2 = tris[start + 2];

        Vector3 v0 = transform.TransformPoint(verts[i0]);
        Vector3 v1 = transform.TransformPoint(verts[i1]);
        Vector3 v2 = transform.TransformPoint(verts[i2]);

        Gizmos.DrawLine(v0, v1);
        Gizmos.DrawLine(v1, v2);
        Gizmos.DrawLine(v2, v0);
    }
}