using UnityEngine;

public class AxiomBuoyancyRaycastVisualizer : MonoBehaviour
{
    [Header("Vessel Reference")]
    [SerializeField] private AxiomBuoyancyVessel vessel;

    // ============================================================
    // Independent Diagnostic Settings
    // ============================================================

    [Header("Raycast Settings (Independent)")]
    public int beamCount = 4;
    public int lengthCount = 10;

    [Tooltip("Vertical ignore band for deck rays.")]
    public float deckIgnoreBand = 0.5f;

    [Tooltip("Side ray offset as fraction of hull width.")]
    public float sideOffsetFraction = 0.25f;

    [Tooltip("Side ray max distance multiplier.")]
    public float sideDistanceMultiplier = 3f;

    [Tooltip("Vertical spacing for side rows.")]
    public float sideRowSpacing = 0.75f;

    [Header("Deck Ray Mode")]
    public DeckRayMode deckRayMode = DeckRayMode.TopHitThenIgnoreBand;

    public enum DeckRayMode
    {
        TopHitOnly,
        TopHitThenIgnoreBand,
        AverageSampling,
        MedianSampling
    }

    // ============================================================
    // Toggles
    // ============================================================

    [Header("Ray Toggles")]
    public bool showKeelRays = true;
    public bool showDeckRays = true;
    public bool showSideRays = true;
    public bool showMisses = true;

    // ============================================================
    // Overlay Lines
    // ============================================================

    [Header("Overlay Lines")]
    public bool showDeckLine = true;
    public bool showKeelLine = true;

    public Color deckLineColor = Color.yellow;
    public Color keelLineColor = Color.blue;

    [Header("Gizmo Settings")]
    public float hitSphereRadius = 0.05f;
    public float rayLengthMultiplier = 2f;

    private void OnDrawGizmos()
    {
        if (vessel == null || vessel.HullRenderer == null)
            return;

        MeshCollider mc = vessel.GetComponentInChildren<MeshCollider>();
        if (mc == null || mc.sharedMesh == null)
            return;

        Bounds rendererBounds = vessel.HullRenderer.bounds;

        // Overlay lines first
        DrawOverlayLines(rendererBounds);

        // Rays
        if (showKeelRays) DrawKeelRays(mc, rendererBounds);
        if (showDeckRays) DrawDeckRays(mc, rendererBounds);
        if (showSideRays) DrawSideRays(mc, rendererBounds);
    }

    // ============================================================
    // OVERLAY LINES
    // ============================================================
    private void DrawOverlayLines(Bounds rendererBounds)
    {
        Vector3 min = rendererBounds.min;
        Vector3 max = rendererBounds.max;

        float keelY = min.y;
        float deckY = max.y;

        // Keel line
        if (showKeelLine)
        {
            Gizmos.color = keelLineColor;
            Gizmos.DrawLine(
                new Vector3(min.x, keelY, min.z),
                new Vector3(max.x, keelY, max.z)
            );
        }

        // Deck line
        if (showDeckLine)
        {
            Gizmos.color = deckLineColor;
            Gizmos.DrawLine(
                new Vector3(min.x, deckY, min.z),
                new Vector3(max.x, deckY, max.z)
            );
        }
    }

