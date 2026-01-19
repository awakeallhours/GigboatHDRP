using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    /// <summary>
    /// Provides low‑level geometric utilities for computing heel angle,
    /// righting arm (GZ), and metacentric height (GM).
    /// 
    /// IMPORTANT:
    /// These functions operate purely on geometry. They do NOT interpret
    /// stability, detect peaks, or determine validity. That responsibility
    /// belongs to the scanner and <see cref="StabilityProfileBuilder"/>.
    /// </summary>
    public static class GMGZUtility
    {
        // --------------------------------------------------------------------
        // Heel Angle
        // --------------------------------------------------------------------

        /// <summary>
        /// Computes the signed heel angle of the vessel in degrees.
        /// 
        /// This uses:
        /// - The vessel's current up vector (boat.up)
        /// - The vessel's local roll axis (boat.right)
        /// - A cross‑product test to determine heel direction
        /// 
        /// Positive/negative sign indicates heel direction relative to the
        /// vessel's detected roll axis. The magnitude represents the angular
        /// deviation between the vessel's up vector and world up.
        /// </summary>
        /// <param name="boat">The vessel transform whose orientation is sampled.</param>
        /// <returns>Signed heel angle in degrees.</returns>
        public static float ComputeHeelAngle(Transform boat)
        {
            // Vessel's current up direction in world space
            Vector3 up = boat.up;

            // Local roll axis (right vector) determines heel direction
            Vector3 rollAxis = boat.right;

            // Determine sign of heel using cross‑product orientation test
            float heelSign = Mathf.Sign(Vector3.Dot(
                Vector3.Cross(Vector3.up, up),
                rollAxis));

            // Compute unsigned heel angle via dot product
            float heelAngleRad = Mathf.Acos(
                Mathf.Clamp(Vector3.Dot(Vector3.up, up), -1f, 1f));

            // Apply sign to produce signed heel angle
            heelAngleRad *= heelSign;

            return heelAngleRad * Mathf.Rad2Deg;
        }


        // --------------------------------------------------------------------
        // GZ (Righting Arm)
        // --------------------------------------------------------------------

        /// <summary>
        /// Computes the righting arm (GZ) as the perpendicular distance between
        /// the COM→COB vector and the vessel's roll axis.
        /// 
        /// GZ represents the lever arm generating the righting moment.
        /// 
        /// Interpretation:
        /// - GZ &gt; 0 → vessel is self‑righting at this angle
        /// - GZ = 0 → vanishing stability
        /// - GZ &lt; 0 → overturning moment (not physically expected here)
        /// </summary>
        /// <param name="comWorld">World‑space centre of mass.</param>
        /// <param name="cobWorld">World‑space centre of buoyancy.</param>
        /// <param name="rollAxis">Unit roll axis of the vessel.</param>
        /// <returns>Righting arm magnitude in meters.</returns>
        public static float ComputeGZ(Vector3 comWorld, Vector3 cobWorld, Vector3 rollAxis)
        {
            // Vector from COM to COB
            Vector3 lever = cobWorld - comWorld;

            // Remove component parallel to roll axis → perpendicular lever arm
            Vector3 leverPerp = Vector3.ProjectOnPlane(lever, rollAxis);

            return leverPerp.magnitude;
        }


        // --------------------------------------------------------------------
        // GM (Metacentric Height)
        // --------------------------------------------------------------------

        /// <summary>
        /// Computes metacentric height (GM) using the relationship:
        /// 
        ///     GM = GZ / sin(|heel|)
        /// 
        /// GM describes the vessel's instantaneous stability response at a
        /// given heel angle. It is undefined at very small angles where
        /// sin(heel) approaches zero.
        /// 
        /// NOTE:
        /// - Heel angle magnitude is used (|heel|) because GM is always defined
        ///   using the absolute heel angle, not the signed direction.
        /// - If heel is too small, GM returns 0 to avoid numerical instability.
        /// </summary>
        /// <param name="heelDeg">Signed heel angle in degrees.</param>
        /// <param name="gz">Righting arm at this heel angle.</param>
        /// <returns>Metacentric height in meters.</returns>
        public static float ComputeGM(float heelDeg, float gz)
        {
            // Convert to radians and use magnitude for GM definition
            float heelRad = Mathf.Abs(heelDeg * Mathf.Deg2Rad);

            // Avoid division by zero at tiny heel angles
            float sinHeel = Mathf.Sin(heelRad);
            if (Mathf.Abs(sinHeel) < 0.0001f)
                return 0f;

            return gz / sinHeel;
        }
    }
}