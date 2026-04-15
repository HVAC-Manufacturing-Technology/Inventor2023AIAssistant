using System;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class EngraveHandler
    {
        private Inventor.Application _app;

        public EngraveHandler(
            Inventor.Application app)
        {
            _app = app;
        }

        public string TryHandleEngrave(string prompt)
        {
            string n =
                prompt.Trim().ToLowerInvariant();

            if (!n.Contains("engrave"))
                return null;

            try
            {
                // ── Get active part document ──────────
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return
                        "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return
                        "Engraving requires an active " +
                        "Part document.";

                PartDocument part =
                    (PartDocument)doc;

                // ── Get filename without extension ────
                string fullName = doc.DisplayName;
                string engraveName =
                    System.IO.Path
                        .GetFileNameWithoutExtension(
                            fullName);

                // ── Get selected face ─────────────────
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                Face selectedFace = null;

                if (sel != null && sel.Count > 0)
                {
                    foreach (object item in sel)
                    {
                        if (item is Face f)
                        {
                            selectedFace = f;
                            break;
                        }
                    }
                }

                if (selectedFace == null)
                    return
                        "Please select a face first, " +
                        "then type 'engrave part name'.";

                // ── Get face area ─────────────────────
                double faceArea =
                    selectedFace.Evaluator.Area;

                // Convert from cm² to in²
                double faceAreaIn =
                    faceArea * 0.155;

                // Estimate short side from area
                double shortSide =
                    Math.Sqrt(faceAreaIn) * 0.8;

                // ── Calculate text height ─────────────
                double textHeight =
                    shortSide * 0.15;

                // Clamp between 0.125 and 0.500 inches
                textHeight =
                    Math.Max(0.125,
                        Math.Min(0.500, textHeight));

                // Convert inches to cm for Inventor
                double textHeightCm =
                    textHeight * 2.54;

                // ── Get gauge thickness ───────────────
                double thickness = 0.040;
                try
                {
                    Parameters parameters =
                        part.ComponentDefinition
                            .Parameters;
                    foreach (Parameter p in parameters)
                    {
                        string pName =
                            p.Name.ToLowerInvariant();
                        if (pName == "thickness" ||
                            pName == "gauge" ||
                            pName == "sheetthickness" ||
                            pName == "sheet_thickness")
                        {
                            thickness =
                                (double)p.Value *
                                0.393701;
                            break;
                        }
                    }
                }
                catch { }

                // ── Calculate engrave depth ───────────
                double engraveDepth =
                    thickness * 0.25;

                engraveDepth =
                    Math.Max(0.008,
                        Math.Min(0.030, engraveDepth));

                // Convert to cm for Inventor
                double engraveDepthCm =
                    engraveDepth * 2.54;

                // ── Create sketch on selected face ────
                PlanarSketch sketch =
                    part.ComponentDefinition
                        .Sketches.Add(selectedFace);

                // ── Create center point ───────────────
                TransientGeometry tg =
                    _app.TransientGeometry;

                Point2d centerPt =
                    tg.CreatePoint2d(0, 0);

                // ── Add text to sketch ────────────────
                TextBox textBox =
                    sketch.TextBoxes.AddFitted(
                        centerPt,
                        engraveName);

                // Set font size using FormattedText
                textBox.FormattedText =
                    "<StyleOverride FontSize='" +
                    textHeightCm.ToString("F4") +
                    "'>" +
                    engraveName +
                    "</StyleOverride>";

                // ── Build profile from text ───────────
                ObjectCollection profiles =
                    _app.TransientObjects
                        .CreateObjectCollection();

                foreach (Profile prof in
                    sketch.Profiles)
                    profiles.Add(prof);

                if (profiles.Count == 0)
                    return
                        "Could not create text " +
                        "profiles. Try a flatter face.";

                // ── Create emboss via reflection ──────
                EmbossFeatures embossFeatures =
                    part.ComponentDefinition
                        .Features.EmbossFeatures;

                Type embossType =
                    embossFeatures.GetType();

                object result =
                    embossType.InvokeMember(
                        "Add",
                        System.Reflection
                            .BindingFlags.InvokeMethod,
                        null,
                        embossFeatures,
                        new object[]
                        {
                            profiles,
                            PartFeatureExtentDirectionEnum
                                .kNegativeExtentDirection,
                            engraveDepthCm,
                            EmbossTypeEnum
                                .kEmbossFromFace,
                            false
                        });

                return
                    "\u2705 Engraved '" +
                    engraveName +
                    "' on selected face." +
                    System.Environment.NewLine +
                    "Text height: " +
                    Math.Round(textHeight, 3) +
                    " in" +
                    System.Environment.NewLine +
                    "Engrave depth: " +
                    Math.Round(engraveDepth, 3) +
                    " in";
            }
            catch (Exception ex)
            {
                return
                    "Engrave failed: " +
                    ex.Message +
                    System.Environment.NewLine +
                    "Make sure you have selected " +
                    "a flat face before running " +
                    "the engrave command.";
            }
        }
    }
}