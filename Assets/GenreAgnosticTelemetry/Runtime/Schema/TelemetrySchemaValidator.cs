using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GenreAgnosticTelemetry
{
    public static class TelemetrySchemaValidator
    {
        private static readonly Regex SnakeCasePattern = new Regex(
            "^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$",
            RegexOptions.Compiled);

        public static List<string> ValidateSession(TelemetrySession session)
        {
            return ToMessages(ValidateSessionDetailed(session));
        }

        public static List<TelemetryValidationResult> ValidateSessionDetailed(TelemetrySession session)
        {
            var results = new List<TelemetryValidationResult>();

            if (session == null)
            {
                AddResult(results, "session_null", string.Empty, "Session is null.");
                return results;
            }

            AddRequiredWarning(results, session.sessionId, "sessionId", "Session id is required.");
            AddRequiredWarning(results, session.gameId, "gameId", "Game id is required.");

            ValidateProperties(session.metadata, "metadata", results);

            ValidateEvents(session.events, results);
            ValidateStateSamples(session.states, results);
            ValidatePerformanceSamples(session.performanceSamples, results);

            return results;
        }

        public static List<string> ValidateEvent(TelemetryEventBase telemetryEvent)
        {
            return ToMessages(ValidateEventDetailed(telemetryEvent));
        }

        public static List<TelemetryValidationResult> ValidateEventDetailed(TelemetryEventBase telemetryEvent)
        {
            var results = new List<TelemetryValidationResult>();
            ValidateEvent(telemetryEvent, 0, results);
            return results;
        }

        public static List<string> ValidateStateSample(TelemetryStateSample stateSample)
        {
            return ToMessages(ValidateStateSampleDetailed(stateSample));
        }

        public static List<TelemetryValidationResult> ValidateStateSampleDetailed(TelemetryStateSample stateSample)
        {
            var results = new List<TelemetryValidationResult>();
            ValidateStateSample(stateSample, 0, results);
            return results;
        }

        private static void ValidateEvents(IReadOnlyList<TelemetryEventBase> events, List<TelemetryValidationResult> results)
        {
            if (events == null)
            {
                return;
            }

            for (var i = 0; i < events.Count; i++)
            {
                ValidateEvent(events[i], i, results);
            }
        }

        private static void ValidateEvent(
            TelemetryEventBase telemetryEvent,
            int index,
            List<TelemetryValidationResult> results)
        {
            var context = $"event[{index}]";

            if (telemetryEvent == null)
            {
                AddResult(results, "event_null", context, $"{context} is null.");
                return;
            }

            AddRequiredWarning(results, telemetryEvent.eventType, $"{context}.eventType", $"{context}.eventType is required.");
            AddRequiredWarning(results, telemetryEvent.eventId, $"{context}.eventId", $"{context}.eventId is required.");
            ValidateTimestamp(telemetryEvent.timestampSeconds, $"{context}.timestampSeconds", results);
            ValidateFrameIndex(telemetryEvent.frameIndex, $"{context}.frameIndex", results);
            AddRequiredWarning(results, telemetryEvent.category, $"{context}.category", $"{context}.category is required.");
            AddRequiredWarning(results, telemetryEvent.name, $"{context}.name", $"{context}.name is required.");
            ValidateSnakeCase(telemetryEvent.eventType, $"{context}.eventType", results);
            ValidateSnakeCase(telemetryEvent.category, $"{context}.category", results);
            ValidateSnakeCase(telemetryEvent.name, $"{context}.name", results);

            if (telemetryEvent.hasPosition && telemetryEvent.position == null)
            {
                AddResult(
                    results,
                    "position_required",
                    $"{context}.position",
                    $"{context}.position is required when hasPosition is true.");
            }

            ValidateProperties(telemetryEvent.properties, $"{context}.properties", results);
            telemetryEvent.Validate(results, context);
        }

        private static void ValidateStateSamples(IReadOnlyList<TelemetryStateSample> states, List<TelemetryValidationResult> results)
        {
            if (states == null)
            {
                return;
            }

            for (var i = 0; i < states.Count; i++)
            {
                ValidateStateSample(states[i], i, results);
            }
        }

        private static void ValidateStateSample(
            TelemetryStateSample stateSample,
            int index,
            List<TelemetryValidationResult> results)
        {
            var context = $"state[{index}]";

            if (stateSample == null)
            {
                AddResult(results, "state_null", context, $"{context} is null.");
                return;
            }

            ValidateTimestamp(stateSample.timestampSeconds, $"{context}.timestampSeconds", results);
            AddRequiredWarning(results, stateSample.name, $"{context}.name", $"{context}.name is required.");
            ValidateSnakeCase(stateSample.name, $"{context}.name", results);
            ValidateProperties(stateSample.properties, $"{context}.properties", results);
        }

        private static void ValidatePerformanceSamples(
            IReadOnlyList<TelemetryPerformanceSample> performanceSamples,
            List<TelemetryValidationResult> results)
        {
            if (performanceSamples == null)
            {
                return;
            }

            for (var i = 0; i < performanceSamples.Count; i++)
            {
                var sample = performanceSamples[i];
                var context = $"performanceSamples[{i}]";

                if (sample == null)
                {
                    AddResult(results, "performance_sample_null", context, $"{context} is null.");
                    continue;
                }

                ValidateTimestamp(sample.timestampSeconds, $"{context}.timestampSeconds", results);
            }
        }

        private static void ValidateProperties(
            IReadOnlyList<TelemetryProperty> properties,
            string context,
            List<TelemetryValidationResult> results)
        {
            if (properties == null)
            {
                return;
            }

            var keys = new HashSet<string>();

            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                var propertyContext = $"{context}[{i}]";

                if (property == null)
                {
                    AddResult(results, "property_null", propertyContext, $"{propertyContext} is null.");
                    continue;
                }

                AddRequiredWarning(results, property.key, $"{propertyContext}.key", $"{propertyContext}.key is required.");
                ValidateSnakeCase(property.key, $"{propertyContext}.key", results);

                if (!string.IsNullOrWhiteSpace(property.key) && !keys.Add(property.key))
                {
                    AddResult(
                        results,
                        "duplicate_property_key",
                        $"{propertyContext}.key",
                        $"{propertyContext}.key duplicates another property key in {context}.");
                }

                if (!Enum.IsDefined(typeof(TelemetryPropertyType), property.type))
                {
                    AddResult(
                        results,
                        "unsupported_property_type",
                        $"{propertyContext}.type",
                        $"{propertyContext}.type is unsupported.");
                }
            }
        }

        private static void ValidateTimestamp(
            double timestampSeconds,
            string fieldName,
            List<TelemetryValidationResult> results)
        {
            if (timestampSeconds < 0)
            {
                AddResult(results, "negative_timestamp", fieldName, $"{fieldName} must not be negative.");
            }

            if (double.IsNaN(timestampSeconds) || double.IsInfinity(timestampSeconds))
            {
                AddResult(results, "invalid_timestamp", fieldName, $"{fieldName} must be a finite number.");
            }
        }

        private static void ValidateFrameIndex(
            int frameIndex,
            string fieldName,
            List<TelemetryValidationResult> results)
        {
            if (frameIndex < 0)
            {
                AddResult(results, "negative_frame_index", fieldName, $"{fieldName} must not be negative.");
            }
        }

        private static void AddRequiredWarning(
            List<TelemetryValidationResult> results,
            string value,
            string path,
            string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddResult(results, "required_field", path, message);
            }
        }

        private static void ValidateSnakeCase(
            string value,
            string fieldName,
            List<TelemetryValidationResult> results)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!SnakeCasePattern.IsMatch(value))
            {
                AddResult(results, "snake_case", fieldName, $"{fieldName} should use snake_case.");
            }
        }

        private static void AddResult(
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

        private static List<string> ToMessages(IReadOnlyList<TelemetryValidationResult> results)
        {
            var messages = new List<string>();

            if (results == null)
            {
                return messages;
            }

            for (var i = 0; i < results.Count; i++)
            {
                messages.Add(results[i].ToString());
            }

            return messages;
        }
    }
}
