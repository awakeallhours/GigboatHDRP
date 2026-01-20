using UnityEditor;
using UnityEngine;

public class VesselSetupWizard : EditorWindow
{
    private int currentStep = 0;
    private const int totalSteps = 5; // adjust later

    [MenuItem("Axiom/Vessel Setup Wizard")]
    public static void Open()
    {
        GetWindow<VesselSetupWizard>("Axiom Vessel Setup");
    }

    private void OnGUI()
    {
        switch (currentStep)
        {
            case 0:
                WelcomePage.Draw(
                    currentStep,
                    totalSteps,
                    () => currentStep--,
                    () => currentStep++
                );
                break;

                // other pages go here later
        }
    }
}
