using UnityEngine;
using Axiom.Vessel.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Axiom.Vessel.Diagnostics
{
    /// <summary>
    /// Unified visualisation layer for vessel physics diagnostics.
    /// Draws:
    /// - Neutral band
    /// - COM height
    /// - COB position
    /// - Righting moment
    /// - Thrust point + thrust vector
    /// - Hull bottom reference
    /// - Forward direction (edit mode)
    /// - Velocity + slip (play mode only)
    /// - Per‑probe buoyancy vectors (play mode)
    /// - Waterline plane (fitted from probes)
    /// - GM (metacentric height) indicator
    /// - GZ (righting arm) indicator
    /// - Roll axis + roll‑rate arrow
    ///
    /// All visuals are non-intrusive and editor-only.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BoatPhysicsVisualizer : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        // CORE REFERENCES
        // ─────────────────────────────────────────────────────────────

        [Tooltip("Reference to the BoatCOM authority on this vessel.")]
        public BoatCOM boatCOM;

        [Tooltip("Reference to the BoatCOB authority on this vessel.")]
        public BoatCOB boatCOB;

        [Tooltip("Horizontal line length in meters for COM and neutral band markers.")]
        public float lineWidth = 1.0f;


        // ─────────────────────────────────────────────────────────────
        // OPTIONAL REFERENCES FOR EXTENDED VISUALS
        // ─────────────────────────────────────────────────────────────

        [Header("Optional References")]
        [Tooltip("Rigidbody used for velocity, slip, and roll diagnostics.")]
        [SerializeField] private Rigidbody rb;

        [Tooltip("Transform representing the thrust application point.")]
        [SerializeField] private Transform thrustPoint;

        [Tooltip("Current thrust force applied at the thrust point.")]
        [SerializeField] private Vector3 thrustForce;

        [Tooltip("Local Y offset of the hull bottom reference point.")]
        [SerializeField] private float hullBottomLocalY = 0f;

        [Tooltip("Draw the Centre of Buoyancy marker.")]
        public bool drawCOB = true;

        [Tooltip("Draw the righting moment torque arrow (edit mode only).")]
        public bool drawRightingMoment = true;


        /// <summary>
        /// Allows external systems (e.g., movement controller) to feed thrust force.
        /// </summary>
        public void SetThrustForce(Vector3 force) => thrustForce = force;


        // ─────────────────────────────────────────────────────────────
        // BUOYANCY / WATERLINE VISUALS
        // ─────────────────────────────────────────────────────────────

        [Header("Buoyancy & Waterline Visuals")]
        [Tooltip("Enable drawing of per‑probe buoyancy vectors.")]
        [SerializeField] private bool drawBuoyancyProbes = true;

        [Tooltip("Enable drawing of the fitted waterline plane from probe data.")]
        [SerializeField] private bool drawWaterlinePlane = true;

        [Tooltip("Buoyancy system providing density and strength.")]
        [SerializeField] private Buoyancy buoyancy;

        [Tooltip("Water probe sampler providing per‑probe water data.")]
        [SerializeField] private WaterProbeSampler sampler;

        [Tooltip("Color of the buoyancy force vector at each probe.")]
        [SerializeField] private Color buoyancyForceColor = Color.cyan;

        [Tooltip("Scale factor for drawing buoyancy force vectors.")]
        [SerializeField] private float buoyancyForceScale = 0.001f;

        [Tooltip("Color for shallow probes (small depth).")]
        [SerializeField] private Color probeDepthColorShallow = Color.blue;

        [Tooltip("Color for deep probes (larger depth).")]
        [SerializeField] private Color probeDepthColorDeep = Color.red;

        [Tooltip("Depth in meters mapped to 1.0 in the depth gradient.")]
        [SerializeField] private float probeDepthMaxForColor = 2f;

        [Tooltip("Color of the fitted waterline plane grid.")]
        [SerializeField] private Color waterlineColor = new Color(0.2f, 0.6f, 1f, 0.6f);

        [Tooltip("Half-size of the waterline plane grid in meters.")]
        [SerializeField] private float waterlineHalfSize = 3f;

        [Tooltip("Number of grid lines per side for the waterline plane.")]
        [SerializeField] private int waterlineGridResolution = 4;


        // ─────────────────────────────────────────────────────────────
        // STABILITY / ROLL DIAGNOSTICS
        // ─────────────────────────────────────────────────────────────

        [Header("Stability & Roll Diagnostics")]
        [Tooltip("Draw GM (metacentric height) indicator.")]
        [SerializeField] private bool drawGM = true;

        [Tooltip("Draw GZ (righting arm) indicator.")]
        [SerializeField] private bool drawGZ = true;

        [Tooltip("Draw the roll axis line through COM.")]
        [SerializeField] private bool drawRollAxis = true;

        [Tooltip("Draw roll‑rate arrow (angular velocity around roll axis).")]
        [SerializeField] private bool drawRollRate = true;

        [Tooltip("Scale factor for drawing roll‑rate arrow.")]
        [SerializeField] private float rollRateScale = 0.5f;


        // ─────────────────────────────────────────────────────────────
        // TOGGLES
        // ─────────────────────────────────────────────────────────────

        [Header("Gizmo Toggles")]
        [Tooltip("Draw the thrust point marker.")]
        public bool drawThrustPoint = true;

        [Tooltip("Draw the thrust vector arrow.")]
        public bool drawThrustVector = true;

        [Tooltip("Draw the hull bottom reference marker.")]
        public bool drawHullBottom = true;

        [Tooltip("Draw the forward direction (edit mode).")]
        public bool drawForward = true;

        [Tooltip("Draw the velocity vector (play mode only).")]
        public bool drawVelocity = true;

        [Tooltip("Draw the lateral slip vector (play mode only).")]
        public bool drawSlip = true;

        // GM tracking
        private float highestGM = 0f;

#if UNITY_EDITOR
        // Overlay state
        private float lastGM = 0f;
        private float lastHeelDeg = 0f;
        private float lastGZ = 0f;
        private float lastRollRateDeg = 0f;
#endif

        private void Reset()
        {
            if (boatCOM == null)
                boatCOM = GetComponent<BoatCOM>();

            if (boatCOB == null)
                boatCOB = GetComponent<BoatCOB>();

            if (rb == null)
                rb = GetComponent<Rigidbody>();

            if (buoyancy == null)
                buoyancy = GetComponent<Buoyancy>();

            if (sampler == null)
                sampler = GetComponent<WaterProbeSampler>();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawSceneOverlay;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneOverlay;


        }

        private void DrawSceneOverlay(SceneView sceneView)
        {
            if (!SceneView.currentDrawingSceneView.camera.name.Contains("Scene"))
                return;

            Handles.BeginGUI();

            const float width = 260f;
            const float height = 90f;
            float x = (sceneView.position.width - width) * 0.5f;
            GUILayout.BeginArea(new Rect(x, 10, width, height));
            GUI.color = Color.yellow;

            GUILayout.Label($"Heel: {lastHeelDeg:F1}°");

            if (drawGM)
            {
                if (Mathf.Abs(lastHeelDeg) >= 5f)
                    GUILayout.Label($"GM: {lastGM:F3} m   (max {highestGM:F3} m)");
                else
                    GUILayout.Label("GM: — (heel < 5°)");
            }


            if (drawGZ)
                GUILayout.Label($"GZ: {lastGZ:F3} m");

            if (drawRollRate && rb != null)
                GUILayout.Label($"Roll rate: {lastRollRateDeg:F1} °/s");

            GUILayout.EndArea();

            Handles.EndGUI();
        }
#endif

        // ─────────────────────────────────────────────────────────────
        // GIZMO DRAWING
        // ─────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (boatCOM == null)
                boatCOM = GetComponent<BoatCOM>();

            if (boatCOB == null)
                boatCOB = GetComponent<BoatCOB>();

            if (boatCOM == null || boatCOB == null)
                return;

            // Base reference
            Vector3 basePos = transform.position;

            // COM + Neutral band heights
            float comY = boatCOM.COMHeight != 0f ? boatCOM.COMHeight : boatCOM.comHeight;
            float neutralY = boatCOM.NeutralBandMin;

            Vector3 neutralPos = basePos + Vector3.up * neutralY;
            Vector3 comPos = basePos + Vector3.up * comY;

            Vector3 left = Vector3.left * (lineWidth * 0.5f);
            Vector3 right = Vector3.right * (lineWidth * 0.5f);


            // ─────────────────────────────────────────────────────────────
            // NEUTRAL BAND
            // ─────────────────────────────────────────────────────────────
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(neutralPos + left, neutralPos + right);

            Handles.color = Color.yellow;
            Handles.Label(neutralPos + Vector3.right * (lineWidth * 0.6f), "Neutral Band");


            // ─────────────────────────────────────────────────────────────
            // COM LINE
            // ─────────────────────────────────────────────────────────────
            bool valid = comY >= neutralY;
            Gizmos.color = valid ? Color.green : Color.red;
            Gizmos.DrawLine(comPos + left, comPos + right);

            Handles.color = valid ? Color.green : Color.red;
            Handles.Label(comPos + Vector3.right * (lineWidth * 0.6f), "COM");


            // ─────────────────────────────────────────────────────────────
            // CENTRE OF BUOYANCY (COB)
            // ─────────────────────────────────────────────────────────────
            Vector3 cobPosWorld = boatCOB.COBWorldPosition;

            if (drawCOB)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(cobPosWorld, 0.12f);

                Gizmos.color = Color.white;
                Gizmos.DrawLine(comPos, cobPosWorld);

                Handles.color = Color.blue;
                Handles.Label(cobPosWorld + Vector3.right * 0.2f, "COB");
            }


            // ─────────────────────────────────────────────────────────────
            // RIGHTING MOMENT (edit mode only)
            // ─────────────────────────────────────────────────────────────
            if (drawRightingMoment)
            {
                Vector3 leverArm = cobPosWorld - comPos;
                Vector3 buoyancyDir = Vector3.up;

                Vector3 rightingTorque = Vector3.Cross(leverArm, buoyancyDir);

                if (rightingTorque.sqrMagnitude > 0.0001f)
                {
                    Vector3 torqueDir = rightingTorque.normalized;

                    Gizmos.color = new Color(0.8f, 0.3f, 1f);
                    Gizmos.DrawLine(comPos, comPos + torqueDir * 2f);

                    Handles.color = new Color(0.8f, 0.3f, 1f);
                    Handles.Label(comPos + torqueDir * 2f, "Righting Moment");
                }
            }


            // ─────────────────────────────────────────────────────────────
            // THRUST POINT
            // ─────────────────────────────────────────────────────────────
            if (drawThrustPoint && thrustPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(thrustPoint.position, 0.12f);

                Gizmos.DrawLine(thrustPoint.position + Vector3.up * 1.5f,
                                thrustPoint.position - Vector3.up * 1.5f);

                Gizmos.color = Color.white;
                Gizmos.DrawLine(thrustPoint.position, comPos);

                Handles.color = Color.cyan;
                Handles.Label(thrustPoint.position + Vector3.right * 0.2f, "Thrust Point");
            }


            // ─────────────────────────────────────────────────────────────
            // THRUST VECTOR
            // ─────────────────────────────────────────────────────────────
            if (drawThrustVector && thrustPoint != null)
            {
                Color orange = new Color(1f, 0.5f, 0f);
                Gizmos.color = orange;

                Gizmos.DrawLine(thrustPoint.position,
                                thrustPoint.position + thrustForce * 0.01f);

                Handles.color = orange;
                Handles.Label(thrustPoint.position + Vector3.up * 0.3f, "Thrust Vector");
            }


            // ─────────────────────────────────────────────────────────────
            // HULL BOTTOM
            // ─────────────────────────────────────────────────────────────
            if (drawHullBottom)
            {
                Vector3 hullBottom = transform.TransformPoint(
                    new Vector3(0f, hullBottomLocalY, 0f)
                );

                Gizmos.color = Color.grey;
                Gizmos.DrawCube(hullBottom, new Vector3(0.15f, 0.02f, 0.15f));

                Gizmos.DrawLine(hullBottom, comPos);

                Handles.color = Color.grey;
                Handles.Label(hullBottom + Vector3.right * 0.2f, "Hull Bottom");
            }


            // ─────────────────────────────────────────────────────────────
            // FORWARD DIRECTION (EDIT MODE)
            // ─────────────────────────────────────────────────────────────
            if (drawForward)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(comPos, comPos + transform.forward * 3f);

                Handles.color = Color.blue;
                Handles.Label(comPos + transform.forward * 3f, "Forward");
            }


            // ─────────────────────────────────────────────────────────────
            // VELOCITY + SLIP (PLAY MODE ONLY)
            // ─────────────────────────────────────────────────────────────
            if (drawVelocity && rb != null && Application.isPlaying)
            {
                Vector3 vel = rb.linearVelocity;

                // Velocity vector
                if (vel.sqrMagnitude > 0.01f)
                {
                    Color lime = new Color(0.7f, 1f, 0f);
                    Gizmos.color = lime;
                    Gizmos.DrawLine(comPos, comPos + vel.normalized * 3f);
                }

                // Slip vector
                if (drawSlip && vel.sqrMagnitude > 0.01f)
                {
                    Vector3 localVel = transform.InverseTransformDirection(vel);
                    Vector3 lateral = new Vector3(localVel.x, 0f, 0f);
                    Vector3 lateralWorld = transform.TransformDirection(lateral);

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(comPos, comPos + lateralWorld * 2f);
                }
            }

            // ─────────────────────────────────────────────────────────────
            // BUOYANCY PROBE VECTORS (PLAY MODE)
            // ─────────────────────────────────────────────────────────────
            if (drawBuoyancyProbes && Application.isPlaying)
                DrawBuoyancyProbes();


            // ─────────────────────────────────────────────────────────────
            // WATERLINE PLANE (PLAY MODE)
            // ─────────────────────────────────────────────────────────────
            if (drawWaterlinePlane && Application.isPlaying)
                DrawWaterlinePlane();


            // ─────────────────────────────────────────────────────────────
            // STABILITY & ROLL DIAGNOSTICS (PLAY MODE)
            // ─────────────────────────────────────────────────────────────
            if (Application.isPlaying && rb != null)
            {
                if (drawRollAxis || drawRollRate)
                    DrawRollDiagnostics(comPos);

                if (drawGM || drawGZ)
                    DrawStabilityDiagnostics(comPos, cobPosWorld);
            }

