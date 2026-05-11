using System;
using UnityEngine;

namespace GenreAgnosticTelemetry
{
    [Serializable]
    public sealed class TelemetryValidationProfile
    {
        [Tooltip("Warn when an event or field has no description.")]
        public bool requireDescriptions = true;

        [Tooltip("Warn when an event does not name the person or team responsible for it.")]
        public bool requireOwner = true;

        [Tooltip("Marks privacy classification as required for validation and any catalog tooling that reads this profile.")]
        public bool requirePrivacyClassification = true;

        [Tooltip("Allow recorded events to contain keys that are not listed in their event definition.")]
        public bool allowUnknownRuntimeFields;

        [Tooltip("The largest number of custom fields a single event definition may contain.")]
        public int maxFieldsPerEvent = 32;

        [Tooltip("The longest text value accepted by tooling that applies this validation profile.")]
        public int maxStringLength = 512;

        [Tooltip("The largest number of choices an Enum field may define.")]
        public int maxEnumOptions = 64;
    }
}
