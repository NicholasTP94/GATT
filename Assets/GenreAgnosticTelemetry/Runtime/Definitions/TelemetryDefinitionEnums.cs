namespace GenreAgnosticTelemetry
{
    public enum TelemetryDefinitionStatus
    {
        Draft,
        Active,
        Deprecated
    }

    public enum TelemetryDefinitionPrivacy
    {
        Anonymous,
        Pseudonymous,
        Personal,
        Sensitive
    }

    public enum TelemetryDefinitionFieldType
    {
        String,
        Number,
        Bool,
        Integer,
        Enum
    }

    public enum TelemetrySamplingMode
    {
        Always,
        Disabled
    }
}
