using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Axiom.Vessel
{
    /// <summary>
    /// Defines the vessel's canonical orientation profile.
    /// 
    /// This struct describes:
    /// - The vessel's roll, pitch, and yaw axes (in LOCAL space)
    /// - The sign conventions for each axis (+1 / -1)
    /// - Whether the hull is mirrored (left/right swapped)
    /// - Whether the profile has been fully detected and validated
    /// 
    /// The orientation profile is produced by the vessel bootstrap system and
    /// consumed by all physics subsystems (buoyancy, stability, controls, etc.).
    /// 
    /// IMPORTANT:
    /// These axes MUST be orthogonal, normalised, and consistent with the
    /// vessel's geometry. Incorrect orientation profiles will corrupt all
    /// downstream physics (GM/GZ, roll damping, steering, etc.).
    /// </summary>
    [System.Serializable]
    public struct VesselOrientationProfile
    {
        // --------------------------------------------------------------------
        // Roll
        // --------------------------------------------------------------------

        /// <summary>
        /// Local-space axis the vessel rolls around.
        /// Must be a normalised vector.
        /// 
        /// Example (typical boat):
        ///     RollAxis = Vector3.right
        /// </summary>
        public Vector3 RollAxis;

        /// <summary>
        /// Direction multiplier (+1 or -1) indicating which direction of rotation
        /// around <see cref="RollAxis"/> produces a positive righting moment.
        /// 
        /// This resolves left/right ambiguity and ensures heel sign is consistent.
        /// </summary>
        public float RollDirection;


        // --------------------------------------------------------------------
        // Pitch
        // --------------------------------------------------------------------

        /// <summary>
        /// Local-space axis the vessel pitches around.
        /// Must be a normalised vector.
        /// 
        /// Example (typical boat):
        ///     PitchAxis = Vector3.forward
        /// </summary>
        public Vector3 PitchAxis;

        /// <summary>
        /// Direction multiplier (+1 or -1) indicating which direction of rotation
        /// around <see cref="PitchAxis"/> corresponds to "bow down".
        /// </summary>
        public float PitchDirection;


        // --------------------------------------------------------------------
        // Yaw
        // --------------------------------------------------------------------

        /// <summary>
        /// Local-space axis the vessel yaws around.
        /// Must be a normalised vector.
        /// 
        /// Example (typical boat):
        ///     YawAxis = Vector3.up
        /// </summary>
        public Vector3 YawAxis;

        /// <summary>
        /// Direction multiplier (+1 or -1) indicating which direction of rotation
        /// around <see cref="YawAxis"/> corresponds to "turn right".
        /// </summary>
        public float YawDirection;


        // --------------------------------------------------------------------
        // Mirroring
        // --------------------------------------------------------------------

        /// <summary>
        /// True if the hull is mirrored (left/right swapped).
        /// 
        /// This is detected automatically by the bootstrap system and ensures
        /// that roll/pitch/yaw sign conventions remain correct even if the
        /// model is mirrored in the modelling software.
        /// </summary>
        public bool IsMirrored;


        // --------------------------------------------------------------------
        // Validity
        // --------------------------------------------------------------------

        /// <summary>
        /// True if the orientation profile has been fully detected and validated.
        /// 
        /// All physics systems should check this flag before using the profile.
        /// If false, the vessel's orientation is undefined and physics behaviour
        /// may be incorrect or unstable.
        /// </summary>
        public bool IsValid;

        public string[] Warnings;



    }
}