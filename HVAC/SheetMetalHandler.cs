using System;
using System.Text;
using System.Text.RegularExpressions;
using Inventor;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace Inventor2023AIAssistant
{
    public class SheetMetalHandler
    {
        private Inventor.Application _inventorApplication;
        private const double InToCm = 2.54;

        public SheetMetalHandler(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        public string TryHandleSheetMetal(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if ((n.Contains("new") ||
                 n.Contains("create")) &&
                n.Contains("sheet metal"))
                return NewSheetMetalPart();

            if (n.Contains("set thickness") ||
                (n.Contains("thickness") &&
                 n.Contains("set")))
                return SetThickness(prompt);

            if (n.Contains("gauge") &&
                (n.Contains("set") ||
                 n.Contains("change")))
                return SetGauge(prompt);

            if (n.Contains("set material") ||
                (n.Contains("material") &&
                 n.Contains("set")))
                return SetSheetMetalMaterial(prompt);

            if (n.Contains("bend radius") ||
                (n.Contains("bend") &&
                 n.Contains("radius")))
                return SetBendRadius(prompt);

            if (n.Contains("k-factor") ||
                n.Contains("k factor") ||
                n.Contains("kfactor"))
                return SetKFactor(prompt);

            if (n.Contains("flat pattern") ||
                n.Contains("flatten") ||
                n.Contains("unfold"))
                return CreateFlatPattern();

            if (n.Contains("sheet metal info") ||
                n.Contains("show sheet metal") ||
                n.Contains("sheet metal settings"))
                return ShowSheetMetalInfo();

            if (n.Contains("gauge table") ||
                n.Contains("gauge chart") ||
                n.Contains("gauge reference") ||
                n.Contains("what gauge") ||
                (n.Contains("gauge") &&
                 n.Contains("thickness")))
                return ShowGaugeTable();

            return null;
        }

        private string NewSheetMetalPart()
        {
            try
            {
                string templatePath = "";

                try
                {
                    string standardTemplate =
                        _inventorApplication.FileManager
                            .GetTemplateFile(
                                DocumentTypeEnum
                                    .kPartDocumentObject);

                    string dir =
                        IOPath.GetDirectoryName(
                            standardTemplate);

                    string smTemplate =
                        IOPath.Combine(
                            dir, "Sheet Metal.ipt");

                    if (IOFile.Exists(smTemplate))
                        templatePath = smTemplate;
                    else
                        templatePath = standardTemplate;
                }
                catch
                {
                    templatePath =
                        _inventorApplication.FileManager
                            .GetTemplateFile(
                                DocumentTypeEnum
                                    .kPartDocumentObject);
                }

                Document doc =
                    _inventorApplication.Documents.Add(
                        DocumentTypeEnum.kPartDocumentObject,
                        templatePath);

                return
                    "✅ New Sheet Metal part created: " +
                    doc.DisplayName +
                    System.Environment.NewLine +
                    "Say 'show sheet metal settings' to " +
                    "see thickness and bend radius." +
                    System.Environment.NewLine +
                    "Say 'set thickness 0.040' to change.";
            }
            catch (Exception ex)
            {
                return
                    "Failed to create sheet metal part: " +
                    ex.Message;
            }
        }

        private string SetThickness(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sheet metal commands require " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                double thickness =
                    ParseValue(prompt, 0.040);
                bool set = false;

                foreach (Parameter param in
                    part.ComponentDefinition.Parameters)
                {
                    try
                    {
                        string name =
                            param.Name.ToLowerInvariant();

                        if (name == "thickness" ||
                            name == "sheetmetalthickness" ||
                            name == "sheet_thickness" ||
                            name == "t")
                        {
                            param.Expression =
                                (thickness * InToCm) + " cm";
                            set = true;
                            return
                                "✅ Thickness set to " +
                                thickness + " in" +
                                System.Environment.NewLine +
                                "(" +
                                GaugeFromThickness(
                                    thickness) + ")" +
                                System.Environment.NewLine +
                                "Parameter: " + param.Name;
                        }
                    }
                    catch { }
                }

                if (!set)
                    return
                        "Thickness parameter not found." +
                        System.Environment.NewLine +
                        "Set via Sheet Metal tab > " +
                        "Sheet Metal Defaults > Thickness: " +
                        thickness + " in" +
                        System.Environment.NewLine +
                        "(" +
                        GaugeFromThickness(thickness) + ")" +
                        System.Environment.NewLine +
                        "Or add a parameter named " +
                        "'Thickness' and try again.";

                return "Thickness updated.";
            }
            catch (Exception ex)
            {
                return "Failed to set thickness: " +
                       ex.Message;
            }
        }

        private string SetGauge(string prompt)
        {
            try
            {
                double gaugeNum = ParseValue(prompt, 20.0);
                double thickness =
                    ThicknessFromGauge(gaugeNum);

                if (thickness <= 0)
                    return "Unknown gauge: " + gaugeNum +
                           System.Environment.NewLine +
                           "Valid HVAC gauges: " +
                           "16, 18, 20, 22, 24, 26";

                string result =
                    SetThickness(
                        "set thickness " + thickness);

                return
                    "Gauge " + gaugeNum + " selected." +
                    System.Environment.NewLine +
                    "Thickness: " + thickness + " in" +
                    System.Environment.NewLine + result;
            }
            catch (Exception ex)
            {
                return "Failed to set gauge: " + ex.Message;
            }
        }

        private string SetSheetMetalMaterial(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sheet metal commands require " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                string n = prompt.ToLowerInvariant();
                string materialName = "Galvanized Steel";

                if (n.Contains("stainless") ||
                    n.Contains("304"))
                    materialName = "Steel, Stainless 304";
                else if (n.Contains("aluminum") ||
                         n.Contains("aluminium"))
                    materialName = "Aluminum-6061";
                else if (n.Contains("mild") ||
                         n.Contains("carbon"))
                    materialName = "Steel, Mild";
                else if (n.Contains("copper"))
                    materialName = "Copper, Alloy 110";

                try
                {
                    part.PropertySets[
                        "Design Tracking Properties"][
                        "Material"].Value = materialName;

                    return
                        "✅ Material set to: " +
                        materialName +
                        System.Environment.NewLine +
                        "Verify in the Material dialog " +
                        "if rendering is needed.";
                }
                catch (Exception ex2)
                {
                    return
                        "Could not set material: " +
                        ex2.Message +
                        System.Environment.NewLine +
                        "Set it manually via the " +
                        "Material dropdown in Inventor.";
                }
            }
            catch (Exception ex)
            {
                return "Failed to set material: " +
                       ex.Message;
            }
        }

        private string SetBendRadius(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sheet metal commands require " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                double radius = ParseValue(prompt, 0.125);
                bool set = false;

                foreach (Parameter param in
                    part.ComponentDefinition.Parameters)
                {
                    try
                    {
                        string name =
                            param.Name.ToLowerInvariant();

                        if (name == "bendradius" ||
                            name == "bend_radius" ||
                            name == "br" ||
                            name == "innerradius")
                        {
                            param.Expression =
                                (radius * InToCm) + " cm";
                            set = true;
                            return
                                "✅ Bend radius set to " +
                                radius + " in" +
                                System.Environment.NewLine +
                                "Parameter: " + param.Name;
                        }
                    }
                    catch { }
                }

                if (!set)
                    return
                        "Bend radius parameter not found." +
                        System.Environment.NewLine +
                        "Set via Sheet Metal tab > " +
                        "Sheet Metal Defaults > " +
                        "Bend Radius: " + radius + " in" +
                        System.Environment.NewLine +
                        "Or add a parameter named " +
                        "'BendRadius' and try again.";

                return "Bend radius updated.";
            }
            catch (Exception ex)
            {
                return "Failed to set bend radius: " +
                       ex.Message;
            }
        }

        private string SetKFactor(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sheet metal commands require " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                double kfactor = ParseValue(prompt, 0.44);
                bool set = false;

                foreach (Parameter param in
                    part.ComponentDefinition.Parameters)
                {
                    try
                    {
                        string name =
                            param.Name.ToLowerInvariant();

                        if (name == "kfactor" ||
                            name == "k_factor" ||
                            name == "k")
                        {
                            param.Expression =
                                kfactor.ToString("0.000") +
                                " ul";
                            set = true;
                            return
                                "✅ K-factor set to " +
                                kfactor +
                                System.Environment.NewLine +
                                "Parameter: " + param.Name;
                        }
                    }
                    catch { }
                }

                if (!set)
                    return
                        "K-factor parameter not found." +
                        System.Environment.NewLine +
                        "Set via Sheet Metal tab > " +
                        "Sheet Metal Defaults > " +
                        "Unfold Rule > K-factor: " +
                        kfactor +
                        System.Environment.NewLine +
                        "Common K-factors:" +
                        System.Environment.NewLine +
                        "Soft:          0.35" +
                        System.Environment.NewLine +
                        "Semi-hard:     0.41" +
                        System.Environment.NewLine +
                        "Hard:          0.50" +
                        System.Environment.NewLine +
                        "HVAC standard: 0.44";

                return "K-factor updated.";
            }
            catch (Exception ex)
            {
                return "Failed to set K-factor: " +
                       ex.Message;
            }
        }

        private string CreateFlatPattern()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Flat pattern requires " +
                           "a Part document.";

                _inventorApplication.CommandManager
                    .ControlDefinitions[
                        "SheetMetalFlatPatternCmd"]
                    .Execute();

                return
                    "✅ Flat pattern command launched." +
                    System.Environment.NewLine +
                    "The flat pattern view will appear " +
                    "in the model browser." +
                    System.Environment.NewLine +
                    "Say 'export dxf' to export it.";
            }
            catch (Exception ex)
            {
                return
                    "Failed to create flat pattern: " +
                    ex.Message +
                    System.Environment.NewLine +
                    "Make sure the part is a Sheet Metal " +
                    "part with at least one bend.";
            }
        }

        private string ShowSheetMetalInfo()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sheet metal info requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                var sb = new StringBuilder();
                sb.AppendLine("── Sheet Metal Settings ──");
                sb.AppendLine("Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                double thickness = -1;
                double bendRadius = -1;
                double kFactor = -1;

                foreach (Parameter param in
                    part.ComponentDefinition.Parameters)
                {
                    try
                    {
                        string name =
                            param.Name.ToLowerInvariant();

                        if (name == "thickness" ||
                            name == "sheetmetalthickness" ||
                            name == "t")
                            thickness =
                                param.Value / InToCm;
                        else if (name == "bendradius" ||
                                 name == "bend_radius" ||
                                 name == "br")
                            bendRadius =
                                param.Value / InToCm;
                        else if (name == "kfactor" ||
                                 name == "k_factor" ||
                                 name == "k")
                            kFactor = param.Value;
                    }
                    catch { }
                }

                sb.AppendLine(thickness > 0
                    ? "Thickness:   " +
                      Math.Round(thickness, 4) + " in (" +
                      GaugeFromThickness(thickness) + ")"
                    : "Thickness:   not found in parameters");

                sb.AppendLine(bendRadius > 0
                    ? "Bend Radius: " +
                      Math.Round(bendRadius, 4) + " in"
                    : "Bend Radius: not found in parameters");

                sb.AppendLine(kFactor > 0
                    ? "K-Factor:    " +
                      Math.Round(kFactor, 3)
                    : "K-Factor:    not found in parameters");

                try
                {
                    sb.AppendLine("Material:    " +
                        part.ComponentDefinition
                            .Material.Name);
                }
                catch
                {
                    sb.AppendLine("Material:    not set");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read sheet metal info: " +
                       ex.Message;
            }
        }

        private string ShowGaugeTable()
        {
            return
                "── HVAC Sheet Metal Gauge Reference ──" +
                System.Environment.NewLine +
                new string('-', 45) +
                System.Environment.NewLine +
                "Gauge".PadRight(8) +
                "Thickness (in)".PadRight(18) +
                "Thickness (mm)" +
                System.Environment.NewLine +
                new string('-', 45) +
                System.Environment.NewLine +
                "10".PadRight(8) + "0.1345".PadRight(18) +
                "3.416" + System.Environment.NewLine +
                "12".PadRight(8) + "0.1084".PadRight(18) +
                "2.753" + System.Environment.NewLine +
                "14".PadRight(8) + "0.0747".PadRight(18) +
                "1.897" + System.Environment.NewLine +
                "16".PadRight(8) + "0.0598".PadRight(18) +
                "1.519" + System.Environment.NewLine +
                "18".PadRight(8) + "0.0478".PadRight(18) +
                "1.214" + System.Environment.NewLine +
                "20".PadRight(8) + "0.0359".PadRight(18) +
                "0.912" + System.Environment.NewLine +
                "22".PadRight(8) + "0.0299".PadRight(18) +
                "0.759" + System.Environment.NewLine +
                "24".PadRight(8) + "0.0239".PadRight(18) +
                "0.607" + System.Environment.NewLine +
                "26".PadRight(8) + "0.0179".PadRight(18) +
                "0.455" + System.Environment.NewLine +
                new string('-', 45) +
                System.Environment.NewLine +
                "HVAC standard:" +
                System.Environment.NewLine +
                "Light duty (< 24 in):  22 gauge" +
                System.Environment.NewLine +
                "Standard (24-48 in):   20 gauge" +
                System.Environment.NewLine +
                "Heavy duty (> 48 in):  18 gauge";
        }

        private string GaugeFromThickness(double inches)
        {
            if (inches >= 0.1345) return "10 gauge";
            if (inches >= 0.1084) return "12 gauge";
            if (inches >= 0.0747) return "14 gauge";
            if (inches >= 0.0598) return "16 gauge";
            if (inches >= 0.0478) return "18 gauge";
            if (inches >= 0.0359) return "20 gauge";
            if (inches >= 0.0299) return "22 gauge";
            if (inches >= 0.0239) return "24 gauge";
            if (inches >= 0.0179) return "26 gauge";
            return "non-standard gauge";
        }

        private double ThicknessFromGauge(double gauge)
        {
            switch ((int)gauge)
            {
                case 10: return 0.1345;
                case 12: return 0.1084;
                case 14: return 0.0747;
                case 16: return 0.0598;
                case 18: return 0.0478;
                case 20: return 0.0359;
                case 22: return 0.0299;
                case 24: return 0.0239;
                case 26: return 0.0179;
                default: return -1;
            }
        }

        private double ParseValue(
            string text, double defaultValue)
        {
            try
            {
                Match m = Regex.Match(text, @"([\d.]+)");
                if (m.Success)
                    return double.Parse(m.Groups[1].Value);
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}