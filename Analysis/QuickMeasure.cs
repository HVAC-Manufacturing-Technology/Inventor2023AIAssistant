using System;
using System.Collections.Generic;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class QuickMeasure
    {
        private readonly Inventor.Application _app;
        private const double CmToIn = 0.393701;
        private const double Cm2ToIn2 = 0.155001;

        public QuickMeasure(Inventor.Application app)
        {
            _app = app;
        }

        // ── Entry point ───────────────────────────────
        public string TryHandleQuickMeasure(
            string prompt)
        {
            if (prompt == null) return null;
            string n = prompt.Trim().ToLowerInvariant();

            if (n == "measure" ||
                n == "measure selected" ||
                n == "measure selection")
                return MeasureSelected();

            if (n == "measure edge" ||
                n == "measure selected edge" ||
                n == "edge length")
                return MeasureEdge();

            if (n == "measure face" ||
                n == "measure selected face" ||
                n == "face area")
                return MeasureFace();

            if (n == "surface area" ||
                n == "total surface area" ||
                n == "part surface area")
                return TotalSurfaceArea();

            if (n == "bounding box" ||
                n == "model size" ||
                n == "part size" ||
                n == "part dimensions")
                return BoundingBox();

            if (n == "measure hole" ||
                n == "hole diameter" ||
                n == "selected hole diameter")
                return MeasureHole();

            if (n == "measure radius" ||
                n == "selected radius" ||
                n == "arc radius")
                return MeasureRadius();

            if (n == "measure angle" ||
                n == "selected angle" ||
                n == "angle between faces")
                return MeasureAngle();

            if (n == "minimum distance" ||
                n == "min distance" ||
                n == "minimum clearance distance")
                return MinimumDistance();

            if (n == "measure all edges" ||
                n == "all edge lengths" ||
                n == "edge length summary")
                return MeasureAllEdges();

            return null;
        }

        // ── Measure selected ──────────────────────────
        private string MeasureSelected()
        {
            try
            {
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                if (sel == null || sel.Count == 0)
                    return
                        "Nothing is selected. " +
                        "Select geometry first.";

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine(
                    "\u2500\u2500 Measure Selected " +
                    "\u2500\u2500");

                int i = 1;
                foreach (object item in sel)
                {
                    try
                    {
                        if (item is Edge edge)
                        {
                            double len =
                                GetEdgeLength(edge);
                            sb.AppendLine(
                                i + ". Edge: " +
                                Math.Round(len, 4) +
                                " in");
                        }
                        else if (item is Face face)
                        {
                            double area =
                                GetFaceArea(face);
                            sb.AppendLine(
                                i + ". Face area: " +
                                Math.Round(area, 4) +
                                " in\u00b2");
                        }
                        else if (item is PartFeature pf)
                        {
                            sb.AppendLine(
                                i + ". Feature: " +
                                pf.Name);
                        }
                        else if (item is
                            ComponentOccurrence occ)
                        {
                            sb.AppendLine(
                                i + ". Component: " +
                                occ.Name);
                        }
                        else
                        {
                            sb.AppendLine(
                                i + ". " +
                                item.GetType().Name);
                        }
                        i++;
                    }
                    catch
                    {
                        sb.AppendLine(
                            i++ + ". (unreadable)");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to measure: " +
                       ex.Message;
            }
        }

        // ── Measure edge ──────────────────────────────
        private string MeasureEdge()
        {
            try
            {
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                Edge edge = null;
                if (sel != null)
                    foreach (object item in sel)
                        if (item is Edge e)
                        { edge = e; break; }

                if (edge == null)
                    return
                        "No edge selected. " +
                        "Select an edge first.";

                double len = GetEdgeLength(edge);

                return
                    "\u2705 Edge length: " +
                    Math.Round(len, 4) + " in" +
                    System.Environment.NewLine +
                    "(" +
                    Math.Round(len * 25.4, 3) +
                    " mm)";
            }
            catch (Exception ex)
            {
                return "Failed to measure edge: " +
                       ex.Message;
            }
        }

        // ── Measure face ──────────────────────────────
        private string MeasureFace()
        {
            try
            {
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                Face face = null;
                if (sel != null)
                    foreach (object item in sel)
                        if (item is Face f)
                        { face = f; break; }

                if (face == null)
                    return
                        "No face selected. " +
                        "Select a face first.";

                double area = GetFaceArea(face);

                return
                    "\u2705 Face area: " +
                    Math.Round(area, 4) + " in\u00b2" +
                    System.Environment.NewLine +
                    "(" +
                    Math.Round(area * 645.16, 2) +
                    " mm\u00b2)";
            }
            catch (Exception ex)
            {
                return "Failed to measure face: " +
                       ex.Message;
            }
        }

        // ── Total surface area ────────────────────────
        private string TotalSurfaceArea()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "No active document.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return
                        "Surface area requires " +
                        "a Part document.";

                PartDocument part = (PartDocument)doc;
                double total = 0;
                int count = 0;

                foreach (Face f in
                    part.ComponentDefinition
                        .SurfaceBodies[1].Faces)
                {
                    try
                    {
                        total += f.Evaluator.Area;
                        count++;
                    }
                    catch { }
                }

                double totalIn2 = total * Cm2ToIn2;

                return
                    "\u2705 Total surface area:" +
                    System.Environment.NewLine +
                    Math.Round(totalIn2, 4) +
                    " in\u00b2" +
                    System.Environment.NewLine +
                    "(" +
                    Math.Round(totalIn2 * 645.16, 2) +
                    " mm\u00b2)" +
                    System.Environment.NewLine +
                    "(" + count + " faces)";
            }
            catch (Exception ex)
            {
                return "Failed to get surface area: " +
                       ex.Message;
            }
        }

        // ── Bounding box ──────────────────────────────
        private string BoundingBox()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "No active document.";

                Box rangeBox = null;

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                    rangeBox =
                        ((PartDocument)doc)
                            .ComponentDefinition
                            .RangeBox;
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                    rangeBox =
                        ((AssemblyDocument)doc)
                            .ComponentDefinition
                            .RangeBox;
                else
                    return
                        "Bounding box requires " +
                        "a Part or Assembly.";

                if (rangeBox == null)
                    return
                        "Could not get bounding box.";

                double w = Math.Abs(
                    rangeBox.MaxPoint.X -
                    rangeBox.MinPoint.X) * CmToIn;
                double h = Math.Abs(
                    rangeBox.MaxPoint.Y -
                    rangeBox.MinPoint.Y) * CmToIn;
                double d = Math.Abs(
                    rangeBox.MaxPoint.Z -
                    rangeBox.MinPoint.Z) * CmToIn;

                return
                    "\u2705 Bounding Box:" +
                    System.Environment.NewLine +
                    "Width:  " +
                    Math.Round(w, 4) + " in" +
                    System.Environment.NewLine +
                    "Height: " +
                    Math.Round(h, 4) + " in" +
                    System.Environment.NewLine +
                    "Depth:  " +
                    Math.Round(d, 4) + " in" +
                    System.Environment.NewLine +
                    "(" +
                    Math.Round(w, 3) + " x " +
                    Math.Round(h, 3) + " x " +
                    Math.Round(d, 3) + " in)";
            }
            catch (Exception ex)
            {
                return "Failed to get bounding box: " +
                       ex.Message;
            }
        }

        // ── Measure hole ──────────────────────────────
        private string MeasureHole()
        {
            try
            {
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                if (sel == null || sel.Count == 0)
                    return
                        "Select a hole face or " +
                        "circular edge first.";

                foreach (object item in sel)
                {
                    if (item is Edge edge)
                    {
                        double r = GetEdgeRadius(edge);
                        if (r > 0)
                        {
                            double dia = r * 2 * CmToIn;
                            return
                                "\u2705 Hole diameter: " +
                                Math.Round(dia, 4) +
                                " in" +
                                System.Environment
                                    .NewLine +
                                "Radius: " +
                                Math.Round(dia / 2, 4) +
                                " in";
                        }
                    }

                    if (item is Face face)
                    {
                        double area = GetFaceArea(face);
                        double r =
                            Math.Sqrt(area / Math.PI);
                        double dia = r * 2;
                        return
                            "\u2705 Estimated diameter: " +
                            Math.Round(dia, 4) + " in" +
                            System.Environment.NewLine +
                            "(from face area)";
                    }
                }

                return
                    "No circular edge or face found.";
            }
            catch (Exception ex)
            {
                return "Failed to measure hole: " +
                       ex.Message;
            }
        }

        // ── Measure radius ────────────────────────────
        private string MeasureRadius()
        {
            try
            {
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                Edge edge = null;
                if (sel != null)
                    foreach (object item in sel)
                        if (item is Edge e)
                        { edge = e; break; }

                if (edge == null)
                    return
                        "No edge selected. " +
                        "Select a circular edge.";

                double r = GetEdgeRadius(edge);
                if (r <= 0)
                    return
                        "Selected edge does not " +
                        "appear to be circular.";

                double rIn = r * CmToIn;

                return
                    "\u2705 Radius: " +
                    Math.Round(rIn, 4) + " in" +
                    System.Environment.NewLine +
                    "Diameter: " +
                    Math.Round(rIn * 2, 4) + " in";
            }
            catch (Exception ex)
            {
                return "Failed to measure radius: " +
                       ex.Message;
            }
        }

        // ── Measure angle ─────────────────────────────
        private string MeasureAngle()
        {
            try
            {
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                if (sel == null || sel.Count < 2)
                    return
                        "Select 2 faces to measure " +
                        "the angle between them.";

                var faces = new List<Face>();
                foreach (object item in sel)
                    if (item is Face f) faces.Add(f);

                if (faces.Count < 2)
                    return
                        "Select exactly 2 faces for " +
                        "angle measurement.";

                double angle =
                    _app.MeasureTools.GetAngle(
                        faces[0], faces[1]);

                double deg =
                    angle * (180.0 / Math.PI);

                return
                    "\u2705 Angle between faces: " +
                    Math.Round(deg, 3) + "\u00b0";
            }
            catch (Exception ex)
            {
                return "Failed to measure angle: " +
                       ex.Message;
            }
        }

        // ── Minimum distance ──────────────────────────
        private string MinimumDistance()
        {
            try
            {
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                if (sel == null || sel.Count < 2)
                    return
                        "Select 2 items to measure " +
                        "minimum distance.";

                object item1 = null;
                object item2 = null;
                int idx = 0;
                foreach (object item in sel)
                {
                    if (idx == 0) item1 = item;
                    if (idx == 1) item2 = item;
                    idx++;
                    if (idx >= 2) break;
                }

                double dist =
                    _app.MeasureTools
                        .GetMinimumDistance(
                            item1, item2);

                double distIn = dist * CmToIn;

                return
                    "\u2705 Minimum distance: " +
                    Math.Round(distIn, 4) + " in" +
                    System.Environment.NewLine +
                    "(" +
                    Math.Round(distIn * 25.4, 3) +
                    " mm)";
            }
            catch (Exception ex)
            {
                return "Failed to measure distance: " +
                       ex.Message;
            }
        }

        // ── Measure all edges ─────────────────────────
        private string MeasureAllEdges()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return
                        "Measure all edges requires " +
                        "a Part document.";

                PartDocument part = (PartDocument)doc;
                var lengths =
                    new Dictionary<double, int>();

                foreach (Edge e in
                    part.ComponentDefinition
                        .SurfaceBodies[1].Edges)
                {
                    try
                    {
                        double len =
                            Math.Round(
                                GetEdgeLength(e), 4);
                        if (lengths.ContainsKey(len))
                            lengths[len]++;
                        else
                            lengths[len] = 1;
                    }
                    catch { }
                }

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine(
                    "\u2500\u2500 Edge Lengths " +
                    "\u2500\u2500");

                var sorted =
                    new List<KeyValuePair<double, int>>(
                        lengths);
                sorted.Sort((a, b) =>
                    b.Value.CompareTo(a.Value));

                int total = 0;
                foreach (var kv in sorted)
                {
                    sb.AppendLine(
                        kv.Key.ToString("0.0000")
                            .PadRight(12) +
                        " in  x" + kv.Value);
                    total += kv.Value;
                }

                sb.AppendLine();
                sb.AppendLine("Total edges: " + total);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to measure edges: " +
                       ex.Message;
            }
        }

        // ── Helpers ───────────────────────────────────
        private double GetEdgeLength(Edge edge)
        {
            try
            {
                CurveEvaluator eval = edge.Evaluator;
                double startParam = 0;
                double endParam = 0;
                eval.GetParamExtents(
                    out startParam, out endParam);
                double startLength = 0;
                double endLength = 0;
                eval.GetLengthAtParam(
                    startParam, endParam, out startLength);
                eval.GetLengthAtParam(
                    endParam, startParam, out endLength);
                return Math.Abs(endLength) * CmToIn;
            }
            catch { return 0; }
        }
        private double GetFaceArea(Face face)
        {
            try
            {
                return face.Evaluator.Area * Cm2ToIn2;
            }
            catch { return 0; }
        }

        private double GetEdgeRadius(Edge edge)
        {
            try
            {
                object geom = edge.Geometry;
                if (geom is Circle c)
                    return c.Radius;
                if (geom is Arc3d a)
                    return a.Radius;
                return 0;
            }
            catch { return 0; }
        }
    }
}