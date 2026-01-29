//TODO

/*using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class HullPreprocessor : MonoBehaviour
{
    [Header("Output Asset")]
    public HullHydroDataAsset hydroDataAsset;

    [Header("Hull Source")]
    public MeshCollider hullCollider;   // Required for mesh-based hydrostatics

    // ---------------------------------------------------------
    // Inspector Button (Unity will call this when you press it)
    // ---------------------------------------------------------
    public void BakeHydroData()
    {
        if (hydroDataAsset == null)
        {
            Debug.LogError("[HullPreprocessor] No HullHydroDataAsset assigned.");
            return;
        }

        if (hullCollider == null)
        {
            Debug.LogError("[HullPreprocessor] No MeshCollider assigned.");
            return;
        }

        Debug.Log("[HullPreprocessor] Starting hydrostatics bake...");

        // -----------------------------------------------------
        // PHASE 1: Placeholder — real solver goes here later
        // -----------------------------------------------------
        HullHydroData data = hydroDataAsset.data;

        // These will be filled by the mesh-based solver later
        data.displacementVolume*/