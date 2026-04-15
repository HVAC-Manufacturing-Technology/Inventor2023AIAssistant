using System;
using System.IO;
using System.Windows.Forms;
using Inventor;

namespace Inventor2023AIAssistant
{
    /// <summary>
    /// Exports and reports parameters from chat.
    /// Commands:
    ///   export parameters to csv
    ///   export parameters to excel
    ///   parameter report
    ///   list user parameters
    ///   list model parameters
    ///   parameter summary
    /// </summary>
    public class ParameterExporter
    {
        private readonly Inventor.Application _app;
        private const double CmToIn = 0.393701;

        public ParameterExporter(
            Inventor.Application app)
        {
            _app = app;
        }

        // ─── Entry point ──────────────────────────────

        public string TryHandleParameterExport(
            string prompt)
        {
            if (prompt == null) return null;
            string n = prompt.Trim().ToLowerInvariant();

            if (n == "export parameters to csv" ||
                n == "export parameters csv" ||
                n == "parameters to csv" ||
                n == "export parameters")
                return ExportToCsv();

            if (n == "export parameters to excel" ||
                n == "export parameters excel" ||
                n == "parameters to excel")
                return ExportToExcel();

            if (n == "parameter report" ||
                n == "parameters report" ||
                n == "full parameter report")
                return GenerateParameterReport();

            if (n == "list user parameters" ||
                n == "show user parameters" ||
                n == "user parameters")
                return ListUserParameters();

            if (n == "list model parameters" ||
                n == "show model parameters" ||
                n == "model parameters")
                return ListModelParameters();

            if (n == "parameter summary" ||
                n == "parameters summary")
                return ParameterSummary();

            return null;
        }

        // ─── Export to CSV ────────────────────────────

        private string ExportToCsv()
        {
            try
            {
                Parameters parameters = GetParameters();
                if (parameters == null)
                    return "No parameters available. " +
                           "Open a Part or Assembly first.";

                Document doc = _app.ActiveDocument;

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Export Parameters to CSV";
                    dlg.Filter = "CSV Files (*.csv)|*.csv";
                    dlg.FileName =
                        System.IO.Path
                            .GetFileNameWithoutExtension(
                                doc.DisplayName) +
                        "_parameters.csv";

                    if (!string.IsNullOrWhiteSpace(
                            doc.FullFileName))
                        dlg.InitialDirectory =
                            System.IO.Path.GetDirectoryName(
                                doc.FullFileName);

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return "Export cancelled.";

                    using (var sw = new StreamWriter(
                        dlg.FileName, false,
                        System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine(
                            "Name,Expression," +
                            "Value (in),Units,Type");

                        WriteParamRows(
                            sw,
                            parameters.UserParameters,
                            "User");
                        WriteParamRows(
                            sw,
                            parameters.ModelParameters,
                            "Model");
                        WriteParamRows(
                            sw,
                            parameters.ReferenceParameters,
                            "Reference");
                    }

                    return "\u2705 Parameters exported to:" +
                           System.Environment.NewLine +
                           dlg.FileName;
                }
            }
            catch (Exception ex)
            {
                return "Failed to export CSV: " + ex.Message;
            }
        }

        // ─── Export to Excel ──────────────────────────

        private string ExportToExcel()
        {
            try
            {
                Parameters parameters = GetParameters();
                if (parameters == null)
                    return "No parameters available.";

                Document doc = _app.ActiveDocument;

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title =
                        "Export Parameters to Excel";
                    dlg.Filter =
                        "Excel Files (*.xlsx)|*.xlsx" +
                        "|CSV Files (*.csv)|*.csv";
                    dlg.FileName =
                        System.IO.Path
                            .GetFileNameWithoutExtension(
                                doc.DisplayName) +
                        "_parameters.xlsx";

                    if (!string.IsNullOrWhiteSpace(
                            doc.FullFileName))
                        dlg.InitialDirectory =
                            System.IO.Path.GetDirectoryName(
                                doc.FullFileName);

                    if (dlg.ShowDialog() != DialogResult.OK)
                        return "Export cancelled.";

                    // Write as CSV — Excel opens it natively
                    using (var sw = new StreamWriter(
                        dlg.FileName, false,
                        System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine(
                            "Name,Expression," +
                            "Value (in),Units,Type");

                        WriteParamRows(
                            sw,
                            parameters.UserParameters,
                            "User");
                        WriteParamRows(
                            sw,
                            parameters.ModelParameters,
                            "Model");
                        WriteParamRows(
                            sw,
                            parameters.ReferenceParameters,
                            "Reference");
                    }

                    return "\u2705 Parameters exported to:" +
                           System.Environment.NewLine +
                           dlg.FileName +
                           System.Environment.NewLine +
                           "(Open with Excel)";
                }
            }
            catch (Exception ex)
            {
                return "Failed to export: " + ex.Message;
            }
        }

