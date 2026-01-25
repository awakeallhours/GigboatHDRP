using UnityEngine;
using System.Collections;
using Axiom.Vessel.Stability.Editor;


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
            /*
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
            }*/

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
    }
}