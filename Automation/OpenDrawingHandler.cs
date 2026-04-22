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
        private string _lastSearchLog = "";

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

                    object refs =
                        valueSet.ReferencedFiles;

                    if (refs == null)
                        return
                            "❌ No referenced " +
                            "files found.";

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

                    // Get first item via enumerator
                    object fd = null;
                    try
                    {
                        var enumerator =
                            refs.GetType()
                            .InvokeMember(
                                "GetEnumerator",
                                BindingFlags
                                .InvokeMethod,
                                null, refs, null)
                            as System.Collections
                            .IEnumerator;

                        if (enumerator != null &&
                            enumerator.MoveNext())
                            fd = enumerator.Current;
                    }
                    catch { }

                    // Fallback index 0
                    if (fd == null)
                    {
                        try
                        {
                            fd = refs.GetType()
                                .InvokeMember("Item",
                                BindingFlags
                                .InvokeMethod |
                                BindingFlags
                                .GetProperty,
                                null, refs,
                                new object[] { 0 });
                        }
                        catch { }
                    }

                    // Fallback index 1
                    if (fd == null)
                    {
                        try
                        {
                            fd = refs.GetType()
                                .InvokeMember("Item",
                                BindingFlags
                                .InvokeMethod |
                                BindingFlags
                                .GetProperty,
                                null, refs,
                                new object[] { 1 });
                        }
                        catch { }
                    }

                    if (fd == null)
                        return
                            "❌ Could not get " +
                            "file descriptor.\n" +
                            "Count was: " + count;

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
                        "⚠️ No drawing found for:\n" +
                        partName + "\n\n" +
                        "Search log:\n" +
                        _lastSearchLog;
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
            _lastSearchLog = "";
            try
            {
                string folder =
                    SysPath.GetDirectoryName(
                        partPath);
                string baseName =
                    SysPath
                    .GetFileNameWithoutExtension(
                        partPath);

                _lastSearchLog +=
                    "Base: " + baseName + "\n";
                _lastSearchLog +=
                    "Start: " + folder + "\n";

                // Same folder
                string idwPath =
                    SysPath.Combine(
                        folder, baseName + ".idw");
                _lastSearchLog +=
                    "Check: " + idwPath + "\n";
                if (SysFile.Exists(idwPath))
                    return idwPath;

                string dwgPath =
                    SysPath.Combine(
                        folder, baseName + ".dwg");
                if (SysFile.Exists(dwgPath))
                    return dwgPath;

                // Search up 4 levels
                string searchFolder = folder;
                for (int i = 0; i < 4; i++)
                {
                    searchFolder =
                        SysPath.GetDirectoryName(
                            searchFolder);
                    if (searchFolder == null) break;

                    _lastSearchLog +=
                        "Level " + (i + 1) +
                        ": " + searchFolder + "\n";

                    idwPath = SysPath.Combine(
                        searchFolder,
                        baseName + ".idw");
                    if (SysFile.Exists(idwPath))
                        return idwPath;

                    try
                    {
                        foreach (string subDir in
                            Directory
                            .GetDirectories(
                                searchFolder,
                                "*",
                                SearchOption
                                .AllDirectories))
                        {
                            idwPath = SysPath
                                .Combine(subDir,
                                baseName + ".idw");
                            if (SysFile
                                .Exists(idwPath))
                                return idwPath;

                            dwgPath = SysPath
                                .Combine(subDir,
                                baseName + ".dwg");
                            if (SysFile
                                .Exists(dwgPath))
                                return dwgPath;
                        }
                    }
                    catch { }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}