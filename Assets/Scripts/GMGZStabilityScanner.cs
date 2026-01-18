using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    public sealed class GMGZStabilityScanner
    {
        private readonly Transform boat;
        private readonly Rigidbody rb;
        private readonly BoatCOB cob;
        private readonly BoatCOM com;

        public GMGZStabilityScanner(Transform boat, Rigidbody rb, BoatCOB cob, BoatCOM com)
        {
            this.boat = boat;
            this.rb = rb;
            this.cob = cob;
            this.com = com;
        }

        public IEnumerator RunScan(
            float startAngle,
            float endAngle,
            float step,
            float settleTime,
            Action<StabilityProfile> onComplete)
        {
            List<StabilitySample> samples = new List<StabilitySample>();

            for (float angle = startAngle; angle <= endAngle; angle += step)
            {
                // Apply heel
                boat.localRotation = Quaternion.Euler(0f, 0f, angle);
                rb.rotation = boat.rotation;
                rb.angularVelocity = Vector3.zero;

                // Let buoyancy settle
                yield return new WaitForSeconds(settleTime);

                // Compute GM/GZ
                float heelDeg = GMGZUtility.ComputeHeelAngle(boat);
                float gz = GMGZUtility.ComputeGZ(rb.worldCenterOfMass, cob.COBWorldPosition, boat.right);
                float gm = GMGZUtility.ComputeGM(heelDeg, gz);

                samples.Add(new StabilitySample(heelDeg, gm, gz));
            }

            // Build final profile
            var profile = StabilityProfileBuilder.Build(
                samples,
                com.NeutralBandMin,
                com.NeutralBandMin + 0.5f, // placeholder safe band
                "Auto-generated GM/GZ scan"
            );

            onComplete?.Invoke(profile);
        }
    }
}
