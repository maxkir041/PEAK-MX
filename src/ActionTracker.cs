using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PeakMX
{
    /// <summary>
    /// Sends important one-shot gameplay actions to the telemetry backend.
    /// Unlike <see cref="CheatTracker"/>, this is for explicit user-triggered operations
    /// such as inventory edits, teleports, world changes, and achievement actions.
    /// </summary>
    public static class ActionTracker
    {
        private const string EventUrl = "https://peak-mx.rkngov.com/api/event";

        public static void Track(string name, double? value = null, Dictionary<string, object> meta = null)
        {
            if (!ModConfig.AllowAnonymousStats.Value)
                return;

            string id = ModConfig.InstallId.Value;
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                return;

            Task.Run(() =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using var c = new WebClient { Encoding = Encoding.UTF8 };
                    c.Headers[HttpRequestHeader.ContentType] = "application/json";
                    c.UploadString(EventUrl, "POST", BuildJson(id, name, value, meta));
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogDebug($"[ActionTracker] {name}: {e.Message}");
                }
            });
        }

        private static string BuildJson(string id, string name, double? value, Dictionary<string, object> meta)
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            WriteProp(sb, "id", id); sb.Append(',');
            WriteProp(sb, "type", "action"); sb.Append(',');
            WriteProp(sb, "name", name); sb.Append(',');
            WriteProp(sb, "t", TelemetryToken.Value);

            if (value.HasValue)
            {
                sb.Append(',');
                sb.Append("\"value\":").Append(value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (meta != null && meta.Count > 0)
            {
                sb.Append(',');
                sb.Append("\"meta\":");
                WriteObject(sb, meta);
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static void WriteObject(StringBuilder sb, Dictionary<string, object> values)
        {
            sb.Append('{');
            bool first = true;
            foreach (var kv in values)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
                    continue;

                if (!first) sb.Append(',');
                first = false;
                WritePropName(sb, kv.Key);
                WriteValue(sb, kv.Value);
            }
            sb.Append('}');
        }

        private static void WriteValue(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    break;
                case string s:
                    WriteString(sb, s);
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case int i:
                    sb.Append(i);
                    break;
                case long l:
                    sb.Append(l);
                    break;
                case float f:
                    sb.Append(f.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case double d:
                    sb.Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case Dictionary<string, object> obj:
                    WriteObject(sb, obj);
                    break;
                default:
                    WriteString(sb, value.ToString());
                    break;
            }
        }

        private static void WriteProp(StringBuilder sb, string key, string value)
        {
            WritePropName(sb, key);
            WriteString(sb, value);
        }

        private static void WritePropName(StringBuilder sb, string key)
        {
            WriteString(sb, key);
            sb.Append(':');
        }

        private static void WriteString(StringBuilder sb, string value)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('"');
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
