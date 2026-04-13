using System;
using System.Text;
using System.Text.RegularExpressions;
using Inventor;
using SysEnv = System.Environment;

namespace Inventor2023AIAssistant
{
    public class DrawingAutomation
    {
        private Inventor.Application _inventorApplication;

        public DrawingAutomation(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleDrawing(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if ((n.Contains("new") ||
                 n.Contains("create")) &&
                n.Contains("drawing"))
                return NewDrawing();

            if (n.Contains("add all views") ||
                n.Contains("all views") ||
                (n.Contains("add") &&
                 n.Contains("views")))
                return AddAllViews();

            if (n.Contains("add front view") ||
                n == "front view")
                return AddSingleView(
                    ViewOrientationTypeEnum
                        .kFrontViewOrientation,
                    "Front", 8, 12,
                    DrawingViewStyleEnum
                        .kHiddenLineDrawingViewStyle);

            if (n.Contains("add top view") ||
                n == "top view")
                return AddSingleView(
                    ViewOrientationTypeEnum
                        .kTopViewOrientation,
                    "Top", 8, 24,
                    DrawingViewStyleEnum
                        .kHiddenLineDrawingViewStyle);

            if (n.Contains("add right view") ||
                n == "right view" ||
                n == "side view")
                return AddSingleView(
                    ViewOrientationTypeEnum
                        .kRightViewOrientation,
                    "Right", 22, 12,
                    DrawingViewStyleEnum
                        .kHiddenLineDrawingViewStyle);

            if (n.Contains("add iso") ||
                n.Contains("isometric") ||
                n == "iso view")
                return AddSingleView(
                    ViewOrientationTypeEnum
                        .kIsoTopRightViewOrientation,
                    "Isometric", 22, 24,
                    DrawingViewStyleEnum
                        .kShadedDrawingViewStyle);

            if (n.Contains("add dimension") ||
                n.Contains("auto dimension") ||
                n.Contains("add all dimension"))
                return AddDimensions();

            if (n.Contains("title block") ||
                n.Contains("update title") ||
                n.Contains("fill title"))
                return UpdateTitleBlock();

            if (n.Contains("add border") ||
                n.Contains("sheet border"))
                return AddBorder();

            if (n.Contains("export drawing") ||
                (n.Contains("export") &&
                 n.Contains("pdf") &&
                 n.Contains("drawing")))
                return ExportDrawingPdf();

            if (n.Contains("export") &&
                n.Contains("dwg"))
                return ExportDrawingDwg();

            if (n.Contains("drawing info") ||
                n.Contains("show drawing") ||
                n.Contains("drawing sheet"))
                return GetDrawingInfo();

            if (n.Contains("add sheet") ||
                n.Contains("new sheet"))
                return AddSheet();

            if (n.Contains("delete all views") ||
                n.Contains("clear views"))
                return DeleteAllViews();

            if (n.Contains("set scale") ||
                n.Contains("change scale"))
                return SetDrawingScale(prompt);

            return null;
        }

        // ─── New drawing ──────────────────────────────────────────────

        private string NewDrawing()
        {
            try
            {
                // Try to find custom template first
                string templatePath = FindTemplate(
                    "LANDSCAPE_SM_26_BC.idw");

                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    // Fall back to default drawing template
                    templatePath =
                        _inventorApplication.FileManager
                            .GetTemplateFile(
                                DocumentTypeEnum
                                    .kDrawingDocumentObject);
                }

                Document doc =
                    _inventorApplication.Documents.Add(
                        DocumentTypeEnum
                            .kDrawingDocumentObject,
                        templatePath);

                return
                    "✅ New Drawing created: " +
                    doc.DisplayName +
                    System.Environment.NewLine +
                    "Template: " +
                    System.IO.Path.GetFileName(
                        templatePath) +
                    System.Environment.NewLine +
                    "Make sure your Part or Assembly " +
                    "is also open, then say:" +
                    System.Environment.NewLine +
                    "'add all views'";
            }
            catch (Exception ex)
            {
                return "Failed to create drawing: " +
                       ex.Message;
            }
        }

