using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Inventor;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace Inventor2023AIAssistant
{
    public class BomExporter
    {
        private Inventor.Application _inventorApplication;

        public BomExporter(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleBom(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (!n.Contains("bom") &&
                !n.Contains("bill of material") &&
                !n.Contains("parts list") &&
                !n.Contains("part list") &&
                !n.Contains("export bom") &&
                !n.Contains("count fastener") &&
                !n.Contains("count parts") &&
                !n.Contains("list all parts") &&
                !n.Contains("list unique parts"))
                return null;

            if (n.Contains("excel") ||
                n.Contains("export"))
                return ExportBomToExcel();

            if (n.Contains("csv"))
                return ExportBomToCsv();

            if (n.Contains("fastener") ||
                n.Contains("bolt") ||
                n.Contains("screw") ||
                n.Contains("nut") ||
                n.Contains("rivet"))
                return CountFasteners();

            if (n.Contains("unique"))
                return ListUniqueParts();

            return ShowBomInPanel();
        }

        // ─── Show BOM in panel ────────────────────────────────────────

        private string ShowBomInPanel()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return GetAssemblyBom(
                        (AssemblyDocument)doc);

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                    return GetPartInfo((PartDocument)doc);

                return "BOM is available for " +
                       "Assembly and Part documents.";
            }
            catch (Exception ex)
            {
                return "Failed to get BOM: " + ex.Message;
            }
        }

        // ─── Assembly BOM ─────────────────────────────────────────────

        private string GetAssemblyBom(AssemblyDocument asm)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("── Bill of Materials ──");
                sb.AppendLine("Assembly: " + asm.DisplayName);
                sb.AppendLine(new string('-', 60));
                sb.AppendLine(
                    "Item".PadRight(5) +
                    "Part Number".PadRight(25) +
                    "Description".PadRight(25) +
                    "Qty".PadRight(6) +
                    "Material");
                sb.AppendLine(new string('-', 60));

                var partCounts =
                    new Dictionary<string, BomItem>(
                        StringComparer.OrdinalIgnoreCase);

                CollectComponents(
                    asm.ComponentDefinition.Occurrences,
                    partCounts);

                int item = 1;
                foreach (var kvp in partCounts)
                {
                    BomItem bi = kvp.Value;
                    sb.AppendLine(
                        item.ToString().PadRight(5) +
                        bi.PartNumber.PadRight(25) +
                        bi.Description.PadRight(25) +
                        bi.Quantity.ToString().PadRight(6) +
                        bi.Material);
                    item++;
                }

                sb.AppendLine(new string('-', 60));
                sb.AppendLine(
                    "Total unique parts: " + partCounts.Count);

                int totalQty = 0;
                foreach (var kvp in partCounts)
                    totalQty += kvp.Value.Quantity;

                sb.AppendLine(
                    "Total parts (with qty): " + totalQty);
                sb.AppendLine();
                sb.AppendLine(
                    "Say 'export bom to excel' or " +
                    "'export bom to csv' to save.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read assembly BOM: " +
                       ex.Message;
            }
        }

        private void CollectComponents(
            ComponentOccurrences occurrences,
            Dictionary<string, BomItem> partCounts)
        {
            foreach (ComponentOccurrence occ in occurrences)
            {
                try
                {
                    if (!occ.Visible) continue;

                    Document refDoc =
                        occ.Definition.Document as Document;

                    if (refDoc == null) continue;

                    string partNumber = "";
                    string description = "";
                    string material = "";

                    try
                    {
                        Property pn =
                            refDoc.PropertySets[
                                "{32853F0F-3444-11D1-" +
                                "9E93-0060B03C1CA6}"][
                                "Part Number"];
                        partNumber =
                            Convert.ToString(pn.Value);
                    }
                    catch
                    {
                        partNumber = refDoc.DisplayName;
                    }

                    try
                    {
                        Property desc =
                            refDoc.PropertySets[
                                "{32853F0F-3444-11D1-" +
                                "9E93-0060B03C1CA6}"][
                                "Description"];
                        description =
                            Convert.ToString(desc.Value);
                    }
                    catch { }

                    try
                    {
                        if (refDoc.DocumentType ==
                            DocumentTypeEnum
                                .kPartDocumentObject)
                        {
                            PartDocument pd =
                                (PartDocument)refDoc;
                            material =
                                pd.ComponentDefinition
                                  .Material.Name;
                        }
                    }
                    catch { }

                    string key = partNumber;

                    if (partCounts.ContainsKey(key))
                        partCounts[key].Quantity++;
                    else
                        partCounts[key] = new BomItem
                        {
                            PartNumber = partNumber,
                            Description = description,
                            Material = material,
                            Quantity = 1
                        };

                    if (refDoc.DocumentType ==
                        DocumentTypeEnum
                            .kAssemblyDocumentObject)
                    {
                        AssemblyDocument subAsm =
                            (AssemblyDocument)refDoc;
                        CollectComponents(
                            subAsm.ComponentDefinition
                                  .Occurrences,
                            partCounts);
                    }
                }
                catch { }
            }
        }

        // ─── Part info ────────────────────────────────────────────────

        private string GetPartInfo(PartDocument part)
        {
            try
            {
                string partNumber = "";
                string description = "";
                string material = "";

                try
                {
                    partNumber = Convert.ToString(
                        part.PropertySets[
                            "{32853F0F-3444-11D1-" +
                            "9E93-0060B03C1CA6}"][
                            "Part Number"].Value);
                }
                catch { partNumber = part.DisplayName; }

                try
                {
                    description = Convert.ToString(
                        part.PropertySets[
                            "{32853F0F-3444-11D1-" +
                            "9E93-0060B03C1CA6}"][
                            "Description"].Value);
                }
                catch { }

                try
                {
                    material =
                        part.ComponentDefinition
                            .Material.Name;
                }
                catch { }

                double mass =
                    part.ComponentDefinition
                        .MassProperties.Mass;

                return
                    "── Part Information ──" +
                    System.Environment.NewLine +
                    "Part Number: " + partNumber +
                    System.Environment.NewLine +
                    "Description: " + description +
                    System.Environment.NewLine +
                    "Material:    " + material +
                    System.Environment.NewLine +
                    "Mass:        " +
                    Math.Round(mass * 2.20462, 4) + " lbs";
            }
            catch (Exception ex)
            {
                return "Failed to read part info: " +
                       ex.Message;
            }
        }

        // ─── Export to CSV ────────────────────────────────────────────

        private string ExportBomToCsv()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "BOM export requires an " +
                           "Assembly document.";

                AssemblyDocument asm = (AssemblyDocument)doc;

                var partCounts =
                    new Dictionary<string, BomItem>(
                        StringComparer.OrdinalIgnoreCase);

                CollectComponents(
                    asm.ComponentDefinition.Occurrences,
                    partCounts);

                string defaultPath = IOPath.Combine(
                    IOPath.GetDirectoryName(
                        doc.FullFileName),
                    IOPath.GetFileNameWithoutExtension(
                        doc.FullFileName) + "_BOM.csv");

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Export BOM as CSV";
                    dlg.Filter = "CSV Files (*.csv)|*.csv";
                    dlg.FileName =
                        IOPath.GetFileName(defaultPath);
                    dlg.InitialDirectory =
                        IOPath.GetDirectoryName(
                            doc.FullFileName);

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return "Export cancelled by user.";

                    string savePath = dlg.FileName;

                    var sb = new StringBuilder();
                    sb.AppendLine(
                        "Item,Part Number,Description," +
                        "Quantity,Material");

                    int item = 1;
                    foreach (var kvp in partCounts)
                    {
                        BomItem bi = kvp.Value;
                        sb.AppendLine(
                            item + "," +
                            CsvEscape(bi.PartNumber) + "," +
                            CsvEscape(bi.Description) + "," +
                            bi.Quantity + "," +
                            CsvEscape(bi.Material));
                        item++;
                    }

                    IOFile.WriteAllText(
                        savePath, sb.ToString());

                    return
                        "BOM exported to CSV:" +
                        System.Environment.NewLine +
                        savePath +
                        System.Environment.NewLine +
                        "Total parts: " + partCounts.Count;
                }
            }
            catch (Exception ex)
            {
                return "Failed to export BOM: " + ex.Message;
            }
        }

        // ─── Export to Excel ──────────────────────────────────────────

        private string ExportBomToExcel()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "BOM export requires an " +
                           "Assembly document.";

                AssemblyDocument asm = (AssemblyDocument)doc;

                var partCounts =
                    new Dictionary<string, BomItem>(
                        StringComparer.OrdinalIgnoreCase);

                CollectComponents(
                    asm.ComponentDefinition.Occurrences,
                    partCounts);

                string defaultPath = IOPath.Combine(
                    IOPath.GetDirectoryName(
                        doc.FullFileName),
                    IOPath.GetFileNameWithoutExtension(
                        doc.FullFileName) + "_BOM.xls");

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Export BOM as Excel";
                    dlg.Filter =
                        "Excel Files (*.xls)|*.xls|" +
                        "CSV Files (*.csv)|*.csv";
                    dlg.FileName =
                        IOPath.GetFileName(defaultPath);
                    dlg.InitialDirectory =
                        IOPath.GetDirectoryName(
                            doc.FullFileName);

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return "Export cancelled by user.";

                    string savePath = dlg.FileName;

                    if (savePath.EndsWith(".csv"))
                        return ExportBomToCsv();

                    var sb = new StringBuilder();
                    sb.AppendLine("<?xml version=\"1.0\"?>");
                    sb.AppendLine(
                        "<Workbook xmlns=\"urn:schemas-" +
                        "microsoft-com:office:spreadsheet\"" +
                        " xmlns:ss=\"urn:schemas-microsoft-" +
                        "com:office:spreadsheet\">");
                    sb.AppendLine(
                        "<Worksheet ss:Name=\"BOM\">");
                    sb.AppendLine("<Table>");

                    sb.AppendLine("<Row>");
                    WriteExcelCell(sb, "Item", true);
                    WriteExcelCell(sb, "Part Number", true);
                    WriteExcelCell(sb, "Description", true);
                    WriteExcelCell(sb, "Quantity", true);
                    WriteExcelCell(sb, "Material", true);
                    sb.AppendLine("</Row>");

                    int item = 1;
                    foreach (var kvp in partCounts)
                    {
                        BomItem bi = kvp.Value;
                        sb.AppendLine("<Row>");
                        WriteExcelCell(
                            sb, item.ToString(), false);
                        WriteExcelCell(
                            sb, bi.PartNumber, false);
                        WriteExcelCell(
                            sb, bi.Description, false);
                        WriteExcelCell(
                            sb, bi.Quantity.ToString(),
                            false);
                        WriteExcelCell(
                            sb, bi.Material, false);
                        sb.AppendLine("</Row>");
                        item++;
                    }

                    sb.AppendLine("</Table>");
                    sb.AppendLine("</Worksheet>");
                    sb.AppendLine("</Workbook>");

                    IOFile.WriteAllText(
                        savePath, sb.ToString());

                    try
                    {
                        System.Diagnostics.Process.Start(
                            savePath);
                    }
                    catch { }

                    return
                        "BOM exported to Excel:" +
                        System.Environment.NewLine +
                        savePath +
                        System.Environment.NewLine +
                        "Total unique parts: " +
                        partCounts.Count;
                }
            }
            catch (Exception ex)
            {
                return "Failed to export BOM to Excel: " +
                       ex.Message;
            }
        }

        private void WriteExcelCell(
            StringBuilder sb,
            string value,
            bool bold)
        {
            string style = bold ?
                " ss:StyleID=\"bold\"" : "";

            sb.AppendLine(
                "<Cell" + style + ">" +
                "<Data ss:Type=\"String\">" +
                XmlEscape(value) +
                "</Data></Cell>");
        }

        // ─── Count fasteners ──────────────────────────────────────────

        private string CountFasteners()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Fastener count requires an " +
                           "Assembly document.";

                AssemblyDocument asm = (AssemblyDocument)doc;

                var partCounts =
                    new Dictionary<string, BomItem>(
                        StringComparer.OrdinalIgnoreCase);

                CollectComponents(
                    asm.ComponentDefinition.Occurrences,
                    partCounts);

                var fasteners =
                    new Dictionary<string, BomItem>();
                var nonFasteners =
                    new Dictionary<string, BomItem>();

                string[] fastenerKeywords = new string[]
                {
                    "bolt", "screw", "nut", "washer",
                    "rivet", "rivnut", "fastener",
                    "stud", "anchor", "hex", "socket",
                    "pan head", "flat head", "round head"
                };

                foreach (var kvp in partCounts)
                {
                    BomItem bi = kvp.Value;
                    bool isFastener = false;

                    string combined =
                        (bi.PartNumber + " " +
                         bi.Description).ToLower();

                    foreach (string kw in fastenerKeywords)
                    {
                        if (combined.Contains(kw))
                        {
                            isFastener = true;
                            break;
                        }
                    }

                    if (isFastener)
                        fasteners[kvp.Key] = bi;
                    else
                        nonFasteners[kvp.Key] = bi;
                }

                var sb = new StringBuilder();
                sb.AppendLine("── Fastener Count ──");
                sb.AppendLine();

                if (fasteners.Count == 0)
                {
                    sb.AppendLine(
                        "No fasteners detected by name.");
                    sb.AppendLine(
                        "Fastener detection looks for: " +
                        "bolt, screw, nut, washer, rivet, " +
                        "rivnut in part number or description.");
                }
                else
                {
                    sb.AppendLine(
                        "Fasteners found (" +
                        fasteners.Count + " types):");
                    sb.AppendLine(new string('-', 50));

                    int totalFasteners = 0;
                    foreach (var kvp in fasteners)
                    {
                        BomItem bi = kvp.Value;
                        sb.AppendLine(
                            bi.PartNumber.PadRight(30) +
                            "Qty: " + bi.Quantity);
                        totalFasteners += bi.Quantity;
                    }

                    sb.AppendLine(new string('-', 50));
                    sb.AppendLine(
                        "Total fastener quantity: " +
                        totalFasteners);
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Non-fastener parts: " +
                    nonFasteners.Count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to count fasteners: " +
                       ex.Message;
            }
        }

        // ─── List unique parts ────────────────────────────────────────

        private string ListUniqueParts()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Parts list requires an " +
                           "Assembly document.";

                AssemblyDocument asm = (AssemblyDocument)doc;

                var partCounts =
                    new Dictionary<string, BomItem>(
                        StringComparer.OrdinalIgnoreCase);

                CollectComponents(
                    asm.ComponentDefinition.Occurrences,
                    partCounts);

                var sb = new StringBuilder();
                sb.AppendLine("── Unique Parts ──");
                sb.AppendLine(
                    "Assembly: " + asm.DisplayName);
                sb.AppendLine(new string('-', 50));

                int i = 1;
                foreach (var kvp in partCounts)
                {
                    BomItem bi = kvp.Value;
                    sb.AppendLine(
                        i + ". " +
                        bi.PartNumber.PadRight(25) +
                        " x" + bi.Quantity);
                    i++;
                }

                sb.AppendLine(new string('-', 50));
                sb.AppendLine(
                    "Unique parts: " + partCounts.Count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list parts: " + ex.Message;
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────

        private string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") ||
                value.Contains("\"") ||
                value.Contains("\n"))
                return "\"" +
                       value.Replace("\"", "\"\"") +
                       "\"";
            return value;
        }

        private string XmlEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        // ─── BOM item model ───────────────────────────────────────────

        private class BomItem
        {
            public string PartNumber { get; set; } = "";
            public string Description { get; set; } = "";
            public string Material { get; set; } = "";
            public int Quantity { get; set; } = 0;
        }
    }
}