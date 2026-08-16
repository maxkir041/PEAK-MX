using System;
using System.Collections.Generic;
#if !DISABLE_FEEDBACK_FILE_ATTACHMENTS
using System.Diagnostics;
#endif
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PeakMX
{
    public struct FeedbackTicket
    {
        public string Code;
        public string Type;
        public string Status;
        public string Title;
        public string Message;
        public string AdminReply;
        public int CommentsCount;
        public string LatestCommentAt;
        public string UpdatedAt;
    }

    public struct FeedbackAttachment
    {
        public string Name;
        public string ContentType;
        public byte[] Data;
    }

    public static class FeedbackClient
    {
        private const string Endpoint = "https://peak-mx.rkngov.com/api/feedback";
        private const string CommentEndpoint = "https://peak-mx.rkngov.com/api/feedback/comment";
        private const string CloseEndpoint = "https://peak-mx.rkngov.com/api/feedback/close";
        private const string ClientToken = "peak-mx-public-v1";
        private const int MaxAttachmentBytes = 330 * 1024;
        private static readonly List<FeedbackTicket> _tickets = new List<FeedbackTicket>();
        private static System.Reflection.MethodInfo _loadImageMethod;
        private static System.Reflection.MethodInfo _encodeJpgMethod;
#if !DISABLE_FEEDBACK_FILE_ATTACHMENTS
        private static readonly object _pickerLock = new object();
        private static bool _pickerRunning;
        private static bool _pickerCompleted;
        private static Process _pickerProcess;
        private static string _pickerResultPath;
        private static string _pickerScriptPath;
        private static string[] _pickedPaths;
        private static string _pickerError;
#endif

        public static bool IsSending { get; private set; }
        public static bool IsCommenting { get; private set; }
        public static bool IsClosing { get; private set; }
        public static bool IsCheckingReplies { get; private set; }
        public static string LastTicketCode { get; private set; }
        public static string LastStatus { get; private set; }
        public static string LastCommentStatus { get; private set; }
        public static string LastCloseStatus { get; private set; }
        public static string LastError { get; private set; }
        public static IReadOnlyList<FeedbackTicket> Tickets
        {
            get
            {
                lock (_tickets)
                    return _tickets.ToArray();
            }
        }
#if !DISABLE_FEEDBACK_FILE_ATTACHMENTS
        public static bool IsPickingFiles
        {
            get
            {
                lock (_pickerLock)
                    return _pickerRunning;
            }
        }
#endif

        public static void SubmitAsync(string type, string title, string message, string contact, byte[] screenshotJpeg = null)
        {
            SubmitAsync(type, title, message, contact, AttachmentListFromScreenshot(screenshotJpeg));
        }

        public static void SubmitAsync(string type, string title, string message, string contact, IReadOnlyList<FeedbackAttachment> attachments)
        {
            if (IsSending)
                return;

            type = NormalizeType(type);
            title = Clip(title, 120);
            message = Clip(message, 3000);
            contact = Clip(contact, 160);

            if (string.IsNullOrWhiteSpace(message))
            {
                LastError = "empty_message";
                return;
            }

            IsSending = true;
            LastError = "";
            LastStatus = "sending";

            Task.Run(() =>
            {
                try
                {
                    string response = PostJson(Endpoint, BuildPayload(type, title, message, contact));
                    LastTicketCode = ExtractString(response, "ticketCode") ?? ExtractString(response, "ticket_code") ?? "";
                    if (attachments != null && attachments.Count > 0 && !string.IsNullOrWhiteSpace(LastTicketCode))
                    {
                        string comment = Localization.Current == Lang.Russian
                            ? "Изображения к первому сообщению."
                            : "Images attached to the first message.";
                        PostCommentAttachments(LastTicketCode, comment, attachments);
                    }
                    LastStatus = "sent";
                    CheckRepliesNow();
                }
                catch (Exception e)
                {
                    LastError = e.Message;
                    LastStatus = "";
                    Plugin.Log?.LogDebug($"[Feedback] submit failed: {e.Message}");
                }
                finally
                {
                    IsSending = false;
                }
            });
        }

        public static void AddCommentAsync(string ticketCode, string message, byte[] screenshotJpeg = null)
        {
            AddCommentAsync(ticketCode, message, AttachmentListFromScreenshot(screenshotJpeg));
        }

        public static void AddCommentAsync(string ticketCode, string message, IReadOnlyList<FeedbackAttachment> attachments)
        {
            if (IsCommenting)
                return;

            ticketCode = Clip(ticketCode, 40);
            message = Clip(message, 3000);
            if (string.IsNullOrWhiteSpace(ticketCode))
            {
                LastError = "missing_ticket";
                return;
            }
            if (string.IsNullOrWhiteSpace(message) && (attachments == null || attachments.Count <= 0))
            {
                LastError = "empty_comment";
                return;
            }

            IsCommenting = true;
            LastError = "";
            LastCommentStatus = "sending";
            Task.Run(() =>
            {
                try
                {
                    PostCommentAttachments(ticketCode, message, attachments);
                    LastCommentStatus = "sent";
                    CheckRepliesNow();
                }
                catch (Exception e)
                {
                    LastError = e.Message;
                    LastCommentStatus = "";
                    Plugin.Log?.LogDebug($"[Feedback] comment failed: {e.Message}");
                }
                finally
                {
                    IsCommenting = false;
                }
            });
        }

        public static void CloseTicketAsync(string ticketCode)
        {
            if (IsClosing)
                return;

            ticketCode = Clip(ticketCode, 40);
            if (string.IsNullOrWhiteSpace(ticketCode))
            {
                LastError = "missing_ticket";
                return;
            }

            IsClosing = true;
            LastError = "";
            LastCloseStatus = "closing";
            Task.Run(() =>
            {
                try
                {
                    PostJson(CloseEndpoint, BuildClosePayload(ticketCode));
                    LastCloseStatus = "closed";
                    CheckRepliesNow();
                }
                catch (Exception e)
                {
                    LastError = e.Message;
                    LastCloseStatus = "";
                    Plugin.Log?.LogDebug($"[Feedback] close failed: {e.Message}");
                }
                finally
                {
                    IsClosing = false;
                }
            });
        }

#if !DISABLE_FEEDBACK_FILE_ATTACHMENTS
        public static void PickImageFilesAsync()
        {
            lock (_pickerLock)
            {
                if (_pickerRunning)
                    return;

                _pickerRunning = true;
                _pickerCompleted = false;
                _pickerProcess = null;
                _pickerResultPath = null;
                _pickerScriptPath = null;
                _pickedPaths = null;
                _pickerError = "";
            }

            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "PEAK-MX");
                Directory.CreateDirectory(tempDir);
                string id = Guid.NewGuid().ToString("N");
                string resultPath = Path.Combine(tempDir, "feedback-picker-" + id + ".txt");
                string scriptPath = Path.Combine(tempDir, "feedback-picker-" + id + ".ps1");
                File.WriteAllText(scriptPath, BuildExternalPickerScript(resultPath), new UTF8Encoding(false));

                var start = new ProcessStartInfo
                {
                    FileName = PowerShellExe(),
                    Arguments = "-NoLogo -NoProfile -ExecutionPolicy Bypass -STA -File \"" + scriptPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = tempDir,
                };

                Process process = Process.Start(start);
                if (process == null)
                    throw new InvalidOperationException("file_picker_not_started");

                lock (_pickerLock)
                {
                    _pickerProcess = process;
                    _pickerResultPath = resultPath;
                    _pickerScriptPath = scriptPath;
                }
            }
            catch (Exception e)
            {
                lock (_pickerLock)
                {
                    _pickedPaths = null;
                    _pickerError = e.Message;
                    _pickerRunning = false;
                    _pickerCompleted = true;
                }
            }
        }

        public static void CancelPickImageFiles()
        {
            Process process = null;
            string resultPath = null;
            string scriptPath = null;
            lock (_pickerLock)
            {
                if (!_pickerRunning)
                    return;

                process = _pickerProcess;
                resultPath = _pickerResultPath;
                scriptPath = _pickerScriptPath;
                _pickerProcess = null;
                _pickerResultPath = null;
                _pickerScriptPath = null;
                _pickedPaths = null;
                _pickerError = "";
                _pickerRunning = false;
                _pickerCompleted = false;
            }

            try
            {
                if (process != null && !process.HasExited)
                    process.Kill();
            }
            catch
            {
            }
            finally
            {
                process?.Dispose();
                DeleteQuiet(resultPath);
                DeleteQuiet(scriptPath);
            }
        }

        private static string PowerShellExe()
        {
            try
            {
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string path = Path.Combine(winDir, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                if (File.Exists(path))
                    return path;
            }
            catch
            {
            }
            return "powershell.exe";
        }

        private static string BuildExternalPickerScript(string resultPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("$ErrorActionPreference = 'Stop'");
            sb.Append("$out = ").AppendLine(PowerShellQuote(resultPath));
            sb.AppendLine("$enc = New-Object System.Text.UTF8Encoding -ArgumentList $false");
            sb.AppendLine("try {");
            sb.AppendLine("  Add-Type -AssemblyName System.Windows.Forms");
            sb.AppendLine("  Add-Type -AssemblyName System.Drawing");
            sb.AppendLine("  [System.Windows.Forms.Application]::EnableVisualStyles()");
            sb.AppendLine("  $form = New-Object System.Windows.Forms.Form");
            sb.AppendLine("  $form.Text = 'PEAK-MX - choose images'");
            sb.AppendLine("  $form.TopMost = $true");
            sb.AppendLine("  $form.ShowInTaskbar = $true");
            sb.AppendLine("  $form.StartPosition = 'CenterScreen'");
            sb.AppendLine("  $form.FormBorderStyle = 'FixedToolWindow'");
            sb.AppendLine("  $form.Width = 430");
            sb.AppendLine("  $form.Height = 120");
            sb.AppendLine("  $label = New-Object System.Windows.Forms.Label");
            sb.AppendLine("  $label.Dock = 'Fill'");
            sb.AppendLine("  $label.TextAlign = 'MiddleCenter'");
            sb.AppendLine("  $label.Text = 'PEAK-MX: choose one or more images.'");
            sb.AppendLine("  $form.Controls.Add($label)");
            sb.AppendLine("  $dialog = New-Object System.Windows.Forms.OpenFileDialog");
            sb.AppendLine("  $dialog.Title = 'PEAK-MX'");
            sb.AppendLine("  $dialog.Filter = 'Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*'");
            sb.AppendLine("  $dialog.Multiselect = $true");
            sb.AppendLine("  $dialog.CheckFileExists = $true");
            sb.AppendLine("  $dialog.CheckPathExists = $true");
            sb.AppendLine("  $timer = New-Object System.Windows.Forms.Timer");
            sb.AppendLine("  $timer.Interval = 300");
            sb.AppendLine("  $timer.Add_Tick({ $form.TopMost = $true; $form.Activate(); $form.BringToFront() })");
            sb.AppendLine("  $timer.Start()");
            sb.AppendLine("  $form.Add_Shown({ $form.Activate(); $form.BringToFront() })");
            sb.AppendLine("  $null = $form.Show()");
            sb.AppendLine("  [System.Windows.Forms.Application]::DoEvents()");
            sb.AppendLine("  $result = $dialog.ShowDialog($form)");
            sb.AppendLine("  $timer.Stop()");
            sb.AppendLine("  if ($result -eq [System.Windows.Forms.DialogResult]::OK) {");
            sb.AppendLine("    $lines = @()");
            sb.AppendLine("    foreach ($file in $dialog.FileNames) {");
            sb.AppendLine("      $lines += [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($file))");
            sb.AppendLine("    }");
            sb.AppendLine("    [System.IO.File]::WriteAllLines($out, [string[]]$lines, $enc)");
            sb.AppendLine("  } else {");
            sb.AppendLine("    [System.IO.File]::WriteAllText($out, '', $enc)");
            sb.AppendLine("  }");
            sb.AppendLine("  $form.Close()");
            sb.AppendLine("  exit 0");
            sb.AppendLine("} catch {");
            sb.AppendLine("  [System.IO.File]::WriteAllText($out, ('ERROR:' + $_.Exception.Message), $enc)");
            sb.AppendLine("  exit 2");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string PowerShellQuote(string value)
        {
            return "'" + (value ?? "").Replace("'", "''") + "'";
        }

        private static void FinishExternalPickerLocked()
        {
            var paths = new List<string>();
            string error = "";
            try
            {
                int exitCode = _pickerProcess != null && _pickerProcess.HasExited ? _pickerProcess.ExitCode : 0;
                if (!string.IsNullOrWhiteSpace(_pickerResultPath) && File.Exists(_pickerResultPath))
                {
                    foreach (string rawLine in File.ReadAllLines(_pickerResultPath, Encoding.UTF8))
                    {
                        string line = (rawLine ?? "").Trim().Trim('\uFEFF');
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                        if (line.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
                        {
                            error = line.Substring(6);
                            break;
                        }

                        try
                        {
                            paths.Add(Encoding.UTF8.GetString(Convert.FromBase64String(line)));
                        }
                        catch
                        {
                            paths.Add(line);
                        }
                    }
                }
                else if (exitCode != 0)
                {
                    error = "file_picker_failed_" + exitCode;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            _pickedPaths = paths.ToArray();
            _pickerError = error;
            _pickerRunning = false;
            _pickerCompleted = true;
        }

        public static List<FeedbackAttachment> TakePickedImageFiles(out string error)
        {
            string[] paths;
            Process process = null;
            string resultPath = null;
            string scriptPath = null;
            lock (_pickerLock)
            {
                if (!_pickerCompleted && _pickerRunning && _pickerProcess != null)
                {
                    try
                    {
                        if (_pickerProcess.HasExited)
                            FinishExternalPickerLocked();
                    }
                    catch (Exception e)
                    {
                        _pickedPaths = null;
                        _pickerError = e.Message;
                        _pickerRunning = false;
                        _pickerCompleted = true;
                    }
                }

                if (!_pickerCompleted)
                {
                    error = "";
                    return null;
                }

                paths = _pickedPaths;
                error = _pickerError ?? "";
                process = _pickerProcess;
                resultPath = _pickerResultPath;
                scriptPath = _pickerScriptPath;
                _pickerProcess = null;
                _pickerResultPath = null;
                _pickerScriptPath = null;
                _pickedPaths = null;
                _pickerError = "";
                _pickerCompleted = false;
            }

            process?.Dispose();
            DeleteQuiet(resultPath);
            DeleteQuiet(scriptPath);

            if (!string.IsNullOrWhiteSpace(error))
                return new List<FeedbackAttachment>();
            return AttachmentsFromPaths(paths, out error);
        }

        private static void DeleteQuiet(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

#endif

        public static byte[] CaptureScreenshotJpeg(out string error)
        {
            error = "";
            Texture2D source = null;
            Texture2D scaled = null;
            RenderTexture rt = null;
            RenderTexture old = null;
            try
            {
                source = ScreenCapture.CaptureScreenshotAsTexture();
                if (source == null)
                {
                    error = "screenshot_failed";
                    return null;
                }

                return EncodeTextureToJpeg(source, out error);
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
            finally
            {
                if (rt != null)
                {
                    RenderTexture.active = old;
                    RenderTexture.ReleaseTemporary(rt);
                }
                if (scaled != null)
                    UnityEngine.Object.Destroy(scaled);
                if (source != null)
                    UnityEngine.Object.Destroy(source);
            }
        }

        public static void CheckRepliesAsync()
        {
            if (IsCheckingReplies)
                return;

            IsCheckingReplies = true;
            LastError = "";
            Task.Run(() =>
            {
                try
                {
                    CheckRepliesNow();
                }
                catch (Exception e)
                {
                    LastError = e.Message;
                    Plugin.Log?.LogDebug($"[Feedback] replies failed: {e.Message}");
                }
                finally
                {
                    IsCheckingReplies = false;
                }
            });
        }

        private static void CheckRepliesNow()
        {
            string id = ClientIdentity.StableId;
            if (string.IsNullOrWhiteSpace(id))
                return;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string url = Endpoint
                + "/replies?id=" + Uri.EscapeDataString(id)
                + "&t=" + Uri.EscapeDataString(ClientToken);

            using var client = CreateClient();
            string json = client.DownloadString(url);
            var parsed = ParseTickets(json);
            lock (_tickets)
            {
                _tickets.Clear();
                _tickets.AddRange(parsed);
            }
        }

        private static string BuildPayload(string type, string title, string message, string contact)
        {
            string nick = SafeStr(() => Photon.Pun.PhotonNetwork.NickName);
            string gameVersion = SafeStr(() => Application.version);
            string screen = SafeStr(() => Screen.currentResolution.width + "x" + Screen.currentResolution.height);

            var sb = new StringBuilder("{");
            WriteProp(sb, "t", ClientToken); Comma(sb);
            WriteProp(sb, "id", ClientIdentity.StableId); Comma(sb);
            WriteProp(sb, "steamId", ClientIdentity.SteamId); Comma(sb);
            WriteProp(sb, "idKind", ClientIdentity.UsesSteam ? "steam" : "anon"); Comma(sb);
            WriteProp(sb, "type", type); Comma(sb);
            WriteProp(sb, "title", title); Comma(sb);
            WriteProp(sb, "message", message); Comma(sb);
            WriteProp(sb, "contact", contact); Comma(sb);
            WriteProp(sb, "mod", Plugin.Version); Comma(sb);
            WriteProp(sb, "lang", Localization.Current.ToString()); Comma(sb);
            WriteProp(sb, "nick", nick); Comma(sb);
            WriteProp(sb, "os", SystemInfo.operatingSystem); Comma(sb);
            WriteProp(sb, "gameVer", gameVersion); Comma(sb);
            WriteProp(sb, "screen", screen);
            sb.Append('}');
            return sb.ToString();
        }

        private static string BuildClosePayload(string ticketCode)
        {
            string nick = SafeStr(() => Photon.Pun.PhotonNetwork.NickName);
            var sb = new StringBuilder("{");
            WriteProp(sb, "t", ClientToken); Comma(sb);
            WriteProp(sb, "id", ClientIdentity.StableId); Comma(sb);
            WriteProp(sb, "steamId", ClientIdentity.SteamId); Comma(sb);
            WriteProp(sb, "ticketCode", ticketCode); Comma(sb);
            WriteProp(sb, "mod", Plugin.Version); Comma(sb);
            WriteProp(sb, "lang", Localization.Current.ToString()); Comma(sb);
            WriteProp(sb, "nick", nick);
            sb.Append('}');
            return sb.ToString();
        }

        private static string BuildCommentPayload(string ticketCode, string message, FeedbackAttachment attachment)
        {
            string nick = SafeStr(() => Photon.Pun.PhotonNetwork.NickName);
            var sb = new StringBuilder("{");
            WriteProp(sb, "t", ClientToken); Comma(sb);
            WriteProp(sb, "id", ClientIdentity.StableId); Comma(sb);
            WriteProp(sb, "steamId", ClientIdentity.SteamId); Comma(sb);
            WriteProp(sb, "ticketCode", ticketCode); Comma(sb);
            WriteProp(sb, "message", message); Comma(sb);
            WriteProp(sb, "mod", Plugin.Version); Comma(sb);
            WriteProp(sb, "lang", Localization.Current.ToString()); Comma(sb);
            WriteProp(sb, "nick", nick);

            if (attachment.Data != null && attachment.Data.Length > 0)
            {
                Comma(sb);
                WriteProp(sb, "attachmentName", attachment.Name); Comma(sb);
                WriteProp(sb, "attachmentType", attachment.ContentType); Comma(sb);
                WriteProp(sb, "attachmentBase64", Convert.ToBase64String(attachment.Data));
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static void PostCommentAttachments(string ticketCode, string message, IReadOnlyList<FeedbackAttachment> attachments)
        {
            if (attachments == null || attachments.Count <= 0)
            {
                PostJson(CommentEndpoint, BuildCommentPayload(ticketCode, message, default));
                return;
            }

            for (int i = 0; i < attachments.Count; i++)
            {
                string text = i == 0 ? message : "";
                if (string.IsNullOrWhiteSpace(text))
                    text = Localization.Current == Lang.Russian
                        ? $"Изображение {i + 1}/{attachments.Count}"
                        : $"Image {i + 1}/{attachments.Count}";
                PostJson(CommentEndpoint, BuildCommentPayload(ticketCode, text, attachments[i]));
            }
        }

        private static List<FeedbackAttachment> AttachmentListFromScreenshot(byte[] screenshotJpeg)
        {
            var list = new List<FeedbackAttachment>();
            if (screenshotJpeg != null && screenshotJpeg.Length > 0)
            {
                list.Add(new FeedbackAttachment
                {
                    Name = "peak-mx-screenshot.jpg",
                    ContentType = "image/jpeg",
                    Data = screenshotJpeg,
                });
            }
            return list;
        }

        private static List<FeedbackAttachment> AttachmentsFromPaths(string[] paths, out string error)
        {
            var result = new List<FeedbackAttachment>();
            var errors = new List<string>();
            error = "";

            if (paths == null || paths.Length == 0)
                return result;

            for (int i = 0; i < paths.Length && result.Count < 6; i++)
            {
                string path = paths[i];
                FeedbackAttachment attachment = AttachmentFromFile(path, out string itemError);
                if (attachment.Data != null && attachment.Data.Length > 0)
                    result.Add(attachment);
                else if (!string.IsNullOrWhiteSpace(itemError))
                    errors.Add(Path.GetFileName(path) + ": " + itemError);
            }

            if (paths.Length > 6)
                errors.Add("max_6_images");
            error = errors.Count > 0 ? string.Join("; ", errors.ToArray()) : "";
            return result;
        }

        private static FeedbackAttachment AttachmentFromFile(string path, out string error)
        {
            error = "";
            Texture2D source = null;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    error = "file_not_found";
                    return default;
                }

                var info = new FileInfo(path);
                if (info.Length > 24 * 1024 * 1024)
                {
                    error = "file_too_large";
                    return default;
                }

                byte[] bytes = File.ReadAllBytes(path);
                source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!LoadImage(source, bytes))
                {
                    error = "unsupported_image";
                    return default;
                }

                byte[] jpeg = EncodeTextureToJpeg(source, out error);
                if (jpeg == null || jpeg.Length <= 0)
                    return default;

                string baseName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(baseName))
                    baseName = "image";
                return new FeedbackAttachment
                {
                    Name = Clip(baseName, 80) + ".jpg",
                    ContentType = "image/jpeg",
                    Data = jpeg,
                };
            }
            catch (Exception e)
            {
                error = e.Message;
                return default;
            }
            finally
            {
                if (source != null)
                    UnityEngine.Object.Destroy(source);
            }
        }

        private static byte[] EncodeTextureToJpeg(Texture2D source, out string error)
        {
            error = "";
            Texture2D scaled = null;
            RenderTexture rt = null;
            RenderTexture old = null;
            try
            {
                int[] widths = { 960, 800, 640, 520, 420 };
                int[] qualities = { 68, 60, 52, 44, 36, 30 };
                byte[] best = null;
                foreach (int maxWidth in widths)
                {
                    int w = source.width;
                    int h = source.height;
                    if (w > maxWidth)
                    {
                        float scale = maxWidth / (float)w;
                        w = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
                        h = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
                    }

                    old = RenderTexture.active;
                    rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(source, rt);
                    RenderTexture.active = rt;
                    scaled = new Texture2D(w, h, TextureFormat.RGB24, false);
                    scaled.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    scaled.Apply(false, false);

                    foreach (int quality in qualities)
                    {
                        byte[] jpg = EncodeToJpg(scaled, quality);
                        if (jpg != null && jpg.Length > 0)
                        {
                            best = jpg;
                            if (jpg.Length <= MaxAttachmentBytes)
                                return jpg;
                        }
                    }

                    UnityEngine.Object.Destroy(scaled);
                    scaled = null;
                    RenderTexture.active = old;
                    RenderTexture.ReleaseTemporary(rt);
                    rt = null;
                }

                error = best == null ? "image_encode_failed" : "image_too_large";
                return null;
            }
            finally
            {
                if (rt != null)
                {
                    RenderTexture.active = old;
                    RenderTexture.ReleaseTemporary(rt);
                }
                if (scaled != null)
                    UnityEngine.Object.Destroy(scaled);
            }
        }

        private static bool LoadImage(Texture2D texture, byte[] data)
        {
            try
            {
                if (_loadImageMethod == null)
                {
                    var type = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                    _loadImageMethod = type?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
                }
                return _loadImageMethod != null && (bool)_loadImageMethod.Invoke(null, new object[] { texture, data });
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[Feedback] LoadImage: {e.Message}");
                return false;
            }
        }

        private static byte[] EncodeToJpg(Texture2D texture, int quality)
        {
            try
            {
                if (_encodeJpgMethod == null)
                {
                    var type = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                    _encodeJpgMethod = type?.GetMethod("EncodeToJPG", new[] { typeof(Texture2D), typeof(int) });
                }
                return _encodeJpgMethod == null
                    ? null
                    : (byte[])_encodeJpgMethod.Invoke(null, new object[] { texture, quality });
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[Feedback] EncodeToJPG: {e.Message}");
                return null;
            }
        }

        private static string PostJson(string url, string body)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using var client = CreateClient();
            client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=utf-8";
            return client.UploadString(url, "POST", body);
        }

        private static WebClient CreateClient()
        {
            var client = new WebClient { Encoding = Encoding.UTF8 };
            client.Headers[HttpRequestHeader.UserAgent] = "PEAK-MX/" + Plugin.Version;
            return client;
        }

        private static List<FeedbackTicket> ParseTickets(string json)
        {
            var result = new List<FeedbackTicket>();
            foreach (Match match in Regex.Matches(json ?? "", "\\{[^{}]*\"ticketCode\"[^{}]*\\}", RegexOptions.Singleline))
            {
                string obj = match.Value;
                result.Add(new FeedbackTicket
                {
                    Code = ExtractString(obj, "ticketCode") ?? "",
                    Type = ExtractString(obj, "type") ?? "",
                    Status = ExtractString(obj, "status") ?? "",
                    Title = ExtractString(obj, "title") ?? "",
                    Message = ExtractString(obj, "message") ?? "",
                    AdminReply = ExtractString(obj, "adminReply") ?? "",
                    CommentsCount = ExtractInt(obj, "commentsCount"),
                    LatestCommentAt = ExtractString(obj, "latestCommentAt") ?? "",
                    UpdatedAt = ExtractString(obj, "updatedAt") ?? "",
                });
            }
            return result;
        }

        private static string ExtractString(string json, string name)
        {
            var match = Regex.Match(json ?? "",
                "\"" + Regex.Escape(name) + "\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"])*)\"",
                RegexOptions.Singleline);
            return match.Success ? Regex.Unescape(match.Groups["v"].Value).Replace("\\/", "/") : null;
        }

        private static int ExtractInt(string json, string name)
        {
            var match = Regex.Match(json ?? "",
                "\"" + Regex.Escape(name) + "\"\\s*:\\s*(?<v>-?\\d+)",
                RegexOptions.Singleline);
            return match.Success && int.TryParse(match.Groups["v"].Value, out int value) ? value : 0;
        }

        private static string NormalizeType(string value)
        {
            string type = (value ?? "").Trim().ToLowerInvariant();
            return type == "bug" || type == "suggestion" || type == "other" ? type : "suggestion";
        }

        private static string Clip(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            string text = value.Trim();
            return text.Length <= max ? text : text.Substring(0, max);
        }

        private static string SafeStr(Func<string> read)
        {
            try { return read(); }
            catch { return null; }
        }

        private static void Comma(StringBuilder sb) => sb.Append(',');
        private static void WriteProp(StringBuilder sb, string key, string value) { sb.Append('"').Append(key).Append("\":"); WriteString(sb, value); }
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
