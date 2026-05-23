namespace GenreAgnosticTelemetry
{
    public sealed class TelemetryJsonSerializationOptions
    {
        public bool prettyPrint;
        public bool includeNullFields;
        public bool wrapSession = true;
        public string schemaVersion = TelemetryJsonSerializer.DefaultSchemaVersion;
    }
}
