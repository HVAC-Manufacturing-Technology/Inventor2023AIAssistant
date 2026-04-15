using System;
using System.Collections.Generic;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class AssemblyHealthCheck
    {
        private Inventor.Application _inventorApplication;

        public AssemblyHealthCheck(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleHealthCheck(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("assembly health") ||
                n.Contains("health check") ||
                n.Contains("assembly check") ||
                n.Contains("check assembly"))
                return RunHealthCheck();

            if (n.Contains("suppressed component") ||
                n.Contains("find suppressed"))
                return FindSuppressedComponents();

            if (n.Contains("hidden component") ||
                n.Contains("find hidden"))
                return FindHiddenComponents();

            if (n.Contains("missing part number") ||
                n.Contains("no part number") ||
                n.Contains("empty part number"))
                return FindMissingPartNumbers();

            if (n.Contains("missing material") ||
                n.Contains("no material"))
                return FindMissingMaterials();

            if (n.Contains("duplicate part number"))
                return FindDuplicatePartNumbers();

            if (n.Contains("list all open") ||
                n.Contains("open documents"))
                return ListOpenDocuments();

            return null;
        }

        // ─── Full health check ────────────────────────────────────────

        public string RunHealthCheck()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                var sb = new StringBuilder();

                if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    sb.AppendLine("── Assembly Health Check ──");
                    sb.AppendLine("Assembly: " + doc.DisplayName);
                    sb.AppendLine(new string('═', 40));

                    AssemblyDocument asm = (AssemblyDocument)doc;

                    int total =
                        asm.ComponentDefinition.Occurrences.Count;

                    sb.AppendLine("Total components: " + total);
                    sb.AppendLine();

                    var suppressed = new List<string>();
                    var hidden = new List<string>();
                    var noPartNumber = new List<string>();
                    var noMaterial = new List<string>();
                    var partNumbers = new Dictionary<string,
                        List<string>>(
                        StringComparer.OrdinalIgnoreCase);

                    ScanOccurrences(
                        asm.ComponentDefinition.Occurrences,
                        suppressed, hidden,
                        noPartNumber, noMaterial,
                        partNumbers);

                    sb.AppendLine(
                        "Suppressed components: " + suppressed.Count);
                    if (suppressed.Count > 0)
                        foreach (string s in suppressed)
                            sb.AppendLine("  ⚠️ " + s);

                    sb.AppendLine(
                        "Hidden components: " + hidden.Count);
                    if (hidden.Count > 0)
                        foreach (string s in hidden)
                            sb.AppendLine("  ℹ️ " + s);

                    sb.AppendLine(
                        "Missing part numbers: " + noPartNumber.Count);
                    if (noPartNumber.Count > 0)
                        foreach (string s in noPartNumber)
                            sb.AppendLine("  ❌ " + s);

                    sb.AppendLine(
                        "Missing/generic material: " + noMaterial.Count);
                    if (noMaterial.Count > 0)
                        foreach (string s in noMaterial)
                            sb.AppendLine("  ⚠️ " + s);

                    int dupCount = 0;
                    sb.AppendLine();
                    sb.AppendLine("Duplicate part numbers:");
                    foreach (var kvp in partNumbers)
                    {
                        if (kvp.Value.Count > 1)
                        {
                            sb.AppendLine(
                                "  ⚠️ " + kvp.Key +
                                " used by " + kvp.Value.Count +
                                " components");
                            dupCount++;
                        }
                    }
                    if (dupCount == 0)
                        sb.AppendLine("  ✅ No duplicates found");

                    int totalIssues =
                        noPartNumber.Count + suppressed.Count;
                    int totalWarnings =
                        noMaterial.Count + hidden.Count + dupCount;

                    sb.AppendLine();
                    sb.AppendLine(new string('═', 40));
                    sb.AppendLine("Summary:");
                    sb.AppendLine("  ❌ Issues:   " + totalIssues);
                    sb.AppendLine("  ⚠️ Warnings: " + totalWarnings);

                    if (totalIssues == 0 && totalWarnings == 0)
                        sb.AppendLine("  ✅ Assembly is healthy.");
                    else if (totalIssues == 0)
                        sb.AppendLine(
                            "  ⚠️ Review warnings before release.");
                    else
                        sb.AppendLine(
                            "  ❌ Fix issues before release.");
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    var validator =
                        new ParameterValidator(_inventorApplication);
                    return validator.ValidateParameters();
                }
                else
                {
                    return "Health check requires a " +
                           "Part or Assembly document.";
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to run health check: " + ex.Message;
            }
        }

        // ─── Scan occurrences ─────────────────────────────────────────

        private void ScanOccurrences(
            ComponentOccurrences occurrences,
            List<string> suppressed,
            List<string> hidden,
            List<string> noPartNumber,
            List<string> noMaterial,
            Dictionary<string, List<string>> partNumbers)
        {
            foreach (ComponentOccurrence occ in occurrences)
            {
                try
                {
                    string name = occ.Name;

                    if (occ.Suppressed)
                        suppressed.Add(name);

                    if (!occ.Visible)
                        hidden.Add(name);

                    Document refDoc =
                        occ.Definition.Document as Document;

                    if (refDoc == null) continue;

                    string pn = "";
                    try
                    {
                        pn = Convert.ToString(
                            refDoc.PropertySets[
                                "{32853F0F-3444-11D1-" +
                                "9E93-0060B03C1CA6}"][
                                "Part Number"].Value);
                    }
                    catch { }

                    if (string.IsNullOrWhiteSpace(pn))
                        noPartNumber.Add(name);
                    else
                    {
                        if (!partNumbers.ContainsKey(pn))
                            partNumbers[pn] = new List<string>();
                        partNumbers[pn].Add(name);
                    }

                    if (refDoc.DocumentType ==
                        DocumentTypeEnum.kPartDocumentObject)
                    {
                        try
                        {
                            string mat =
                                ((PartDocument)refDoc)
                                    .ComponentDefinition
                                    .Material.Name;

                            if (string.IsNullOrWhiteSpace(mat) ||
                                mat.ToLower() == "generic" ||
                                mat.ToLower() == "default")
                                noMaterial.Add(name);
                        }
                        catch
                        {
                            noMaterial.Add(name);
                        }
                    }

                    if (refDoc.DocumentType ==
                        DocumentTypeEnum.kAssemblyDocumentObject)
                    {
                        ScanOccurrences(
                            ((AssemblyDocument)refDoc)
                                .ComponentDefinition.Occurrences,
                            suppressed, hidden,
                            noPartNumber, noMaterial,
                            partNumbers);
                    }
                }
                catch { }
            }
        }

        // ─── Individual checks ────────────────────────────────────────

        private string FindSuppressedComponents()
        {
            try
            {
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Requires an Assembly document.";

                AssemblyDocument asm = (AssemblyDocument)doc;
                var sb = new StringBuilder();
                sb.AppendLine("── Suppressed Components ──");

                int count = 0;
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Suppressed)
                        {
                            sb.AppendLine("⚠️ " + occ.Name);
                            count++;
                        }
                    }
                    catch { }
                }

                if (count == 0)
                    sb.AppendLine("✅ No suppressed components.");
                else
                    sb.AppendLine(
                        System.Environment.NewLine +
                        "Total suppressed: " + count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to find suppressed: " + ex.Message;
            }
        }

        private string FindHiddenComponents()
        {
            try
            {
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Requires an Assembly document.";

                AssemblyDocument asm = (AssemblyDocument)doc;
                var sb = new StringBuilder();
                sb.AppendLine("── Hidden Components ──");

                int count = 0;
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (!occ.Visible)
                        {
                            sb.AppendLine("ℹ️ " + occ.Name);
                            count++;
                        }
                    }
                    catch { }
                }

                if (count == 0)
                    sb.AppendLine("✅ No hidden components.");
                else
                    sb.AppendLine(
                        System.Environment.NewLine +
                        "Total hidden: " + count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to find hidden: " + ex.Message;
            }
        }

        private string FindMissingPartNumbers()
        {
            try
            {
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Requires an Assembly document.";

                AssemblyDocument asm = (AssemblyDocument)doc;
                var sb = new StringBuilder();
                sb.AppendLine("── Missing Part Numbers ──");

                int count = 0;
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        Document refDoc =
                            occ.Definition.Document as Document;

                        if (refDoc == null) continue;

                        string pn = Convert.ToString(
                            refDoc.PropertySets[
                                "{32853F0F-3444-11D1-" +
                                "9E93-0060B03C1CA6}"][
                                "Part Number"].Value);

                        if (string.IsNullOrWhiteSpace(pn))
                        {
                            sb.AppendLine("❌ " + occ.Name);
                            count++;
                        }
                    }
                    catch
                    {
                        sb.AppendLine(
                            "❌ " + occ.Name + " (could not read)");
                        count++;
                    }
                }

                if (count == 0)
                    sb.AppendLine("✅ All components have part numbers.");
                else
                    sb.AppendLine(
                        System.Environment.NewLine + "Missing: " + count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to check part numbers: " + ex.Message;
            }
        }

        private string FindMissingMaterials()
        {
            try
            {
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Requires an Assembly document.";

                AssemblyDocument asm = (AssemblyDocument)doc;
                var sb = new StringBuilder();
                sb.AppendLine("── Missing Materials ──");

                int count = 0;
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        Document refDoc =
                            occ.Definition.Document as Document;

                        if (refDoc == null ||
                            refDoc.DocumentType !=
                            DocumentTypeEnum.kPartDocumentObject)
                            continue;

                        string mat =
                            ((PartDocument)refDoc)
                                .ComponentDefinition
                                .Material.Name;

                        if (string.IsNullOrWhiteSpace(mat) ||
                            mat.ToLower() == "generic" ||
                            mat.ToLower() == "default")
                        {
                            sb.AppendLine(
                                "⚠️ " + occ.Name + " — " + mat);
                            count++;
                        }
                    }
                    catch { }
                }

                if (count == 0)
                    sb.AppendLine("✅ All parts have materials set.");
                else
                    sb.AppendLine(
                        System.Environment.NewLine +
                        "Missing/generic: " + count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to check materials: " + ex.Message;
            }
        }

        private string FindDuplicatePartNumbers()
        {
            try
            {
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Requires an Assembly document.";

                AssemblyDocument asm = (AssemblyDocument)doc;

                var partNumbers =
                    new Dictionary<string, List<string>>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        Document refDoc =
                            occ.Definition.Document as Document;

                        if (refDoc == null) continue;

                        string pn = Convert.ToString(
                            refDoc.PropertySets[
                                "{32853F0F-3444-11D1-" +
                                "9E93-0060B03C1CA6}"][
                                "Part Number"].Value);

                        if (string.IsNullOrWhiteSpace(pn))
                            continue;

                        if (!partNumbers.ContainsKey(pn))
                            partNumbers[pn] = new List<string>();

                        partNumbers[pn].Add(occ.Name);
                    }
                    catch { }
                }

                var sb = new StringBuilder();
                sb.AppendLine("── Duplicate Part Numbers ──");

                int dupCount = 0;
                foreach (var kvp in partNumbers)
                {
                    if (kvp.Value.Count > 1)
                    {
                        sb.AppendLine(
                            "⚠️ " + kvp.Key +
                            " (" + kvp.Value.Count + " occurrences):");
                        foreach (string n in kvp.Value)
                            sb.AppendLine("    - " + n);
                        dupCount++;
                    }
                }

                if (dupCount == 0)
                    sb.AppendLine("✅ No duplicate part numbers.");
                else
                    sb.AppendLine(
                        System.Environment.NewLine +
                        "Duplicate part numbers: " + dupCount);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to check duplicates: " + ex.Message;
            }
        }

        // ─── List open documents ──────────────────────────────────────

        private string ListOpenDocuments()
        {
            try
            {
                if (_inventorApplication.Documents.Count == 0)
                    return "No documents are currently open.";

                var sb = new StringBuilder();
                sb.AppendLine(
                    "Open documents (" +
                    _inventorApplication.Documents.Count + "):");
                sb.AppendLine(new string('-', 40));

                int i = 1;
                foreach (Document doc in
                    _inventorApplication.Documents)
                {
                    try
                    {
                        string type = "";
                        switch (doc.DocumentType)
                        {
                            case DocumentTypeEnum.kPartDocumentObject:
                                type = "Part"; break;
                            case DocumentTypeEnum.kAssemblyDocumentObject:
                                type = "Assembly"; break;
                            case DocumentTypeEnum.kDrawingDocumentObject:
                                type = "Drawing"; break;
                            default:
                                type = "Document"; break;
                        }
                        sb.AppendLine(
                            i++ + ". [" + type + "] " +
                            doc.DisplayName);
                    }
                    catch { }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list documents: " + ex.Message;
            }
        }
    }
}