    // ============================================================
    // KEEL RAYS (UPWARD) — BLUE
    // ============================================================
    private void DrawKeelRays(MeshCollider mc, Bounds rendererBounds)
    {
        Bounds local = mc.sharedMesh.bounds;
        Vector3 min = mc.transform.TransformPoint(local.min);
        Vector3 max = mc.transform.TransformPoint(local.max);

        float beamSpacing = (max.x - min.x) / (beamCount + 1);
        float lengthSpacing = (max.z - min.z) / (lengthCount + 1);

        float startY = rendererBounds.min.y - 5f;
        float rayDist = (rendererBounds.size.y + 10f) * rayLengthMultiplier;

        int mask = 1 << mc.gameObject.layer;

        for (int bx = 1; bx <= beamCount; bx++)
        {
            float x = min.x + beamSpacing * bx;

            for (int lz = 1; lz <= lengthCount; lz++)
            {
                float z = min.z + lengthSpacing * lz;

                Vector3 origin = new Vector3(x, startY, z);
                Vector3 dir = Vector3.up;

                if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDist, mask))
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(origin, hit.point);
                    Gizmos.DrawSphere(hit.point, hitSphereRadius);
                }
                else if (showMisses)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(origin, origin + dir * rayDist);
                }
            }
        }
    }

    // ============================================================
    // DECK RAYS (DOWNWARD) — YELLOW
    // ============================================================
    private void DrawDeckRays(MeshCollider mc, Bounds rendererBounds)
    {
        Bounds local = mc.sharedMesh.bounds;
        Vector3 min = mc.transform.TransformPoint(local.min);
        Vector3 max = mc.transform.TransformPoint(local.max);

        float hullHeight = rendererBounds.size.y;
        float beamSpacing = (max.x - min.x) / (beamCount + 1);
        float lengthSpacing = (max.z - min.z) / (lengthCount + 1);

        float startY = rendererBounds.max.y + hullHeight * 0.5f;
        float rayDist = hullHeight * rayLengthMultiplier;

        int mask = 1 << mc.gameObject.layer;

        for (int bx = 1; bx <= beamCount; bx++)
        {
            float x = min.x + beamSpacing * bx;

            for (int lz = 1; lz <= lengthCount; lz++)
            {
                float z = min.z + lengthSpacing * lz;

                Vector3 origin = new Vector3(x, startY, z);
                Vector3 dir = Vector3.down;

                // First ray
                if (!Physics.Raycast(origin, dir, out RaycastHit topHit, rayDist, mask))
                {
                    if (showMisses)
                    {
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine(origin, origin + dir * rayDist);
                    }
                    continue;
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin, topHit.point);
                Gizmos.DrawSphere(topHit.point, hitSphereRadius);

                if (deckRayMode == DeckRayMode.TopHitOnly)
                    continue;

                // Second ray (ignore band)
                float secondStartY = topHit.point.y - deckIgnoreBand;
                if (secondStartY <= rendererBounds.min.y)
                    continue;

                Vector3 origin2 = new Vector3(x, secondStartY, z);

                if (Physics.Raycast(origin2, dir, out RaycastHit deckHit, rayDist, mask))
                {
                    Gizmos.DrawLine(origin2, deckHit.point);
                    Gizmos.DrawSphere(deckHit.point, hitSphereRadius);
                }
                else if (showMisses)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(origin2, origin2 + dir * rayDist);
                }
            }
        }
    }

    // ============================================================
    // SIDE RAYS (INWARD) — GREEN
    // ============================================================
    private void DrawSideRays(MeshCollider mc, Bounds rendererBounds)
    {
        Bounds local = mc.sharedMesh.bounds;
        Vector3 min = mc.transform.TransformPoint(local.min);
        Vector3 max = mc.transform.TransformPoint(local.max);

        float hullWidth = max.x - min.x;
        float lengthSpacing = (max.z - min.z) / (lengthCount + 1);

        float sideOffset = hullWidth * sideOffsetFraction;
        float rayDist = hullWidth * sideDistanceMultiplier;

        int mask = 1 << mc.gameObject.layer;

        float y = rendererBounds.min.y + sideRowSpacing;

        while (y < rendererBounds.max.y)
        {
            for (int lz = 1; lz <= lengthCount; lz++)
            {
                float z = min.z + lengthSpacing * lz;

                // PORT
                Vector3 portOrigin = new Vector3(min.x - sideOffset, y, z);
                Vector3 portDir = mc.transform.right;

                if (Physics.Raycast(portOrigin, portDir, out RaycastHit hitP, rayDist, mask))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(portOrigin, hitP.point);
                    Gizmos.DrawSphere(hitP.point, hitSphereRadius);
                }
                else if (showMisses)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(portOrigin, portOrigin + portDir * rayDist);
                }

                // STARBOARD
                Vector3 starOrigin = new Vector3(max.x + sideOffset, y, z);
                Vector3 starDir = -mc.transform.right;

                if (Physics.Raycast(starOrigin, starDir, out RaycastHit hitS, rayDist, mask))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(starOrigin, hitS.point);
                    Gizmos.DrawSphere(hitS.point, hitSphereRadius);
                }
                else if (showMisses)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(starOrigin, starOrigin + starDir * rayDist);
                }
            }

            y += sideRowSpacing;
        }
    }
}