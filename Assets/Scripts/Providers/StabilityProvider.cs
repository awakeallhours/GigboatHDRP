public sealed class StabilityProvider
{
    private readonly StabilityProfile profile;

    public StabilityProvider(StabilityProfile profile)
    {
        this.profile = profile;
    }

    // GM
    public float GM_Initial => profile.GM_Initial;
    public bool GM_Initial_Valid => profile.GM_Initial_Valid;

    public float GM_Peak => profile.GM_Peak;
    public float GM_PeakAngle => profile.GM_PeakAngle;
    public bool GM_Peak_Valid => profile.GM_Peak_Valid;

    // GZ
    public float GZ_Peak => profile.GZ_Peak;
    public float GZ_PeakAngle => profile.GZ_PeakAngle;
    public bool GZ_Peak_Valid => profile.GZ_Peak_Valid;

    public float GZ_ZeroAngle => profile.GZ_ZeroAngle;
    public bool GZ_ZeroAngle_Valid => profile.GZ_ZeroAngle_Valid;

    // COM safety band
    public float COM_SafeMin => profile.COM_SafeMin;
    public float COM_SafeMax => profile.COM_SafeMax;

    // Metadata
    public float PositiveStabilityRange => profile.PositiveStabilityRange;
    public string Notes => profile.Notes;
}