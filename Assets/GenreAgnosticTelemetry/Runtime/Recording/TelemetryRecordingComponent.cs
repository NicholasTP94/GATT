using System.Collections.Generic;
using UnityEngine;

namespace GenreAgnosticTelemetry.Runtime.Recording
{
    public sealed class TelemetryRecordingComponent : MonoBehaviour
    {
        [SerializeField] private float sampleInterval = 1f;
        private readonly List<Vector3> positions = new();
        private float nextSampleTime;

        private void OnEnable()
        {
            positions.Clear();
            nextSampleTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextSampleTime) return;
            positions.Add(transform.position);
            nextSampleTime = Time.unscaledTime + sampleInterval;
        }

        public IReadOnlyList<Vector3> GetPositions()
        {
            return positions;
        }
    }
}
