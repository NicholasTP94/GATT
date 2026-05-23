using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GenreAgnosticTelemetry
{
    [DisallowMultipleComponent]
    public sealed class TelemetryManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The main settings that control session startup, recording, and local JSON output.")]
        private TelemetryConfig config = new TelemetryConfig();

        private readonly TelemetryClock clock = new TelemetryClock();
        private readonly TelemetrySessionStore sessionStore = new TelemetrySessionStore();
        private bool hasLoggedDisabledWarning;
        private bool hasLoggedMissingSessionWarning;

        public TelemetryConfig Config
        {
            get { return config; }
        }

        public TelemetrySessionStore SessionStore
        {
            get { return sessionStore; }
        }

        public TelemetrySession ActiveSession
        {
            get { return sessionStore.ActiveSession; }
        }

        public TelemetrySession LastCompletedSession { get; private set; }

        public string LastSavedSessionPath { get; private set; }

        public event Action<TelemetrySession> SessionEnded;

        public double TimestampSeconds
        {
            get { return clock.TimestampSeconds; }
        }

        public bool IsRecording
        {
            get { return config != null && config.telemetryEnabled && sessionStore.HasActiveSession; }
        }

        private void Awake()
        {
            if (config == null)
            {
                config = new TelemetryConfig();
            }

            if (config.autoStartSession)
            {
                StartSession();
            }
        }

        private void OnDestroy()
        {
            EndSession();
        }

        private void OnApplicationQuit()
        {
            EndSession();
        }

        public TelemetrySession StartSession()
        {
            if (!IsTelemetryEnabled())
            {
                return null;
            }

            if (sessionStore.HasActiveSession)
            {
                return sessionStore.ActiveSession;
            }

            clock.Reset();

            return sessionStore.StartSession(
                Guid.NewGuid().ToString("N"),
                config.gameId,
                SceneManager.GetActiveScene().name,
                Application.unityVersion,
                config.toolVersion,
                DateTime.UtcNow);
        }

        public TelemetrySession EndSession()
        {
            var completedSession = sessionStore.EndSession(DateTime.UtcNow);

            if (completedSession != null)
            {
                LastCompletedSession = completedSession;
                TrySaveSessionToJson(completedSession);
                SessionEnded?.Invoke(completedSession);
            }

            return completedSession;
        }

        public bool TryRecordEvent(TelemetryEventBase telemetryEvent)
        {
            if (!CanRecord())
            {
                return false;
            }

            PrepareEvent(telemetryEvent);
            sessionStore.AddEvent(telemetryEvent);
            return true;
        }

        public bool TryRecordEvent(string category, string name)
        {
            return TryRecordEvent(new TelemetryEvent(0, category, name));
        }

        public TelemetryEventBuilder CreateEvent(TelemetryEventDefinition definition)
        {
            return new TelemetryEventBuilder(definition);
        }

        public bool TryRecordEvent(TelemetryEventDefinition definition, IEnumerable<TelemetryProperty> properties)
        {
            if (definition == null)
            {
                Debug.LogWarning("Telemetry event definition is required.");
                return false;
            }

            var telemetryEvent = new TelemetryEvent
            {
                eventType = definition.eventType,
                category = definition.category,
                name = definition.eventName
            };

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    if (property != null)
                    {
                        telemetryEvent.properties.Add(property);
                    }
                }
            }

            var validationResults = TelemetryDefinitionValidator.ValidateRuntimeEvent(
                definition,
                telemetryEvent,
                null);

            if (validationResults.Count > 0)
            {
                Debug.LogWarning($"Telemetry event '{definition.eventType}' has {validationResults.Count} validation warning(s).");
            }

            return TryRecordEvent(telemetryEvent);
        }

        public bool TryRecordState(TelemetryStateSample stateSample)
        {
            if (!CanRecord())
            {
                return false;
            }

            sessionStore.AddState(stateSample);
            return true;
        }

        public bool TryRecordPerformanceSample(TelemetryPerformanceSample performanceSample)
        {
            if (!CanRecord())
            {
                return false;
            }

            sessionStore.AddPerformanceSample(performanceSample);
            return true;
        }

        public bool TryAddMetadata(TelemetryProperty property)
        {
            if (!CanRecord())
            {
                return false;
            }

            sessionStore.AddMetadata(property);
            return true;
        }

        public string SerializeActiveSessionToJson()
        {
            return SerializeSessionToJson(ActiveSession);
        }

        public string SerializeSessionToJson(TelemetrySession session)
        {
            return TelemetryJsonSerializer.SerializeSession(
                session,
                config != null && config.prettyJson);
        }

        public bool TrySaveSessionToJson(TelemetrySession session)
        {
            if (session == null || config == null || !config.saveJsonLocally)
            {
                return false;
            }

            return TrySaveSessionToJson(session, SerializeSessionToJson(session));
        }

        public bool TrySaveSessionToJson(TelemetrySession session, string json)
        {
            if (session == null
                || config == null
                || !config.saveJsonLocally
                || string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                var outputDirectory = GetLocalOutputDirectory();
                Directory.CreateDirectory(outputDirectory);

                var sessionId = SanitizeFileName(string.IsNullOrWhiteSpace(session.sessionId)
                    ? "session"
                    : session.sessionId);
                var outputPath = Path.Combine(
                    outputDirectory,
                    $"telemetry_session_{sessionId}.json");

                File.WriteAllText(outputPath, json, new UTF8Encoding(false));
                LastSavedSessionPath = outputPath;
                Debug.Log($"Telemetry session '{session.sessionId}' saved locally to {outputPath}.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not save telemetry session JSON locally: {exception.Message}");
                return false;
            }
        }

        private string GetLocalOutputDirectory()
        {
            var assetsDirectory = Path.GetFullPath(Application.dataPath);
            var folder = string.IsNullOrWhiteSpace(config.outputFolder)
                ? "Telemetry"
                : config.outputFolder.Trim();
            var outputDirectory = Path.GetFullPath(Path.Combine(assetsDirectory, folder));
            var assetsPrefix = assetsDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!outputDirectory.StartsWith(assetsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Telemetry output folder must stay inside the Unity project's Assets folder.");
            }

            return outputDirectory;
        }

        private static string SanitizeFileName(string value)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);

            for (var i = 0; i < value.Length; i++)
            {
                builder.Append(Array.IndexOf(invalidCharacters, value[i]) >= 0 ? '_' : value[i]);
            }

            return builder.ToString();
        }

        private bool CanRecord()
        {
            if (!IsTelemetryEnabled())
            {
                return false;
            }

            if (!sessionStore.HasActiveSession)
            {
                if (!hasLoggedMissingSessionWarning)
                {
                    Debug.LogWarning("Telemetry record ignored because no telemetry session is active.");
                    hasLoggedMissingSessionWarning = true;
                }

                return false;
            }

            return true;
        }

        private void PrepareEvent(TelemetryEventBase telemetryEvent)
        {
            if (telemetryEvent == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(telemetryEvent.eventId))
            {
                telemetryEvent.eventId = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(telemetryEvent.eventType))
            {
                telemetryEvent.eventType = ToSnakeCase(telemetryEvent.GetType().Name);
            }

            if (telemetryEvent.timestampSeconds <= 0)
            {
                telemetryEvent.timestampSeconds = clock.TimestampSeconds;
            }

            if (telemetryEvent.frameIndex <= 0)
            {
                telemetryEvent.frameIndex = Time.frameCount;
            }
        }

        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var chars = new System.Text.StringBuilder(value.Length + 8);

            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];

                if (char.IsUpper(current))
                {
                    if (i > 0)
                    {
                        chars.Append('_');
                    }

                    chars.Append(char.ToLowerInvariant(current));
                    continue;
                }

                chars.Append(current);
            }

            return chars.ToString();
        }

        private bool IsTelemetryEnabled()
        {
            if (config != null && config.telemetryEnabled)
            {
                return true;
            }

            if (!hasLoggedDisabledWarning)
            {
                Debug.LogWarning("Telemetry is disabled; session and record operations will be ignored.");
                hasLoggedDisabledWarning = true;
            }

            return false;
        }
    }
}
