using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Axiom.Vessel.Diagnostics
{
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

        // COM editing lock (per boat)
        [SerializeField, Tooltip("When locked, COM changes are not applied to the Rigidbody.")]
        private bool comEditingLocked = true;
        public bool ComEditingLocked => comEditingLocked;

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

        /// <summary>Allows external systems (e.g., movement controller) to feed thrust force.</summary>
        public void SetThrustForce(Vector3 force) => thrustForce = force;

        // ─────────────────────────────────────────────────────────────
        // BUOYANCY / WATERLINE VISUALS
        // ─────────────────────────────────────────────────────────────

        [Header("Buoyancy & Waterline Visuals")]
        [SerializeField] private bool drawBuoyancyProbes = true;
        [SerializeField] private bool drawWaterlinePlane = true;
        [SerializeField] private Buoyancy buoyancy;
        [SerializeField] private WaterProbeSampler sampler;
        [SerializeField] private Color buoyancyForceColor = Color.cyan;
        [SerializeField] private float buoyancyForceScale = 0.001f;
        [SerializeField] private Color probeDepthColorShallow = Color.blue;
        [SerializeField] private Color probeDepthColorDeep = Color.red;
        [SerializeField] private float probeDepthMaxForColor = 2f;
        [SerializeField] private Color waterlineColor = new Color(0.2f, 0.6f, 1f, 0.6f);
        [SerializeField] private float waterlineHalfSize = 3f;
        [SerializeField] private int waterlineGridResolution = 4;

        // ─────────────────────────────────────────────────────────────
        // STABILITY / ROLL DIAGNOSTICS
        // ─────────────────────────────────────────────────────────────

        [Header("Stability & Roll Diagnostics")]
        [SerializeField] private bool drawGM = true;
        [SerializeField] private bool drawGZ = true;
        [SerializeField] private bool drawRollAxis = true;
        [SerializeField] private bool drawRollRate = true;
        [SerializeField] private float rollRateScale = 0.5f;

        // ─────────────────────────────────────────────────────────────
        // TOGGLES
        // ─────────────────────────────────────────────────────────────

        [Header("Gizmo Toggles")]
        public bool drawThrustPoint = true;
        public bool drawThrustVector = true;
        public bool drawHullBottom = true;
        public bool drawForward = true;
        public bool drawVelocity = true;
        public bool drawSlip = true;

        // GM tracking
        private float highestGM = 0f;

#if UNITY_EDITOR
        // Overlay state
        private float lastGM = 0f;
        private float lastHeelDeg = 0f;
        private float lastGZ = 0f;
        private float lastRollRateDeg = 0f;

        // COM diagnostics panel state
        private bool comPanelOpen = true;
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
            if (SceneView.currentDrawingSceneView == null ||
                !SceneView.currentDrawingSceneView.camera.name.Contains("Scene"))
                return;

            Handles.BeginGUI();

            const float gmWidth = 280f;
            const float gmHeight = 120f;
            float gmX = (sceneView.position.width - gmWidth) * 0.5f;
            float gmY = 10f;

            // GM / Stability panel (center)
            GUILayout.BeginArea(new Rect(gmX, gmY, gmWidth, gmHeight), GUI.skin.box);
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

            // COM diagnostics panel (to the right of GM panel)
            const float comWidth = 260f;
            const float comHeight = 160f;
            float comX = gmX + gmWidth + 20f;
            float comY = gmY;

            GUILayout.BeginArea(new Rect(comX, comY, comWidth, comHeight), GUI.skin.box);

            GUIStyle header = new GUIStyle(EditorStyles.boldLabel);
            header.normal.textColor = Color.white;

            string arrow = comPanelOpen ? "▼ " : "► ";
            if (GUILayout.Button(arrow + "COM Diagnostics", header))
            {
                comPanelOpen = !comPanelOpen;
            }

            if (comPanelOpen && boatCOM != null)
            {
                float com = boatCOM.comHeight;
                float neutral = boatCOM.NeutralBandMin;

                // Health colour
                Color healthColor =
                    com < neutral ? Color.red :
                    com < neutral + 0.1f ? new Color(1f, 0.65f, 0f) :
                    Color.green;

                GUIStyle valueStyle = new GUIStyle(EditorStyles.label);
                valueStyle.normal.textColor = healthColor;

                GUILayout.Label($"COM Height: {com:F3} m", valueStyle);
                GUILayout.Label($"Neutral Band: {neutral:F3} m");

                // Enable COM Offset toggle
                bool newEnable = GUILayout.Toggle(boatCOM.enableCOMOffset, "Enable COM Offset");
                if (newEnable != boatCOM.enableCOMOffset)
                {
                    Undo.RecordObject(boatCOM, "Toggle COM Offset");
                    boatCOM.enableCOMOffset = newEnable;
                    boatCOM.ApplyCOM();
                    boatCOM.CheckNeutralBand();
                }

                // Apply COM button
                if (GUILayout.Button("Apply COM"))
                {
                    Undo.RecordObject(boatCOM, "Apply COM");
                    boatCOM.ApplyCOM();
                    boatCOM.CheckNeutralBand();
                }

                GUILayout.Space(5f);

                // COM Editing Lock/Unlock button
                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = comEditingLocked ? Color.green : Color.red;
                string label = comEditingLocked ? "COM Editing Locked" : "COM Editing Unlocked";
                if (GUILayout.Button(label))
                {
                    comEditingLocked = !comEditingLocked;

                    // When unlocking, force BoatCOM to re-apply COM immediately
                    if (!comEditingLocked && boatCOM != null)
                    {
                        boatCOM.ApplyCOM();
                        boatCOM.CheckNeutralBand();
                    }
                }
                GUI.backgroundColor = prevColor;

                // Warning when locked
                if (comEditingLocked)
                {
                    GUI.color = Color.red;
                    GUILayout.Label("COM Editing Locked — inspector changes not applied.");
                    GUI.color = Color.white;
                }
            }

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

            if (rb == null)
                rb = GetComponent<Rigidbody>();
            if (rb == null)
                return;

            // Base reference (hull origin)
            Vector3 basePos = transform.position;

            // REAL COM in world space
            Vector3 comWorld = rb.worldCenterOfMass;
            float comY = comWorld.y;

            // Neutral band height (world Y)
            float neutralY = boatCOM.NeutralBandMin;

            // Positions for horizontal lines (over hull origin)
            Vector3 neutralPos = basePos + Vector3.up * neutralY;
            Vector3 comHeightPos = new Vector3(basePos.x, comY, basePos.z);

            Vector3 left = Vector3.left * (lineWidth * 0.5f);
            Vector3 right = Vector3.right * (lineWidth * 0.5f);

            // NEUTRAL BAND LINE
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(neutralPos + left, neutralPos + right);
            Handles.color = Color.cyan;
            Handles.Label(neutralPos + Vector3.right * (lineWidth * 0.6f), "Neutral Band");

            // COM HEIGHT BAND (2D band over hull origin)
            bool valid = comY >= neutralY;
            Gizmos.color = valid ? Color.green : Color.red;
            Gizmos.DrawLine(comHeightPos + left, comHeightPos + right);
            Handles.color = valid ? Color.green : Color.red;
            Handles.Label(comHeightPos + Vector3.right * (lineWidth * 0.6f), "COM Height");

            // COM DISC (at real COM)
            Handles.color = Color.yellow;
            Handles.DrawSolidDisc(comWorld, Vector3.up, 0.05f);

            // CENTRE OF BUOYANCY (COB)
            Vector3 cobPosWorld = boatCOB.COBWorldPosition;

            if (drawCOB)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(cobPosWorld, 0.12f);

                Gizmos.color = Color.white;
                Gizmos.DrawLine(comWorld, cobPosWorld);

                Handles.color = Color.blue;
                Handles.Label(cobPosWorld + Vector3.right * 0.2f, "COB");
            }

            // RIGHTING MOMENT (edit mode only)
            if (drawRightingMoment)
            {
                Vector3 leverArm = cobPosWorld - comWorld;
                Vector3 buoyancyDir = Vector3.up;
                Vector3 rightingTorque = Vector3.Cross(leverArm, buoyancyDir);

                if (rightingTorque.sqrMagnitude > 0.0001f)
                {
                    Vector3 torqueDir = rightingTorque.normalized;

                    Gizmos.color = new Color(0.8f, 0.3f, 1f);
                    Gizmos.DrawLine(comWorld, comWorld + torqueDir * 2f);

                    Handles.color = new Color(0.8f, 0.3f, 1f);
                    Handles.Label(comWorld + torqueDir * 2f, "Righting Moment");
                }
            }

            // THRUST POINT
            if (drawThrustPoint && thrustPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(thrustPoint.position, 0.12f);

                Gizmos.DrawLine(thrustPoint.position + Vector3.up * 1.5f,
                                thrustPoint.position - Vector3.up * 1.5f);

                Gizmos.color = Color.white;
                Gizmos.DrawLine(thrustPoint.position, comWorld);

                Handles.color = Color.cyan;
                Handles.Label(thrustPoint.position + Vector3.right * 0.2f, "Thrust Point");
            }

            // THRUST VECTOR
            if (drawThrustVector && thrustPoint != null)
            {
                Color orange = new Color(1f, 0.5f, 0f);
                Gizmos.color = orange;

                Gizmos.DrawLine(thrustPoint.position,
                                thrustPoint.position + thrustForce * 0.01f);

                Handles.color = orange;
                Handles.Label(thrustPoint.position + Vector3.up * 0.3f, "Thrust Vector");
            }

            // HULL BOTTOM
            if (drawHullBottom)
            {
                Vector3 hullBottom = transform.TransformPoint(
                    new Vector3(0f, hullBottomLocalY, 0f)
                );

                Gizmos.color = Color.grey;
                Gizmos.DrawCube(hullBottom, new Vector3(0.15f, 0.02f, 0.15f));

                Gizmos.DrawLine(hullBottom, comWorld);

                Handles.color = Color.grey;
                Handles.Label(hullBottom + Vector3.right * 0.2f, "Hull Bottom");
            }

            // FORWARD DIRECTION (EDIT MODE)
            if (drawForward)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(comWorld, comWorld + transform.forward * 3f);

                Handles.color = Color.blue;
                Handles.Label(comWorld + transform.forward * 3f, "Forward");
            }

            // VELOCITY + SLIP (PLAY MODE ONLY)
            if (drawVelocity && rb != null && Application.isPlaying)
            {
                Vector3 vel = rb.linearVelocity; // your custom extension

                if (vel.sqrMagnitude > 0.01f)
                {
                    Color lime = new Color(0.7f, 1f, 0f);
                    Gizmos.color = lime;
                    Gizmos.DrawLine(comWorld, comWorld + vel.normalized * 3f);
                }

                if (drawSlip && vel.sqrMagnitude > 0.01f)
                {
                    Vector3 localVel = transform.InverseTransformDirection(vel);
                    Vector3 lateral = new Vector3(localVel.x, 0f, 0f);
                    Vector3 lateralWorld = transform.TransformDirection(lateral);

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(comWorld, comWorld + lateralWorld * 2f);
                }
            }

            // BUOYANCY PROBE VECTORS (PLAY MODE)
            if (drawBuoyancyProbes && Application.isPlaying)
                DrawBuoyancyProbes();

            // WATERLINE PLANE (PLAY MODE)
            if (drawWaterlinePlane && Application.isPlaying)
                DrawWaterlinePlane();

            // STABILITY & ROLL DIAGNOSTICS (PLAY MODE)
            if (Application.isPlaying && rb != null)
            {
                if (drawRollAxis || drawRollRate)
                    DrawRollDiagnostics(comWorld);

                if (drawGM || drawGZ)
                    DrawStabilityDiagnostics(comWorld, cobPosWorld);
            }
