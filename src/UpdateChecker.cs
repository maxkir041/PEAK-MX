using System;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace PeakMX
{
    // Public release metadata only; no identifiers or game data.
    public static class UpdateChecker
    {
        private static volatile bool _checking;
        private static volatile Result _result = new Result();
        public static bool IsChecking => _checking;
        public static bool HasChecked => _result.Checked;
        public static bool UpdateAvailable => _result.Newer;
        public static string LatestVersion => _result.Version;
        public static string Error => _result.Error;
        private sealed class Result
        {
            public bool Checked, Newer;
            public string Version, Error;
        }
        [DataContract]
        private sealed class Release
        {
            [DataMember(Name = "tag_name")] public string Tag { get; set; }
            [DataMember(Name = "draft")] public bool Draft { get; set; }
            [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
        }
        public static void Init() => CheckAsync();
        public static void CheckAsync(bool force = false)
        {
            if (_checking || (!force && HasChecked)) return;
            _checking = true;
            Task.Run(() =>
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/maxkir041/PEAK-MX/releases/latest");
                    request.UserAgent = "PEAK-MX-release-check";
                    request.Accept = "application/vnd.github+json";
                    request.Timeout = 10000;
                    request.ReadWriteTimeout = 10000;
                    request.AllowAutoRedirect = false;
                    using (var response = request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    {
                        var release = (Release)new DataContractJsonSerializer(typeof(Release)).ReadObject(stream);
                        Version latest;
                        if (release == null || release.Draft || release.Prerelease ||
                            !Version.TryParse((release.Tag ?? "").TrimStart('v', 'V'), out latest))
                            throw new InvalidOperationException("Invalid release version.");
                        _result = new Result { Checked = true, Version = latest.ToString(),
                            Newer = latest > new Version(Plugin.Version) };
                    }
                }
                catch (Exception) { _result = new Result { Error = "GitHub release check unavailable." }; }
                finally { _checking = false; }
            });
        }
    }
}
