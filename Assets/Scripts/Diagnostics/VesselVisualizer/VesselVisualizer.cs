using UnityEngine;
using Axiom.Vessel.Diagnostics;

namespace Axiom.Diagnostics.Visualization
{
    public class VesselVisualizer : MonoBehaviour
    {
        public COMVisualizer comVisualizer;
        public COBVisualizer cobVisualizer;
        public OrientationVisualizer orientationVisualizer;
        public WaterplaneVisualizer waterplaneVisualizer;
        public ForcesAndStabilityVisualizer forcesAndStabilityVisualizer;
        public RightingMomentVisualizer RightingMomentVisualizer;
        public BuoyancyProbeForceVisualizer buoyancyForceVisualizer;

        private void OnDrawGizmos()
        {
            // Compute COM world position for modules that need it
            Vector3 comWorld = Vector3.zero;
            if (comVisualizer != null && comVisualizer.rb != null)
                comWorld = comVisualizer.rb.worldCenterOfMass;

            // Draw modules
            comVisualizer?.Draw();
            cobVisualizer?.Draw();
            orientationVisualizer?.Draw(comWorld);
            waterplaneVisualizer?.Draw();
            forcesAndStabilityVisualizer?.Draw();
            RightingMomentVisualizer?.Draw();
            buoyancyForceVisualizer?.Draw();

        }
    }
}