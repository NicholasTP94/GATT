using System.Collections.Generic;
using UnityEngine;

namespace GenreAgnosticTelemetry
{
    [CreateAssetMenu(
        fileName = "TelemetryEventDefinition",
        menuName = "Genre Agnostic Telemetry/Event Definition")]
    public sealed class TelemetryEventDefinition : ScriptableObject
    {
        [Tooltip("The stable ID written to each event, such as ui_button_clicked. Use snake_case and avoid renaming it after release.")]
        public string eventType;

        [Tooltip("The broad area this event belongs to, such as ui, combat, or economy.")]
        public string category;

        [Tooltip("The specific action being recorded, such as button_clicked or item_purchased.")]
        public string eventName;

        [Tooltip("A friendly name people can recognize in the Inspector, catalog, and reports.")]
        public string displayName;

        [Tooltip("A plain-language explanation of what the event means and when it should be recorded.")]
        [TextArea(3, 8)]
        public string description;

        [Tooltip("The version of this event's schema. Increase it when you make a breaking change to its fields or meaning.")]
        public int version = 1;

        [Tooltip("Shows whether this definition is still being designed, ready to use, or kept only for older integrations.")]
        public TelemetryDefinitionStatus status = TelemetryDefinitionStatus.Draft;

        [Tooltip("The person or team responsible for keeping this event accurate and up to date.")]
        public string owner;

        [Tooltip("Short labels that make this event easier to group and find in the catalog.")]
        public List<string> tags = new List<string>();

        [Tooltip("The most sensitive kind of information this event is expected to contain.")]
        public TelemetryDefinitionPrivacy privacyClassification = TelemetryDefinitionPrivacy.Anonymous;

        [Tooltip("Controls whether this event is recorded. Choose Disabled to keep the definition without collecting it.")]
        public TelemetrySamplingMode samplingMode = TelemetrySamplingMode.Always;

        [Tooltip("The custom values that may be attached to this event, along with their types and validation rules.")]
        public List<TelemetryFieldDefinition> fields = new List<TelemetryFieldDefinition>();
    }
}