        private void WriteParamRows(
            StreamWriter sw,
            dynamic collection,
            string type)
        {
            try
            {
                foreach (Parameter p in collection)
                {
                    try
                    {
                        double valIn = 0;
                        try
                        {
                            valIn =
                                (double)p.Value * CmToIn;
                        }
                        catch { }

                        string name =
                            CsvEscape(p.Name);
                        string expr =
                            CsvEscape(p.Expression);
                        string val =
                            Math.Round(valIn, 6)
                                .ToString();
                        string units = "";
                        try { units = p.get_Units(); }
                        catch { }

                        sw.WriteLine(string.Join(",",
                            name, expr, val,
                            CsvEscape(units), type));
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string CsvEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"") ||
                s.Contains("\n"))
                return "\"" +
                       s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        // ─── Generate report ──────────────────────────

        private string GenerateParameterReport()
        {
            try
            {
                Parameters parameters = GetParameters();
                if (parameters == null)
                    return "No parameters available.";

                Document doc = _app.ActiveDocument;
                var sb = new System.Text.StringBuilder();

                sb.AppendLine(
                    "\u2500\u2500 Parameter Report: " +
                    doc.DisplayName + " \u2500\u2500");
                sb.AppendLine();

                AppendParamSection(
                    sb,
                    "User Parameters",
                    parameters.UserParameters);
                AppendParamSection(
                    sb,
                    "Model Parameters",
                    parameters.ModelParameters);
                AppendParamSection(
                    sb,
                    "Reference Parameters",
                    parameters.ReferenceParameters);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to generate report: " +
                       ex.Message;
            }
        }

        private void AppendParamSection(
            System.Text.StringBuilder sb,
            string title,
            dynamic collection)
        {
            try
            {
                int count = 0;
                try { count = collection.Count; }
                catch { return; }

                if (count == 0) return;

                sb.AppendLine("--- " + title +
                    " (" + count + ") ---");

                foreach (Parameter p in collection)
                {
                    try
                    {
                        double valIn = 0;
                        try
                        {
                            valIn =
                                (double)p.Value * CmToIn;
                        }
                        catch { }

                        sb.AppendLine(
                            p.Name.PadRight(22) +
                            p.Expression.PadRight(18) +
                            Math.Round(valIn, 4)
                                .ToString().PadRight(12) +
                            " in");
                    }
                    catch { }
                }
                sb.AppendLine();
            }
            catch { }
        }

        // ─── List user parameters ─────────────────────

        private string ListUserParameters()
        {
            try
            {
                Parameters parameters = GetParameters();
                if (parameters == null)
                    return "No parameters available.";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("User Parameters:");
                sb.AppendLine(new string('-', 40));

                int count = 0;
                foreach (Parameter p in
                    parameters.UserParameters)
                {
                    try
                    {
                        sb.AppendLine(
                            p.Name.PadRight(22) +
                            p.Expression);
                        count++;
                    }
                    catch { }
                }

                if (count == 0)
                    sb.AppendLine("No user parameters.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed: " + ex.Message;
            }
        }

        // ─── List model parameters ────────────────────

        private string ListModelParameters()
        {
            try
            {
                Parameters parameters = GetParameters();
                if (parameters == null)
                    return "No parameters available.";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Model Parameters:");
                sb.AppendLine(new string('-', 40));

                int count = 0;
                foreach (Parameter p in
                    parameters.ModelParameters)
                {
                    try
                    {
                        sb.AppendLine(
                            p.Name.PadRight(22) +
                            p.Expression);
                        count++;
                    }
                    catch { }
                }

                if (count == 0)
                    sb.AppendLine("No model parameters.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed: " + ex.Message;
            }
        }

        // ─── Parameter summary ────────────────────────

        private string ParameterSummary()
        {
            try
            {
                Parameters parameters = GetParameters();
                if (parameters == null)
                    return "No parameters available.";

                int user = 0;
                int model = 0;
                int reference = 0;

                try
                {
                    user = parameters
                        .UserParameters.Count;
                }
                catch { }
                try
                {
                    model = parameters
                        .ModelParameters.Count;
                }
                catch { }
                try
                {
                    reference = parameters
                        .ReferenceParameters.Count;
                }
                catch { }

                return
                    "\u2500\u2500 Parameter Summary \u2500\u2500" +
                    System.Environment.NewLine +
                    "User Parameters:      " + user +
                    System.Environment.NewLine +
                    "Model Parameters:     " + model +
                    System.Environment.NewLine +
                    "Reference Parameters: " + reference +
                    System.Environment.NewLine +
                    "Total:                " +
                    (user + model + reference);
            }
            catch (Exception ex)
            {
                return "Failed: " + ex.Message;
            }
        }

        // ─── Helper ───────────────────────────────────

        private Parameters GetParameters()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null) return null;

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                    return ((PartDocument)doc)
                        .ComponentDefinition.Parameters;

                if (doc.DocumentType ==
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                    return ((AssemblyDocument)doc)
                        .ComponentDefinition.Parameters;

                return null;
            }
            catch { return null; }
        }
    }
}
