using System;
using System.Collections.Generic;

namespace GenreAgnosticTelemetry
{
    public sealed class TelemetrySessionStore
    {
        public TelemetrySession ActiveSession { get; private set; }

        public bool HasActiveSession
        {
            get { return ActiveSession != null; }
        }

        public TelemetrySession StartSession(
            string sessionId,
            string gameId,
            string sceneName,
            string unityVersion,
            string toolVersion,
            DateTime startedAtUtc)
        {
            ActiveSession = new TelemetrySession(sessionId, gameId)
            {
                sceneName = sceneName,
                unityVersion = unityVersion,
                toolVersion = toolVersion
            };

            ActiveSession.MarkStarted(startedAtUtc);
            return ActiveSession;
        }

        public TelemetrySession EndSession(DateTime endedAtUtc)
        {
            if (ActiveSession == null)
            {
                return null;
            }

            ActiveSession.MarkEnded(endedAtUtc);
            var completedSession = ActiveSession;
            ActiveSession = null;
            return completedSession;
        }

        public void AddMetadata(TelemetryProperty property)
        {
            if (ActiveSession == null || property == null)
            {
                return;
            }

            ActiveSession.metadata.Add(property);
        }

        public void AddMetadata(IEnumerable<TelemetryProperty> properties)
        {
            if (properties == null)
            {
                return;
            }

            foreach (var property in properties)
            {
                AddMetadata(property);
            }
        }

        public void AddEvent(TelemetryEventBase telemetryEvent)
        {
            if (ActiveSession == null || telemetryEvent == null)
            {
                return;
            }

            ActiveSession.events.Add(telemetryEvent);
        }

        public void AddState(TelemetryStateSample stateSample)
        {
            if (ActiveSession == null || stateSample == null)
            {
                return;
            }

            ActiveSession.states.Add(stateSample);
        }

        public void AddPerformanceSample(TelemetryPerformanceSample performanceSample)
        {
            if (ActiveSession == null || performanceSample == null)
            {
                return;
            }

            ActiveSession.performanceSamples.Add(performanceSample);
        }
    }
}
