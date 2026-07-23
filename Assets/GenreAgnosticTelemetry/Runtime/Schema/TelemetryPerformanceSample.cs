using System;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public sealed class TelemetryPerformanceSample
    {
        public double timestampSeconds;
        public double fps;
        public double frameTimeMs;
        public long totalAllocatedMemoryBytes;
        public long totalReservedMemoryBytes;
        public long monoUsedMemoryBytes;

        public TelemetryPerformanceSample()
        {
        }

        public TelemetryPerformanceSample(double timestampSeconds, double fps, double frameTimeMs)
        {
            this.timestampSeconds = timestampSeconds;
            this.fps = fps;
            this.frameTimeMs = frameTimeMs;
        }
    }
}
