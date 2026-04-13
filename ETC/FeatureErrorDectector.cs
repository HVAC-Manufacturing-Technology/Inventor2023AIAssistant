using System;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class FeatureErrorDetector
    {
        private Inventor.Application _inventorApplication;

        public FeatureErrorDetector(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        public string TryHandleFeatureErrors(
            string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("feature error") ||
                n.Contains("show error") ||
                n.Contains("failed feature") ||
                n.Contains("why is this feature") ||
                n.Contains("what caused this error") ||
                n.Contains("model error") ||
                n.Contains("broken feature") ||
                n.Contains("features with error"))
                return FindFeatureErrors();

            if (n.Contains("feature health") ||
                n.Contains("model health") ||
                n.Contains("model tree") ||
                n.Contains("feature status"))
                return GetFeatureHealthReport();

            if (n.Contains("suppressed feature") ||
                n.Contains("find suppressed feature"))
                return FindSuppressedFeatures();

            if (n.Contains("feature warning") ||
                n.Contains("show warning"))
                return FindFeatureWarnings();

            if (n.Contains("list features") ||
                n.Contains("all features") ||
                n.Contains("feature list"))
                return ListAllFeatures();

            return null;
        }

        private string FindFeatureErrors()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Feature error detection " +
                           "requires a Part document.";

                PartDocument part = (PartDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Feature Error Report ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('═', 40));

                int errorCount = 0;
                int warningCount = 0;
                int suppressedCount = 0;
                int healthyCount = 0;
                int total = 0;

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        total++;

                        if (feature.Suppressed)
                        {
                            suppressedCount++;
                            continue;
                        }

                        int health =
                            (int)feature.HealthStatus;

                        // 0 = up to date / healthy
                        // 1 = error
                        // 2 = suppressed
                        // other = warning

                        if (health == 0)
                        {
                            healthyCount++;
                        }
                        else if (health == 1)
                        {
                            errorCount++;
                            sb.AppendLine(
                                "❌ ERROR: " +
                                feature.Name);
                            sb.AppendLine(
                                "   Fix: Right-click in " +
                                "model tree > Edit Feature");
                            sb.AppendLine();
                        }
                        else
                        {
                            warningCount++;
                            sb.AppendLine(
                                "⚠️ WARNING: " +
                                feature.Name +
                                " (status=" + health + ")");
                        }
                    }
                    catch { }
                }

                sb.AppendLine(new string('═', 40));
                sb.AppendLine("Summary:");
                sb.AppendLine(
                    "  Total:      " + total);
                sb.AppendLine(
                    "  ✅ Healthy:  " + healthyCount);
                sb.AppendLine(
                    "  ❌ Errors:   " + errorCount);
                sb.AppendLine(
                    "  ⚠️ Warnings: " + warningCount);
                sb.AppendLine(
                    "  ○ Suppressed: " + suppressedCount);
                sb.AppendLine();

                if (errorCount == 0 && warningCount == 0)
                    sb.AppendLine(
                        "✅ All active features are healthy.");
                else if (errorCount > 0)
                    sb.AppendLine(
                        "❌ " + errorCount +
                        " feature(s) need attention.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to check feature errors: " +
                       ex.Message;
            }
        }

        private string GetFeatureHealthReport()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Feature health requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Feature Health Report ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 50));
                sb.AppendLine(
                    "Name".PadRight(28) +
                    "Status".PadRight(16) +
                    "Type");
                sb.AppendLine(new string('-', 50));

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        string status;
                        string icon;

                        if (feature.Suppressed)
                        {
                            status = "Suppressed";
                            icon = "○";
                        }
                        else
                        {
                            int h =
                                (int)feature.HealthStatus;

                            if (h == 0)
                            {
                                status = "Healthy";
                                icon = "✅";
                            }
                            else if (h == 1)
                            {
                                status = "Error";
                                icon = "❌";
                            }
                            else
                            {
                                status = "Warning";
                                icon = "⚠️";
                            }
                        }

                        string typeName = "";
                        try
                        {
                            typeName =
                                feature.Type.ToString()
                                    .Replace(
                                        "kFeatureType",
                                        "")
                                    .Replace(
                                        "Feature", "");
                        }
                        catch { }

                        sb.AppendLine(
                            icon + " " +
                            feature.Name.PadRight(26) +
                            status.PadRight(16) +
                            typeName);
                    }
                    catch { }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get health report: " +
                       ex.Message;
            }
        }

        private string FindSuppressedFeatures()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Requires a Part document.";

                PartDocument part = (PartDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Suppressed Features ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                int count = 0;

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed)
                        {
                            sb.AppendLine(
                                "○ " + feature.Name);
                            count++;
                        }
                    }
                    catch { }
                }

                sb.AppendLine(new string('-', 40));

                if (count == 0)
                    sb.AppendLine(
                        "✅ No suppressed features.");
                else
                    sb.AppendLine(
                        "Suppressed: " + count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to find suppressed: " +
                       ex.Message;
            }
        }

        private string FindFeatureWarnings()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Requires a Part document.";

                PartDocument part = (PartDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine("── Feature Warnings ──");

                int count = 0;

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed) continue;

                        int h = (int)feature.HealthStatus;

                        if (h != 0)
                        {
                            string label =
                                h == 1 ? "ERROR" : "WARNING";
                            sb.AppendLine(
                                "⚠️ " + feature.Name +
                                " — " + label +
                                " (code " + h + ")");
                            count++;
                        }
                    }
                    catch { }
                }

                if (count == 0)
                    sb.AppendLine(
                        "✅ No feature warnings.");
                else
                    sb.AppendLine(
                        System.Environment.NewLine +
                        "Total: " + count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to find warnings: " +
                       ex.Message;
            }
        }

        private string ListAllFeatures()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Requires a Part document.";

                PartDocument part = (PartDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine("── Feature List ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                int i = 1;
                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        string s = feature.Suppressed
                            ? " (suppressed)"
                            : (int)feature.HealthStatus == 1
                                ? " ❌"
                                : "";

                        sb.AppendLine(
                            i + ". " +
                            feature.Name + s);
                        i++;
                    }
                    catch { }
                }

                sb.AppendLine(new string('-', 40));
                sb.AppendLine(
                    "Total: " + (i - 1));

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list features: " +
                       ex.Message;
            }
        }
    }
}