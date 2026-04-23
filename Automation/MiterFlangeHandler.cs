using System;
using System.Windows.Forms;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class MiterFlangeHandler
    {
        private readonly Inventor.Application _app;

        public MiterFlangeHandler(
            Inventor.Application app)
        {
            _app = app;
        }

        public string TryHandle(string prompt)
        {
            string n =
                prompt.Trim().ToLowerInvariant();

            if (!n.Contains("miter flange") &&
                !n.Contains("add miter flange"))
                return null;

            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "No active document.";

                if (doc.DocumentType !=
                    DocumentTypeEnum
                    .kPartDocumentObject)
                    return
                        "Please open a Part " +
                        "document first.";

                PartDocument partDoc =
                    (PartDocument)doc;

                SheetMetalComponentDefinition smDef =
                    partDoc.ComponentDefinition
                    as SheetMetalComponentDefinition;

                if (smDef == null)
                    return "Not a Sheet Metal part.";

                // ── Get selected edges ────────────
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                if (sel == null || sel.Count == 0)
                    return
                        "Please select one or " +
                        "more edges and try again.";

                EdgeCollection edges =
                    _app.TransientObjects
                    .CreateEdgeCollection();

                foreach (object obj in sel)
                {
                    if (obj is Edge edge)
                        edges.Add(edge);
                }

                if (edges.Count == 0)
                    return
                        "No edges found in " +
                        "selection.";

                // ── Show form ─────────────────────
                MiterFlangeForm form =
                    new MiterFlangeForm();

                DialogResult result =
                    form.ShowDialog();

                if (result != DialogResult.OK)
                    return
                        "Miter flange cancelled.";

                // ── Get SheetMetalFeatures ────────
                SheetMetalFeatures smFeatures =
                    smDef.Features
                    as SheetMetalFeatures;

                if (smFeatures == null)
                    return
                        "SheetMetalFeatures " +
                        "not accessible.";

                // ── Convert inches to cm ──────────
                double heightCm =
                    form.FlangeHeight * 2.54;
                double bendRadiusCm =
                    form.BendRadius * 2.54;
                double miterGapCm =
                    form.MiterGap * 2.54;

                // ── Create definition ─────────────
                FlangeFeatures flangeFeatures =
                    smFeatures.FlangeFeatures;

                FlangeDefinition def = null;
                try
                {
                    def = flangeFeatures
                        .CreateFlangeDefinition(
                            edges,
                            form.FlangeAngle,
                            heightCm);
                }
                catch (Exception ex)
                {
                    return
                        "Failed at " +
                        "CreateFlangeDefinition:\n" +
                        ex.Message;
                }

                if (def == null)
                    return "Definition is null.";

                // ── Apply settings ────────────────
                try
                {
                    def.BendPosition =
                        form.BendPosition;
                }
                catch { }

                try
                {
                    def.BendRadius = bendRadiusCm;
                }
                catch { }

                try
                {
                    def.MiterGap = miterGapCm;
                }
                catch { }

                try
                {
                    def.ApplyAutoMitering = true;
                }
                catch { }

                try
                {
                    def.SetDistanceHeightExtent(
                        heightCm,
                        form.ExtentDirection,
                        form.HeightDatum);
                }
                catch { }

                try
                {
                    def.CornerOptions
                        .CornerReliefShape =
                        form.CornerRelief;
                }
                catch { }

                // ── Add feature ───────────────────
                FlangeFeature feature = null;
                try
                {
                    feature =
                        flangeFeatures.Add(def);
                }
                catch (Exception ex)
                {
                    return
                        "Failed at Add:\n" +
                        ex.Message;
                }

                if (feature == null)
                    return
                        "Feature returned null.";

                return
                    "Miter flange created!\n" +
                    "Edges: " + edges.Count + "\n" +
                    "Height: " +
                    Math.Round(
                        form.FlangeHeight, 3) +
                    " in\n" +
                    "Angle: " +
                    form.FlangeAngle + "\u00b0\n" +
                    "Bend Radius: " +
                    form.BendRadius + " in\n" +
                    "Miter Gap: " +
                    form.MiterGap + " in";
            }
            catch (Exception ex)
            {
                return
                    "Miter flange failed:\n" +
                    ex.Message;
            }
        }
    }
}