        private string FindTemplate(string fileName)
        {
            try
            {
                // Common Inventor template locations
                string[] searchRoots = new string[]
                {
                    // Inventor default templates folder
                    System.IO.Path.Combine(
                        SysEnv.GetFolderPath(
                            SysEnv.SpecialFolder
                                .CommonApplicationData),
                        @"Autodesk\Inventor 2023\Templates"),

                    System.IO.Path.Combine(
                        SysEnv.GetFolderPath(
                            SysEnv.SpecialFolder
                                .CommonApplicationData),
                        @"Autodesk\Inventor 2024\Templates"),

                    System.IO.Path.Combine(
                        SysEnv.GetFolderPath(
                            SysEnv.SpecialFolder
                                .CommonApplicationData),
                        @"Autodesk\Inventor 2025\Templates"),

                    // Public documents
                    System.IO.Path.Combine(
                        SysEnv.GetFolderPath(
                            SysEnv.SpecialFolder
                                .CommonDocuments),
                        @"Autodesk\Inventor 2023\Templates"),

                    // User documents
                    System.IO.Path.Combine(
                        SysEnv.GetFolderPath(
                            SysEnv.SpecialFolder
                                .MyDocuments),
                        @"Inventor\Templates"),

                    // Try to get from Inventor file options
                    GetInventorTemplatesFolder(),
                };

                foreach (string root in searchRoots)
                {
                    if (string.IsNullOrWhiteSpace(root))
                        continue;

                    if (!System.IO.Directory.Exists(root))
                        continue;

                    // Search recursively
                    string[] found =
                        System.IO.Directory.GetFiles(
                            root,
                            fileName,
                            System.IO.SearchOption
                                .AllDirectories);

                    if (found.Length > 0)
                        return found[0];
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        private string GetInventorTemplatesFolder()
        {
            try
            {
                // Get templates path from Inventor
                // file options
                string defaultTemplate =
                    _inventorApplication.FileManager
                        .GetTemplateFile(
                            DocumentTypeEnum
                                .kDrawingDocumentObject);

                return System.IO.Path.GetDirectoryName(
                    defaultTemplate);
            }
            catch
            {
                return "";
            }
        }

        // ─── Get drawing doc ──────────────────────────────────────────

        private DrawingDocument GetDrawingDoc()
        {
            Document doc =
                _inventorApplication.ActiveDocument;

            if (doc == null) return null;

            if (doc.DocumentType !=
                DocumentTypeEnum.kDrawingDocumentObject)
                return null;

            return (DrawingDocument)doc;
        }

        // ─── Get model doc — robust version ───────────────────────────

        private Document GetModelDoc()
        {
            // First try — find a visible part or assembly
            // that is NOT a drawing
            foreach (Document doc in
                _inventorApplication.Documents)
            {
                try
                {
                    if (doc.DocumentType ==
                        DocumentTypeEnum
                            .kPartDocumentObject ||
                        doc.DocumentType ==
                        DocumentTypeEnum
                            .kAssemblyDocumentObject)
                    {
                        // Make sure it is fully loaded
                        string path = doc.FullFileName;

                        if (!string.IsNullOrWhiteSpace(
                                path))
                            return doc;
                    }
                }
                catch { }
            }

            return null;
        }

        // ─── Add single view ──────────────────────────────────────────

        private string AddSingleView(
            ViewOrientationTypeEnum orientation,
            string name,
            double x,
            double y,
            DrawingViewStyleEnum style)
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document. " +
                           "Open or create a drawing first.";

                Document model = GetModelDoc();

                if (model == null)
                    return "No Part or Assembly is open. " +
                           "Open your model first, then " +
                           "switch back to the drawing " +
                           "and try again.";

                Sheet sheet = dwg.ActiveSheet;

                TransientGeometry tg =
                    _inventorApplication.TransientGeometry;

                Point2d pos = tg.CreatePoint2d(x, y);

                // Cast to _Document as required by API
                _Document modelDoc = (_Document)model;

                DrawingView view =
                    sheet.DrawingViews.AddBaseView(
                        modelDoc,
                        pos,
                        1.0,
                        orientation,
                        style,
                        "",
                        null,
                        null);

                return
                    "✅ " + name + " view added: " +
                    view.Name +
                    System.Environment.NewLine +
                    "Scale: 1:1";
            }
            catch (Exception ex)
            {
                return
                    "Failed to add " + name + " view." +
                    System.Environment.NewLine +
                    "Error: " + ex.Message +
                    System.Environment.NewLine +
                    System.Environment.NewLine +
                    "── Troubleshooting ──" +
                    System.Environment.NewLine +
                    "1. Make sure a Part or Assembly " +
                    "is open alongside the drawing." +
                    System.Environment.NewLine +
                    "2. Make sure the drawing is the " +
                    "active document (click on it)." +
                    System.Environment.NewLine +
                    "3. Make sure no Inventor command " +
                    "is active (press Escape first)." +
                    System.Environment.NewLine +
                    "4. Try saving the part first " +
                    "then retry.";
            }
        }

        // ─── Add all views ────────────────────────────────────────────

        private string AddAllViews()
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return
                        "No active Drawing document." +
                        System.Environment.NewLine +
                        "Step 1: Open your Part or Assembly." +
                        System.Environment.NewLine +
                        "Step 2: Say 'new drawing'." +
                        System.Environment.NewLine +
                        "Step 3: Say 'add all views'.";

                Document model = GetModelDoc();

                if (model == null)
                    return
                        "No Part or Assembly is open." +
                        System.Environment.NewLine +
                        "Open your model file first, " +
                        "then say 'add all views' again.";

                // Make sure model is saved
                string modelPath = "";
                try { modelPath = model.FullFileName; }
                catch { }

                if (string.IsNullOrWhiteSpace(modelPath))
                    return
                        "The model has not been saved yet." +
                        System.Environment.NewLine +
                        "Save the part first " +
                        "(say 'save'), then try again.";

                Sheet sheet = dwg.ActiveSheet;
                TransientGeometry tg =
                    _inventorApplication.TransientGeometry;

                // Cast once — required by Inventor API
                _Document modelDoc = (_Document)model;

                var sb = new StringBuilder();
                sb.AppendLine("── Adding Views ──");
                sb.AppendLine(
                    "Model: " +
                    System.IO.Path.GetFileName(modelPath));
                sb.AppendLine();

                int added = 0;

                // ── Front view ────────────────────────────
                try
                {
                    DrawingView front =
                        sheet.DrawingViews.AddBaseView(
                            modelDoc,
                            tg.CreatePoint2d(8, 12),
                            1.0,
                            ViewOrientationTypeEnum
                                .kFrontViewOrientation,
                            DrawingViewStyleEnum
                                .kHiddenLineDrawingViewStyle,
                            "", null, null);

                    sb.AppendLine(
                        "✅ Front: " + front.Name);
                    added++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine(
                        "⚠️ Front failed: " + ex.Message);
                }

                // ── Top view ──────────────────────────────
                try
                {
                    DrawingView top =
                        sheet.DrawingViews.AddBaseView(
                            modelDoc,
                            tg.CreatePoint2d(8, 24),
                            1.0,
                            ViewOrientationTypeEnum
                                .kTopViewOrientation,
                            DrawingViewStyleEnum
                                .kHiddenLineDrawingViewStyle,
                            "", null, null);

                    sb.AppendLine(
                        "✅ Top: " + top.Name);
                    added++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine(
                        "⚠️ Top failed: " + ex.Message);
                }

                // ── Right view ────────────────────────────
                try
                {
                    DrawingView right =
                        sheet.DrawingViews.AddBaseView(
                            modelDoc,
                            tg.CreatePoint2d(22, 12),
                            1.0,
                            ViewOrientationTypeEnum
                                .kRightViewOrientation,
                            DrawingViewStyleEnum
                                .kHiddenLineDrawingViewStyle,
                            "", null, null);

                    sb.AppendLine(
                        "✅ Right: " + right.Name);
                    added++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine(
                        "⚠️ Right failed: " + ex.Message);
                }

                // ── Isometric view ────────────────────────
                try
                {
                    DrawingView iso =
                        sheet.DrawingViews.AddBaseView(
                            modelDoc,
                            tg.CreatePoint2d(22, 24),
                            1.0,
                            ViewOrientationTypeEnum
                                .kIsoTopRightViewOrientation,
                            DrawingViewStyleEnum
                                .kShadedDrawingViewStyle,
                            "", null, null);

                    sb.AppendLine(
                        "✅ Isometric: " + iso.Name);
                    added++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine(
                        "⚠️ Iso failed: " + ex.Message);
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Views added: " + added + " of 4");

                if (added > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(
                        "Next steps:");
                    sb.AppendLine(
                        "  'update title block'");
                    sb.AppendLine(
                        "  'set scale 1:2'");
                    sb.AppendLine(
                        "  'export drawing pdf'");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine(
                        "── All views failed ──");
                    sb.AppendLine(
                        "Common fixes:");
                    sb.AppendLine(
                        "1. Save the part file first.");
                    sb.AppendLine(
                        "2. Press Escape in Inventor " +
                        "to cancel any active command.");
                    sb.AppendLine(
                        "3. Click on the drawing tab " +
                        "to make it active.");
                    sb.AppendLine(
                        "4. Make sure the part and " +
                        "drawing are both open.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return
                    "Failed to add views: " + ex.Message +
                    System.Environment.NewLine +
                    System.Environment.NewLine +
                    "Make sure:" +
                    System.Environment.NewLine +
                    "1. A Part or Assembly is open." +
                    System.Environment.NewLine +
                    "2. The Drawing is the active document." +
                    System.Environment.NewLine +
                    "3. The part file has been saved.";
            }
        }

        // ─── Add dimensions ───────────────────────────────────────────

        private string AddDimensions()
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document.";

                Sheet sheet = dwg.ActiveSheet;

                if (sheet.DrawingViews.Count == 0)
                    return "No views found. " +
                           "Add views first.";

                // Launch the auto-dimension command
                try
                {
                    _inventorApplication
                        .CommandManager
                        .ControlDefinitions[
                            "DrawingAutoDimCmd"]
                        .Execute();

                    return
                        "✅ Auto-Dimension launched." +
                        System.Environment.NewLine +
                        "Select geometry in the view " +
                        "and click OK to place dimensions.";
                }
                catch
                {
                    return
                        "Auto-dimension command not " +
                        "available in this context." +
                        System.Environment.NewLine +
                        "Add dimensions manually via:" +
                        System.Environment.NewLine +
                        "Annotate tab > Dimension";
                }
            }
            catch (Exception ex)
            {
                return "Failed to add dimensions: " +
                       ex.Message;
            }
        }

