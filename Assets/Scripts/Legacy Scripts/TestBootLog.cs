using UnityEngine;

public class TestBootstrap_Debug : MonoBehaviour
{
    [Header("Run once")]
    public bool run;

    void Update()
    {
        if (!Application.isPlaying && run)
        {
            run = false;
            RunBootstrap();
        }
    }

    void RunBootstrap()
    {
        Transform hull = this.transform;

        Debug.Log("=== BEFORE BOOTSTRAP ===");
        Debug.Log($"Hull WORLD rotation: {hull.rotation.eulerAngles}");
        Debug.Log($"Hull LOCAL rotation: {hull.localRotation.eulerAngles}");

        // 1. Create the root
        GameObject rootGO = new GameObject("BoatRoot_Test");
        Transform root = rootGO.transform;

        // 2. Copy the hull's world rotation & position into the root
        root.position = hull.position;
        root.rotation = hull.rotation;

        Debug.Log("\nCreated root and copied hull rotation into it.");
        Debug.Log($"Root WORLD rotation: {root.rotation.eulerAngles}");
        Debug.Log($"Root LOCAL rotation: {root.localRotation.eulerAngles}");

        // 3. Parent the hull under the root (keep world space)
        hull.SetParent(root, worldPositionStays: true);

        Debug.Log("\nAfter parenting (worldPositionStays = true):");
        Debug.Log($"Hull WORLD rotation: {hull.rotation.eulerAngles}");
        Debug.Log($"Hull LOCAL rotation: {hull.localRotation.eulerAngles}");

        // 4. Reset hull's local rotation (bakes the dev's rotation into the root)
        hull.localRotation = Quaternion.identity;

        Debug.Log("\n=== AFTER RESETTING LOCAL ROTATION ===");
        Debug.Log($"Hull WORLD rotation: {hull.rotation.eulerAngles}");
        Debug.Log($"Hull LOCAL rotation: {hull.localRotation.eulerAngles}");
        Debug.Log($"Root WORLD rotation: {root.rotation.eulerAngles}");
        Debug.Log($"Root LOCAL rotation: {root.localRotation.eulerAngles}");

        Debug.Log("\nTestBootstrap_Debug: Completed.");
    }
}