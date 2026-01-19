namespace Axiom.Vessel.Diagnostics
{
    /// <summary>
    /// Represents a single GM/GZ stability measurement taken at a specific heel angle.
    /// 
    /// This struct is produced by the stability scanner and consumed by
    /// <see cref="StabilityProfileBuilder"/> to construct a full stability profile.
    /// 
    /// Each sample contains:
    /// - HeelDeg : Signed heel angle in degrees (left/right)
    /// - GM      : Metacentric height computed at this angle
    /// - GZ      : Righting arm (lever arm) computed at this angle
    /// 
    /// IMPORTANT:
    /// This struct contains raw, unprocessed data. It does NOT interpret stability.
    /// Interpretation (peaks, zero-crossings, ranges) happens in the builder.
    /// </summary>
    public struct StabilitySample
    {
        /// <summary>
        /// Signed heel angle in degrees at which this sample was taken.
        /// Positive/negative sign indicates heel direction based on the vessel's
        /// detected roll axis and orientation profile.
        /// </summary>
        public float HeelDeg;

        /// <summary>
        /// Metacentric height (GM) computed at this heel angle.
        /// Represents the vessel's instantaneous stability response.
        /// 
        /// NOTE:
        /// GM may be zero or negative at small angles or if the vessel is unstable.
        /// </summary>
        public float GM;

        /// <summary>
        /// Righting arm (GZ) computed at this heel angle.
        /// Represents the perpendicular distance between COM and COB projected
        /// onto the righting plane.
        /// 
        /// GZ > 0  → vessel is self-righting at this angle  
        /// GZ = 0  → vanishing stability  
        /// GZ < 0  → overturning moment
        /// </summary>
        public float GZ;

        /// <summary>
        /// Constructs a new stability sample with explicit heel, GM, and GZ values.
        /// </summary>
        public StabilitySample(float heelDeg, float gm, float gz)
        {
            HeelDeg = heelDeg;
            GM = gm;
            GZ = gz;
        }
    }
}