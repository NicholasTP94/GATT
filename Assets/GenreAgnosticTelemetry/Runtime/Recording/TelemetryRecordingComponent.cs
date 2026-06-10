using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GenreAgnosticTelemetry
{
    [DisallowMultipleComponent]
    public sealed class TelemetryRecordingComponent : MonoBehaviour
    {
        [Tooltip("The manager that receives this component's events. Leave this empty to find one in the scene automatically.")]
        public TelemetryManager telemetryManager;

        [Tooltip("The event schema this component will record, including its event ID and custom fields.")]
        public TelemetryEventDefinition eventDefinition;

        [Tooltip("Choose what causes this component to record its event. Manual waits for Record or Record From Unity Event to be called.")]
        public TelemetryRecordingTrigger trigger = TelemetryRecordingTrigger.Manual;

        [Tooltip("Maps each field in the event definition to a constant, scene value, trigger context, or component member.")]
        public List<TelemetryFieldBinding> fieldBindings = new List<TelemetryFieldBinding>();

        [Tooltip("Write this GameObject's name into the event's object ID.")]
        public bool recordObjectId = true;

        [Tooltip("Write an object type into the event. If Object Type is empty, the GameObject's tag is used.")]
        public bool recordObjectType;

        [Tooltip("The object type written when Record Object Type is enabled. Leave it empty to use the GameObject's tag.")]
        public string objectType;

        [Tooltip("Include this GameObject's world position in the recorded event.")]
        public bool recordTransformPosition;

        [Tooltip("Allow this component to record in a player build. Turn it off to keep the setup without collecting events.")]
        public bool enabledInBuild = true;

        [Header("Timer Triggers")]
        [Tooltip("Seconds between recordings for Fixed Interval and Repeat Count triggers.")]
        public double intervalSeconds = 1;

        [Tooltip("Seconds to wait before After Delay fires, or before a repeating timer begins.")]
        public double startDelaySeconds;

        [Tooltip("Record once as soon as this component is enabled when a timer trigger is selected.")]
        public bool recordImmediatelyOnStart;

        [Tooltip("Use real time so timer triggers keep running when Time.timeScale is zero.")]
        public bool useUnscaledTime = true;

        [Tooltip("Stop after this many timer recordings. Use 0 for no limit, except Repeat Count requires a value above 0.")]
        public int maxRepeatCount;

        [Tooltip("The shortest delay between recordings when Random Interval is selected.")]
        public double randomIntervalMinSeconds = 0.5;

        [Tooltip("The longest delay between recordings when Random Interval is selected.")]
        public double randomIntervalMaxSeconds = 2;

        [Header("Input Triggers")]
        [Tooltip("The key that records the event when Key Down is selected.")]
        public KeyCode key = KeyCode.Space;

        [Tooltip("The mouse button that records the event when Mouse Button Down is selected: 0 is left, 1 is right, and 2 is middle.")]
        public int mouseButton;

        [Header("Value Triggers")]
        [Tooltip("The public numeric field or property watched by Value Changed and Value Threshold triggers.")]
        public TelemetryFieldBinding observedValueBinding = new TelemetryFieldBinding
        {
            source = TelemetryBindingSource.ComponentMember
        };

        [Tooltip("How the watched value is compared with Threshold Value when Value Threshold is selected.")]
        public TelemetryThresholdComparison thresholdComparison = TelemetryThresholdComparison.GreaterThanOrEqual;

        [Tooltip("The value that must be crossed before a Value Threshold trigger records the event.")]
        public double thresholdValue = 1;

        private bool isQuitting;
        private bool hasRecordedDelayed;
        private bool hasObservedValue;
        private bool previousThresholdMatched;
        private double previousObservedValue;
        private double enabledAtTime;
        private double nextRecordTime;
        private int repeatCount;

        private void Awake()
        {
            enabledAtTime = CurrentTime;

            if (trigger == TelemetryRecordingTrigger.Awake)
            {
                Record();
            }
        }

        private void OnEnable()
        {
            enabledAtTime = CurrentTime;
            hasRecordedDelayed = false;
            repeatCount = 0;
            ScheduleNextRecord();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;

            if (recordImmediatelyOnStart && IsTimerTrigger())
            {
                Record();
                repeatCount++;
            }

            if (trigger == TelemetryRecordingTrigger.OnEnable)
            {
                Record();
            }
        }

        private void Start()
        {
            if (trigger == TelemetryRecordingTrigger.Start)
            {
                Record();
            }
        }

        private void Update()
        {
            if (!enabledInBuild)
            {
                return;
            }

            UpdateTimerTriggers();
            UpdateInputTriggers();
            UpdateValueTriggers();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;

            if (!isQuitting && trigger == TelemetryRecordingTrigger.OnDisable)
            {
                Record();
            }
        }

        private void OnDestroy()
        {
            if (!isQuitting && trigger == TelemetryRecordingTrigger.OnDestroy)
            {
                Record();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (trigger == TelemetryRecordingTrigger.OnApplicationPause)
            {
                Record(new TelemetryRecordingContext
                {
                    contextBool = pauseStatus
                });
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (trigger == TelemetryRecordingTrigger.OnApplicationFocus)
            {
                Record(new TelemetryRecordingContext
                {
                    contextBool = hasFocus
                });
            }
        }

        private void OnApplicationQuit()
        {
            if (trigger == TelemetryRecordingTrigger.OnApplicationQuit)
            {
                Record();
            }

            isQuitting = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (trigger == TelemetryRecordingTrigger.OnTriggerEnter)
            {
                Record(ContextFromObject(other.gameObject));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (trigger == TelemetryRecordingTrigger.OnTriggerExit)
            {
                Record(ContextFromObject(other.gameObject));
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (trigger == TelemetryRecordingTrigger.OnTriggerStay)
            {
                Record(ContextFromObject(other.gameObject));
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (trigger == TelemetryRecordingTrigger.OnCollisionEnter)
            {
                Record(ContextFromObject(collision.gameObject));
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (trigger == TelemetryRecordingTrigger.OnCollisionExit)
            {
                Record(ContextFromObject(collision.gameObject));
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (trigger == TelemetryRecordingTrigger.OnCollisionStay)
            {
                Record(ContextFromObject(collision.gameObject));
            }
        }

        private void OnMouseDown()
        {
            if (trigger == TelemetryRecordingTrigger.OnMouseDown)
            {
                Record();
            }
        }

        private void OnBecameVisible()
        {
            if (trigger == TelemetryRecordingTrigger.BecameVisible)
            {
                Record();
            }
        }

        private void OnBecameInvisible()
        {
            if (trigger == TelemetryRecordingTrigger.BecameInvisible)
            {
                Record();
            }
        }

        public bool Record()
        {
            return Record(null);
        }

        public bool RecordFromUnityEvent()
        {
            return Record();
        }

        public bool Record(TelemetryRecordingContext context)
        {
            if (!enabledInBuild || eventDefinition == null)
            {
                return false;
            }

            var manager = ResolveTelemetryManager();

            if (manager == null)
            {
                Debug.LogWarning("TelemetryRecordingComponent could not find a TelemetryManager.");
                return false;
            }

            var telemetryEvent = BuildEvent(context);

            if (telemetryEvent == null)
            {
                return false;
            }

            var validationResults = TelemetryDefinitionValidator.ValidateRuntimeEvent(
                eventDefinition,
                telemetryEvent,
                null);

            if (validationResults.Count > 0)
            {
                Debug.LogWarning(
                    $"TelemetryRecordingComponent produced {validationResults.Count} validation warning(s) for event '{eventDefinition.eventType}'.");
            }

            return manager.TryRecordEvent(telemetryEvent);
        }

        public List<TelemetryValidationResult> ValidateBindings()
        {
            var results = new List<TelemetryValidationResult>();

            if (eventDefinition == null)
            {
                results.Add(Result("definition_required", "eventDefinition", "Event definition is required."));
                return results;
            }

            ValidateTriggerConfiguration(results);
            ValidateFieldBindings(results);
            return results;
        }

        private void UpdateTimerTriggers()
        {
            if (trigger == TelemetryRecordingTrigger.AfterDelay)
            {
                if (!hasRecordedDelayed && CurrentTime - enabledAtTime >= startDelaySeconds)
                {
                    hasRecordedDelayed = true;
                    Record();
                }

                return;
            }

            if (!IsRepeatingTimerTrigger())
            {
                return;
            }

            if (maxRepeatCount > 0 && repeatCount >= maxRepeatCount)
            {
                return;
            }

            if (trigger == TelemetryRecordingTrigger.RepeatCount && maxRepeatCount <= 0)
            {
                return;
            }

            if (CurrentTime < nextRecordTime)
            {
                return;
            }

            Record();
            repeatCount++;
            ScheduleNextRecord();
        }

        private void UpdateInputTriggers()
        {
            if (trigger == TelemetryRecordingTrigger.KeyDown && Input.GetKeyDown(key))
            {
                Record();
            }
            else if (trigger == TelemetryRecordingTrigger.MouseButtonDown
                && Input.GetMouseButtonDown(mouseButton))
            {
                Record();
            }
        }

        private void UpdateValueTriggers()
        {
            if (trigger != TelemetryRecordingTrigger.ValueChanged
                && trigger != TelemetryRecordingTrigger.ValueThreshold)
            {
                return;
            }

            if (!TryResolveObservedNumber(out var currentValue))
            {
                return;
            }

            if (!hasObservedValue)
            {
                hasObservedValue = true;
                previousObservedValue = currentValue;
                previousThresholdMatched = Compare(currentValue, thresholdComparison, thresholdValue);
                return;
            }

            if (trigger == TelemetryRecordingTrigger.ValueChanged)
            {
                if (!Approximately(previousObservedValue, currentValue))
                {
                    previousObservedValue = currentValue;
                    Record();
                }

                return;
            }

            var thresholdMatched = Compare(currentValue, thresholdComparison, thresholdValue);

            if (!previousThresholdMatched && thresholdMatched)
            {
                Record();
            }

            previousObservedValue = currentValue;
            previousThresholdMatched = thresholdMatched;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (trigger == TelemetryRecordingTrigger.SceneLoaded)
            {
                Record(new TelemetryRecordingContext
                {
                    sceneName = scene.name
                });
            }
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (trigger == TelemetryRecordingTrigger.SceneUnloaded)
            {
                Record(new TelemetryRecordingContext
                {
                    sceneName = scene.name
                });
            }
        }

        private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (trigger == TelemetryRecordingTrigger.ActiveSceneChanged)
            {
                Record(new TelemetryRecordingContext
                {
                    sceneName = newScene.name
                });
            }
        }

        private TelemetryEvent BuildEvent(TelemetryRecordingContext context)
        {
            if (eventDefinition == null)
            {
                return null;
            }

            var telemetryEvent = new TelemetryEvent
            {
                eventType = eventDefinition.eventType,
                category = eventDefinition.category,
                name = eventDefinition.eventName
            };

            if (recordObjectId)
            {
                telemetryEvent.objectId = gameObject.name;
            }

            if (recordObjectType)
            {
                telemetryEvent.objectType = string.IsNullOrWhiteSpace(objectType)
                    ? gameObject.tag
                    : objectType;
            }

            if (recordTransformPosition)
            {
                var position = transform.position;
                telemetryEvent.SetPosition(position.x, position.y, position.z);
            }

            ApplyDefaultProperties(telemetryEvent);

            for (var i = 0; i < fieldBindings.Count; i++)
            {
                var binding = fieldBindings[i];

                if (binding == null || string.IsNullOrWhiteSpace(binding.fieldKey))
                {
                    continue;
                }

                var field = FindField(binding.fieldKey);
                var property = ResolveBinding(binding, field, context);

                if (property == null)
                {
                    continue;
                }

                telemetryEvent.properties.RemoveAll(existing => existing.key == property.key);
                telemetryEvent.properties.Add(property);
            }

            return telemetryEvent;
        }

        private void ApplyDefaultProperties(TelemetryEvent telemetryEvent)
        {
            for (var i = 0; i < eventDefinition.fields.Count; i++)
            {
                var field = eventDefinition.fields[i];

                if (field == null || string.IsNullOrWhiteSpace(field.key))
                {
                    continue;
                }

                if (field.fieldType == TelemetryDefinitionFieldType.String
                    || field.fieldType == TelemetryDefinitionFieldType.Enum)
                {
                    if (!string.IsNullOrEmpty(field.defaultStringValue))
                    {
                        telemetryEvent.properties.Add(TelemetryProperty.String(field.key, field.defaultStringValue));
                    }
                }
                else if (field.fieldType == TelemetryDefinitionFieldType.Bool)
                {
                    telemetryEvent.properties.Add(TelemetryProperty.Bool(field.key, field.defaultBoolValue));
                }
                else
                {
                    telemetryEvent.properties.Add(TelemetryProperty.Number(field.key, field.defaultNumberValue));
                }
            }
        }

        private void ValidateTriggerConfiguration(List<TelemetryValidationResult> results)
        {
            if ((IsRepeatingTimerTrigger() || trigger == TelemetryRecordingTrigger.AfterDelay)
                && intervalSeconds <= 0
                && trigger != TelemetryRecordingTrigger.AfterDelay)
            {
                results.Add(Result("invalid_interval", "intervalSeconds", "Interval seconds must be greater than zero."));
            }

            if (trigger == TelemetryRecordingTrigger.RepeatCount && maxRepeatCount <= 0)
            {
                results.Add(Result("invalid_repeat_count", "maxRepeatCount", "RepeatCount requires max repeat count greater than zero."));
            }

            if (trigger == TelemetryRecordingTrigger.AfterDelay && startDelaySeconds < 0)
            {
                results.Add(Result("invalid_start_delay", "startDelaySeconds", "Start delay must not be negative."));
            }

            if (trigger == TelemetryRecordingTrigger.RandomInterval)
            {
                if (randomIntervalMinSeconds <= 0 || randomIntervalMaxSeconds <= 0)
                {
                    results.Add(Result("invalid_random_interval", "randomInterval", "Random interval bounds must be greater than zero."));
                }

                if (randomIntervalMinSeconds > randomIntervalMaxSeconds)
                {
                    results.Add(Result("invalid_random_interval_range", "randomInterval", "Random interval minimum must not exceed maximum."));
                }
            }

            if (trigger == TelemetryRecordingTrigger.ValueChanged
                || trigger == TelemetryRecordingTrigger.ValueThreshold)
            {
                if (observedValueBinding == null
                    || observedValueBinding.source != TelemetryBindingSource.ComponentMember
                    || observedValueBinding.targetComponent == null
                    || string.IsNullOrWhiteSpace(observedValueBinding.memberName))
                {
                    results.Add(Result(
                        "observed_member_required",
                        "observedValueBinding",
                        "Value triggers require an observed component member binding."));
                }
            }
        }

        private void ValidateFieldBindings(List<TelemetryValidationResult> results)
        {
            var bindingsByField = new HashSet<string>();

            for (var i = 0; i < fieldBindings.Count; i++)
            {
                var binding = fieldBindings[i];

                if (binding == null)
                {
                    results.Add(Result("binding_null", $"fieldBindings[{i}]", $"fieldBindings[{i}] is null."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.fieldKey))
                {
                    results.Add(Result("binding_key_required", $"fieldBindings[{i}].fieldKey", "Binding field key is required."));
                    continue;
                }

                if (!bindingsByField.Add(binding.fieldKey))
                {
                    results.Add(Result(
                        "duplicate_binding",
                        $"fieldBindings[{i}].fieldKey",
                        $"Field '{binding.fieldKey}' has multiple bindings."));
                }

                if (binding.source == TelemetryBindingSource.ComponentMember)
                {
                    if (binding.targetComponent == null)
                    {
                        results.Add(Result(
                            "component_binding_target_required",
                            $"fieldBindings[{i}].targetComponent",
                            $"Field '{binding.fieldKey}' uses ComponentMember and requires a target component."));
                    }

                    if (string.IsNullOrWhiteSpace(binding.memberName))
                    {
                        results.Add(Result(
                            "component_binding_member_required",
                            $"fieldBindings[{i}].memberName",
                            $"Field '{binding.fieldKey}' uses ComponentMember and requires a member name."));
                    }
                }
            }

            for (var i = 0; i < eventDefinition.fields.Count; i++)
            {
                var field = eventDefinition.fields[i];

                if (field == null || string.IsNullOrWhiteSpace(field.key) || !field.required)
                {
                    continue;
                }

                if (!bindingsByField.Contains(field.key))
                {
                    results.Add(Result(
                        "missing_required_binding",
                        $"fieldBindings.{field.key}",
                        $"Required field '{field.key}' has no binding."));
                }
            }
        }

        private TelemetryFieldDefinition FindField(string key)
        {
            if (eventDefinition == null)
            {
                return null;
            }

            for (var i = 0; i < eventDefinition.fields.Count; i++)
            {
                var field = eventDefinition.fields[i];

                if (field != null && field.key == key)
                {
                    return field;
                }
            }

            return null;
        }

        private TelemetryProperty ResolveBinding(
            TelemetryFieldBinding binding,
            TelemetryFieldDefinition field,
            TelemetryRecordingContext context)
        {
            if (field != null)
            {
                if (field.fieldType == TelemetryDefinitionFieldType.Bool)
                {
                    return TelemetryProperty.Bool(binding.fieldKey, ResolveBool(binding, context));
                }

                if (field.fieldType == TelemetryDefinitionFieldType.Number
                    || field.fieldType == TelemetryDefinitionFieldType.Integer)
                {
                    return TelemetryProperty.Number(binding.fieldKey, ResolveNumber(binding, context));
                }
            }

            return TelemetryProperty.String(binding.fieldKey, ResolveString(binding, context));
        }

        private string ResolveString(TelemetryFieldBinding binding, TelemetryRecordingContext context)
        {
            switch (binding.source)
            {
                case TelemetryBindingSource.GameObjectName:
                    return gameObject.name;
                case TelemetryBindingSource.GameObjectTag:
                    return gameObject.tag;
                case TelemetryBindingSource.GameObjectLayer:
                    return LayerMask.LayerToName(gameObject.layer);
                case TelemetryBindingSource.SceneName:
                    return SceneManager.GetActiveScene().name;
                case TelemetryBindingSource.ContextObjectName:
                    return context != null && context.contextObject != null ? context.contextObject.name : string.Empty;
                case TelemetryBindingSource.ContextObjectTag:
                    return context != null && context.contextObject != null ? context.contextObject.tag : string.Empty;
                case TelemetryBindingSource.ContextObjectLayer:
                    return context != null && context.contextObject != null
                        ? LayerMask.LayerToName(context.contextObject.layer)
                        : string.Empty;
                case TelemetryBindingSource.ContextSceneName:
                    return context != null ? context.sceneName : string.Empty;
                case TelemetryBindingSource.ComponentMember:
                    return Convert.ToString(ResolveComponentMember(binding), CultureInfo.InvariantCulture);
                default:
                    return binding.stringValue;
            }
        }

        private double ResolveNumber(TelemetryFieldBinding binding, TelemetryRecordingContext context)
        {
            switch (binding.source)
            {
                case TelemetryBindingSource.TransformPositionX:
                    return transform.position.x;
                case TelemetryBindingSource.TransformPositionY:
                    return transform.position.y;
                case TelemetryBindingSource.TransformPositionZ:
                    return transform.position.z;
                case TelemetryBindingSource.GameObjectLayer:
                    return gameObject.layer;
                case TelemetryBindingSource.TimeSinceEnabled:
                    return CurrentTime - enabledAtTime;
                case TelemetryBindingSource.TimeSinceStartup:
                    return Time.realtimeSinceStartupAsDouble;
                case TelemetryBindingSource.FrameCount:
                    return Time.frameCount;
                case TelemetryBindingSource.ContextObjectLayer:
                    return context != null && context.contextObject != null ? context.contextObject.layer : 0;
                case TelemetryBindingSource.ComponentMember:
                    return ConvertToDouble(ResolveComponentMember(binding));
                default:
                    return binding.numberValue;
            }
        }

        private bool ResolveBool(TelemetryFieldBinding binding, TelemetryRecordingContext context)
        {
            switch (binding.source)
            {
                case TelemetryBindingSource.ContextBool:
                    return context != null && context.contextBool;
                case TelemetryBindingSource.ComponentMember:
                    return ConvertToBool(ResolveComponentMember(binding));
                default:
                    return binding.source == TelemetryBindingSource.ConstantBool && binding.boolValue;
            }
        }

        private bool TryResolveObservedNumber(out double value)
        {
            value = 0;

            if (observedValueBinding == null)
            {
                return false;
            }

            var rawValue = ResolveComponentMember(observedValueBinding);

            if (rawValue == null)
            {
                return false;
            }

            value = ConvertToDouble(rawValue);
            return true;
        }

        private object ResolveComponentMember(TelemetryFieldBinding binding)
        {
            if (binding == null
                || binding.targetComponent == null
                || string.IsNullOrWhiteSpace(binding.memberName))
            {
                return null;
            }

            var type = binding.targetComponent.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public;
            var property = type.GetProperty(binding.memberName, flags);

            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(binding.targetComponent, null);
            }

            var field = type.GetField(binding.memberName, flags);
            return field != null ? field.GetValue(binding.targetComponent) : null;
        }

        private void ScheduleNextRecord()
        {
            if (trigger == TelemetryRecordingTrigger.RandomInterval)
            {
                nextRecordTime = CurrentTime + UnityEngine.Random.Range(
                    (float)randomIntervalMinSeconds,
                    (float)randomIntervalMaxSeconds);
                return;
            }

            var delay = repeatCount == 0 ? startDelaySeconds : 0;
            nextRecordTime = CurrentTime + delay + intervalSeconds;
        }

        private bool IsTimerTrigger()
        {
            return trigger == TelemetryRecordingTrigger.FixedInterval
                || trigger == TelemetryRecordingTrigger.AfterDelay
                || trigger == TelemetryRecordingTrigger.RepeatCount
                || trigger == TelemetryRecordingTrigger.RandomInterval;
        }

        private bool IsRepeatingTimerTrigger()
        {
            return trigger == TelemetryRecordingTrigger.FixedInterval
                || trigger == TelemetryRecordingTrigger.RepeatCount
                || trigger == TelemetryRecordingTrigger.RandomInterval;
        }

        private TelemetryManager ResolveTelemetryManager()
        {
            if (telemetryManager != null)
            {
                return telemetryManager;
            }

            telemetryManager = FindAnyObjectByType<TelemetryManager>();
            return telemetryManager;
        }

        private double CurrentTime
        {
            get { return useUnscaledTime ? Time.unscaledTimeAsDouble : Time.timeAsDouble; }
        }

        private static TelemetryRecordingContext ContextFromObject(GameObject contextObject)
        {
            return new TelemetryRecordingContext
            {
                contextObject = contextObject
            };
        }

        private static bool Approximately(double left, double right)
        {
            return Math.Abs(left - right) <= 0.000001;
        }

        private static bool Compare(
            double value,
            TelemetryThresholdComparison comparison,
            double threshold)
        {
            switch (comparison)
            {
                case TelemetryThresholdComparison.LessThan:
                    return value < threshold;
                case TelemetryThresholdComparison.LessThanOrEqual:
                    return value <= threshold;
                case TelemetryThresholdComparison.Equal:
                    return Approximately(value, threshold);
                case TelemetryThresholdComparison.NotEqual:
                    return !Approximately(value, threshold);
                case TelemetryThresholdComparison.GreaterThanOrEqual:
                    return value >= threshold;
                default:
                    return value > threshold;
            }
        }

        private static double ConvertToDouble(object value)
        {
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static bool ConvertToBool(object value)
        {
            if (value == null)
            {
                return false;
            }

            try
            {
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static TelemetryValidationResult Result(string code, string path, string message)
        {
            return new TelemetryValidationResult(
                TelemetryValidationSeverity.Warning,
                code,
                path,
                message);
        }
    }

    public sealed class TelemetryRecordingContext
    {
        public GameObject contextObject;
        public string sceneName;
        public bool contextBool;
    }
}
