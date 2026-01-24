using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Axiom.Vessel.Diagnostics;

namespace Axiom.Diagnostics.Visualization
{
    public sealed class ForcesAndStabilityVisualizer : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        // REFERENCES
        // ─────────────────────────────────────────────────────────────
        [Header("References")]
        public VesselBootstrap bootstrap;
        public BoatCOB boatCOB;
        public BoatCOM boatCOM;
        public Rigidbody rb;

        // TEMPORARY: force providers (drag, lift, etc.)
        public MonoBehaviour[] forceProviders;

        // ─────────────────────────────────────────────────────────────
        // THRUST VISUALISATION (TRANSFORM + FORCE VECTOR)
        // ─────────────────────────────────────────────────────────────
        [Header("Thrust")]
        [Tooltip("Transform representing the thrust application point.")]
        [SerializeField] private Transform thrustPoint;

        [Tooltip("Current thrust force applied at the thrust point.")]
        [SerializeField] private Vector3 thrustForce;

        [Tooltip("Draw the thrust point marker.")]
        public bool drawThrustPoint = true;

        [Tooltip("Draw the thrust vector arrow.")]
        public bool drawThrustVector = true;

        [Tooltip("Draw the vessel forward direction from COM.")]
        public bool drawForward = true;

        public Color thrustColor = Color.red;

        /// <summary>Allows external systems (e.g., movement controller) to feed thrust force.</summary>
        public void SetThrustForce(Vector3 force) => thrustForce = force;

        // ─────────────────────────────────────────────────────────────
        // STABILITY VISUALISATION (GM / GZ)
        // ─────────────────────────────────────────────────────────────
        [Header("Stability")]
        public bool drawGZ = true;
        public bool drawGM = true;

        public float vectorScale = 1f;
        public Color gzColor = Color.green;
        public Color gmColor = Color.yellow;

#if UNITY_EDITOR
        private float lastHeelDeg;
        private float lastGZ;
        private float lastGM;
        private float highestGM;
#endif

        // ─────────────────────────────────────────────────────────────
        // FORCE PROVIDERS (drag, lift, etc.)
        // ─────────────────────────────────────────────────────────────
        [Header("Force Providers")]
        public float forceScale = 1f;
        public Color forceColor = Color.yellow;
        public bool drawForceLabels = true;

        // ─────────────────────────────────────────────────────────────
        // DRAW
        // ─────────────────────────────────────────────────────────────
        public void Draw()
        {
#if UNITY_EDITOR
            if (bootstrap == null || boatCOB == null || boatCOM == null || rb == null)
                return;

            Transform vessel = bootstrap.transform;

            Vector3 comWorld = rb.worldCenterOfMass;
            Vector3 cobWorld = boatCOB.COBWorldPosition;

            // Roll axis (signed)
            Vector3 rollAxisWorld =
                bootstrap.Orientation.RollAxis *
                bootstrap.Orientation.RollDirection;

            // Heel angle
            float heelDeg = GMGZUtility.ComputeHeelAngle(
                vessel,
                rollAxisWorld
            );

            // GZ
            float gz = GMGZUtility.ComputeGZ(
                comWorld,
                cobWorld,
                rollAxisWorld
            );

            // GM
            float gm = GMGZUtility.ComputeGM(
                heelDeg,
                gz
            );

            // Track values
            lastHeelDeg = heelDeg;
            lastGZ = gz;
            lastGM = gm;
            if (gm > highestGM) highestGM = gm;

            // ─────────────────────────────────────────────
            // THRUST POINT
            // ─────────────────────────────────────────────
            if (drawThrustPoint && thrustPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(thrustPoint.position, 0.12f);

                Gizmos.DrawLine(
                    thrustPoint.position + Vector3.up * 1.5f,
                    thrustPoint.position - Vector3.up * 1.5f
                );

                Gizmos.color = Color.white;
                Gizmos.DrawLine(thrustPoint.position, comWorld);

                Handles.color = Color.cyan;
                Handles.Label(
                    thrustPoint.position + Vector3.right * 0.2f,
                    "Thrust Point"
                );
            }

            // ─────────────────────────────────────────────
            // THRUST VECTOR
            // ─────────────────────────────────────────────
            if (drawThrustVector && thrustPoint != null)
            {
                Color orange = new Color(1f, 0.5f, 0f);
                Gizmos.color = orange;

                Gizmos.DrawLine(
                    thrustPoint.position,
                    thrustPoint.position + thrustForce * 0.01f
                );

                Handles.color = orange;
                Handles.Label(
                    thrustPoint.position + Vector3.up * 0.3f,
                    "Thrust Vector"
                );
            }

            // ─────────────────────────────────────────────
            // FORWARD DIRECTION 
            // ─────────────────────────────────────────────
            if (drawForward)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(comWorld, comWorld + vessel.forward * 3f);

                Handles.color = Color.blue;
                Handles.Label(
                    comWorld + vessel.forward * 3f,
                    "Forward"
                );
            }

            // ─────────────────────────────────────────────
            // GZ VISUALISATION
            // ─────────────────────────────────────────────
            if (drawGZ && gz > 0.0001f)
            {
                Vector3 lever = cobWorld - comWorld;
                Vector3 leverPerp = Vector3.ProjectOnPlane(lever, rollAxisWorld);
                Vector3 gzDir = leverPerp.normalized;

                Gizmos.color = gzColor;
                Gizmos.DrawLine(comWorld, comWorld + gzDir * gz * vectorScale);

                Handles.color = gzColor;
                Handles.Label(
                    comWorld + gzDir * gz * vectorScale,
                    $"GZ: {gz:F3} m"
                );
            }

            // ─────────────────────────────────────────────
            // GM VISUALISATION
            // ─────────────────────────────────────────────
            if (drawGM && gm > 0.0001f)
            {
                Vector3 heelPlaneNormal =
                    Vector3.Cross(rollAxisWorld, Vector3.up).normalized;

                if (heelPlaneNormal.sqrMagnitude < 0.0001f)
                    heelPlaneNormal = Vector3.forward;

                Vector3 M = comWorld + heelPlaneNormal * gm * vectorScale;

                Gizmos.color = gmColor;
                Gizmos.DrawLine(comWorld, M);

                Handles.color = gmColor;
                Handles.Label(M, $"GM: {gm:F3} m");
            }

            // ─────────────────────────────────────────────
            // FORCE PROVIDERS (drag, lift, etc.)
            // No IForceProvider system implemented yet.
            // This block stays dormant until real providers exist.
            // ─────────────────────────────────────────────
            if (forceProviders != null)
            {
                foreach (var provider in forceProviders)
                {
                    if (provider == null)
                        continue;

                    // No force provider interface implemented yet.
                    // Skip safely without errors.
                    continue;
                }
            }

#endif
        }
    }
}