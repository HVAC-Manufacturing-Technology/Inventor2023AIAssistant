using Inventor;
using System;
using System.Windows.Forms;
using SysColor = System.Drawing.Color;
using SysFont = System.Drawing.Font;

namespace Inventor2023AIAssistant
{
    public class ParametersPanel : UserControl
    {
        private readonly Inventor.Application _app;
        private DataGridView _grid;
        private Button _btnRefresh;
        private string _lastDocName = "";

        public ParametersPanel(
            Inventor.Application app)
        {
            _app = app;
            BuildUI();
            HookEvents();
        }

        private void HookEvents()
        {
            var timer =
                new System.Windows.Forms.Timer();
            timer.Interval = 1500;
            timer.Tick += (s, e) =>
            {
                try
                {
                    string current = "";
                    try
                    {
                        Document doc = null;

                        // Try ActiveEditDocument first
                        // This changes when editing
                        // a part inside an assembly
                        try
                        {
                            doc = _app.ActiveEditDocument;
                        }
                        catch { }

                        // Fallback to ActiveDocument
                        if (doc == null)
                        {
                            try
                            {
                                doc = _app.ActiveDocument;
                            }
                            catch { }
                        }

                        if (doc != null)
                            current = doc.FullFileName;
                    }
                    catch { }

                    if (current != _lastDocName)
                    {
                        _lastDocName = current;
                        LoadParameters();
                    }
                }
                catch { }
            };
            timer.Start();
        }

        private void BuildUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor =
                SysColor.FromArgb(45, 45, 48);

            _btnRefresh = new Button();
            _btnRefresh.Text = "Refresh";
            _btnRefresh.Dock = DockStyle.Top;
            _btnRefresh.Height = 28;
            _btnRefresh.BackColor =
                SysColor.FromArgb(0, 122, 204);
            _btnRefresh.ForeColor = SysColor.White;
            _btnRefresh.FlatStyle = FlatStyle.Flat;
            _btnRefresh.Font =
                new SysFont("Segoe UI", 9f);
            _btnRefresh.Click +=
                (s, e) => LoadParameters();

            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode =
                DataGridViewSelectionMode
                .FullRowSelect;
            _grid.MultiSelect = false;
            _grid.BorderStyle = BorderStyle.None;
            _grid.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode
                .Fill;
            _grid.BackgroundColor =
                SysColor.FromArgb(30, 30, 30);
            _grid.GridColor =
                SysColor.FromArgb(60, 60, 60);
            _grid.DefaultCellStyle.BackColor =
                SysColor.FromArgb(30, 30, 30);
            _grid.DefaultCellStyle.ForeColor =
                SysColor.White;
            _grid.DefaultCellStyle
                .SelectionBackColor =
                SysColor.FromArgb(0, 122, 204);
            _grid.DefaultCellStyle
                .SelectionForeColor =
                SysColor.White;
            _grid.DefaultCellStyle.Font =
                new SysFont("Segoe UI", 8.5f);
            _grid.ColumnHeadersDefaultCellStyle
                .BackColor =
                SysColor.FromArgb(45, 45, 48);
            _grid.ColumnHeadersDefaultCellStyle
                .ForeColor = SysColor.White;
            _grid.ColumnHeadersDefaultCellStyle
                .Font =
                new SysFont("Segoe UI", 8.5f,
                    System.Drawing.FontStyle.Bold);
            _grid.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode
                .AutoSize;
            _grid.EnableHeadersVisualStyles = false;
            _grid.EditMode =
                DataGridViewEditMode
                .EditOnKeystrokeOrF2;

            // ── Columns ───────────────────────────
            _grid.Columns.Add(
                "ParamName", "Parameter Name");
            _grid.Columns["ParamName"]
                .ReadOnly = true;

            _grid.Columns.Add(
                "Units", "Units");
            _grid.Columns["Units"]
                .ReadOnly = true;

            _grid.Columns.Add(
                "Equation", "Equation");
            _grid.Columns["Equation"]
                .ReadOnly = false;

            _grid.Columns.Add(
                "NominalValue", "Nominal");
            _grid.Columns["NominalValue"]
                .ReadOnly = true;

            _grid.Columns.Add(
                "ModelValue", "Model Value");
            _grid.Columns["ModelValue"]
                .ReadOnly = true;

            var keyCol =
                new DataGridViewCheckBoxColumn();
            keyCol.Name = "Key";
            keyCol.HeaderText = "Key";
            keyCol.ReadOnly = true;
            _grid.Columns.Add(keyCol);

            var exportCol =
                new DataGridViewCheckBoxColumn();
            exportCol.Name = "Export";
            exportCol.HeaderText = "Export";
            exportCol.ReadOnly = true;
            _grid.Columns.Add(exportCol);

            _grid.Columns.Add(
                "Comment", "Comment");
            _grid.Columns["Comment"]
                .ReadOnly = false;

            // Column widths
            _grid.Columns["ParamName"]
                .FillWeight = 20;
            _grid.Columns["Units"]
                .FillWeight = 8;
            _grid.Columns["Equation"]
                .FillWeight = 18;
            _grid.Columns["NominalValue"]
                .FillWeight = 12;
            _grid.Columns["ModelValue"]
                .FillWeight = 12;
            _grid.Columns["Key"]
                .FillWeight = 6;
            _grid.Columns["Export"]
                .FillWeight = 6;
            _grid.Columns["Comment"]
                .FillWeight = 18;

            _grid.CellEndEdit += Grid_CellEndEdit;

            this.Controls.Add(_grid);
            this.Controls.Add(_btnRefresh);
        }

