using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace GenreAgnosticTelemetry
{
    public static class TelemetryJsonSerializer
    {
        public const string DefaultSchemaVersion = "1.0";

        private const BindingFlags PublicInstanceFields =
            BindingFlags.Instance | BindingFlags.Public;

        public static string SerializeSession(TelemetrySession session)
        {
            return SerializeSession(session, null);
        }

        public static string SerializeSession(TelemetrySession session, bool prettyPrint)
        {
            return SerializeSession(
                session,
                new TelemetryJsonSerializationOptions
                {
                    prettyPrint = prettyPrint
                });
        }

        public static string SerializeSession(
            TelemetrySession session,
            TelemetryJsonSerializationOptions options)
        {
            options = NormalizeOptions(options);
            var writer = new JsonWriter(options.prettyPrint, options.includeNullFields);

            if (options.wrapSession)
            {
                writer.BeginObject();
                writer.WritePropertyName("schema_version");
                writer.WriteString(options.schemaVersion);
                writer.WritePropertyName("payload_type");
                writer.WriteString("telemetry_session");
                writer.WritePropertyName("generated_at_utc");
                writer.WriteString(TelemetrySession.FormatUtc(DateTime.UtcNow));
                writer.WritePropertyName("session");
                WriteObject(writer, session);
                writer.EndObject();
            }
            else
            {
                WriteObject(writer, session);
            }

            return writer.ToString();
        }

        public static string SerializeEvent(TelemetryEventBase telemetryEvent)
        {
            return SerializeEvent(telemetryEvent, null);
        }

        public static string SerializeEvent(
            TelemetryEventBase telemetryEvent,
            TelemetryJsonSerializationOptions options)
        {
            options = NormalizeOptions(options);
            var writer = new JsonWriter(options.prettyPrint, options.includeNullFields);
            WriteObject(writer, telemetryEvent);
            return writer.ToString();
        }

        private static TelemetryJsonSerializationOptions NormalizeOptions(
            TelemetryJsonSerializationOptions options)
        {
            if (options == null)
            {
                options = new TelemetryJsonSerializationOptions();
            }

            if (string.IsNullOrWhiteSpace(options.schemaVersion))
            {
                options.schemaVersion = DefaultSchemaVersion;
            }

            return options;
        }

        private static void WriteValue(JsonWriter writer, object value)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var type = value.GetType();

            if (type == typeof(string))
            {
                writer.WriteString((string)value);
                return;
            }

            if (type == typeof(bool))
            {
                writer.WriteBool((bool)value);
                return;
            }

            if (type.IsEnum)
            {
                writer.WriteString(ToSnakeCase(value.ToString()));
                return;
            }

            if (IsNumeric(type))
            {
                writer.WriteNumber(value);
                return;
            }

            var enumerable = value as IEnumerable;

            if (enumerable != null)
            {
                WriteArray(writer, enumerable);
                return;
            }

            WriteObject(writer, value);
        }

        private static void WriteArray(JsonWriter writer, IEnumerable values)
        {
            writer.BeginArray();

            foreach (var value in values)
            {
                writer.WriteArrayValue();
                WriteValue(writer, value);
            }

            writer.EndArray();
        }

        private static void WriteObject(JsonWriter writer, object value)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.BeginObject();

            foreach (var field in GetSerializableFields(value.GetType()))
            {
                var fieldValue = field.GetValue(value);

                if (fieldValue == null && !writer.IncludeNullFields)
                {
                    continue;
                }

                writer.WritePropertyName(ToSnakeCase(field.Name));
                WriteValue(writer, fieldValue);
            }

            writer.EndObject();
        }

        private static List<FieldInfo> GetSerializableFields(Type type)
        {
            var fields = new List<FieldInfo>();

            while (type != null && type != typeof(object))
            {
                fields.AddRange(type.GetFields(PublicInstanceFields | BindingFlags.DeclaredOnly));
                type = type.BaseType;
            }

            fields.Sort(CompareFields);
            return fields;
        }

        private static int CompareFields(FieldInfo left, FieldInfo right)
        {
            var declaringTypeCompare = string.CompareOrdinal(
                left.DeclaringType.FullName,
                right.DeclaringType.FullName);

            if (declaringTypeCompare != 0)
            {
                return declaringTypeCompare;
            }

            return left.MetadataToken.CompareTo(right.MetadataToken);
        }

        private static bool IsNumeric(Type type)
        {
            return type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }

        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var builder = new StringBuilder(value.Length + 8);

            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];

                if (char.IsUpper(current))
                {
                    if (i > 0 && value[i - 1] != '_')
                    {
                        builder.Append('_');
                    }

                    builder.Append(char.ToLowerInvariant(current));
                    continue;
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private sealed class JsonWriter
        {
            private readonly StringBuilder builder = new StringBuilder();
            private readonly Stack<bool> hasValues = new Stack<bool>();
            private readonly bool prettyPrint;
            private int depth;

            public JsonWriter(bool prettyPrint, bool includeNullFields)
            {
                this.prettyPrint = prettyPrint;
                IncludeNullFields = includeNullFields;
            }

            public bool IncludeNullFields { get; }

            public void BeginObject()
            {
                builder.Append('{');
                hasValues.Push(false);
                depth++;
            }

            public void EndObject()
            {
                depth--;

                if (PopHasValues())
                {
                    WriteNewLineAndIndent();
                }

                builder.Append('}');
            }

            public void BeginArray()
            {
                builder.Append('[');
                hasValues.Push(false);
                depth++;
            }

            public void EndArray()
            {
                depth--;

                if (PopHasValues())
                {
                    WriteNewLineAndIndent();
                }

                builder.Append(']');
            }

            public void WritePropertyName(string name)
            {
                WriteValueSeparator();
                WriteNewLineAndIndent();
                WriteEscapedString(name);
                builder.Append(prettyPrint ? ": " : ":");
            }

            public void WriteArrayValue()
            {
                WriteValueSeparator();
                WriteNewLineAndIndent();
            }

            public void WriteString(string value)
            {
                if (value == null)
                {
                    WriteNull();
                    return;
                }

                WriteEscapedString(value);
            }

            public void WriteBool(bool value)
            {
                builder.Append(value ? "true" : "false");
            }

            public void WriteNumber(object value)
            {
                if (value is double doubleValue)
                {
                    WriteDouble(doubleValue);
                    return;
                }

                if (value is float floatValue)
                {
                    WriteDouble(floatValue);
                    return;
                }

                if (value is decimal decimalValue)
                {
                    builder.Append(decimalValue.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            }

            public void WriteNull()
            {
                builder.Append("null");
            }

            public override string ToString()
            {
                return builder.ToString();
            }

            private void WriteDouble(double value)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    builder.Append("null");
                    return;
                }

                builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
            }

            private void WriteValueSeparator()
            {
                var hasExistingValue = hasValues.Pop();

                if (hasExistingValue)
                {
                    builder.Append(',');
                }

                hasValues.Push(true);
            }

            private bool PopHasValues()
            {
                return hasValues.Pop();
            }

            private void WriteNewLineAndIndent()
            {
                if (!prettyPrint)
                {
                    return;
                }

                builder.AppendLine();

                for (var i = 0; i < depth; i++)
                {
                    builder.Append("  ");
                }
            }

            private void WriteEscapedString(string value)
            {
                builder.Append('"');

                for (var i = 0; i < value.Length; i++)
                {
                    var current = value[i];

                    switch (current)
                    {
                        case '"':
                            builder.Append("\\\"");
                            break;
                        case '\\':
                            builder.Append("\\\\");
                            break;
                        case '\b':
                            builder.Append("\\b");
                            break;
                        case '\f':
                            builder.Append("\\f");
                            break;
                        case '\n':
                            builder.Append("\\n");
                            break;
                        case '\r':
                            builder.Append("\\r");
                            break;
                        case '\t':
                            builder.Append("\\t");
                            break;
                        default:
                            if (current < ' ')
                            {
                                builder.Append("\\u");
                                builder.Append(((int)current).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                builder.Append(current);
                            }

                            break;
                    }
                }

                builder.Append('"');
            }
        }
    }
}
