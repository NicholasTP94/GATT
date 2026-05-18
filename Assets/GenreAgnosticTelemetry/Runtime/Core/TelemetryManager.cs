using System;
using UnityEngine;

namespace GenreAgnosticTelemetry.Runtime.Core
{
    public sealed class TelemetryManager : MonoBehaviour
    {
        private bool recording;
        private DateTime startedAt;

        public bool IsRecording => recording;

        public void StartRecording()
        {
            if (recording) return;
            startedAt = DateTime.UtcNow;
            recording = true;
            Debug.Log("Telemetry recording started");
        }

        public void StopRecording()
        {
            if (!recording) return;
            recording = false;
            Debug.Log($"Telemetry recording stopped after {DateTime.UtcNow - startedAt}");
        }
    }
}
