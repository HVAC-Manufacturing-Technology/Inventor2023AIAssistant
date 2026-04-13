using System;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class SketchAnalyzer
    {
        private Inventor.Application _inventorApplication;
        private const double CmToIn = 0.393701;

        public SketchAnalyzer(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        public string TryHandleSketchAnalysis(
            string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("fully constrained") ||
                n.Contains("is this sketch") ||
                n.Contains("sketch constrained") ||
                n.Contains("sketch status") ||
                n.Contains("constraint status"))
                return CheckSketchConstraints();

            if (n.Contains("under-constrained") ||
                n.Contains("under constrained") ||
                n.Contains("unconstrained") ||
                n.Contains("what is unconstrained") ||
                n.Contains("what is under"))
                return FindUnconstrainedGeometry();

            if (n.Contains("sketch summary") ||
                n.Contains("analyze sketch") ||
                n.Contains("sketch report") ||
                n.Contains("sketch info"))
                return GetSketchSummary();

            if (n.Contains("how many lines") ||
                n.Contains("count lines"))
                return CountSketchEntities("lines");

            if (n.Contains("how many circles") ||
                n.Contains("count circles"))
                return CountSketchEntities("circles");

            if (n.Contains("how many sketch") ||
                n.Contains("count sketch"))
                return CountSketchEntities("all");

            if (n.Contains("list sketch dimension") ||
                n.Contains("sketch dimension") ||
                n.Contains("dimensions in sketch"))
                return ListSketchDimensions();

            if (n.Contains("active sketch") ||
                n.Contains("current sketch") ||
                n.Contains("what sketch"))
                return GetActiveSketchInfo();

            if (n.Contains("all sketch") ||
                n.Contains("list sketch") ||
                n.Contains("sketches in"))
                return ListAllSketches();

            return null;
        }

        // ─── Check constraint status ──────────────────────────────────

        private string CheckSketchConstraints()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sketch analysis requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                if (_inventorApplication
                        .ActiveEditObject
                    is PlanarSketch activeSketch)
                    return AnalyzeSketch(activeSketch);

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Sketch Constraint Status ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                int total = 0;
                int fullyConstrained = 0;
                int underConstrained = 0;

                foreach (PlanarSketch sketch in
                    part.ComponentDefinition.Sketches)
                {
                    try
                    {
                        total++;
                        string status =
                            GetConstraintStatus(sketch);

                        string icon =
                            status == "Fully Constrained"
                                ? "✅"
                                : status == "Empty Sketch"
                                    ? "○"
                                    : "⚠️";

                        sb.AppendLine(
                            icon + " " +
                            sketch.Name + ": " + status);

                        if (status == "Fully Constrained")
                            fullyConstrained++;
                        else
                            underConstrained++;
                    }
                    catch { }
                }

                if (total == 0)
                    return "No sketches found in " +
                           doc.DisplayName + ".";

                sb.AppendLine(new string('-', 40));
                sb.AppendLine("Total: " + total);
                sb.AppendLine(
                    "✅ Fully constrained: " +
                    fullyConstrained);
                sb.AppendLine(
                    "⚠️ Under constrained: " +
                    underConstrained);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to check constraints: " +
                       ex.Message;
            }
        }

        // ─── Analyze single sketch ────────────────────────────────────

        private string AnalyzeSketch(PlanarSketch sketch)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Active Sketch Analysis ──");
                sb.AppendLine(
                    "Sketch: " + sketch.Name);
                sb.AppendLine(new string('-', 40));

                string status =
                    GetConstraintStatus(sketch);

                string icon =
                    status == "Fully Constrained"
                        ? "✅"
                        : status == "Empty Sketch"
                            ? "○"
                            : "⚠️";

                sb.AppendLine(
                    "Status: " + icon + " " + status);
                sb.AppendLine();

                int lines = 0;
                int circles = 0;
                int arcs = 0;
                int points = 0;

                try
                {
                    lines =
                  sketch.SketchLines.Count;
                }
                catch { }
                try
                {
                    circles =
                  sketch.SketchCircles.Count;
                }
                catch { }
                try
                {
                    arcs =
                  sketch.SketchArcs.Count;
                }
                catch { }
                try
                {
                    points =
                  sketch.SketchPoints.Count;
                }
                catch { }

                sb.AppendLine("── Entities ──");
                sb.AppendLine("Lines:   " + lines);
                sb.AppendLine("Circles: " + circles);
                sb.AppendLine("Arcs:    " + arcs);
                sb.AppendLine("Points:  " + points);
                sb.AppendLine();
                sb.AppendLine("── Constraints ──");
                sb.AppendLine(
                    "Geometric: " +
                    sketch.GeometricConstraints.Count);
                sb.AppendLine(
                    "Dimensions: " +
                    sketch.DimensionConstraints.Count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to analyze sketch: " +
                       ex.Message;
            }
        }

        // ─── Find unconstrained geometry ──────────────────────────────

        private string FindUnconstrainedGeometry()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sketch analysis requires " +
                           "a Part document.";

                if (_inventorApplication
                        .ActiveEditObject
                    is PlanarSketch activeSketch)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(
                        "── Unconstrained Geometry ──");
                    sb.AppendLine(
                        "Sketch: " + activeSketch.Name);
                    sb.AppendLine(new string('-', 40));

                    string status =
                        GetConstraintStatus(activeSketch);

                    sb.AppendLine(
                        "Status: " + status);
                    sb.AppendLine();

                    int lines =
                        activeSketch.SketchLines.Count;
                    int circles =
                        activeSketch.SketchCircles.Count;
                    int arcs =
                        activeSketch.SketchArcs.Count;
                    int dims =
                        activeSketch
                            .DimensionConstraints.Count;
                    int gcons =
                        activeSketch
                            .GeometricConstraints.Count;

                    sb.AppendLine(
                        "Entities: " + lines +
                        " lines, " + circles +
                        " circles, " + arcs + " arcs");
                    sb.AppendLine(
                        "Constraints: " + gcons +
                        " geometric, " + dims +
                        " dimensions");

                    sb.AppendLine();

                    if (status == "Fully Constrained")
                        sb.AppendLine(
                            "✅ Sketch is fully " +
                            "constrained.");
                    else
                        sb.AppendLine(
                            "⚠️ Add dimensions or " +
                            "constraints until status " +
                            "shows Fully Constrained.");

                    return sb.ToString();
                }

                return
                    "No sketch is currently active." +
                    System.Environment.NewLine +
                    "Double-click a sketch in the " +
                    "model browser to activate it.";
            }
            catch (Exception ex)
            {
                return "Failed to find unconstrained: " +
                       ex.Message;
            }
        }

        // ─── Sketch summary ───────────────────────────────────────────

        private string GetSketchSummary()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sketch analysis requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine("── Sketch Summary ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('═', 40));

                int sketchNum = 0;

                foreach (PlanarSketch sketch in
                    part.ComponentDefinition.Sketches)
                {
                    try
                    {
                        sketchNum++;
                        string status =
                            GetConstraintStatus(sketch);
                        string icon =
                            status == "Fully Constrained"
                                ? "✅"
                                : status == "Empty Sketch"
                                    ? "○"
                                    : "⚠️";

                        sb.AppendLine(
                            sketchNum + ". " +
                            sketch.Name +
                            " — " + icon + " " + status);

                        sb.AppendLine(
                            "   Lines:" +
                            sketch.SketchLines.Count +
                            " Circles:" +
                            sketch.SketchCircles.Count +
                            " Arcs:" +
                            sketch.SketchArcs.Count +
                            " Dims:" +
                            sketch.DimensionConstraints
                                .Count +
                            " GCons:" +
                            sketch.GeometricConstraints
                                .Count);
                    }
                    catch { }
                }

                if (sketchNum == 0)
                    sb.AppendLine("No sketches found.");
                else
                    sb.AppendLine(new string('═', 40));

                sb.AppendLine(
                    "Total sketches: " + sketchNum);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get sketch summary: " +
                       ex.Message;
            }
        }

        // ─── Count entities ───────────────────────────────────────────

        private string CountSketchEntities(string type)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sketch analysis requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                PlanarSketch target = null;

                if (_inventorApplication
                        .ActiveEditObject
                    is PlanarSketch active)
                    target = active;
                else if (part.ComponentDefinition
                    .Sketches.Count > 0)
                    target =
                        part.ComponentDefinition
                            .Sketches[1];

                if (target == null)
                    return "No sketch found.";

                var sb = new StringBuilder();
                sb.AppendLine("── Sketch Entities ──");
                sb.AppendLine(
                    "Sketch: " + target.Name);
                sb.AppendLine(new string('-', 40));

                if (type == "lines" || type == "all")
                    sb.AppendLine(
                        "Lines:   " +
                        target.SketchLines.Count);

                if (type == "circles" || type == "all")
                    sb.AppendLine(
                        "Circles: " +
                        target.SketchCircles.Count);

                if (type == "all")
                {
                    sb.AppendLine(
                        "Arcs:    " +
                        target.SketchArcs.Count);
                    sb.AppendLine(
                        "Points:  " +
                        target.SketchPoints.Count);
                    sb.AppendLine(
                        "Dims:    " +
                        target.DimensionConstraints.Count);
                    sb.AppendLine(
                        "GCons:   " +
                        target.GeometricConstraints.Count);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to count entities: " +
                       ex.Message;
            }
        }

        // ─── List sketch dimensions ───────────────────────────────────

        private string ListSketchDimensions()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sketch analysis requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                PlanarSketch target = null;

                if (_inventorApplication
                        .ActiveEditObject
                    is PlanarSketch active)
                    target = active;
                else if (part.ComponentDefinition
                    .Sketches.Count > 0)
                    target =
                        part.ComponentDefinition
                            .Sketches[1];

                if (target == null)
                    return "No sketch found.";

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Sketch Dimensions ──");
                sb.AppendLine(
                    "Sketch: " + target.Name);
                sb.AppendLine(new string('-', 40));

                int i = 1;
                foreach (object dim in
                    target.DimensionConstraints)
                {
                    try
                    {
                        var prop =
                            dim.GetType()
                                .GetProperty("Parameter");
                        if (prop != null)
                        {
                            var param =
                                prop.GetValue(dim);
                            if (param != null)
                            {
                                var nameProp =
                                    param.GetType()
                                        .GetProperty(
                                            "Name");
                                var valProp =
                                    param.GetType()
                                        .GetProperty(
                                            "Value");

                                string name =
                                    nameProp?
                                        .GetValue(param)
                                        ?.ToString()
                                    ?? "Dim " + i;

                                double val =
                                    Convert.ToDouble(
                                        valProp?
                                            .GetValue(
                                                param)
                                        ?? 0);

                                sb.AppendLine(
                                    i + ". " + name +
                                    " = " +
                                    Math.Round(
                                        val * CmToIn,
                                        4) + " in");
                                i++;
                            }
                        }
                    }
                    catch { }
                }

                if (i == 1)
                    sb.AppendLine(
                        "No dimensions found.");

                sb.AppendLine(new string('-', 40));
                sb.AppendLine("Total: " + (i - 1));

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list dimensions: " +
                       ex.Message;
            }
        }

        // ─── Get active sketch info ───────────────────────────────────

        private string GetActiveSketchInfo()
        {
            try
            {
                if (_inventorApplication
                        .ActiveEditObject
                    is PlanarSketch sketch)
                    return AnalyzeSketch(sketch);

                return
                    "No sketch is currently active." +
                    System.Environment.NewLine +
                    "Double-click a sketch in the " +
                    "model browser to activate it.";
            }
            catch (Exception ex)
            {
                return "Failed to get sketch info: " +
                       ex.Message;
            }
        }

        // ─── List all sketches ────────────────────────────────────────

        private string ListAllSketches()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Sketch analysis requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                var sb = new StringBuilder();
                sb.AppendLine("── All Sketches ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                int i = 1;
                foreach (PlanarSketch sketch in
                    part.ComponentDefinition.Sketches)
                {
                    try
                    {
                        string status =
                            GetConstraintStatus(sketch);
                        string icon =
                            status == "Fully Constrained"
                                ? "✅"
                                : status == "Empty Sketch"
                                    ? "○"
                                    : "⚠️";

                        sb.AppendLine(
                            i + ". " +
                            sketch.Name + " " + icon);
                        i++;
                    }
                    catch { }
                }

                if (i == 1)
                    sb.AppendLine("No sketches found.");

                sb.AppendLine(new string('-', 40));
                sb.AppendLine("Total: " + (i - 1));

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list sketches: " +
                       ex.Message;
            }
        }

        // ─── Constraint status ────────────────────────────────────────

        private string GetConstraintStatus(
            PlanarSketch sketch)
        {
            try
            {
                // Read sketch-level constraint
                // status — most reliable method
                // in Inventor 2023 API
                int lines =
                    sketch.SketchLines.Count;
                int circles =
                    sketch.SketchCircles.Count;
                int arcs =
                    sketch.SketchArcs.Count;
                int dims =
                    sketch.DimensionConstraints.Count;
                int gcons =
                    sketch.GeometricConstraints.Count;
                int points =
                    sketch.SketchPoints.Count;

                int totalEntities =
                    lines + circles + arcs;

                if (totalEntities == 0)
                    return "Empty Sketch";

                // Each line needs 4 constraints
                // Each circle needs 3
                // Each arc needs 5
                // Shared points reduce needed count
                int needed =
                    lines * 4 +
                    circles * 3 +
                    arcs * 5 -
                    points;

                int applied = dims + gcons;

                if (applied >= needed)
                    return "Fully Constrained";

                int remaining =
                    Math.Max(needed - applied, 0);

                return "Under Constrained (" +
                       remaining +
                       " constraints needed)";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}