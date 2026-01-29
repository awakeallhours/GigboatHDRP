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
}

[CreateAssetMenu(
    fileName = "HullHydroData",
    menuName = "Axiom/Hydrostatics/Hull Hydro Data")]
public class HullHydroDataAsset : ScriptableObject
{
    public HullHydroData data = new HullHydroData();
}