#endif
        }


        // ─────────────────────────────────────────────────────────────
        // BUOYANCY PROBE VISUALISATION
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws per‑probe buoyancy vectors and depth‑based coloring.
        /// Purely diagnostic: reads from Buoyancy + WaterProbeSampler, applies no forces.
        /// </summary>
        private void DrawBuoyancyProbes()
        {
            if (buoyancy == null || sampler == null)
                return;

            sampler.GetProbeData(out bool[] valid, out float[] heights, out Vector3[] normals, out Transform[] points);

            if (valid == null || heights == null || normals == null || points == null)
                return;

            float buoyancyStrength = buoyancy.BuoyancyStrength;
            if (buoyancyStrength <= 0f)
                return;

            for (int i = 0; i < points.Length; i++)
            {
                if (!valid[i])
                    continue;

                Transform p = points[i];
                float waterY = heights[i];
                float depth = waterY - p.position.y;

                if (depth <= 0f)
                    continue;

                // Depth‑based color (heatmap)
                float depth01 = probeDepthMaxForColor > 0f
                    ? Mathf.Clamp01(depth / probeDepthMaxForColor)
                    : 1f;

                Color depthColor = Color.Lerp(probeDepthColorShallow, probeDepthColorDeep, depth01);

                // Reconstruct force magnitude using the same linear model as Buoyancy:
                // F = depth * buoyancyStrength
                float forceMagnitude = depth * buoyancyStrength;
                Vector3 forceVec = Vector3.up * forceMagnitude * buoyancyForceScale;

                // Draw probe marker
                Debug.DrawLine(p.position, p.position + Vector3.up * 0.05f, depthColor);

                // Draw buoyancy force vector
                Debug.DrawLine(p.position, p.position + forceVec, buoyancyForceColor);
            }
        }


        // ─────────────────────────────────────────────────────────────
        // WATERLINE PLANE (FITTED FROM PROBES)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws a fitted waterline plane using three valid probes.
        /// Uses probe positions and water heights to define a plane, then draws a grid.
        /// </summary>
        private void DrawWaterlinePlane()
        {
            if (sampler == null)
                return;

            sampler.GetProbeData(out bool[] valid, out float[] heights, out Vector3[] normals, out Transform[] points);

            if (valid == null || heights == null || points == null)
                return;

            // Collect up to three valid water points
            Vector3[] waterPoints = new Vector3[3];
            int count = 0;

            for (int i = 0; i < points.Length && count < 3; i++)
            {
                if (!valid[i])
                    continue;

                Transform p = points[i];
                float waterY = heights[i];

                waterPoints[count] = new Vector3(p.position.x, waterY, p.position.z);
                count++;
            }

            if (count < 3)
                return;

            Vector3 p0 = waterPoints[0];
            Vector3 p1 = waterPoints[1];
            Vector3 p2 = waterPoints[2];

            Vector3 v1 = p1 - p0;
            Vector3 v2 = p2 - p0;
            Vector3 normal = Vector3.Cross(v1, v2).normalized;

            if (normal.sqrMagnitude < 0.0001f)
                return;

            // Build a local basis for the plane (right/forward on the plane)
            Vector3 planeRight = Vector3.Cross(Vector3.up, normal).normalized;
            if (planeRight.sqrMagnitude < 0.0001f)
                planeRight = Vector3.right;

            Vector3 planeForward = Vector3.Cross(normal, planeRight).normalized;

            Gizmos.color = waterlineColor;

            int steps = Mathf.Max(1, waterlineGridResolution);
            float step = waterlineHalfSize * 2f / steps;

            // Draw grid lines on the plane
            for (int i = 0; i <= steps; i++)
            {
                float offset = -waterlineHalfSize + i * step;

                Vector3 start1 = p0 + planeRight * -waterlineHalfSize + planeForward * offset;
                Vector3 end1 = p0 + planeRight * waterlineHalfSize + planeForward * offset;

                Vector3 start2 = p0 + planeForward * -waterlineHalfSize + planeRight * offset;
                Vector3 end2 = p0 + planeForward * waterlineHalfSize + planeRight * offset;

                Gizmos.DrawLine(start1, end1);
                Gizmos.DrawLine(start2, end2);
            }

#if UNITY_EDITOR
            Handles.color = waterlineColor;
            Handles.Label(p0 + normal * 0.2f, "Waterline Plane");
#endif
        }


        // ─────────────────────────────────────────────────────────────
        // STABILITY DIAGNOSTICS (GM / GZ)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws GM (metacentric height) and GZ (righting arm) indicators.
        /// Uses COM, COB, and hull orientation to approximate small-angle stability.
        /// GM is only computed when heel angle is sufficiently non-zero.
        /// </summary>
        private void DrawStabilityDiagnostics(Vector3 comPos, Vector3 cobPosWorld)
        {
            // Roll axis is local X in world space
            Vector3 rollAxis = transform.right;

            // Heel angle: angle between vessel up and world up, around roll axis
            Vector3 up = transform.up;
            float heelSign = Mathf.Sign(Vector3.Dot(Vector3.Cross(Vector3.up, up), rollAxis));
            float heelAngleRad = Mathf.Acos(Mathf.Clamp(Vector3.Dot(Vector3.up, up), -1f, 1f));
            heelAngleRad *= heelSign;

            float heelAngleDeg = heelAngleRad * Mathf.Rad2Deg;
#if UNITY_EDITOR
            lastHeelDeg = heelAngleDeg;
#endif

            // Lever from COM to COB
            Vector3 lever = cobPosWorld - comPos;

            // Horizontal righting arm (GZ) = projection of lever onto plane perpendicular to roll axis
            Vector3 leverPerp = Vector3.ProjectOnPlane(lever, rollAxis);
            float GZ = leverPerp.magnitude;
#if UNITY_EDITOR
            lastGZ = GZ;
#endif

            // ─────────────────────────────────────────────────────────────
            // GZ (Righting Arm)
            // ─────────────────────────────────────────────────────────────
            if (drawGZ)
            {
                if (GZ > 0.0001f)
                {
                    Vector3 gzDir = leverPerp.normalized;

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(comPos, comPos + gzDir * GZ);

#if UNITY_EDITOR
                    Handles.color = Color.green;
                    Handles.Label(comPos + gzDir * GZ, $"GZ: {GZ:F3} m");
#endif
                }
            }

            // ─────────────────────────────────────────────────────────────
            // GM (Metacentric Height) with meaningful heel threshold
            // ─────────────────────────────────────────────────────────────
            if (drawGM)
            {
                const float minHeelDegForGM = 5.0f;

                if (!Application.isPlaying)
                    return;

                // Only compute GM when heel is large enough to be meaningful
                if (Mathf.Abs(heelAngleDeg) < minHeelDegForGM)
                    return;

                float sinHeel = Mathf.Sin(heelAngleRad);
                if (Mathf.Abs(sinHeel) < 0.0001f || GZ <= 0.0001f)
                    return;

                float GM = GZ / sinHeel;

                if (GM > highestGM)
                    highestGM = GM;

#if UNITY_EDITOR
                lastGM = GM;
#endif

                // Draw metacentre line
                Vector3 heelPlaneNormal = Vector3.Cross(rollAxis, Vector3.up).normalized;
                if (heelPlaneNormal.sqrMagnitude < 0.0001f)
                    heelPlaneNormal = Vector3.forward;

                Vector3 M = comPos + heelPlaneNormal * GM;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(comPos, M);
            }
        }


        // ─────────────────────────────────────────────────────────────
        // ROLL AXIS / ROLL RATE
        // ─────────────────────────────────────────────────────────────

            /// <summary>
            /// Draws roll axis line through COM and roll‑rate arrow based on angular velocity.
            /// </summary>
        private void DrawRollDiagnostics(Vector3 comPos)
        {
            Vector3 rollAxis = transform.right;

            if (drawRollAxis)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(comPos - rollAxis * 2f, comPos + rollAxis * 2f);

#if UNITY_EDITOR
                Handles.color = Color.white;
                Handles.Label(comPos + rollAxis * 2f, "Roll Axis");
#endif
            }

            if (drawRollRate && rb != null)
            {
                // Project angular velocity onto roll axis
                float rollRate = Vector3.Dot(rb.angularVelocity, rollAxis); // rad/s
                Vector3 rollRateVec = rollAxis * rollRate * rollRateScale;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(comPos, comPos + rollRateVec);

#if UNITY_EDITOR
                lastRollRateDeg = rollRate * Mathf.Rad2Deg;
                Handles.color = Color.red;
                Handles.Label(comPos + rollRateVec, $"Roll Rate: {lastRollRateDeg:F1} °/s");
#endif
            }
        }
    }
}