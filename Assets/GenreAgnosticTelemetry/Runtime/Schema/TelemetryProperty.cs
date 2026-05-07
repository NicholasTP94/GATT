using System;

namespace GenreAgnosticTelemetry
{
    public enum TelemetryPropertyType
    {
        String,
        Number,
        Bool
    }

    [Serializable]
    public sealed class TelemetryProperty
    {
        public string key;
        public TelemetryPropertyType type;
        public string stringValue;
        public double numberValue;
        public bool boolValue;

        public TelemetryProperty()
        {
        }

        private TelemetryProperty(string key, TelemetryPropertyType type)
        {
            this.key = key;
            this.type = type;
        }

        public static TelemetryProperty String(string key, string value)
        {
            return new TelemetryProperty(key, TelemetryPropertyType.String)
            {
                stringValue = value
            };
        }

        public static TelemetryProperty Number(string key, double value)
        {
            return new TelemetryProperty(key, TelemetryPropertyType.Number)
            {
                numberValue = value
            };
        }

        public static TelemetryProperty Bool(string key, bool value)
        {
            return new TelemetryProperty(key, TelemetryPropertyType.Bool)
            {
                boolValue = value
            };
        }
    }
}
