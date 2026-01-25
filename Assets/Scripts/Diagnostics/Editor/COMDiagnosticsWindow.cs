using UnityEngine;
using UnityEditor;
using Axiom.Vessel.Diagnostics;

namespace Axiom.Diagnostics.Visualization.Editor
{
    public sealed class COMDiagnosticsWindow : EditorWindow
    {
        // -----------------------------
        // Static overlay state
        // -----------------------------

        private static Rect overlayRect;
        private static bool dragging = false;
        private static Vector2 dragOffset;

        private const string OverlayPrefKey = "Axiom_COMOverlay_Enabled";
        private const string OverlayXKey = "Axiom_COMOverlay_PosX";
        private const string OverlayYKey = "Axiom_COMOverlay_PosY";

        // Static constructor: always hooks overlay
        static COMDiagnosticsWindow()
        {
            SceneView.duringSceneGui += DrawOverlayStatic;

            // Load saved position or default
            float x = EditorPrefs.GetFloat(OverlayXKey, 80f);
            float y = EditorPrefs.GetFloat(OverlayYKey, 10f);
            overlayRect = new Rect(x, y, 200f, 60f);
        }

        // -----------------------------
        // Menu Items
        // -----------------------------

        [MenuItem("Axiom/Diagnostics/COM Diagnostics")]
        public static void OpenWindow()
        {
            GetWindow<COMDiagnosticsWindow>("COM Diagnostics");
        }

        [MenuItem("Axiom/Diagnostics/Show COM Overlay")]
        private static void ToggleOverlay()
        {
            bool enabled = !EditorPrefs.GetBool(OverlayPrefKey, true);
            EditorPrefs.SetBool(OverlayPrefKey, enabled);
            Menu.SetChecked("Axiom/Diagnostics/Show COM Overlay", enabled);
            SceneView.RepaintAll();
        }

        [MenuItem("Axiom/Diagnostics/Show COM Overlay", true)]
        private static bool ValidateOverlay()
        {
            Menu.SetChecked("Axiom/Diagnostics/Show COM Overlay",
                EditorPrefs.GetBool(OverlayPrefKey, true));
            return true;
        }

        // -----------------------------
        // Instance window fields
        // -----------------------------

        private BoatCOM targetCOM;
        private GUIStyle headerStyle;
        private GUIStyle valueStyle;

        private void OnEnable()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white }
            };

            valueStyle = new GUIStyle(EditorStyles.label);
        }

        // -----------------------------
        // Window GUI
        // -----------------------------

        private void OnGUI()
        {
            ResolveTarget();

            GUILayout.Space(5f);
            GUILayout.Label("COM Diagnostics", headerStyle);
            GUILayout.Space(5f);

            if (targetCOM == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a GameObject with a BoatCOM component.",
                    MessageType.Info
                );
                return;
            }

            DrawCOMInfo();
            DrawControls();
        }

        private void ResolveTarget()
        {
            if (Selection.activeGameObject == null)
            {
                targetCOM = null;
                return;
            }

            targetCOM = Selection.activeGameObject.GetComponent<BoatCOM>();
        }

        private void DrawCOMInfo()
        {
            float com = targetCOM.comHeight;
            float neutral = targetCOM.NeutralBandMin;

            Color healthColor =
                com < neutral ? Color.red :
                com < neutral + 0.1f ? new Color(1f, 0.65f, 0f) :
                Color.green;

            valueStyle.normal.textColor = healthColor;

            GUILayout.Label($"COM Height: {com:F3} m", valueStyle);
            GUILayout.Label($"Neutral Band: {neutral:F3} m");
            GUILayout.Space(5f);
        }

        private void DrawControls()
        {
            EditorGUI.BeginChangeCheck();

            bool newEnable = EditorGUILayout.Toggle("Enable COM Offset", targetCOM.enableCOMOffset);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetCOM, "Toggle COM Offset");
                targetCOM.enableCOMOffset = newEnable;
                targetCOM.ApplyCOM();
                targetCOM.CheckNeutralBand();
            }
        }

        // -----------------------------
        // STATIC OVERLAY
        // -----------------------------

        private static void DrawOverlayStatic(SceneView sceneView)
        {
            // Overlay disabled?
            if (!EditorPrefs.GetBool(OverlayPrefKey, true))
                return;

            // Resolve selected BoatCOM
            BoatCOM com = null;
            if (Selection.activeGameObject != null)
                com = Selection.activeGameObject.GetComponent<BoatCOM>();

            // Auto-hide when no boat selected
            if (com == null)
                return;

            Handles.BeginGUI();

            // Background
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.Box(overlayRect, GUIContent.none);
            GUI.color = Color.white;

            GUILayout.BeginArea(overlayRect);

            float comVal = com.comHeight;
            float neutral = com.NeutralBandMin;

            Color healthColor =
                comVal < neutral ? Color.red :
                comVal < neutral + 0.1f ? new Color(1f, 0.65f, 0f) :
                Color.green;

            GUIStyle miniStyle = new GUIStyle(EditorStyles.label);
            miniStyle.normal.textColor = healthColor;

            GUILayout.Label($"COM: {comVal:F3} m", miniStyle);
            GUILayout.Label($"Neutral Band: {neutral:F3} m");

            

            GUILayout.EndArea();

            HandleDraggingStatic();

            Handles.EndGUI();
        }

        // -----------------------------
        // STATIC DRAGGING LOGIC
        // -----------------------------

        private static void HandleDraggingStatic()
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && overlayRect.Contains(e.mousePosition))
            {
                dragging = true;
                dragOffset = e.mousePosition - new Vector2(overlayRect.x, overlayRect.y);
                e.Use();
            }

            if (dragging && e.type == EventType.MouseDrag)
            {
                overlayRect.position = e.mousePosition - dragOffset;
                SaveOverlayPosition();
                e.Use();
            }

            if (e.type == EventType.MouseUp)
            {
                dragging = false;
            }
        }

        private static void SaveOverlayPosition()
        {
            EditorPrefs.SetFloat(OverlayXKey, overlayRect.x);
            EditorPrefs.SetFloat(OverlayYKey, overlayRect.y);
        }
    }
}