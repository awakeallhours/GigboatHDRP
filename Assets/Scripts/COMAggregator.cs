using UnityEngine;
using Axiom.Vessel.Diagnostics;

namespace Axiom.Vessel.Mass
{
    [DisallowMultipleComponent]
    public sealed class COMAggregator : MonoBehaviour
    {
        [Header("Authority")]
        public BoatCOM boatCOM;

        [Header("Base Hull")]
        public float baseMass = 1000f;
        public Vector3 baseCOMLocal = new Vector3(0f, 0.35f, 0f);

        [Header("Fuel")]
        public float fuelMass;
        public Vector3 fuelCOMLocal;

        [Header("Ballast")]
        public float ballastMass;
        public Vector3 ballastCOMLocal;

        [Header("Auto Trim")]
        public Vector3 autoTrimOffsetLocal; // z only for now

        public float TotalMass
        {
            get; private set;
        }
        public Vector3 TotalCOMLocal
        {
            get; private set;
        }

        private void Reset()
        {
            if (boatCOM == null)
                boatCOM = GetComponent<BoatCOM>();
        }

        private void LateUpdate()
        {
            if (boatCOM == null)
                return;

            RecalculateCOM();
            ApplyToBoatCOM();
        }

        public void SetFuel(float mass, Vector3 comLocal)
        {
            fuelMass = mass;
            fuelCOMLocal = comLocal;
        }

        public void SetBallast(float mass, Vector3 comLocal)
        {
            ballastMass = mass;
            ballastCOMLocal = comLocal;
        }

        public void SetAutoTrimOffset(Vector3 localOffset)
        {
            autoTrimOffsetLocal = localOffset;
        }

        private void RecalculateCOM()
        {
            float totalMass =
                baseMass +
                fuelMass +
                ballastMass;

            if (totalMass <= 0f)
            {
                TotalMass = 0f;
                TotalCOMLocal = baseCOMLocal;
                return;
            }

            Vector3 weighted =
                baseCOMLocal * baseMass +
                fuelCOMLocal * fuelMass +
                ballastCOMLocal * ballastMass;

            Vector3 com = weighted / totalMass;

            // apply auto-trim as a pure offset (z only)
            com += autoTrimOffsetLocal;

            TotalMass = totalMass;
            TotalCOMLocal = com;
        }

        private void ApplyToBoatCOM()
        {
            boatCOM.comHeight = TotalCOMLocal.y;
            boatCOM.comForwardOffset = TotalCOMLocal.z;
            boatCOM.ApplyCOM();
        }
    }
}