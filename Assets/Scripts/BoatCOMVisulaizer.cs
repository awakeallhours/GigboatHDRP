using UnityEngine;
using Axiom.Vessel.Diagnostics;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Axiom.Vessel.Diagnostics
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BoatCOMVisualizer : MonoBehaviour
    {
        [Tooltip("Reference to the BoatCOM authority on this vessel.")]
        public BoatCOM boatCOM;

        [Tooltip("Horizontal line length in meters for visual markers.")]
        public float lineWidth = 1.0f;

        private void Reset()
        {
            if (boatCOM == null)
                boatCOM = GetComponent<BoatCOM>();
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (boatCOM == null)
                boatCOM = GetComponent<BoatCOM>();

            if (boatCOM == null)
                return;

            Vector3 basePos = transform.position;

            float comY = boatCOM.COMHeight != 0f ? boatCOM.COMHeight : boatCOM.comHeight;
            float neutralY = boatCOM.NeutralBandMin;

            Vector3 neutralPos = basePos + Vector3.up * neutralY;
            Vector3 comPos = basePos + Vector3.up * comY;

            Vector3 left = Vector3.left * (lineWidth * 0.5f);
            Vector3 right = Vector3.right * (lineWidth * 0.5f);

            // Neutral band line (yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(neutralPos + left, neutralPos + right);

            // COM line (green if valid, red if below neutral)
            bool valid = comY >= neutralY;
            Gizmos.color = valid ? Color.green : Color.red;
            Gizmos.DrawLine(comPos + left, comPos + right);

            Handles.color = Color.yellow;
            Handles.Label(neutralPos + Vector3.right * (lineWidth * 0.6f), "Neutral Band");

            Handles.color = valid ? Color.green : Color.red;
            Handles.Label(comPos + Vector3.right * (lineWidth * 0.6f), "COM");
#endif
        }
    }
}

