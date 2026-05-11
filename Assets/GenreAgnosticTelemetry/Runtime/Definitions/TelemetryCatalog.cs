using System.Collections.Generic;
using UnityEngine;

namespace GenreAgnosticTelemetry
{
    [CreateAssetMenu(
        fileName = "TelemetryCatalog",
        menuName = "Genre Agnostic Telemetry/Catalog")]
    public sealed class TelemetryCatalog : ScriptableObject
    {
        [Tooltip("The version of this catalog as a whole. Increase it when you publish a meaningful catalog change.")]
        public int catalogVersion = 1;

        [Tooltip("The event definitions collected in this catalog for review and validation.")]
        public List<TelemetryEventDefinition> events = new List<TelemetryEventDefinition>();

        [Tooltip("The category names your team has agreed to use when organizing events in this catalog.")]
        public List<string> allowedCategories = new List<string>();

        [Tooltip("The tag names your team has agreed to use when labeling events in this catalog.")]
        public List<string> allowedTags = new List<string>();

        [Tooltip("The catalog's fallback privacy level for tooling that uses a default classification.")]
        public TelemetryDefinitionPrivacy defaultPrivacyClassification = TelemetryDefinitionPrivacy.Anonymous;

        [Tooltip("The rules used when this catalog checks its event definitions for common mistakes.")]
        public TelemetryValidationProfile validationProfile = new TelemetryValidationProfile();
    }
}
