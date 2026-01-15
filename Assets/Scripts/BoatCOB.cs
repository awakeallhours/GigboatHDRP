using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    /// <summary>
    /// Stores buoyancy‑related spatial data for the vessel.
    /// This component does NOT compute buoyancy forces.
    /// It simply receives buoyancy state (COB, submerged volume, etc.)
    /// from the buoyancy system and exposes it for diagnostics.
    ///
    /// This mirrors BoatCOM, but for buoyancy instead of mass.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoatCOB : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        // CENTRE OF BUOYANCY (COB)
        // ─────────────────────────────────────────────────────────────

        [Header("Centre of Buoyancy")]
        [Tooltip("Centre of Buoyancy in LOCAL space. Updated by the buoyancy system.")]
        [SerializeField] private Vector3 localCOB = Vector3.zero;

        /// <summary>
        /// Sets the Centre of Buoyancy in local space.
        /// Called by the buoyancy system after computing buoyancy forces.
        /// </summary>
        /// <param name="cobLocal">COB position in local vessel space.</param>
        public void SetLocalCOB(Vector3 cobLocal)
        {
            localCOB = cobLocal;
        }

        /// <summary>
        /// Centre of Buoyancy in world space.
        /// Used by the BoatPhysicsVisualizer.
        /// </summary>
        public Vector3 COBWorldPosition => transform.TransformPoint(localCOB);


        // ─────────────────────────────────────────────────────────────
        // OPTIONAL BUOYANCY STATE (for future diagnostics)
        // These fields are NOT required for COB visualisation,
        // but they are extremely useful for debugging hydrodynamics.
        // ─────────────────────────────────────────────────────────────

        [Header("Buoyancy State (Optional)")]

        [Tooltip("Total submerged volume in cubic meters. Updated by buoyancy system.")]
        [SerializeField] private float submergedVolume = 0f;

        [Tooltip("Total upward buoyancy force in Newtons. Updated by buoyancy system.")]
        [SerializeField] private float totalBuoyancyForce = 0f;

        /// <summary>
        /// Sets the submerged volume (m³).
        /// </summary>
        public void SetSubmergedVolume(float volume)
        {
            submergedVolume = volume;
        }

        /// <summary>
        /// Sets the total buoyancy force (N).
        /// </summary>
        public void SetTotalBuoyancyForce(float force)
        {
            totalBuoyancyForce = force;
        }

        /// <summary>
        /// Gets the submerged volume (m³).
        /// </summary>
        public float SubmergedVolume => submergedVolume;

        /// <summary>
        /// Gets the total buoyancy force (N).
        /// </summary>
        public float TotalBuoyancyForce => totalBuoyancyForce;
    }
}
