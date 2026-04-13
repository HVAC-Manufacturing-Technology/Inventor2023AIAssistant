using System;
using System.Text;
using System.Text.RegularExpressions;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class HolePatternCalculator
    {
        private Inventor.Application _inventorApplication;
        private const double Pi = Math.PI;

        public HolePatternCalculator(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleHolePattern(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("bolt circle") ||
                n.Contains("bolt pattern") ||
                (n.Contains("hole") &&
                 n.Contains("bolt circle")))
                return BoltCirclePattern(prompt);

            if (n.Contains("rivet spacing") ||
                n.Contains("rivet pattern") ||
                (n.Contains("rivet") &&
                 n.Contains("spacing")))
                return RivetSpacing(prompt);

            if (n.Contains("flange bolt") ||
                n.Contains("flange pattern") ||
                n.Contains("duct flange"))
                return FlangePattern(prompt);

            if (n.Contains("hole spacing") ||
                (n.Contains("spacing") &&
                 n.Contains("hole") &&
                 n.Contains("across")))
                return EvenSpacing(prompt);

            if (n.Contains("holes across") ||
                n.Contains("holes over") ||
                n.Contains("holes in") ||
                (n.Contains("holes") &&
                 n.Contains("spacing")))
                return EvenSpacing(prompt);

            if (n.Contains("smacna") ||
                n.Contains("standing seam") ||
                n.Contains("duct seam"))
                return SmacnaPattern(prompt);

            if (n.Contains("corner bolt") ||
                n.Contains("corner pattern") ||
                n.Contains("4 corner"))
                return CornerBoltPattern(prompt);

            if (n.Contains("hole pattern"))
                return GeneralHolePattern(prompt);

            return null;
        }

        // ─── Bolt circle pattern ──────────────────────────────────────

        private string BoltCirclePattern(string prompt)
        {
            try
            {
                // Parse: "4 holes on 8 inch bolt circle"
                // Parse: "6 holes on 6 inch bolt circle"
                int count = ParseHoleCount(prompt, 4);
                double bcd = ParseDimension(
                    prompt, "bolt circle", 6.0);
                double holeDia = ParseHoleDia(prompt, 0.375);

                double radius = bcd / 2.0;
                double angleStep = 360.0 / count;
                double chord = 2.0 * radius *
                    Math.Sin(Pi / count);

                var sb = new StringBuilder();
                sb.AppendLine("── Bolt Circle Pattern ──");
                sb.AppendLine(
                    "Holes: " + count);
                sb.AppendLine(
                    "Bolt Circle Dia: " + bcd + " in");
                sb.AppendLine(
                    "Hole Diameter: " + holeDia + " in");
                sb.AppendLine(
                    "Angle per hole: " +
                    Math.Round(angleStep, 4) + "°");
                sb.AppendLine(
                    "Chord (hole to hole): " +
                    Math.Round(chord, 4) + " in");
                sb.AppendLine();
                sb.AppendLine("── Hole Coordinates ──");
                sb.AppendLine(
                    "(from center, 0° = 3 o'clock)");
                sb.AppendLine(
                    "X".PadRight(12) + "Y".PadRight(12) +
                    "Angle");
                sb.AppendLine(new string('-', 36));

                for (int i = 0; i < count; i++)
                {
                    double angleDeg = i * angleStep;
                    double angleRad = angleDeg * Pi / 180.0;
                    double x = radius * Math.Cos(angleRad);
                    double y = radius * Math.Sin(angleRad);
                    sb.AppendLine(
                        Math.Round(x, 4)
                            .ToString("0.0000")
                            .PadRight(12) +
                        Math.Round(y, 4)
                            .ToString("0.0000")
                            .PadRight(12) +
                        Math.Round(angleDeg, 2) + "°");
                }

                sb.AppendLine();
                sb.AppendLine("── HVAC Reference ──");
                sb.AppendLine(
                    "Min edge distance: " +
                    Math.Round(holeDia * 1.5, 3) + " in");
                sb.AppendLine(
                    "Min hole to hole: " +
                    Math.Round(holeDia * 3.0, 3) + " in");
                sb.AppendLine(
                    "Actual hole to hole: " +
                    Math.Round(chord, 4) + " in " +
                    (chord >= holeDia * 3.0
                        ? "✅ OK"
                        : "⚠️ Too close"));

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to calculate bolt circle: " +
                       ex.Message;
            }
        }

        // ─── Rivet spacing ────────────────────────────────────────────

        private string RivetSpacing(string prompt)
        {
            try
            {
                // Parse: "rivet spacing for 24 inch panel"
                // Parse: "rivet spacing 24 inches
                //         3/16 rivet"
                double length = ParseFirstDimension(
                    prompt, 24.0);
                double rivetDia = ParseHoleDia(
                    prompt, 0.1875);

                // SMACNA standard rivet spacing
                // 3/16 rivet = 1.5 in max spacing
                // for HVAC panels
                double maxSpacing = rivetDia <= 0.125
                    ? 1.0
                    : rivetDia <= 0.1875 ? 1.5
                    : rivetDia <= 0.25 ? 2.0
                    : 2.5;

                // Calculate optimal count and spacing
                int count = (int)Math.Ceiling(
                    length / maxSpacing) + 1;
                double actualSpacing =
                    length / (count - 1);

                // Edge distance = 2x rivet dia min
                double edgeDist =
                    Math.Max(rivetDia * 2.0, 0.375);

                var sb = new StringBuilder();
                sb.AppendLine("── Rivet Spacing ──");
                sb.AppendLine(
                    "Panel length: " + length + " in");
                sb.AppendLine(
                    "Rivet diameter: " + rivetDia + " in (" +
                    FractionFromDecimal(rivetDia) + ")");
                sb.AppendLine(
                    "Max SMACNA spacing: " +
                    maxSpacing + " in");
                sb.AppendLine();
                sb.AppendLine("── Recommended Layout ──");
                sb.AppendLine(
                    "Number of rivets: " + count);
                sb.AppendLine(
                    "Actual spacing: " +
                    Math.Round(actualSpacing, 4) + " in (" +
                    FractionFromDecimal(actualSpacing) + ")");
                sb.AppendLine(
                    "Edge distance: " +
                    Math.Round(edgeDist, 4) + " in");
                sb.AppendLine();
                sb.AppendLine("── Rivet Positions ──");
                sb.AppendLine(
                    "Position".PadRight(12) +
                    "From start");
                sb.AppendLine(new string('-', 28));

                for (int i = 0; i < count; i++)
                {
                    double pos = i == 0
                        ? edgeDist
                        : i == count - 1
                            ? length - edgeDist
                            : edgeDist +
                              (i * (length -
                               2 * edgeDist) /
                               (count - 1));

                    sb.AppendLine(
                        ("Rivet " + (i + 1)).PadRight(12) +
                        Math.Round(pos, 4) + " in");
                }

                sb.AppendLine();
                sb.AppendLine(
                    "Drill size: #" +
                    DrillNumberFromDia(rivetDia));

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to calculate rivet spacing: " +
                       ex.Message;
            }
        }

        // ─── Flange pattern ───────────────────────────────────────────

        private string FlangePattern(string prompt)
        {
            try
            {
                // Parse duct size from prompt
                // e.g. "flange bolt pattern for 12x12 duct"
                double width = 12.0;
                double height = 12.0;

                Match m = Regex.Match(
                    prompt,
                    @"(\d+\.?\d*)\s*[xX×]\s*(\d+\.?\d*)");
                if (m.Success)
                {
                    width = double.Parse(m.Groups[1].Value);
                    height = double.Parse(m.Groups[2].Value);
                }
                else
                {
                    double dim = ParseFirstDimension(
                        prompt, 12.0);
                    width = dim;
                    height = dim;
                }

                // SMACNA flange bolt spacing
                // max 8 inches on center for TDC/TDF
                double boltSpacing = 8.0;
                double boltDia = 0.375; // 3/8 inch standard
                double edgeDist = 0.75;

                // Bolts per side
                int boltsW = (int)Math.Ceiling(
                    (width - 2 * edgeDist) /
                    boltSpacing) + 1;
                int boltsH = (int)Math.Ceiling(
                    (height - 2 * edgeDist) /
                    boltSpacing) + 1;

                double spacingW = boltsW > 1
                    ? (width - 2 * edgeDist) / (boltsW - 1)
                    : width / 2;
                double spacingH = boltsH > 1
                    ? (height - 2 * edgeDist) / (boltsH - 1)
                    : height / 2;

                int totalBolts =
                    2 * boltsW + 2 * boltsH - 4;

                var sb = new StringBuilder();
                sb.AppendLine("── Duct Flange Bolt Pattern ──");
                sb.AppendLine(
                    "Duct size: " +
                    width + " x " + height + " in");
                sb.AppendLine(
                    "Bolt diameter: " + boltDia +
                    " in (3/8\")");
                sb.AppendLine(
                    "Max spacing (SMACNA): " +
                    boltSpacing + " in");
                sb.AppendLine(
                    "Edge distance: " + edgeDist + " in");
                sb.AppendLine();
                sb.AppendLine("── Layout ──");
                sb.AppendLine(
                    "Bolts on " + width +
                    "\" side: " + boltsW +
                    " @ " +
                    Math.Round(spacingW, 4) + " in OC");
                sb.AppendLine(
                    "Bolts on " + height +
                    "\" side: " + boltsH +
                    " @ " +
                    Math.Round(spacingH, 4) + " in OC");
                sb.AppendLine(
                    "Total bolts: " + totalBolts);
                sb.AppendLine();
                sb.AppendLine("── Hardware ──");
                sb.AppendLine(
                    "Bolt: 3/8\"-16 x 1\" hex bolt");
                sb.AppendLine(
                    "Nut:  3/8\"-16 hex nut");
                sb.AppendLine(
                    "Washer: 3/8\" flat washer");
                sb.AppendLine(
                    "Drill: 7/16\" (0.4375 in)");
                sb.AppendLine();
                sb.AppendLine(
                    "SMACNA TDC/TDF flange standard.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to calculate flange pattern: " +
                       ex.Message;
            }
        }

        // ─── Even spacing ─────────────────────────────────────────────

        private string EvenSpacing(string prompt)
        {
            try
            {
                // Parse: "6 holes across 20 inches"
                // Parse: "what is the spacing for
                //         6 holes across 20 inches"
                int count = ParseHoleCount(prompt, 4);
                double length = ParseFirstDimension(
                    prompt, 20.0);
                double holeDia = ParseHoleDia(
                    prompt, 0.25);

                // Edge distance
                double edgeDist = holeDia * 2.0;

                // Spacing options
                double spacingEdge =
                    count > 1
                        ? (length - 2 * edgeDist) /
                          (count - 1)
                        : length / 2;

                double spacingCenterline =
                    count > 1
                        ? length / (count - 1)
                        : length;

                double spacingEqualPitch =
                    length / count;

                var sb = new StringBuilder();
                sb.AppendLine("── Hole Spacing ──");
                sb.AppendLine(
                    "Holes: " + count);
                sb.AppendLine(
                    "Length: " + length + " in");
                sb.AppendLine(
                    "Hole dia: " + holeDia + " in");
                sb.AppendLine();
                sb.AppendLine("── Spacing Options ──");
                sb.AppendLine(
                    "Option 1 — Edge to edge with " +
                    Math.Round(edgeDist, 4) +
                    " in edge dist:");
                sb.AppendLine(
                    "  Spacing: " +
                    Math.Round(spacingEdge, 4) + " in (" +
                    FractionFromDecimal(spacingEdge) + ")");
                sb.AppendLine(
                    "  First hole at: " +
                    Math.Round(edgeDist, 4) + " in");
                sb.AppendLine(
                    "  Last hole at: " +
                    Math.Round(length - edgeDist, 4) +
                    " in");
                sb.AppendLine();
                sb.AppendLine(
                    "Option 2 — First/last on ends:");
                sb.AppendLine(
                    "  Spacing: " +
                    Math.Round(spacingCenterline, 4) +
                    " in (" +
                    FractionFromDecimal(
                        spacingCenterline) + ")");
                sb.AppendLine(
                    "  First hole at: 0.000 in");
                sb.AppendLine(
                    "  Last hole at: " + length + " in");
                sb.AppendLine();
                sb.AppendLine(
                    "Option 3 — Equal pitch (no end holes):");
                sb.AppendLine(
                    "  Pitch: " +
                    Math.Round(spacingEqualPitch, 4) +
                    " in (" +
                    FractionFromDecimal(
                        spacingEqualPitch) + ")");
                sb.AppendLine(
                    "  First hole at: " +
                    Math.Round(spacingEqualPitch / 2, 4) +
                    " in");
                sb.AppendLine();

                // All positions for Option 1
                sb.AppendLine(
                    "── Option 1 Positions ──");
                for (int i = 0; i < count; i++)
                {
                    double pos = edgeDist +
                        i * spacingEdge;
                    sb.AppendLine(
                        "  Hole " + (i + 1) + ": " +
                        Math.Round(pos, 4) + " in");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to calculate spacing: " +
                       ex.Message;
            }
        }

        // ─── SMACNA pattern ───────────────────────────────────────────

        private string SmacnaPattern(string prompt)
        {
            try
            {
                double thickness = ParseFirstDimension(
                    prompt, 0.040);

                var sb = new StringBuilder();
                sb.AppendLine("── SMACNA Seam Pattern ──");

                // SMACNA screw spacing by gauge
                double screwSpacing;
                string gauge;

                if (thickness >= 0.0598)
                {
                    screwSpacing = 12.0;
                    gauge = "16 gauge";
                }
                else if (thickness >= 0.0478)
                {
                    screwSpacing = 9.0;
                    gauge = "18 gauge";
                }
                else if (thickness >= 0.0359)
                {
                    screwSpacing = 6.0;
                    gauge = "20 gauge";
                }
                else if (thickness >= 0.0299)
                {
                    screwSpacing = 4.0;
                    gauge = "22 gauge";
                }
                else
                {
                    screwSpacing = 3.0;
                    gauge = "24 gauge or lighter";
                }

                sb.AppendLine(
                    "Thickness: " + thickness + " in " +
                    "(" + gauge + ")");
                sb.AppendLine(
                    "Max screw spacing: " +
                    screwSpacing + " in OC");
                sb.AppendLine();
                sb.AppendLine("── SMACNA Requirements ──");
                sb.AppendLine(
                    "Screw: #10 sheet metal screw");
                sb.AppendLine(
                    "Drill: #18 (0.1695 in)");
                sb.AppendLine(
                    "Edge distance: 3/8\" min");
                sb.AppendLine(
                    "Standard: SMACNA HVAC Duct " +
                    "Construction Standards");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to calculate SMACNA pattern: " +
                       ex.Message;
            }
        }

        // ─── Corner bolt pattern ──────────────────────────────────────

        private string CornerBoltPattern(string prompt)
        {
            try
            {
                double width = 12.0;
                double height = 12.0;

                Match m = Regex.Match(
                    prompt,
                    @"(\d+\.?\d*)\s*[xX×]\s*(\d+\.?\d*)");
                if (m.Success)
                {
                    width = double.Parse(m.Groups[1].Value);
                    height = double.Parse(m.Groups[2].Value);
                }

                double edgeDist = 0.75;
                double boltDia = 0.375;

                var sb = new StringBuilder();
                sb.AppendLine("── Corner Bolt Pattern ──");
                sb.AppendLine(
                    "Panel: " + width + " x " +
                    height + " in");
                sb.AppendLine(
                    "Edge distance: " + edgeDist + " in");
                sb.AppendLine(
                    "Bolt diameter: " + boltDia +
                    " in (3/8\")");
                sb.AppendLine();
                sb.AppendLine("── 4 Corner Positions ──");
                sb.AppendLine(
                    "BL: X=" + edgeDist +
                    "  Y=" + edgeDist);
                sb.AppendLine(
                    "BR: X=" + (width - edgeDist) +
                    "  Y=" + edgeDist);
                sb.AppendLine(
                    "TL: X=" + edgeDist +
                    "  Y=" + (height - edgeDist));
                sb.AppendLine(
                    "TR: X=" + (width - edgeDist) +
                    "  Y=" + (height - edgeDist));
                sb.AppendLine();
                sb.AppendLine("BL=Bottom Left  " +
                    "BR=Bottom Right");
                sb.AppendLine("TL=Top Left     " +
                    "TR=Top Right");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to calculate corner pattern: " +
                       ex.Message;
            }
        }

        // ─── General hole pattern ─────────────────────────────────────

        private string GeneralHolePattern(string prompt)
        {
            // Route to most likely intent
            string n = prompt.ToLowerInvariant();

            if (n.Contains("rivet"))
                return RivetSpacing(prompt);
            if (n.Contains("flange") ||
                n.Contains("duct"))
                return FlangePattern(prompt);
            if (n.Contains("bolt circle") ||
                n.Contains("bcd"))
                return BoltCirclePattern(prompt);

            return EvenSpacing(prompt);
        }

        // ─── Parse helpers ────────────────────────────────────────────

        private int ParseHoleCount(
            string prompt, int defaultVal)
        {
            try
            {
                // Look for pattern like "4 holes" or
                // "6 holes"
                Match m = Regex.Match(
                    prompt,
                    @"(\d+)\s*holes?");
                if (m.Success)
                    return int.Parse(m.Groups[1].Value);

                // Look for first integer
                m = Regex.Match(prompt, @"^(\d+)");
                if (m.Success)
                    return int.Parse(m.Groups[1].Value);

                return defaultVal;
            }
            catch
            {
                return defaultVal;
            }
        }

        private double ParseDimension(
            string prompt,
            string keyword,
            double defaultVal)
        {
            try
            {
                int idx = prompt.ToLower()
                    .IndexOf(keyword);
                if (idx < 0) return defaultVal;

                string after = prompt.Substring(idx);
                Match m = Regex.Match(
                    after, @"([\d.]+)");
                if (m.Success)
                    return double.Parse(m.Groups[1].Value);

                return defaultVal;
            }
            catch
            {
                return defaultVal;
            }
        }

        private double ParseFirstDimension(
            string prompt, double defaultVal)
        {
            try
            {
                // Handle fractions like 3/16
                Match mf = Regex.Match(
                    prompt, @"(\d+)/(\d+)");
                if (mf.Success)
                    return double.Parse(
                        mf.Groups[1].Value) /
                        double.Parse(
                            mf.Groups[2].Value);

                // Handle decimals
                Match m = Regex.Match(
                    prompt, @"([\d.]+)");
                if (m.Success)
                    return double.Parse(m.Groups[1].Value);

                return defaultVal;
            }
            catch
            {
                return defaultVal;
            }
        }

        private double ParseHoleDia(
            string prompt, double defaultVal)
        {
            try
            {
                string n = prompt.ToLowerInvariant();

                // Look for fraction pattern near
                // "diameter" or "dia" or "hole"
                Match mf = Regex.Match(
                    n, @"(\d+)/(\d+)\s*(?:in|inch|dia)");
                if (mf.Success)
                    return double.Parse(
                        mf.Groups[1].Value) /
                        double.Parse(
                            mf.Groups[2].Value);

                // Look for decimal after "diameter"
                // or "dia"
                Match m = Regex.Match(
                    n,
                    @"(?:dia(?:meter)?|hole)\s*([\d.]+)");
                if (m.Success)
                    return double.Parse(m.Groups[1].Value);

                return defaultVal;
            }
            catch
            {
                return defaultVal;
            }
        }

        // ─── Fraction helper ──────────────────────────────────────────

        private string FractionFromDecimal(double value)
        {
            try
            {
                int[] denominators = new int[]
                {
                    2, 4, 8, 16, 32, 64
                };

                foreach (int denom in denominators)
                {
                    double num = value * denom;
                    if (Math.Abs(num - Math.Round(num))
                        < 0.01)
                    {
                        int n = (int)Math.Round(num);
                        int d = denom;

                        // Simplify
                        int gcd = Gcd(
                            Math.Abs(n), d);
                        n /= gcd;
                        d /= gcd;

                        int whole = n / d;
                        int remainder = n % d;

                        if (remainder == 0)
                            return whole + "\"";
                        if (whole > 0)
                            return whole + " " +
                                   remainder + "/" +
                                   d + "\"";
                        return remainder + "/" + d + "\"";
                    }
                }

                return Math.Round(value, 4) + "\"";
            }
            catch
            {
                return value.ToString();
            }
        }

        private int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a;
        }

        private string DrillNumberFromDia(double dia)
        {
            // Common drill sizes for HVAC
            if (dia <= 0.0635) return "52";
            if (dia <= 0.0890) return "43";
            if (dia <= 0.1015) return "38";
            if (dia <= 0.1160) return "32";
            if (dia <= 0.1285) return "30";
            if (dia <= 0.1495) return "25";
            if (dia <= 0.1660) return "19";
            if (dia <= 0.1695) return "18";
            if (dia <= 0.1800) return "15";
            if (dia <= 0.1960) return "9";
            if (dia <= 0.2010) return "7";
            if (dia <= 0.2130) return "3";
            if (dia <= 0.2210) return "2";
            if (dia <= 0.2280) return "1";
            if (dia <= 0.2500) return "1/4\"";
            if (dia <= 0.2810) return "9/32\"";
            if (dia <= 0.3125) return "5/16\"";
            if (dia <= 0.3438) return "11/32\"";
            if (dia <= 0.3750) return "3/8\"";
            if (dia <= 0.4375) return "7/16\"";
            if (dia <= 0.5000) return "1/2\"";
            return Math.Round(dia, 4) + "\"";
        }
    }
}