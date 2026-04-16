using System;
using System.IO;
using System.Text;

namespace Inventor2023AIAssistant
{
    public class ChatHistoryManager
    {
        private string _historyFolder;
        private string _currentSessionFile;
        private bool _initialized = false;

        public ChatHistoryManager()
        {
            try
            {
                _historyFolder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),
                    "InventorAI",
                    "ChatHistory");

                if (!Directory.Exists(_historyFolder))
                    Directory.CreateDirectory(_historyFolder);

                string timestamp =
                    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

                _currentSessionFile = Path.Combine(
                    _historyFolder,
                    "session_" + timestamp + ".txt");

                // Write session header
                File.WriteAllText(
                    _currentSessionFile,
                    "═══════════════════════════════════════" +
                    System.Environment.NewLine +
                    "Inventor AI Assistant — Chat Session" +
                    System.Environment.NewLine +
                    "Started: " +
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss") +
                    System.Environment.NewLine +
                    "═══════════════════════════════════════" +
                    System.Environment.NewLine +
                    System.Environment.NewLine);

                _initialized = true;
            }
            catch { }
        }

        // ─── Log a message ────────────────────────────────────────────

        public void LogUserMessage(string prompt)
        {
            if (!_initialized) return;

            try
            {
                string line =
                    "[" + DateTime.Now.ToString("HH:mm:ss") +
                    "] You: " + prompt +
                    System.Environment.NewLine;

                File.AppendAllText(
                    _currentSessionFile, line);
            }
            catch { }
        }

        public void LogAssistantMessage(string response)
        {
            if (!_initialized) return;

            try
            {
                string line =
                    "[" + DateTime.Now.ToString("HH:mm:ss") +
                    "] Assistant: " + response +
                    System.Environment.NewLine +
                    System.Environment.NewLine;

                File.AppendAllText(
                    _currentSessionFile, line);
            }
            catch { }
        }

        public void LogNewChat()
        {
            if (!_initialized) return;

            try
            {
                string line =
                    System.Environment.NewLine +
                    "─── New Chat ───────────────────────────" +
                    System.Environment.NewLine +
                    System.Environment.NewLine;

                File.AppendAllText(
                    _currentSessionFile, line);
            }
            catch { }
        }

        // ─── Open history folder ──────────────────────────────────────

        public string OpenHistoryFolder()
        {
            try
            {
                if (!Directory.Exists(_historyFolder))
                    return "History folder not found: " +
                           _historyFolder;

                System.Diagnostics.Process.Start(
                    "explorer.exe", _historyFolder);

                return "Opening chat history folder:" +
                       System.Environment.NewLine +
                       _historyFolder;
            }
            catch (Exception ex)
            {
                return "Failed to open history folder: " +
                       ex.Message;
            }
        }

        // ─── List sessions ────────────────────────────────────────────

        public string ListRecentSessions()
        {
            try
            {
                if (!Directory.Exists(_historyFolder))
                    return "No chat history found.";

                string[] files =
                    Directory.GetFiles(
                        _historyFolder, "*.txt");

                if (files.Length == 0)
                    return "No chat sessions found.";

                Array.Sort(files);
                Array.Reverse(files);

                var sb = new StringBuilder();
                sb.AppendLine("Recent chat sessions:");
                sb.AppendLine(new string('-', 40));

                int count = Math.Min(files.Length, 10);

                for (int i = 0; i < count; i++)
                {
                    string name =
                        Path.GetFileNameWithoutExtension(
                            files[i]);
                    long size =
                        new FileInfo(files[i]).Length;

                    sb.AppendLine(
                        (i + 1) + ". " + name +
                        " (" + size + " bytes)");
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Location: " + _historyFolder);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list sessions: " + ex.Message;
            }
        }

        // ─── Get current session path ─────────────────────────────────

        public string GetCurrentSessionPath()
        {
            return _currentSessionFile ?? "Not initialized";
        }
    }
}