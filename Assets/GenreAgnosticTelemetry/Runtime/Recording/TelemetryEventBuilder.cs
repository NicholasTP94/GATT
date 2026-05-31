using System.Collections.Generic;

namespace GenreAgnosticTelemetry
{
    public sealed class TelemetryEventBuilder
    {
        private readonly TelemetryEventDefinition definition;
        private readonly Dictionary<string, TelemetryProperty> properties =
            new Dictionary<string, TelemetryProperty>();

        public TelemetryEventBuilder(TelemetryEventDefinition definition)
        {
            this.definition = definition;
        }

        public TelemetryEventBuilder SetString(string key, string value)
        {
            properties[key] = TelemetryProperty.String(key, value);
            return this;
        }

        public TelemetryEventBuilder SetNumber(string key, double value)
        {
            properties[key] = TelemetryProperty.Number(key, value);
            return this;
        }

        public TelemetryEventBuilder SetBool(string key, bool value)
        {
            properties[key] = TelemetryProperty.Bool(key, value);
            return this;
        }

        public TelemetryEvent Build()
        {
            if (definition == null)
            {
                return null;
            }

            var telemetryEvent = new TelemetryEvent
            {
                eventType = definition.eventType,
                category = definition.category,
                name = definition.eventName
            };

            ApplyDefaultFields(telemetryEvent);

            foreach (var property in properties.Values)
            {
                telemetryEvent.properties.RemoveAll(existing => existing.key == property.key);
                telemetryEvent.properties.Add(property);
            }

            return telemetryEvent;
        }

        public bool TrySubmit(TelemetryManager manager)
        {
            if (manager == null)
            {
                return false;
            }

            var telemetryEvent = Build();
            return telemetryEvent != null && manager.TryRecordEvent(telemetryEvent);
        }

        private void ApplyDefaultFields(TelemetryEvent telemetryEvent)
        {
            for (var i = 0; i < definition.fields.Count; i++)
            {
                var field = definition.fields[i];

                if (field == null || string.IsNullOrWhiteSpace(field.key) || properties.ContainsKey(field.key))
                {
                    continue;
                }

                if (field.fieldType == TelemetryDefinitionFieldType.String
                    || field.fieldType == TelemetryDefinitionFieldType.Enum)
                {
                    if (!string.IsNullOrEmpty(field.defaultStringValue))
                    {
                        telemetryEvent.properties.Add(TelemetryProperty.String(field.key, field.defaultStringValue));
                    }
                }
                else if (field.fieldType == TelemetryDefinitionFieldType.Bool)
                {
                    telemetryEvent.properties.Add(TelemetryProperty.Bool(field.key, field.defaultBoolValue));
                }
                else
                {
                    telemetryEvent.properties.Add(TelemetryProperty.Number(field.key, field.defaultNumberValue));
                }
            }
        }
    }
}
