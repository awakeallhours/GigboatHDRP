using UnityEngine;

public sealed class MassPropertiesProvider
{
    private readonly Rigidbody rb;

    public MassPropertiesProvider(Rigidbody rb)
    {
        this.rb = rb;
    }

    public float Mass => rb.mass;

    public Vector3 CenterOfMass => rb.worldCenterOfMass;

    public Vector3 InertiaTensor => rb.inertiaTensor;

    public Quaternion InertiaTensorRotation => rb.inertiaTensorRotation;
}