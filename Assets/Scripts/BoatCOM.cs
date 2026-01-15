using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BoatCOM : MonoBehaviour
    {
        /*
        -------------------------------------------------------------------------
        CENTER OF MASS NOTES (READ THIS BEFORE TUNING)
        -------------------------------------------------------------------------

        WHY THIS SCRIPT EXISTS:
        -----------------------
        The boat’s roll direction during turning depends on the vertical position
        of the Center of Mass (COM) relative to where lateral forces are applied.
        If lateral forces are applied BELOW the COM, the boat rolls the WRONG way.
        If lateral forces are applied ABOVE the COM, the boat rolls INTO the turn.

        WHAT YOU MUST DO WHEN BUILDING A NEW BOAT:
        ------------------------------------------
        1. Set an initial COM height using 'comHeight' below.
        2. Use the LateralForceDebug script to find the NEUTRAL BAND:
            - Apply a sideways force at different heights.
            - Find the height where roll torque ≈ 0.
            - This is the neutral band.
        3. Your COM should sit ABOVE the neutral band for correct turning roll.
        4. All lateral/turning forces (rudder, crossflow, side drag) must be
            applied AT OR ABOVE the COM height.

        HOW TO USE THE LATERAL FORCE DEBUG TOOL:
        ----------------------------------------
        - Add the LateralForceDebug script to the boat root.
        - Adjust 'testPointHeight' and click Apply Force.
        - Observe roll direction:
            * Rolls INTO force  -> test point is ABOVE COM
            * Rolls AWAY        -> test point is BELOW COM
            * Neutral           -> test point ≈ COM height
        - Record the neutral band height.
        - Set COM slightly ABOVE that value.

        WHEN TO CHANGE COM:
        --------------------
        - Raise COM to increase roll responsiveness (more tippy).
        - Lower COM to increase stability (less roll).
        - ANY TIME YOU CHANGE COM, you MUST re-run the lateral-force test.

        -------------------------------------------------------------------------
        */
        [Header("Center of Mass")]
        [Tooltip("Vertical COM offset in meters. Must be ABOVE the neutral lateral-force band.")]
        public float comHeight = 0.35f;

        [Header("Neutral Band (meters)")]
        [Tooltip("Minimum acceptable COM height. COM must be ABOVE this value.")]
        public float neutralBandMin = 0.0f;

        [Header("Runtime Controls")]
        [Tooltip("If disabled, COM offset is not applied and Rigidbody uses its default COM.")]
        public bool enableCOMOffset = true;

        [Header("Debug Options")]
        [Tooltip("If enabled, COM changes and warnings will be logged to the console.")]
        public bool enableDebugLogs = true;

        [SerializeField, Tooltip("Actual COM.y applied to the Rigidbody (read-only).")]
        private float appliedComY;

        private Rigidbody rb;

        public float COMHeight => appliedComY;
        public float NeutralBandMin => neutralBandMin;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ApplyCOM();
            CheckNeutralBand();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && rb != null)
            {
                ApplyCOM();
                CheckNeutralBand();
            }
        }

        public void ApplyCOM()
        {
            if (!enableCOMOffset)
            {
                if (enableDebugLogs)
                    Debug.Log("[BoatCOM] COM offset disabled — using Rigidbody default COM.");
                return;
            }

            Vector3 com = rb.centerOfMass;
            com.y = comHeight;
            rb.centerOfMass = com;

            appliedComY = com.y;

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[BoatCOM] Applied COM offset.\n" +
                    $"  COM.y: {appliedComY} m\n" +
                    $"  Neutral band min: {neutralBandMin} m\n" +
                    $"  Rigidbody mass: {rb.mass} kg"
                );
            }
        }

        public void CheckNeutralBand()
        {
            if (comHeight < neutralBandMin)
            {
                Debug.LogWarning(
                    $"[BoatCOM] COM height ({comHeight} m) is BELOW the neutral band minimum ({neutralBandMin} m). " +
                    $"Turning roll behaviour will be incorrect."
                );
            }
        }

        [ContextMenu("Re-test COM")]
        private void RetestCOM()
        {
            ApplyCOM();
            CheckNeutralBand();

            if (enableDebugLogs)
                Debug.Log("[BoatCOM] Re-tested COM and neutral band.");
        }
    }
}

