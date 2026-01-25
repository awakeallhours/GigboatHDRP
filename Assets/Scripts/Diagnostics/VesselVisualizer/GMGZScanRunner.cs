#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using Axiom.Vessel.Diagnostics;


namespace Axiom.Vessel.Stability.Editor
{
    public static class GMGZScanRunner
    {
        public static IEnumerator RunScan(
            VesselBootstrap bootstrap,
            Rigidbody rb,
            BoatCOB boatCOB,
            BoatCOM boatCOM,
            StabilityProfileComponent stabilityProfileComponent)
        {
            if (bootstrap == null || rb == null || boatCOB == null || boatCOM == null || stabilityProfileComponent == null)
            {
                Debug.LogWarning("[GM/GZ Scan] Missing dependencies, aborting scan.");
                yield break;
            }

            var scanner = new GMGZStabilityScanner(
                bootstrap,
                bootstrap.transform,
                rb,
                boatCOB,
                boatCOM
            );

            yield return scanner.RunScan(
                startAngle: 0f,
                endAngle: 45f,
                step: 1f,
                settleTime: 0.25f,
                onComplete: profile =>
                {
                    stabilityProfileComponent.SetProfile(profile);
                    LogResults(profile);
                });
        }

        private static void LogResults(StabilityProfile profile)
        {
            Debug.Log(
                "<b>[GM/GZ Stability Scan Results]</b>\n" +
                "\n" +
                $"<b>Initial Stability (GM_Initial):</b> {profile.GM_Initial:F3} m   " +
                $"Valid={profile.GM_Initial_Valid}\n" +
                "Plain: Stability when the boat first starts to lean.\n" +
                "\n" +
                $"<b>Strongest Stability (GM_Peak):</b> {profile.GM_Peak:F3} m @ {profile.GM_PeakAngle:F1}°   " +
                $"Valid={profile.GM_Peak_Valid}\n" +
                "Plain: The strongest overall stability the boat showed.\n" +
                "\n" +
                $"<b>Strongest Righting Force (GZ_Peak):</b> {profile.GZ_Peak:F3} m @ {profile.GZ_PeakAngle:F1}°   " +
                $"Valid={profile.GZ_Peak_Valid}\n" +
                "Plain: The strongest force pushing the boat upright.\n" +
                "\n" +
                $"<b>Vanishing Stability Angle (GZ_ZeroAngle):</b> {profile.GZ_ZeroAngle:F1}°   " +
                $"Valid={profile.GZ_ZeroAngle_Valid}\n" +
                "Plain: The angle where the boat stops being able to right itself.\n" +
                "\n" +
                $"<b>Positive Stability Range:</b> {profile.PositiveStabilityRange:F1}°\n" +
                "Plain: How far the boat can lean while still being stable.\n" +
                "\n" +
                $"<b>COM Safe Range:</b> {profile.COM_SafeMin:F3} m → {profile.COM_SafeMax:F3} m\n" +
                "Plain: Lowest and highest safe centre‑of‑mass height.\n" +
                "\n" +
                $"<b>Notes:</b> {profile.Notes}"
            );
        }
    }
}
#endif