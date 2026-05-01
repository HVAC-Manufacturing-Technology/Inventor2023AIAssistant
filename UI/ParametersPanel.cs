using Inventor;
using System;
using System.Windows.Forms;
using SysColor = System.Drawing.Color;
using SysFont = System.Drawing.Font;
using SysTextBox = System.Windows.Forms.TextBox;
using SysSize = System.Drawing.Size;
using SysPoint = System.Drawing.Point;

namespace Inventor2023AIAssistant
{
    public class ParametersPanel : UserControl
    {
        private readonly Inventor.Application _app;
        private DataGridView _grid;
        private Button _btnRefresh;
        private string _lastDocName = "";

        // Add parameter controls
        private Panel _addPanel;
        private SysTextBox _txtName;
        private SysTextBox _txtValue;
        private ComboBox _cboUnits;
        private Button _btnAdd;
        private Button _btnDelete;

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
                        try
                        {
                            doc =
                                _app.ActiveEditDocument;
                        }
                        catch { }
                        if (doc == null)
                        {
                            try
                            {
                                doc =
                                    _app.ActiveDocument;
                            }
                            catch { }
                        }
                        if (doc != null)
                            current =
                                doc.FullFileName;
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

            // ── Add Parameter Panel ───────────────
            _addPanel = new Panel();
            _addPanel.Dock = DockStyle.Top;
            _addPanel.Height = 68;
            _addPanel.BackColor =
                SysColor.FromArgb(37, 37, 38);
            _addPanel.Padding =
                new Padding(6, 4, 6, 4);

            // Row 1: Name and Value
            var lblName = new Label();
            lblName.Text = "Name:";
            lblName.ForeColor = SysColor.White;
            lblName.Font =
                new SysFont("Segoe UI", 8f);
            lblName.Location =
                new SysPoint(6, 6);
            lblName.Size = new SysSize(40, 18);
            _addPanel.Controls.Add(lblName);

            _txtName = new SysTextBox();
            _txtName.Location =
                new SysPoint(48, 4);
            _txtName.Size = new SysSize(90, 22);
            _txtName.BackColor =
                SysColor.FromArgb(30, 30, 30);
            _txtName.ForeColor = SysColor.White;
            _txtName.BorderStyle =
                BorderStyle.FixedSingle;
            _txtName.Font =
                new SysFont("Segoe UI", 8.5f);
            _addPanel.Controls.Add(_txtName);

            var lblValue = new Label();
            lblValue.Text = "Value:";
            lblValue.ForeColor = SysColor.White;
            lblValue.Font =
                new SysFont("Segoe UI", 8f);
            lblValue.Location =
                new SysPoint(144, 6);
            lblValue.Size = new SysSize(40, 18);
            _addPanel.Controls.Add(lblValue);

            _txtValue = new SysTextBox();
            _txtValue.Location =
                new SysPoint(186, 4);
            _txtValue.Size = new SysSize(70, 22);
            _txtValue.BackColor =
                SysColor.FromArgb(30, 30, 30);
            _txtValue.ForeColor = SysColor.White;
            _txtValue.BorderStyle =
                BorderStyle.FixedSingle;
            _txtValue.Font =
                new SysFont("Segoe UI", 8.5f);
            _addPanel.Controls.Add(_txtValue);

            // Row 2: Units, Add, Delete
            var lblUnits = new Label();
            lblUnits.Text = "Units:";
            lblUnits.ForeColor = SysColor.White;
            lblUnits.Font =
                new SysFont("Segoe UI", 8f);
            lblUnits.Location =
                new SysPoint(6, 34);
            lblUnits.Size = new SysSize(40, 18);
            _addPanel.Controls.Add(lblUnits);

            _cboUnits = new ComboBox();
            _cboUnits.Location =
                new SysPoint(48, 32);
            _cboUnits.Size = new SysSize(90, 22);
            _cboUnits.DropDownStyle =
                ComboBoxStyle.DropDownList;
            _cboUnits.BackColor =
                SysColor.FromArgb(30, 30, 30);
            _cboUnits.ForeColor = SysColor.White;
            _cboUnits.FlatStyle = FlatStyle.Flat;
            _cboUnits.Font =
                new SysFont("Segoe UI", 8.5f);
            _cboUnits.Items.AddRange(
                new object[]
                {
                    "in",
                    "mm",
                    "cm",
                    "ft",
                    "deg",
                    "rad",
                    "ul"
                });
            _cboUnits.SelectedIndex = 0;
            _addPanel.Controls.Add(_cboUnits);

