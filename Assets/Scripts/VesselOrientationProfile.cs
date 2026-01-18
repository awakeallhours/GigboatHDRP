using UnityEngine;

namespace Axiom.Vessel
{
    [System.Serializable]
    public struct VesselOrientationProfile
    {
        // The axis the vessel rolls around (local space)
        public Vector3 RollAxis;

        // +1 or -1 depending on which direction produces positive righting moment
        public float RollDirection;

        // The axis the vessel pitches around (local space)
        public Vector3 PitchAxis;

        // +1 or -1 depending on which direction is "bow down"
        public float PitchDirection;

        // The axis the vessel yaws around (local space)
        public Vector3 YawAxis;

        // +1 or -1 depending on which direction is "turn right"
        public float YawDirection;

        // True if the hull is mirrored (left/right swapped)
        public bool IsMirrored;

        // Optional: a flag to indicate the profile has been fully detected
        public bool IsValid;
    }
}
