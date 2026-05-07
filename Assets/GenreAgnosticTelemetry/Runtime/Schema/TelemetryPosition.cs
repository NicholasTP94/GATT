using System;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public sealed class TelemetryPosition
    {
        public float x;
        public float y;
        public float z;

        public TelemetryPosition()
        {
        }

        public TelemetryPosition(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
