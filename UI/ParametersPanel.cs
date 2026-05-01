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

        public ParametersPanel(
            Inventor.Application app)
        {
            _app = app;
            BuildUI();
        }

        private void BuildUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor =
                SysColor.FromArgb(45, 45, 48);

            // ── Refresh Button ────────────────────
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

            // ── DataGridView ──────────────────────
            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
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
            _grid.DefaultCellStyle.SelectionBackColor =
                SysColor.FromArgb(0, 122, 204);
            _grid.DefaultCellStyle.SelectionForeColor =
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

            // ── Columns matching Inventor ─────────
            _grid.Columns.Add(
                "ParamName", "Parameter Name");
            _grid.Columns.Add(
                "ConsumedBy", "Consumed By");
            _grid.Columns.Add(
                "Units", "Uni");
            _grid.Columns.Add(
                "Equation", "Equation");
            _grid.Columns.Add(
                "NominalValue", "Nominal");
            _grid.Columns.Add(
                "Tolerance", "Tol");
            _grid.Columns.Add(
                "ModelValue", "Model Value");

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

            // Column widths
            _grid.Columns["ParamName"]
                .FillWeight = 15;
            _grid.Columns["ConsumedBy"]
                .FillWeight = 15;
            _grid.Columns["Units"]
                .FillWeight = 5;
            _grid.Columns["Equation"]
                .FillWeight = 15;
            _grid.Columns["NominalValue"]
                .FillWeight = 10;
            _grid.Columns["Tolerance"]
                .FillWeight = 5;
            _grid.Columns["ModelValue"]
                .FillWeight = 10;
            _grid.Columns["Key"]
                .FillWeight = 5;
            _grid.Columns["Export"]
                .FillWeight = 5;
            _grid.Columns["Comment"]
                .FillWeight = 15;

            this.Controls.Add(_grid);
            this.Controls.Add(_btnRefresh);
        }

        public void LoadParameters()
        {
            _grid.Rows.Clear();

            try
            {
                Document doc = null;
                try
                {
                    doc = _app.ActiveDocument;
                }
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

                // ── Add group header ──────────────
                AddGroupHeader("Model Parameters");

                // ── Model Parameters ──────────────
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

                // ── User Parameters ───────────────
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
        }

        private void AddParameterRow(
            Parameter p, bool isUser)
        {
            string name = "";
            string consumedBy = "";
            string units = "";
            string equation = "";
            string nominal = "";
            string tolerance = "";
            string modelVal = "";
            bool isKey = false;
            bool isExport = false;
            string comment = "";

            try { name = p.Name; }
            catch { }

            try
            {
                consumedBy = "";
            }
            catch { }

            try { units = p.get_Units(); }
            catch { }

            try { equation = p.Expression; }
            catch { }

            try
            {
                double val =
                    (double)p.Value * 0.393701;
                if (units == "in" || units == "in.")
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
                tolerance = p.Tolerance.ToString();
            }
            catch { }

            try
            {
                double val =
                    (double)p.ModelValue * 0.393701;
                if (units == "in" || units == "in.")
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
                    modelVal = p.ModelValue.ToString();
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
            row.Cells["ConsumedBy"].Value = consumedBy;
            row.Cells["Units"].Value = units;
            row.Cells["Equation"].Value = equation;
            row.Cells["NominalValue"].Value = nominal;
            row.Cells["Tolerance"].Value = tolerance;
            row.Cells["ModelValue"].Value = modelVal;
            row.Cells["Key"].Value = isKey;
            row.Cells["Export"].Value = isExport;
            row.Cells["Comment"].Value = comment;

            if (isUser)
            {
                row.DefaultCellStyle.ForeColor =
                    SysColor.FromArgb(86, 156, 214);
            }
        }
    }
}