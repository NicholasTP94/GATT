using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GenreAgnosticTelemetry
{
    public static class TelemetryDefinitionValidator
    {
        private static readonly Regex SnakeCasePattern = new Regex(
            "^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$",
            RegexOptions.Compiled);

        public static List<TelemetryValidationResult> ValidateCatalog(TelemetryCatalog catalog)
        {
            var results = new List<TelemetryValidationResult>();

            if (catalog == null)
            {
                Add(results, "catalog_null", string.Empty, "Telemetry catalog is null.");
                return results;
            }

            var eventTypes = new HashSet<string>();
            var profile = catalog.validationProfile ?? new TelemetryValidationProfile();

            for (var i = 0; i < catalog.events.Count; i++)
            {
                var definition = catalog.events[i];
                var path = $"events[{i}]";

                if (definition == null)
                {
                    Add(results, "catalog_event_null", path, $"{path} is null.");
                    continue;
                }

                ValidateEventDefinition(definition, profile, results, path);

                if (!string.IsNullOrWhiteSpace(definition.eventType) && !eventTypes.Add(definition.eventType))
                {
                    Add(
                        results,
                        "duplicate_event_type",
                        $"{path}.eventType",
                        $"{path}.eventType duplicates another event type in the catalog.");
                }
            }

            return results;
        }

        public static List<TelemetryValidationResult> ValidateEventDefinition(
            TelemetryEventDefinition definition)
        {
            var results = new List<TelemetryValidationResult>();
            ValidateEventDefinition(definition, new TelemetryValidationProfile(), results, "eventDefinition");
            return results;
        }

        public static List<TelemetryValidationResult> ValidateRuntimeEvent(
            TelemetryEventDefinition definition,
            TelemetryEvent telemetryEvent,
            TelemetryValidationProfile profile)
        {
            var results = new List<TelemetryValidationResult>();
            profile = profile ?? new TelemetryValidationProfile();

            if (definition == null)
            {
                Add(results, "definition_null", "definition", "Telemetry event definition is required.");
                return results;
            }

            if (telemetryEvent == null)
            {
                Add(results, "event_null", "event", "Telemetry event is null.");
                return results;
            }

            var propertiesByKey = new Dictionary<string, TelemetryProperty>();

            for (var i = 0; i < telemetryEvent.properties.Count; i++)
            {
                var property = telemetryEvent.properties[i];

                if (property == null || string.IsNullOrWhiteSpace(property.key))
                {
                    continue;
                }

                propertiesByKey[property.key] = property;
            }

            for (var i = 0; i < definition.fields.Count; i++)
            {
                var field = definition.fields[i];

                if (field == null || string.IsNullOrWhiteSpace(field.key))
                {
                    continue;
                }

                if (!propertiesByKey.TryGetValue(field.key, out var property))
                {
                    if (field.required)
                    {
                        Add(
                            results,
                            "missing_required_field",
                            $"event.properties.{field.key}",
                            $"Required telemetry field '{field.key}' is missing.");
                    }

                    continue;
                }

                ValidatePropertyAgainstField(property, field, results, $"event.properties.{field.key}");
            }

            if (!profile.allowUnknownRuntimeFields)
            {
                var knownFields = new HashSet<string>();

                for (var i = 0; i < definition.fields.Count; i++)
                {
                    if (definition.fields[i] != null && !string.IsNullOrWhiteSpace(definition.fields[i].key))
                    {
                        knownFields.Add(definition.fields[i].key);
                    }
                }

                foreach (var property in propertiesByKey.Values)
                {
                    if (!knownFields.Contains(property.key))
                    {
                        Add(
                            results,
                            "unknown_field",
                            $"event.properties.{property.key}",
                            $"Telemetry field '{property.key}' is not defined by event '{definition.eventType}'.");
                    }
                }
            }

            return results;
        }

        private static void ValidateEventDefinition(
            TelemetryEventDefinition definition,
            TelemetryValidationProfile profile,
            List<TelemetryValidationResult> results,
            string path)
        {
            if (definition == null)
            {
                Add(results, "definition_null", path, $"{path} is null.");
                return;
            }

            Require(results, definition.eventType, $"{path}.eventType", "Event type is required.");
            Require(results, definition.category, $"{path}.category", "Category is required.");
            Require(results, definition.eventName, $"{path}.eventName", "Event name is required.");
            ValidateSnakeCase(results, definition.eventType, $"{path}.eventType");
            ValidateSnakeCase(results, definition.category, $"{path}.category");
            ValidateSnakeCase(results, definition.eventName, $"{path}.eventName");

            if (profile.requireDescriptions)
            {
                Require(results, definition.description, $"{path}.description", "Description is required.");
            }

            if (profile.requireOwner)
            {
                Require(results, definition.owner, $"{path}.owner", "Owner is required.");
            }

            if (definition.version <= 0)
            {
                Add(results, "invalid_version", $"{path}.version", "Version must be greater than zero.");
            }

            if (definition.fields.Count > profile.maxFieldsPerEvent)
            {
                Add(
                    results,
                    "too_many_fields",
                    $"{path}.fields",
                    $"Event has {definition.fields.Count} fields; maximum is {profile.maxFieldsPerEvent}.");
            }

            var fieldKeys = new HashSet<string>();

            for (var i = 0; i < definition.fields.Count; i++)
            {
                var field = definition.fields[i];
                var fieldPath = $"{path}.fields[{i}]";

                if (field == null)
                {
                    Add(results, "field_null", fieldPath, $"{fieldPath} is null.");
                    continue;
                }

                ValidateFieldDefinition(field, profile, results, fieldPath);

                if (!string.IsNullOrWhiteSpace(field.key) && !fieldKeys.Add(field.key))
                {
                    Add(
                        results,
                        "duplicate_field_key",
                        $"{fieldPath}.key",
                        $"{fieldPath}.key duplicates another field key in this event.");
                }
            }
        }

        private static void ValidateFieldDefinition(
            TelemetryFieldDefinition field,
            TelemetryValidationProfile profile,
            List<TelemetryValidationResult> results,
            string path)
        {
            Require(results, field.key, $"{path}.key", "Field key is required.");
            ValidateSnakeCase(results, field.key, $"{path}.key");

            if (profile.requireDescriptions)
            {
                Require(results, field.description, $"{path}.description", "Field description is required.");
            }

            if (field.hasMinimum && field.hasMaximum && field.minimum > field.maximum)
            {
                Add(results, "invalid_range", path, "Field minimum must not be greater than maximum.");
            }

            if (field.fieldType == TelemetryDefinitionFieldType.Enum)
            {
                if (field.enumOptions.Count == 0)
                {
                    Add(results, "enum_options_required", $"{path}.enumOptions", "Enum fields require options.");
                }

                if (field.enumOptions.Count > profile.maxEnumOptions)
                {
                    Add(
                        results,
                        "too_many_enum_options",
                        $"{path}.enumOptions",
                        $"Enum has {field.enumOptions.Count} options; maximum is {profile.maxEnumOptions}.");
                }
            }

            if (field.status == TelemetryDefinitionStatus.Deprecated
                && string.IsNullOrWhiteSpace(field.replacementKey))
            {
                Add(
                    results,
                    "deprecated_field_missing_replacement",
                    $"{path}.replacementKey",
                    "Deprecated fields should specify a replacement key.");
            }
        }

        private static void ValidatePropertyAgainstField(
            TelemetryProperty property,
            TelemetryFieldDefinition field,
            List<TelemetryValidationResult> results,
            string path)
        {
            if (field.fieldType == TelemetryDefinitionFieldType.String
                || field.fieldType == TelemetryDefinitionFieldType.Enum)
            {
                if (property.type != TelemetryPropertyType.String)
                {
                    Add(results, "field_type_mismatch", path, $"Field '{field.key}' must be a string.");
                    return;
                }

                if (field.fieldType == TelemetryDefinitionFieldType.Enum
                    && !field.enumOptions.Contains(property.stringValue))
                {
                    Add(results, "enum_value_not_allowed", path, $"Value '{property.stringValue}' is not allowed for '{field.key}'.");
                }

                return;
            }

            if (field.fieldType == TelemetryDefinitionFieldType.Bool)
            {
                if (property.type != TelemetryPropertyType.Bool)
                {
                    Add(results, "field_type_mismatch", path, $"Field '{field.key}' must be a bool.");
                }

                return;
            }

            if (property.type != TelemetryPropertyType.Number)
            {
                Add(results, "field_type_mismatch", path, $"Field '{field.key}' must be numeric.");
                return;
            }

            if (field.fieldType == TelemetryDefinitionFieldType.Integer
                && property.numberValue % 1 != 0)
            {
                Add(results, "integer_required", path, $"Field '{field.key}' must be an integer.");
            }

            if (field.hasMinimum && property.numberValue < field.minimum)
            {
                Add(results, "number_below_minimum", path, $"Field '{field.key}' is below minimum {field.minimum}.");
            }

            if (field.hasMaximum && property.numberValue > field.maximum)
            {
                Add(results, "number_above_maximum", path, $"Field '{field.key}' is above maximum {field.maximum}.");
            }
        }

        private static void Require(
            List<TelemetryValidationResult> results,
            string value,
            string path,
            string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Add(results, "required_field", path, message);
            }
        }

        private static void ValidateSnakeCase(
            List<TelemetryValidationResult> results,
            string value,
            string path)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!SnakeCasePattern.IsMatch(value))
            {
                Add(results, "snake_case", path, $"{path} should use snake_case.");
            }
        }

        private static void Add(
            List<TelemetryValidationResult> results,
            string code,
            string path,
            string message)
        {
            results.Add(new TelemetryValidationResult(
                TelemetryValidationSeverity.Warning,
                code,
                path,
                message));
        }
    }
}
