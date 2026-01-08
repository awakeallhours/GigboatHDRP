using UnityEngine;
using TMPro;

/// <summary>
/// Displays real-time debug information for buoyancy, hydrodynamics,
/// roll tuning, and propulsion state using a GigboatDebugProbe.
/// </summary>
public class BuoyancyDebugUI : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("The TextMeshProUGUI element used to display debug information.")]
    [SerializeField] private TextMeshProUGUI debugText;

    private GigboatDebugProbe probe;
    private bool warnedNoProbe = false;

    public void SetBoat(GigboatDebugProbe newProbe)
    {
        probe = newProbe;
        warnedNoProbe = false; // reset warning if a probe is later assigned
    }

    /// <summary>
    /// Returns a colour tag based on value sign or critical threshold.
    /// </summary>
    private string Colour(float value, float criticalThreshold = 0f)
    {
        if (criticalThreshold != 0f && Mathf.Abs(value) > criticalThreshold)
            return "<color=#FF4A4A>";   // red

        if (value > 0f) return "<color=#4AA3FF>";   // blue
        if (value < 0f) return "<color=#FFD93D>";   // yellow
        return "<color=#FFFFFF>";                   // white
    }

    private void Update()
    {
        if (debugText == null)
            return;

        // ---------------------------------------------------------
        // PROBE SAFETY
        // ---------------------------------------------------------
        if (probe == null)
        {
            if (!warnedNoProbe)
            {
                Debug.LogWarning("BuoyancyDebugUI: No GigboatDebugProbe assigned. Debug UI will show placeholder text.");
                warnedNoProbe = true;
            }

            debugText.text =
                "<b><size=120%>BUOYANCY DEBUG</size></b>\n\n" +
                "<color=#FF4A4A><b>NO PROBE CONNECTED</b></color>\n" +
                "Assign a GigboatDebugProbe to begin receiving data.";

            return;
        }

        var rb = probe.RB;
        string output = $"<b><size=120%>BUOYANCY DEBUG</size></b>\n";

        // ---------------------------------------------------------
        // PROPULSION
        // ---------------------------------------------------------
        output += $"\n<b><size=110%>PROPULSION</size></b>\n";
        output += $"Velocity: {Colour(rb.linearVelocity.magnitude)}{rb.linearVelocity.magnitude:F2}</color> m/s\n";
        output += $"Throttle: {Colour(probe.ThrottlePercent)}{probe.ThrottlePercent:F0}%</color>\n";
        output += $"RPM: {Colour(probe.RPM)}{probe.RPM:F0}</color>\n";

        // ---------------------------------------------------------
        // ATTITUDE & MOTION
        // ---------------------------------------------------------
        output += $"\n<b><size=110%>ATTITUDE & MOTION</size></b>\n";
        output += $"Roll: {Colour(probe.Roll, 25f)}{probe.Roll:F2}°</color>\n";
        output += $"Pitch: {Colour(probe.Pitch)}{probe.Pitch:F2}°</color>\n";
        output += $"Heave: {Colour(probe.Heave)}{probe.Heave:F2}</color>\n";
        output += $"Angular Vel: {Colour(rb.angularVelocity.magnitude)}{rb.angularVelocity.magnitude:F2}</color>\n";
        output += $"Yaw Rate: {Colour(probe.YawRateDeg)}{probe.YawRateDeg:F1}</color> °/s\n";
        output += $"Rudder: {Colour(probe.RudderAngle)}{probe.RudderAngle:F2}</color>\n";

        // ---------------------------------------------------------
        // BUOYANCY POINTS
        // ---------------------------------------------------------
        output += $"\n<b><size=110%>BUOYANCY POINTS</size></b>\n";

        if (probe.Points != null)
        {
            foreach (Transform p in probe.Points)
            {
                float depth = probe.GetPointDepth(p);
                output += $"{p.name}: {Colour(depth)}{depth:F2}</color>\n";
            }
        }

        // ---------------------------------------------------------
        // ROLL TUNING
        // ---------------------------------------------------------
        output += $"\n<b><size=110%>ROLL TUNING</size></b>\n";
        output += $"Damping: {Colour(probe.RollDamping)}{probe.RollDamping:F2}</color>\n";
        output += $"Stiffness Base: {Colour(probe.RollStiffnessBase)}{probe.RollStiffnessBase:F2}</color>\n";
        output += $"Stiffness Speed: {Colour(probe.RollStiffnessSpeed)}{probe.RollStiffnessSpeed:F2}</color>\n";
        output += $"Rudder Roll: {Colour(probe.RudderRoll)}{probe.RudderRoll:F2}</color>\n";
        output += $"Roll Threshold: {Colour(probe.RudderRollThreshold)}{probe.RudderRollThreshold:F2}</color>\n";

        // ---------------------------------------------------------
        // HYDRODYNAMICS
        // ---------------------------------------------------------
        output += $"\n<b><size=110%>HYDRODYNAMICS</size></b>\n";
        output += $"Forward Speed: {Colour(probe.ForwardSpeed)}{probe.ForwardSpeed:F2}</color> m/s\n";
        output += $"Forward Drag: {Colour(probe.ForwardDragMagnitude)}{probe.ForwardDragMagnitude:F2}</color> N\n";
        output += $"Forward Drag Vec: {Colour(probe.ForwardDragForce.magnitude)}{probe.ForwardDragForce}</color>\n";

        output += $"Lateral Speed: {Colour(probe.LateralSpeed)}{probe.LateralSpeed:F2}</color> m/s\n";
        output += $"Lateral Drag: {Colour(probe.LateralDragMagnitude)}{probe.LateralDragMagnitude:F2}</color> N\n";
        output += $"Lateral Drag Vec: {Colour(probe.LateralDragForce.magnitude)}{probe.LateralDragForce}</color>\n";

        output += $"Yaw Rate (Hydro): {Colour(probe.YawRateHydro)}{probe.YawRateHydro:F2}</color> rad/s\n";
        output += $"Yaw Damp Torque: {Colour(probe.YawDampingTorque)}{probe.YawDampingTorque:F2}</color> Nm\n";

        debugText.text = output;
    }
}