using System;
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
                    return
                        "Not a Sheet Metal part.";

                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                if (sel == null || sel.Count == 0)
                    return
                        "Please select one or " +
                        "more edges and try again.";

                ObjectCollection edgeCollection =
                    _app.TransientObjects
                    .CreateObjectCollection();

                foreach (object obj in sel)
                {
                    if (obj is Edge edge)
                        edgeCollection.Add(edge);
                }

                if (edgeCollection.Count == 0)
                    return
                        "No edges found in " +
                        "selection.";

                SheetMetalFeatures smFeatures =
                    smDef.Features
                    as SheetMetalFeatures;

                if (smFeatures == null)
                    return
                        "SheetMetalFeatures " +
                        "not accessible.";

                Edge firstEdge =
                    (Edge)edgeCollection[1];

                Inventor.Path path =
                    smFeatures.CreatePath(
                        firstEdge);

                ContourFlangeFeatures contourFlanges =
                    smFeatures.ContourFlangeFeatures;

                ContourFlangeDefinition def =
                    contourFlanges
                    .CreateContourFlangeDefinition(
                        path,
                        edgeCollection);

                def.ApplyAutoMitering = true;
                def.MiterGap = "0.02 in";

                foreach (object obj in edgeCollection)
                {
                    if (obj is Edge e)
                    {
                        try
                        {
                            def.SetEdgeWidthExtent(e);
                        }
                        catch { }
                    }
                }

                contourFlanges.Add(def);

                return
                    "Miter flange created!\n" +
                    "Edges: " +
                    edgeCollection.Count + "\n" +
                    "Auto mitering: ON\n" +
                    "Miter gap: 0.02 in";
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