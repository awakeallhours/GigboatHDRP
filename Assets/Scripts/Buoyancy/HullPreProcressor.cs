using UnityEngine;

public class HullPreprocessor : MonoBehaviour
{
    [Header("Output Asset")]
    [Tooltip("The asset where baked hydrostatic data will be stored.")]
    public HullHydroDataAsset outputAsset;

    [Header("Hull Mesh (Preferred Source)")]
    [Tooltip("Preferred: The MeshFilter containing the visible hull mesh.")]
    public MeshFilter hullMeshFilter;

    [Header("Optional Mesh Sources (Fallbacks)")]
    [Tooltip("Fallback: MeshCollider containing the hull mesh (only if different from MeshFilter).")]
    public MeshCollider hullMeshCollider;

    [Tooltip("Fallback: Direct mesh override (use only if the hull mesh is not in the scene).")]
    public Mesh meshOverride;

    // -------------------------------------------------------------------------
    //  Bake Button
    // -------------------------------------------------------------------------
    [ContextMenu("Bake Hydrostatics")]
    public void BakeHydrostatics()
    {
        if (outputAsset == null)
        {
            Debug.LogError("[HullPreprocessor] No output asset assigned.");
            return;
        }

        // Safety: warn if user assigned a convex MeshCollider as the mesh source
        if (hullMeshCollider != null && hullMeshFilter == null && meshOverride == null)
        {
            if (hullMeshCollider.convex)
            {
                Debug.LogError("[HullPreprocessor] The assigned MeshCollider is set to CONVEX. " +
                               "Convex MeshColliders use a simplified hull and will produce incorrect hydrostatics. " +
                               "Use the MeshFilter instead.");
                return;
            }
        }

        Mesh mesh = ResolveMesh();
        if (mesh == null)
        {
            Debug.LogError("[HullPreprocessor] No hull mesh found. Assign a MeshFilter (preferred), MeshCollider, or Mesh Override.");
            return;
        }

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        int triCount = tris.Length / 3;
        var triData = new HullHydroData.HullTriangle[triCount];

        float totalVolume = 0f;
        Vector3 weightedCentroid = Vector3.zero;

        for (int i = 0; i < triCount; i++)
        {
            int i0 = tris[i * 3 + 0];
            int i1 = tris[i * 3 + 1];
            int i2 = tris[i * 3 + 2];

            Vector3 v0 = verts[i0];
            Vector3 v1 = verts[i1];
            Vector3 v2 = verts[i2];

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
            float area = normal.magnitude * 0.5f;
            Vector3 centroid = (v0 + v1 + v2) / 3f;

            triData[i] = new HullHydroData.HullTriangle
            {
                v0Local = v0,
                v1Local = v1,
                v2Local = v2,
                normalLocal = normal.sqrMagnitude > 0f ? normal.normalized : Vector3.up,
                area = area,
                centroidLocal = centroid
            };

            float signedVolume = Vector3.Dot(v0, Vector3.Cross(v1, v2)) / 6f;
            totalVolume += signedVolume;
            weightedCentroid += centroid * signedVolume;
        }

        var data = outputAsset.data;
        data.triangles = triData;
        data.hullMeshVolume = totalVolume;
        data.hullMeshCentroidLocal =
            Mathf.Abs(totalVolume) > 1e-6f ? weightedCentroid / totalVolume : Vector3.zero;

        Debug.Log($"[HullPreprocessor] Baked {triCount} triangles. Volume={totalVolume} m³");
    }

    // -------------------------------------------------------------------------
    //  Mesh Resolution Logic (Priority: Override → MeshFilter → MeshCollider)
    // -------------------------------------------------------------------------
    private Mesh ResolveMesh()
    {
        if (meshOverride != null)
            return meshOverride;

        if (hullMeshFilter != null && hullMeshFilter.sharedMesh != null)
            return hullMeshFilter.sharedMesh;

        if (hullMeshCollider != null && hullMeshCollider.sharedMesh != null)
            return hullMeshCollider.sharedMesh;

        return null;
    }
}