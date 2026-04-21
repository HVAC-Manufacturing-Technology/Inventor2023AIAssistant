using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SysPath = System.IO.Path;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class ExportPackageHandler
    {
        private readonly Inventor.Application _app;

        public ExportPackageHandler(
            Inventor.Application app)
        {
            _app = app;
        }

        public string CreateExportPackage()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "❌ No active document.";

                if (doc.DocumentType ==
                    DocumentTypeEnum
                    .kPartDocumentObject)
                    return ExportPart(
                        (PartDocument)doc);

                if (doc.DocumentType ==
                    DocumentTypeEnum
                    .kAssemblyDocumentObject)
                    return ExportAssembly(
                        (AssemblyDocument)doc);

                return
                    "❌ Please open a Part or " +
                    "Assembly document.";
            }
            catch (Exception ex)
            {
                return $"❌ Export failed:\n{ex.Message}";
            }
        }

        private string ExportPart(PartDocument part)
        {
            var results = new List<string>();

            string folder = SysPath.GetDirectoryName(
                part.FullFileName);
            string baseName =
                SysPath.GetFileNameWithoutExtension(
                    part.FullFileName);
            string exportFolder = SysPath.Combine(
                folder, baseName + "_Export");

            Directory.CreateDirectory(exportFolder);
            results.Add(
                $"📁 {exportFolder}\n");

            // STP
            ExportStp(part, exportFolder,
                baseName, results);

            // PDF
            ExportPdf(part, exportFolder,
                baseName, results);

            // PNG
            ExportPng(exportFolder,
                baseName, results);

            // DXF (sheet metal only)
            if (part.ComponentDefinition
                is SheetMetalComponentDefinition)
                ExportDxf(part, exportFolder,
                    baseName, results);

            return BuildReport(
                part.DisplayName,
                exportFolder, results);
        }

        private string ExportAssembly(
            AssemblyDocument asm)
        {
            var results = new List<string>();

            string folder = SysPath.GetDirectoryName(
                asm.FullFileName);
            string baseName =
                SysPath.GetFileNameWithoutExtension(
                    asm.FullFileName);
            string exportFolder = SysPath.Combine(
                folder, baseName + "_Export");

            Directory.CreateDirectory(exportFolder);
            results.Add($"📁 {exportFolder}\n");

            // Assembly STP
            ExportStp(asm, exportFolder,
                baseName + "_Assembly", results);

            // Assembly PNG
            ExportPng(exportFolder,
                baseName, results);

            // Each part individually
            int partCount = 0;
            int stpCount = 0;
            int dxfCount = 0;

            var exported = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition
                    .Occurrences)
                {
                    try
                    {
                        Document refDoc =
                            occ.Definition.Document
                            as Document;

                        if (refDoc == null ||
                            refDoc.DocumentType !=
                            DocumentTypeEnum
                            .kPartDocumentObject)
                            continue;

                        if (exported.Contains(
                            refDoc.FullFileName))
                            continue;

                        exported.Add(
                            refDoc.FullFileName);

                        PartDocument part =
                            (PartDocument)refDoc;
                        string pName =
                            SysPath
                            .GetFileNameWithoutExtension(
                                part.FullFileName);

                        partCount++;

                        // STP per part
                        var stpResults =
                            new List<string>();
                        ExportStp(part,
                            exportFolder,
                            pName, stpResults);
                        if (stpResults.Count > 0 &&
                            stpResults[0]
                            .Contains("✅"))
                            stpCount++;

                        // DXF for sheet metal
                        if (part.ComponentDefinition
                            is SheetMetalComponentDefinition)
                        {
                            var dxfResults =
                                new List<string>();
                            ExportDxf(part,
                                exportFolder,
                                pName, dxfResults);
                            if (dxfResults.Count > 0
                                && dxfResults[0]
                                .Contains("✅"))
                                dxfCount++;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            results.Add(
                $"✅ Parts processed: {partCount}");
            results.Add(
                $"✅ STP files: {stpCount}");
            if (dxfCount > 0)
                results.Add(
                    $"✅ DXF files: {dxfCount}");

            return BuildReport(
                asm.DisplayName,
                exportFolder, results);
        }

        private void ExportStp(
            object doc,
            string folder,
            string baseName,
            List<string> results)
        {
            try
            {
                string stpPath = SysPath.Combine(
                    folder, baseName + ".stp");

                TranslatorAddIn addin =
                    GetTranslator(
                    "{90AF7F40-0C01-11D5" +
                    "-8E83-0010B541CD80}");

                if (addin == null)
                {
                    results.Add(
                        "⚠️ STP translator not found");
                    return;
                }

                NameValueMap opts =
                    _app.TransientObjects
                    .CreateNameValueMap();
                TranslationContext ctx =
                    _app.TransientObjects
                    .CreateTranslationContext();
                ctx.Type =
                    IOMechanismEnum
                    .kUnspecifiedIOMechanism;
                DataMedium dm =
                    _app.TransientObjects
                    .CreateDataMedium();
                dm.FileName = stpPath;

                addin.SaveCopyAs(
                    doc, ctx, opts, dm);
                results.Add("✅ STP exported");
            }
            catch (Exception ex)
            {
                results.Add(
                    $"⚠️ STP failed: {ex.Message}");
            }
        }

        private void ExportPdf(
            PartDocument part,
            string folder,
            string baseName,
            List<string> results)
        {
            try
            {
                string pdfPath = SysPath.Combine(
                    folder, baseName + ".pdf");

                TranslatorAddIn addin =
                    GetTranslator(
                    "{0AC6FD96-2F4D-42CE" +
                    "-8BE0-8AEA580399E4}");

                if (addin == null)
                {
                    results.Add(
                        "⚠️ PDF translator not found");
                    return;
                }

                NameValueMap opts =
                    _app.TransientObjects
                    .CreateNameValueMap();
                TranslationContext ctx =
                    _app.TransientObjects
                    .CreateTranslationContext();
                ctx.Type =
                    IOMechanismEnum
                    .kUnspecifiedIOMechanism;
                DataMedium dm =
                    _app.TransientObjects
                    .CreateDataMedium();
                dm.FileName = pdfPath;

                addin.SaveCopyAs(
                    part, ctx, opts, dm);
                results.Add("✅ PDF exported");
            }
            catch (Exception ex)
            {
                results.Add(
                    $"⚠️ PDF failed: {ex.Message}");
            }
        }

        private void ExportPng(
            string folder,
            string baseName,
            List<string> results)
        {
            try
            {
                string pngPath = SysPath.Combine(
                    folder, baseName + ".png");

                _app.ActiveView.SaveAsBitmap(
                    pngPath, 1920, 1080);
                results.Add("✅ PNG exported");
            }
            catch (Exception ex)
            {
                results.Add(
                    $"⚠️ PNG failed: {ex.Message}");
            }
        }

        private void ExportDxf(
            PartDocument part,
            string folder,
            string baseName,
            List<string> results)
        {
            try
            {
                string dxfPath = SysPath.Combine(
                    folder, baseName + ".dxf");

                TranslatorAddIn addin =
                    GetTranslator(
                    "{C24E3AC2-122E-11D5" +
                    "-8E91-0010B541CD80}");

                if (addin == null)
                {
                    results.Add(
                        "⚠️ DXF translator not found");
                    return;
                }

                NameValueMap opts =
                    _app.TransientObjects
                    .CreateNameValueMap();
                TranslationContext ctx =
                    _app.TransientObjects
                    .CreateTranslationContext();
                ctx.Type =
                    IOMechanismEnum
                    .kUnspecifiedIOMechanism;
                DataMedium dm =
                    _app.TransientObjects
                    .CreateDataMedium();
                dm.FileName = dxfPath;

                addin.SaveCopyAs(
                    part, ctx, opts, dm);
                results.Add("✅ DXF exported");
            }
            catch (Exception ex)
            {
                results.Add(
                    $"⚠️ DXF failed: {ex.Message}");
            }
        }

        private TranslatorAddIn GetTranslator(
            string guid)
        {
            try
            {
                foreach (ApplicationAddIn addin in
                    _app.ApplicationAddIns)
                {
                    if (string.Equals(
                        addin.ClassIdString,
                        guid,
                        StringComparison
                        .OrdinalIgnoreCase))
                        return addin
                            as TranslatorAddIn;
                }
            }
            catch { }
            return null;
        }

        private string BuildReport(
            string docName,
            string exportFolder,
            List<string> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "📦 Export Package Created");
            sb.AppendLine($"Document: {docName}");
            sb.AppendLine(new string('─', 35));
            foreach (var r in results)
                sb.AppendLine(r);
            sb.AppendLine(new string('─', 35));
            sb.AppendLine($"📁 {exportFolder}");
            return sb.ToString();
        }
    }
}