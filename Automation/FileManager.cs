using System;
using System.Diagnostics;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class FileManager
    {
        private Inventor.Application _app;

        public FileManager(Inventor.Application app)
        {
            _app = app;
        }

        public string TryHandleFileCommand(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n == "save") return Save();
            if (n == "save all") return SaveAll();
            if (n == "list open files" ||
                n == "open files") return ListOpenFiles();
            if (n == "file info" ||
                n == "document info") return FileInfo();
            if (n == "new part") return NewPart();
            if (n == "new assembly") return NewAssembly();
            if (n == "open file location" ||
                n == "show in explorer") return OpenFileLocation();
            if (n == "save copy as") return SaveCopyAs();
            if (n == "close") return CloseActive();

            return null;
        }

        private string Save()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "No active document to save.";
                doc.Save();
                return "\u2705 Saved: " + doc.DisplayName;
            }
            catch (Exception ex)
            {
                return "Save failed: " + ex.Message;
            }
        }

        private string SaveAll()
        {
            try
            {
                int count = 0;
                foreach (Document doc in _app.Documents)
                {
                    try { doc.Save(); count++; }
                    catch { }
                }
                return "\u2705 Saved " + count + " document(s).";
            }
            catch (Exception ex)
            {
                return "Save All failed: " + ex.Message;
            }
        }

        private string ListOpenFiles()
        {
            try
            {
                if (_app.Documents.Count == 0)
                    return "No documents are currently open.";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Open documents (" +
                    _app.Documents.Count + "):");
                sb.AppendLine(new string('-', 40));
                int i = 1;
                foreach (Document doc in _app.Documents)
                {
                    try { sb.AppendLine(i++ + ". " + doc.DisplayName); }
                    catch { }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list files: " + ex.Message;
            }
        }

        private string FileInfo()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("File Info: " + doc.DisplayName);
                sb.AppendLine(new string('-', 40));
                sb.AppendLine("Full Path : " + doc.FullFileName);
                sb.AppendLine("Modified  : " + doc.Dirty);

                try
                {
                    if (System.IO.File.Exists(doc.FullFileName))
                    {
                        var fi = new System.IO.FileInfo(
                            doc.FullFileName);
                        sb.AppendLine("File Size : " +
                            Math.Round(fi.Length / 1024.0, 1) + " KB");
                        sb.AppendLine("Last Write: " +
                            fi.LastWriteTime.ToString(
                                "yyyy-MM-dd HH:mm"));
                    }
                }
                catch { }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to get file info: " + ex.Message;
            }
        }

        private string NewPart()
        {
            try
            {
                _app.Documents.Add(
                    DocumentTypeEnum.kPartDocumentObject,
                    _app.FileManager.GetTemplateFile(
                        DocumentTypeEnum.kPartDocumentObject));
                return "\u2705 New Part document created.";
            }
            catch (Exception ex)
            {
                return "Failed to create new part: " + ex.Message;
            }
        }

        private string NewAssembly()
        {
            try
            {
                _app.Documents.Add(
                    DocumentTypeEnum.kAssemblyDocumentObject,
                    _app.FileManager.GetTemplateFile(
                        DocumentTypeEnum.kAssemblyDocumentObject));
                return "\u2705 New Assembly document created.";
            }
            catch (Exception ex)
            {
                return "Failed to create new assembly: " + ex.Message;
            }
        }

        private string OpenFileLocation()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";

                string path = doc.FullFileName;
                if (string.IsNullOrWhiteSpace(path) ||
                    !System.IO.File.Exists(path))
                    return "File has not been saved to disk yet.";

                System.Diagnostics.Process.Start(
                    "explorer.exe",
                    "/select,\"" + path + "\"");
                return "\u2705 Opened file location in Explorer.";
            }
            catch (Exception ex)
            {
                return "Failed to open location: " + ex.Message;
            }
        }

        private string SaveCopyAs()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";

                string original = doc.FullFileName;
                string dir = System.IO.Path.GetDirectoryName(original);
                string name = System.IO.Path.GetFileNameWithoutExtension(
                    original);
                string ext = System.IO.Path.GetExtension(original);
                string copy = System.IO.Path.Combine(
                    dir, name + "_copy" + ext);

                doc.SaveAs(copy, true);
                return "\u2705 Saved copy as: " +
                    System.IO.Path.GetFileName(copy);
            }
            catch (Exception ex)
            {
                return "Save Copy As failed: " + ex.Message;
            }
        }

        private string CloseActive()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                    return "No active document to close.";
                string name = doc.DisplayName;
                doc.Close(true);
                return "\u2705 Closed: " + name;
            }
            catch (Exception ex)
            {
                return "Close failed: " + ex.Message;
            }
        }
    }
}