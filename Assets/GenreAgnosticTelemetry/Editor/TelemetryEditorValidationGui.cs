using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GenreAgnosticTelemetry.Editor
{
    internal static class TelemetryEditorValidationGui
    {
        public static void DrawResults(IReadOnlyList<TelemetryValidationResult> results)
        {
            if (results == null || results.Count == 0)
            {
                EditorGUILayout.HelpBox("Validation passed.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox($"{results.Count} validation warning(s).", MessageType.Warning);

            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                EditorGUILayout.LabelField(
                    new GUIContent(result.code, "A short code you can use to identify this kind of validation warning."),
                    EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(result.ToString(), MessageType.Warning);
            }
        }
    }
}
