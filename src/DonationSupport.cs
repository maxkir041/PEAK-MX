using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PeakMX
{
    public struct DonationGoal
    {
        public static readonly DonationGoal Empty = new DonationGoal();
        public string Title;
        public double Raised;
        public double? Target;
        public string Currency;
        public double? Percent;
    }

    public struct DonationSupporter
    {
        public string Name;
        public double Amount;
        public string Currency;
        public int Count;
        public string LastDonationAt;
    }

    public static class DonationSupport
    {
        private const string Endpoint = "https://peak-mx.rkngov.com/api/donations";
        private static readonly List<DonationSupporter> _supporters = new List<DonationSupporter>();
        private static readonly List<DonationSupporter> _latest = new List<DonationSupporter>();
        private static DateTime _lastRefreshUtc = DateTime.MinValue;
        private static string _lastLang;
        private static bool _loading;

        public static bool HasLoaded { get; private set; }
        public static bool IsLoading => _loading;
        public static string Warning { get; private set; }
        public static double MinPublicAmount { get; private set; } = 100d;
        public static int HiddenOlderThanDays { get; private set; } = 60;
        public static DonationGoal Goal { get; private set; } = DonationGoal.Empty;
        public static IReadOnlyList<DonationSupporter> Supporters => _supporters;
        public static IReadOnlyList<DonationSupporter> Latest => _latest;

        public static void Init()
        {
            Refresh();
        }

        public static void Refresh()
        {
            if (_loading)
                return;
            string lang = Localization.Current.ToString();
            if (HasLoaded && string.Equals(_lastLang, lang, StringComparison.Ordinal) && (DateTime.UtcNow - _lastRefreshUtc).TotalMinutes < 5)
                return;

            _loading = true;
            Task.Run(() =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using var client = new WebClient();
                    string body = client.DownloadString(Endpoint + "?lang=" + Uri.EscapeDataString(lang));
                    Parse(body);
                    Warning = ExtractString(body, "warning");
                    HasLoaded = true;
                    _lastLang = lang;
                    _lastRefreshUtc = DateTime.UtcNow;
                }
                catch (Exception e)
                {
                    Warning = e.Message;
                    Plugin.Log?.LogDebug($"[DonationSupport] {e.Message}");
                }
                finally
                {
                    _loading = false;
                }
            });
        }

        private static void Parse(string json)
        {
            Goal = new DonationGoal
            {
                Title = ExtractString(json, "title") ?? "PEAK-MX support",
                Raised = ExtractDouble(json, "raised") ?? 0d,
                Target = ExtractNullableDouble(json, "target"),
                Currency = ExtractString(json, "currency") ?? "RUB",
                Percent = ExtractNullableDouble(json, "percent"),
            };
            HiddenOlderThanDays = (int)(ExtractDouble(json, "hiddenOlderThanDays") ?? 60d);
            MinPublicAmount = ExtractDouble(json, "minPublicAmount") ?? 100d;

            var parsed = ParseDonationArray(json, "supporters", "lastDonationAt");
            var parsedLatest = ParseDonationArray(json, "latest", "createdAt");

            lock (_supporters)
            {
                _supporters.Clear();
                _supporters.AddRange(parsed);
                _latest.Clear();
                _latest.AddRange(parsedLatest);
            }
        }

        private static List<DonationSupporter> ParseDonationArray(string json, string arrayName, string dateField)
        {
            var parsed = new List<DonationSupporter>();
            var match = Regex.Match(json ?? "", $"\"{Regex.Escape(arrayName)}\"\\s*:\\s*\\[(?<items>.*?)\\]\\s*,", RegexOptions.Singleline);
            if (!match.Success)
                return parsed;

            foreach (Match item in Regex.Matches(match.Groups["items"].Value, "\\{(?<obj>.*?)\\}", RegexOptions.Singleline))
            {
                string obj = item.Groups["obj"].Value;
                string name = ExtractString(obj, "name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                parsed.Add(new DonationSupporter
                {
                    Name = name,
                    Amount = ExtractDouble(obj, "amount") ?? 0d,
                    Currency = ExtractString(obj, "currency") ?? Goal.Currency ?? "RUB",
                    Count = (int)(ExtractDouble(obj, "count") ?? 1d),
                    LastDonationAt = ExtractString(obj, dateField),
                });
            }

            return parsed;
        }

        private static double? ExtractNullableDouble(string json, string name)
        {
            var m = Regex.Match(json ?? "", $"\"{Regex.Escape(name)}\"\\s*:\\s*(null|-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
            if (!m.Success || string.Equals(m.Groups[1].Value, "null", StringComparison.OrdinalIgnoreCase))
                return null;
            return double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : null;
        }

        private static double? ExtractDouble(string json, string name) => ExtractNullableDouble(json, name);

        private static string ExtractString(string json, string name)
        {
            var m = Regex.Match(json ?? "", $"\"{Regex.Escape(name)}\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"])*)\"", RegexOptions.Singleline);
            return m.Success ? Regex.Unescape(m.Groups["v"].Value) : null;
        }
    }
}
