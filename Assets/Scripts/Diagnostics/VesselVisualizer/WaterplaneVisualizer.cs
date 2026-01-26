using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Axiom.Diagnostics.Visualization
{
    public sealed class WaterplaneVisualizer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Waterplane estimator providing slice beams, areas, and LCF.")]
        public WaterplaneEstimator estimator;

        [Tooltip("Root transform of the vessel (same one used for probe sampling).")]
        public Transform vesselRoot;

        [Header("Toggles")]
        public bool drawSliceBeams = true;
        public bool drawWaterplanePolygon = true;
        public bool drawWaterline = true;
        public bool drawLCF = true;
        public bool drawHullBottom = false;

        [Header("Appearance")]
        public Color sliceBeamColor = Color.yellow;
        public Color waterplaneColor = Color.cyan;
        public Color waterlineColor = Color.green;
        public Color lcfColor = Color.magenta;
        public Color hullBottomColor = Color.blue;

        [Tooltip("Vertical offset applied when drawing the waterplane polygon (purely visual).")]
        public float waterplaneYOffset = 0.02f;

        [Tooltip("Local Y position of the hull bottom relative to vesselRoot.")]
        public float hullBottomLocalY = -0.5f;

        public void Draw()
        {
#if UNITY_EDITOR
            if (estimator == null || vesselRoot == null)
                return;

            int sliceCount = estimator.sliceBeam?.Length ?? 0;
            if (sliceCount == 0)
                return;

            float[] beams = estimator.sliceBeam;
            float minZ = estimator.MinZ;
            float sliceLength = estimator.SliceLength;

            // ─────────────────────────────────────────────
            // DRAW SLICE BEAMS
            // ─────────────────────────────────────────────
            if (drawSliceBeams)
            {
                Handles.color = sliceBeamColor;

                for (int s = 0; s < sliceCount; s++)
                {
                    float beam = beams[s];
                    if (beam <= 0f)
                        continue;

                    float z = minZ + (s + 0.5f) * sliceLength;

                    Vector3 left = vesselRoot.TransformPoint(new Vector3(-beam * 0.5f, 0f, z));
                    Vector3 right = vesselRoot.TransformPoint(new Vector3(beam * 0.5f, 0f, z));

                    Handles.DrawLine(left, right);
                }
            }

            // ─────────────────────────────────────────────
            // DRAW WATERPLANE POLYGON
            // ─────────────────────────────────────────────
            if (drawWaterplanePolygon)
            {
                Handles.color = waterplaneColor;

                Vector3[] poly = new Vector3[sliceCount * 2];
                int idx = 0;

                // Left side forward
                for (int s = 0; s < sliceCount; s++)
                {
                    float beam = beams[s];
                    float z = minZ + (s + 0.5f) * sliceLength;

                    poly[idx++] = vesselRoot.TransformPoint(
                        new Vector3(-beam * 0.5f, waterplaneYOffset, z)
                    );
                }

                // Right side backward
                for (int s = sliceCount - 1; s >= 0; s--)
                {
                    float beam = beams[s];
                    float z = minZ + (s + 0.5f) * sliceLength;

                    poly[idx++] = vesselRoot.TransformPoint(
                        new Vector3(beam * 0.5f, waterplaneYOffset, z)
                    );
                }

                Handles.DrawAAPolyLine(2f, poly);
            }

            // ─────────────────────────────────────────────
            // DRAW WATERLINE (left/right edges only)
            // ─────────────────────────────────────────────
            if (drawWaterline)
            {
                Handles.color = waterlineColor;

                for (int s = 0; s < sliceCount; s++)
                {
                    float beam = beams[s];
                    if (beam <= 0f)
                        continue;

                    float z = minZ + (s + 0.5f) * sliceLength;

                    Vector3 left = vesselRoot.TransformPoint(new Vector3(-beam * 0.5f, 0f, z));
                    Vector3 right = vesselRoot.TransformPoint(new Vector3(beam * 0.5f, 0f, z));

                    Handles.DrawLine(left, right);
                }
            }

            // ─────────────────────────────────────────────
            // DRAW LCF MARKER
            // ─────────────────────────────────────────────
            if (drawLCF)
            {
                float lcfZ = estimator.LCF;
                Vector3 lcfWorld = vesselRoot.TransformPoint(new Vector3(0f, waterplaneYOffset, lcfZ));

                Handles.color = lcfColor;
                Handles.SphereHandleCap(0, lcfWorld, Quaternion.identity, 0.15f, EventType.Repaint);
                Handles.Label(lcfWorld + Vector3.up * 0.1f, "LCF");
            }

            // ─────────────────────────────────────────────
            // DRAW HULL BOTTOM MARKER (optional)
            // ─────────────────────────────────────────────
            if (drawHullBottom)
            {
                Vector3 hullBottom = vesselRoot.TransformPoint(
                    new Vector3(0f, hullBottomLocalY, 0f)
                );

                Handles.color = hullBottomColor;
                Handles.SphereHandleCap(0, hullBottom, Quaternion.identity, 0.1f, EventType.Repaint);
                Handles.Label(hullBottom + Vector3.up * 0.1f, "Hull Bottom");
            }
#endif
        }
    }
}