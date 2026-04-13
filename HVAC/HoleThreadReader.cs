using System;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class HoleThreadReader
    {
        private Inventor.Application _inventorApplication;
        private const double CmToIn = 0.393701;

        public HoleThreadReader(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        public string TryHandleHoleThread(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("what size thread") ||
                n.Contains("thread size") ||
                n.Contains("thread callout") ||
                n.Contains("read thread") ||
                n.Contains("thread info"))
                return ReadThreadInfo();

            if (n.Contains("all threaded hole") ||
                n.Contains("show threaded") ||
                n.Contains("list threaded") ||
                n.Contains("find threaded"))
                return ListThreadedHoles();

            if (n.Contains("tap drill") ||
                n.Contains("tap size") ||
                n.Contains("drill for thread"))
                return GetTapDrillSize(prompt);

            if (n.Contains("hole callout") ||
                n.Contains("all hole callout") ||
                n.Contains("list hole") ||
                n.Contains("hole report") ||
                n.Contains("hole table"))
                return GetHoleReport();

            if (n.Contains("selected hole") ||
                n.Contains("this hole") ||
                n.Contains("hole diameter") ||
                n.Contains("what size hole"))
                return ReadSelectedHole();

            return null;
        }

        // ─── Read thread info ─────────────────────────────────────────

        private string ReadThreadInfo()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Thread reading requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine("── Thread Information ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 50));

                int threadCount = 0;

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed) continue;

                        if (feature is ThreadFeature tf)
                        {
                            threadCount++;
                            sb.AppendLine(
                                threadCount + ". " +
                                tf.Name);

                            try
                            {
                                ThreadInfo ti =
                                    tf.ThreadInfo;

                                sb.AppendLine(
                                    "   Type: " +
                                    ti.ThreadType);
                                sb.AppendLine(
                                    "   Designation: " +
                                    ti.ThreadDesignation);
                                sb.AppendLine(
                                    "   Direction: " +
                                    (ti.RightHanded
                                        ? "Right hand"
                                        : "Left hand"));

                                string tap =
                                    GetTapDrillForThread(
                                        ti
                                        .ThreadDesignation);
                                if (!string
                                    .IsNullOrWhiteSpace(tap))
                                    sb.AppendLine(
                                        "   Tap drill: " +
                                        tap);
                            }
                            catch { }

                            sb.AppendLine();
                        }
                        else if (feature is HoleFeature hf)
                        {
                            try
                            {
                                // Check if threaded by
                                // looking at feature name
                                // or type string
                                string htype =
                                    hf.HoleType.ToString()
                                        .ToLower();

                                if (htype.Contains(
                                        "thread"))
                                {
                                    threadCount++;
                                    sb.AppendLine(
                                        threadCount + ". " +
                                        hf.Name);

                                    double dia = -1;
                                    try
                                    {
                                        dia =
                                            hf.HoleDiameter
                                                .Value *
                                            CmToIn;
                                        sb.AppendLine(
                                            "   Dia: " +
                                            Math.Round(
                                                dia, 4) +
                                            " in");
                                    }
                                    catch { }

                                    sb.AppendLine(
                                        "   Type: " +
                                        hf.HoleType);
                                    sb.AppendLine();
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                if (threadCount == 0)
                    sb.AppendLine(
                        "No threaded features found." +
                        System.Environment.NewLine +
                        "Add Thread features via the " +
                        "3D Model tab > Thread.");

                sb.AppendLine(new string('-', 50));
                sb.AppendLine(
                    "Total threads: " + threadCount);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read thread info: " +
                       ex.Message;
            }
        }

        // ─── List threaded holes ──────────────────────────────────────

        private string ListThreadedHoles()
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

                var sb = new StringBuilder();
                sb.AppendLine("── Threaded Holes ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                int count = 0;

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed) continue;

                        if (feature is HoleFeature hf)
                        {
                            string htype =
                                hf.HoleType.ToString()
                                    .ToLower();

                            if (htype.Contains("thread"))
                            {
                                count++;
                                double dia = -1;

                                try
                                {
                                    dia =
                                        hf.HoleDiameter
                                            .Value *
                                        CmToIn;
                                }
                                catch { }

                                sb.AppendLine(
                                    count + ". " +
                                    hf.Name);

                                if (dia > 0)
                                    sb.AppendLine(
                                        "   Dia: " +
                                        Math.Round(
                                            dia, 4) +
                                        " in");
                            }
                        }

                        if (feature is ThreadFeature tf)
                        {
                            count++;
                            string designation = "";
                            try
                            {
                                designation =
                                    tf.ThreadInfo
                                        .ThreadDesignation;
                            }
                            catch { }

                            sb.AppendLine(
                                count + ". " +
                                tf.Name + " — " +
                                designation);
                        }
                    }
                    catch { }
                }

                sb.AppendLine(new string('-', 40));

                if (count == 0)
                    sb.AppendLine(
                        "No threaded holes found.");
                else
                    sb.AppendLine(
                        "Total threaded: " + count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list threaded holes: " +
                       ex.Message;
            }
        }

        // ─── Get tap drill size ───────────────────────────────────────

        private string GetTapDrillSize(string prompt)
        {
            try
            {
                string n = prompt.ToLowerInvariant();

                if (n.Contains("1/4-20") ||
                    n.Contains("1/4 20"))
                    return TapDrillInfo(
                        "1/4-20 UNC", "#7", 0.2010);
                if (n.Contains("1/4-28") ||
                    n.Contains("1/4 28"))
                    return TapDrillInfo(
                        "1/4-28 UNF", "#3", 0.2130);
                if (n.Contains("5/16-18") ||
                    n.Contains("5/16 18"))
                    return TapDrillInfo(
                        "5/16-18 UNC", "F", 0.2570);
                if (n.Contains("5/16-24") ||
                    n.Contains("5/16 24"))
                    return TapDrillInfo(
                        "5/16-24 UNF", "I", 0.2720);
                if (n.Contains("3/8-16") ||
                    n.Contains("3/8 16"))
                    return TapDrillInfo(
                        "3/8-16 UNC", "5/16\"", 0.3125);
                if (n.Contains("3/8-24") ||
                    n.Contains("3/8 24"))
                    return TapDrillInfo(
                        "3/8-24 UNF", "Q", 0.3320);
                if (n.Contains("1/2-13") ||
                    n.Contains("1/2 13"))
                    return TapDrillInfo(
                        "1/2-13 UNC", "27/64\"", 0.4219);
                if (n.Contains("1/2-20") ||
                    n.Contains("1/2 20"))
                    return TapDrillInfo(
                        "1/2-20 UNF", "29/64\"", 0.4531);
                if (n.Contains("#10-32") ||
                    n.Contains("10-32"))
                    return TapDrillInfo(
                        "#10-32 UNF", "#21", 0.1590);
                if (n.Contains("#10-24") ||
                    n.Contains("10-24"))
                    return TapDrillInfo(
                        "#10-24 UNC", "#25", 0.1495);
                if (n.Contains("#8-32") ||
                    n.Contains("8-32"))
                    return TapDrillInfo(
                        "#8-32 UNC", "#29", 0.1360);
                if (n.Contains("#6-32") ||
                    n.Contains("6-32"))
                    return TapDrillInfo(
                        "#6-32 UNC", "#36", 0.1065);

                return GetFullTapDrillTable();
            }
            catch (Exception ex)
            {
                return "Failed to get tap drill: " +
                       ex.Message;
            }
        }

        private string TapDrillInfo(
            string thread,
            string drill,
            double drillDia)
        {
            return
                "── Tap Drill ──" +
                System.Environment.NewLine +
                "Thread:    " + thread +
                System.Environment.NewLine +
                "Tap drill: " + drill +
                " (" + drillDia + " in)" +
                System.Environment.NewLine +
                "Clearance: " +
                Math.Round(drillDia + 0.03125, 4) +
                " in";
        }

        // ─── Hole report ──────────────────────────────────────────────

        private string GetHoleReport()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Hole report requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine("── Hole Report ──");
                sb.AppendLine(
                    "Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 55));
                sb.AppendLine(
                    "Name".PadRight(20) +
                    "Type".PadRight(16) +
                    "Dia (in)");
                sb.AppendLine(new string('-', 55));

                int total = 0;
                int threaded = 0;
                int drilled = 0;
                int counterbore = 0;
                int countersink = 0;

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed) continue;

                        if (!(feature is HoleFeature hf))
                            continue;

                        total++;

                        string typeName = "";
                        double dia = -1;

                        try
                        {
                            dia =
                                hf.HoleDiameter.Value *
                                CmToIn;
                        }
                        catch { }

                        string htype =
                            hf.HoleType.ToString()
                                .ToLower();

                        if (htype.Contains("thread"))
                        {
                            typeName = "Threaded";
                            threaded++;
                        }
                        else if (htype.Contains(
                            "counterbore"))
                        {
                            typeName = "Counterbore";
                            counterbore++;
                        }
                        else if (htype.Contains(
                            "countersink") ||
                            htype.Contains("sunk"))
                        {
                            typeName = "Countersink";
                            countersink++;
                        }
                        else
                        {
                            typeName = "Drilled";
                            drilled++;
                        }

                        sb.AppendLine(
                            hf.Name.PadRight(20) +
                            typeName.PadRight(16) +
                            (dia > 0
                                ? Math.Round(dia, 4)
                                    .ToString("0.0000")
                                : "?"));
                    }
                    catch { }
                }

                sb.AppendLine(new string('-', 55));
                sb.AppendLine("Total holes: " + total);

                if (threaded > 0)
                    sb.AppendLine(
                        "  Threaded:    " + threaded);
                if (drilled > 0)
                    sb.AppendLine(
                        "  Drilled:     " + drilled);
                if (counterbore > 0)
                    sb.AppendLine(
                        "  Counterbore: " + counterbore);
                if (countersink > 0)
                    sb.AppendLine(
                        "  Countersink: " + countersink);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get hole report: " +
                       ex.Message;
            }
        }

        // ─── Read selected hole ───────────────────────────────────────

        private string ReadSelectedHole()
        {
            try
            {
                SelectSet sel =
                    _inventorApplication.ActiveDocument
                        .SelectSet;

                if (sel == null || sel.Count == 0)
                    return
                        "Nothing is selected." +
                        System.Environment.NewLine +
                        "Select a hole feature in the " +
                        "model browser then try again.";

                var sb = new StringBuilder();
                sb.AppendLine("── Selected Hole Info ──");

                foreach (object item in sel)
                {
                    try
                    {
                        if (item is HoleFeature hf)
                        {
                            sb.AppendLine(
                                "Name: " + hf.Name);
                            sb.AppendLine(
                                "Type: " + hf.HoleType);

                            try
                            {
                                double dia =
                                    hf.HoleDiameter.Value *
                                    CmToIn;
                                sb.AppendLine(
                                    "Diameter: " +
                                    Math.Round(dia, 4) +
                                    " in");
                            }
                            catch { }
                        }
                        else if (item is Face)
                        {
                            sb.AppendLine(
                                "Face selected." +
                                System.Environment.NewLine +
                                "Select a Hole feature " +
                                "from the model browser " +
                                "for full details.");
                        }
                    }
                    catch { }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read selected hole: " +
                       ex.Message;
            }
        }

        // ─── Tap drill lookup ─────────────────────────────────────────

        private string GetTapDrillForThread(
            string designation)
        {
            if (string.IsNullOrWhiteSpace(designation))
                return "";

            string d = designation.ToUpperInvariant();

            if (d.Contains("1/4-20")) return "#7";
            if (d.Contains("1/4-28")) return "#3";
            if (d.Contains("5/16-18")) return "F";
            if (d.Contains("5/16-24")) return "I";
            if (d.Contains("3/8-16")) return "5/16\"";
            if (d.Contains("3/8-24")) return "Q";
            if (d.Contains("7/16-14")) return "U";
            if (d.Contains("7/16-20")) return "25/64\"";
            if (d.Contains("1/2-13")) return "27/64\"";
            if (d.Contains("1/2-20")) return "29/64\"";
            if (d.Contains("#10-32")) return "#21";
            if (d.Contains("#10-24")) return "#25";
            if (d.Contains("#8-32")) return "#29";
            if (d.Contains("#6-32")) return "#36";
            if (d.Contains("#4-40")) return "#43";

            return "";
        }

        private string GetFullTapDrillTable()
        {
            return
                "── Tap Drill Reference ──" +
                System.Environment.NewLine +
                new string('-', 42) +
                System.Environment.NewLine +
                "Thread".PadRight(14) +
                "Tap Drill".PadRight(14) +
                "Dia (in)" +
                System.Environment.NewLine +
                new string('-', 42) +
                System.Environment.NewLine +
                "#6-32".PadRight(14) +
                "#36".PadRight(14) + "0.1065" +
                System.Environment.NewLine +
                "#8-32".PadRight(14) +
                "#29".PadRight(14) + "0.1360" +
                System.Environment.NewLine +
                "#10-24".PadRight(14) +
                "#25".PadRight(14) + "0.1495" +
                System.Environment.NewLine +
                "#10-32".PadRight(14) +
                "#21".PadRight(14) + "0.1590" +
                System.Environment.NewLine +
                "1/4-20".PadRight(14) +
                "#7".PadRight(14) + "0.2010" +
                System.Environment.NewLine +
                "1/4-28".PadRight(14) +
                "#3".PadRight(14) + "0.2130" +
                System.Environment.NewLine +
                "5/16-18".PadRight(14) +
                "F".PadRight(14) + "0.2570" +
                System.Environment.NewLine +
                "5/16-24".PadRight(14) +
                "I".PadRight(14) + "0.2720" +
                System.Environment.NewLine +
                "3/8-16".PadRight(14) +
                "5/16\"".PadRight(14) + "0.3125" +
                System.Environment.NewLine +
                "3/8-24".PadRight(14) +
                "Q".PadRight(14) + "0.3320" +
                System.Environment.NewLine +
                "1/2-13".PadRight(14) +
                "27/64\"".PadRight(14) + "0.4219" +
                System.Environment.NewLine +
                "1/2-20".PadRight(14) +
                "29/64\"".PadRight(14) + "0.4531" +
                System.Environment.NewLine +
                new string('-', 42) +
                System.Environment.NewLine +
                "Say 'tap drill 3/8-16' for one size.";
        }
    }
}