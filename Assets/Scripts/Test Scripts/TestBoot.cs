using UnityEngine;
using UnityEditor;

public class VesselSetupWizard : MonoBehaviour
{
    public enum VesselType
    {
        Generic,
        SmallBoat,
        Sailboat,
        Tug,
        Cargo,
        Fishing
    }

    public VesselType vesselType = VesselType.Generic;

    [ContextMenu("Run Vessel Setup Wizard")]
    public void RunWizard()
    {
        bool proceed = EditorUtility.DisplayDialog(
            "Vessel Setup",
            "Ensure the vessel is correctly oriented:\n" +
            "• Bow facing forward (+Z)\n" +
            "• Keel below waterline\n" +
            "• Scale set to real vessel size\n\n" +
            "Proceed with default vessel setup?",
            "Yes, continue",
            "Cancel"
        );

        if (!proceed)
        {
            Debug.Log("Vessel setup cancelled.");
            return;
        }

        Transform hull = this.transform;

        // 1. Create root
        GameObject rootGO = new GameObject("VesselRoot");
        Transform root = rootGO.transform;

        // 2. Copy world transform
        root.position = hull.position;
        root.rotation = hull.rotation;

        // 3. Parent hull under root
        hull.SetParent(root, true);

        // 4. Reset local rotation
        hull.localRotation = Quaternion.identity;

        // 5. Add placeholder components
        if (rootGO.GetComponent<VesselRootPlaceholder>() == null)
            rootGO.AddComponent<VesselRootPlaceholder>();

        // 6. Add COM object
        GameObject com = new GameObject("COM");
        com.transform.SetParent(root);
        com.transform.localPosition = Vector3.zero;

        // 7. Add buoyancy placeholder
        GameObject buoyancy = new GameObject("BuoyancySystem");
        buoyancy.transform.SetParent(root);
        buoyancy.transform.localPosition = Vector3.zero;

        Debug.Log($"Vessel setup complete. Vessel type: {vesselType}");
    }
}

// Placeholder components so the hierarchy shows intent
public class VesselRootPlaceholder : MonoBehaviour
{
}