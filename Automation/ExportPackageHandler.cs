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
                    .kDrawingDocumentObject)
                    return ExportDrawing(
                        (DrawingDocument)doc);

                if (doc.DocumentType ==
                    DocumentTypeEnum
                    .kAssemblyDocumentObject)
                    return ExportAssembly(
                        (AssemblyDocument)doc);

                return
                    "❌ Please open an IPT, " +
                    "IDW or IAM document.";
            }
            catch (Exception ex)
            {
                return
                    $"❌ Export failed:\n" +
                    $"{ex.Message}";
            }
        }

        public string ListTranslators()
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "📋 Available Translators:");
            sb.AppendLine(new string('─', 35));
            try
            {
                foreach (ApplicationAddIn addin in
                    _app.ApplicationAddIns)
                {
                    try
                    {
                        if (addin is TranslatorAddIn)
                        {
                            sb.AppendLine(
                                addin.DisplayName);
                            sb.AppendLine(
                                "  " +
                                addin.ClassIdString);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(
                    $"Error: {ex.Message}");
            }
            return sb.ToString();
        }

        // ── IPT ──────────────────────────────────
        private string ExportPart(
            PartDocument part)
        {
            var results = new List<string>();
            string folder =
                SysPath.GetDirectoryName(
                    part.FullFileName);
            string baseName =
                SysPath.GetFileNameWithoutExtension(
                    part.FullFileName);
            string exportFolder =
                SysPath.Combine(
                    folder,
                    baseName + "_Export");

            Directory.CreateDirectory(exportFolder);
            results.Add($"📁 {exportFolder}\n");

            // STP
            try
            {
                string stpPath =
                    SysPath.Combine(
                        exportFolder,
                        baseName + ".stp");
                part.SaveAs(stpPath, true);
                results.Add("✅ STP exported");
            }
            catch (Exception ex)
            {
                results.Add(
                    $"⚠️ STP failed: {ex.Message}");
            }

                       return BuildReport(
                part.DisplayName,
                exportFolder, results);
        }

        // ── IDW ──────────────────────────────────
        private string ExportDrawing(
            DrawingDocument drawing)
        {
            var results = new List<string>();
            string folder =
                SysPath.GetDirectoryName(
                    drawing.FullFileName);
            string baseName =
                SysPath.GetFileNameWithoutExtension(
                    drawing.FullFileName);
            string exportFolder =
                SysPath.Combine(
                    folder,
                    baseName + "_Export");

            Directory.CreateDirectory(exportFolder);
            results.Add($"📁 {exportFolder}\n");

            // PDF
            try
            {
                string pdfPath =
                    SysPath.Combine(
                        exportFolder,
                        baseName + ".pdf");

                drawing.SaveAs(pdfPath, true);
                results.Add("✅ PDF exported");
            }
            catch
            {
                // Try translator method
                try
                {
                    string pdfPath =
                        SysPath.Combine(
                            exportFolder,
                            baseName + ".pdf");

                    TranslatorAddIn pdfAddin =
                        GetTranslator(
                        "{0AC6FD96-2F4D-42CE" +
                        "-8BE0-8AEA580399E4}");

                    if (pdfAddin != null)
                    {
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
                        pdfAddin.SaveCopyAs(
                            drawing, ctx, opts, dm);
                        results.Add("✅ PDF exported");
                    }
                    else
                        results.Add(
                            "⚠️ PDF translator not found");
                }
                catch (Exception ex)
                {
                    results.Add(
                        $"⚠️ PDF failed: {ex.Message}");
                }
            }

            // PNG
            try
            {
                string pngPath =
                    SysPath.Combine(
                        exportFolder,
                        baseName + ".png");
                _app.ActiveView.SaveAsBitmap(
                    pngPath, 1920, 1080);
                results.Add("✅ PNG exported");
            }
            catch (Exception ex)
            {
                results.Add(
                    $"⚠️ PNG failed: {ex.Message}");
            }

            return BuildReport(
                drawing.DisplayName,
                exportFolder, results);
        }

        // ── IAM ──────────────────────────────────
        private string ExportAssembly(
            AssemblyDocument asm)
        {
            var results = new List<string>();
            string folder =
                SysPath.GetDirectoryName(
                    asm.FullFileName);
            string baseName =
                SysPath.GetFileNameWithoutExtension(
                    asm.FullFileName);
            string exportFolder =
                SysPath.Combine(
                    folder,
                    baseName + "_Export");

            Directory.CreateDirectory(exportFolder);
            results.Add($"📁 {exportFolder}\n");

            // STP
            try
            {
                string stpPath =
                    SysPath.Combine(
                        exportFolder,
                        baseName + ".stp");
                asm.SaveAs(stpPath, true);
                results.Add("✅ STP exported");
            }
            catch (Exception ex)
            {
                results.Add(
                    $"⚠️ STP failed: {ex.Message}");
            }

            return BuildReport(
                asm.DisplayName,
                exportFolder, results);
        }

        // ── DXF ──────────────────────────────────
        private void ExportDxf(
            PartDocument part,
            string folder,
            string baseName,
            List<string> results)
        {
            try
            {
                string dxfPath =
                    SysPath.Combine(
                        folder,
                        baseName + ".dxf");

                TranslatorAddIn addin =
                    GetTranslator(
                    "{C24E3AC4-122E-11D5" +
                    "-8E91-0010B541CD80}");

                if (addin == null)
                {
                    results.Add(
                        "⚠️ DXF translator " +
                        "not found");
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
            sb.AppendLine(
                $"Document: {docName}");
            sb.AppendLine(new string('─', 35));
            foreach (var r in results)
                sb.AppendLine(r);
            sb.AppendLine(new string('─', 35));
            sb.AppendLine($"📁 {exportFolder}");
            return sb.ToString();
        }
    }
}