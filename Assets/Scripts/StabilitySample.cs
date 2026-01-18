namespace Axiom.Vessel.Diagnostics
{
    public struct StabilitySample
    {
        public float HeelDeg;
        public float GM;
        public float GZ;

        public StabilitySample(float heelDeg, float gm, float gz)
        {
            HeelDeg = heelDeg;
            GM = gm;
            GZ = gz;
        }
    }
}
