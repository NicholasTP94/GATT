using System;
using UnityEngine;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public sealed class TelemetryFieldBinding
    {
        [Tooltip("The event-definition field that receives this value. It must match the field's key exactly.")]
        public string fieldKey;

        [Tooltip("Where this binding gets its value when the event is recorded.")]
        public TelemetryBindingSource source;

        [Tooltip("The value used when Source is Constant String, or as the text fallback for another string source.")]
        public string stringValue;

        [Tooltip("The value used when Source is Constant Number, or as the numeric fallback for another number source.")]
        public double numberValue;

        [Tooltip("The value used when Source is Constant Bool.")]
        public bool boolValue;

        [Tooltip("An optional object reference kept with this binding. Built-in GameObject sources read from the recorder's own GameObject.")]
        public GameObject targetObject;

        [Tooltip("The component read when Source is Component Member.")]
        public Component targetComponent;

        [Tooltip("The exact name of the public field or property read from Target Component.")]
        public string memberName;
    }
}
