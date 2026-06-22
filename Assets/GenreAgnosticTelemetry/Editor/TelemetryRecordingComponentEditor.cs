using UnityEditor;
using UnityEngine;

namespace GenreAgnosticTelemetry.Editor
{
    [CustomEditor(typeof(TelemetryRecordingComponent))]
    public sealed class TelemetryRecordingComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var component = (TelemetryRecordingComponent)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(
                new GUIContent("Binding Validation", "Checks that this recorder has the bindings and trigger settings it needs to build the event."),
                EditorStyles.boldLabel);
            TelemetryEditorValidationGui.DrawResults(component.ValidateBindings());

            if (component.eventDefinition != null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField(
                    new GUIContent("Definition Validation", "Checks the assigned event definition and its fields for schema problems."),
                    EditorStyles.boldLabel);
                TelemetryEditorValidationGui.DrawResults(
                    TelemetryDefinitionValidator.ValidateEventDefinition(component.eventDefinition));
            }

            using (new EditorGUI.DisabledScope(!UnityEngine.Application.isPlaying))
            {
                if (GUILayout.Button(new GUIContent(
                    "Record Now",
                    "Records this component's event immediately using its current field bindings. This is available in Play Mode.")))
                {
                    component.Record();
                }
            }
        }
    }
}
