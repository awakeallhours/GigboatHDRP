using UnityEngine;
using Axiom.Vessel.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Axiom.Diagnostics.Visualization
{
    public sealed class COMVisualizer : MonoBehaviour
    {
        [Header("References")]
        public BoatCOM boatCOM;
        public Rigidbody rb;

        [Header("Settings")]
        public float lineWidth = 1f;

        [Tooltip("Radius of the COM disc in meters.")]
        public float comDiscRadius = 0.15f;

        [Header("Master Toggle")]
        public bool drawGizmos = true;

        [Header("Feature Toggles")]
        public bool drawNeutralBand = true;
        public bool drawCOMHeight = true;
        public bool drawCOMDisc = true;
        public bool drawLabels = true;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (drawGizmos)
                Draw();
        }
#endif

        public void Draw()
        {
#if UNITY_EDITOR
            if (boatCOM == null || rb == null)
                return;

            Vector3 basePos = transform.position;

            // COM world position
            Vector3 comWorld = rb.worldCenterOfMass;
            float comY = comWorld.y;

            // Neutral band height (world Y)
            float neutralY = boatCOM.NeutralBandMin;

            // Line positions
            Vector3 neutralPos = basePos + Vector3.up * neutralY;
            Vector3 comHeightPos = new Vector3(basePos.x, comY, basePos.z);

            Vector3 left = Vector3.left * (lineWidth * 0.5f);
            Vector3 right = Vector3.right * (lineWidth * 0.5f);

            // ─────────────────────────────────────────────
            // NEUTRAL BAND
            // ─────────────────────────────────────────────
            if (drawNeutralBand)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(neutralPos + left, neutralPos + right);

                if (drawLabels)
                {
                    Handles.color = Color.cyan;
                    Handles.Label(neutralPos + Vector3.right * (lineWidth * 0.6f), "Neutral Band");
                }
            }

            // ─────────────────────────────────────────────
            // COM HEIGHT LINE
            // ─────────────────────────────────────────────
            if (drawCOMHeight)
            {
                bool valid = comY >= neutralY;
                Gizmos.color = valid ? Color.green : Color.red;
                Gizmos.DrawLine(comHeightPos + left, comHeightPos + right);

                if (drawLabels)
                {
                    Handles.color = valid ? Color.green : Color.red;
                    Handles.Label(comHeightPos + Vector3.right * (lineWidth * 0.6f), "COM Height");
                }
            }

            // ─────────────────────────────────────────────
            // COM DISC
            // ─────────────────────────────────────────────
            if (drawCOMDisc)
            {
                Handles.color = Color.yellow;
                Handles.DrawSolidDisc(comWorld, Vector3.up, comDiscRadius);
            }
#endif
        }
    }
}