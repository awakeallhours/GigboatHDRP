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
    // Probe Generation Settings (cleaned)
    // ---------------------------------------------------------------------

    [Header("Probe Generation Settings")]
    [Tooltip("Number of probe columns across the beam (X axis).")]
    [SerializeField] private int beamCount = 4;

    [Tooltip("Number of probe rows along the vessel length (Z axis).")]
    [SerializeField] private int lengthCount = 10;

    [Header("Probe Visual Radius")]
    [SerializeField] private float probeRadius = 0.25f;

    [Header("Side Probe Layers")]
    [SerializeField] private bool overrideSideLayers = false;

    [SerializeField] private int manualSideLayers = 3;

    public bool OverrideSideLayers => overrideSideLayers;
    public int ManualSideLayers => manualSideLayers;
    public float ProbeRadius => probeRadius;

    public int BeamCount => beamCount;
    public int LengthCount => lengthCount;


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

    /// <summary>
    /// Ensures the probe hierarchy exists. Creates it if missing.
    /// </summary>
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

    /// <summary>
    /// Deletes all existing probe GameObjects and clears the probe list.
    /// </summary>
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

    /// <summary>
    /// Assigns a new set of probe GameObjects to this vessel.
    /// Called by the editor menu after generating and instantiating probes.
    /// </summary>
    public void SetProbeObjects(List<Transform> newProbeObjects)
    {
        probeObjects.Clear();

        if (newProbeObjects != null && newProbeObjects.Count > 0)
            probeObjects.AddRange(newProbeObjects);
    }
}