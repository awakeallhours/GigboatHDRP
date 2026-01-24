using UnityEngine;
using Axiom.Vessel.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Axiom.Diagnostics.Visualization
{
    public sealed class COBVisualizer : MonoBehaviour
    {
        [Header("References")]
        public BoatCOB boatCOB;
        public BoatCOM boatCOM;     // for COM→COB line
        public Rigidbody rb;        // for COM world position

        [Header("Settings")]
        [Tooltip("Radius of the COB sphere in meters.")]
        public float cobSphereRadius = 0.12f;

        [Tooltip("Draw the Centre of Buoyancy marker.")]
        public bool drawCOB = true;

        public void Draw()
        {
#if UNITY_EDITOR
            if (!drawCOB)
                return;

            if (boatCOB == null || boatCOM == null || rb == null)
                return;

            // COB world position
            Vector3 cobWorld = boatCOB.COBWorldPosition;

            // COM world position
            Vector3 comWorld = rb.worldCenterOfMass;

            // Draw COB sphere
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(cobWorld, cobSphereRadius);

            // Line from COM to COB
            Gizmos.color = Color.white;
            Gizmos.DrawLine(comWorld, cobWorld);

            // Label
            Handles.color = Color.blue;
            Handles.Label(cobWorld + Vector3.right * 0.2f, "COB");
#endif
        }
    }
}