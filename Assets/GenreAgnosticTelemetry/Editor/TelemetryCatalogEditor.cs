using UnityEditor;
using UnityEngine;

namespace GenreAgnosticTelemetry.Editor
{
    [CustomEditor(typeof(TelemetryCatalog))]
    public sealed class TelemetryCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(
                new GUIContent("Validation", "Checks every event in this catalog and points out schema problems or duplicate event IDs."),
                EditorStyles.boldLabel);
            TelemetryEditorValidationGui.DrawResults(
                TelemetryDefinitionValidator.ValidateCatalog((TelemetryCatalog)target));
        }
    }
}
