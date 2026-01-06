using UnityEngine;
using TMPro;

public class BuoyancyDebugUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;

    private GigboatDebugProbe probe;

    public void SetBoat(GigboatDebugProbe newProbe)
    {
        probe = newProbe;
    }

    private string Colour(float value, float criticalThreshold = 0f)
    {
        if (criticalThreshold != 0f && Mathf.Abs(value) > criticalThreshold)
            return "<color=#FF4A4A>";   // red

        if (value > 0f) return "<color=#4AA3FF>";   // blue
        if (value < 0f) return "<color=#FFD93D>";   // yellow
        return "<color=#FFFFFF>";                   // white
    }

    void Update()
    {
        if (probe == null || debugText == null) return;

        var rb = probe.RB;

        string output = $"<b><size=120%>BUOYANCY DEBUG</size></b>\n";

        output += $"Roll: {Colour(probe.Roll, 25f)}{probe.Roll:F2}°</color>\n";
        output += $"Pitch: {Colour(probe.Pitch)}{probe.Pitch:F2}°</color>\n";
        output += $"Heave: {Colour(probe.Heave)}{probe.Heave:F2}</color>\n";
        output += $"Velocity: {Colour(rb.linearVelocity.magnitude)}{rb.linearVelocity.magnitude:F2}</color> m/s\n";
        output += $"Angular Vel: {Colour(rb.angularVelocity.magnitude)}{rb.angularVelocity.magnitude:F2}</color>\n";
        output += $"Yaw Rate: {Colour(probe.YawRateDeg)}{probe.YawRateDeg:F1}</color> °/s\n";
        output += $"Rudder: {Colour(probe.RudderAngle)}{probe.RudderAngle:F2}</color>\n";


        output += $"\n<b><size=110%>BUOYANCY POINTS</size></b>\n";
        foreach (Transform p in probe.Points)
        {
            float depth = probe.GetPointDepth(p);
            output += $"{p.name}: {Colour(depth)}{depth:F2}</color>\n";
        }

        
        output += $"\n<b><size=110%>ROLL TUNING</size></b>\n";
        output += $"Damping: {Colour(probe.RollDamping)}{probe.RollDamping:F2}</color>\n";
        output += $"Stiffness Base: {Colour(probe.RollStiffnessBase)}{probe.RollStiffnessBase:F2}</color>\n";
        output += $"Stiffness Speed: {Colour(probe.RollStiffnessSpeed)}{probe.RollStiffnessSpeed:F2}</color>\n";
        output += $"Rudder Roll: {Colour(probe.RudderRoll)}{probe.RudderRoll:F2}</color>\n";
        output += $"Roll Threshold: {Colour(probe.RudderRollThreshold)}{probe.RudderRollThreshold:F2}</color>\n";

        output += $"\n<b><size=110%>HYDRODYNAMICS</size></b>\n";
        output += $"Forward Speed: {Colour(probe.ForwardSpeed)}{probe.ForwardSpeed:F2}</color> m/s\n";
        output += $"Forward Drag: {Colour(probe.ForwardDragMagnitude)}{probe.ForwardDragMagnitude:F2}</color> N\n";
        output += $"Forward Drag Vec: {Colour(probe.ForwardDragForce.magnitude)}{probe.ForwardDragForce}</color>\n";

        output += $"Lateral Speed: {Colour(probe.LateralSpeed)}{probe.LateralSpeed:F2}</color> m/s\n";
        output += $"Lateral Drag: {Colour(probe.LateralDragMagnitude)}{probe.LateralDragMagnitude:F2}</color> N\n";
        output += $"Lateral Drag Vec: {Colour(probe.LateralDragForce.magnitude)}{probe.LateralDragForce}</color>\n";

        output += $"Yaw Rate (Hydro): {Colour(probe.YawRateHydro)}{probe.YawRateHydro:F2}</color> rad/s\n";
        output += $"Yaw Damp Torque: {Colour(probe.YawDampingTorque)}{probe.YawDampingTorque:F2}</color> Nm\n";

        output += $"Throttle: {Colour(probe.ThrottlePercent)}{probe.ThrottlePercent:F0}%</color>\n";
        output += $"RPM: {Colour(probe.RPM)}{probe.RPM:F0}</color>\n";


        //output += $"Planing: {(probe.IsPlaning ? "<color=#4AFF4A>YES</color>" : "<color=#FF4A4A>NO</color>")}\n"; // not yet ready to tell the truth (need new boat model)


        debugText.text = output;
    }
}