#endif
        }

        // ─────────────────────────────────────────────────────────────
        // BUOYANCY PROBE VISUALISATION
        // ─────────────────────────────────────────────────────────────

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

                float depth01 = probeDepthMaxForColor > 0f
                    ? Mathf.Clamp01(depth / probeDepthMaxForColor)
                    : 1f;

                Color depthColor = Color.Lerp(probeDepthColorShallow, probeDepthColorDeep, depth01);

                float forceMagnitude = depth * buoyancyStrength;
                Vector3 forceVec = Vector3.up * forceMagnitude * buoyancyForceScale;

                Debug.DrawLine(p.position, p.position + Vector3.up * 0.05f, depthColor);
                Debug.DrawLine(p.position, p.position + forceVec, buoyancyForceColor);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // WATERLINE PLANE (FITTED FROM PROBES)
        // ─────────────────────────────────────────────────────────────

        private void DrawWaterlinePlane()
        {
            if (sampler == null)
                return;

            sampler.GetProbeData(out bool[] valid, out float[] heights, out Vector3[] normals, out Transform[] points);

            if (valid == null || heights == null || points == null)
                return;

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

            Vector3 planeRight = Vector3.Cross(Vector3.up, normal).normalized;
            if (planeRight.sqrMagnitude < 0.0001f)
                planeRight = Vector3.right;

            Vector3 planeForward = Vector3.Cross(normal, planeRight).normalized;

            Gizmos.color = waterlineColor;

            int steps = Mathf.Max(1, waterlineGridResolution);
            float step = waterlineHalfSize * 2f / steps;

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

        private void DrawStabilityDiagnostics(Vector3 comWorld, Vector3 cobPosWorld)
        {
            Vector3 rollAxis = transform.right;

            Vector3 up = transform.up;
            float heelSign = Mathf.Sign(Vector3.Dot(Vector3.Cross(Vector3.up, up), rollAxis));
            float heelAngleRad = Mathf.Acos(Mathf.Clamp(Vector3.Dot(Vector3.up, up), -1f, 1f));
            heelAngleRad *= heelSign;

            float heelAngleDeg = heelAngleRad * Mathf.Rad2Deg;
#if UNITY_EDITOR
            lastHeelDeg = heelAngleDeg;
#endif

            Vector3 lever = cobPosWorld - comWorld;
            Vector3 leverPerp = Vector3.ProjectOnPlane(lever, rollAxis);
            float GZ = leverPerp.magnitude;
#if UNITY_EDITOR
            lastGZ = GZ;
#endif

            if (drawGZ && GZ > 0.0001f)
            {
                Vector3 gzDir = leverPerp.normalized;

                Gizmos.color = Color.green;
                Gizmos.DrawLine(comWorld, comWorld + gzDir * GZ);

#if UNITY_EDITOR
                Handles.color = Color.green;
                Handles.Label(comWorld + gzDir * GZ, $"GZ: {GZ:F3} m");
#endif
            }

            if (drawGM)
            {
                const float minHeelDegForGM = 5.0f;

                if (!Application.isPlaying)
                    return;

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

                Vector3 heelPlaneNormal = Vector3.Cross(rollAxis, Vector3.up).normalized;
                if (heelPlaneNormal.sqrMagnitude < 0.0001f)
                    heelPlaneNormal = Vector3.forward;

                Vector3 M = comWorld + heelPlaneNormal * GM;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(comWorld, M);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // ROLL AXIS / ROLL RATE
        // ─────────────────────────────────────────────────────────────

        private void DrawRollDiagnostics(Vector3 comWorld)
        {
            Vector3 rollAxis = transform.right;

            if (drawRollAxis)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(comWorld - rollAxis * 2f, comWorld + rollAxis * 2f);

#if UNITY_EDITOR
                Handles.color = Color.white;
                Handles.Label(comWorld + rollAxis * 2f, "Roll Axis");
#endif
            }

            if (drawRollRate && rb != null)
            {
                float rollRate = Vector3.Dot(rb.angularVelocity, rollAxis);
                Vector3 rollRateVec = rollAxis * rollRate * rollRateScale;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(comWorld, comWorld + rollRateVec);

#if UNITY_EDITOR
                lastRollRateDeg = rollRate * Mathf.Rad2Deg;
                Handles.color = Color.red;
                Handles.Label(comWorld + rollRateVec, $"Roll Rate: {lastRollRateDeg:F1} °/s");
#endif
            }
        }
    }
}