#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Axiom.Diagnostics.Visualization
{
    [ExecuteAlways]
    public sealed class GMGZScanVisualizer : MonoBehaviour
    {
        private ForcesAndStabilityVisualizer stabilityVis;

        private void OnEnable()
        {
            stabilityVis = GetComponent<ForcesAndStabilityVisualizer>();
            SceneView.duringSceneGui += DrawOverlay;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawOverlay;
        }

        private void DrawOverlay(SceneView sceneView)
        {
            if (SceneView.currentDrawingSceneView == null ||
                !SceneView.currentDrawingSceneView.camera.name.Contains("Scene"))
                return;

            if (stabilityVis == null)
                return;

            Handles.BeginGUI();

            const float width = 280f;
            const float height = 140f;
            float x = (sceneView.position.width - width) * 0.5f;
            float y = 10f;

            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);

            GUI.color = Color.yellow;

            GUILayout.Label($"Heel: {stabilityVis.LastHeelDeg:F1}°");
            GUILayout.Label($"GM:   {stabilityVis.LastGM:F3} m   (max {stabilityVis.HighestGM:F3} m)");
            GUILayout.Label($"GZ:   {stabilityVis.LastGZ:F3} m");
            GUILayout.Label($"Roll rate: {stabilityVis.LastRollRateDeg:F1} °/s");

            GUILayout.Space(5f);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Run GM/GZ Stability Scan"))
            {
                stabilityVis.StartGMGZScan();
            }
            GUI.enabled = true;

            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }
}
#endif