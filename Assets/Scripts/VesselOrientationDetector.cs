using System.Collections;
using UnityEngine;


namespace Axiom.Vessel.Diagnostics
{
    /// <summary>
    /// Detects the vessel's canonical orientation profile by applying controlled
    /// perturbations around each local axis and measuring the resulting restoring
    /// angular velocity from buoyancy.
    /// 
    /// This detector determines:
    /// - Roll axis (axis with strongest restoring moment)
    /// - Pitch and yaw axes (orthogonal axes inferred from roll)
    /// - Roll direction (+1 / -1) based on asymmetric restoring response
    /// 
    /// IMPORTANT:
    /// This class does NOT assume any initial vessel orientation. It derives
    /// the correct axes purely from physical response, making it robust to:
    /// - Mirrored hulls
    /// - Arbitrary model rotations
    /// - Non‑standard modelling conventions
    /// 
    /// The resulting <see cref="VesselOrientationProfile"/> is consumed by all
    /// downstream systems (stability, buoyancy, controls, etc.).
    /// </summary>
    public sealed class VesselOrientationDetector
    {
        // --------------------------------------------------------------------
        // Dependencies
        // --------------------------------------------------------------------

        private readonly Transform boat;   // Vessel root transform
        private readonly Rigidbody rb;     // Rigidbody used for angular velocity sampling
        private readonly BoatCOB cob;      // Centre‑of‑buoyancy provider (not used directly yet)


        /// <summary>
        /// Constructs a new orientation detector for a specific vessel instance.
        /// </summary>
        public VesselOrientationDetector(Transform boat, Rigidbody rb, BoatCOB cob)
        {
            this.boat = boat;
            this.rb = rb;
            this.cob = cob;
        }


        // --------------------------------------------------------------------
        // Orientation Detection
        // --------------------------------------------------------------------

        /// <summary>
        /// Runs the full orientation detection routine.
        /// 
        /// Steps:
        /// 1. Apply a small rotation around each local axis (X, Y, Z)
        /// 2. Allow buoyancy to respond
        /// 3. Measure restoring angular velocity magnitude
        /// 4. Select the axis with the strongest restoring response → roll axis
        /// 5. Infer pitch/yaw axes from remaining orthogonal axes
        /// 6. Determine roll direction by comparing +angle vs -angle response
        /// 7. Restore original vessel state
        /// 8. Return a populated <see cref="VesselOrientationProfile"/>
        /// </summary>
        public IEnumerator DetectOrientation(System.Action<VesselOrientationProfile> onComplete)
        {
            const float testAngleDeg = 15f;   // Perturbation angle for axis testing
            const int settleFrames = 5;       // Frames to wait for buoyancy to respond

            // ------------------------------------------------------------
            // Cache original vessel state so we can restore it later
            // ------------------------------------------------------------
            Quaternion originalRot = boat.localRotation;
            Vector3 originalAngVel = rb.angularVelocity;


            // ------------------------------------------------------------
            // Local helper: apply rotation around an axis, wait for buoyancy,
            // measure restoring angular velocity, then restore state.
            // ------------------------------------------------------------
            IEnumerator TestAxis(Vector3 localAxis, float angleDeg, System.Action<float> onMeasured)
            {
                // Apply rotation relative to original orientation
                Quaternion delta = Quaternion.AngleAxis(angleDeg, localAxis);
                boat.localRotation = originalRot * delta;

                // Sync rigidbody to match transform
                rb.rotation = boat.rotation;
                rb.angularVelocity = Vector3.zero;
                UnityEngine.Physics.SyncTransforms();


                // Allow buoyancy to generate restoring torque
                for (int i = 0; i < settleFrames; i++)
                    yield return new WaitForFixedUpdate();

                // Measure restoring angular velocity magnitude
                float magnitude = rb.angularVelocity.magnitude;

                // Reset vessel to original state
                boat.localRotation = originalRot;
                rb.rotation = boat.rotation;
                rb.angularVelocity = Vector3.zero;
                UnityEngine.Physics.SyncTransforms();


                onMeasured?.Invoke(magnitude);
            }


            // ------------------------------------------------------------
            // 1. Test each axis (X, Y, Z) for restoring response
            // ------------------------------------------------------------
            float xResponse = 0f;
            float yResponse = 0f;
            float zResponse = 0f;

            yield return TestAxis(Vector3.right, testAngleDeg, m => xResponse = m);
            yield return TestAxis(Vector3.up, testAngleDeg, m => yResponse = m);
            yield return TestAxis(Vector3.forward, testAngleDeg, m => zResponse = m);


            // ------------------------------------------------------------
            // 2. Determine roll axis by strongest restoring response
            // ------------------------------------------------------------
            Vector3 rollAxis;
            Vector3 pitchAxis;
            Vector3 yawAxis;

            if (xResponse >= yResponse && xResponse >= zResponse)
            {
                rollAxis = Vector3.right;
                pitchAxis = Vector3.forward;
                yawAxis = Vector3.up;
            }
            else if (yResponse >= xResponse && yResponse >= zResponse)
            {
                rollAxis = Vector3.up;
                pitchAxis = Vector3.right;
                yawAxis = Vector3.forward;
            }
            else
            {
                rollAxis = Vector3.forward;
                pitchAxis = Vector3.right;
                yawAxis = Vector3.up;
            }


            // ------------------------------------------------------------
            // 3. Determine roll direction (+1 or -1)
            //    Compare restoring response for +angle vs -angle.
            // ------------------------------------------------------------
            float rollDirection = 1f;

            {
                float posMag = 0f;
                float negMag = 0f;

                yield return TestAxis(rollAxis, testAngleDeg, m => posMag = m);
                yield return TestAxis(rollAxis, -testAngleDeg, m => negMag = m);

                rollDirection = posMag >= negMag ? 1f : -1f;
            }


            // ------------------------------------------------------------
            // 4. Pitch/Yaw direction detection (future extension)
            //    For now, assume +1 for both.
            // ------------------------------------------------------------
            float pitchDirection = 1f;
            float yawDirection = 1f;

            bool isMirrored = false; // Placeholder for future hull‑mirroring detection


            // ------------------------------------------------------------
            // 5. Restore original vessel state
            // ------------------------------------------------------------
            boat.localRotation = originalRot;
            rb.rotation = boat.rotation;
            rb.angularVelocity = originalAngVel;
            UnityEngine.Physics.SyncTransforms();



            // ------------------------------------------------------------
            // 6. Construct final orientation profile
            // ------------------------------------------------------------
            var profile = new VesselOrientationProfile
            {
                RollAxis = rollAxis,
                RollDirection = rollDirection,

                PitchAxis = pitchAxis,
                PitchDirection = pitchDirection,

                YawAxis = yawAxis,
                YawDirection = yawDirection,

                IsMirrored = isMirrored,
                IsValid = true
            };


            // ------------------------------------------------------------
            // 7. Return result
            // ------------------------------------------------------------
            onComplete?.Invoke(profile);
        }
    }
}