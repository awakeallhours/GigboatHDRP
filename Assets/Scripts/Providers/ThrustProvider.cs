using UnityEngine;

public sealed class ThrustProvider
{
    private readonly MarinePowertrainController power;

    public ThrustProvider(MarinePowertrainController power)
    {
        this.power = power;
    }

    public Vector3 Thrust => power.PropThrustVector;
    public float Throttle01 => power.Throttle01;
}