        // ─── Update title block ───────────────────────────────────────

        private string UpdateTitleBlock()
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document.";

                string partNumber = "";
                string description = "";
                string material = "";
                string author = "Blake Conner";

                Document model = GetModelDoc();

                if (model != null)
                {
                    try
                    {
                        partNumber = Convert.ToString(
                            model.PropertySets[
                                "{32853F0F-3444-11D1-" +
                                "9E93-0060B03C1CA6}"][
                                "Part Number"].Value);
                    }
                    catch { }

                    try
                    {
                        description = Convert.ToString(
                            model.PropertySets[
                                "{32853F0F-3444-11D1-" +
                                "9E93-0060B03C1CA6}"][
                                "Description"].Value);
                    }
                    catch { }

                    try
                    {
                        if (model.DocumentType ==
                            DocumentTypeEnum
                                .kPartDocumentObject)
                            material =
                                ((PartDocument)model)
                                    .ComponentDefinition
                                    .Material.Name;
                    }
                    catch { }

                    try
                    {
                        string a = Convert.ToString(
                            model.PropertySets[
                                "Summary Information"][
                                "Author"].Value);
                        if (!string.IsNullOrWhiteSpace(a))
                            author = a;
                    }
                    catch { }
                }

                // Write to drawing iProperties
                try
                {
                    if (!string.IsNullOrWhiteSpace(
                            partNumber))
                        dwg.PropertySets[
                            "{32853F0F-3444-11D1-" +
                            "9E93-0060B03C1CA6}"][
                            "Part Number"].Value =
                            partNumber;
                }
                catch { }

