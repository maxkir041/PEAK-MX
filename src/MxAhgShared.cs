using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace MxAhg.Shared
{
    internal static class Protocol
    {
        public const byte EventCode = 172;
        public const string Magic = "MX_AHG";
        public const int Version = 1;
        public const int CommandChallenge = 1;
        public const int CommandReport = 2;

        public const string AgentVersionKey = "mxahg_v";
        public const string ProtocolKey = "mxahg_p";
        public const string FileSignatureKey = "mxahg_s";
        public const string FileCountKey = "mxahg_c";
        public const string LoadedSignatureKey = "mxahg_l";
        public const string LoadedCountKey = "mxahg_lc";
        public const string HeartbeatKey = "mxahg_h";
        public const string SelfHashKey = "mxahg_i";

        public const int MaxFiles = 512;
        public const int MaxPlugins = 256;
        public const int MaxHarmonyOwners = 256;

        public static string Short(string value, int length = 16)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "?";
            return value.Length <= length ? value : value.Substring(0, length);
        }

        public static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return ToHex(sha.ComputeHash(stream));
        }

        public static string HashText(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? "")));
        }

        public static string ComputeProof(string nonce, Snapshot snapshot)
        {
            if (snapshot == null)
                return "";
            return HashText((nonce ?? "") + "|" + snapshot.FileSignature + "|" + snapshot.LoadedSignature + "|" + snapshot.SelfHash
                + "|" + (snapshot.Truncated ? "1" : "0") + "|" + (snapshot.Error ?? "") + "|" + snapshot.BuildCanonical());
        }

        public static string[] ToFlatFiles(IReadOnlyList<FileEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return Array.Empty<string>();
            var flat = new string[entries.Count * 4];
            for (int i = 0; i < entries.Count; i++)
            {
                int offset = i * 4;
                flat[offset] = entries[i].RelativePath ?? "";
                flat[offset + 1] = entries[i].Sha256 ?? "";
                flat[offset + 2] = entries[i].AssemblyName ?? "";
                flat[offset + 3] = entries[i].AssemblyVersion ?? "";
            }
            return flat;
        }

        public static string[] ToFlatPlugins(IReadOnlyList<PluginEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return Array.Empty<string>();
            var flat = new string[entries.Count * 5];
            for (int i = 0; i < entries.Count; i++)
            {
                int offset = i * 5;
                flat[offset] = entries[i].Guid ?? "";
                flat[offset + 1] = entries[i].Name ?? "";
                flat[offset + 2] = entries[i].Version ?? "";
                flat[offset + 3] = entries[i].RelativePath ?? "";
                flat[offset + 4] = entries[i].Sha256 ?? "";
            }
            return flat;
        }

        public static List<FileEntry> ParseFiles(object value)
        {
            string[] flat = ReadStringArray(value);
            var result = new List<FileEntry>();
            int count = Math.Min(flat.Length / 4, MaxFiles);
            for (int i = 0; i < count; i++)
            {
                int offset = i * 4;
                result.Add(new FileEntry
                {
                    RelativePath = Limit(flat[offset], 320),
                    Sha256 = Limit(flat[offset + 1], 64),
                    AssemblyName = Limit(flat[offset + 2], 160),
                    AssemblyVersion = Limit(flat[offset + 3], 80)
                });
            }
            return result;
        }

        public static List<PluginEntry> ParsePlugins(object value)
        {
            string[] flat = ReadStringArray(value);
            var result = new List<PluginEntry>();
            int count = Math.Min(flat.Length / 5, MaxPlugins);
            for (int i = 0; i < count; i++)
            {
                int offset = i * 5;
                result.Add(new PluginEntry
                {
                    Guid = Limit(flat[offset], 180),
                    Name = Limit(flat[offset + 1], 180),
                    Version = Limit(flat[offset + 2], 80),
                    RelativePath = Limit(flat[offset + 3], 320),
                    Sha256 = Limit(flat[offset + 4], 64)
                });
            }
            return result;
        }

        public static string[] ReadStringArray(object value)
        {
            if (value is string[] strings)
                return strings;
            if (value is object[] objects)
            {
                int count = Math.Min(objects.Length, MaxFiles * 5);
                var converted = new string[count];
                for (int i = 0; i < count; i++)
                    converted[i] = objects[i]?.ToString() ?? "";
                return converted;
            }
            return Array.Empty<string>();
        }

        public static string Limit(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }
    }

    internal sealed class FileEntry
    {
        public string RelativePath = "";
        public string Sha256 = "";
        public string AssemblyName = "";
        public string AssemblyVersion = "";
    }

    internal sealed class PluginEntry
    {
        public string Guid = "";
        public string Name = "";
        public string Version = "";
        public string RelativePath = "";
        public string Sha256 = "";
    }

    internal sealed class Snapshot
    {
        public readonly List<FileEntry> Files = new List<FileEntry>();
        public readonly List<PluginEntry> Plugins = new List<PluginEntry>();
        public readonly List<string> HarmonyOwners = new List<string>();
        public string FileSignature = "";
        public string LoadedSignature = "";
        public string SelfHash = "";
        public string Error = "";
        public bool Truncated;

        public void Recalculate()
        {
            Files.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
            Plugins.Sort((a, b) => string.Compare(a.Guid, b.Guid, StringComparison.OrdinalIgnoreCase));
            HarmonyOwners.Sort(StringComparer.OrdinalIgnoreCase);
            FileSignature = Protocol.HashText(BuildFileCanonical());
            LoadedSignature = Protocol.HashText(BuildLoadedCanonical());
        }

        public string BuildCanonical()
        {
            return BuildFileCanonical() + "\n--loaded--\n" + BuildLoadedCanonical();
        }

        private string BuildFileCanonical()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Files.Count; i++)
            {
                AppendField(sb, Files[i].RelativePath.ToLowerInvariant());
                AppendField(sb, Files[i].Sha256.ToLowerInvariant());
                AppendField(sb, Files[i].AssemblyName.ToLowerInvariant());
                AppendField(sb, Files[i].AssemblyVersion);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private string BuildLoadedCanonical()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Plugins.Count; i++)
            {
                AppendField(sb, Plugins[i].Guid.ToLowerInvariant());
                AppendField(sb, Plugins[i].Name.ToLowerInvariant());
                AppendField(sb, Plugins[i].Version);
                AppendField(sb, Plugins[i].RelativePath.ToLowerInvariant());
                AppendField(sb, Plugins[i].Sha256.ToLowerInvariant());
                sb.Append('\n');
            }
            sb.Append("owners\n");
            for (int i = 0; i < HarmonyOwners.Count; i++)
                AppendField(sb, HarmonyOwners[i].ToLowerInvariant());
            return sb.ToString();
        }

        private static void AppendField(StringBuilder sb, string value)
        {
            value = value ?? "";
            sb.Append(value.Length).Append(':').Append(value).Append('|');
        }
    }

    internal static class Scanner
    {
        public const string AgentGuid = "com.maxkir041.mxahg";
        public const string PeakMxGuid = "com.maxkir041.peakmx";

        public static Snapshot Scan(string selfAssemblyPath, bool excludePeakMx = false)
        {
            var snapshot = new Snapshot();
            var errors = new List<string>();
            string root = Paths.PluginPath;
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                root = Path.GetDirectoryName(selfAssemblyPath) ?? "";

            try
            {
                if (!string.IsNullOrWhiteSpace(selfAssemblyPath) && File.Exists(selfAssemblyPath))
                    snapshot.SelfHash = Protocol.HashFile(selfAssemblyPath);
            }
            catch (Exception e)
            {
                errors.Add("self: " + e.Message);
            }

            try
            {
                string[] files = Directory.Exists(root)
                    ? Directory.GetFiles(root, "*.dll", SearchOption.AllDirectories)
                    : Array.Empty<string>();
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < files.Length; i++)
                {
                    if (snapshot.Files.Count >= Protocol.MaxFiles)
                    {
                        snapshot.Truncated = true;
                        break;
                    }

                    string fileName = Path.GetFileName(files[i]);
                    if (ShouldExcludeFile(fileName, excludePeakMx))
                        continue;

                    try
                    {
                        AssemblyName assemblyName = null;
                        try { assemblyName = AssemblyName.GetAssemblyName(files[i]); } catch { }
                        snapshot.Files.Add(new FileEntry
                        {
                            RelativePath = RelativePath(root, files[i]),
                            Sha256 = Protocol.HashFile(files[i]),
                            AssemblyName = assemblyName?.Name ?? Path.GetFileNameWithoutExtension(files[i]),
                            AssemblyVersion = assemblyName?.Version?.ToString() ?? ""
                        });
                    }
                    catch (Exception e)
                    {
                        errors.Add(Path.GetFileName(files[i]) + ": " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                errors.Add("files: " + e.Message);
            }

            try
            {
                foreach (KeyValuePair<string, PluginInfo> pair in Chainloader.PluginInfos)
                {
                    if (snapshot.Plugins.Count >= Protocol.MaxPlugins)
                    {
                        snapshot.Truncated = true;
                        break;
                    }

                    PluginInfo info = pair.Value;
                    string guid = info?.Metadata?.GUID ?? pair.Key ?? "";
                    if (ShouldExcludePlugin(guid, excludePeakMx))
                        continue;

                    string location = info?.Location ?? "";
                    string hash = "";
                    try { if (File.Exists(location)) hash = Protocol.HashFile(location); } catch { }
                    snapshot.Plugins.Add(new PluginEntry
                    {
                        Guid = guid,
                        Name = info?.Metadata?.Name ?? "",
                        Version = info?.Metadata?.Version?.ToString() ?? "",
                        RelativePath = RelativePath(root, location),
                        Sha256 = hash
                    });
                }
            }
            catch (Exception e)
            {
                errors.Add("plugins: " + e.Message);
            }

            try
            {
                var owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (MethodBase method in Harmony.GetAllPatchedMethods())
                {
                    Patches patches = Harmony.GetPatchInfo(method);
                    if (patches?.Owners == null)
                        continue;
                    foreach (string owner in patches.Owners)
                    {
                        if (string.IsNullOrWhiteSpace(owner)
                            || ShouldExcludePlugin(owner, excludePeakMx)
                            || IsUnstableHarmonyOwner(owner))
                            continue;
                        owners.Add(owner);
                    }
                }
                foreach (string owner in owners)
                {
                    if (snapshot.HarmonyOwners.Count >= Protocol.MaxHarmonyOwners)
                    {
                        snapshot.Truncated = true;
                        break;
                    }
                    snapshot.HarmonyOwners.Add(owner);
                }
            }
            catch (Exception e)
            {
                errors.Add("harmony: " + e.Message);
            }

            snapshot.Error = string.Join("; ", errors.ToArray());
            snapshot.Recalculate();
            return snapshot;
        }

        public static bool IsTechnicalFile(string path)
        {
            string fileName = Path.GetFileName(path ?? "");
            return string.Equals(fileName, "MX-AHG.dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, "PEAK-MX.dll", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTechnicalPlugin(string guid)
        {
            return string.Equals(guid, AgentGuid, StringComparison.OrdinalIgnoreCase)
                || string.Equals(guid, PeakMxGuid, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsUnstableHarmonyOwner(string owner)
        {
            return !string.IsNullOrWhiteSpace(owner)
                && owner.StartsWith("harmony-auto-", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldExcludeFile(string fileName, bool excludePeakMx)
        {
            if (string.Equals(fileName, "MX-AHG.dll", StringComparison.OrdinalIgnoreCase))
                return true;
            return excludePeakMx
                && string.Equals(fileName, "PEAK-MX.dll", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldExcludePlugin(string guid, bool excludePeakMx)
        {
            if (string.Equals(guid, AgentGuid, StringComparison.OrdinalIgnoreCase))
                return true;
            return excludePeakMx
                && string.Equals(guid, PeakMxGuid, StringComparison.OrdinalIgnoreCase);
        }

        private static string RelativePath(string root, string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return "";
            try
            {
                Uri rootUri = new Uri(AppendSlash(Path.GetFullPath(root)));
                Uri fileUri = new Uri(Path.GetFullPath(file));
                return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
            }
            catch
            {
                return Path.GetFileName(file);
            }
        }

        private static string AppendSlash(string value)
        {
            if (value.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                || value.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                return value;
            return value + Path.DirectorySeparatorChar;
        }
    }
}
