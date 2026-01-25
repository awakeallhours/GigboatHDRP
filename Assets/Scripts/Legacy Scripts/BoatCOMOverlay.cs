/*#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BoatCOMOverlay : MonoBehaviour
    {
        public BoatCOM boatCOM;

        private bool panelOpen = true;

        private void Reset()
        {
            if (boatCOM == null)
                boatCOM = GetComponent<BoatCOM>();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawSceneOverlay;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneOverlay;
        }

        // ─────────────────────────────────────────────────────────────
        // COM GIZMOS (Scene View)
        // ─────────────────────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            if (boatCOM == null)
                return;

            Vector3 comPos = transform.position + Vector3.up * boatCOM.comHeight;

            Handles.color = Color.yellow;
            Handles.DrawSolidDisc(comPos, Vector3.up, 0.05f);

            Vector3 neutralPos = transform.position + Vector3.up * boatCOM.NeutralBandMin;
            Handles.color = Color.cyan;
            Handles.DrawLine(neutralPos + Vector3.left * 0.5f, neutralPos + Vector3.right * 0.5f);
        }

        // ─────────────────────────────────────────────────────────────
        // COM PANEL (Scene View IMGUI)
        // ─────────────────────────────────────────────────────────────
        private void DrawSceneOverlay(SceneView sceneView)
        {
            if (boatCOM == null)
                return;

            Handles.BeginGUI();

            const float width = 260f;
            const float height = 140f;

            // Position: to the RIGHT of the GM overlay
            float x = (sceneView.position.width - width) * 0.5f + width + 20f;
            float y = 10f;

            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);

            GUIStyle header = new GUIStyle(EditorStyles.boldLabel);
            header.normal.textColor = Color.white;

            if (GUILayout.Button((panelOpen ? "▼ " : "► ") + "COM Diagnostics", header))
            {
                panelOpen = !panelOpen;
            }

            if (!panelOpen)
            {
                GUILayout.EndArea();
                Handles.EndGUI();
                return;
            }

            float com = boatCOM.comHeight;
            float neutral = boatCOM.NeutralBandMin;

            // Stability health colour coding
            Color healthColor =
                com < neutral ? Color.red :
                com < neutral + 0.1f ? new Color(1f, 0.65f, 0f) :
                Color.green;

            GUIStyle valueStyle = new GUIStyle(EditorStyles.label);
            valueStyle.normal.textColor = healthColor;

            GUILayout.Label($"COM Height: {com:F3} m", valueStyle);
            GUILayout.Label($"Neutral Band: {neutral:F3} m");

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
            Handles.EndGUI();
        }
    }
}
#endif*/