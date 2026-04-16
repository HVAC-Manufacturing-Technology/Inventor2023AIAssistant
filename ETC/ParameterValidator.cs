using System;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class ParameterValidator
    {
        private Inventor.Application _inventorApplication;
        private const double CmToIn = 0.393701;

        public ParameterValidator(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleValidation(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("validate") ||
                n.Contains("validation") ||
                n.Contains("check parameter") ||
                n.Contains("parameter check") ||
                n.Contains("check for zero") ||
                n.Contains("check dimension") ||
                n.Contains("pre-release check") ||
                n.Contains("release check"))
                return ValidateParameters();

            if (n.Contains("check material is set") ||
                n.Contains("material set"))
                return CheckMaterialSet();

            if (n.Contains("check part number is set") ||
                n.Contains("part number set"))
                return CheckPartNumberSet();

            if (n.Contains("check description is set") ||
                n.Contains("description set"))
                return CheckDescriptionSet();

            return null;
        }

        // ─── Full validation ──────────────────────────────────────────

        public string ValidateParameters()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                var sb = new StringBuilder();
                sb.AppendLine("── Parameter Validation ──");
                sb.AppendLine(
                    "Document: " + doc.DisplayName);
                sb.AppendLine(new string('═', 40));

                int issues = 0;
                int warnings = 0;
                int passed = 0;

                // ── iProperty checks ──────────────────────

                sb.AppendLine();
                sb.AppendLine("iProperties:");

                // Part number
                try
                {
                    string pn = Convert.ToString(
                        doc.PropertySets[
                            "{32853F0F-3444-11D1-" +
                            "9E93-0060B03C1CA6}"][
                            "Part Number"].Value);

                    if (string.IsNullOrWhiteSpace(pn))
                    {
                        sb.AppendLine(
                            "  ❌ Part Number is empty");
                        issues++;
                    }
                    else
                    {
                        sb.AppendLine(
                            "  ✅ Part Number: " + pn);
                        passed++;
                    }
                }
                catch
                {
                    sb.AppendLine(
                        "  ❌ Part Number not found");
                    issues++;
                }

                // Description
                try
                {
                    string desc = Convert.ToString(
                        doc.PropertySets[
                            "{32853F0F-3444-11D1-" +
                            "9E93-0060B03C1CA6}"][
                            "Description"].Value);

                    if (string.IsNullOrWhiteSpace(desc))
                    {
                        sb.AppendLine(
                            "  ⚠️ Description is empty");
                        warnings++;
                    }
                    else
                    {
                        sb.AppendLine(
                            "  ✅ Description: " + desc);
                        passed++;
                    }
                }
                catch
                {
                    sb.AppendLine(
                        "  ⚠️ Description not found");
                    warnings++;
                }

                // ── Material check ────────────────────────

                sb.AppendLine();
                sb.AppendLine("Material:");

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    try
                    {
                        string mat =
                            ((PartDocument)doc)
                                .ComponentDefinition
                                .Material.Name;

                        if (string.IsNullOrWhiteSpace(mat) ||
                            mat.ToLower() == "generic" ||
                            mat.ToLower() == "default")
                        {
                            sb.AppendLine(
                                "  ⚠️ Material is generic: " +
                                mat);
                            warnings++;
                        }
                        else
                        {
                            sb.AppendLine(
                                "  ✅ Material: " + mat);
                            passed++;
                        }
                    }
                    catch
                    {
                        sb.AppendLine(
                            "  ❌ Material not set");
                        issues++;
                    }
                }

                // ── Parameter checks ──────────────────────

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject ||
                    doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    sb.AppendLine();
                    sb.AppendLine("Parameters:");

                    Parameters parameters = null;

                    if (doc.DocumentType ==
                        DocumentTypeEnum.kPartDocumentObject)
                        parameters = ((PartDocument)doc)
                            .ComponentDefinition.Parameters;
                    else
                        parameters = ((AssemblyDocument)doc)
                            .ComponentDefinition.Parameters;

                    foreach (Parameter param in parameters)
                    {
                        try
                        {
                            // Skip non-user parameters
                            if (!(param is
                                UserParameter)) continue;

                            double val = param.Value;

                            // Check for zero
                            if (Math.Abs(val) < 0.0001)
                            {
                                sb.AppendLine(
                                    "  ❌ " + param.Name +
                                    " = 0 (zero value)");
                                issues++;
                                continue;
                            }

                            // Check for negative
                            if (val < 0)
                            {
                                sb.AppendLine(
                                    "  ❌ " + param.Name +
                                    " = " +
                                    Math.Round(val, 4) +
                                    " (negative value)");
                                issues++;
                                continue;
                            }

                            // Check for suspiciously
                            // large values
                            string units =
                                param.get_Units()
                                    .ToLowerInvariant();

                            if (units.Contains("cm") ||
                                units.Contains("in") ||
                                units.Contains("mm"))
                            {
                                double valIn =
                                    units.Contains("cm")
                                        ? val * CmToIn
                                        : units.Contains("mm")
                                            ? val / 25.4
                                            : val;

                                if (valIn > 500)
                                {
                                    sb.AppendLine(
                                        "  ⚠️ " + param.Name +
                                        " = " +
                                        Math.Round(
                                            valIn, 3) +
                                        " in (unusually large)");
                                    warnings++;
                                    continue;
                                }
                            }

                            sb.AppendLine(
                                "  ✅ " + param.Name +
                                " = " +
                                param.Expression);
                            passed++;
                        }
                        catch { }
                    }
                }

                // ── Summary ───────────────────────────────

                sb.AppendLine();
                sb.AppendLine(new string('═', 40));
                sb.AppendLine("Summary:");
                sb.AppendLine(
                    "  ✅ Passed:   " + passed);
                sb.AppendLine(
                    "  ⚠️ Warnings: " + warnings);
                sb.AppendLine(
                    "  ❌ Issues:   " + issues);
                sb.AppendLine();

                if (issues == 0 && warnings == 0)
                    sb.AppendLine(
                        "✅ All checks passed. " +
                        "Ready for release.");
                else if (issues == 0)
                    sb.AppendLine(
                        "⚠️ No critical issues. " +
                        "Review warnings before release.");
                else
                    sb.AppendLine(
                        "❌ " + issues +
                        " issue(s) must be fixed " +
                        "before release.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to validate: " + ex.Message;
            }
        }

        private string CheckMaterialSet()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Material check requires " +
                           "a Part document.";

                string mat =
                    ((PartDocument)doc)
                        .ComponentDefinition
                        .Material.Name;

                if (string.IsNullOrWhiteSpace(mat) ||
                    mat.ToLower() == "generic")
                    return "❌ Material is not set. " +
                           "Say 'set material galvanized " +
                           "steel' to fix.";

                return "✅ Material is set: " + mat;
            }
            catch (Exception ex)
            {
                return "Failed to check material: " +
                       ex.Message;
            }
        }

        private string CheckPartNumberSet()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                string pn = Convert.ToString(
                    doc.PropertySets[
                        "{32853F0F-3444-11D1-" +
                        "9E93-0060B03C1CA6}"][
                        "Part Number"].Value);

                if (string.IsNullOrWhiteSpace(pn))
                    return "❌ Part Number is empty. " +
                           "Say 'rename part number " +
                           "FPB-001' to fix.";

                return "✅ Part Number is set: " + pn;
            }
            catch (Exception ex)
            {
                return "Failed to check part number: " +
                       ex.Message;
            }
        }

        private string CheckDescriptionSet()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                string desc = Convert.ToString(
                    doc.PropertySets[
                        "{32853F0F-3444-11D1-" +
                        "9E93-0060B03C1CA6}"][
                        "Description"].Value);

                if (string.IsNullOrWhiteSpace(desc))
                    return "❌ Description is empty. " +
                           "Say 'set description Fan " +
                           "Powered Box' to fix.";

                return "✅ Description is set: " + desc;
            }
            catch (Exception ex)
            {
                return "Failed to check description: " +
                       ex.Message;
            }
        }
    }
}