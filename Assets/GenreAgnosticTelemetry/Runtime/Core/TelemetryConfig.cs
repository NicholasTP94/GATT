using System;
using UnityEngine;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public sealed class TelemetryConfig
    {
        [Tooltip("Master switch for telemetry. When this is off, session and recording calls are ignored.")]
        public bool telemetryEnabled = true;

        [Tooltip("Start a telemetry session automatically when the Telemetry Manager wakes up.")]
        public bool autoStartSession = true;

        [Tooltip("Format saved JSON with line breaks and indentation so it is easier to read.")]
        public bool prettyJson = true;

        [Tooltip("Save every completed or uploaded session as JSON inside the Unity project's Assets folder.")]
        public bool saveJsonLocally = true;

        [Tooltip("The stable ID written into every session to identify this game or application.")]
        public string gameId = "unity_game";

        [Tooltip("The version of this telemetry package or integration written into each session.")]
        public string toolVersion = "0.1.0";

        [Tooltip("The folder under the Unity project's Assets folder where session JSON files are saved.")]
        public string outputFolder = "Telemetry";
    }
}
