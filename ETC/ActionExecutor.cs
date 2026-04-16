using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class ActionExecutor
    {
        private Inventor.Application _inventorApplication;

        // Inventor internal units are cm.
        // All user input is in inches.
        // 1 inch = 2.54 cm
        private const double InToCm = 2.54;

        public ActionExecutor(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryExecuteAction(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            // ── Multi-step chained actions ────────────────────────────

            // "create a 4x2 plate 0.125 thick"
            if (IsChainedPlateRequest(n))
                return CreatePlate(prompt);

            // "create a cylinder radius 1 height 2"
            if (IsChainedCylinderRequest(n))
                return CreateCylinder(prompt);

            // ── Sketch creation ───────────────────────────────────────
            if (n.Contains("create sketch") ||
                n.Contains("new sketch") ||
                n.Contains("start sketch") ||
                n.Contains("make sketch") ||
                n.Contains("add sketch"))
                return CreateSketch(DetectPlane(n));

            // ── Finish sketch ─────────────────────────────────────────
            if (n == "finish sketch" ||
                n == "exit sketch" ||
                n == "close sketch" ||
                n == "done sketch" ||
                n == "end sketch")
                return FinishSketch();

            // ── Draw geometry ─────────────────────────────────────────
            if (n.Contains("draw circle") ||
                n.Contains("add circle") ||
                n.Contains("create circle"))
                return DrawCircle(prompt);

            if (n.Contains("draw rectangle") ||
                n.Contains("add rectangle") ||
                n.Contains("create rectangle"))
                return DrawRectangle(prompt);

            if (n.Contains("draw line") ||
                n.Contains("add line") ||
                n.Contains("create line"))
                return DrawLine(prompt);

            // ── Features ──────────────────────────────────────────────
            if (n.Contains("extrude"))
                return Extrude(prompt);

            if (n.Contains("fillet"))
                return Fillet(prompt);

            if (n.Contains("chamfer"))
                return Chamfer(prompt);

            if (n.Contains("create hole") ||
                n.Contains("add hole") ||
                n.Contains("drill hole"))
                return CreateHole(prompt);

            // ── Document ──────────────────────────────────────────────
            if (n == "new part" ||
                n == "create new part" ||
                n == "create part")
                return NewPart();

            if (n == "save" ||
                n == "save file" ||
                n == "save document")
                return SaveDocument();

            return null;
        }

        // ─── Chained action detection ─────────────────────────────────

        private bool IsChainedPlateRequest(string n)
        {
            return (n.Contains("plate") ||
                    n.Contains("block") ||
                    n.Contains("sheet") ||
                    n.Contains("panel")) &&
                   (n.Contains("create") ||
                    n.Contains("make") ||
                    n.Contains("new"));
        }

        private bool IsChainedCylinderRequest(string n)
        {
            return (n.Contains("cylinder") ||
                    n.Contains("rod") ||
                    n.Contains("tube") ||
                    n.Contains("boss")) &&
                   (n.Contains("create") ||
                    n.Contains("make") ||
                    n.Contains("new"));
        }

        // ─── Chained: Create plate ────────────────────────────────────

        private string CreatePlate(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Plate creation requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                PartComponentDefinition compDef =
                    part.ComponentDefinition;

                double[] numbers = ExtractAllNumbers(prompt);

                double width = numbers.Length > 0 ?
                    numbers[0] : 4.0;
                double height = numbers.Length > 1 ?
                    numbers[1] : 2.0;
                double thickness = numbers.Length > 2 ?
                    numbers[2] : 0.125;

                var log = new System.Text.StringBuilder();
                log.AppendLine("── Creating plate ──");

                // Step 1 — Create sketch on XY
                WorkPlane wp = compDef.WorkPlanes[3];
                PlanarSketch sketch =
                    compDef.Sketches.Add(wp);

                log.AppendLine(
                    "✅ Sketch created on XY (Front) plane");

                // Step 2 — Draw rectangle
                double wCm = width * InToCm;
                double hCm = height * InToCm;

                TransientGeometry tg =
                    _inventorApplication.TransientGeometry;

                Point2d p1 = tg.CreatePoint2d(
                    -wCm / 2, -hCm / 2);
                Point2d p2 = tg.CreatePoint2d(
                    wCm / 2, hCm / 2);

                sketch.SketchLines
                      .AddAsTwoPointRectangle(p1, p2);

                log.AppendLine(
                    "✅ Rectangle drawn: " +
                    width + " x " + height + " in");

                // Step 3 — Extrude
                Profile profile =
                    sketch.Profiles.AddForSolid();

                double thickCm = thickness * InToCm;

                ExtrudeDefinition extrudeDef =
                    compDef.Features.ExtrudeFeatures
                           .CreateExtrudeDefinition(
                               profile,
                               PartFeatureOperationEnum
                                   .kJoinOperation);

                extrudeDef.SetDistanceExtent(
                    thickCm,
                    PartFeatureExtentDirectionEnum
                        .kPositiveExtentDirection);

                ExtrudeFeature extrude =
                    compDef.Features.ExtrudeFeatures
                           .Add(extrudeDef);

                log.AppendLine(
                    "✅ Extruded: " + thickness + " in");
                log.AppendLine(
                    "✅ Feature: " + extrude.Name);
                log.AppendLine();
                log.AppendLine("Plate created:");
                log.AppendLine(
                    "Width:     " + width + " in");
                log.AppendLine(
                    "Height:    " + height + " in");
                log.AppendLine(
                    "Thickness: " + thickness + " in");

                return log.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to create plate: " + ex.Message;
            }
        }

        // ─── Chained: Create cylinder ─────────────────────────────────

        private string CreateCylinder(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Cylinder creation requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                PartComponentDefinition compDef =
                    part.ComponentDefinition;

                double radius = ExtractNumber(
                    prompt,
                    new[] { "radius", "r", "diameter" },
                    1.0);

                // If diameter was specified halve it
                string n = prompt.ToLowerInvariant();
                if (n.Contains("diameter") ||
                    n.Contains("dia"))
                    radius = radius / 2.0;

                double height = ExtractNumber(
                    prompt,
                    new[] { "height", "h", "length", "l" },
                    2.0);

                var log = new System.Text.StringBuilder();
                log.AppendLine("── Creating cylinder ──");

                // Step 1 — Create sketch on XY
                WorkPlane wp = compDef.WorkPlanes[3];
                PlanarSketch sketch =
                    compDef.Sketches.Add(wp);

                log.AppendLine(
                    "✅ Sketch created on XY (Front) plane");

                // Step 2 — Draw circle at origin
                TransientGeometry tg =
                    _inventorApplication.TransientGeometry;

                Point2d center = tg.CreatePoint2d(0, 0);
                double radiusCm = radius * InToCm;

                sketch.SketchCircles
                      .AddByCenterRadius(center, radiusCm);

                log.AppendLine(
                    "✅ Circle drawn: radius " +
                    radius + " in");

                // Step 3 — Extrude
                Profile profile =
                    sketch.Profiles.AddForSolid();

                double heightCm = height * InToCm;

                ExtrudeDefinition extrudeDef =
                    compDef.Features.ExtrudeFeatures
                           .CreateExtrudeDefinition(
                               profile,
                               PartFeatureOperationEnum
                                   .kJoinOperation);

                extrudeDef.SetDistanceExtent(
                    heightCm,
                    PartFeatureExtentDirectionEnum
                        .kPositiveExtentDirection);

                ExtrudeFeature extrude =
                    compDef.Features.ExtrudeFeatures
                           .Add(extrudeDef);

                log.AppendLine(
                    "✅ Extruded: " + height + " in");
                log.AppendLine(
                    "✅ Feature: " + extrude.Name);
                log.AppendLine();
                log.AppendLine("Cylinder created:");
                log.AppendLine(
                    "Radius: " + radius + " in");
                log.AppendLine(
                    "Height: " + height + " in");

                return log.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to create cylinder: " +
                       ex.Message;
            }
        }

        // ─── Plane detection ──────────────────────────────────────────
        // Your convention:
        //   front  = XY = WorkPlanes[3]
        //   top    = XZ = WorkPlanes[2]
        //   side   = YZ = WorkPlanes[1]

        private string DetectPlane(string n)
        {
            // YZ — check before XZ
            if (n.Contains("yz") ||
                n.Contains("y-z") ||
                n.Contains("y z") ||
                n.Contains("y/z") ||
                n.Contains("side"))
                return "YZ";

            // XZ — check before XY
            if (n.Contains("xz") ||
                n.Contains("x-z") ||
                n.Contains("x z") ||
                n.Contains("x/z") ||
                n.Contains("top"))
                return "XZ";

            // XY
            if (n.Contains("xy") ||
                n.Contains("x-y") ||
                n.Contains("x y") ||
                n.Contains("x/y") ||
                n.Contains("front") ||
                n.Contains("bottom") ||
                n.Contains("horizontal"))
                return "XY";

            return "XY";
        }

        // ─── Create sketch ────────────────────────────────────────────

        private string CreateSketch(string plane)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sketches can only be created " +
                           "in a Part document.";

                PartDocument part = (PartDocument)doc;
                PartComponentDefinition compDef =
                    part.ComponentDefinition;

                WorkPlane workPlane;
                string planeFriendlyName;

                switch (plane.ToUpper())
                {
                    case "YZ":
                        workPlane = compDef.WorkPlanes[1];
                        planeFriendlyName = "YZ (Side)";
                        break;
                    case "XZ":
                        workPlane = compDef.WorkPlanes[2];
                        planeFriendlyName = "XZ (Top)";
                        break;
                    default:
                        workPlane = compDef.WorkPlanes[3];
                        planeFriendlyName = "XY (Front)";
                        break;
                }

                PlanarSketch sketch =
                    compDef.Sketches.Add(workPlane);

                sketch.Edit();

                return "Sketch created on the " +
                       planeFriendlyName +
                       " plane and is now active." +
                       System.Environment.NewLine +
                       "You can now draw geometry. " +
                       "Say 'finish sketch' when done.";
            }
            catch (Exception ex)
            {
                return "Failed to create sketch: " + ex.Message;
            }
        }

        // ─── Finish sketch ────────────────────────────────────────────

        private string FinishSketch()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "No active part document.";

                PartDocument part = (PartDocument)doc;
                PartComponentDefinition compDef =
                    part.ComponentDefinition;

                foreach (PlanarSketch sk in compDef.Sketches)
                {
                    try
                    {
                        sk.ExitEdit();
                        return "Sketch finished.";
                    }
                    catch { }
                }

                int count = compDef.Sketches.Count;

                if (count > 0)
                {
                    try
                    {
                        compDef.Sketches[count].ExitEdit();
                        return "Sketch finished.";
                    }
                    catch { }
                }

                try
                {
                    _inventorApplication.CommandManager
                        .ControlDefinitions["AppEscapeCmd"]
                        .Execute();

                    return "Sketch exit attempted via Escape.";
                }
                catch { }

                return "Could not exit sketch automatically. " +
                       "Press Escape or click " +
                       "'Finish Sketch' in the ribbon.";
            }
            catch (Exception ex)
            {
                return "Failed to finish sketch: " + ex.Message;
            }
        }

        // ─── Draw circle ──────────────────────────────────────────────

        private string DrawCircle(string prompt)
        {
            try
            {
                PlanarSketch sketch = GetActiveSketch();

                if (sketch == null)
                    return "No active sketch found. " +
                           "Say 'create sketch on front " +
                           "plane' first.";

                double radius = ExtractNumber(
                    prompt, new[] { "radius", "r" }, 1.0);

                double cx = 0, cy = 0;
                ExtractPoint(prompt, ref cx, ref cy);

                double radiusCm = radius * InToCm;
                double cxCm = cx * InToCm;
                double cyCm = cy * InToCm;

                TransientGeometry tg =
                    _inventorApplication.TransientGeometry;

                Point2d center =
                    tg.CreatePoint2d(cxCm, cyCm);

                sketch.SketchCircles
                      .AddByCenterRadius(center, radiusCm);

                return "Circle drawn:" +
                       System.Environment.NewLine +
                       "Center: (" + cx + ", " + cy + ") in" +
                       System.Environment.NewLine +
                       "Radius: " + radius + " in";
            }
            catch (Exception ex)
            {
                return "Failed to draw circle: " + ex.Message;
            }
        }

        // ─── Draw rectangle ───────────────────────────────────────────

        private string DrawRectangle(string prompt)
        {
            try
            {
                PlanarSketch sketch = GetActiveSketch();

                if (sketch == null)
                    return "No active sketch found. " +
                           "Say 'create sketch on front " +
                           "plane' first.";

                double[] numbers = ExtractAllNumbers(prompt);

                double width = numbers.Length > 0 ?
                    numbers[0] : 4.0;
                double height = numbers.Length > 1 ?
                    numbers[1] : width;

                double wCm = width * InToCm;
                double hCm = height * InToCm;

                TransientGeometry tg =
                    _inventorApplication.TransientGeometry;

                Point2d p1 = tg.CreatePoint2d(
                    -wCm / 2, -hCm / 2);
                Point2d p2 = tg.CreatePoint2d(
                    wCm / 2, hCm / 2);

                sketch.SketchLines
                      .AddAsTwoPointRectangle(p1, p2);

                return "Rectangle drawn:" +
                       System.Environment.NewLine +
                       "Width: " + width + " in" +
                       System.Environment.NewLine +
                       "Height: " + height + " in" +
                       System.Environment.NewLine +
                       "Centered at origin.";
            }
            catch (Exception ex)
            {
                return "Failed to draw rectangle: " + ex.Message;
            }
        }

        // ─── Draw line ────────────────────────────────────────────────

        private string DrawLine(string prompt)
        {
            try
            {
                PlanarSketch sketch = GetActiveSketch();

                if (sketch == null)
                    return "No active sketch found. " +
                           "Say 'create sketch on front " +
                           "plane' first.";

                double[] numbers = ExtractAllNumbers(prompt);

                double x1 = numbers.Length > 0 ? numbers[0] : 0;
                double y1 = numbers.Length > 1 ? numbers[1] : 0;
                double x2 = numbers.Length > 2 ? numbers[2] : 4;
                double y2 = numbers.Length > 3 ? numbers[3] : 0;

                TransientGeometry tg =
                    _inventorApplication.TransientGeometry;

                Point2d start = tg.CreatePoint2d(
                    x1 * InToCm, y1 * InToCm);
                Point2d end = tg.CreatePoint2d(
                    x2 * InToCm, y2 * InToCm);

                sketch.SketchLines.AddByTwoPoints(start, end);

                return "Line drawn:" +
                       System.Environment.NewLine +
                       "From: (" + x1 + ", " + y1 + ") in" +
                       System.Environment.NewLine +
                       "To: (" + x2 + ", " + y2 + ") in";
            }
            catch (Exception ex)
            {
                return "Failed to draw line: " + ex.Message;
            }
        }

        // ─── Extrude ──────────────────────────────────────────────────

        private string Extrude(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Extrude is only available " +
                           "in a Part document.";

                PartDocument part = (PartDocument)doc;
                PartComponentDefinition compDef =
                    part.ComponentDefinition;

                int sketchCount = compDef.Sketches.Count;

                if (sketchCount == 0)
                    return "No sketches found. " +
                           "Create a sketch with geometry first.";

                PlanarSketch sketch =
                    compDef.Sketches[sketchCount];

                Profile profile =
                    sketch.Profiles.AddForSolid();

                if (profile == null)
                    return "Could not create a profile. " +
                           "Make sure the sketch has " +
                           "closed geometry.";

                double distance = ExtractNumber(
                    prompt,
                    new[] { "extrude", "by", "depth" },
                    1.0);

                double distCm = distance * InToCm;

                bool symmetric = prompt.ToLowerInvariant()
                    .Contains("symmetric");

                ExtrudeDefinition extrudeDef =
                    compDef.Features.ExtrudeFeatures
                           .CreateExtrudeDefinition(
                               profile,
                               PartFeatureOperationEnum
                                   .kJoinOperation);

                if (symmetric)
                {
                    extrudeDef.SetDistanceExtent(
                        distCm,
                        PartFeatureExtentDirectionEnum
                            .kSymmetricExtentDirection);
                }
                else
                {
                    extrudeDef.SetDistanceExtent(
                        distCm,
                        PartFeatureExtentDirectionEnum
                            .kPositiveExtentDirection);
                }

                ExtrudeFeature extrude =
                    compDef.Features.ExtrudeFeatures
                           .Add(extrudeDef);

                return "Extrusion created:" +
                       System.Environment.NewLine +
                       "Distance: " + distance + " in" +
                       (symmetric ? " (symmetric)" : "") +
                       System.Environment.NewLine +
                       "Feature: " + extrude.Name;
            }
            catch (Exception ex)
            {
                return "Failed to extrude: " + ex.Message;
            }
        }

        // ─── Fillet ───────────────────────────────────────────────────

        private string Fillet(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Fillet is only available " +
                           "in a Part document.";

                SelectSet sel =
                    _inventorApplication.ActiveDocument
                                        .SelectSet;

                if (sel == null || sel.Count == 0)
                    return "No edges selected. " +
                           "Select one or more edges first, " +
                           "then say 'fillet radius 0.25'.";

                double radius = ExtractNumber(
                    prompt,
                    new[] { "radius", "r", "fillet" },
                    0.25);

                double radiusCm = radius * InToCm;

                PartDocument part = (PartDocument)doc;
                PartComponentDefinition compDef =
                    part.ComponentDefinition;

                EdgeCollection edges =
                    _inventorApplication.TransientObjects
                        .CreateEdgeCollection();

                foreach (object item in sel)
                {
                    if (item is Edge edge)
                        edges.Add(edge);
                }

                if (edges.Count == 0)
                    return "No edges found in selection. " +
                           "Select edges and try again.";

                FilletFeature fillet =
                    compDef.Features.FilletFeatures
                           .AddSimple(edges, radiusCm);

                return "Fillet created:" +
                       System.Environment.NewLine +
                       "Radius: " + radius + " in" +
                       System.Environment.NewLine +
                       "Feature: " + fillet.Name +
                       System.Environment.NewLine +
                       "Edges filleted: " + edges.Count;
            }
            catch (Exception ex)
            {
                return "Failed to create fillet: " + ex.Message;
            }
        }

        // ─── Chamfer ──────────────────────────────────────────────────

        private string Chamfer(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Chamfer is only available " +
                           "in a Part document.";

                SelectSet sel =
                    _inventorApplication.ActiveDocument
                                        .SelectSet;

                if (sel == null || sel.Count == 0)
                    return "No edges selected. " +
                           "Select one or more edges first, " +
                           "then say 'chamfer distance 0.125'.";

                double distance = ExtractNumber(
                    prompt,
                    new[] { "distance", "d", "chamfer" },
                    0.125);

                double distCm = distance * InToCm;

                PartDocument part = (PartDocument)doc;
                PartComponentDefinition compDef =
                    part.ComponentDefinition;

                EdgeCollection edges =
                    _inventorApplication.TransientObjects
                        .CreateEdgeCollection();

                foreach (object item in sel)
                {
                    if (item is Edge edge)
                        edges.Add(edge);
                }

                if (edges.Count == 0)
                    return "No edges found in selection. " +
                           "Select edges and try again.";

                ChamferFeature chamfer =
                    compDef.Features.ChamferFeatures
                           .AddUsingDistance(edges, distCm);

                return "Chamfer created:" +
                       System.Environment.NewLine +
                       "Distance: " + distance + " in" +
                       System.Environment.NewLine +
                       "Feature: " + chamfer.Name +
                       System.Environment.NewLine +
                       "Edges chamfered: " + edges.Count;
            }
            catch (Exception ex)
            {
                return "Failed to create chamfer: " + ex.Message;
            }
        }

        // ─── Create hole ──────────────────────────────────────────────

        private string CreateHole(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Holes can only be created " +
                           "in a Part document.";

                double diameter = ExtractNumber(
                    prompt,
                    new[] { "diameter", "dia" },
                    0.5);

                double depth = ExtractNumber(
                    prompt,
                    new[] { "depth" },
                    1.0);

                _inventorApplication.CommandManager
                    .ControlDefinitions["HoleCmd"]
                    .Execute();

                return
                    "Hole command launched." +
                    System.Environment.NewLine +
                    "Requested: diameter " + diameter +
                    " in, depth " + depth + " in." +
                    System.Environment.NewLine +
                    "Set the values in the Hole dialog " +
                    "and click OK to place the hole.";
            }
            catch (Exception ex)
            {
                return "Failed to launch hole command: " +
                       ex.Message;
            }
        }

        // ─── New part ─────────────────────────────────────────────────

        private string NewPart()
        {
            try
            {
                Document doc =
                    _inventorApplication.Documents.Add(
                        DocumentTypeEnum.kPartDocumentObject,
                        _inventorApplication
                            .FileManager
                            .GetTemplateFile(
                                DocumentTypeEnum
                                    .kPartDocumentObject));

                return "New Part document created: " +
                       doc.DisplayName;
            }
            catch (Exception ex)
            {
                return "Failed to create new part: " + ex.Message;
            }
        }

        // ─── Save document ────────────────────────────────────────────

        private string SaveDocument()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                doc.Save();

                return "Document saved: " + doc.DisplayName;
            }
            catch (Exception ex)
            {
                return "Failed to save: " + ex.Message;
            }
        }

        // ─── Get active sketch ────────────────────────────────────────

        private PlanarSketch GetActiveSketch()
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
                PartComponentDefinition compDef =
                    part.ComponentDefinition;

                int count = compDef.Sketches.Count;

                if (count == 0)
                    return null;

                return compDef.Sketches[count];
            }
            catch
            {
                return null;
            }
        }

        // ─── Unit-aware number extraction ─────────────────────────────

        private double ExtractNumber(
            string prompt,
            string[] precedingWords,
            double defaultValue)
        {
            try
            {
                string lower = prompt.ToLowerInvariant();

                foreach (string word in precedingWords)
                {
                    int idx = lower.IndexOf(word);

                    if (idx >= 0)
                    {
                        string after =
                            prompt.Substring(
                                idx + word.Length).Trim();

                        double val =
                            ParseValueWithUnits(after);

                        if (val >= 0)
                            return val;
                    }
                }

                double fallback =
                    ParseValueWithUnits(prompt);

                if (fallback >= 0)
                    return fallback;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private double ParseValueWithUnits(string text)
        {
            try
            {
                string t = text.Trim().ToLowerInvariant();

                // Feet and inches combined
                Match feetInches = Regex.Match(t,
                    @"([\d.]+)\s*(?:feet|foot|ft|')" +
                    @"[\s,]*([\d.]+)\s*" +
                    @"(?:inches|inch|in)?");

                if (feetInches.Success)
                {
                    double feet =
                        double.Parse(
                            feetInches.Groups[1].Value);
                    double inches =
                        double.Parse(
                            feetInches.Groups[2].Value);
                    return (feet * 12.0) + inches;
                }

                // Feet only
                Match feetOnly = Regex.Match(t,
                    @"([\d.]+)\s*(?:feet|foot|ft|')");

                if (feetOnly.Success)
                    return double.Parse(
                        feetOnly.Groups[1].Value) * 12.0;

                // Whole + fraction
                Match fracInch = Regex.Match(t,
                    @"([\d]+)\s+([\d]+)/([\d]+)\s*" +
                    @"(?:inches|inch|in)?");

                if (fracInch.Success)
                {
                    double whole =
                        double.Parse(
                            fracInch.Groups[1].Value);
                    double num =
                        double.Parse(
                            fracInch.Groups[2].Value);
                    double den =
                        double.Parse(
                            fracInch.Groups[3].Value);
                    return whole + (num / den);
                }

                // Fraction only
                Match fracOnly = Regex.Match(t,
                    @"([\d]+)/([\d]+)");

                if (fracOnly.Success)
                {
                    double num =
                        double.Parse(
                            fracOnly.Groups[1].Value);
                    double den =
                        double.Parse(
                            fracOnly.Groups[2].Value);
                    return num / den;
                }

                // Decimal inches
                Match decInch = Regex.Match(t,
                    @"([\d.]+)\s*(?:inches|inch|in)");

                if (decInch.Success)
                    return double.Parse(
                        decInch.Groups[1].Value);

                // Plain number
                Match plain = Regex.Match(t, @"([\d.]+)");

                if (plain.Success)
                    return double.Parse(plain.Groups[1].Value);

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        private double[] ExtractAllNumbers(string prompt)
        {
            try
            {
                string t = prompt.ToLowerInvariant();

                t = Regex.Replace(t,
                    @"(\d)\s*x\s*(\d)", "$1 $2");

                MatchCollection matches =
                    Regex.Matches(t,
                        @"([\d.]+)\s*" +
                        @"(?:feet|foot|ft|" +
                        @"inches|inch|in)?");

                var results = new List<double>();

                foreach (Match m in matches)
                {
                    if (!m.Success ||
                        string.IsNullOrWhiteSpace(
                            m.Value.Trim()))
                        continue;

                    double val = ParseValueWithUnits(m.Value);

                    if (val >= 0)
                        results.Add(val);
                }

                return results.ToArray();
            }
            catch
            {
                return new double[0];
            }
        }

        private void ExtractPoint(
            string prompt, ref double x, ref double y)
        {
            try
            {
                string lower = prompt.ToLowerInvariant();

                if (lower.Contains("origin") ||
                    lower.Contains("0,0"))
                {
                    x = 0;
                    y = 0;
                    return;
                }

                Match m = Regex.Match(
                    prompt,
                    @"at\s+([\d.-]+)\s+([\d.-]+)",
                    RegexOptions.IgnoreCase);

                if (m.Success)
                {
                    x = double.Parse(m.Groups[1].Value);
                    y = double.Parse(m.Groups[2].Value);
                }
            }
            catch
            {
                x = 0;
                y = 0;
            }
        }
    }
}