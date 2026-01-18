using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    public static class GMGZUtility
    {
        // Compute heel angle in degrees
        public static float ComputeHeelAngle(Transform boat)
        {
            Vector3 up = boat.up;
            Vector3 rollAxis = boat.right;

            float heelSign = Mathf.Sign(Vector3.Dot(Vector3.Cross(Vector3.up, up), rollAxis));
            float heelAngleRad = Mathf.Acos(Mathf.Clamp(Vector3.Dot(Vector3.up, up), -1f, 1f));
            heelAngleRad *= heelSign;

            return heelAngleRad * Mathf.Rad2Deg;
        }

        // Compute GZ (righting arm)
        public static float ComputeGZ(Vector3 comWorld, Vector3 cobWorld, Vector3 rollAxis)
        {
            Vector3 lever = cobWorld - comWorld;
            Vector3 leverPerp = Vector3.ProjectOnPlane(lever, rollAxis);
            return leverPerp.magnitude;
        }

        // Compute GM (metacentric height)
        public static float ComputeGM(float heelDeg, float gz)
        {
            float heelRad = heelDeg * Mathf.Deg2Rad;
            float sinHeel = Mathf.Sin(heelRad);

            if (Mathf.Abs(sinHeel) < 0.0001f)
                return 0f;

            return gz / sinHeel;
        }
    }
}