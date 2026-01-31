using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a buoyant vessel in the scene. Holds a reference to the hull mesh
/// used for probe generation and exposes validated hull bounds, probe settings,
/// and the current probe object hierarchy.
///
/// This component does not perform buoyancy itself; it simply provides the
/// geometric and configuration data required by the probe generator and editor tools.
/// </summary>
public class AxiomBuoyancyVessel : MonoBehaviour
{
    // ---------------------------------------------------------------------
    // Hull Reference
    // ---------------------------------------------------------------------

    [Header("Hull Reference")]
    [Tooltip(
        "The MeshRenderer that defines the hull volume for probe generation. " +
        "Assign the main hull mesh here. Required for probe placement."
    )]
    [SerializeField] private MeshRenderer hullRenderer;
    public MeshRenderer HullRenderer => hullRenderer;


    // ---------------------------------------------------------------------
    // Probe Generation Settings (density)
    // ---------------------------------------------------------------------

    [Header("Probe Generation Settings")]
    [Tooltip("Number of probe columns across the beam (X axis).")]
    [SerializeField] private int beamCount = 4;

    [Tooltip("Number of probe rows along the vessel length (Z axis).")]
    [SerializeField] private int lengthCount = 10;

    public int BeamCount => beamCount;
    public int LengthCount => lengthCount;


    // ---------------------------------------------------------------------
    // Probe Classification & Distribution Parameters
    // ---------------------------------------------------------------------

    [Header("Probe Classification Parameters")]

    [Tooltip("Fraction of hull height below which a probe is considered a keel probe. 0 = bottom, 1 = top.")]
    [SerializeField] private float keelCutoffHeightFraction = 0.6f;

    [Tooltip("Fraction of hull height from the top that counts as deck. Example: 0.1 = top 10% of hull.")]
    [SerializeField] private float deckRegionHeightFraction = 0.1f;

    [Tooltip("Absolute deck thickness in metres. Used if smaller than the fraction-based value.")]
    [SerializeField] private float deckRegionAbsolute = 0.03f;

    [Tooltip("Fraction of hull height below which side probes are NOT allowed. Side probes must be above this Y-level.")]
    [SerializeField] private float sideLowerCutoffFraction = 0.6f;

    public float KeelCutoffHeightFraction => keelCutoffHeightFraction;
    public float DeckRegionHeightFraction => deckRegionHeightFraction;
    public float DeckRegionAbsolute => deckRegionAbsolute;
    public float SideLowerCutoffFraction => sideLowerCutoffFraction;


    // ---------------------------------------------------------------------
    // Side Probe Raycasting
    // ---------------------------------------------------------------------

    [Header("Side Probe Raycasting")]

    [Tooltip("Fraction of hull width used to offset side probe raycasts outward.")]
    [SerializeField] private float sideOffsetWidthFraction = 0.25f;

    [Tooltip("Multiplier applied to hull width to determine max raycast distance for side probes.")]
    [SerializeField] private float sideRaycastDistanceMultiplier = 3f;

    public float SideOffsetWidthFraction => sideOffsetWidthFraction;
    public float SideRaycastDistanceMultiplier => sideRaycastDistanceMultiplier;


    // ---------------------------------------------------------------------
    // Vertical Probe Layering
    // ---------------------------------------------------------------------

    [Header("Vertical Probe Layering")]

    [Tooltip("Vertical spacing between probe layers in metres when auto-calculated.")]
    [SerializeField] private float verticalLayerResolution = 0.75f;

    public float VerticalLayerResolution => verticalLayerResolution;


    // ---------------------------------------------------------------------
    // Probe Visual Radius
    // ---------------------------------------------------------------------

    [Header("Probe Visual Radius")]
    [SerializeField] private float probeRadius = 0.25f;
    public float ProbeRadius => probeRadius;


    // ---------------------------------------------------------------------
    // Side Probe Layers
    // ---------------------------------------------------------------------

    [Header("Side Probe Layers")]
    [SerializeField] private bool overrideSideLayers = false;
    [SerializeField] private int manualSideLayers = 3;

    public bool OverrideSideLayers => overrideSideLayers;
    public int ManualSideLayers => manualSideLayers;


    // ---------------------------------------------------------------------
    // Probe Object Hierarchy
    // ---------------------------------------------------------------------

    [Header("Probe Object Hierarchy (auto‑generated)")]
    [SerializeField] private Transform probeRoot;
    [SerializeField] private Transform keelProbeRoot;
    [SerializeField] private Transform sideProbeRoot;
    [SerializeField] private Transform deckProbeRoot;

    public Transform ProbeRoot => probeRoot;
    public Transform KeelProbeRoot => keelProbeRoot;
    public Transform SideProbeRoot => sideProbeRoot;
    public Transform DeckProbeRoot => deckProbeRoot;

    public IReadOnlyList<Transform> ProbeObjects => probeObjects;

    [SerializeField]
    private List<Transform> probeObjects = new List<Transform>();


    // ---------------------------------------------------------------------
    // Hull Bounds
    // ---------------------------------------------------------------------

    public Bounds GetHullBounds()
    {
        if (hullRenderer == null)
        {
            Debug.LogError(
                $"Axiom Vessel '{name}': No hullRenderer assigned. " +
                "Probe generation cannot proceed."
            );

            return new Bounds(transform.position, Vector3.zero);
        }

        return hullRenderer.bounds;
    }

    public bool HasValidHull => hullRenderer != null;


    // ---------------------------------------------------------------------
    // Probe Hierarchy Management
    // ---------------------------------------------------------------------

    public void EnsureProbeHierarchy()
    {
        if (probeRoot == null)
        {
            probeRoot = new GameObject("BuoyancyProbes").transform;
            probeRoot.SetParent(transform, false);
            probeRoot.localScale = Vector3.one;
        }

        if (keelProbeRoot == null)
        {
            keelProbeRoot = new GameObject("KeelProbes").transform;
            keelProbeRoot.SetParent(probeRoot, false);
            keelProbeRoot.localScale = Vector3.one;
        }

        if (sideProbeRoot == null)
        {
            sideProbeRoot = new GameObject("SideProbes").transform;
            sideProbeRoot.SetParent(probeRoot, false);
            sideProbeRoot.localScale = Vector3.one;
        }

        if (deckProbeRoot == null)
        {
            deckProbeRoot = new GameObject("DeckProbes").transform;
            deckProbeRoot.SetParent(probeRoot, false);
            deckProbeRoot.localScale = Vector3.one;
        }
    }

    public void ClearProbes()
    {
        probeObjects.Clear();

        if (probeRoot != null)
            DestroyImmediate(probeRoot.gameObject);

        probeRoot = null;
        keelProbeRoot = null;
        sideProbeRoot = null;
        deckProbeRoot = null;
    }

    public void SetProbeObjects(List<Transform> newProbeObjects)
    {
        probeObjects.Clear();

        if (newProbeObjects != null && newProbeObjects.Count > 0)
            probeObjects.AddRange(newProbeObjects);
    }
}