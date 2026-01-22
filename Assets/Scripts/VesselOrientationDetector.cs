using System.Collections;
using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    /// <summary>
    /// Detects the vessel's canonical orientation profile using geometry
    /// (up from gravity, forward from hull extent) and then refines sign
    /// using buoyancy response. This avoids misclassifying pitch as roll.
    /// </summary>
    public sealed class VesselOrientationDetector
    {
        // --------------------------------------------------------------------
        // Dependencies
        // --------------------------------------------------------------------

        private readonly Transform boat;   // Vessel root transform
        private readonly Rigidbody rb;     // Rigidbody used for angular velocity sampling
        private readonly BoatCOB cob;      // Centre-of-buoyancy provider (for hull reference)

        public VesselOrientationDetector(Transform boat, Rigidbody rb, BoatCOB cob)
        {
            this.boat = boat;
            this.rb = rb;
            this.cob = cob;
        }

        // --------------------------------------------------------------------
        // Orientation Detection
        // --------------------------------------------------------------------

        public IEnumerator DetectOrientation(System.Action<VesselOrientationProfile> onComplete)
        {
            const float testAngleDeg = 10f;
            const int settleFrames = 5;

            // ------------------------------------------------------------
            // 1. Cache original state
            // ------------------------------------------------------------
            Quaternion originalRot = boat.localRotation;
            Vector3 originalAngVel = rb.angularVelocity;

            // ------------------------------------------------------------
            // 2. Determine canonical axes geometrically
            // ------------------------------------------------------------

            // 2.1 UP axis: align with world up in local space
            Vector3 localUp = boat.InverseTransformDirection(Vector3.up);
            Vector3 absUp = new Vector3(Mathf.Abs(localUp.x), Mathf.Abs(localUp.y), Mathf.Abs(localUp.z));

            Vector3 upAxisLocal;
            if (absUp.x >= absUp.y && absUp.x >= absUp.z)
                upAxisLocal = new Vector3(Mathf.Sign(localUp.x), 0f, 0f);
            else if (absUp.y >= absUp.x && absUp.y >= absUp.z)
                upAxisLocal = new Vector3(0f, Mathf.Sign(localUp.y), 0f);
            else
                upAxisLocal = new Vector3(0f, 0f, Mathf.Sign(localUp.z));

            // 2.2 FORWARD axis: longest hull extent in local space
            // Fallback: use boat's forward if no COB/hull available
            Vector3 forwardAxisLocal = Vector3.forward;

            // Try to find the hull mesh anywhere under the boat root
            MeshFilter[] filters = boat.GetComponentsInChildren<MeshFilter>();

            MeshFilter largest = null;
            float largestVolume = 0f;

            foreach (var f in filters)
            {
                if (f.sharedMesh == null)
                    continue;

                Bounds b = f.sharedMesh.bounds;
                float volume = b.size.x * b.size.y * b.size.z;

                if (volume > largestVolume)
                {
                    largestVolume = volume;
                    largest = f;
                }
            }

            if (largest != null)
            {
                Bounds b = largest.sharedMesh.bounds;
                Vector3 size = b.size;

                // Ignore vertical component when choosing forward
                size = new Vector3(
                    Mathf.Abs(size.x) * (Mathf.Abs(upAxisLocal.x) < 0.9f ? 1f : 0f),
                    Mathf.Abs(size.y) * (Mathf.Abs(upAxisLocal.y) < 0.9f ? 1f : 0f),
                    Mathf.Abs(size.z) * (Mathf.Abs(upAxisLocal.z) < 0.9f ? 1f : 0f)
                );

                if (size.x >= size.z)
                    forwardAxisLocal = new Vector3(1f, 0f, 0f);
                else
                    forwardAxisLocal = new Vector3(0f, 0f, 1f);
            }

            // Ensure forward is orthogonal to up
            forwardAxisLocal = Vector3.ProjectOnPlane(forwardAxisLocal, upAxisLocal).normalized;
            if (forwardAxisLocal.sqrMagnitude < 0.5f)
                forwardAxisLocal = Vector3.forward; // fallback

            // 2.3 ROLL axis = forward
            Vector3 rollAxisLocal = forwardAxisLocal.normalized;

            // 2.4 PITCH axis = beam = cross(up, roll)
            Vector3 pitchAxisLocal = Vector3.Cross(upAxisLocal, rollAxisLocal).normalized;

            // 2.5 YAW axis = up
            Vector3 yawAxisLocal = upAxisLocal.normalized;

            // ------------------------------------------------------------
            // Local helper: test sign for a given axis
            // ------------------------------------------------------------
            IEnumerator TestAxisSign(Vector3 localAxis, System.Action<float> onSign)
            {
                float posMag = 0f;
                float negMag = 0f;

                IEnumerator TestOnce(float angleDeg, System.Action<float> onMeasured)
                {
                    Quaternion delta = Quaternion.AngleAxis(angleDeg, localAxis);
                    boat.localRotation = originalRot * delta;

                    rb.rotation = boat.rotation;
                    rb.angularVelocity = Vector3.zero;
                    UnityEngine.Physics.SyncTransforms();

                    for (int i = 0; i < settleFrames; i++)
                        yield return new WaitForFixedUpdate();

                    float mag = rb.angularVelocity.magnitude;

                    boat.localRotation = originalRot;
                    rb.rotation = boat.rotation;
                    rb.angularVelocity = Vector3.zero;
                    UnityEngine.Physics.SyncTransforms();

                    onMeasured?.Invoke(mag);
                }

                yield return TestOnce(+testAngleDeg, m => posMag = m);
                yield return TestOnce(-testAngleDeg, m => negMag = m);

                float sign = posMag >= negMag ? 1f : -1f;
                onSign?.Invoke(sign);
            }

            // ------------------------------------------------------------
            // 3. Determine directions (+1 / -1) using buoyancy response
            // ------------------------------------------------------------
            float rollDirection = 1f;
            float pitchDirection = 1f;
            float yawDirection = 1f;

            // Roll direction
            yield return TestAxisSign(rollAxisLocal, s => rollDirection = s);

            // (Optional) pitch/yaw direction detection – keep +1 for now
            // yield return TestAxisSign(pitchAxisLocal, s => pitchDirection = s);
            // yield return TestAxisSign(yawAxisLocal, s => yawDirection = s);

            bool isMirrored = false; // future extension

            // ------------------------------------------------------------
            // 4. Restore original state
            // ------------------------------------------------------------
            boat.localRotation = originalRot;
            rb.rotation = boat.rotation;
            rb.angularVelocity = originalAngVel;
            UnityEngine.Physics.SyncTransforms();

            // ------------------------------------------------------------
            // 5. Build profile
            // ------------------------------------------------------------
            var profile = new VesselOrientationProfile
            {
                RollAxis = rollAxisLocal,
                RollDirection = rollDirection,

                PitchAxis = pitchAxisLocal,
                PitchDirection = pitchDirection,

                YawAxis = yawAxisLocal,
                YawDirection = yawDirection,

                IsMirrored = isMirrored,
                IsValid = true
            };

            onComplete?.Invoke(profile);
        }
    }
}