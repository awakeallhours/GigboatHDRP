#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    /// <summary>
    /// Editor‑only overlay for displaying COM diagnostics in the Scene View.
    /// Draws gizmos in OnDrawGizmos and IMGUI in OnGUI.
    /// Never mixes GUILayout with Gizmos to avoid repaint/layout errors.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BoatCOMOverlay : MonoBehaviour
    {
        [Tooltip("Reference to the BoatCOM authority on this vessel.")]
        public BoatCOM boatCOM;

        private void Reset()
        {
            if (boatCOM == null)
                boatCOM = GetComponent<BoatCOM>();
        }

        /// <summary>
        /// Draws COM gizmos in the Scene View.
        /// Only Handles/Gizmos are allowed here — no GUILayout.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (boatCOM == null)
                return;

            // Draw COM marker in Scene View
            Vector3 comPos = transform.position + Vector3.up * boatCOM.comHeight;

            Handles.color = Color.yellow;
            Handles.DrawSolidDisc(comPos, Vector3.up, 0.05f);

            // Draw neutral band line
            Vector3 neutralPos = transform.position + Vector3.up * boatCOM.NeutralBandMin;
            Handles.color = Color.cyan;
            Handles.DrawLine(neutralPos + Vector3.left * 0.5f, neutralPos + Vector3.right * 0.5f);
        }

        /// <summary>
        /// Draws the floating debug panel in the Scene View using IMGUI.
        /// This is the correct place for GUILayout.
        /// </summary>
        private void OnGUI()
        {
            if (boatCOM == null)
                return;

            // Convert COM world position to GUI position
            Vector3 screenPos = HandleUtility.WorldToGUIPoint(
                transform.position + Vector3.up * boatCOM.comHeight
            );

            Rect rect = new Rect(screenPos.x + 10f, screenPos.y - 30f, 180f, 90f);

            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label("Boat COM Debug");
            GUILayout.Label($"COM: {boatCOM.comHeight:F3} m");
            GUILayout.Label($"Neutral: {boatCOM.NeutralBandMin:F3} m");

            bool newEnable = GUILayout.Toggle(boatCOM.enableCOMOffset, "Enable COM Offset");
            if (newEnable != boatCOM.enableCOMOffset)
            {
                Undo.RecordObject(boatCOM, "Toggle COM Offset");
                boatCOM.enableCOMOffset = newEnable;
                boatCOM.ApplyCOM();
            }

            if (GUILayout.Button("Re-test COM"))
            {
                boatCOM.ApplyCOM();
                boatCOM.CheckNeutralBand();
            }

            GUILayout.EndArea();
        }
    }
}
#endif