#if !THUNDERSTORE_NO_ANALYTICS
namespace PeakMX
{
    internal static class TelemetryToken
    {
        // Public client key shared by every release build.
        // This is intentionally not secret so analytics works for all users.
        internal const string Value = "peak-mx-public-v1";
    }
}
#endif
