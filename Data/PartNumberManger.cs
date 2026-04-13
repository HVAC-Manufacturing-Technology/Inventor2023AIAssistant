using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Inventor;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace Inventor2023AIAssistant
{
    public class PartNumberManager
    {
        private Inventor.Application _inventorApplication;
        private string _dataFile;

        public PartNumberManager(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;

            // Store part number registry next to
            // chat history
            string folder = IOPath.Combine(
                System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder
                        .MyDocuments),
                "InventorAI");

            if (!IODirectory.Exists(folder))
                IODirectory.CreateDirectory(folder);

            _dataFile = IOPath.Combine(
                folder, "PartNumbers.txt");
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandlePartNumber(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("next part number") ||
                n.Contains("assign next") ||
                n.Contains("next available") ||
                n.Contains("auto number") ||
                n.Contains("auto part number"))
                return GetNextPartNumber(prompt);

            if (n.Contains("last used part number") ||
                n.Contains("last part number") ||
                n.Contains("current part number series"))
                return ShowLastUsedNumbers();

            if (n.Contains("assign part number") &&
                (n.Contains("next") ||
                 n.Contains("auto")))
                return AssignNextPartNumber(prompt);

            if (n.Contains("list part numbers") ||
                n.Contains("show part numbers") ||
                n.Contains("part number registry") ||
                n.Contains("part number list"))
                return ShowPartNumberRegistry();

            if (n.Contains("reset part number") ||
                n.Contains("reset series"))
                return ResetSeries(prompt);

            if (n.Contains("open part number file") ||
                n.Contains("open part number folder"))
                return OpenPartNumberFolder();

            return null;
        }

        // ─── Get next part number ─────────────────────────────────────

        private string GetNextPartNumber(string prompt)
        {
            try
            {
                string prefix =
                    ExtractPrefix(prompt);

                if (string.IsNullOrWhiteSpace(prefix))
                    return
                        "Please specify a prefix." +
                        System.Environment.NewLine +
                        "Examples:" +
                        System.Environment.NewLine +
                        "  'next part number FPB'" +
                        System.Environment.NewLine +
                        "  'next part number SAV'" +
                        System.Environment.NewLine +
                        "  'next part number PANEL'";

                int next = GetNextNumber(prefix);
                string partNumber =
                    FormatPartNumber(prefix, next);

                var sb = new StringBuilder();
                sb.AppendLine("── Next Part Number ──");
                sb.AppendLine(
                    "Prefix: " + prefix);
                sb.AppendLine(
                    "Next number: " + partNumber);
                sb.AppendLine();
                sb.AppendLine(
                    "Say 'assign part number " +
                    partNumber + "' to apply it to " +
                    "the active document.");
                sb.AppendLine(
                    "Or say 'assign next part number " +
                    prefix + "' to get and assign " +
                    "in one step.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get next part number: " +
                       ex.Message;
            }
        }

        // ─── Assign next part number ──────────────────────────────────

        private string AssignNextPartNumber(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                string prefix = ExtractPrefix(prompt);

                if (string.IsNullOrWhiteSpace(prefix))
                    return
                        "Specify a prefix. Example:" +
                        System.Environment.NewLine +
                        "'assign next part number FPB'";

                int next = GetNextNumber(prefix);
                string partNumber =
                    FormatPartNumber(prefix, next);

                // Set on active document
                try
                {
                    doc.PropertySets[
                        "{32853F0F-3444-11D1-" +
                        "9E93-0060B03C1CA6}"][
                        "Part Number"].Value = partNumber;
                }
                catch (Exception ex)
                {
                    return
                        "Could not set part number: " +
                        ex.Message;
                }

                // Record in registry
                RecordPartNumber(
                    prefix, next, partNumber,
                    doc.DisplayName);

                return
                    "✅ Part number assigned: " +
                    partNumber +
                    System.Environment.NewLine +
                    "Document: " + doc.DisplayName +
                    System.Environment.NewLine +
                    "Registry updated." +
                    System.Environment.NewLine +
                    "Next in series will be: " +
                    FormatPartNumber(prefix, next + 1);
            }
            catch (Exception ex)
            {
                return "Failed to assign part number: " +
                       ex.Message;
            }
        }

        // ─── Show last used numbers ───────────────────────────────────

        private string ShowLastUsedNumbers()
        {
            try
            {
                var registry = LoadRegistry();

                if (registry.Count == 0)
                    return
                        "No part numbers have been " +
                        "assigned yet." +
                        System.Environment.NewLine +
                        "Say 'assign next part number " +
                        "FPB' to start a series.";

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Last Used Part Numbers ──");
                sb.AppendLine(new string('-', 40));

                foreach (var kvp in registry)
                {
                    sb.AppendLine(
                        "Prefix: " + kvp.Key);
                    sb.AppendLine(
                        "  Last: " +
                        FormatPartNumber(
                            kvp.Key, kvp.Value));
                    sb.AppendLine(
                        "  Next: " +
                        FormatPartNumber(
                            kvp.Key, kvp.Value + 1));
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read registry: " +
                       ex.Message;
            }
        }

        // ─── Show full registry ───────────────────────────────────────

        private string ShowPartNumberRegistry()
        {
            try
            {
                if (!IOFile.Exists(_dataFile))
                    return
                        "Part number registry is empty." +
                        System.Environment.NewLine +
                        "Say 'assign next part number " +
                        "FPB' to start tracking.";

                string[] lines =
                    IOFile.ReadAllLines(_dataFile);

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Part Number Registry ──");
                sb.AppendLine(
                    "File: " + _dataFile);
                sb.AppendLine(new string('-', 50));

                foreach (string line in lines)
                    sb.AppendLine(line);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read registry: " +
                       ex.Message;
            }
        }

        // ─── Reset series ─────────────────────────────────────────────

        private string ResetSeries(string prompt)
        {
            try
            {
                string prefix = ExtractPrefix(prompt);

                if (string.IsNullOrWhiteSpace(prefix))
                    return
                        "Specify a prefix to reset. " +
                        "Example:" +
                        System.Environment.NewLine +
                        "'reset part number series FPB'";

                // Extract starting number if provided
                int startNum = 1;
                Match m = Regex.Match(
                    prompt, @"from\s+(\d+)");
                if (m.Success)
                    startNum = int.Parse(
                        m.Groups[1].Value) - 1;

                var registry = LoadRegistry();
                registry[prefix.ToUpper()] = startNum;
                SaveRegistry(registry);

                return
                    "✅ Series reset." +
                    System.Environment.NewLine +
                    "Prefix: " + prefix.ToUpper() +
                    System.Environment.NewLine +
                    "Next number will be: " +
                    FormatPartNumber(
                        prefix.ToUpper(), startNum + 1);
            }
            catch (Exception ex)
            {
                return "Failed to reset series: " +
                       ex.Message;
            }
        }

        // ─── Open folder ──────────────────────────────────────────────

        private string OpenPartNumberFolder()
        {
            try
            {
                string folder =
                    IOPath.GetDirectoryName(_dataFile);

                System.Diagnostics.Process.Start(folder);

                return
                    "✅ Opened part number folder:" +
                    System.Environment.NewLine + folder;
            }
            catch (Exception ex)
            {
                return "Failed to open folder: " + ex.Message;
            }
        }

        // ─── Registry helpers ─────────────────────────────────────────

        private int GetNextNumber(string prefix)
        {
            var registry = LoadRegistry();
            string key = prefix.ToUpper();

            if (!registry.ContainsKey(key))
                return 1;

            return registry[key] + 1;
        }

        private void RecordPartNumber(
            string prefix,
            int number,
            string partNumber,
            string docName)
        {
            try
            {
                var registry = LoadRegistry();
                string key = prefix.ToUpper();
                registry[key] = number;
                SaveRegistry(registry);

                // Also append to log
                string logLine =
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm") +
                    " | " + partNumber +
                    " | " + docName;

                IOFile.AppendAllText(
                     _dataFile,
                    logLine +
                    System.Environment.NewLine);
            }
            catch { }
        }

        private Dictionary<string, int> LoadRegistry()
        {
            var dict = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

            try
            {
                string registryFile =
                    _dataFile.Replace(
                        "PartNumbers.txt",
                        "PartNumberRegistry.txt");

                if (!IOFile.Exists(registryFile))
                    return dict;

                foreach (string line in
                    IOFile.ReadAllLines(registryFile))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2 &&
                        int.TryParse(
                            parts[1].Trim(),
                            out int num))
                        dict[parts[0].Trim()] = num;
                }
            }
            catch { }

            return dict;
        }

        private void SaveRegistry(
            Dictionary<string, int> registry)
        {
            try
            {
                string registryFile =
                    _dataFile.Replace(
                        "PartNumbers.txt",
                        "PartNumberRegistry.txt");

                var lines = new List<string>();
                foreach (var kvp in registry)
                    lines.Add(kvp.Key + " = " + kvp.Value);

                IOFile.WriteAllLines(registryFile, lines);
            }
            catch { }
        }

        private string FormatPartNumber(
            string prefix, int number)
        {
            // Format: FPB-001, SAV-042, PANEL-007
            return prefix.ToUpper() + "-" +
                   number.ToString("D3");
        }

        private string ExtractPrefix(string prompt)
        {
            try
            {
                string n = prompt.Trim();

                // Look for known HVAC prefixes first
                string[] knownPrefixes = new string[]
                {
                    "FPB", "SAV", "ERU", "DDU",
                    "PANEL", "BRACKET", "FLANGE",
                    "DUCT", "COIL", "FAN", "DAMPER",
                    "PLENUM", "VAV", "AHU", "FCU"
                };

                string upper = n.ToUpperInvariant();
                foreach (string p in knownPrefixes)
                {
                    if (upper.Contains(p))
                        return p;
                }

                // Look for uppercase word after
                // "number", "for", "prefix"
                Match m = Regex.Match(
                    n,
                    @"(?:number|for|prefix|series)" +
                    @"\s+([A-Za-z]{2,10})");
                if (m.Success)
                    return m.Groups[1].Value.ToUpper();

                // Last word that looks like a prefix
                m = Regex.Match(
                    n, @"\b([A-Za-z]{2,10})\s*$");
                if (m.Success)
                {
                    string candidate =
                        m.Groups[1].Value.ToUpper();

                    // Skip common words
                    string[] skip = new string[]
                    {
                        "THE", "FOR", "AND", "NEXT",
                        "PART", "NUMBER", "ASSIGN",
                        "AUTO", "SERIES", "GET"
                    };

                    bool isSkip = false;
                    foreach (string s in skip)
                        if (candidate == s)
                        {
                            isSkip = true;
                            break;
                        }

                    if (!isSkip) return candidate;
                }

                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}