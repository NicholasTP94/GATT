using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GenreAgnosticTelemetry.Editor
{
    public sealed class TelemetryCatalogWindow : EditorWindow
    {
        private static readonly GUIContent RefreshButtonContent = new GUIContent(
            "Refresh",
            "Scans the project again for Telemetry Event Definition assets.");

        private static readonly GUIContent SearchFieldTooltip = new GUIContent(
            string.Empty,
            "Filters events by event type, category, name, or owner.");

        private static readonly GUIContent SelectButtonContent = new GUIContent(
            "Select",
            "Selects this event definition in the Project window and shows it in the Inspector.");

        private static readonly GUIContent CategoryLabel = new GUIContent(
            "Category",
            "The broad area this event belongs to.");

        private static readonly GUIContent NameLabel = new GUIContent(
            "Name",
            "The specific action this event records.");

        private static readonly GUIContent StatusLabel = new GUIContent(
            "Status",
            "Whether this event is a draft, active, or deprecated.");

        private static readonly GUIContent OwnerLabel = new GUIContent(
            "Owner",
            "The person or team responsible for this event.");

        private static readonly GUIContent FieldsLabel = new GUIContent(
            "Fields",
            "The number of custom values defined for this event.");

        private readonly List<TelemetryEventDefinition> definitions = new List<TelemetryEventDefinition>();
        private Vector2 scrollPosition;
        private string search = string.Empty;

        [MenuItem("Tools/Genre Agnostic Telemetry/Catalog")]
        public static void Open()
        {
            GetWindow<TelemetryCatalogWindow>("Telemetry Catalog");
        }

        private void OnEnable()
        {
            RefreshDefinitions();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(RefreshButtonContent, EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshDefinitions();
            }

            var searchRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                EditorStyles.toolbarSearchField,
                GUILayout.ExpandWidth(true));
            search = EditorGUI.TextField(searchRect, search, EditorStyles.toolbarSearchField);
            GUI.Label(searchRect, SearchFieldTooltip, GUIStyle.none);
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];

                if (definition == null || !MatchesSearch(definition))
                {
                    continue;
                }

                DrawDefinition(definition);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDefinition(TelemetryEventDefinition definition)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                new GUIContent(definition.eventType, "The stable ID written into recorded events."),
                EditorStyles.boldLabel);

            if (GUILayout.Button(SelectButtonContent, GUILayout.Width(70)))
            {
                Selection.activeObject = definition;
                EditorGUIUtility.PingObject(definition);
            }

            EditorGUILayout.EndHorizontal();
            DrawDetail(CategoryLabel, definition.category);
            DrawDetail(NameLabel, definition.eventName);
            DrawDetail(StatusLabel, definition.status.ToString());
            DrawDetail(OwnerLabel, definition.owner);
            DrawDetail(FieldsLabel, definition.fields.Count.ToString());

            var results = TelemetryDefinitionValidator.ValidateEventDefinition(definition);

            if (results.Count > 0)
            {
                EditorGUILayout.HelpBox($"{results.Count} validation warning(s).", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawDetail(GUIContent label, string value)
        {
            EditorGUILayout.LabelField(label, new GUIContent(value, label.tooltip));
        }

        private bool MatchesSearch(TelemetryEventDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            var value = search.ToLowerInvariant();
            return Contains(definition.eventType, value)
                || Contains(definition.category, value)
                || Contains(definition.eventName, value)
                || Contains(definition.owner, value);
        }

        private static bool Contains(string source, string value)
        {
            return !string.IsNullOrWhiteSpace(source)
                && source.ToLowerInvariant().Contains(value);
        }

        private void RefreshDefinitions()
        {
            definitions.Clear();
            var guids = AssetDatabase.FindAssets("t:TelemetryEventDefinition");

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var definition = AssetDatabase.LoadAssetAtPath<TelemetryEventDefinition>(path);

                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }

            definitions.Sort((left, right) => string.CompareOrdinal(left.eventType, right.eventType));
        }
    }
}
