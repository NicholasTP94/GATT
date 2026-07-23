using System;

namespace GenreAgnosticTelemetry
{
    public enum TelemetryValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    [Serializable]
    public sealed class TelemetryValidationResult
    {
        public TelemetryValidationSeverity severity;
        public string code;
        public string path;
        public string message;

        public TelemetryValidationResult()
        {
        }

        public TelemetryValidationResult(
            TelemetryValidationSeverity severity,
            string code,
            string path,
            string message)
        {
            this.severity = severity;
            this.code = code;
            this.path = path;
            this.message = message;
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return message;
            }

            return $"{path}: {message}";
        }
    }
}
