using UnityEngine;

public class StabilityProfileComponent : MonoBehaviour
{
    [SerializeField]
    private StabilityProfile profile;

    public StabilityProfile Profile => profile;

    public void SetProfile(StabilityProfile newProfile)
    {
        profile = newProfile;
    }
}