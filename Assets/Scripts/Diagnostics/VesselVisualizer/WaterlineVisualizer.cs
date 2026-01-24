using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Axiom.Vessel.Diagnostics;

namespace Axiom.Diagnostics.Visualization
{
    public sealed class WaterlineVisualizer : MonoBehaviour
    {
        [Header("References")]
        public VesselBootstrap bootstrap;
        public BoatCOB boatCOB;

        [Header("PLACEHOLDER Waterplane")]
        [Tooltip("TEMPORARY: Visual-only waterplane height. Real waterplane will come from hydrostatics.")]
        public bool useCOBHeight = true;

        [Tooltip("Optional override for placeholder waterplane height.")]
        public float manualWaterHeight = 0f;

        [Header("PLACEHOLDER Draft")]
        [Tooltip("TEMPORARY: Visual-only keel baseline. Real draft will come from hydrostatics.")]
        public Transform keelReference;

        [Header("Toggles")]
        public bool drawWaterplane = true;
        public bool drawDraft = true;

        [Header("Settings")]
        public float waterplaneRadius = 3f;
        public Color waterplaneColor = Color.cyan;
        public Color draftColor = Color.blue;

        public void Draw()
        {
#if UNITY_EDITOR
            if (bootstrap == null || boatCOB == null)
                return;

            // ─────────────────────────────────────────────
            // PLACEHOLDER WATERPLANE HEIGHT
            // ─────────────────────────────────────────────
            float waterHeight;

            if (useCOBHeight)
            {
                // TEMP: Using COB height as a stand-in for waterplane height.
                // This is NOT physically correct and will be replaced by hydrostatics.
                waterHeight = boatCOB.COBWorldPosition.y;
            }
            else
            {
                // TEMP: Manual override for testing.
                waterHeight = manualWaterHeight;
            }

            Vector3 waterplaneCenter = new Vector3(
                bootstrap.transform.position.x,
                waterHeight,
                bootstrap.transform.position.z
            );

            // ─────────────────────────────────────────────
            // DRAW PLACEHOLDER WATERPLANE (flat, world-horizontal)
            // ─────────────────────────────────────────────
            if (drawWaterplane)
            {
                Handles.color = waterplaneColor;
                Handles.DrawWireDisc(
                    waterplaneCenter,
                    Vector3.up,          // flat sea
                    waterplaneRadius
                );

                Handles.Label(
                    waterplaneCenter + Vector3.up * 0.2f,
                    "Waterplane (placeholder)"
                );
            }

            // ─────────────────────────────────────────────
            // PLACEHOLDER DRAFT (vertical distance from keel to waterplane)
            // ─────────────────────────────────────────────
            if (drawDraft && keelReference != null)
            {
                Vector3 keelPos = keelReference.position;

                Vector3 verticalOffset = Vector3.Project(
                    waterplaneCenter - keelPos,
                    Vector3.up
                );

                float draft = verticalOffset.magnitude;

                Gizmos.color = draftColor;
                Gizmos.DrawLine(
                    keelPos,
                    keelPos + verticalOffset
                );

                Handles.color = draftColor;
                Handles.Label(
                    keelPos + verticalOffset * 0.5f,
                    $"Draft (placeholder): {draft:F2} m"
                );
            }
#endif
        }
    }
}