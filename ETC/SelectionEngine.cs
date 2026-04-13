using System;
using System.Text;
using System.Text.RegularExpressions;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class SelectionEngine
    {
        private Inventor.Application _inventorApplication;
        private const double CmToIn = 0.393701;

        public SelectionEngine(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        public string TryHandleSelection(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("deselect all") ||
                n.Contains("clear selection"))
                return DeselectAll();

            if (n.Contains("select all hole") ||
                (n.Contains("select") &&
                 n.Contains("hole")))
                return SelectHolesBySize(prompt);

            if (n.Contains("select all face") ||
                (n.Contains("select") &&
                 n.Contains("face") &&
                 (n.Contains("parallel") ||
                  n.Contains("plane"))))
                return SelectFacesByPlane(prompt);

            if (n.Contains("select all edge") ||
                (n.Contains("select") &&
                 n.Contains("edge") &&
                 n.Contains("longer")))
                return SelectEdgesByLength(prompt);

            if (n.Contains("select all fillet") ||
                (n.Contains("select") &&
                 n.Contains("fillet")))
                return SelectFillets(prompt);

            if (n.Contains("select all bend") ||
                (n.Contains("select") &&
                 n.Contains("bend")))
                return SelectSheetMetalBends();

            if (n.Contains("select") &&
                n.Contains("suppressed"))
                return SelectSuppressedFeatures();

            if (n.Contains("what is selected") ||
                n.Contains("show selection") ||
                n.Contains("selection info") ||
                n.Contains("list selected"))
                return ShowCurrentSelection();

            if (n.Contains("select all") &&
                n.Contains("component"))
                return SelectAllComponents();

            return null;
        }

        // ─── Deselect all ─────────────────────────────────────────────

        private string DeselectAll()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                doc.SelectSet.Clear();
                return "✅ Selection cleared.";
            }
            catch (Exception ex)
            {
                return "Failed to clear selection: " +
                       ex.Message;
            }
        }

        // ─── Select holes by size ─────────────────────────────────────

        private string SelectHolesBySize(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Select holes requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                double maxSize = -1;
                double minSize = -1;
                double exactSize = -1;

                Match m = Regex.Match(
                    prompt, @"smaller than ([\d.]+)");
                if (m.Success)
                    maxSize = double.Parse(
                        m.Groups[1].Value);

                m = Regex.Match(
                    prompt, @"larger than ([\d.]+)");
                if (m.Success)
                    minSize = double.Parse(
                        m.Groups[1].Value);

                m = Regex.Match(
                    prompt, @"equal to ([\d.]+)");
                if (m.Success)
                    exactSize = double.Parse(
                        m.Groups[1].Value);

                doc.SelectSet.Clear();

                int selected = 0;
                var sb = new StringBuilder();
                sb.AppendLine("── Select Holes ──");

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed) continue;

                        if (!(feature is HoleFeature hf))
                            continue;

                        double dia = -1;
                        try
                        {
                            dia =
                                hf.HoleDiameter.Value *
                                CmToIn;
                        }
                        catch { }

                        bool passes = true;

                        if (maxSize > 0 && dia > 0)
                            passes = dia < maxSize;
                        if (minSize > 0 && dia > 0)
                            passes = dia > minSize;
                        if (exactSize > 0 && dia > 0)
                            passes =
                                Math.Abs(dia - exactSize)
                                < 0.001;

                        if (passes)
                        {
                            try
                            {
                                doc.SelectSet
                                    .Select(feature);
                                selected++;
                                sb.AppendLine(
                                    "✅ " + hf.Name +
                                    (dia > 0
                                        ? " ⌀" +
                                          Math.Round(
                                              dia, 4) +
                                          " in"
                                        : ""));
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Selected: " + selected +
                    " hole(s)");

                if (selected == 0)
                    sb.AppendLine(
                        "No holes matched the filter." +
                        System.Environment.NewLine +
                        "Try: 'select all holes " +
                        "smaller than 0.25'");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to select holes: " +
                       ex.Message;
            }
        }

        // ─── Select faces by plane ────────────────────────────────────

        private string SelectFacesByPlane(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Select faces requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                string n = prompt.ToLowerInvariant();

                double nx = 0, ny = 0, nz = 1;
                string planeName = "XY plane";

                if (n.Contains("xy") ||
                    n.Contains("horizontal") ||
                    n.Contains("top") ||
                    n.Contains("bottom"))
                {
                    nx = 0; ny = 0; nz = 1;
                    planeName = "XY plane";
                }
                else if (n.Contains("xz") ||
                         n.Contains("front") ||
                         n.Contains("back"))
                {
                    nx = 0; ny = 1; nz = 0;
                    planeName = "XZ plane";
                }
                else if (n.Contains("yz") ||
                         n.Contains("side") ||
                         n.Contains("left") ||
                         n.Contains("right"))
                {
                    nx = 1; ny = 0; nz = 0;
                    planeName = "YZ plane";
                }

                doc.SelectSet.Clear();

                int selected = 0;
                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Select Faces Parallel to " +
                    planeName + " ──");

                foreach (SurfaceBody body in
                    part.ComponentDefinition.SurfaceBodies)
                {
                    foreach (Face face in body.Faces)
                    {
                        try
                        {
                            if (face.SurfaceType !=
                                SurfaceTypeEnum
                                    .kPlaneSurface)
                                continue;

                            try
                            {
                                Plane plane =
                                    face.Geometry as Plane;

                                if (plane == null)
                                    continue;

                                UnitVector faceNormal =
                                    plane.Normal;

                                double fnx =
                                    Math.Abs(faceNormal.X);
                                double fny =
                                    Math.Abs(faceNormal.Y);
                                double fnz =
                                    Math.Abs(faceNormal.Z);

                                bool parallel = false;

                                if (nz == 1 && fnz > 0.99)
                                    parallel = true;
                                else if (ny == 1 &&
                                         fny > 0.99)
                                    parallel = true;
                                else if (nx == 1 &&
                                         fnx > 0.99)
                                    parallel = true;

                                if (parallel)
                                {
                                    doc.SelectSet
                                        .Select(face);
                                    selected++;
                                }
                            }
                            catch { }
                        }
                        catch { }
                    }
                }

                sb.AppendLine(
                    "Selected: " + selected +
                    " face(s) parallel to " +
                    planeName);

                if (selected == 0)
                    sb.AppendLine(
                        "No matching faces found.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to select faces: " +
                       ex.Message;
            }
        }

        // ─── Select edges by length ───────────────────────────────────

        private string SelectEdgesByLength(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Select edges requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                double minLength = 2.0;
                double maxLength = -1;

                Match m = Regex.Match(
                    prompt, @"longer than ([\d.]+)");
                if (m.Success)
                    minLength = double.Parse(
                        m.Groups[1].Value);

                m = Regex.Match(
                    prompt, @"shorter than ([\d.]+)");
                if (m.Success)
                    maxLength = double.Parse(
                        m.Groups[1].Value);

                doc.SelectSet.Clear();

                int selected = 0;
                var sb = new StringBuilder();
                sb.AppendLine("── Select Edges ──");

                if (minLength > 0)
                    sb.AppendLine(
                        "Filter: longer than " +
                        minLength + " in");

                foreach (SurfaceBody body in
                    part.ComponentDefinition.SurfaceBodies)
                {
                    foreach (Edge edge in body.Edges)
                    {
                        try
                        {
                            double len = 0;
                            try
                            {
                                double startParam = 0;
                                double endParam = 0;
                                edge.Evaluator
                                    .GetParamExtents(
                                        out startParam,
                                        out endParam);
                                len =
                                    Math.Abs(
                                        endParam -
                                        startParam) *
                                    CmToIn;
                            }
                            catch { }

                            bool passes = true;

                            if (minLength > 0)
                                passes = len >= minLength;
                            if (maxLength > 0)
                                passes =
                                    passes &&
                                    len <= maxLength;

                            if (passes)
                            {
                                doc.SelectSet
                                    .Select(edge);
                                selected++;
                            }
                        }
                        catch { }
                    }
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Selected: " + selected +
                    " edge(s)");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to select edges: " +
                       ex.Message;
            }
        }

        // ─── Select fillets ───────────────────────────────────────────

        private string SelectFillets(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Select fillets requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                doc.SelectSet.Clear();

                int selected = 0;
                var sb = new StringBuilder();
                sb.AppendLine("── Select Fillets ──");

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed) continue;

                        string type =
                            feature.Type.ToString()
                                .ToLower();

                        if (!type.Contains("fillet"))
                            continue;

                        doc.SelectSet.Select(feature);
                        selected++;
                        sb.AppendLine(
                            "✅ " + feature.Name);
                    }
                    catch { }
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Selected: " + selected +
                    " fillet(s)");

                if (selected == 0)
                    sb.AppendLine("No fillets found.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to select fillets: " +
                       ex.Message;
            }
        }

        // ─── Select sheet metal bends ─────────────────────────────────

        private string SelectSheetMetalBends()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Select bends requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                doc.SelectSet.Clear();

                int selected = 0;
                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Select Sheet Metal Bends ──");

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed) continue;

                        string type =
                            feature.Type.ToString()
                                .ToLower();
                        string name =
                            feature.Name.ToLower();

                        if (type.Contains("bend") ||
                            name.Contains("bend") ||
                            type.Contains("sheetmetal"))
                        {
                            doc.SelectSet.Select(feature);
                            selected++;
                            sb.AppendLine(
                                "✅ " + feature.Name);
                        }
                    }
                    catch { }
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Selected: " + selected +
                    " bend(s)");

                if (selected == 0)
                    sb.AppendLine(
                        "No bends found. Make sure " +
                        "this is a sheet metal part.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to select bends: " +
                       ex.Message;
            }
        }

        // ─── Select suppressed features ───────────────────────────────

        private string SelectSuppressedFeatures()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Requires a Part document.";

                PartDocument part = (PartDocument)doc;

                doc.SelectSet.Clear();

                int selected = 0;
                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Select Suppressed Features ──");

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed)
                        {
                            doc.SelectSet.Select(feature);
                            selected++;
                            sb.AppendLine(
                                "✅ " + feature.Name);
                        }
                    }
                    catch { }
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Selected: " + selected +
                    " suppressed feature(s)");

                if (selected == 0)
                    sb.AppendLine(
                        "No suppressed features found.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to select suppressed: " +
                       ex.Message;
            }
        }

        // ─── Show current selection ───────────────────────────────────

        private string ShowCurrentSelection()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                SelectSet sel = doc.SelectSet;

                if (sel == null || sel.Count == 0)
                    return
                        "Nothing is currently selected.";

                var sb = new StringBuilder();
                sb.AppendLine("── Current Selection ──");
                sb.AppendLine("Count: " + sel.Count);
                sb.AppendLine(new string('-', 40));

                int i = 1;
                foreach (object item in sel)
                {
                    try
                    {
                        string desc =
                            item.GetType().Name;

                        if (item is PartFeature pf)
                            desc = "Feature: " + pf.Name;
                        else if (item is Face face)
                            desc = "Face (" +
                                   face.SurfaceType + ")";
                        else if (item is Edge)
                            desc = "Edge";
                        else if (item is
                            ComponentOccurrence co)
                            desc = "Component: " + co.Name;

                        sb.AppendLine(i + ". " + desc);
                        i++;
                    }
                    catch
                    {
                        sb.AppendLine(
                            i + ". (unreadable)");
                        i++;
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read selection: " +
                       ex.Message;
            }
        }

        // ─── Select all components ────────────────────────────────────

        private string SelectAllComponents()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Requires an Assembly document.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;

                doc.SelectSet.Clear();

                int selected = 0;

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        doc.SelectSet.Select(occ);
                        selected++;
                    }
                    catch { }
                }

                return
                    "✅ Selected " + selected +
                    " component(s).";
            }
            catch (Exception ex)
            {
                return "Failed to select components: " +
                       ex.Message;
            }
        }
    }
}