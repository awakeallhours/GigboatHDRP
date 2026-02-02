using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class HullRepairTool : MonoBehaviour
{
    [Header("Mesh Source")]
    public MeshFilter hullMeshFilter;
    public MeshCollider hullMeshCollider;
    public Mesh meshOverride;

    [Header("Weld Settings (for Moderate/Aggressive)")]
    public float weldTolerance = 1e-4f;

    [Header("Last Validation")]
    public bool lastValid;
    public float lastVolume;

    private Mesh workingMesh;

    [ContextMenu("Validate Only")]
    public void ValidateOnly()
    {
        ResolveMesh();
        if (workingMesh == null)
        {
            Debug.LogError("[HullRepairTool] No mesh found.");
            return;
        }

        var validation = HullMeshValidator.Validate(workingMesh);
        lastValid = validation.IsValid;
        lastVolume = validation.signedVolume;

        Debug.Log($"[HullRepairTool] Validation complete. Valid={validation.IsValid}, Volume={validation.signedVolume}");
    }

    [ContextMenu("Repair - Conservative (A)")]
    public void RepairConservative()
    {
        RunRepair(conservative: true, moderate: false, aggressive: false);
    }

    [ContextMenu("Repair - Moderate (B)")]
    public void RepairModerate()
    {
        RunRepair(conservative: true, moderate: true, aggressive: false);
    }

    [ContextMenu("Repair - Aggressive (C)")]
    public void RepairAggressive()
    {
        RunRepair(conservative: true, moderate: true, aggressive: true);
    }

    private void RunRepair(bool conservative, bool moderate, bool aggressive)
    {
        ResolveMesh();
        if (workingMesh == null)
        {
            Debug.LogError("[HullRepairTool] No mesh found.");
            return;
        }

        // Always work on a clone so we never mutate the original asset in-place
        workingMesh = HullMeshRepair.CloneMesh(workingMesh);

        var validation = HullMeshValidator.Validate(workingMesh);

        if (conservative)
        {
            HullMeshRepair.RemoveDegenerateTriangles(workingMesh, validation);
            HullMeshRepair.FixFlippedTriangles(workingMesh, validation);
        }

        if (moderate)
        {
            HullMeshRepair.WeldVertices(workingMesh, weldTolerance);
        }

        if (aggressive)
        {
            // Try to remove non-manifold edges
            validation = HullMeshValidator.Validate(workingMesh);
            HullMeshRepair.RemoveNonManifoldEdges(workingMesh, validation);
        }

        HullMeshRepair.RecalculateNormalsAndTangents(workingMesh);

        // Re-validate after repair
        var postValidation = HullMeshValidator.Validate(workingMesh);
        lastValid = postValidation.IsValid;
        lastVolume = postValidation.signedVolume;

        Debug.Log($"[HullRepairTool] Repair complete. Valid={postValidation.IsValid}, Volume={postValidation.signedVolume}");

        SaveMeshAsNewAsset(workingMesh);
    }

    private void ResolveMesh()
    {
        if (meshOverride != null)
        {
            workingMesh = meshOverride;
        }
        else if (hullMeshFilter != null && hullMeshFilter.sharedMesh != null)
        {
            workingMesh = hullMeshFilter.sharedMesh;
        }
        else if (hullMeshCollider != null && hullMeshCollider.sharedMesh != null)
        {
            workingMesh = hullMeshCollider.sharedMesh;
        }
        else
        {
            workingMesh = null;
        }
    }

    private void SaveMeshAsNewAsset(Mesh mesh)
    {
#if UNITY_EDITOR
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Repaired Hull Mesh",
            mesh.name + "_Repaired.asset",
            "asset",
            "Choose location for repaired mesh asset");

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("[HullRepairTool] Save cancelled.");
            return;
        }

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[HullRepairTool] Repaired mesh saved to: {path}");
#else
        Debug.LogWarning("[HullRepairTool] Mesh saving only supported in editor.");
#endif
    }
}