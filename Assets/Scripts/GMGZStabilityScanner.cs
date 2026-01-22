using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    public sealed class GMGZStabilityScanner
    {
        private readonly VesselBootstrap bootstrap;
        private readonly Transform boat;
        private readonly Rigidbody rb;
        private readonly BoatCOB cob;
        private readonly BoatCOM com;

        public GMGZStabilityScanner(
            VesselBootstrap bootstrap,
            Transform boat,
            Rigidbody rb,
            BoatCOB cob,
            BoatCOM com)
        {
            this.bootstrap = bootstrap;
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
                // ------------------------------------------------------------
                // 1. LOCAL roll axis (from orientation detector)
                // ------------------------------------------------------------
                Vector3 rollAxisLocal =
                bootstrap.Orientation.RollAxis *
                bootstrap.Orientation.RollDirection;

               

                // ------------------------------------------------------------
                // 2. Apply heel in LOCAL space
                // ------------------------------------------------------------
                boat.localRotation = Quaternion.AngleAxis(angle, rollAxisLocal);

                

                // ------------------------------------------------------------
                // 3. Sync rigidbody to match transform
                // ------------------------------------------------------------
                rb.rotation = boat.rotation;
                rb.angularVelocity = Vector3.zero;

                

                yield return new WaitForSeconds(settleTime);



                // ------------------------------------------------------------
                // 4. Convert roll axis to WORLD space
                // ------------------------------------------------------------
                Vector3 rollAxisWorld = boat.TransformDirection(rollAxisLocal);

                // ------------------------------------------------------------
                // 5. Compute heel angle using the SAME roll axis
                // ------------------------------------------------------------
                float heelDeg = GMGZUtility.ComputeHeelAngle(boat, rollAxisWorld);

                // ------------------------------------------------------------
                // 6. Compute GZ using the SAME roll axis
                // ------------------------------------------------------------
                float gz = GMGZUtility.ComputeGZ(
                    rb.worldCenterOfMass,
                    cob.COBWorldPosition,
                    rollAxisWorld);

                // ------------------------------------------------------------
                // 7. Compute GM
                // ------------------------------------------------------------
                float gm = GMGZUtility.ComputeGM(heelDeg, gz);

                samples.Add(new StabilitySample(heelDeg, gm, gz));
            }

            var profile = StabilityProfileBuilder.Build(
                samples,
                com.NeutralBandMin,
                com.NeutralBandMin + 0.5f,
                "Auto-generated GM/GZ scan"
            );

            onComplete?.Invoke(profile);

            

        }
    }
}