using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    [ExecuteAlways]
    public sealed class BoatCOM : MonoBehaviour
    {
        [Header("Center of Mass")]
        [Tooltip("Vertical COM offset in meters. Must be ABOVE the neutral lateral-force band.")]
        public float comHeight = 0.35f;

        [Tooltip("Forward (+) / Aft (-) COM offset in meters.")]
        public float comForwardOffset = 0.0f;

        [Header("Neutral Band (meters)")]
        [Tooltip("Minimum acceptable COM height. COM must be ABOVE this value.")]
        public float neutralBandMin = 0.0f;

        [Header("Runtime Controls")]
        [Tooltip("If disabled, COM offset is not applied and Rigidbody uses its default COM.")]
        public bool enableCOMOffset = true;

        [Header("Debug Options")]
        [Tooltip("If enabled, COM changes and warnings will be logged to the console.")]
        public bool enableDebugLogs = true;

        [SerializeField, Tooltip("Actual COM applied to the Rigidbody (read-only).")]
        private Vector3 appliedCom;

        private Rigidbody rb;
        private BoatPhysicsVisualizer visualizer;

        public float COMHeight => appliedCom.y;
        public float COMForward => appliedCom.z;
        public float NeutralBandMin => neutralBandMin;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            visualizer = GetComponent<BoatPhysicsVisualizer>();
            ApplyCOM();
            CheckNeutralBand();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
                return;
#endif

            if (rb == null)
                rb = GetComponent<Rigidbody>();
            if (visualizer == null)
                visualizer = GetComponent<BoatPhysicsVisualizer>();

            // NEW: Do not apply COM when editing is locked
            if (visualizer != null && visualizer.ComEditingLocked)
                return;

            if (rb != null)
            {
                ApplyCOM();
                CheckNeutralBand();
            }
        }

        public void ApplyCOM()
        {
            if (!enableCOMOffset || rb == null)
            {
                if (enableDebugLogs && Application.isPlaying)
                    Debug.Log("[BoatCOM] COM offset disabled — using Rigidbody default COM.");
                return;
            }

            Vector3 com = rb.centerOfMass;

            // Unity default axes: Z = forward, X = right, Y = up
            com.z = comForwardOffset;
            com.y = comHeight;

            rb.centerOfMass = com;
            appliedCom = com;

            if (enableDebugLogs && Application.isPlaying)
            {
                Debug.Log(
                    $"[BoatCOM] Applied COM offset.\n" +
                    $"  COM.y (height): {appliedCom.y} m\n" +
                    $"  COM.z (fore/aft): {appliedCom.z} m\n" +
                    $"  Neutral band min: {neutralBandMin} m\n" +
                    $"  Rigidbody mass: {rb.mass} kg"
                );
            }
        }

        public void CheckNeutralBand()
        {
            // Do NOT warn if COM editing is locked — the real COM hasn't changed.
            if (visualizer != null && visualizer.ComEditingLocked)
                return;

            if (comHeight < neutralBandMin)
            {
                Debug.LogWarning(
                    $"[BoatCOM] COM height ({comHeight} m) is BELOW the neutral band minimum ({neutralBandMin} m). " +
                    $"Turning roll behaviour will be incorrect."
                );
            }
        }

        [ContextMenu("Apply COM")]
        private void ApplyCOM_ContextMenu()
        {
            ApplyCOM();
            CheckNeutralBand();

            if (enableDebugLogs)
                Debug.Log("[BoatCOM] Applied COM and checked neutral band.");
        }
    }
}


