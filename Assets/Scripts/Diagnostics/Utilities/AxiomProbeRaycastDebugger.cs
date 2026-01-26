using UnityEngine;

[ExecuteAlways]
public class AxiomProbeRaycastDebugger : MonoBehaviour
{
    public AxiomBuoyancyVessel vessel;

    [Header("Debug Settings")]
    public bool drawSideRays = true;
    public float gizmoSphereRadius = 0.25f;

    private void OnDrawGizmos()
    {
        if (!drawSideRays || vessel == null || !vessel.HasValidHull)
            return;

        var hullRenderer = vessel.HullRenderer;
        var meshFilter = hullRenderer.GetComponent<MeshFilter>();
        var mc = hullRenderer.GetComponent<MeshCollider>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        if (mc == null)
            return;

        Bounds local = mc.sharedMesh.bounds;
        Vector3 worldMin = mc.transform.TransformPoint(local.min);
        Vector3 worldMax = mc.transform.TransformPoint(local.max);

        float hullWidth = worldMax.x - worldMin.x;
        float hullHeight = worldMax.y - worldMin.y;

        float sideOffset = hullWidth * 0.25f;
        float maxRaycastDistance = hullWidth * 3f;

        float desiredVerticalResolution = 0.75f;
        int verticalLayers = Mathf.Clamp(
            Mathf.RoundToInt(hullHeight / desiredVerticalResolution),
            1, 10
        );

        float bottomFrac = 0.15f;
        float topFrac = 0.85f;

        float deckCutoffY = worldMax.y - Mathf.Min(0.5f, hullHeight * 0.1f);

        int lengthCount = vessel.LengthCount;
        float lengthSpacing = (worldMax.z - worldMin.z) / (lengthCount + 1);

        int mask = 1 << mc.gameObject.layer;

        for (int v = 0; v < verticalLayers; v++)
        {
            float t = (verticalLayers == 1) ? 0.5f : (float)v / (verticalLayers - 1);
            float frac = Mathf.Lerp(bottomFrac, topFrac, t);
            float y = Mathf.Lerp(worldMin.y, worldMax.y, frac);

            for (int lz = 1; lz <= lengthCount; lz++)
            {
                float z = worldMin.z + lengthSpacing * lz;

                // PORT
                Vector3 portOrigin = new Vector3(worldMin.x - sideOffset, y, z);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(portOrigin, portOrigin + Vector3.right * maxRaycastDistance);

                if (Physics.Raycast(portOrigin, Vector3.right, out RaycastHit hitP, maxRaycastDistance, mask))
                {
                    Gizmos.color = hitP.point.y >= deckCutoffY ? Color.yellow : Color.green;
                    Gizmos.DrawSphere(hitP.point, gizmoSphereRadius);
                }
                else
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(portOrigin + Vector3.right * 1f, gizmoSphereRadius);
                }

                // STARBOARD
                Vector3 starOrigin = new Vector3(worldMax.x + sideOffset, y, z);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(starOrigin, starOrigin + Vector3.left * maxRaycastDistance);

                if (Physics.Raycast(starOrigin, Vector3.left, out RaycastHit hitS, maxRaycastDistance, mask))
                {
                    Gizmos.color = hitS.point.y >= deckCutoffY ? Color.yellow : Color.green;
                    Gizmos.DrawSphere(hitS.point, gizmoSphereRadius);
                }
                else
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(starOrigin + Vector3.left * 1f, gizmoSphereRadius);
                }
            }
        }
    }
}