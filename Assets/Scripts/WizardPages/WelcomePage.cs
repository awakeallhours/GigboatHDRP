using System;
using UnityEditor;
using UnityEngine;

public static class WelcomePage
{
    private static Texture2D logo;

    // Load assets once when Unity reloads scripts
    [InitializeOnLoadMethod]
    private static void LoadAssets()
    {
        logo = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/WizardAssets/AxiomLogoPlaceholder.png"
        );
    }

    public static void Draw(
        int currentStep,
        int totalSteps,
        Action onBack,
        Action onNext
    )
    {
        WizardSkeleton.Draw(
            title: "Welcome to the Axiom Vessel Setup Wizard",
            description:
                "This wizard is part of the Axiom simulation framework.\n" +
                "It will guide you through the initial setup of your vessel.\n\n" +
                "You'll configure geometry, probes, mass, draft, and other\n" +
                "essential systems to ensure accurate and stable simulation.",
            drawContent: DrawContent,
            previewTexture: logo,
            currentStep: currentStep,
            totalSteps: totalSteps,
            onBack: onBack,
            onNext: onNext
        );
    }

    private static void DrawContent()
    {
        GUILayout.Label("What this wizard will help you do:", EditorStyles.boldLabel);
        GUILayout.Label("• Analyse vessel orientation and scale");
        GUILayout.Label("• Choose a geometry source");
        GUILayout.Label("• Generate buoyancy probes");
        GUILayout.Label("• Configure mass and draft");
        GUILayout.Label("• Prepare the vessel for Axiom simulation");
    }
}