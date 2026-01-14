using Axiom.Physics.Units;
using UnityEngine;

[CreateAssetMenu(fileName = "BoatConfig", menuName = "Boat/Boat Config")]
public class BoatConfig : ScriptableObject
{
    public BuoyancyConfig buoyancy;
    public FollowCamConfig followCam;
}

[System.Serializable]
public struct BuoyancyConfig
{
    public float buoyancyStrength;
    public float waterDrag;
    public float waterAngularDrag;

    public float heaveDampingStrength;

    public DensityValue waterDensity;
    public AreaValue probeArea;
    public bool autoComputeStrength;

    public bool enableRightingMoment;
    public float rightingStrength;

    public int sternProbeIndex;
    public DistanceValue sternReferenceDepth;
}

[System.Serializable]
public struct FollowCamConfig
{
    [Range(0f, 1f)]
    public float sideDriftStrength;

    [Range(0f, 1f)]
    public float forwardDriftStrength;


    public float reverseSlewSpeed;
    public float deadZone;
    public float reverseCommit;
    public float forwardCommit;
}

