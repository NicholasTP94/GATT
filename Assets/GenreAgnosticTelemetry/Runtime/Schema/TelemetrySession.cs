using System;
using System.Collections.Generic;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public sealed class TelemetrySession
    {
        public string sessionId;
        public string gameId;
        public string sceneName;
        public string unityVersion;
        public string toolVersion;
        public string startedAtUtc;
        public string endedAtUtc;
        public List<TelemetryProperty> metadata = new List<TelemetryProperty>();
        public List<TelemetryEventBase> events = new List<TelemetryEventBase>();
        public List<TelemetryStateSample> states = new List<TelemetryStateSample>();
        public List<TelemetryPerformanceSample> performanceSamples = new List<TelemetryPerformanceSample>();

        public TelemetrySession()
        {
        }

        public TelemetrySession(string sessionId, string gameId)
        {
            this.sessionId = sessionId;
            this.gameId = gameId;
        }

        public void MarkStarted(DateTime utcNow)
        {
            startedAtUtc = FormatUtc(utcNow);
        }

        public void MarkEnded(DateTime utcNow)
        {
            endedAtUtc = FormatUtc(utcNow);
        }

        public static string FormatUtc(DateTime value)
        {
            return value.ToUniversalTime().ToString("o");
        }
    }
}
