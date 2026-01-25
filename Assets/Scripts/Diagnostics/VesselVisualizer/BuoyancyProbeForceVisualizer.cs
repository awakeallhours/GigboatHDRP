using UnityEngine;

namespace Axiom.Diagnostics.Visualization
{
    [ExecuteAlways]
    public sealed class BuoyancyProbeForceVisualizer : MonoBehaviour
    {
        [Header("References")]
        public Buoyancy buoyancy;
        public WaterProbeSampler sampler;

        [Header("Debug Toggle")]
        public bool drawBuoyancyForces = true;

        [Header("Depth Colouring")]
        public Color shallowColor = Color.blue;
        public Color deepColor = Color.red;
        public float maxDepthForColor = 2f;

        [Header("Force Vector")]
        public Color forceColor = Color.cyan;
        public float forceScale = 0.001f;

        // Cached probe data
        private bool[] valid;
        private float[] heights;
        private Vector3[] normals;
        private Transform[] points;
        private ProbeType[] types;

        public void Draw()
        {
            if (!drawBuoyancyForces)
                return;

            if (!Application.isPlaying)
                return;

            if (buoyancy == null || sampler == null)
                return;

            sampler.GetProbeData(out valid, out heights, out normals, out points, out types);

            if (valid == null || heights == null || normals == null || points == null)
                return;

            float strength = buoyancy.BuoyancyStrength;
            if (strength <= 0f)
                return;

            for (int i = 0; i < points.Length; i++)
            {
                if (!valid[i])
                    continue;

                Transform p = points[i];
                float waterY = heights[i];
                float depth = waterY - p.position.y;

                if (depth <= 0f)
                    continue;

                float depth01 = maxDepthForColor > 0f
                    ? Mathf.Clamp01(depth / maxDepthForColor)
                    : 1f;

                Color depthColor = Color.Lerp(shallowColor, deepColor, depth01);

                float forceMag = depth * strength;
                Vector3 forceVec = Vector3.up * forceMag * forceScale;

                Debug.DrawLine(p.position, p.position + Vector3.up * 0.05f, depthColor);
                Debug.DrawLine(p.position, p.position + forceVec, forceColor);
            }
        }
    }
}