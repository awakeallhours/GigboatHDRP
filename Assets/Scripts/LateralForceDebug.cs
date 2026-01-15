using UnityEngine;

namespace Axiom.Vessel.Diagnostics
{

    public class LateralForceDebug : MonoBehaviour
    {
        public Rigidbody rb;

        public BoatCOMIntegration comIntegration;


        [Header("Test Point Height (Y Offset from Boat Root)")]
        public float testPointHeight = 0f;

        [Header("Force Settings")]
        public float testForce = 5000f;

        [Header("Click in Play Mode")]
        public bool applyForce = false;

        private Transform testPoint;

        private void Awake()
        {
            // Ensure RB exists
            if (rb == null)
                rb = GetComponent<Rigidbody>();

            // Always create the test point
            testPoint = new GameObject("LateralForceTestPoint").transform;
            testPoint.SetParent(transform, false);
        }

        private void Update()
        {
            if (testPoint == null)
                return;

            // Update test point position
            testPoint.localPosition = new Vector3(0f, testPointHeight, 0f);

            if (applyForce)
            {
                applyForce = false;

                if (rb == null)
                {
                    Debug.LogError("[LateralForceDebug] Rigidbody is missing.");
                    return;
                }

                Vector3 force = transform.right * testForce;
                rb.AddForceAtPosition(force, testPoint.position, ForceMode.Force);

                // Compute torque
                Vector3 r = testPoint.position - rb.worldCenterOfMass;
                Vector3 torque = Vector3.Cross(r, force);
                float rollTorque = torque.x;

                if (comIntegration != null)
                {
                    float forceHeight = testPoint.position.y - transform.position.y;
                    comIntegration.SetLateralForceHeight(forceHeight);
                }

                Debug.Log(
                    $"[LateralForceDebug]\n" +
                    $"Force: {force}\n" +
                    $"Height: {testPointHeight}\n" +
                    $"Torque: {torque}\n" +
                    $"Roll Torque: {rollTorque}\n" +
                    $"{(rollTorque > 0 ? "RIGHT SIDE DOWN" : "LEFT SIDE DOWN")}"
                );
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || testPoint == null)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                return;

            Vector3 screenPos = cam.WorldToScreenPoint(testPoint.position);

            if (screenPos.z < 0)
                return;

            float size = 12f;
            float x = screenPos.x - size * 0.5f;
            float y = Screen.height - screenPos.y - size * 0.5f;

            GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture);
        }

        private void Reset()
        {
            if (comIntegration == null)
                comIntegration = GetComponent<BoatCOMIntegration>();
        }

    }
}