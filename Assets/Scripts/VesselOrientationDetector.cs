using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    /// <summary>
    /// Determines the vessel's canonical orientation axes (roll, pitch, yaw)
    /// using geometry and physics-based sign detection.
    /// 
    /// Standalone class — not a MonoBehaviour.
    /// Called by VesselBootstrap or the setup wizard.
    /// </summary>
    public sealed class VesselOrientationDetector
    {
        private readonly Transform boat;
        private readonly Rigidbody rb;

        private readonly List<string> warnings = new List<string>();

        private void Warn(string message)
        {
            warnings.Add(message);
            Debug.LogWarning($"[VesselOrientation] {message}");
        }


        public VesselOrientationDetector(Transform boatRoot, Rigidbody rigidbody)
        {
            boat = boatRoot;
            rb = rigidbody;
        }

        /// <summary>
        /// Runs the full orientation detection pipeline.
        /// </summary>
        public IEnumerator DetectOrientation(System.Action<VesselOrientationProfile> onComplete)
        {
            // Cache original state
            Quaternion originalLocalRotation = boat.localRotation;
            Vector3 originalAngVel = rb.angularVelocity;

            // 1. Detect UP
            Vector3 upAxisLocal = DetectUpAxisLocal();

            // 2. Detect FORWARD
            Vector3 forwardAxisLocal = DetectForwardAxisLocal(upAxisLocal);

            // 3. Derive BEAM
            Vector3 beamAxisLocal = DeriveBeamAxisLocal(upAxisLocal, forwardAxisLocal);

            // 4. Assign canonical axes
            Vector3 rollAxisLocal = forwardAxisLocal.normalized;
            Vector3 pitchAxisLocal = beamAxisLocal.normalized;
            Vector3 yawAxisLocal = upAxisLocal.normalized;

            // 5. Detect signs
            float rollSign = 1f;
            float pitchSign = 1f;
            float yawSign = 1f;

            if (rb == null)
            {
                Warn("Rigidbody missing — roll sign detection skipped, using +1.");
            }
            else
            {
                yield return DetectRollSign(rollAxisLocal, originalLocalRotation, s => rollSign = s);
            }

            // Placeholders for now
            yield return DetectPitchSign(pitchAxisLocal, originalLocalRotation, s => pitchSign = s);
            yield return DetectYawSign(yawAxisLocal, originalLocalRotation, s => yawSign = s);

            // 6. Restore original state
            boat.localRotation = originalLocalRotation;
            rb.angularVelocity = originalAngVel;
            UnityEngine.Physics.SyncTransforms();

            // 7. Build profile
            var profile = new VesselOrientationProfile
            {
                RollAxis = rollAxisLocal,
                RollDirection = rollSign,

                PitchAxis = pitchAxisLocal,
                PitchDirection = pitchSign,

                YawAxis = yawAxisLocal,
                YawDirection = yawSign,

                IsMirrored = false, // future extension
                IsValid = true,
                Warnings = warnings.ToArray()
            };

            onComplete?.Invoke(profile);
        }

        private Vector3 DetectUpAxisLocal()
        {
            // World up expressed in boat local space
            Vector3 localUp = boat.InverseTransformDirection(Vector3.up);

            // Take absolute values to find dominant axis
            Vector3 absUp = new Vector3(
                Mathf.Abs(localUp.x),
                Mathf.Abs(localUp.y),
                Mathf.Abs(localUp.z)
            );

            Vector3 upAxisLocal;

            if (absUp.x >= absUp.y && absUp.x >= absUp.z)
                upAxisLocal = new Vector3(Mathf.Sign(localUp.x), 0f, 0f);
            else if (absUp.y >= absUp.x && absUp.y >= absUp.z)
                upAxisLocal = new Vector3(0f, Mathf.Sign(localUp.y), 0f);
            else
                upAxisLocal = new Vector3(0f, 0f, Mathf.Sign(localUp.z));

            return upAxisLocal.normalized;
        }

        private Vector3 DetectForwardAxisLocal(Vector3 upAxisLocal)
        {
            // Find the mesh with the largest bounds volume
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

            // Fallback if no mesh found
            if (largest == null)
                return Vector3.forward;

            // Get the mesh bounds
            Bounds bounds = largest.sharedMesh.bounds;
            Vector3 size = bounds.size;

            // Zero out the axis that matches UP (we only want horizontal)
            Vector3 horizontalSize = new Vector3(
                Mathf.Abs(size.x) * (Mathf.Abs(upAxisLocal.x) < 0.9f ? 1f : 0f),
                Mathf.Abs(size.y) * (Mathf.Abs(upAxisLocal.y) < 0.9f ? 1f : 0f),
                Mathf.Abs(size.z) * (Mathf.Abs(upAxisLocal.z) < 0.9f ? 1f : 0f)
            );

            // Choose the longest horizontal axis
            Vector3 forwardLocal;

            if (horizontalSize.x >= horizontalSize.z)
                forwardLocal = new Vector3(1f, 0f, 0f);
            else
                forwardLocal = new Vector3(0f, 0f, 1f);

            // Ensure orthogonality with UP
            forwardLocal = Vector3.ProjectOnPlane(forwardLocal, upAxisLocal).normalized;

            // Fallback if projection collapses
            if (forwardLocal.sqrMagnitude < 0.5f)
                forwardLocal = Vector3.forward;

            return forwardLocal.normalized;
        }

        private Vector3 DeriveBeamAxisLocal(Vector3 upAxisLocal, Vector3 forwardAxisLocal)
        {
            // Beam = cross(UP, FORWARD)
            Vector3 beam = Vector3.Cross(upAxisLocal, forwardAxisLocal).normalized;

            // Fallback if something went wrong
            if (beam.sqrMagnitude < 0.5f)
            {
                Warn("Beam axis degenerate — check UP and FORWARD axes.");
                // Try the opposite cross just in case
                beam = Vector3.Cross(forwardAxisLocal, upAxisLocal).normalized;
            }

            return beam;
        }

        private IEnumerator DetectRollSign(Vector3 rollAxisLocal, Quaternion originalLocalRotation, System.Action<float> onSign)
        {
            const float testAngleDeg = 10f;
            const int settleFrames = 5;

            float posMag = 0f;
            float negMag = 0f;

            IEnumerator TestOnce(float angleDeg, System.Action<float> onMeasured)
            {
                Quaternion delta = Quaternion.AngleAxis(angleDeg, rollAxisLocal);
                boat.localRotation = originalLocalRotation * delta;

                rb.rotation = boat.rotation;
                rb.angularVelocity = Vector3.zero;
                UnityEngine.Physics.SyncTransforms();

                for (int i = 0; i < settleFrames; i++)
                    yield return new WaitForFixedUpdate();

                float mag = rb.angularVelocity.magnitude;

                boat.localRotation = originalLocalRotation;
                rb.rotation = boat.rotation;
                rb.angularVelocity = Vector3.zero;
                UnityEngine.Physics.SyncTransforms();

                onMeasured?.Invoke(mag);
            }

            yield return TestOnce(+testAngleDeg, m => posMag = m);
            yield return TestOnce(-testAngleDeg, m => negMag = m);

            float sign = 1f;

            if (Mathf.Approximately(posMag, negMag))
            {
                Warn("Roll sign detection inconclusive — using +1. Check Rigidbody and buoyancy setup.");
                sign = 1f;
            }
            else
            {
                sign = posMag >= negMag ? 1f : -1f;
            }

            onSign?.Invoke(sign);
        }

        private IEnumerator DetectPitchSign(Vector3 pitchAxisLocal, Quaternion originalLocalRotation, System.Action<float> onSign)
        {
            // Placeholder — always +1 for now
            onSign?.Invoke(1f);
            yield break;
        }

        private IEnumerator DetectYawSign(Vector3 yawAxisLocal, Quaternion originalLocalRotation, System.Action<float> onSign)
        {
            // Placeholder — always +1 for now
            onSign?.Invoke(1f);
            yield break;
        }
    }
}