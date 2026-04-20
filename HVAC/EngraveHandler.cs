using System;
using System.Reflection;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class EngraveHandler
    {
        private readonly Inventor.Application _app;
        private bool _waitingForFace = false;
        private bool _waitingForMarkType = false;
        private Face _selectedFace = null;
        private string _markType = "surface";

        public EngraveHandler(
            Inventor.Application app)
        {
            _app = app;
        }

        public string TryHandleEngrave(string prompt)
        {
            string n =
                prompt.Trim().ToLowerInvariant();

            // ── Step 3 — Mark type selected ───────
            if (_waitingForMarkType)
            {
                if (n == "surface" || n == "s")
                {
                    _markType = "surface";
                    _waitingForMarkType = false;
                    return ExecuteEngrave();
                }
                if (n == "through" || n == "t")
                {
                    _markType = "through";
                    _waitingForMarkType = false;
                    return ExecuteEngrave();
                }
                return
                    "⚠️ Please type:\n" +
                    "• 'surface' — Mark Surface\n" +
                    "• 'through' — Mark Through";
            }

            // ── Step 2 — Execute after face ───────
            if (_waitingForFace &&
                (n == "go" || n == "ok" ||
                 n == "done" || n == "run"))
            {
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;
                _selectedFace = null;

                if (sel != null && sel.Count > 0)
                {
                    foreach (object item in sel)
                    {
                        if (item is Face f)
                        {
                            _selectedFace = f;
                            break;
                        }
                    }
                }

                if (_selectedFace == null)
                    return
                        "⚠️ No face selected.\n" +
                        "Please select a flat face " +
                        "and type 'go' again.";

                _waitingForFace = false;
                _waitingForMarkType = true;

                return
                    "How would you like to mark?\n\n" +
                    "Type:\n" +
                    "• 'surface' — Mark Surface\n" +
                    "• 'through' — Mark Through";
            }

            // ── Step 1 — Start engrave ────────────
            if (!n.Contains("engrave") &&
                !n.Contains("emboss") &&
                !n.Contains("mark"))
                return null;

            Document doc = _app.ActiveDocument;
            if (doc == null)
                return "No active document.";

            if (doc.DocumentType !=
                DocumentTypeEnum
                .kPartDocumentObject)
                return
                    "Please open a Part " +
                    "document first.";

            PartDocument part =
                (PartDocument)doc;

            if (!(part.ComponentDefinition
                is SheetMetalComponentDefinition))
                return
                    "⚠️ This feature requires " +
                    "a Sheet Metal part.";

            _waitingForFace = true;

            return
                "📋 Ready to engrave!\n\n" +
                "1️⃣ Select a flat face " +
                "on the part\n" +
                "2️⃣ Type 'go' when ready\n\n" +
                "The part name will be marked " +
                "on the selected face.";
        }

        private string ExecuteEngrave()
        {
            string debugInfo = "";
            try
            {
                PartDocument part =
                    (PartDocument)_app.ActiveDocument;

                SheetMetalComponentDefinition smDef =
                    (SheetMetalComponentDefinition)
                    part.ComponentDefinition;

                SheetMetalFeatures smFeatures =
                    (SheetMetalFeatures)smDef.Features;

                // ── Get part name ─────────────────
                string partName =
                    System.IO.Path
                    .GetFileNameWithoutExtension(
                        _app.ActiveDocument
                        .DisplayName);

                // ── Calculate text height ─────────
                double faceArea =
                    _selectedFace.Evaluator.Area;
                double faceAreaIn =
                    faceArea * 0.155;
                double shortSide =
                    Math.Sqrt(faceAreaIn) * 0.8;
                double textHeight =
                    shortSide * 0.15;
                textHeight = Math.Max(0.125,
                    Math.Min(0.500, textHeight));
                double textHeightCm =
                    textHeight * 2.54;

                // ── Create sketch on face ─────────
                PlanarSketch sketch =
                    part.ComponentDefinition
                    .Sketches.Add(_selectedFace);

                TransientGeometry tg =
                    _app.TransientGeometry;

                double boxWidth =
                    textHeightCm *
                    partName.Length * 0.6;
                double boxHeight =
                    textHeightCm * 1.2;

                Point2d startPt =
                    tg.CreatePoint2d(
                        -(boxWidth / 2),
                        -(boxHeight / 2));

                Point2d cornerPt =
                    tg.CreatePoint2d(
                        boxWidth / 2,
                        boxHeight / 2);

                // ── Add text to sketch ────────────
                Inventor.TextBox textBox =
                    sketch.TextBoxes
                    .AddByRectangle(
                        startPt,
                        cornerPt,
                        "<StyleOverride FontSize='" +
                        textHeightCm.ToString("F4") +
                        "'>" + partName +
                        "</StyleOverride>",
                        Missing.Value);

                debugInfo += "Sketch ✅ Text ✅\n";

                // ── Build collection with TextBox ─
                ObjectCollection profiles =
                    _app.TransientObjects
                    .CreateObjectCollection();

                // Add the TextBox directly
                profiles.Add(textBox);

                debugInfo +=
                    $"TextBox added to collection ✅\n";

                // ── Find correct MarkStyle ────────
                MarkStyle selectedStyle = null;
                string targetStyle =
                    _markType == "through"
                    ? "through" : "surface";

                foreach (MarkStyle ms in
                    part.MarkStyles)
                {
                    debugInfo +=
                        $"Style: {ms.Name}\n";
                    if (ms.Name.ToLowerInvariant()
                        .Contains(targetStyle) &&
                        selectedStyle == null)
                        selectedStyle = ms;
                }

                if (selectedStyle == null)
                    return debugInfo +
                        "❌ No matching Mark " +
                        "Style found.\n" +
                        "Go to Manage → " +
                        "Styles Editor → Mark.";

                debugInfo +=
                    $"Using: " +
                    $"{selectedStyle.Name} ✅\n";

                // ── Create empty collection ───────
                ObjectCollection emptyCol =
                    _app.TransientObjects
                    .CreateObjectCollection();

                // ── Create mark definition ────────
                MarkDefinition markDef =
                    smFeatures.MarkFeatures
                    .CreateMarkDefinition(
                        emptyCol, selectedStyle);

                debugInfo +=
                    "Mark definition ✅\n";

                // ── Add geometry set ──────────────
                markDef.AddGeometrySet(
                    profiles, selectedStyle);

                debugInfo +=
                    "Geometry set added ✅\n";
                // ── Finish sketch first ───────────
                try
                {
                    sketch.ExitEdit();
                    debugInfo += "Sketch finished ✅\n";
                }
                catch (Exception exitEx)
                {
                    debugInfo +=
                        $"⚠️ ExitEdit: {exitEx.Message}\n";
                }

                // ── Debug MarkDefinition ──────────
                try
                {
                    debugInfo +=
                        $"GeomSetCount: " +
                        $"{markDef.MarkGeometrySetCount}\n";
                }
                catch (Exception gcEx)
                {
                    debugInfo +=
                        $"⚠️ GeomSet: {gcEx.Message}\n";
                }

                // ── Try Add via reflection ────────
                Type mfType =
                    smFeatures.MarkFeatures.GetType();

                string methods = "Methods: ";
                foreach (var m in mfType.GetMethods())
                    methods += m.Name + " ";

                debugInfo += methods + "\n";

                try
                {
                    mfType.InvokeMember(
                        "Add",
                        BindingFlags.InvokeMethod,
                        null,
                        smFeatures.MarkFeatures,
                        new object[] { markDef });
                }
                catch (Exception refEx)
                {
                    return debugInfo +
                        $"❌ Reflection Add failed:\n" +
                        $"{refEx.Message}\n" +
                        $"Inner: " +
                        $"{refEx.InnerException?.Message}";
                }

                return
                    $"✅ Marked '{partName}'" +
                    $" on selected face.\n" +
                    $"Type: {_markType}\n" +
                    $"Text height: " +
                    $"{Math.Round(textHeight, 3)}" +
                    $" in";
            }
            catch (Exception ex)
            {
                return debugInfo +
                    $"❌ Mark failed:\n" +
                    $"{ex.Message}";
            }
        }
    }
}