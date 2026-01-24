using UnityEngine;

public sealed class VelocityProvider
{
    private readonly Rigidbody rb;

    public VelocityProvider(Rigidbody rb)
    {
        this.rb = rb;
    }

    public Vector3 WorldVelocity => rb.linearVelocity;

    public Vector3 LocalVelocity =>
        rb.transform.InverseTransformDirection(rb.linearVelocity);

    public Vector3 AngularVelocity => rb.angularVelocity;

    public Rigidbody Rigidbody => rb;
}