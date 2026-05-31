namespace GenreAgnosticTelemetry
{
    public enum TelemetryRecordingTrigger
    {
        Manual,
        Awake,
        Start,
        OnEnable,
        OnDisable,
        OnDestroy,
        OnApplicationPause,
        OnApplicationFocus,
        OnApplicationQuit,
        SceneLoaded,
        SceneUnloaded,
        ActiveSceneChanged,
        OnTriggerEnter,
        OnTriggerExit,
        OnTriggerStay,
        OnCollisionEnter,
        OnCollisionExit,
        OnCollisionStay,
        OnMouseDown,
        BecameVisible,
        BecameInvisible,
        FixedInterval,
        AfterDelay,
        RepeatCount,
        RandomInterval,
        KeyDown,
        MouseButtonDown,
        ValueChanged,
        ValueThreshold
    }

    public enum TelemetryBindingSource
    {
        ConstantString,
        ConstantNumber,
        ConstantBool,
        GameObjectName,
        GameObjectTag,
        GameObjectLayer,
        SceneName,
        TimeSinceEnabled,
        TimeSinceStartup,
        FrameCount,
        TransformPositionX,
        TransformPositionY,
        TransformPositionZ,
        ContextObjectName,
        ContextObjectTag,
        ContextObjectLayer,
        ContextSceneName,
        ContextBool,
        ComponentMember
    }

    public enum TelemetryThresholdComparison
    {
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual,
        GreaterThanOrEqual,
        GreaterThan
    }
}