                try
                {
                    if (!string.IsNullOrWhiteSpace(
                            description))
                        dwg.PropertySets[
                            "{32853F0F-3444-11D1-" +
                            "9E93-0060B03C1CA6}"][
                            "Description"].Value =
                            description;
                }
                catch { }

                try
                {
                    dwg.PropertySets[
                        "Summary Information"][
                        "Author"].Value = author;
                }
                catch { }

                try { dwg.Update(); } catch { }

                return
                    "✅ Title block updated." +
                    System.Environment.NewLine +
                    "Part Number: " + partNumber +
                    System.Environment.NewLine +
                    "Description: " + description +
                    System.Environment.NewLine +
                    "Material:    " + material +
                    System.Environment.NewLine +
                    "Author:      " + author +
                    System.Environment.NewLine +
                    "Date:        " +
                    DateTime.Now.ToString("MM/dd/yyyy") +
                    System.Environment.NewLine +
                    System.Environment.NewLine +
                    "Note: Title block fields that are " +
                    "linked to iProperties will update " +
                    "automatically.";
            }
            catch (Exception ex)
            {
                return "Failed to update title block: " +
                       ex.Message;
            }
        }

        // ─── Add border ───────────────────────────────────────────────

        private string AddBorder()
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document.";

                if (dwg.BorderDefinitions.Count == 0)
                    return
                        "No border definitions found." +
                        System.Environment.NewLine +
                        "Add via Drawing Resources " +
                        "in the model browser.";

                Sheet sheet = dwg.ActiveSheet;

                BorderDefinition bd =
                    dwg.BorderDefinitions[1];

                NameValueMap map =
                    _inventorApplication.TransientObjects
                        .CreateNameValueMap();

                sheet.AddBorder(bd, map);

                return "✅ Border added: " + bd.Name;
            }
            catch (Exception ex)
            {
                return "Failed to add border: " + ex.Message;
            }
        }

        // ─── Export PDF ───────────────────────────────────────────────

        private string ExportDrawingPdf()
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document.";

                if (string.IsNullOrWhiteSpace(
                        dwg.FullFileName))
                    return
                        "Save the drawing first, " +
                        "then say 'export drawing pdf'.";

                string savePath =
                    System.IO.Path.ChangeExtension(
                        dwg.FullFileName, ".pdf");

                TranslatorAddIn pdfAddIn = null;

                foreach (ApplicationAddIn addIn in
                    _inventorApplication.ApplicationAddIns)
                {
                    try
                    {
                        if (addIn.ClassIdString ==
                            "{0AC6FD96-2F4D-42CE-" +
                            "8BE0-8AEA580399E4}")
                        {
                            pdfAddIn =
                                addIn as TranslatorAddIn;
                            break;
                        }
                    }
                    catch { }
                }

                if (pdfAddIn == null)
                    return
                        "PDF translator not found." +
                        System.Environment.NewLine +
                        "Export via File > Export > PDF.";

                TranslationContext ctx =
                    _inventorApplication.TransientObjects
                        .CreateTranslationContext();
                ctx.Type =
                    IOMechanismEnum
                        .kFileBrowseIOMechanism;

                NameValueMap opts =
                    _inventorApplication.TransientObjects
                        .CreateNameValueMap();

                DataMedium data =
                    _inventorApplication.TransientObjects
                        .CreateDataMedium();
                data.FileName = savePath;

                pdfAddIn.SaveCopyAs(dwg, ctx, opts, data);

                try
                {
                    System.Diagnostics.Process
                        .Start(savePath);
                }
                catch { }

                return
                    "✅ Exported to PDF:" +
                    System.Environment.NewLine + savePath;
            }
            catch (Exception ex)
            {
                return "Failed to export PDF: " + ex.Message;
            }
        }

        // ─── Export DWG ───────────────────────────────────────────────

        private string ExportDrawingDwg()
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document.";

                if (string.IsNullOrWhiteSpace(
                        dwg.FullFileName))
                    return
                        "Save the drawing first, " +
                        "then say 'export drawing dwg'.";

                string savePath =
                    System.IO.Path.ChangeExtension(
                        dwg.FullFileName, ".dwg");

                TranslatorAddIn dwgAddIn = null;

                foreach (ApplicationAddIn addIn in
                    _inventorApplication.ApplicationAddIns)
                {
                    try
                    {
                        if (addIn.ClassIdString ==
                            "{C24E3AC2-122E-11D5-" +
                            "8E91-0010B541CD80}")
                        {
                            dwgAddIn =
                                addIn as TranslatorAddIn;
                            break;
                        }
                    }
                    catch { }
                }

                if (dwgAddIn == null)
                    return
                        "DWG translator not found." +
                        System.Environment.NewLine +
                        "Export via File > Export > DWG.";

                TranslationContext ctx =
                    _inventorApplication.TransientObjects
                        .CreateTranslationContext();
                ctx.Type =
                    IOMechanismEnum
                        .kFileBrowseIOMechanism;

                NameValueMap opts =
                    _inventorApplication.TransientObjects
                        .CreateNameValueMap();

                DataMedium data =
                    _inventorApplication.TransientObjects
                        .CreateDataMedium();
                data.FileName = savePath;

                dwgAddIn.SaveCopyAs(dwg, ctx, opts, data);

                return
                    "✅ Exported to DWG:" +
                    System.Environment.NewLine + savePath;
            }
            catch (Exception ex)
            {
                return "Failed to export DWG: " + ex.Message;
            }
        }

        // ─── Drawing info ─────────────────────────────────────────────

        private string GetDrawingInfo()
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document.";

                var sb = new StringBuilder();
                sb.AppendLine("── Drawing Info ──");
                sb.AppendLine("File: " + dwg.DisplayName);
                sb.AppendLine(
                    "Sheets: " + dwg.Sheets.Count);

                foreach (Sheet sheet in dwg.Sheets)
                {
                    sb.AppendLine(
                        System.Environment.NewLine +
                        "Sheet: " + sheet.Name);
                    sb.AppendLine(
                        "  Size: " +
                        sheet.Width.ToString("0.00") +
                        " x " +
                        sheet.Height.ToString("0.00") +
                        " cm");
                    sb.AppendLine(
                        "  Views: " +
                        sheet.DrawingViews.Count);

                    foreach (DrawingView v in
                        sheet.DrawingViews)
                    {
                        try
                        {
                            sb.AppendLine(
                                "    - " + v.Name +
                                " (1:" +
                                (1.0 / v.Scale)
                                    .ToString("0.##") +
                                ")");
                        }
                        catch { }
                    }

                    sb.AppendLine(
                        "  Dimensions: " +
                        sheet.DrawingDimensions
                             .GeneralDimensions.Count);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get drawing info: " +
                       ex.Message;
            }
        }

        // ─── Add sheet ────────────────────────────────────────────────

        private string AddSheet()
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document.";

                int count = dwg.Sheets.Count;
                Sheet s = dwg.Sheets.Add();
                s.Name = "Sheet" + (count + 1);

                return
                    "✅ Sheet added: " + s.Name +
                    System.Environment.NewLine +
                    "Total sheets: " + dwg.Sheets.Count;
            }
            catch (Exception ex)
            {
                return "Failed to add sheet: " + ex.Message;
            }
        }

        // ─── Delete all views ─────────────────────────────────────────

        private string DeleteAllViews()
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document.";

                Sheet sheet = dwg.ActiveSheet;
                int count = sheet.DrawingViews.Count;

                if (count == 0)
                    return "No views to delete.";

                for (int i = count; i >= 1; i--)
                {
                    try
                    {
                        sheet.DrawingViews[i].Delete();
                    }
                    catch { }
                }

                return "✅ Deleted " + count + " view(s).";
            }
            catch (Exception ex)
            {
                return "Failed to delete views: " +
                       ex.Message;
            }
        }

        // ─── Set scale ────────────────────────────────────────────────

        private string SetDrawingScale(string prompt)
        {
            try
            {
                DrawingDocument dwg = GetDrawingDoc();

                if (dwg == null)
                    return "No active Drawing document.";

                Sheet sheet = dwg.ActiveSheet;

                if (sheet.DrawingViews.Count == 0)
                    return "No views found. Add views first.";

                double scale = 1.0;

                Match m = Regex.Match(
                    prompt, @"1\s*:\s*(\d+)");

                if (m.Success)
                    scale = 1.0 /
                            double.Parse(m.Groups[1].Value);
                else
                {
                    m = Regex.Match(prompt, @"([\d.]+)");
                    if (m.Success)
                        scale = double.Parse(
                            m.Groups[1].Value);
                }

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Setting Scale 1:" +
                    (1.0 / scale).ToString("0.##") +
                    " ──");

                foreach (DrawingView view in
                    sheet.DrawingViews)
                {
                    try
                    {
                        view.Scale = scale;
                        sb.AppendLine(
                            "✅ " + view.Name +
                            " → 1:" +
                            (1.0 / scale)
                                .ToString("0.##"));
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine(
                            "⚠️ " + view.Name +
                            ": " + ex.Message);
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to set scale: " + ex.Message;
            }
        }
    }
}