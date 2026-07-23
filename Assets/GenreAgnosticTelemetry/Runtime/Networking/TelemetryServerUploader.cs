using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public sealed class TelemetryUploadResult
    {
        public bool success;
        public long responseCode;
        public string sessionId;
        public string message;
        public string responseBody;
        public string completedAtUtc;
    }

    [DisallowMultipleComponent]
    public sealed class TelemetryServerUploader : MonoBehaviour
    {
        public const string DefaultServerBaseUrl = "http://127.0.0.1:8000";
        public const string DefaultIngestPath = "/telemetry/sessions";

        [SerializeField]
        [Tooltip("The manager that provides sessions to upload. Leave this empty to find one in the scene automatically.")]
        private TelemetryManager telemetryManager;

        [SerializeField]
        [Tooltip("The server address used for uploads, including the protocol and port but not the ingest path.")]
        private string serverBaseUrl = DefaultServerBaseUrl;

        [SerializeField]
        [Tooltip("The server route that accepts telemetry sessions. It is appended to Server Base Url.")]
        private string ingestPath = DefaultIngestPath;

        [SerializeField]
        [Tooltip("Upload each session automatically after the Telemetry Manager ends it.")]
        private bool uploadOnSessionEnd;

        [SerializeField]
        [Tooltip("The most characters kept from a server response for the in-game status preview. Use 0 to hide response bodies.")]
        private int responsePreviewCharacterLimit = 2000;

        private TelemetryManager subscribedTelemetryManager;
        private TelemetryUploadResult lastResult = new TelemetryUploadResult
        {
            message = "No upload attempted."
        };
        private bool isUploading;
        private bool suppressNextSessionEndedUpload;

        public bool IsUploading
        {
            get { return isUploading; }
        }

        public TelemetryUploadResult LastResult
        {
            get { return lastResult; }
        }

        public string ServerBaseUrl
        {
            get { return serverBaseUrl; }
            set { serverBaseUrl = string.IsNullOrWhiteSpace(value) ? DefaultServerBaseUrl : value.Trim(); }
        }

        public string IngestPath
        {
            get { return ingestPath; }
            set { ingestPath = string.IsNullOrWhiteSpace(value) ? DefaultIngestPath : value.Trim(); }
        }

        public bool UploadOnSessionEnd
        {
            get { return uploadOnSessionEnd; }
            set { uploadOnSessionEnd = value; }
        }

        private void Awake()
        {
            EnsureTelemetryManager();
        }

        private void OnEnable()
        {
            SubscribeToSessionEnded();
        }

        private void OnDisable()
        {
            UnsubscribeFromSessionEnded();
        }

        public void SetTelemetryManager(TelemetryManager manager)
        {
            if (telemetryManager == manager)
            {
                return;
            }

            UnsubscribeFromSessionEnded();
            telemetryManager = manager;

            if (isActiveAndEnabled)
            {
                SubscribeToSessionEnded();
            }
        }

        public bool UploadActiveSession()
        {
            EnsureTelemetryManager();

            if (telemetryManager == null || telemetryManager.ActiveSession == null)
            {
                SetFailure("No active telemetry session is available to upload.", null);
                return false;
            }

            return UploadSession(telemetryManager.ActiveSession);
        }

        public bool UploadLastCompletedSession()
        {
            EnsureTelemetryManager();

            if (telemetryManager == null || telemetryManager.LastCompletedSession == null)
            {
                SetFailure("No completed telemetry session is available to upload.", null);
                return false;
            }

            return UploadSession(telemetryManager.LastCompletedSession);
        }

        public bool EndSessionAndUpload()
        {
            EnsureTelemetryManager();

            if (telemetryManager == null)
            {
                SetFailure("Telemetry manager is required before uploading.", null);
                return false;
            }

            TelemetrySession session;

            if (telemetryManager.ActiveSession != null)
            {
                suppressNextSessionEndedUpload = true;
                session = telemetryManager.EndSession();
                suppressNextSessionEndedUpload = false;
            }
            else
            {
                session = telemetryManager.LastCompletedSession;
            }

            if (session == null)
            {
                SetFailure("No active or completed telemetry session is available to upload.", null);
                return false;
            }

            return UploadSession(session);
        }

        public bool UploadSession(TelemetrySession session)
        {
            EnsureTelemetryManager();

            if (session == null)
            {
                SetFailure("Telemetry session is required before uploading.", null);
                return false;
            }

            if (telemetryManager == null)
            {
                SetFailure("Telemetry manager is required before uploading.", session.sessionId);
                return false;
            }

            if (isUploading)
            {
                SetFailure("Telemetry upload is already in progress.", session.sessionId);
                return false;
            }

            var json = telemetryManager.SerializeSessionToJson(session);

            if (string.IsNullOrWhiteSpace(json))
            {
                SetFailure("Serialized telemetry payload was empty.", session.sessionId);
                return false;
            }

            // Keep a local copy even when the server is unavailable or rejects the upload.
            telemetryManager.TrySaveSessionToJson(session, json);

            StartCoroutine(PostSession(json, session.sessionId));
            return true;
        }

        private IEnumerator PostSession(string json, string sessionId)
        {
            isUploading = true;
            lastResult = new TelemetryUploadResult
            {
                success = false,
                responseCode = 0,
                sessionId = sessionId,
                message = "Uploading telemetry session...",
                responseBody = string.Empty,
                completedAtUtc = TelemetrySession.FormatUtc(DateTime.UtcNow)
            };

            var body = Encoding.UTF8.GetBytes(json);

            using (var request = new UnityWebRequest(BuildIngestUrl(), UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();

                var responseBody = request.downloadHandler == null
                    ? string.Empty
                    : LimitResponseBody(request.downloadHandler.text);
                var success = request.result == UnityWebRequest.Result.Success
                    && request.responseCode >= 200
                    && request.responseCode < 300;

                lastResult = new TelemetryUploadResult
                {
                    success = success,
                    responseCode = request.responseCode,
                    sessionId = sessionId,
                    message = success
                        ? "Telemetry upload accepted by server."
                        : BuildFailureMessage(request),
                    responseBody = responseBody,
                    completedAtUtc = TelemetrySession.FormatUtc(DateTime.UtcNow)
                };
            }

            isUploading = false;

            if (lastResult.success)
            {
                Debug.Log($"Telemetry session '{sessionId}' uploaded to {BuildIngestUrl()}.");
            }
            else
            {
                Debug.LogWarning($"Telemetry session '{sessionId}' upload failed: {lastResult.message}");
            }
        }

        private void HandleSessionEnded(TelemetrySession session)
        {
            if (suppressNextSessionEndedUpload)
            {
                suppressNextSessionEndedUpload = false;
                return;
            }

            if (uploadOnSessionEnd)
            {
                UploadSession(session);
            }
        }

        private void EnsureTelemetryManager()
        {
            if (telemetryManager == null)
            {
                telemetryManager = FindAnyObjectByType<TelemetryManager>();
            }
        }

        private void SubscribeToSessionEnded()
        {
            EnsureTelemetryManager();

            if (subscribedTelemetryManager == telemetryManager)
            {
                return;
            }

            UnsubscribeFromSessionEnded();

            if (telemetryManager != null)
            {
                telemetryManager.SessionEnded += HandleSessionEnded;
                subscribedTelemetryManager = telemetryManager;
            }
        }

        private void UnsubscribeFromSessionEnded()
        {
            if (subscribedTelemetryManager != null)
            {
                subscribedTelemetryManager.SessionEnded -= HandleSessionEnded;
                subscribedTelemetryManager = null;
            }
        }

        private string BuildIngestUrl()
        {
            var baseUrl = string.IsNullOrWhiteSpace(serverBaseUrl)
                ? DefaultServerBaseUrl
                : serverBaseUrl.Trim();
            var path = string.IsNullOrWhiteSpace(ingestPath)
                ? DefaultIngestPath
                : ingestPath.Trim();

            return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        private string LimitResponseBody(string value)
        {
            if (string.IsNullOrEmpty(value) || responsePreviewCharacterLimit <= 0)
            {
                return string.Empty;
            }

            if (value.Length <= responsePreviewCharacterLimit)
            {
                return value;
            }

            return value.Substring(0, responsePreviewCharacterLimit) + "\n...";
        }

        private static string BuildFailureMessage(UnityWebRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.error))
            {
                return request.error;
            }

            return $"Server returned HTTP {request.responseCode}.";
        }

        private void SetFailure(string message, string sessionId)
        {
            lastResult = new TelemetryUploadResult
            {
                success = false,
                responseCode = 0,
                sessionId = sessionId,
                message = message,
                responseBody = string.Empty,
                completedAtUtc = TelemetrySession.FormatUtc(DateTime.UtcNow)
            };

            Debug.LogWarning(message);
        }
    }
}
