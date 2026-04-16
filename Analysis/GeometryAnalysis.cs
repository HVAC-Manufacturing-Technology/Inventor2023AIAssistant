using System;
using System.Text;
using System.Text.RegularExpressions;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class GeometryAnalysis
    {
        private Inventor.Application _inventorApplication;
        private const double CmToIn = 0.393701;
        private const double Cm2ToIn2 = 0.155;
        private const double Cm3ToIn3 = 0.0610237;
        private const double GToLbs = 0.00220462;

        public GeometryAnalysis(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        public string TryHandleGeometry(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("surface area"))
                return GetSurfaceArea();
            if (n.Contains("volume"))
                return GetVolume();
            if (n.Contains("bounding box") ||
                n.Contains("overall size") ||
                n.Contains("overall dimension"))
                return GetBoundingBox();
            if (n.Contains("count hole") ||
                n.Contains("how many hole") ||
                n.Contains("number of hole"))
                return CountHoles(prompt);
            if (n.Contains("wall thickness") ||
                n.Contains("minimum thickness") ||
                n.Contains("thinnest"))
                return GetWallThickness();
            if (n.Contains("largest face") ||
                n.Contains("biggest face"))
                return GetLargestFace();
            if (n.Contains("center of mass") ||
                n.Contains("center of gravity") ||
                n.Contains("centroid"))
                return GetCenterOfMass();
            if (n.Contains("mass") ||
                n.Contains("weight"))
                return GetMassAndWeight();
            if (n.Contains("geometry") ||
                n.Contains("geometry summary") ||
                n.Contains("analyze geometry") ||
                n.Contains("geometry report"))
                return GetFullGeometryReport();

            return null;
        }

        // ─── Surface area ─────────────────────────────────────────────

        private string GetSurfaceArea()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";
                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Surface area requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                double totalArea = 0;
                int faceCount = 0;

                foreach (SurfaceBody body in
                    part.ComponentDefinition.SurfaceBodies)
                {
                    try
                    {
                        foreach (Face face in body.Faces)
                        {
                            try
                            {
                                totalArea +=
                                    face.Evaluator.Area;
                                faceCount++;
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                double areaIn2 = totalArea * Cm2ToIn2;
                double areaSqFt = areaIn2 / 144.0;

                return
                    "── Surface Area ──" +
                    System.Environment.NewLine +
                    "Part: " + doc.DisplayName +
                    System.Environment.NewLine +
                    "Total faces: " + faceCount +
                    System.Environment.NewLine +
                    "Surface area: " +
                    Math.Round(areaIn2, 4) + " in²" +
                    System.Environment.NewLine +
                    "Surface area: " +
                    Math.Round(areaSqFt, 4) + " ft²" +
                    System.Environment.NewLine +
                    "Sheet metal needed: ~" +
                    Math.Round(areaSqFt * 1.1, 3) +
                    " ft² (includes 10% waste)";
            }
            catch (Exception ex)
            {
                return "Failed to get surface area: " +
                       ex.Message;
            }
        }

        // ─── Volume ───────────────────────────────────────────────────

        private string GetVolume()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";
                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Volume requires a Part document.";

                PartDocument part = (PartDocument)doc;
                double vol =
                    part.ComponentDefinition
                        .MassProperties.Volume;
                double volIn3 = vol * Cm3ToIn3;
                double volFt3 = volIn3 / 1728.0;

                return
                    "── Volume ──" +
                    System.Environment.NewLine +
                    "Part: " + doc.DisplayName +
                    System.Environment.NewLine +
                    "Volume: " +
                    Math.Round(volIn3, 4) + " in³" +
                    System.Environment.NewLine +
                    "Volume: " +
                    Math.Round(volFt3, 6) + " ft³";
            }
            catch (Exception ex)
            {
                return "Failed to get volume: " + ex.Message;
            }
        }

        // ─── Bounding box ─────────────────────────────────────────────

        private string GetBoundingBox()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";

                double w = 0, h = 0, d = 0;
                string label = "";

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    PartDocument part = (PartDocument)doc;
                    label = "Part: " + doc.DisplayName;
                    Box bb =
                        part.ComponentDefinition.RangeBox;
                    w = Math.Abs(
                        bb.MaxPoint.X - bb.MinPoint.X)
                        * CmToIn;
                    h = Math.Abs(
                        bb.MaxPoint.Y - bb.MinPoint.Y)
                        * CmToIn;
                    d = Math.Abs(
                        bb.MaxPoint.Z - bb.MinPoint.Z)
                        * CmToIn;
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    AssemblyDocument asm =
                        (AssemblyDocument)doc;
                    label = "Assembly: " + doc.DisplayName;
                    Box bb =
                        asm.ComponentDefinition.RangeBox;
                    w = Math.Abs(
                        bb.MaxPoint.X - bb.MinPoint.X)
                        * CmToIn;
                    h = Math.Abs(
                        bb.MaxPoint.Y - bb.MinPoint.Y)
                        * CmToIn;
                    d = Math.Abs(
                        bb.MaxPoint.Z - bb.MinPoint.Z)
                        * CmToIn;
                }
                else
                {
                    return "Bounding box available for " +
                           "Part and Assembly only.";
                }

                return
                    "── Bounding Box ──" +
                    System.Environment.NewLine +
                    label +
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
                    "Overall: " +
                    Math.Round(w, 3) + " x " +
                    Math.Round(h, 3) + " x " +
                    Math.Round(d, 3) + " in";
            }
            catch (Exception ex)
            {
                return "Failed to get bounding box: " +
                       ex.Message;
            }
        }

        // ─── Count holes ──────────────────────────────────────────────

        private string CountHoles(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";
                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Hole count requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;

                double maxSize = -1;
                double minSize = -1;

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

                int totalHoles = 0;
                int filteredHoles = 0;
                var sb = new StringBuilder();
                sb.AppendLine("── Hole Count ──");
                sb.AppendLine("Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                foreach (PartFeature feature in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        if (feature.Suppressed) continue;

                        string type =
                            feature.Type.ToString()
                                .ToLower();
                        bool isHole =
                            type.Contains("hole") ||
                            type.Contains("thread") ||
                            feature.Name.ToLower()
                                .Contains("hole") ||
                            feature.Name.ToLower()
                                .Contains("drill") ||
                            feature.Name.ToLower()
                                .Contains("tap") ||
                            feature.Name.ToLower()
                                .Contains("thru");

                        if (!isHole) continue;
                        totalHoles++;

                        double diameter = -1;
                        try
                        {
                            if (feature is HoleFeature hf)
                                diameter =
                                    hf.HoleDiameter.Value
                                    * CmToIn;
                        }
                        catch { }

                        bool passes = true;
                        if (maxSize > 0 && diameter > 0)
                            passes = diameter < maxSize;
                        if (minSize > 0 && diameter > 0)
                            passes = diameter > minSize;

                        if (passes)
                        {
                            filteredHoles++;
                            string dimStr = diameter > 0
                                ? " ⌀" +
                                  Math.Round(diameter, 4) +
                                  " in"
                                : "";
                            sb.AppendLine(
                                filteredHoles + ". " +
                                feature.Name + dimStr);
                        }
                    }
                    catch { }
                }

                sb.AppendLine(new string('-', 40));
                sb.AppendLine("Total holes: " + totalHoles);
                if (maxSize > 0 || minSize > 0)
                    sb.AppendLine(
                        "Filtered: " + filteredHoles);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to count holes: " + ex.Message;
            }
        }

        // ─── Wall thickness ───────────────────────────────────────────

        private string GetWallThickness()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";
                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Wall thickness requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                double thickness = -1;
                string paramName = "";

                foreach (Parameter param in
                    part.ComponentDefinition.Parameters)
                {
                    try
                    {
                        string name =
                            param.Name.ToLowerInvariant();
                        if (name == "thickness" ||
                            name == "t" ||
                            name == "sheetmetalthickness" ||
                            name == "wall" ||
                            name == "wallthickness")
                        {
                            thickness = param.Value * CmToIn;
                            paramName = param.Name;
                            break;
                        }
                    }
                    catch { }
                }

                var sb = new StringBuilder();
                sb.AppendLine("── Wall Thickness ──");
                sb.AppendLine("Part: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));

                if (thickness > 0)
                {
                    sb.AppendLine(
                        "Thickness: " +
                        Math.Round(thickness, 6) + " in");
                    sb.AppendLine(
                        "Thickness: " +
                        Math.Round(thickness * 25.4, 4) +
                        " mm");
                    sb.AppendLine(
                        "Parameter: '" + paramName + "'");
                    sb.AppendLine(
                        "Gauge: " +
                        GaugeFromThickness(thickness));
                }
                else
                {
                    sb.AppendLine(
                        "Thickness parameter not found.");
                    sb.AppendLine(
                        "Add a parameter named 'Thickness'.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get wall thickness: " +
                       ex.Message;
            }
        }

        // ─── Largest face ─────────────────────────────────────────────

        private string GetLargestFace()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";
                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "Face analysis requires " +
                           "a Part document.";

                PartDocument part = (PartDocument)doc;
                double maxArea = 0;
                double totalArea = 0;
                int faceCount = 0;

                foreach (SurfaceBody body in
                    part.ComponentDefinition.SurfaceBodies)
                {
                    foreach (Face face in body.Faces)
                    {
                        try
                        {
                            double area =
                                face.Evaluator.Area;
                            totalArea += area;
                            faceCount++;
                            if (area > maxArea)
                                maxArea = area;
                        }
                        catch { }
                    }
                }

                double maxIn2 = maxArea * Cm2ToIn2;
                double totalIn2 = totalArea * Cm2ToIn2;

                return
                    "── Face Analysis ──" +
                    System.Environment.NewLine +
                    "Part: " + doc.DisplayName +
                    System.Environment.NewLine +
                    "Total faces: " + faceCount +
                    System.Environment.NewLine +
                    "Largest face: " +
                    Math.Round(maxIn2, 4) + " in²" +
                    System.Environment.NewLine +
                    "Largest face: " +
                    Math.Round(maxIn2 / 144.0, 4) + " ft²" +
                    System.Environment.NewLine +
                    "Total surface: " +
                    Math.Round(totalIn2, 4) + " in²";
            }
            catch (Exception ex)
            {
                return "Failed to analyze faces: " +
                       ex.Message;
            }
        }

        // ─── Center of mass ───────────────────────────────────────────

        private string GetCenterOfMass()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";

                double x = 0, y = 0, z = 0;

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    Point com =
                        ((PartDocument)doc)
                            .ComponentDefinition
                            .MassProperties.CenterOfMass;
                    x = com.X * CmToIn;
                    y = com.Y * CmToIn;
                    z = com.Z * CmToIn;
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    Point com =
                        ((AssemblyDocument)doc)
                            .ComponentDefinition
                            .MassProperties.CenterOfMass;
                    x = com.X * CmToIn;
                    y = com.Y * CmToIn;
                    z = com.Z * CmToIn;
                }
                else
                {
                    return "Center of mass available for " +
                           "Part and Assembly only.";
                }

                return
                    "── Center of Mass ──" +
                    System.Environment.NewLine +
                    "Document: " + doc.DisplayName +
                    System.Environment.NewLine +
                    "X: " + Math.Round(x, 4) + " in" +
                    System.Environment.NewLine +
                    "Y: " + Math.Round(y, 4) + " in" +
                    System.Environment.NewLine +
                    "Z: " + Math.Round(z, 4) + " in";
            }
            catch (Exception ex)
            {
                return "Failed to get center of mass: " +
                       ex.Message;
            }
        }

        // ─── Mass and weight ──────────────────────────────────────────

        private string GetMassAndWeight()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";

                double mass = 0;

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                    mass = ((PartDocument)doc)
                        .ComponentDefinition
                        .MassProperties.Mass;
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    mass = ((AssemblyDocument)doc)
                        .ComponentDefinition
                        .MassProperties.Mass;
                else
                    return "Mass available for " +
                           "Part and Assembly only.";

                double lbs = mass * 1000.0 * GToLbs;
                double oz = lbs * 16.0;

                return
                    "── Mass and Weight ──" +
                    System.Environment.NewLine +
                    "Document: " + doc.DisplayName +
                    System.Environment.NewLine +
                    "Weight: " +
                    Math.Round(lbs, 4) + " lbs" +
                    System.Environment.NewLine +
                    "Weight: " +
                    Math.Round(oz, 3) + " oz" +
                    System.Environment.NewLine +
                    "Mass:   " +
                    Math.Round(mass, 4) + " kg";
            }
            catch (Exception ex)
            {
                return "Failed to get mass: " + ex.Message;
            }
        }

        // ─── Full geometry report ─────────────────────────────────────

        public string GetFullGeometryReport()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";

                var sb = new StringBuilder();
                sb.AppendLine("── Geometry Report ──");
                sb.AppendLine(
                    "Document: " + doc.DisplayName);
                sb.AppendLine(new string('═', 40));

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    PartDocument part = (PartDocument)doc;

                    // Mass
                    try
                    {
                        double mass =
                            part.ComponentDefinition
                                .MassProperties.Mass;
                        double lbs = mass * 1000.0 * GToLbs;
                        sb.AppendLine(
                            "Weight: " +
                            Math.Round(lbs, 4) + " lbs");
                    }
                    catch { sb.AppendLine("Weight: N/A"); }

                    // Volume
                    try
                    {
                        double vol =
                            part.ComponentDefinition
                                .MassProperties.Volume
                            * Cm3ToIn3;
                        sb.AppendLine(
                            "Volume: " +
                            Math.Round(vol, 4) + " in³");
                    }
                    catch { sb.AppendLine("Volume: N/A"); }

                    // Bounding box
                    try
                    {
                        Box bb =
                            part.ComponentDefinition
                                .RangeBox;
                        double w = Math.Abs(
                            bb.MaxPoint.X - bb.MinPoint.X)
                            * CmToIn;
                        double h = Math.Abs(
                            bb.MaxPoint.Y - bb.MinPoint.Y)
                            * CmToIn;
                        double d = Math.Abs(
                            bb.MaxPoint.Z - bb.MinPoint.Z)
                            * CmToIn;
                        sb.AppendLine(
                            "Size:   " +
                            Math.Round(w, 3) + " x " +
                            Math.Round(h, 3) + " x " +
                            Math.Round(d, 3) + " in");
                    }
                    catch { sb.AppendLine("Size: N/A"); }

                    // Surface area
                    try
                    {
                        double totalArea = 0;
                        int faceCount = 0;
                        foreach (SurfaceBody body in
                            part.ComponentDefinition
                                .SurfaceBodies)
                        {
                            foreach (Face face in body.Faces)
                            {
                                try
                                {
                                    totalArea +=
                                        face.Evaluator.Area;
                                    faceCount++;
                                }
                                catch { }
                            }
                        }
                        sb.AppendLine(
                            "Faces:  " + faceCount);
                        sb.AppendLine(
                            "Surface: " +
                            Math.Round(
                                totalArea * Cm2ToIn2, 4) +
                            " in²");
                    }
                    catch { sb.AppendLine("Surface: N/A"); }

                    // Thickness
                    try
                    {
                        foreach (Parameter p in
                            part.ComponentDefinition
                                .Parameters)
                        {
                            string pn =
                                p.Name.ToLowerInvariant();
                            if (pn == "thickness" ||
                                pn == "t")
                            {
                                double t = p.Value * CmToIn;
                                sb.AppendLine(
                                    "Thickness: " +
                                    Math.Round(t, 4) +
                                    " in (" +
                                    GaugeFromThickness(t) +
                                    ")");
                                break;
                            }
                        }
                    }
                    catch { }

                    // Hole count
                    try
                    {
                        int holes = 0;
                        foreach (PartFeature f in
                            part.ComponentDefinition
                                .Features)
                        {
                            if (f.Suppressed) continue;
                            string ft =
                                f.Type.ToString().ToLower();
                            if (ft.Contains("hole") ||
                                f.Name.ToLower()
                                    .Contains("hole"))
                                holes++;
                        }
                        if (holes > 0)
                            sb.AppendLine(
                                "Holes:  " + holes);
                    }
                    catch { }

                    // Material
                    try
                    {
                        string mat =
                            part.ComponentDefinition
                                .Material.Name;
                        sb.AppendLine("Material: " + mat);
                    }
                    catch { sb.AppendLine("Material: N/A"); }
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    AssemblyDocument asm =
                        (AssemblyDocument)doc;

                    try
                    {
                        double mass =
                            asm.ComponentDefinition
                               .MassProperties.Mass;
                        double lbs = mass * 1000.0 * GToLbs;
                        sb.AppendLine(
                            "Assembly weight: " +
                            Math.Round(lbs, 4) + " lbs");
                    }
                    catch { }

                    try
                    {
                        Box bb =
                            asm.ComponentDefinition.RangeBox;
                        double w = Math.Abs(
                            bb.MaxPoint.X - bb.MinPoint.X)
                            * CmToIn;
                        double h = Math.Abs(
                            bb.MaxPoint.Y - bb.MinPoint.Y)
                            * CmToIn;
                        double d = Math.Abs(
                            bb.MaxPoint.Z - bb.MinPoint.Z)
                            * CmToIn;
                        sb.AppendLine(
                            "Size:   " +
                            Math.Round(w, 3) + " x " +
                            Math.Round(h, 3) + " x " +
                            Math.Round(d, 3) + " in");
                    }
                    catch { }

                    sb.AppendLine(
                        "Components: " +
                        asm.ComponentDefinition
                           .Occurrences.Count);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to generate report: " +
                       ex.Message;
            }
        }

        // ─── Gauge helper ─────────────────────────────────────────────

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
    }
}
