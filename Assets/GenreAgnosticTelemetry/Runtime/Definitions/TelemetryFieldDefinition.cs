using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public sealed class TelemetryFieldDefinition
    {
        [Tooltip("The key written into the event payload, such as button_id. Use snake_case and keep it unique within this event.")]
        public string key;

        [Tooltip("A friendly label for this field in editor tools and documentation.")]
        public string displayName;

        [Tooltip("Explains what this value represents so someone reading the schema knows how to use it.")]
        public string description;

        [Tooltip("The kind of value this field accepts and how the validator checks it.")]
        public TelemetryDefinitionFieldType fieldType;

        [Tooltip("When enabled, validation warns if a recorded event does not include this field.")]
        public bool required = true;

        [Tooltip("The sensitivity of the information stored in this field.")]
        public TelemetryDefinitionPrivacy privacyClassification = TelemetryDefinitionPrivacy.Anonymous;

        [Tooltip("Shows whether this field is ready to use, still being designed, or has been replaced.")]
        public TelemetryDefinitionStatus status = TelemetryDefinitionStatus.Active;

        [Tooltip("The field key developers should use instead when this field is deprecated.")]
        public string replacementKey;

        [Tooltip("The starting value for String and Enum fields when no binding supplies another value. Leave it empty to omit the default.")]
        public string defaultStringValue;

        [Tooltip("The starting value for Number and Integer fields when no binding supplies another value.")]
        public double defaultNumberValue;

        [Tooltip("The starting value for Bool fields when no binding supplies another value.")]
        public bool defaultBoolValue;

        [Tooltip("Turn this on to reject numeric values below Minimum during validation.")]
        public bool hasMinimum;

        [Tooltip("The smallest value accepted when Has Minimum is enabled.")]
        public double minimum;

        [Tooltip("Turn this on to reject numeric values above Maximum during validation.")]
        public bool hasMaximum;

        [Tooltip("The largest value accepted when Has Maximum is enabled.")]
        public double maximum;

        [Tooltip("The exact text values accepted by an Enum field.")]
        public List<string> enumOptions = new List<string>();
    }
}
