using UnityEditor;
using UnityEngine;
using System;

public static class WizardSkeleton
{
    public static void Draw(
        string title,
        string description,
        Action drawContent,
        Texture2D previewTexture,
        int currentStep,
        int totalSteps,
        Action onBack,
        Action onNext
    )
    {
        DrawHeader(title, description);
        DrawBody(drawContent, previewTexture);
        DrawFooter(currentStep, totalSteps, onBack, onNext);
    }

    // ------------------------------------------------------------
    // HEADER
    // ------------------------------------------------------------
    private static void DrawHeader(string title, string description)
    {
        GUILayout.Label(title, EditorStyles.boldLabel);
        GUILayout.Label(description, EditorStyles.wordWrappedLabel);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    // ------------------------------------------------------------
    // BODY (Left content + Right preview)
    // ------------------------------------------------------------
    private static void DrawBody(Action drawContent, Texture2D previewTexture)
    {
        GUILayout.BeginHorizontal();

        DrawLeftContent(drawContent);
        DrawRightPreview(previewTexture);

        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }

    private static void DrawLeftContent(Action drawContent)
    {
        GUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.55f));
        drawContent?.Invoke();
        GUILayout.EndVertical();
    }

    private static void DrawRightPreview(Texture2D previewTexture)
    {
        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

        GUILayout.Label("Preview", EditorStyles.boldLabel);

        Rect previewRect = GUILayoutUtility.GetAspectRect(1f);
        GUI.Box(previewRect, GUIContent.none);

        if (previewTexture != null)
        {
            GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.Label(previewRect, "(no preview)", EditorStyles.centeredGreyMiniLabel);
        }

        GUILayout.EndVertical();
    }

    // ------------------------------------------------------------
    // FOOTER (Back / Step indicator / Next)
    // ------------------------------------------------------------
    private static void DrawFooter(
        int currentStep,
        int totalSteps,
        Action onBack,
        Action onNext
    )
    {
        GUILayout.BeginHorizontal();

        // Back button
        GUI.enabled = currentStep > 0;
        if (GUILayout.Button("< Back", GUILayout.Width(100)))
            onBack?.Invoke();
        GUI.enabled = true;

        // Step indicator
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Step {currentStep + 1} of {totalSteps}");
        GUILayout.FlexibleSpace();

        // Next button
        if (GUILayout.Button("Next >", GUILayout.Width(100)))
            onNext?.Invoke();

        GUILayout.EndHorizontal();
    }
}