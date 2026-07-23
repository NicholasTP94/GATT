using UnityEngine;

namespace GenreAgnosticTelemetry
{
    public sealed class TelemetryClock
    {
        private float startRealtimeSeconds;

        public void Reset()
        {
            startRealtimeSeconds = Time.realtimeSinceStartup;
        }

        public double TimestampSeconds
        {
            get
            {
                return Time.realtimeSinceStartup - startRealtimeSeconds;
            }
        }
    }
}
