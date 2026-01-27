using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Axiom.Diagnostics.Visualization
{
    public sealed class WaterplaneVisualizer : MonoBehaviour
    {
        [Header("References")]
        public WaterplaneEstimator estimator;
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
            float minZ = estimator.MinZ;          // LOCAL Z
            float sliceLength = estimator.SliceLength;

            // ─────────────────────────────────────────────
            // SLICE BEAMS
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

                    Vector3 leftLocal = new Vector3(-beam * 0.5f, 0f, z);
                    Vector3 rightLocal = new Vector3(beam * 0.5f, 0f, z);

                    Handles.DrawLine(
                        vesselRoot.TransformPoint(leftLocal),
                        vesselRoot.TransformPoint(rightLocal)
                    );
                }
            }

            // ─────────────────────────────────────────────
            // WATERPLANE POLYGON
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

                    Vector3 pLocal = new Vector3(-beam * 0.5f, waterplaneYOffset, z);
                    poly[idx++] = vesselRoot.TransformPoint(pLocal);
                }

                // Right side backward
                for (int s = sliceCount - 1; s >= 0; s--)
                {
                    float beam = beams[s];
                    float z = minZ + (s + 0.5f) * sliceLength;

                    Vector3 pLocal = new Vector3(beam * 0.5f, waterplaneYOffset, z);
                    poly[idx++] = vesselRoot.TransformPoint(pLocal);
                }

                Handles.DrawAAPolyLine(2f, poly);
            }

            // ─────────────────────────────────────────────
            // WATERLINE
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

                    Vector3 leftLocal = new Vector3(-beam * 0.5f, 0f, z);
                    Vector3 rightLocal = new Vector3(beam * 0.5f, 0f, z);

                    Handles.DrawLine(
                        vesselRoot.TransformPoint(leftLocal),
                        vesselRoot.TransformPoint(rightLocal)
                    );
                }
            }

            // ─────────────────────────────────────────────
            // LCF MARKER
            // ─────────────────────────────────────────────
            if (drawLCF)
            {
                float lcfZ = estimator.LCF;

                Vector3 lcfLocal = new Vector3(0f, waterplaneYOffset, lcfZ);
                Vector3 lcfWorld = vesselRoot.TransformPoint(lcfLocal);

                Handles.color = lcfColor;
                Handles.SphereHandleCap(0, lcfWorld, Quaternion.identity, 0.15f, EventType.Repaint);
                Handles.Label(lcfWorld + Vector3.up * 0.1f, "LCF");
            }

            // ─────────────────────────────────────────────
            // HULL BOTTOM MARKER
            // ─────────────────────────────────────────────
            if (drawHullBottom)
            {
                Vector3 bottomLocal = new Vector3(0f, hullBottomLocalY, 0f);
                Vector3 bottomWorld = vesselRoot.TransformPoint(bottomLocal);

                Handles.color = hullBottomColor;
                Handles.SphereHandleCap(0, bottomWorld, Quaternion.identity, 0.1f, EventType.Repaint);
                Handles.Label(bottomWorld + Vector3.up * 0.1f, "Hull Bottom");
            }
#endif
        }
    }
}