using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Axiom.Vessel.Diagnostics;

namespace Axiom.Diagnostics.Visualization
{
    public sealed class VelocityVisualizer : MonoBehaviour
    {
        [Header("References")]
        public VesselBootstrap bootstrap;
        public VelocityProvider velocityProvider;
       

        [Header("Settings")]
        public float linearScale = 1f;
        public float angularScale = 0.5f;
        public float slipScale = 2f;

        public Color linearColor = Color.green;
        public Color angularColor = Color.magenta;
        public Color slipColor = Color.magenta;

        public bool drawLinear = true;
        public bool drawAngular = true;
        public bool drawSlip = true;
        public bool drawLabels = true;

        public void Draw()
        {
#if UNITY_EDITOR
            if (bootstrap == null || velocityProvider == null)
                return;

            // ─────────────────────────────────────────────
            // ORIGIN = COM (world space)
            // ─────────────────────────────────────────────
            Vector3 origin = velocityProvider.Rigidbody.worldCenterOfMass;

            // ─────────────────────────────────────────────
            // LINEAR VELOCITY
            // ─────────────────────────────────────────────
            if (drawLinear)
            {
                Vector3 v = velocityProvider.WorldVelocity;
                Vector3 end = origin + v * linearScale;

                Handles.color = linearColor;
                Handles.DrawLine(origin, end);

                if (v.sqrMagnitude > 0.0001f)
                {
                    Handles.ConeHandleCap(
                        0,
                        end,
                        Quaternion.LookRotation(v.normalized),
                        0.2f,
                        EventType.Repaint
                    );
                }

                if (drawLabels)
                {
                    Handles.Label(
                        end + Vector3.up * 0.1f,
                        $"Linear: {v.magnitude:F2} m/s"
                    );
                }
            }

            // ─────────────────────────────────────────────
            // SLIP (LATERAL VELOCITY)
            // ─────────────────────────────────────────────
            if (drawSlip)
            {
                Vector3 v = velocityProvider.WorldVelocity;

                if (v.sqrMagnitude > 0.0001f)
                {
                    Vector3 local = velocityProvider.LocalVelocity;

                    // Lateral = X component in local space
                    Vector3 lateral = new Vector3(local.x, 0f, 0f);

                    // Convert back to world
                    Vector3 lateralWorld = velocityProvider.Rigidbody.transform.TransformDirection(lateral);

                    Vector3 slipEnd = origin + lateralWorld * slipScale;

                    Handles.color = slipColor;
                    Handles.DrawLine(origin, slipEnd);

                    if (drawLabels)
                    {
                        Handles.Label(
                            slipEnd + Vector3.up * 0.1f,
                            $"Slip: {Mathf.Abs(lateral.x):F2} m/s"
                        );
                    }
                }
            }

            // ─────────────────────────────────────────────
            // ANGULAR VELOCITY
            // ─────────────────────────────────────────────
            if (drawAngular)
            {
                Vector3 w = velocityProvider.AngularVelocity;
                Vector3 end = origin + w * angularScale;

                Handles.color = angularColor;
                Handles.DrawLine(origin, end);

                if (w.sqrMagnitude > 0.0001f)
                {
                    Handles.ConeHandleCap(
                        0,
                        end,
                        Quaternion.LookRotation(w.normalized),
                        0.2f,
                        EventType.Repaint
                    );
                }

                if (drawLabels)
                {
                    Handles.Label(
                        end + Vector3.up * 0.1f,
                        $"Angular: {w.magnitude:F2} rad/s"
                    );
                }
            }
#endif
        }
    }
}