using UnityEngine;

[System.Serializable]
public class HullHydroData
{
    [Header("Basic Hydrostatics")]
    public float displacementVolume;          // m³
    public float equilibriumDraft;            // metres
    public float equilibriumWaterlineY;       // world-space Y of waterline at equilibrium
    public Vector3 centerOfBuoyancy;          // world-space COB at equilibrium

    [Header("Probe Fallback")]
    public float[] probeWeights;              // normalised weights for distributing buoyant force

    [System.Serializable]
    public struct HullTriangle
    {
        public Vector3 v0Local;
        public Vector3 v1Local;
        public Vector3 v2Local;
        public Vector3 normalLocal;
        public float area;
        public Vector3 centroidLocal;
    }

    [Header("Mesh-based Hydrostatics")]
    public HullTriangle[] triangles;
    public float hullMeshVolume;              // signed volume from mesh (m³)
    public Vector3 hullMeshCentroidLocal;     // centroid in local hull space
}

[CreateAssetMenu(
    fileName = "HullHydroData",
    menuName = "Axiom/Hydrostatics/Hull Hydro Data")]
public class HullHydroDataAsset : ScriptableObject
{
    public HullHydroData data = new HullHydroData();
}