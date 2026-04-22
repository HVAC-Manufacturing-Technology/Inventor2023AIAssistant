using System;
using System.IO;
using System.Reflection;
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
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "❌ No active document.";

                if (doc.DocumentType !=
                    DocumentTypeEnum
                    .kDrawingDocumentObject)
                    return
                        "❌ Please open a Drawing " +
                        "(IDW) document first.";

                SelectSet sel =
                    _app.ActiveDocument.SelectSet;

                if (sel == null || sel.Count == 0)
                    return
                        "⚠️ Please select a " +
                        "balloon first then " +
                        "type 'open drawing'.";

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

                // ── Get file path via reflection ──
                string partPath = null;

                try
                {
                    BalloonValueSets valueSets =
                        balloon.BalloonValueSets;

                    if (valueSets == null ||
                        valueSets.Count == 0)
                        return
                            "❌ Balloon has no " +
                            "value sets.";

                    BalloonValueSet valueSet =
                        valueSets[1];

                    // Try ReferencedFiles via
                    // reflection to get FullFileName
                    object refs =
                        valueSet.ReferencedFiles;

                    if (refs == null)
                        return
                            "❌ No referenced " +
                            "files found.";

                    // Get count
                    int count = 0;
                    try
                    {
                        count = (int)refs.GetType()
                            .InvokeMember("Count",
                            BindingFlags.GetProperty,
                            null, refs, null);
                    }
                    catch { }

                    if (count == 0)
                        return
                            "❌ Referenced files " +
                            "collection is empty.";

                    // Get item 1
                    object fd = null;
                    try
                    {
                        fd = refs.GetType()
                            .InvokeMember("Item",
                            BindingFlags.InvokeMethod,
                            null, refs,
                            new object[] { 1 });
                    }
                    catch { }

                    if (fd == null)
                        return
                            "❌ Could not get " +
                            "first file descriptor.";

                    // Get FullFileName
                    try
                    {
                        partPath = (string)fd
                            .GetType()
                            .InvokeMember(
                            "FullFileName",
                            BindingFlags.GetProperty,
                            null, fd, null);
                    }
                    catch { }
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

                string idwPath =
                    FindMatchingDrawing(partPath);

                if (idwPath == null)
                {
                    string partName =
                        SysPath.GetFileName(partPath);
                    return
                        "⚠️ No drawing found for:\n"
                        + partName + "\n" +
                        "Looked in same folder " +
                        "as part file.";
                }

                _app.Documents.Open(idwPath, true);

                string idwName =
                    SysPath.GetFileName(idwPath);
                return
                    "✅ Opened drawing:\n" + idwName;
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