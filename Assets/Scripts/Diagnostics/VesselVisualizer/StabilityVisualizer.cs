using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Axiom.Vessel.Diagnostics
{
    public sealed class StabilityVisualizer : MonoBehaviour
    {
        [Header("References")]
        public VesselBootstrap bootstrap;
        public BoatCOB boatCOB;
        public BoatCOM boatCOM;
        public Rigidbody rb;

        [Header("Toggles")]
        public bool drawGZ = true;
        public bool drawGM = true;

        [Header("Visual Settings")]
        public float vectorScale = 1f;
        public Color gzColor = Color.green;
        public Color gmColor = Color.yellow;

#if UNITY_EDITOR
        private float lastHeelDeg;
        private float lastGZ;
        private float lastGM;
        private float highestGM;
#endif

        public void Draw()
        {
#if UNITY_EDITOR
            if (bootstrap == null || boatCOB == null || boatCOM == null || rb == null)
                return;

            // Authoritative vessel orientation
            Transform vessel = bootstrap.transform;

            // World positions
            Vector3 comWorld = rb.worldCenterOfMass;
            Vector3 cobWorld = boatCOB.COBWorldPosition;

            // Roll axis (signed)
            Vector3 rollAxisWorld =
                bootstrap.Orientation.RollAxis *
                bootstrap.Orientation.RollDirection;

            // Heel angle (deg)
            float heelDeg = GMGZUtility.ComputeHeelAngle(
                vessel,
                rollAxisWorld
            );

            // GZ (m)
            float gz = GMGZUtility.ComputeGZ(
                comWorld,
                cobWorld,
                rollAxisWorld
            );

            // GM (m)
            float gm = GMGZUtility.ComputeGM(
                heelDeg,
                gz
            );

#if UNITY_EDITOR
            lastHeelDeg = heelDeg;
            lastGZ = gz;
            lastGM = gm;
            if (gm > highestGM) highestGM = gm;
#endif

            // ─────────────────────────────────────────────
            // Draw GZ
            // ─────────────────────────────────────────────
            if (drawGZ && gz > 0.0001f)
            {
                Vector3 lever = cobWorld - comWorld;
                Vector3 leverPerp = Vector3.ProjectOnPlane(lever, rollAxisWorld);
                Vector3 gzDir = leverPerp.normalized;

                Gizmos.color = gzColor;
                Gizmos.DrawLine(comWorld, comWorld + gzDir * gz * vectorScale);

                Handles.color = gzColor;
                Handles.Label(comWorld + gzDir * gz * vectorScale, $"GZ: {gz:F3} m");
            }

            // ─────────────────────────────────────────────
            // Draw GM
            // ─────────────────────────────────────────────
            if (drawGM && gm > 0.0001f)
            {
                // Heel plane normal (same as monolith)
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
#endif
        }
    }
}