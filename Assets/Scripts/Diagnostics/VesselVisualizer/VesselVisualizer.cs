using UnityEngine;
using Axiom.Vessel.Diagnostics;

namespace Axiom.Diagnostics.Visualization
{
    public class VesselVisualizer : MonoBehaviour
    {
        public COMVisualizer comVisualizer;
        public COBVisualizer cobVisualizer;
        public OrientationVisualizer orientationVisualizer;
        public WaterlineVisualizer waterlineVisualizer;
        public ForcesAndStabilityVisualizer forcesAndStabilityVisualizer;
        public RightingMomentVisualizer RightingMomentVisualizer;
        public BuoyancyProbeForceVisualizer buoyancyForceVisualizer;

        private void OnDrawGizmos()
        {
            // Compute COM world position for modules that need it
            Vector3 comWorld = Vector3.zero;
            if (comVisualizer != null && comVisualizer.rb != null)
                comWorld = comVisualizer.rb.worldCenterOfMass;

            // Ensure WaterlineVisualizer has a VelocityProvider
            if (waterlineVisualizer != null && comVisualizer != null && comVisualizer.rb != null)
            {
                if (waterlineVisualizer.velocityProvider == null)
                    waterlineVisualizer.velocityProvider = new VelocityProvider(comVisualizer.rb);
            }

            // Draw modules
            comVisualizer?.Draw();
            cobVisualizer?.Draw();
            orientationVisualizer?.Draw(comWorld);
            waterlineVisualizer?.Draw();
            forcesAndStabilityVisualizer?.Draw();
            RightingMomentVisualizer?.Draw();
            buoyancyForceVisualizer?.Draw();

        }
    }
}