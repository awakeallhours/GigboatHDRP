using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    /// <summary>
    /// Performs a controlled GM/GZ stability scan by incrementally heeling the vessel,
    /// allowing buoyancy to settle, and sampling COM/COB geometry at each angle.
    /// 
    /// This scanner:
    /// - Uses the vessel's detected roll axis (from VesselBootstrap.Orientation)
    /// - Applies heel in local space for deterministic rotation
    /// - Forces rigidbody rotation to match the visual transform
    /// - Waits for buoyancy to stabilise before sampling
    /// - Produces raw StabilitySample entries for the builder to interpret
    /// 
    /// NOTE:
    /// This class does NOT interpret stability results. It only gathers raw data.
    /// All interpretation (GM_Initial, GZ_ZeroAngle, etc.) is handled by StabilityProfileBuilder.
    /// </summary>
    public sealed class GMGZStabilityScanner
    {
        private readonly VesselBootstrap bootstrap;   // Provides orientation profile + roll axis
        private readonly Transform boat;              // The vessel root transform
        private readonly Rigidbody rb;                // Rigidbody used for world COM
        private readonly BoatCOB cob;                 // Provides COB world position
        private readonly BoatCOM com;                 // Provides COM metadata (neutral band etc.)

        /// <summary>
        /// Constructs a stability scanner bound to a specific vessel instance.
        /// </summary>
        public GMGZStabilityScanner(
            VesselBootstrap bootstrap,
            Transform boat,
            Rigidbody rb,
            BoatCOB cob,
            BoatCOM com)
        {
            this.bootstrap = bootstrap;
            this.boat = boat;
            this.rb = rb;
            this.cob = cob;
            this.com = com;
        }

        /// <summary>
        /// Runs a full GM/GZ stability scan over a specified heel range.
        /// 
        /// Parameters:
        /// - startAngle : starting heel angle in degrees (typically 0)
        /// - endAngle   : ending heel angle in degrees (e.g. 60 or 90)
        /// - step       : increment per sample (smaller = more accurate, slower)
        /// - settleTime : seconds to wait after each heel step for buoyancy to stabilise
        /// - onComplete : callback invoked with the final StabilityProfile
        /// 
        /// Behaviour:
        /// - Applies heel around the vessel's detected roll axis
        /// - Forces rigidbody rotation to match transform rotation
        /// - Clears angular velocity to avoid drift
        /// - Waits for buoyancy to settle before sampling
        /// - Computes heel angle, GZ, and GM for each step
        /// - Passes raw samples to StabilityProfileBuilder for interpretation
        /// </summary>
        public IEnumerator RunScan(
            float startAngle,
            float endAngle,
            float step,
            float settleTime,
            Action<StabilityProfile> onComplete)
        {
            // Raw sample list passed to the builder.
            // Each entry contains: heelDeg, GM, GZ.
            List<StabilitySample> samples = new List<StabilitySample>();

            // Sweep from startAngle → endAngle inclusive.
            for (float angle = startAngle; angle <= endAngle; angle += step)
            {
                // ------------------------------------------------------------
                // 1. Determine the correct roll axis for this vessel.
                //    RollAxis is a unit vector; RollDirection is ±1.
                //    Multiplying ensures correct handedness.
                // ------------------------------------------------------------
                Vector3 rollAxis =
                    bootstrap.Orientation.RollAxis *
                    bootstrap.Orientation.RollDirection;

                // ------------------------------------------------------------
                // 2. Apply heel rotation in LOCAL space.
                //    This ensures deterministic rotation independent of world orientation.
                // ------------------------------------------------------------
                boat.localRotation = Quaternion.AngleAxis(angle, rollAxis);

                // ------------------------------------------------------------
                // 3. Force the rigidbody to match the visual transform.
                //    This prevents drift or mismatch between physics and visuals.
                // ------------------------------------------------------------
                rb.rotation = boat.rotation;
                rb.angularVelocity = Vector3.zero; // Hard reset to avoid residual spin

                // ------------------------------------------------------------
                // 4. Allow buoyancy to stabilise.
                //    Required because COB depends on probe forces settling.
                // ------------------------------------------------------------
                yield return new WaitForSeconds(settleTime);

                // ------------------------------------------------------------
                // 5. Compute heel angle using the vessel's orientation profile.
                //    This returns a SIGNED heel angle (left/right).
                // ------------------------------------------------------------
                float heelDeg = GMGZUtility.ComputeHeelAngle(boat);

                // ------------------------------------------------------------
                // 6. Compute GZ (righting arm).
                //    Uses world COM, COB world position, and the roll axis.
                // ------------------------------------------------------------
                float gz = GMGZUtility.ComputeGZ(
                    rb.worldCenterOfMass,
                    cob.COBWorldPosition,
                    rollAxis);

                // ------------------------------------------------------------
                // 7. Compute GM (metacentric height).
                //    Uses corrected GM formula (sin(|heel|)).
                // ------------------------------------------------------------
                float gm = GMGZUtility.ComputeGM(heelDeg, gz);

                // ------------------------------------------------------------
                // 8. Store raw sample.
                //    Interpretation happens later in StabilityProfileBuilder.
                // ------------------------------------------------------------
                samples.Add(new StabilitySample(heelDeg, gm, gz));
            }

            // ------------------------------------------------------------
            // 9. Build the final interpreted stability profile.
            //    The builder computes:
            //    - GM_Initial
            //    - GM_Peak
            //    - GZ_Peak
            //    - GZ_ZeroAngle
            //    - Positive Stability Range
            //    - COM safe band
            // ------------------------------------------------------------
            var profile = StabilityProfileBuilder.Build(
                samples,
                com.NeutralBandMin,          // Lower COM safe limit
                com.NeutralBandMin + 0.5f,   // Upper COM safe limit (placeholder)
                "Auto-generated GM/GZ scan"  // Notes
            );

            // ------------------------------------------------------------
            // 10. Return the completed profile to the caller.
            // ------------------------------------------------------------
            onComplete?.Invoke(profile);
        }
    }
}