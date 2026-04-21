using System;
using System.Collections.Generic;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class DesignPackageChecker
    {
        private readonly Inventor.Application _app;

        public DesignPackageChecker(
            Inventor.Application app)
        {
            _app = app;
        }

        public string CheckDesignPackage()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "❌ No active document.";

                if (doc.DocumentType ==
                    DocumentTypeEnum
                    .kPartDocumentObject)
                    return CheckPart(
                        (PartDocument)doc);

                if (doc.DocumentType ==
                    DocumentTypeEnum
                    .kAssemblyDocumentObject)
                    return CheckAssembly(
                        (AssemblyDocument)doc);

                return
                    "❌ Please open a Part or " +
                    "Assembly document.";
            }
            catch (Exception ex)
            {
                return
                    $"❌ Check failed:\n" +
                    $"{ex.Message}";
            }
        }

        private string CheckPart(
            PartDocument part)
        {
            var issues = new List<string>();
            var passed = new List<string>();

            CheckIProperties(
                part, issues, passed);
            CheckMaterial(
                part, issues, passed);
            CheckSaved(
                part, issues, passed);

            if (part.ComponentDefinition
                is SheetMetalComponentDefinition)
                CheckSheetMetal(
                    part, issues, passed);

            return FormatReport(
                part.DisplayName,
                issues, passed);
        }

        private string CheckAssembly(
            AssemblyDocument asm)
        {
            var issues = new List<string>();
            var passed = new List<string>();
            var partIssues =
                new List<string>();

            CheckIPropertiesAsm(
                asm, issues, passed);
            CheckSavedAsm(
                asm, issues, passed);

            int partCount = 0;
            int issueCount = 0;

            try
            {
                foreach (ComponentOccurrence occ
                    in asm.ComponentDefinition
                    .Occurrences)
                {
                    try
                    {
                        Document refDoc =
                            occ.Definition
                            .Document as Document;

                        if (refDoc == null ||
                            refDoc.DocumentType !=
                            DocumentTypeEnum
                            .kPartDocumentObject)
                            continue;

                        partCount++;
                        PartDocument part =
                            (PartDocument)refDoc;

                        var pIssues =
                            new List<string>();
                        var pPassed =
                            new List<string>();

                        CheckIProperties(
                            part, pIssues, pPassed);
                        CheckMaterial(
                            part, pIssues, pPassed);

                        if (pIssues.Count > 0)
                        {
                            issueCount++;
                            partIssues.Add(
                                $"\n📄 " +
                                $"{part.DisplayName}:");
                            foreach (var issue
                                in pIssues)
                                partIssues.Add(
                                    $"   {issue}");
                        }
                    }
                    catch { }
                }
            }
            catch { }

            var sb = new StringBuilder();
            sb.AppendLine(
                "📦 Design Package Check");
            sb.AppendLine(
                $"Assembly: {asm.DisplayName}");
            sb.AppendLine(
                new string('─', 35));

            foreach (var p in passed)
                sb.AppendLine(p);

            if (issues.Count > 0)
            {
                sb.AppendLine();
                foreach (var i in issues)
                    sb.AppendLine(i);
            }

            sb.AppendLine(
                new string('─', 35));
            sb.AppendLine(
                $"Parts checked: {partCount}");
            sb.AppendLine(
                $"Parts with issues: " +
                $"{issueCount}");

            if (partIssues.Count > 0)
            {
                sb.AppendLine("\nPart Issues:");
                foreach (var p in partIssues)
                    sb.AppendLine(p);
            }
            else if (partCount > 0)
                sb.AppendLine(
                    "\n✅ All parts passed!");

            sb.AppendLine(
                new string('─', 35));
            int total =
                issues.Count + issueCount;
            sb.AppendLine(total == 0
                ? "✅ Package ready!"
                : $"⚠️ {total} issue(s) found.");

            return sb.ToString();
        }

        private void CheckIProperties(
            PartDocument part,
            List<string> issues,
            List<string> passed)
        {
            string designGuid =
                "{32853F0F-3444-11D1-" +
                "9E93-0060B03C1CA6}";

            // Part Number
            try
            {
                string pn = Convert.ToString(
                    part.PropertySets[designGuid]
                    ["Part Number"].Value);
                if (string.IsNullOrWhiteSpace(pn))
                    issues.Add(
                        "❌ Part Number is empty");
                else
                    passed.Add(
                        $"✅ Part Number: {pn}");
            }
            catch
            {
                issues.Add(
                    "❌ Part Number missing");
            }

            // Description
            try
            {
                string desc = Convert.ToString(
                    part.PropertySets[designGuid]
                    ["Description"].Value);
                if (string.IsNullOrWhiteSpace(desc))
                    issues.Add(
                        "❌ Description is empty");
                else
                    passed.Add(
                        $"✅ Description: {desc}");
            }
            catch
            {
                issues.Add(
                    "❌ Description missing");
            }

            // Revision
            try
            {
                string rev = Convert.ToString(
                    part.PropertySets[designGuid]
                    ["Revision Number"].Value);
                if (string.IsNullOrWhiteSpace(rev))
                    issues.Add(
                        "⚠️ Revision is empty");
                else
                    passed.Add(
                        $"✅ Revision: {rev}");
            }
            catch
            {
                issues.Add(
                    "⚠️ Revision missing");
            }
        }

        private void CheckIPropertiesAsm(
            AssemblyDocument asm,
            List<string> issues,
            List<string> passed)
        {
            string designGuid =
                "{32853F0F-3444-11D1-" +
                "9E93-0060B03C1CA6}";

            try
            {
                string pn = Convert.ToString(
                    asm.PropertySets[designGuid]
                    ["Part Number"].Value);
                if (string.IsNullOrWhiteSpace(pn))
                    issues.Add(
                        "❌ Part Number is empty");
                else
                    passed.Add(
                        $"✅ Part Number: {pn}");
            }
            catch
            {
                issues.Add(
                    "❌ Part Number missing");
            }

            try
            {
                string desc = Convert.ToString(
                    asm.PropertySets[designGuid]
                    ["Description"].Value);
                if (string.IsNullOrWhiteSpace(desc))
                    issues.Add(
                        "❌ Description is empty");
                else
                    passed.Add(
                        $"✅ Description: {desc}");
            }
            catch
            {
                issues.Add(
                    "❌ Description missing");
            }

            try
            {
                string rev = Convert.ToString(
                    asm.PropertySets[designGuid]
                    ["Revision Number"].Value);
                if (string.IsNullOrWhiteSpace(rev))
                    issues.Add(
                        "⚠️ Revision is empty");
                else
                    passed.Add(
                        $"✅ Revision: {rev}");
            }
            catch
            {
                issues.Add(
                    "⚠️ Revision missing");
            }
        }

        private void CheckMaterial(
            PartDocument part,
            List<string> issues,
            List<string> passed)
        {
            try
            {
                string mat =
                    part.ComponentDefinition
                    .Material.Name;
                if (string.IsNullOrWhiteSpace(mat)
                    || mat == "Generic")
                    issues.Add(
                        "⚠️ Material is Generic");
                else
                    passed.Add(
                        $"✅ Material: {mat}");
            }
            catch
            {
                issues.Add(
                    "❌ Material not assigned");
            }
        }

        private void CheckSaved(
            PartDocument part,
            List<string> issues,
            List<string> passed)
        {
            try
            {
                if (part.Dirty)
                    issues.Add(
                        "⚠️ Unsaved changes");
                else
                    passed.Add(
                        "✅ Document is saved");
            }
            catch { }
        }

        private void CheckSavedAsm(
            AssemblyDocument asm,
            List<string> issues,
            List<string> passed)
        {
            try
            {
                if (asm.Dirty)
                    issues.Add(
                        "⚠️ Unsaved changes");
                else
                    passed.Add(
                        "✅ Document is saved");
            }
            catch { }
        }

        private void CheckSheetMetal(
            PartDocument part,
            List<string> issues,
            List<string> passed)
        {
            try
            {
                SheetMetalComponentDefinition smDef =
                    (SheetMetalComponentDefinition)
                    part.ComponentDefinition;

                // Thickness
                try
                {
                    string t =
                        smDef.ActiveSheetMetalStyle
                        ?.Thickness ?? "";
                    if (string.IsNullOrWhiteSpace(t))
                        issues.Add(
                            "⚠️ Thickness not set");
                    else
                        passed.Add(
                            $"✅ Thickness: {t}");
                }
                catch
                {
                    issues.Add(
                        "⚠️ Could not read " +
                        "thickness");
                }

                // Flat pattern
                try
                {
                    bool hasFlatPattern = false;
                    foreach (PartFeature feat in
                        part.ComponentDefinition
                        .Features)
                    {
                        if (feat is FlatPattern)
                        {
                            hasFlatPattern = true;
                            break;
                        }
                    }

                    if (hasFlatPattern)
                        passed.Add(
                            "✅ Flat pattern exists");
                    else
                        issues.Add(
                            "⚠️ No flat pattern");
                }
                catch { }
            }
            catch { }
        }

        private string FormatReport(
            string docName,
            List<string> issues,
            List<string> passed)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "📦 Design Package Check");
            sb.AppendLine($"Part: {docName}");
            sb.AppendLine(
                new string('─', 35));

            foreach (var p in passed)
                sb.AppendLine(p);

            if (issues.Count > 0)
            {
                sb.AppendLine();
                foreach (var i in issues)
                    sb.AppendLine(i);
            }

            sb.AppendLine(
                new string('─', 35));
            sb.AppendLine(issues.Count == 0
                ? "✅ Package ready!"
                : $"⚠️ {issues.Count} " +
                  $"issue(s) found.");

            return sb.ToString();
        }
    }
}