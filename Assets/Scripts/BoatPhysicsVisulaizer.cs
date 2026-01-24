using UnityEngine;
using System.Collections;

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

        [SerializeField]
        private StabilityProfileComponent stabilityProfileComponent;

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

        [SerializeField] private WaterProbeSampler probeSampler;

        private bool[] valid;
        private float[] heights;
        private Vector3[] normals;
        private Transform[] points;
        private ProbeType[] types;

        private VesselBootstrap bootstrap;

        // ─────────────────────────────────────────────────────────────
        // OPTIONAL REFERENCES FOR EXTENDED VISUALS
        // ─────────────────────────────────────────────────────────────

        [Header("Optional References")]
        [Tooltip("Rigidbody used for velocity, slip, and roll diagnostics.")]
        [SerializeField] private Rigidbody rb;

        [Tooltip("Draw the Centre of Buoyancy marker.")]
        public bool drawCOB = true;

        [Tooltip("Draw the righting moment torque arrow (edit mode only).")]
        public bool drawRightingMoment = true;

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

        private void Awake()
        {
            bootstrap = GetComponentInParent<VesselBootstrap>();
        }
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

            GUILayout.Space(5f);

            // Run GM/GZ Scan button
            GUI.enabled = Application.isPlaying && boatCOM != null && boatCOB != null && rb != null;
            if (GUILayout.Button("Run GM/GZ Stability Scan"))
            {
                StartCoroutine(RunGMGZScan());
            }
            GUI.enabled = true;

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

            Vector3 cobPosWorld = boatCOB.COBWorldPosition;

            // Neutral band height (world Y)
            float neutralY = boatCOM.NeutralBandMin;

            // Positions for horizontal lines (over hull origin)
            Vector3 neutralPos = basePos + Vector3.up * neutralY;
            Vector3 comHeightPos = new Vector3(basePos.x, comY, basePos.z);

            Vector3 left = Vector3.left * (lineWidth * 0.5f);
            Vector3 right = Vector3.right * (lineWidth * 0.5f);

            // RIGHTING MOMENT (edit mode only)
            if (drawRightingMoment)
            {
                //new section to stop null
                if (!Application.isPlaying)
                    return;

                if (bootstrap == null || bootstrap.Orientation.RollAxis == null)
                    return;

                // end of new section

                Vector3 rollAxis = bootstrap.Orientation.RollAxis;

                // Lever arm from COM to COB
                Vector3 lever = cobPosWorld - comWorld;

                // Remove any component along the roll axis
                Vector3 leverPerp = Vector3.ProjectOnPlane(lever, rollAxis);

                if (leverPerp.sqrMagnitude > 0.0001f)
                {
                    // Righting moment direction
                    Vector3 torqueDir = Vector3.Cross(leverPerp, rollAxis).normalized;

                    Gizmos.color = new Color(0.8f, 0.3f, 1f);
                    Gizmos.DrawLine(comWorld, comWorld + torqueDir * 2f);

#if UNITY_EDITOR
                    Handles.color = new Color(0.8f, 0.3f, 1f);
                    Handles.Label(comWorld + torqueDir * 2f, "Righting Moment");
#endif
                }
            }

            /*// BUOYANCY PROBE VECTORS (PLAY MODE)
            if (drawBuoyancyProbes && Application.isPlaying)
                DrawBuoyancyProbes();*/
#endif
        }
        /*
        // ─────────────────────────────────────────────────────────────
        // BUOYANCY PROBE VISUALISATION
        // ─────────────────────────────────────────────────────────────

        private void DrawBuoyancyProbes()
        {
            if (buoyancy == null || sampler == null)
                return;

            probeSampler.GetProbeData(out valid, out heights, out normals, out points, out types);

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
        */

#if UNITY_EDITOR
        private IEnumerator RunGMGZScan()
        {
            if (boatCOM == null || boatCOB == null || rb == null)
                yield break;

            var scanner = new GMGZStabilityScanner(bootstrap, bootstrap.transform, rb, boatCOB, boatCOM);


            yield return scanner.RunScan(
        startAngle: 0f,
        endAngle: 45f,
        step: 1f,
        settleTime: 0.25f,
        onComplete: profile =>
        {
            stabilityProfileComponent.SetProfile(profile);

            Debug.Log(
            "<b>[GM/GZ Stability Scan Results]</b>\n" +
            "\n" +
            $"<b>Initial Stability (GM_Initial):</b> {profile.GM_Initial:F3} m   " +
            $"Valid={profile.GM_Initial_Valid}\n" +
            "Plain: Stability when the boat first starts to lean.\n" +
            "\n" +
            $"<b>Strongest Stability (GM_Peak):</b> {profile.GM_Peak:F3} m @ {profile.GM_PeakAngle:F1}°   " +
            $"Valid={profile.GM_Peak_Valid}\n" +
            "Plain: The strongest overall stability the boat showed.\n" +
            "\n" +
            $"<b>Strongest Righting Force (GZ_Peak):</b> {profile.GZ_Peak:F3} m @ {profile.GZ_PeakAngle:F1}°   " +
            $"Valid={profile.GZ_Peak_Valid}\n" +
            "Plain: The strongest force pushing the boat upright.\n" +
            "\n" +
            $"<b>Vanishing Stability Angle (GZ_ZeroAngle):</b> {profile.GZ_ZeroAngle:F1}°   " +
            $"Valid={profile.GZ_ZeroAngle_Valid}\n" +
            "Plain: The angle where the boat stops being able to right itself.\n" +
            "\n" +
            $"<b>Positive Stability Range:</b> {profile.PositiveStabilityRange:F1}°\n" +
            "Plain: How far the boat can lean while still being stable.\n" +
            "\n" +
            $"<b>COM Safe Range:</b> {profile.COM_SafeMin:F3} m → {profile.COM_SafeMax:F3} m\n" +
            "Plain: Lowest and highest safe centre‑of‑mass height.\n" +
            "\n" +
            $"<b>Notes:</b> {profile.Notes}");
        });
        }
#endif
    }
}