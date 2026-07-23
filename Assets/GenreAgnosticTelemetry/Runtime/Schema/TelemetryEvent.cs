using System;
using System.Collections.Generic;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public abstract class TelemetryEventBase
    {
        public string eventType;
        public string eventId;
        public double timestampSeconds;
        public int frameIndex;
        public string category;
        public string name;
        public string objectId;
        public string objectType;
        public bool hasPosition;
        public TelemetryPosition position;
        public bool hasSuccess;
        public bool success;
        public List<TelemetryProperty> properties = new List<TelemetryProperty>();

        protected TelemetryEventBase()
        {
        }

        protected TelemetryEventBase(double timestampSeconds, string category, string name)
        {
            this.timestampSeconds = timestampSeconds;
            this.category = category;
            this.name = name;
        }

        public void SetPosition(float x, float y, float z)
        {
            hasPosition = true;
            position = new TelemetryPosition(x, y, z);
        }

        public void SetSuccess(bool value)
        {
            hasSuccess = true;
            success = value;
        }

        public virtual void Validate(List<TelemetryValidationResult> results, string path)
        {
        }
    }

    [Serializable]
    public sealed class TelemetryEvent : TelemetryEventBase
    {
        public TelemetryEvent()
        {
            eventType = "generic_event";
        }

        public TelemetryEvent(double timestampSeconds, string category, string name)
            : base(timestampSeconds, category, name)
        {
            eventType = "generic_event";
        }
    }
}
