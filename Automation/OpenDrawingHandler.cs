using System;
using System.IO;
using SysFile = System.IO.File;
using SysPath = System.IO.Path;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class OpenDrawingHandler
    {
        private readonly Inventor.Application _app;

        public OpenDrawingHandler(
            Inventor.Application app)
        {
            _app = app;
        }

        public string TryHandleOpenDrawing(
            string prompt)
        {
            string n =
                prompt.Trim().ToLowerInvariant();

            if (!n.Contains("open drawing") &&
                !n.Contains("open part drawing") &&
                !n.Contains("drawing from balloon") &&
                !n.Contains("open idw"))
                return null;

            try
            {
                // ── Verify active drawing doc ─────
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "❌ No active document.";

                if (doc.DocumentType !=
                    DocumentTypeEnum
                    .kDrawingDocumentObject)
                    return
                        "❌ Please open a Drawing " +
                        "(IDW) document first.";

                // ── Get selected balloon ──────────
                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                if (sel == null || sel.Count == 0)
                    return
                        "⚠️ Please select a " +
                        "balloon first then " +
                        "type 'open drawing'.";

                // ── Find balloon in selection ─────
                Balloon balloon = null;
                foreach (object item in sel)
                {
                    if (item is Balloon b)
                    {
                        balloon = b;
                        break;
                    }
                }

                if (balloon == null)
                    return
                        "⚠️ No balloon selected.\n" +
                        "Please select a balloon " +
                        "and try again.";

                // ── Get referenced file ───────────
                string partPath = null;

                try
                {
                    BalloonValueSets valueSets =
                        balloon.BalloonValueSets;

                    if (valueSets == null ||
                        valueSets.Count == 0)
                        return
                            "❌ Balloon has no " +
                            "referenced files.";

                    BalloonValueSet valueSet =
                        valueSets[1];

                    ReferencedFileDescriptors refs =
                        valueSet.ReferencedFiles;

                    if (refs == null ||
                        refs.Count == 0)
                        return
                            "❌ No referenced " +
                            "files in balloon.";

                    foreach (FileDescriptor fd
                        in refs)
                    {
                        partPath = fd.FullFileName;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    return
                        "❌ Could not read " +
                        "balloon reference:\n" +
                        ex.Message;
                }

                if (string.IsNullOrEmpty(partPath))
                    return
                        "❌ Could not find " +
                        "referenced file path.";

                // ── Find matching IDW ─────────────
                string idwPath =
                    FindMatchingDrawing(partPath);

                if (idwPath == null)
                {
                    string partName =
                        SysPath.GetFileName(
                            partPath);
                    return
                        "⚠️ No drawing found for:\n" +
                        partName + "\n\n" +
                        "Looked in same folder " +
                        "as part file.";
                }

                // ── Open the IDW ──────────────────
                _app.Documents.Open(idwPath, true);

                string idwName =
                    SysPath.GetFileName(idwPath);
                return "✅ Opened drawing:\n" +
                    idwName;
            }
            catch (Exception ex)
            {
                return
                    "❌ Open drawing failed:\n" +
                    ex.Message;
            }
        }

        private string FindMatchingDrawing(
            string partPath)
        {
            try
            {
                string folder =
                    SysPath.GetDirectoryName(
                        partPath);
                string baseName =
                    SysPath
                    .GetFileNameWithoutExtension(
                        partPath);

                // Same folder IDW
                string idwPath =
                    SysPath.Combine(
                        folder, baseName + ".idw");
                if (SysFile.Exists(idwPath))
                    return idwPath;

                // Same folder DWG
                string dwgPath =
                    SysPath.Combine(
                        folder, baseName + ".dwg");
                if (SysFile.Exists(dwgPath))
                    return dwgPath;

                // One level up
                string parentFolder =
                    SysPath.GetDirectoryName(folder);
                if (parentFolder != null)
                {
                    idwPath = SysPath.Combine(
                        parentFolder,
                        baseName + ".idw");
                    if (SysFile.Exists(idwPath))
                        return idwPath;
                }

                // Search subfolders
                try
                {
                    foreach (string subDir in
                        Directory.GetDirectories(
                            folder))
                    {
                        idwPath = SysPath.Combine(
                            subDir,
                            baseName + ".idw");
                        if (SysFile.Exists(idwPath))
                            return idwPath;
                    }
                }
                catch { }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}