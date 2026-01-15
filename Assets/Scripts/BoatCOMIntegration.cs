using Axiom.Vessel.Diagnostics;
using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class BoatCOMIntegration : MonoBehaviour
    {
        [Tooltip("Reference to the BoatCOM authority on this vessel.")]
        public BoatCOM boatCOM;

        [Header("Force Application Debug")]
        [Tooltip("Height in meters where lateral forces are applied (e.g. rudder, crossflow).")]
        public float lateralForceHeight = 0.35f;

        [Tooltip("If enabled, will warn when lateral force height is below COM or neutral band.")]
        public bool validateForceHeight = true;

        private void Reset()
        {
            if (boatCOM == null)
                boatCOM = GetComponent<BoatCOM>();
        }

        private void OnValidate()
        {
            if (validateForceHeight && boatCOM != null)
                ValidateLateralForceHeight();
        }

        public void ValidateLateralForceHeight()
        {
            if (boatCOM == null)
                return;

            float comY = boatCOM.comHeight;
            float neutralY = boatCOM.NeutralBandMin;

            if (lateralForceHeight < neutralY)
            {
                Debug.LogWarning(
                    $"[BoatCOMIntegration] Lateral force height ({lateralForceHeight} m) is BELOW neutral band ({neutralY} m). " +
                    $"Roll behaviour will be inverted relative to COM."
                );
            }
            else if (lateralForceHeight < comY)
            {
                Debug.LogWarning(
                    $"[BoatCOMIntegration] Lateral force height ({lateralForceHeight} m) is BELOW COM ({comY} m). " +
                    $"Boat will tend to roll the wrong way under lateral load."
                );
            }
        }

        // Example hook for LateralForceDebug or other tools:
        public void SetLateralForceHeight(float heightMeters)
        {
            lateralForceHeight = heightMeters;
            if (validateForceHeight && boatCOM != null)
                ValidateLateralForceHeight();
        }
    }
}
