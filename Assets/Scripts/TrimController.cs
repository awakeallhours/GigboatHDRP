using UnityEngine;
using Axiom.Vessel.Mass;
using Axiom.Vessel.Diagnostics;

namespace Axiom.Vessel.Trim
{
    [DisallowMultipleComponent]
    public sealed class TrimController : MonoBehaviour
    {
        [Header("References")]
        public COMAggregator comAggregator;
        public BoatCOM boatCOM;
        public Transform waterplaneLCF; // world-space LCF reference
        public Rigidbody rb;

        [Header("Activation Thresholds")]
        public float speedThreshold = 0.2f;          // m/s
        public float throttleThreshold = 0.01f;      // normalized
        public float angularVelThreshold = 0.05f;    // rad/s
        public float verticalVelThreshold = 0.05f;   // m/s

        [Header("Trim Logic")]
        public float deadband = 0.05f;               // m
        public float hysteresis = 0.02f;             // m
        public float trimSpeed = 0.05f;              // m/s
        public float stabilityTime = 2.0f;           // seconds required to confirm bias

        [Header("Filtering")]
        public float smoothing = 0.1f;               // low-pass filter factor

        private float filteredCOMZ;
        private float filteredLCFZ;
        private float biasTimer = 0f;
        private bool trimmingActive = false;

        // External input (hook this up later)
        [HideInInspector] public float currentThrottle;

        private void Reset()
        {
            if (comAggregator == null)
                comAggregator = GetComponent<COMAggregator>();
            if (boatCOM == null)
                boatCOM = GetComponent<BoatCOM>();
            if (rb == null)
                rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!IsEligible())
            {
                biasTimer = 0f;
                trimmingActive = false;
                return;
            }

            UpdateFilteredValues();
            EvaluateBias();
            ApplyTrimIfNeeded();
        }

        private bool IsEligible()
        {
            if (comAggregator == null || boatCOM == null || waterplaneLCF == null || rb == null)
                return false;

            if (rb.linearVelocity.magnitude > speedThreshold)
                return false;

            if (Mathf.Abs(currentThrottle) > throttleThreshold)
                return false;

            if (rb.angularVelocity.magnitude > angularVelThreshold)
                return false;

            if (Mathf.Abs(rb.linearVelocity.y) > verticalVelThreshold)
                return false;

            return true;
        }

        private void UpdateFilteredValues()
        {
            float rawCOMZ = comAggregator.TotalCOMLocal.z;
            float rawLCFZ = transform.InverseTransformPoint(waterplaneLCF.position).z;

            filteredCOMZ = Mathf.Lerp(filteredCOMZ, rawCOMZ, smoothing);
            filteredLCFZ = Mathf.Lerp(filteredLCFZ, rawLCFZ, smoothing);
        }

        private void EvaluateBias()
        {
            float offset = filteredCOMZ - filteredLCFZ;
            float absOffset = Mathf.Abs(offset);

            float enterThreshold = deadband + hysteresis;
            float exitThreshold = deadband;

            if (!trimmingActive)
            {
                if (absOffset > enterThreshold)
                {
                    biasTimer += Time.deltaTime;
                    if (biasTimer >= stabilityTime)
                        trimmingActive = true;
                }
                else
                {
                    biasTimer = 0f;
                }
            }
            else
            {
                if (absOffset < exitThreshold)
                {
                    trimmingActive = false;
                    biasTimer = 0f;
                }
            }
        }

        private void ApplyTrimIfNeeded()
        {
            if (!trimmingActive)
                return;

            float offset = filteredCOMZ - filteredLCFZ;
            float direction = -Mathf.Sign(offset);

            Vector3 trimOffset = comAggregator.autoTrimOffsetLocal;
            trimOffset.z += direction * trimSpeed * Time.deltaTime;

            comAggregator.SetAutoTrimOffset(trimOffset);
        }
    }
}