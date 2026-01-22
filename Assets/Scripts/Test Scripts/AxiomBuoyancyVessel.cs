using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a buoyant vessel in the scene. Holds a reference to the hull mesh
/// used for probe generation and exposes validated hull bounds, probe settings,
/// and the current probe object hierarchy.
///
/// This component does not perform buoyancy itself; it simply provides the
/// geometric and configuration data required by
/// <see cref="AxiomAutoBuoyancyProbeGenerator"/> and editor tools such as
/// AxiomBuoyancyProbeMenu.
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

    // ---------------------------------------------------------------------
    // Probe Generation Settings
    // ---------------------------------------------------------------------

    [Header("Probe Generation Settings")]
    [Tooltip("Settings used when generating probes for this vessel.")]
    [SerializeField] private AxiomAutoBuoyancyProbeGenerator.BoundingBoxSettings probeSettings;

    /// <summary>
    /// The settings used for probe generation on this vessel.
    /// </summary>
    public AxiomAutoBuoyancyProbeGenerator.BoundingBoxSettings ProbeSettings => probeSettings;

    // ---------------------------------------------------------------------
    // Probe Object Hierarchy
    // ---------------------------------------------------------------------

    [Header("Probe Object Hierarchy (auto‑generated)")]
    [SerializeField] private Transform probeRoot;
    [SerializeField] private Transform keelProbeRoot;
    [SerializeField] private Transform sideProbeRoot;

    /// <summary>
    /// Root transform under which all probe objects are placed.
    /// </summary>
    public Transform ProbeRoot => probeRoot;

    /// <summary>
    /// Parent transform for keel‑layer probes.
    /// </summary>
    public Transform KeelProbeRoot => keelProbeRoot;

    /// <summary>
    /// Parent transform for side‑layer probes.
    /// </summary>
    public Transform SideProbeRoot => sideProbeRoot;

    /// <summary>
    /// Read‑only list of all probe object transforms.
    /// </summary>
    public IReadOnlyList<Transform> ProbeObjects => probeObjects;
    [SerializeField]private List<Transform> probeObjects = new List<Transform>();

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
        }

        if (keelProbeRoot == null)
        {
            keelProbeRoot = new GameObject("KeelProbes").transform;
            keelProbeRoot.SetParent(probeRoot, false);
        }

        if (sideProbeRoot == null)
        {
            sideProbeRoot = new GameObject("SideProbes").transform;
            sideProbeRoot.SetParent(probeRoot, false);
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