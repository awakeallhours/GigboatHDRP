#if UNITY_EDITOR
using Axiom.Vessel.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Axiom.Diagnostics.Visualization
{
    public sealed class RightingMomentVisualizer : MonoBehaviour
    {
        public VesselBootstrap bootstrap;
        public BoatCOB boatCOB;
        public Rigidbody rb;

        public bool drawRightingMoment = true;

        public void Draw()
        {
            if (!drawRightingMoment)
                return;

            if (!Application.isPlaying)
                return;

            if (bootstrap == null || boatCOB == null || rb == null)
                return;

            Vector3 comWorld = rb.worldCenterOfMass;
            Vector3 cobWorld = boatCOB.COBWorldPosition;

            Vector3 rollAxis = bootstrap.Orientation.RollAxis;
            if (rollAxis == null)
                return;

            Vector3 lever = cobWorld - comWorld;
            Vector3 leverPerp = Vector3.ProjectOnPlane(lever, rollAxis);

            if (leverPerp.sqrMagnitude < 0.0001f)
                return;

            Vector3 torqueDir = Vector3.Cross(leverPerp, rollAxis).normalized;

            Gizmos.color = new Color(0.8f, 0.3f, 1f);
            Gizmos.DrawLine(comWorld, comWorld + torqueDir * 2f);

            Handles.color = new Color(0.8f, 0.3f, 1f);
            Handles.Label(comWorld + torqueDir * 2f, "Righting Moment");
        }
    }
}
#endif