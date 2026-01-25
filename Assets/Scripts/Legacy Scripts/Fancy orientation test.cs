/*using UnityEngine;

[ExecuteInEditMode]
public class HullOrientationDiagnostic : MonoBehaviour
{
    public MeshFilter hullMesh;

    [Header("Run once")]
    public bool runDiagnostic;

    void Update()
    {
        if (!Application.isPlaying && runDiagnostic)
        {
            runDiagnostic = false;
            Run();
        }
    }

    void Run()
    {
        if (hullMesh == null || hullMesh.sharedMesh == null)
        {
            Debug.LogWarning("No mesh assigned.");
            return;
        }

        Vector3 f, r, u;
        ComputePCAFrame(hullMesh.sharedMesh, hullMesh.transform, out f, out r, out u);

        if (Vector3.Dot(u, Vector3.up) < 0)
        {
            f = -f; r = -r; u = -u;
        }

        Debug.Log($"PCA Forward: {f}");
        Debug.Log($"PCA Up:      {u}");
        Debug.Log($"PCA Right:   {r}");

        Debug.Log($"Angle to world forward: {Vector3.Angle(f, Vector3.forward):F2}°");
        Debug.Log($"Angle to world up:      {Vector3.Angle(u, Vector3.up):F2}°");
    }

    void ComputePCAFrame(Mesh mesh, Transform t, out Vector3 forward, out Vector3 right, out Vector3 up)
    {
        Vector3[] verts = mesh.vertices;
        Vector3 centroid = Vector3.zero;
        Vector3[] pts = new Vector3[verts.Length];

        for (int i = 0; i < verts.Length; i++)
        {
            pts[i] = t.TransformPoint(verts[i]);
            centroid += pts[i];
        }
        centroid /= verts.Length;

        float xx = 0, xy = 0, xz = 0;
        float yy = 0, yz = 0, zz = 0;

        foreach (var p in pts)
        {
            Vector3 d = p - centroid;
            xx += d.x * d.x;
            xy += d.x * d.y;
            xz += d.x * d.z;
            yy += d.y * d.y;
            yz += d.y * d.z;
            zz += d.z * d.z;
        }

        Matrix4x4 C = new Matrix4x4();
        C[0, 0] = xx; C[0, 1] = xy; C[0, 2] = xz;
        C[1, 0] = xy; C[1, 1] = yy; C[1, 2] = yz;
        C[2, 0] = xz; C[2, 1] = yz; C[2, 2] = zz;

        Vector3 eig = PowerIteration(C, 32);
        forward = eig.normalized;

        Vector3 temp = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.9f
            ? Vector3.right
            : Vector3.up;

        Vector3.OrthoNormalize(ref forward, ref temp);
        right = temp;
        up = Vector3.Cross(right, forward).normalized;
    }

    Vector3 PowerIteration(Matrix4x4 M, int it)
    {
        Vector3 v = new Vector3(1, 1, 1).normalized;
        for (int i = 0; i < it; i++)
        {
            v = new Vector3(
                M[0, 0] * v.x + M[0, 1] * v.y + M[0, 2] * v.z,
                M[1, 0] * v.x + M[1, 1] * v.y + M[1, 2] * v.z,
                M[2, 0] * v.x + M[2, 1] * v.y + M[2, 2] * v.z
            ).normalized;
        }
        return v;
    }
}*/