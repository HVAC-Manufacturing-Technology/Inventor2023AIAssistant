using System;
using System.Collections.Generic;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class VaultSearchHandler
    {
        private Inventor.Application _inventorApplication;

        public VaultSearchHandler(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        public string TryHandleVaultSearch(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (!n.Contains("vault") &&
                !n.Contains("search vault") &&
                !n.Contains("find in vault") &&
                !n.Contains("checked out") &&
                !n.Contains("check in") &&
                !n.Contains("check out"))
                return null;

            if (n.Contains("checked out") ||
                n.Contains("check out"))
                return GetCheckedOutFiles();

            if (n.Contains("recent") ||
                n.Contains("modified this week") ||
                n.Contains("modified today"))
                return GetRecentVaultFiles();

            if (n.Contains("find") ||
                n.Contains("search"))
                return SearchVault(prompt);

            if (n.Contains("status"))
                return GetVaultStatus();

            return GetVaultStatus();
        }

        private string GetVaultStatus()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("── Vault Connection ──");

                bool vaultFound = false;
                string vaultVersion = "";

                foreach (ApplicationAddIn addIn in
                    _inventorApplication.ApplicationAddIns)
                {
                    try
                    {
                        string name =
                            addIn.DisplayName ?? "";

                        if (name.ToLower().Contains("vault"))
                        {
                            vaultFound = true;
                            vaultVersion = name;
                            break;
                        }
                    }
                    catch { }
                }

                if (vaultFound)
                {
                    sb.AppendLine(
                        "✅ Vault add-in found: " +
                        vaultVersion);
                    sb.AppendLine();
                    sb.AppendLine("Vault search commands:");
                    sb.AppendLine(
                        "- 'search vault for [part number]'");
                    sb.AppendLine(
                        "- 'show checked out files'");
                    sb.AppendLine(
                        "- 'show recent vault files'");
                    sb.AppendLine(
                        "- 'open vault explorer'");
                }
                else
                {
                    sb.AppendLine(
                        "⚠️ Vault add-in not detected.");
                    sb.AppendLine(
                        "Make sure Vault Professional " +
                        "is installed and Inventor is " +
                        "logged into Vault.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to check Vault status: " +
                       ex.Message;
            }
        }

        private string SearchVault(string prompt)
        {
            try
            {
                string searchTerm =
                    ExtractSearchTerm(prompt);

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Vault Search: " +
                    searchTerm + " ──");
                sb.AppendLine();
                sb.AppendLine(
                    "Opening Vault Explorer to search " +
                    "for: " + searchTerm);

                LaunchVaultExplorer();

                sb.AppendLine(
                    "✅ Vault Explorer launched.");
                sb.AppendLine(
                    "Use the search bar to find: " +
                    searchTerm);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to search Vault: " +
                       ex.Message;
            }
        }

        private string GetCheckedOutFiles()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("── Checked Out Files ──");
                sb.AppendLine();

                int checkedOut = 0;

                foreach (Document doc in
                    _inventorApplication.Documents)
                {
                    try
                    {
                        string path = doc.FullFileName;

                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            sb.AppendLine(
                                "📄 " + doc.DisplayName);
                            sb.AppendLine("   " + path);
                            checkedOut++;
                        }
                    }
                    catch { }
                }

                if (checkedOut == 0)
                {
                    sb.AppendLine(
                        "No files currently open " +
                        "in Inventor.");
                    sb.AppendLine(
                        "Open Vault Explorer to see all " +
                        "checked out files.");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine(
                        "Total open: " + checkedOut);
                    sb.AppendLine(
                        "Open Vault Explorer to see " +
                        "full checkout status.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get checked out files: " +
                       ex.Message;
            }
        }

        private string GetRecentVaultFiles()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("── Recent Files ──");
                sb.AppendLine();

                var recentDocs = new List<string>();

                // Get open documents
                foreach (Document d in
                    _inventorApplication.Documents)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(
                                d.FullFileName))
                            recentDocs.Add(d.FullFileName);
                    }
                    catch { }
                }

                // Get recent files from registry
                try
                {
                    string regPath =
                        @"Software\Autodesk\Inventor\" +
                        @"RegistryVersion22.0\RecentFiles";

                    using (var key =
                        Microsoft.Win32.Registry.CurrentUser
                            .OpenSubKey(regPath))
                    {
                        if (key != null)
                        {
                            foreach (string name in
                                key.GetValueNames())
                            {
                                string val =
                                    key.GetValue(name)
                                    as string;

                                if (!string.IsNullOrWhiteSpace(
                                        val) &&
                                    !recentDocs.Contains(val))
                                    recentDocs.Add(val);
                            }
                        }
                    }
                }
                catch { }

                if (recentDocs.Count == 0)
                {
                    sb.AppendLine("No recent files found.");
                    sb.AppendLine(
                        "Open Vault Explorer to browse " +
                        "recent Vault activity.");
                }
                else
                {
                    int count = Math.Min(
                        recentDocs.Count, 15);

                    for (int i = 0; i < count; i++)
                    {
                        sb.AppendLine(
                            (i + 1) + ". " +
                            System.IO.Path.GetFileName(
                                recentDocs[i]));
                        sb.AppendLine(
                            "   " + recentDocs[i]);
                    }

                    sb.AppendLine();
                    sb.AppendLine(
                        "Showing " + count +
                        " recent files.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get recent files: " +
                       ex.Message;
            }
        }

        public string LaunchVaultExplorer()
        {
            try
            {
                string[] paths = new string[]
                {
                    @"C:\Program Files\Autodesk\" +
                    @"Vault Professional 2023\Explorer\" +
                    @"Connectivity.VaultPro.exe",
                    @"C:\Program Files\Autodesk\" +
                    @"Vault Professional 2024\Explorer\" +
                    @"Connectivity.VaultPro.exe",
                    @"C:\Program Files\Autodesk\" +
                    @"Vault Professional 2025\Explorer\" +
                    @"Connectivity.VaultPro.exe",
                    @"C:\Program Files (x86)\Autodesk\" +
                    @"Vault Professional 2023\Explorer\" +
                    @"Connectivity.VaultPro.exe",
                };

                foreach (string path in paths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        System.Diagnostics.Process
                            .Start(path);
                        return "✅ Vault Explorer launched.";
                    }
                }

                try
                {
                    System.Diagnostics.Process
                        .Start("VaultPro.exe");
                    return "✅ Vault Explorer launched.";
                }
                catch { }

                return
                    "Could not find Vault Explorer. " +
                    "Launch it manually from the " +
                    "Start menu.";
            }
            catch (Exception ex)
            {
                return
                    "Failed to launch Vault Explorer: " +
                    ex.Message;
            }
        }

        private string ExtractSearchTerm(string prompt)
        {
            string lower = prompt.ToLowerInvariant();

            string[] keywords = new string[]
            {
                "search vault for ",
                "find in vault ",
                "search for ",
                "vault find ",
                "find ",
                "search ",
            };

            foreach (string kw in keywords)
            {
                int idx = lower.IndexOf(kw);

                if (idx >= 0)
                {
                    string term =
                        prompt.Substring(
                            idx + kw.Length).Trim();

                    if (!string.IsNullOrWhiteSpace(term))
                        return term;
                }
            }

            return prompt.Trim();
        }
    }
}