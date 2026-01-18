using System.Collections;
using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    public sealed class VesselOrientationDetector
    {
        private readonly Transform boat;
        private readonly Rigidbody rb;
        private readonly BoatCOB cob;

        public VesselOrientationDetector(Transform boat, Rigidbody rb, BoatCOB cob)
        {
            this.boat = boat;
            this.rb = rb;
            this.cob = cob;
        }

        public IEnumerator DetectOrientation(System.Action<VesselOrientationProfile> onComplete)
        {
            const float testAngleDeg = 15f;
            const int settleFrames = 5;

            // Cache original state
            Quaternion originalRot = boat.localRotation;
            Vector3 originalAngVel = rb.angularVelocity;

            // Helper local function to test an axis
            IEnumerator TestAxis(Vector3 localAxis, float angleDeg, System.Action<float> onMeasured)
            {
                // Apply rotation
                Quaternion delta = Quaternion.AngleAxis(angleDeg, localAxis);
                boat.localRotation = originalRot * delta;
                rb.rotation = boat.rotation;
                rb.angularVelocity = Vector3.zero;
                UnityEngine.Physics.SyncTransforms();


                // Let buoyancy respond
                for (int i = 0; i < settleFrames; i++)
                    yield return new WaitForFixedUpdate();

                // Measure restoring angular velocity magnitude
                float magnitude = rb.angularVelocity.magnitude;

                // Reset rotation
                boat.localRotation = originalRot;
                rb.rotation = boat.rotation;
                rb.angularVelocity = Vector3.zero;
                UnityEngine.Physics.SyncTransforms();


                onMeasured?.Invoke(magnitude);
            }

            float xResponse = 0f;
            float yResponse = 0f;
            float zResponse = 0f;

            // Test X
            yield return TestAxis(Vector3.right, testAngleDeg, m => xResponse = m);
            // Test Y
            yield return TestAxis(Vector3.up, testAngleDeg, m => yResponse = m);
            // Test Z
            yield return TestAxis(Vector3.forward, testAngleDeg, m => zResponse = m);

            // Decide roll axis by strongest restoring response
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

            // Now determine roll direction (+1 or -1)
            float rollDirection = 1f;

            {
                float posMag = 0f;
                float negMag = 0f;

                // +angle
                yield return TestAxis(rollAxis, testAngleDeg, m => posMag = m);
                // -angle
                yield return TestAxis(rollAxis, -testAngleDeg, m => negMag = m);

                rollDirection = posMag >= negMag ? 1f : -1f;
            }

            // For now, assume positive directions for pitch/yaw
            float pitchDirection = 1f;
            float yawDirection = 1f;

            bool isMirrored = false;

            // Restore original state
            boat.localRotation = originalRot;
            rb.rotation = boat.rotation;
            rb.angularVelocity = originalAngVel;
            UnityEngine.Physics.SyncTransforms();


            var profile = new VesselOrientationProfile
            {
                RollAxis = rollAxis,
                RollDirection = rollDirection,
                PitchAxis = pitchAxis,
                PitchDirection = pitchDirection,
                YawAxis = yawAxis,
                YawDirection = yawDirection,
                IsMirrored = isMirrored
            };

            onComplete?.Invoke(profile);
        }
    }
}