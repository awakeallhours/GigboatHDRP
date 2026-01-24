using Axiom.Vessel;
using UnityEngine;

public sealed class AxesProvider
{
    private readonly VesselOrientationProfile profile;
    private readonly Transform boat;

    public AxesProvider(VesselOrientationProfile profile, Transform boat)
    {
        this.profile = profile;
        this.boat = boat;
    }

    public Vector3 Forward =>
        boat.TransformDirection(profile.RollAxis * profile.RollDirection);

    public Vector3 Right =>
        boat.TransformDirection(profile.PitchAxis * profile.PitchDirection);

    public Vector3 Up =>
        boat.TransformDirection(profile.YawAxis * profile.YawDirection);
}