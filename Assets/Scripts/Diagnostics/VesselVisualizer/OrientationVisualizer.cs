using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Axiom.Diagnostics.Visualization
{
    public sealed class OrientationVisualizer : MonoBehaviour
    {
        [Header("References")]
        public VesselBootstrap bootstrap;
        public Rigidbody rb;

        [Header("Toggles")]
        public bool drawRollAxis = true;
        public bool drawRollRate = true;

        [Header("Settings")]
        [Tooltip("Half-length of the roll axis line in meters.")]
        public float rollAxisHalfLength = 2f;

        [Tooltip("Scale factor for the roll rate vector.")]
        public float rollRateScale = 0.5f;

        public void Draw(Vector3 comWorld)
        {
#if UNITY_EDITOR
            if (bootstrap == null || rb == null)
                return;

            Vector3 rollAxis = bootstrap.Orientation.RollAxis * bootstrap.Orientation.RollDirection;

            // ─────────────────────────────────────────────
            // ROLL AXIS
            // ─────────────────────────────────────────────
            if (drawRollAxis)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(
                    comWorld - rollAxis * rollAxisHalfLength,
                    comWorld + rollAxis * rollAxisHalfLength
                );

                Handles.color = Color.white;
                Handles.Label(comWorld + rollAxis * rollAxisHalfLength, "Roll Axis");
            }

            // ─────────────────────────────────────────────
            // ROLL RATE
            // ─────────────────────────────────────────────
            if (drawRollRate)
            {
                float rollRate = Vector3.Dot(rb.angularVelocity, rollAxis);
                Vector3 rollRateVec = rollAxis * rollRate * rollRateScale;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(comWorld, comWorld + rollRateVec);

                Handles.color = Color.red;
                Handles.Label(comWorld + rollRateVec, $"Roll Rate: {(rollRate * Mathf.Rad2Deg):F1} °/s");
            }
#endif
        }
    }
}