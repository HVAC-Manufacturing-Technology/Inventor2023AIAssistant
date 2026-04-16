using System;
using System.Windows.Forms;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class WriteActionsHandler
    {
        private Inventor.Application _inventorApplication;

        public WriteActionsHandler(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleWriteAction(string prompt)
        {
            string normalized =
                prompt.Trim().ToLowerInvariant();

            if (normalized.StartsWith("set ") &&
                normalized.Contains(" to "))
                return HandleSetParameter(prompt);

            if (normalized.StartsWith("suppress "))
                return HandleSuppressFeature(prompt);

            if (normalized.StartsWith("unsuppress "))
                return HandleUnsuppressFeature(prompt);

            if (normalized.StartsWith("create model state "))
                return HandleCreateModelState(prompt);

            if (normalized.StartsWith("hide "))
                return HandleHideComponent(prompt);

            if (normalized.StartsWith("show "))
                return HandleShowComponent(prompt);

            if (normalized == "list features" ||
                normalized == "show features")
                return HandleListFeatures();

            if (normalized == "list components" ||
                normalized == "show components")
                return HandleListComponents();

            if (normalized == "check interference")
                return HandleCheckInterference();

            if (normalized == "update mass" ||
                normalized == "recalculate mass")
                return HandleUpdateMass();

            if (normalized.StartsWith("rename part number "))
                return HandleRenamePartNumber(prompt);

            if (normalized.StartsWith("set description "))
                return HandleSetDescription(prompt);

            if (normalized == "export dxf" ||
                normalized == "export as dxf")
                return HandleExportDxf();

            if (normalized == "export pdf" ||
                normalized == "export as pdf")
                return HandleExportPdf();

            if (normalized.StartsWith("generate parameters"))
                return HandleGenerateParameters(prompt);

            // ─ Open iProperties panel
            if (normalized.Contains("iproperties") &&
                (normalized.Contains("open") ||
                 normalized.Contains("show") ||
                 normalized.Contains("dock") ||
                 normalized.Contains("panel") ||
                 normalized.Contains("display")))
                return OpenIPropertiesPanel();

            return null;
        }

        // ─── Set parameter ────────────────────────────────────────────

        private string HandleSetParameter(string prompt)
        {
            try
            {
                string[] parts = prompt.Trim().Split(
                    new string[] { " to " },
                    StringSplitOptions.None);

                if (parts.Length != 2)
                    return "Could not parse. " +
                           "Use: set [parameter] to [value]";

                string paramName =
                    parts[0].Substring(4).Trim();
                string newValue = parts[1].Trim();

                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                Parameters parameters =
                    GetParameters(doc);

                if (parameters == null)
                    return "Parameters not available for " +
                           "this document type.";

                Parameter param = null;

                try { param = parameters[paramName]; }
                catch
                {
                    return "Parameter '" + paramName +
                           "' was not found.";
                }

                string currentValue = param.Expression;

                if (!Confirm(
                    "Parameter:  " + paramName +
                    System.Environment.NewLine +
                    "Current:    " + currentValue +
                    System.Environment.NewLine +
                    "New value:  " + newValue))
                    return "Change cancelled by user.";

                param.Expression = newValue;

                return "Parameter '" + paramName +
                       "' updated from " + currentValue +
                       " to " + newValue + ".";
            }
            catch (Exception ex)
            {
                return "Failed to set parameter: " +
                       ex.Message;
            }
        }

        // ─── Suppress feature ─────────────────────────────────────────

        private string HandleSuppressFeature(string prompt)
        {
            try
            {
                string featureName =
                    prompt.Trim().Substring(9).Trim();

                PartFeature feature =
                    FindPartFeature(featureName);

                if (feature == null)
                    return "Feature '" + featureName +
                           "' was not found.";

                if (feature.Suppressed)
                    return "Feature '" + featureName +
                           "' is already suppressed.";

                if (!Confirm("Suppress feature: " +
                        featureName))
                    return "Suppress cancelled by user.";

                feature.Suppressed = true;

                return "Feature '" + featureName +
                       "' has been suppressed.";
            }
            catch (Exception ex)
            {
                return "Failed to suppress: " + ex.Message;
            }
        }

        // ─── Unsuppress feature ───────────────────────────────────────

        private string HandleUnsuppressFeature(string prompt)
        {
            try
            {
                string featureName =
                    prompt.Trim().Substring(11).Trim();

                PartFeature feature =
                    FindPartFeature(featureName);

                if (feature == null)
                    return "Feature '" + featureName +
                           "' was not found.";

                if (!feature.Suppressed)
                    return "Feature '" + featureName +
                           "' is not suppressed.";

                if (!Confirm("Unsuppress feature: " +
                        featureName))
                    return "Unsuppress cancelled by user.";

                feature.Suppressed = false;

                return "Feature '" + featureName +
                       "' has been unsuppressed.";
            }
            catch (Exception ex)
            {
                return "Failed to unsuppress: " +
                       ex.Message;
            }
        }

        // ─── Create model state ───────────────────────────────────────

        private string HandleCreateModelState(string prompt)
        {
            try
            {
                string stateName =
                    prompt.Trim().Substring(19).Trim();

                if (string.IsNullOrWhiteSpace(stateName))
                    return "Provide a name. " +
                           "Example: create model state ALT-1";

                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (!Confirm("Create model state: " +
                        stateName))
                    return "Cancelled by user.";

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    ((PartDocument)doc).ComponentDefinition
                        .ModelStates.Add(stateName);
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    ((AssemblyDocument)doc).ComponentDefinition
                        .ModelStates.Add(stateName);
                }
                else
                {
                    return
                        "Model states are only available " +
                        "for Part and Assembly documents.";
                }

                return "Model state '" + stateName +
                       "' has been created.";
            }
            catch (Exception ex)
            {
                return "Failed to create model state: " +
                       ex.Message;
            }
        }

        // ─── Hide component ───────────────────────────────────────────

        private string HandleHideComponent(string prompt)
        {
            try
            {
                string compName =
                    prompt.Trim().Substring(5).Trim();

                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return
                        "Hide component is only available " +
                        "for Assembly documents.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;
                ComponentOccurrence occ =
                    FindComponent(asm, compName);

                if (occ == null)
                    return "Component '" + compName +
                           "' was not found.";

                if (!Confirm("Hide component: " + compName))
                    return "Cancelled by user.";

                occ.Visible = false;

                return "Component '" + compName +
                       "' is now hidden.";
            }
            catch (Exception ex)
            {
                return "Failed to hide component: " +
                       ex.Message;
            }
        }

        // ─── Show component ───────────────────────────────────────────

        private string HandleShowComponent(string prompt)
        {
            try
            {
                string compName =
                    prompt.Trim().Substring(5).Trim();

                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return
                        "Show component is only available " +
                        "for Assembly documents.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;
                ComponentOccurrence occ =
                    FindComponent(asm, compName);

                if (occ == null)
                    return "Component '" + compName +
                           "' was not found.";

                if (!Confirm("Show component: " + compName))
                    return "Cancelled by user.";

                occ.Visible = true;

                return "Component '" + compName +
                       "' is now visible.";
            }
            catch (Exception ex)
            {
                return "Failed to show component: " +
                       ex.Message;
            }
        }

        // ─── List features ────────────────────────────────────────────

        private string HandleListFeatures()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return
                        "List features is only available " +
                        "for Part documents.";

                PartDocument part = (PartDocument)doc;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Features in " +
                    doc.DisplayName + ":");
                sb.AppendLine(new string('-', 40));

                int i = 1;
                foreach (PartFeature f in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        string suppressed =
                            f.Suppressed ?
                            " [suppressed]" : "";
                        sb.AppendLine(
                            i + ". " + f.Name +
                            suppressed);
                        i++;
                    }
                    catch { }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list features: " +
                       ex.Message;
            }
        }

        // ─── List components ──────────────────────────────────────────

        private string HandleListComponents()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return
                        "List components is only available " +
                        "for Assembly documents.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Components in " +
                    doc.DisplayName + ":");
                sb.AppendLine(new string('-', 40));

                int i = 1;
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        string visible =
                            occ.Visible ? "" : " [hidden]";
                        sb.AppendLine(
                            i + ". " + occ.Name + visible);
                        i++;
                    }
                    catch { }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list components: " +
                       ex.Message;
            }
        }

        // ─── Check interference ───────────────────────────────────────

        private string HandleCheckInterference()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return
                        "Interference check is only " +
                        "available for Assembly documents.";

                _inventorApplication.CommandManager
                    .ControlDefinitions[
                        "AssemblyInterferenceCheckCmd"]
                    .Execute();

                return
                    "Interference check launched." +
                    System.Environment.NewLine +
                    "Review the results in the " +
                    "Interference Check dialog.";
            }
            catch (Exception ex)
            {
                return
                    "Failed to launch interference check: " +
                    ex.Message +
                    System.Environment.NewLine +
                    "Tip: make sure an Assembly is open.";
            }
        }

        // ─── Update mass ──────────────────────────────────────────────

        private string HandleUpdateMass()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    PartDocument part = (PartDocument)doc;
                    double mass =
                        part.ComponentDefinition
                            .MassProperties.Mass;
                    double vol =
                        part.ComponentDefinition
                            .MassProperties.Volume;
                    return
                        "Mass properties updated:" +
                        System.Environment.NewLine +
                        "Mass: " +
                        Math.Round(mass, 4) + " kg" +
                        System.Environment.NewLine +
                        "Volume: " +
                        Math.Round(vol * 1e6, 4) + " cm\u00b3";
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    AssemblyDocument asm =
                        (AssemblyDocument)doc;
                    double mass =
                        asm.ComponentDefinition
                           .MassProperties.Mass;
                    return "Assembly mass updated: " +
                           Math.Round(mass, 4) + " kg";
                }

                return "Mass update not available for " +
                       "this document type.";
            }
            catch (Exception ex)
            {
                return "Failed to update mass: " +
                       ex.Message;
            }
        }

        // ─── Rename part number ───────────────────────────────────────

        private string HandleRenamePartNumber(string prompt)
        {
            try
            {
                string newPartNumber =
                    prompt.Trim().Substring(19).Trim();

                if (string.IsNullOrWhiteSpace(newPartNumber))
                    return "Provide a part number. " +
                           "Example: rename part number ABC-001";

                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (!Confirm("Set Part Number to: " +
                        newPartNumber))
                    return "Cancelled by user.";

                Property partNumProp =
                    doc.PropertySets[
                        "{32853F0F-3444-11D1-9E93-0060B03C1CA6}"][
                        "Part Number"];

                string oldValue =
                    Convert.ToString(partNumProp.Value);

                partNumProp.Value = newPartNumber;

                return "Part Number updated from '" +
                       oldValue + "' to '" +
                       newPartNumber + "'.";
            }
            catch (Exception ex)
            {
                return "Failed to rename part number: " +
                       ex.Message;
            }
        }

        // ─── Set description ──────────────────────────────────────────

        private string HandleSetDescription(string prompt)
        {
            try
            {
                string newDescription =
                    prompt.Trim().Substring(16).Trim();

                if (string.IsNullOrWhiteSpace(newDescription))
                    return "Provide a description. " +
                           "Example: set description " +
                           "HVAC Bracket";

                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (!Confirm("Set Description to: " +
                        newDescription))
                    return "Cancelled by user.";

                Property descProp =
                    doc.PropertySets[
                        "{32853F0F-3444-11D1-9E93-0060B03C1CA6}"][
                        "Description"];

                string oldValue =
                    Convert.ToString(descProp.Value);

                descProp.Value = newDescription;

                return "Description updated from '" +
                       oldValue + "' to '" +
                       newDescription + "'.";
            }
            catch (Exception ex)
            {
                return "Failed to set description: " +
                       ex.Message;
            }
        }

        // ─── Export DXF ───────────────────────────────────────────────

        private string HandleExportDxf()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject &&
                    doc.DocumentType !=
                    DocumentTypeEnum.kDrawingDocumentObject)
                    return "DXF export requires an active " +
                           "Part or Drawing document.";

                string defaultPath =
                    System.IO.Path.ChangeExtension(
                        doc.FullFileName, ".dxf");

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Export as DXF";
                    dlg.Filter =
                        "DXF Files (*.dxf)|*.dxf";
                    dlg.FileName =
                        System.IO.Path.GetFileName(
                            defaultPath);
                    dlg.InitialDirectory =
                        System.IO.Path.GetDirectoryName(
                            doc.FullFileName);

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return "Export cancelled by user.";

                    string savePath = dlg.FileName;

                    TranslatorAddIn dxfTranslator = null;
                    foreach (ApplicationAddIn addIn in
                        _inventorApplication
                            .ApplicationAddIns)
                    {
                        try
                        {
                            if (addIn.ClassIdString ==
                                "{C24E3AC2-122E-11D5-8E91-0010B541CD80}")
                            {
                                dxfTranslator =
                                    addIn as TranslatorAddIn;
                                break;
                            }
                        }
                        catch { }
                    }

                    if (dxfTranslator == null)
                        return
                            "DXF translator add-in " +
                            "not found." +
                            System.Environment.NewLine +
                            "Make sure the Inventor " +
                            "DXF/DWG export add-in " +
                            "is installed.";

                    TranslationContext context =
                        _inventorApplication
                            .TransientObjects
                            .CreateTranslationContext();
                    context.Type =
                        IOMechanismEnum
                            .kFileBrowseIOMechanism;

                    NameValueMap options =
                        _inventorApplication
                            .TransientObjects
                            .CreateNameValueMap();

                    DataMedium dataMedium =
                        _inventorApplication
                            .TransientObjects
                            .CreateDataMedium();
                    dataMedium.FileName = savePath;

                    dxfTranslator.SaveCopyAs(
                        doc, context, options, dataMedium);

                    return "DXF exported to:" +
                           System.Environment.NewLine +
                           savePath;
                }
            }
            catch (Exception ex)
            {
                return "Failed to export DXF: " +
                       ex.Message;
            }
        }

        // ─── Export PDF ───────────────────────────────────────────────

        private string HandleExportPdf()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kDrawingDocumentObject)
                    return "PDF export requires an active " +
                           "Drawing document.";

                string defaultPath =
                    System.IO.Path.ChangeExtension(
                        doc.FullFileName, ".pdf");

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Export Drawing as PDF";
                    dlg.Filter =
                        "PDF Files (*.pdf)|*.pdf";
                    dlg.FileName =
                        System.IO.Path.GetFileName(
                            defaultPath);
                    dlg.InitialDirectory =
                        System.IO.Path.GetDirectoryName(
                            doc.FullFileName);

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return "Export cancelled by user.";

                    string savePath = dlg.FileName;

                    TranslatorAddIn pdfTranslator = null;
                    foreach (ApplicationAddIn addIn in
                        _inventorApplication
                            .ApplicationAddIns)
                    {
                        try
                        {
                            if (addIn.ClassIdString ==
                                "{0AC6FD96-2F4D-42CE-8BE0-8AEA580399E4}")
                            {
                                pdfTranslator =
                                    addIn as TranslatorAddIn;
                                break;
                            }
                        }
                        catch { }
                    }

                    if (pdfTranslator == null)
                        return
                            "PDF translator add-in " +
                            "not found.";

                    TranslationContext context =
                        _inventorApplication
                            .TransientObjects
                            .CreateTranslationContext();
                    context.Type =
                        IOMechanismEnum
                            .kFileBrowseIOMechanism;

                    NameValueMap options =
                        _inventorApplication
                            .TransientObjects
                            .CreateNameValueMap();

                    DataMedium dataMedium =
                        _inventorApplication
                            .TransientObjects
                            .CreateDataMedium();
                    dataMedium.FileName = savePath;

                    pdfTranslator.SaveCopyAs(
                        doc, context, options, dataMedium);

                    return "PDF exported to:" +
                           System.Environment.NewLine +
                           savePath;
                }
            }
            catch (Exception ex)
            {
                return "Failed to export PDF: " +
                       ex.Message;
            }
        }

        // ─── Generate parameters ──────────────────────────────────────

        private string HandleGenerateParameters(
            string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                Parameters parameters =
                    GetParameters(doc);

                if (parameters == null)
                    return "Parameters not available for " +
                           "this document type.";

                string paramSection =
                    prompt.Trim()
                          .ToLower()
                          .Replace(
                              "generate parameters", "")
                          .Trim();

                if (string.IsNullOrWhiteSpace(paramSection))
                    return "Provide parameters to generate." +
                           " Example: generate parameters " +
                           "Width=100 Height=200";

                string[] pairs = paramSection.Split(
                    new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                var created =
                    new System.Collections.Generic
                        .List<string>();
                var skipped =
                    new System.Collections.Generic
                        .List<string>();

                foreach (string pair in pairs)
                {
                    if (!pair.Contains("=")) continue;

                    string[] parts = pair.Split('=');
                    if (parts.Length != 2) continue;

                    string name = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (string.IsNullOrWhiteSpace(name) ||
                        string.IsNullOrWhiteSpace(value))
                        continue;

                    try
                    {
                        parameters.UserParameters
                            .AddByExpression(
                                name,
                                value,
                                UnitsTypeEnum
                                    .kDefaultDisplayLengthUnits);
                        created.Add(name + " = " + value);
                    }
                    catch { skipped.Add(name); }
                }

                if (created.Count == 0)
                    return "No parameters were created. " +
                           "Use format: generate parameters " +
                           "Width=100 Height=200";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Parameters created:");
                foreach (string c in created)
                    sb.AppendLine("  \u2705 " + c);

                if (skipped.Count > 0)
                {
                    sb.AppendLine(
                        "Skipped (already exist or invalid):");
                    foreach (string s in skipped)
                        sb.AppendLine("  \u26a0\ufe0f " + s);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to generate parameters: " +
                       ex.Message;
            }
        }

        // ─── Open iProperties panel ───────────────────────────────────

        private string OpenIPropertiesPanel()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                _inventorApplication.CommandManager
                    .ControlDefinitions[
                        "AppFilePropertiesCmd"]
                    .Execute();

                return
                    "\u2705 iProperties panel opened for " +
                    doc.DisplayName + "." +
                    System.Environment.NewLine +
                    "Tip: Type 'set part number to FPB-001'" +
                    " to write iProperties directly " +
                    "from chat.";
            }
            catch (Exception ex)
            {
                return "Failed to open iProperties: " +
                       ex.Message;
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────

        private bool Confirm(string message)
        {
            DialogResult result = MessageBox.Show(
                message +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "Proceed?",
                "AI Assistant \u2014 Confirm Action",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return result == DialogResult.Yes;
        }

        private Parameters GetParameters(Document doc)
        {
            if (doc.DocumentType ==
                DocumentTypeEnum.kPartDocumentObject)
                return ((PartDocument)doc)
                    .ComponentDefinition.Parameters;

            if (doc.DocumentType ==
                DocumentTypeEnum.kAssemblyDocumentObject)
                return ((AssemblyDocument)doc)
                    .ComponentDefinition.Parameters;

            return null;
        }

        private PartFeature FindPartFeature(string name)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return null;

                PartDocument part = (PartDocument)doc;
                foreach (PartFeature f in
                    part.ComponentDefinition.Features)
                {
                    if (string.Equals(
                        f.Name, name,
                        StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            catch { }

            return null;
        }

        private ComponentOccurrence FindComponent(
            AssemblyDocument asm, string name)
        {
            try
            {
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    if (string.Equals(
                        occ.Name, name,
                        StringComparison.OrdinalIgnoreCase))
                        return occ;
                }
            }
            catch { }

            return null;
        }
    }
}
