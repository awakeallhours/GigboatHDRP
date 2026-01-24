using UnityEngine;
using Axiom.Vessel.Diagnostics;

namespace Axiom.Diagnostics.Visualization
{
    public class VesselVisualizer : MonoBehaviour
    {
        public COMVisualizer comVisualizer;
        public COBVisualizer cobVisualizer;
        public ProbeVisualizer probeVisualizer;
        public StabilityVisualizer stabilityVisualizer;
        public OrientationVisualizer orientationVisualizer;
        public WaterlineVisualizer waterlineVisualizer;
        public ForceVisualizer forceVisualizer;
        public ScanVisualizer scanVisualizer;

        private void OnDrawGizmos()
        {
            // Compute COM world position for modules that need it
            Vector3 comWorld = Vector3.zero;
            if (comVisualizer != null && comVisualizer.rb != null)
                comWorld = comVisualizer.rb.worldCenterOfMass;

            // Draw modules
            comVisualizer?.Draw();
            cobVisualizer?.Draw();
            probeVisualizer?.Draw();
            stabilityVisualizer?.Draw();
            orientationVisualizer?.Draw(comWorld);
            waterlineVisualizer?.Draw();
            forceVisualizer?.Draw();
            scanVisualizer?.Draw();
        }
    }
}