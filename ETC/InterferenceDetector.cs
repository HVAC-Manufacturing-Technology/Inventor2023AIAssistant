using System;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class InterferenceDetector
    {
        private Inventor.Application _inventorApplication;
        private const double CmToIn = 0.393701;
        private const double Cm3ToIn3 = 0.0610237;

        public InterferenceDetector(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        public string TryHandleInterference(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("interference") ||
                n.Contains("clash") ||
                n.Contains("intersect") ||
                n.Contains("overlapping") ||
                n.Contains("parts touching") ||
                n.Contains("check clearance"))
                return RunInterferenceCheck();

            if (n.Contains("minimum clearance") ||
                n.Contains("min clearance") ||
                n.Contains("gap between"))
                return CheckMinimumClearance();

            if (n.Contains("assembly mass") ||
                n.Contains("assembly weight"))
                return GetAssemblyMassProperties();

            return null;
        }

        // ─── Interference check ───────────────────────────────────────

        private string RunInterferenceCheck()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                var sb = new StringBuilder();

                if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    AssemblyDocument asm =
                        (AssemblyDocument)doc;

                    sb.AppendLine(
                        "── Interference Check ──");
                    sb.AppendLine(
                        "Assembly: " + doc.DisplayName);
                    sb.AppendLine(new string('═', 40));

                    int componentCount =
                        asm.ComponentDefinition
                           .Occurrences.Count;

                    sb.AppendLine(
                        "Components: " + componentCount);
                    sb.AppendLine();

                    // Use bounding box overlap check
                    // Inventor 2023 API does not expose
                    // InterferenceResults directly
                    string result =
                        BoundingBoxOverlapCheck(asm);
                    sb.AppendLine(result);
                    sb.AppendLine();
                    sb.AppendLine(
                        "── Full Interference Analysis ──");
                    sb.AppendLine(
                        "For exact interference detection:");
                    sb.AppendLine(
                        "Inspect tab > Interference " +
                        "Analysis in Inventor.");
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    PartDocument part = (PartDocument)doc;

                    sb.AppendLine(
                        "── Part Body Check ──");
                    sb.AppendLine(
                        "Part: " + doc.DisplayName);
                    sb.AppendLine(new string('-', 40));

                    int bodyCount =
                        part.ComponentDefinition
                            .SurfaceBodies.Count;

                    if (bodyCount <= 1)
                        sb.AppendLine(
                            "✅ Single body — no " +
                            "self-interference possible.");
                    else
                    {
                        sb.AppendLine(
                            "⚠️ Multiple bodies: " +
                            bodyCount);
                        sb.AppendLine(
                            "Check for unintended " +
                            "separate bodies in the " +
                            "model browser.");
                    }
                }
                else
                {
                    return "Requires a Part or " +
                           "Assembly document.";
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to check interference: " +
                       ex.Message;
            }
        }

        // ─── Bounding box overlap check ───────────────────────────────

        private string BoundingBoxOverlapCheck(
            AssemblyDocument asm)
        {
            try
            {
                var sb = new StringBuilder();

                var boxes =
                    new System.Collections.Generic
                        .List<(string name, Box bb)>();

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Suppressed) continue;
                        Box bb = occ.RangeBox;
                        boxes.Add((occ.Name, bb));
                    }
                    catch { }
                }

                int overlapCount = 0;

                for (int i = 0; i < boxes.Count; i++)
                {
                    for (int j = i + 1;
                         j < boxes.Count; j++)
                    {
                        try
                        {
                            Box a = boxes[i].bb;
                            Box b = boxes[j].bb;

                            bool overlap =
                                a.MinPoint.X <
                                b.MaxPoint.X &&
                                a.MaxPoint.X >
                                b.MinPoint.X &&
                                a.MinPoint.Y <
                                b.MaxPoint.Y &&
                                a.MaxPoint.Y >
                                b.MinPoint.Y &&
                                a.MinPoint.Z <
                                b.MaxPoint.Z &&
                                a.MaxPoint.Z >
                                b.MinPoint.Z;

                            if (overlap)
                            {
                                overlapCount++;
                                sb.AppendLine(
                                    "⚠️ Overlap: " +
                                    boxes[i].name +
                                    " ↔ " +
                                    boxes[j].name);
                            }
                        }
                        catch { }
                    }
                }

                if (overlapCount == 0)
                    sb.AppendLine(
                        "✅ No bounding box overlaps " +
                        "detected.");
                else
                    sb.AppendLine(
                        System.Environment.NewLine +
                        "⚠️ " + overlapCount +
                        " potential overlap(s) found." +
                        System.Environment.NewLine +
                        "Confirm with Inspect > " +
                        "Interference Analysis.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Overlap check failed: " + ex.Message;
            }
        }

        // ─── Minimum clearance ────────────────────────────────────────

        private string CheckMinimumClearance()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Clearance check requires " +
                           "an Assembly document.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Minimum Clearance Check ──");
                sb.AppendLine(
                    "Assembly: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                double minGap = double.MaxValue;
                string minPairA = "";
                string minPairB = "";

                var boxes =
                    new System.Collections.Generic
                        .List<(string name, Box bb)>();

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Suppressed) continue;
                        boxes.Add(
                            (occ.Name, occ.RangeBox));
                    }
                    catch { }
                }

                for (int i = 0; i < boxes.Count; i++)
                {
                    for (int j = i + 1;
                         j < boxes.Count; j++)
                    {
                        try
                        {
                            Box a = boxes[i].bb;
                            Box b = boxes[j].bb;

                            double gapX = Math.Max(0,
                                Math.Max(
                                    a.MinPoint.X -
                                    b.MaxPoint.X,
                                    b.MinPoint.X -
                                    a.MaxPoint.X));

                            double gapY = Math.Max(0,
                                Math.Max(
                                    a.MinPoint.Y -
                                    b.MaxPoint.Y,
                                    b.MinPoint.Y -
                                    a.MaxPoint.Y));

                            double gapZ = Math.Max(0,
                                Math.Max(
                                    a.MinPoint.Z -
                                    b.MaxPoint.Z,
                                    b.MinPoint.Z -
                                    a.MaxPoint.Z));

                            double gap =
                                Math.Sqrt(
                                    gapX * gapX +
                                    gapY * gapY +
                                    gapZ * gapZ) *
                                CmToIn;

                            if (gap < minGap)
                            {
                                minGap = gap;
                                minPairA = boxes[i].name;
                                minPairB = boxes[j].name;
                            }
                        }
                        catch { }
                    }
                }

                if (minGap == double.MaxValue)
                    sb.AppendLine(
                        "Could not calculate clearance.");
                else if (minGap <= 0)
                {
                    sb.AppendLine(
                        "❌ Clearance: 0 in — " +
                        "interference detected");
                    sb.AppendLine(
                        "Between: " + minPairA);
                    sb.AppendLine(
                        "And:     " + minPairB);
                }
                else
                {
                    sb.AppendLine(
                        "Minimum clearance: " +
                        Math.Round(minGap, 4) + " in");
                    sb.AppendLine(
                        "Between: " + minPairA);
                    sb.AppendLine(
                        "And:     " + minPairB);

                    if (minGap < 0.125)
                        sb.AppendLine(
                            "⚠️ Very tight — review " +
                            "for assembly fit.");
                    else
                        sb.AppendLine(
                            "✅ Clearance looks adequate.");
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Note: Based on bounding boxes." +
                    System.Environment.NewLine +
                    "Use Inspect > Measure for " +
                    "exact face-to-face clearance.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to check clearance: " +
                       ex.Message;
            }
        }

        // ─── Assembly mass properties ─────────────────────────────────

        private string GetAssemblyMassProperties()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Assembly mass requires " +
                           "an Assembly document.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Assembly Mass Properties ──");
                sb.AppendLine(
                    "Assembly: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                try
                {
                    MassProperties mp =
                        asm.ComponentDefinition
                           .MassProperties;

                    double mass = mp.Mass;
                    double lbs = mass * 1000 * 0.00220462;

                    sb.AppendLine(
                        "Total weight: " +
                        Math.Round(lbs, 4) + " lbs");
                    sb.AppendLine(
                        "Total mass:   " +
                        Math.Round(mass, 4) + " kg");

                    try
                    {
                        Point com = mp.CenterOfMass;
                        sb.AppendLine(
                            "Center of mass:");
                        sb.AppendLine(
                            "  X: " +
                            Math.Round(
                                com.X * CmToIn, 4) +
                            " in");
                        sb.AppendLine(
                            "  Y: " +
                            Math.Round(
                                com.Y * CmToIn, 4) +
                            " in");
                        sb.AppendLine(
                            "  Z: " +
                            Math.Round(
                                com.Z * CmToIn, 4) +
                            " in");
                    }
                    catch { }

                    sb.AppendLine();
                    sb.AppendLine(
                        "Components: " +
                        asm.ComponentDefinition
                           .Occurrences.Count);
                }
                catch (Exception ex)
                {
                    sb.AppendLine(
                        "Could not read mass: " +
                        ex.Message +
                        System.Environment.NewLine +
                        "Assign materials to all " +
                        "components first.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get mass: " + ex.Message;
            }
        }
    }
}