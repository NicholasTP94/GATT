using System;
using System.Collections.Generic;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public sealed class TelemetryStateSample
    {
        public double timestampSeconds;
        public string name;
        public string objectId;
        public List<TelemetryProperty> properties = new List<TelemetryProperty>();

        public TelemetryStateSample()
        {
        }

        public TelemetryStateSample(double timestampSeconds, string name)
        {
            this.timestampSeconds = timestampSeconds;
            this.name = name;
        }
    }
}
