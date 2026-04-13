using System;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class ILogicGenerator
    {
        private Inventor.Application _inventorApplication;

        public ILogicGenerator(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleILogic(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (!n.Contains("ilogic") &&
                !n.Contains("rule") &&
                !n.Contains("generate rule") &&
                !n.Contains("write rule"))
                return null;

            if (n.Contains("thickness") ||
                n.Contains("sheet metal") ||
                n.Contains("gauge"))
                return GenerateThicknessRule();

            if (n.Contains("suppress") &&
                n.Contains("hole"))
                return GenerateSuppressSmallHolesRule();

            if (n.Contains("material"))
                return GenerateMaterialRule();

            if (n.Contains("part number") ||
                n.Contains("partnumber"))
                return GeneratePartNumberRule();

            if (n.Contains("mass") ||
                n.Contains("weight"))
                return GenerateMassCheckRule();

            if (n.Contains("export") &&
                n.Contains("dxf"))
                return GenerateExportDxfRule();

            if (n.Contains("parameter") &&
                (n.Contains("check") ||
                 n.Contains("validate")))
                return GenerateParameterValidationRule();

            if (n.Contains("rename") ||
                n.Contains("file name") ||
                n.Contains("filename"))
                return GenerateRenameRule();

            // Generic rule template
            return GenerateGenericTemplate();
        }

        // ─── Rules ────────────────────────────────────────────────────

        private string GenerateThicknessRule()
        {
            string rule = @"
' ─── Sheet Metal Thickness by Width Rule ───────────────────
' Sets sheet metal thickness based on Width parameter
' HVAC standard gauges:
'   Width <= 24 in  -> 0.034 in (22 gauge)
'   Width <= 48 in  -> 0.040 in (20 gauge)
'   Width >  48 in  -> 0.052 in (18 gauge)

Dim width As Double = Parameter(""Width"")

If width <= 24 Then
    Parameter(""Thickness"") = ""0.034 in""
ElseIf width <= 48 Then
    Parameter(""Thickness"") = ""0.040 in""
Else
    Parameter(""Thickness"") = ""0.052 in""
End If

' Update the description iProperty
iProperties.Value(""Summary"", ""Comments"") = _
    ""Thickness auto-set by Width rule""
";
            return FormatRuleOutput(
                "Sheet Metal Thickness by Width", rule);
        }

        private string GenerateSuppressSmallHolesRule()
        {
            string rule = @"
' ─── Suppress Small Holes Rule ─────────────────────────────
' Suppresses all hole features smaller than min diameter
' Change minDiameter to your cutoff size in inches

Dim minDiameter As Double = 0.25 ' inches

Dim oDoc As PartDocument = ThisDoc.Document
Dim oFeatures As PartFeatures = _
    oDoc.ComponentDefinition.Features

For Each oFeature As PartFeature In oFeatures
    If TypeOf oFeature Is HoleFeature Then
        Dim oHole As HoleFeature = oFeature
        Try
            Dim dia As Double = oHole.HoleDiameter.Value
            ' Convert cm to inches (Inventor uses cm)
            Dim diaInches As Double = dia / 2.54
            If diaInches < minDiameter Then
                oHole.Suppressed = True
            Else
                oHole.Suppressed = False
            End If
        Catch
        End Try
    End If
Next
";
            return FormatRuleOutput(
                "Suppress Small Holes", rule);
        }

        private string GenerateMaterialRule()
        {
            string rule = @"
' ─── Auto Material Rule ─────────────────────────────────────
' Sets material based on part thickness parameter
' HVAC standard materials

Dim thickness As Double = Parameter(""Thickness"")
' Convert to inches (Inventor uses cm internally)
Dim thicknessIn As Double = thickness / 2.54

If thicknessIn <= 0.034 Then
    ' 22 gauge - light duty
    iProperties.Value(""Design Tracking Properties"", _
        ""Material"") = ""Galvanized Steel""
ElseIf thicknessIn <= 0.052 Then
    ' 20-18 gauge - standard
    iProperties.Value(""Design Tracking Properties"", _
        ""Material"") = ""Galvanized Steel""
Else
    ' Heavy gauge
    iProperties.Value(""Design Tracking Properties"", _
        ""Material"") = ""Steel, Mild""
End If
";
            return FormatRuleOutput(
                "Auto Material by Thickness", rule);
        }

        private string GeneratePartNumberRule()
        {
            string rule = @"
' ─── Auto Part Number Rule ──────────────────────────────────
' Builds part number from parameters
' Format: PREFIX-WIDTH x HEIGHT x LENGTH
' Example: FPB-24x12x48

Dim prefix As String = ""FPB""
Dim width As String = Parameter(""Width"").ToString(""0"")
Dim height As String = Parameter(""Height"").ToString(""0"")
Dim length As String = Parameter(""Length"").ToString(""0"")

Dim partNumber As String = _
    prefix & ""-"" & width & ""x"" & height & ""x"" & length

iProperties.Value( _
    ""{32853F0F-3444-11D1-9E93-0060B03C1CA6}"", _
    ""Part Number"") = partNumber

iProperties.Value( _
    ""{32853F0F-3444-11D1-9E93-0060B03C1CA6}"", _
    ""Description"") = _
    prefix & "" "" & width & ""W x "" & _
    height & ""H x "" & length & ""L""
";
            return FormatRuleOutput(
                "Auto Part Number from Parameters", rule);
        }

        private string GenerateMassCheckRule()
        {
            string rule = @"
' ─── Mass Check Rule ────────────────────────────────────────
' Warns if part exceeds maximum weight
' Change maxWeightLbs to your limit

Dim maxWeightLbs As Double = 50.0

Dim oDoc As PartDocument = ThisDoc.Document
Dim massKg As Double = _
    oDoc.ComponentDefinition.MassProperties.Mass

' Convert kg to lbs
Dim massLbs As Double = massKg * 2.20462

If massLbs > maxWeightLbs Then
    MsgBox(""WARNING: Part weight "" & _
        massLbs.ToString(""0.0"") & "" lbs "" & _
        ""exceeds limit of "" & _
        maxWeightLbs.ToString(""0.0"") & "" lbs."", _
        MsgBoxStyle.Exclamation, _
        ""Weight Check"")
Else
    MsgBox(""Weight OK: "" & _
        massLbs.ToString(""0.0"") & "" lbs"", _
        MsgBoxStyle.Information, _
        ""Weight Check"")
End If
";
            return FormatRuleOutput(
                "Mass Check", rule);
        }

        private string GenerateExportDxfRule()
        {
            string rule = @"
' ─── Export Flat Pattern as DXF Rule ───────────────────────
' Exports the flat pattern of the active sheet metal part
' Saves DXF to the same folder as the part file

Dim oDoc As PartDocument = ThisDoc.Document
Dim savePath As String = _
    System.IO.Path.ChangeExtension( _
        oDoc.FullFileName, "".dxf"")

' Find the DWG/DXF translator
Dim oAddIn As TranslatorAddIn = Nothing

For Each addIn As ApplicationAddIn In _
    ThisApplication.ApplicationAddIns
    If addIn.ClassIdString = _
        ""{C24E3AC2-122E-11D5-8E91-0010B541CD80}"" Then
        oAddIn = addIn
        Exit For
    End If
Next

If oAddIn Is Nothing Then
    MsgBox(""DXF translator not found."")
Else
    Dim oContext As TranslationContext = _
        ThisApplication.TransientObjects _
            .CreateTranslationContext()
    oContext.Type = _
        IOMechanismEnum.kFileBrowseIOMechanism

    Dim oOptions As NameValueMap = _
        ThisApplication.TransientObjects _
            .CreateNameValueMap()

    Dim oData As DataMedium = _
        ThisApplication.TransientObjects _
            .CreateDataMedium()

    oData.FileName = savePath

    oAddIn.SaveCopyAs(oDoc, oContext, oOptions, oData)

    MsgBox(""DXF exported to:"" & vbCrLf & savePath)
End If
";
            return FormatRuleOutput(
                "Export Flat Pattern as DXF", rule);
        }

        private string GenerateParameterValidationRule()
        {
            string rule = @"
' ─── Parameter Validation Rule ──────────────────────────────
' Checks that key parameters are within valid ranges
' Edit the ranges to match your standards

Dim errors As String = """"

' Check Width
Dim width As Double = Parameter(""Width"")
If width <= 0 Then
    errors &= ""- Width must be greater than 0"" & vbCrLf
End If
If width > 120 Then
    errors &= ""- Width "" & width & _
        "" exceeds maximum of 120 in"" & vbCrLf
End If

' Check Height
Dim height As Double = Parameter(""Height"")
If height <= 0 Then
    errors &= ""- Height must be greater than 0"" & vbCrLf
End If
If height > 120 Then
    errors &= ""- Height "" & height & _
        "" exceeds maximum of 120 in"" & vbCrLf
End If

' Check Thickness
Dim thickness As Double = Parameter(""Thickness"")
If thickness <= 0 Then
    errors &= ""- Thickness must be greater than 0"" & vbCrLf
End If

' Report results
If errors = """" Then
    MsgBox(""All parameters are valid."", _
        MsgBoxStyle.Information, ""Validation"")
Else
    MsgBox(""Parameter errors found:"" & _
        vbCrLf & errors, _
        MsgBoxStyle.Critical, ""Validation"")
End If
";
            return FormatRuleOutput(
                "Parameter Validation", rule);
        }

        private string GenerateRenameRule()
        {
            string rule = @"
' ─── Auto Rename File Rule ──────────────────────────────────
' Renames the file based on Part Number iProperty
' WARNING: closes and reopens the document

Dim partNumber As String = iProperties.Value( _
    ""{32853F0F-3444-11D1-9E93-0060B03C1CA6}"", _
    ""Part Number"")

If String.IsNullOrEmpty(partNumber) Then
    MsgBox(""Part Number iProperty is empty. "" & _
        ""Set it first."")
Else
    Dim folder As String = _
        System.IO.Path.GetDirectoryName( _
            ThisDoc.Document.FullFileName)

    Dim ext As String = _
        System.IO.Path.GetExtension( _
            ThisDoc.Document.FullFileName)

    Dim newPath As String = _
        System.IO.Path.Combine( _
            folder, partNumber & ext)

    Dim confirm As MsgBoxResult = MsgBox( _
        ""Rename file to:"" & vbCrLf & newPath & _
        vbCrLf & vbCrLf & ""Proceed?"", _
        MsgBoxStyle.YesNo, ""Confirm Rename"")

    If confirm = MsgBoxResult.Yes Then
        ThisDoc.Document.SaveAs(newPath, False)
        MsgBox(""File saved as:"" & vbCrLf & newPath)
    End If
End If
";
            return FormatRuleOutput(
                "Auto Rename File from Part Number", rule);
        }

        private string GenerateGenericTemplate()
        {
            string rule = @"
' ─── Custom iLogic Rule Template ────────────────────────────
' Edit this template for your specific needs

' Read a parameter
Dim myParam As Double = Parameter(""Width"")

' Set a parameter
Parameter(""Height"") = ""12 in""

' Read an iProperty
Dim partNum As String = iProperties.Value( _
    ""{32853F0F-3444-11D1-9E93-0060B03C1CA6}"", _
    ""Part Number"")

' Set an iProperty
iProperties.Value( _
    ""{32853F0F-3444-11D1-9E93-0060B03C1CA6}"", _
    ""Description"") = ""My Description""

' Suppress a feature
Dim oDoc As PartDocument = ThisDoc.Document
For Each f As PartFeature In _
    oDoc.ComponentDefinition.Features
    If f.Name = ""MyFeatureName"" Then
        f.Suppressed = True
    End If
Next

' Show a message
MsgBox(""Rule complete."")
";
            return FormatRuleOutput(
                "Custom iLogic Template", rule);
        }

        // ─── How to add the rule to Inventor ─────────────────────────

        private string FormatRuleOutput(
            string ruleName, string ruleCode)
        {
            var sb = new StringBuilder();

            sb.AppendLine("iLogic Rule: " + ruleName);
            sb.AppendLine(new string('─', 40));
            sb.AppendLine();
            sb.AppendLine("── How to add this rule ──");
            sb.AppendLine(
                "1. In Inventor open the iLogic browser");
            sb.AppendLine(
                "   (Tools tab > iLogic > iLogic Browser)");
            sb.AppendLine(
                "2. Right-click Rules > Add Rule");
            sb.AppendLine(
                "3. Name it: " + ruleName);
            sb.AppendLine(
                "4. Paste the code below into the editor");
            sb.AppendLine(
                "5. Click OK to save and run");
            sb.AppendLine();
            sb.AppendLine("── Rule code ──");
            sb.AppendLine(ruleCode);

            return sb.ToString();
        }
    }
}