            _btnAdd = new Button();
            _btnAdd.Text = "+ Add";
            _btnAdd.Location =
                new SysPoint(144, 30);
            _btnAdd.Size = new SysSize(55, 24);
            _btnAdd.BackColor =
                SysColor.FromArgb(0, 122, 204);
            _btnAdd.ForeColor = SysColor.White;
            _btnAdd.FlatStyle = FlatStyle.Flat;
            _btnAdd.Font =
                new SysFont("Segoe UI", 8f);
            _btnAdd.Click += BtnAdd_Click;
            _addPanel.Controls.Add(_btnAdd);

            _btnDelete = new Button();
            _btnDelete.Text = "Delete";
            _btnDelete.Location =
                new SysPoint(204, 30);
            _btnDelete.Size = new SysSize(55, 24);
            _btnDelete.BackColor =
                SysColor.FromArgb(180, 40, 40);
            _btnDelete.ForeColor = SysColor.White;
            _btnDelete.FlatStyle = FlatStyle.Flat;
            _btnDelete.Font =
                new SysFont("Segoe UI", 8f);
            _btnDelete.Click += BtnDelete_Click;
            _addPanel.Controls.Add(_btnDelete);

            // ── Refresh Button ────────────────────
            _btnRefresh = new Button();
            _btnRefresh.Text = "Refresh";
            _btnRefresh.Dock = DockStyle.Top;
            _btnRefresh.Height = 26;
            _btnRefresh.BackColor =
                SysColor.FromArgb(0, 122, 204);
            _btnRefresh.ForeColor = SysColor.White;
            _btnRefresh.FlatStyle = FlatStyle.Flat;
            _btnRefresh.Font =
                new SysFont("Segoe UI", 8.5f);
            _btnRefresh.Click +=
                (s, e) => LoadParameters();

            // ── DataGridView ──────────────────────
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

            // Add order matters for docking
            this.Controls.Add(_grid);
            this.Controls.Add(_btnRefresh);
            this.Controls.Add(_addPanel);
        }

        private void BtnAdd_Click(
            object sender, EventArgs e)
        {
            try
            {
                string name =
                    _txtName.Text.Trim();
                string value =
                    _txtValue.Text.Trim();
                string units =
                    _cboUnits.SelectedItem
                    ?.ToString() ?? "in";

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show(
                        "Please enter a " +
                        "parameter name.",
                        "Add Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    MessageBox.Show(
                        "Please enter a value " +
                        "or expression.",
                        "Add Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

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
                    catch { }
                }
                if (doc == null)
                {
                    MessageBox.Show(
                        "No active document.",
                        "Add Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

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

                UserParameters userParms =
                    parms.UserParameters;

                string expr = value;
                if (!value.Contains(" ") &&
                    units != "ul" &&
                    units != "deg" &&
                    units != "rad")
                    expr = value + " " + units;

                userParms.AddByExpression(
                    name, expr, units);

                _txtName.Clear();
                _txtValue.Clear();
                _txtName.Focus();

                LoadParameters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to add parameter:\n" +
                    ex.Message,
                    "Add Parameter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(
            object sender, EventArgs e)
        {
            try
            {
                if (_grid.SelectedRows.Count == 0)
                {
                    MessageBox.Show(
                        "Please select a " +
                        "parameter to delete.",
                        "Delete Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                DataGridViewRow row =
                    _grid.SelectedRows[0];

                if (row.Tag == null ||
                    row.Tag.ToString() == "header")
                {
                    MessageBox.Show(
                        "Cannot delete a " +
                        "group header.",
                        "Delete Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                string paramName =
                    row.Cells["ParamName"].Value
                    ?.ToString();

                if (string.IsNullOrWhiteSpace(
                    paramName))
                    return;

                DialogResult confirm =
                    MessageBox.Show(
                        "Delete parameter '" +
                        paramName + "'?",
                        "Confirm Delete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                    return;

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
                    catch { }
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

                foreach (Parameter p in parms)
                {
                    if (p.Name == paramName &&
                        p is UserParameter)
                    {
                        p.Delete();
                        LoadParameters();
                        return;
                    }
                }

                MessageBox.Show(
                    "Only User Parameters " +
                    "can be deleted.",
                    "Delete Parameter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to delete:\n" +
                    ex.Message,
                    "Delete Parameter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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
                    _grid.Columns[e.ColumnIndex]
                    .Name;

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
                    catch { }
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
                            AddParameterRow(
                                p, true);
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