        private void Grid_CellEndEdit(
            object sender,
            DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;

                DataGridViewRow row =
                    _grid.Rows[e.RowIndex];

                if (row.Tag == null ||
                    row.Tag.ToString() == "header")
                    return;

                string paramName =
                    row.Cells["ParamName"].Value
                    ?.ToString();

                if (string.IsNullOrWhiteSpace(
                    paramName))
                    return;

                string colName =
                    _grid.Columns[e.ColumnIndex].Name;

                Document doc = null;
                try { doc = _app.ActiveDocument; }
                catch { return; }
                if (doc == null) return;

                Parameters parms = null;
                if (doc.DocumentType ==
                    DocumentTypeEnum
                    .kPartDocumentObject)
                    parms =
                        ((PartDocument)doc)
                        .ComponentDefinition
                        .Parameters;
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                    .kAssemblyDocumentObject)
                    parms =
                        ((AssemblyDocument)doc)
                        .ComponentDefinition
                        .Parameters;

                if (parms == null) return;

                Parameter param = null;
                foreach (Parameter p in parms)
                {
                    if (p.Name == paramName)
                    {
                        param = p;
                        break;
                    }
                }

                if (param == null) return;

                if (colName == "Equation")
                {
                    string newExpr =
                        row.Cells["Equation"].Value
                        ?.ToString();
                    if (string.IsNullOrWhiteSpace(
                        newExpr))
                        return;

                    try
                    {
                        param.Expression = newExpr;
                        _app.ActiveDocument.Update();
                        LoadParameters();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Failed to set " +
                            "expression:\n" +
                            ex.Message,
                            "Parameter Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        LoadParameters();
                    }
                }

                if (colName == "Comment")
                {
                    string newComment =
                        row.Cells["Comment"].Value
                        ?.ToString() ?? "";
                    try
                    {
                        param.Comment = newComment;
                    }
                    catch { }
                }
            }
            catch { }
        }

        public void LoadParameters()
        {
            _grid.Rows.Clear();

            try
            {
                Document doc = null;
                try
                {
                    doc = _app.ActiveEditDocument;
                }
                catch { }
                if (doc == null)
                {
                    try
                    {
                        doc = _app.ActiveDocument;
                    }
                    catch { return; }
                }
                if (doc == null) return;

                Parameters parms = null;

                if (doc.DocumentType ==
                    DocumentTypeEnum
                    .kPartDocumentObject)
                    parms =
                        ((PartDocument)doc)
                        .ComponentDefinition
                        .Parameters;
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                    .kAssemblyDocumentObject)
                    parms =
                        ((AssemblyDocument)doc)
                        .ComponentDefinition
                        .Parameters;

                if (parms == null) return;

                AddGroupHeader("Model Parameters");
                foreach (Parameter p in parms)
                {
                    try
                    {
                        if (p is UserParameter)
                            continue;
                        AddParameterRow(p, false);
                    }
                    catch { }
                }

                bool hasUser = false;
                foreach (Parameter p in parms)
                {
                    if (p is UserParameter)
                    {
                        if (!hasUser)
                        {
                            AddGroupHeader(
                                "User Parameters");
                            hasUser = true;
                        }
                        try
                        {
                            AddParameterRow(p, true);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void AddGroupHeader(string title)
        {
            int idx = _grid.Rows.Add();
            DataGridViewRow row = _grid.Rows[idx];
            row.Cells["ParamName"].Value = title;
            row.DefaultCellStyle.BackColor =
                SysColor.FromArgb(0, 122, 204);
            row.DefaultCellStyle.ForeColor =
                SysColor.White;
            row.DefaultCellStyle.Font =
                new SysFont("Segoe UI", 9f,
                    System.Drawing.FontStyle.Bold);
            row.Height = 24;
            row.ReadOnly = true;
            row.Tag = "header";
        }

        private void AddParameterRow(
            Parameter p, bool isUser)
        {
            string name = "";
            string units = "";
            string equation = "";
            string nominal = "";
            string modelVal = "";
            bool isKey = false;
            bool isExport = false;
            string comment = "";

            try { name = p.Name; }
            catch { }

            try { units = p.get_Units(); }
            catch { }

            try { equation = p.Expression; }
            catch { }

            try
            {
                double val =
                    (double)p.Value * 0.393701;
                if (units == "in" ||
                    units == "in.")
                    nominal =
                        Math.Round(val, 4)
                        .ToString();
                else
                    nominal = p.Value.ToString();
            }
            catch
            {
                try
                {
                    nominal = p.Value.ToString();
                }
                catch { }
            }

            try
            {
                double val =
                    (double)p.ModelValue * 0.393701;
                if (units == "in" ||
                    units == "in.")
                    modelVal =
                        Math.Round(val, 4)
                        .ToString();
                else
                    modelVal =
                        p.ModelValue.ToString();
            }
            catch
            {
                try
                {
                    modelVal =
                        p.ModelValue.ToString();
                }
                catch { }
            }

            try { isKey = p.IsKey; }
            catch { }

            try { isExport = p.ExposedAsProperty; }
            catch { }

            try { comment = p.Comment; }
            catch { }

            int idx = _grid.Rows.Add();
            DataGridViewRow row = _grid.Rows[idx];

            row.Cells["ParamName"].Value = name;
            row.Cells["Units"].Value = units;
            row.Cells["Equation"].Value = equation;
            row.Cells["NominalValue"].Value =
                nominal;
            row.Cells["ModelValue"].Value = modelVal;
            row.Cells["Key"].Value = isKey;
            row.Cells["Export"].Value = isExport;
            row.Cells["Comment"].Value = comment;
            row.Tag = "param";

            if (isUser)
            {
                row.DefaultCellStyle.ForeColor =
                    SysColor.FromArgb(86, 156, 214);
            }
        }
    }
}