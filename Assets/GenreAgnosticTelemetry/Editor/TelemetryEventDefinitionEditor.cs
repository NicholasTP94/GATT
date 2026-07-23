using UnityEditor;
using UnityEngine;

namespace GenreAgnosticTelemetry.Editor
{
    [CustomEditor(typeof(TelemetryEventDefinition))]
    public sealed class TelemetryEventDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("eventType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("category"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("eventName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("version"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("status"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("owner"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("privacyClassification"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("samplingMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fields"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(
                new GUIContent("Validation", "Checks this event and its fields for missing details, naming mistakes, and invalid rules."),
                EditorStyles.boldLabel);
            TelemetryEditorValidationGui.DrawResults(
                TelemetryDefinitionValidator.ValidateEventDefinition((TelemetryEventDefinition)target));
        }
    }
}
