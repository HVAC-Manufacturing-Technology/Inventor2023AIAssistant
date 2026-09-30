using Inventor;
using System;
using System.Windows.Forms;

using SysColor = System.Drawing.Color;
using SysFont = System.Drawing.Font;
using SysPoint = System.Drawing.Point;
using SysSize = System.Drawing.Size;
using SysTextBox = System.Windows.Forms.TextBox;

namespace Inventor2023AIAssistant
{
    public class ParametersPanel : UserControl
    {
        private sealed class InventorWindowWrapper :
    IWin32Window
        {
            public InventorWindowWrapper(
                IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle
            {
                get;
                private set;
            }
        }
        private readonly Inventor.Application _app;

        private DataGridView _grid;
        private Button _btnRefresh;
        private string _lastDocName = "";

        private Panel _addPanel;
        private SysTextBox _txtName;
        private SysTextBox _txtValue;
        private ComboBox _cboUnits;
        private Button _btnAdd;
        private Button _btnBulkAdd;
        private Button _btnDelete;

        private Panel _searchPanel;
        private SysTextBox _txtSearch;

        private bool _loadingParameters;

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

            timer.Tick +=
                (s, e) =>
                {
                    try
                    {
                        string current = "";

                        try
                        {
                            Document doc =
                                GetActiveDocument();

                            if (doc != null)
                            {
                                current =
                                    doc.FullFileName;
                            }
                        }
                        catch
                        {
                        }

                        if (current != _lastDocName)
                        {
                            _lastDocName =
                                current;

                            LoadParameters();
                        }
                    }
                    catch
                    {
                    }
                };

            timer.Start();
        }

        private void BuildUI()
        {
            Dock = DockStyle.Fill;

            BackColor =
                SysColor.FromArgb(45, 45, 48);

            BuildSearchPanel();
            BuildAddPanel();
            BuildRefreshButton();
            BuildParameterGrid();

            Controls.Add(_grid);
            Controls.Add(_btnRefresh);
            Controls.Add(_addPanel);
            Controls.Add(_searchPanel);
        }

        private void BuildSearchPanel()
        {
            _searchPanel =
                new Panel();

            _searchPanel.Dock =
                DockStyle.Top;

            _searchPanel.Height =
                30;

            _searchPanel.BackColor =
                SysColor.FromArgb(37, 37, 38);

            _searchPanel.Padding =
                new Padding(6, 4, 6, 4);

            var lblSearch =
                new Label();

            lblSearch.Text =
                "\U0001F50D";

            lblSearch.ForeColor =
                SysColor.White;

            lblSearch.Font =
                new SysFont("Segoe UI", 9f);

            lblSearch.Location =
                new SysPoint(6, 5);

            lblSearch.Size =
                new SysSize(22, 20);

            _searchPanel.Controls.Add(
                lblSearch);

            _txtSearch =
                new SysTextBox();

            _txtSearch.Location =
                new SysPoint(28, 3);

            _txtSearch.Size =
                new SysSize(230, 22);

            _txtSearch.BackColor =
                SysColor.FromArgb(30, 30, 30);

            _txtSearch.ForeColor =
                SysColor.White;

            _txtSearch.BorderStyle =
                BorderStyle.FixedSingle;

            _txtSearch.Font =
                new SysFont("Segoe UI", 8.5f);

            _txtSearch.TextChanged +=
                (s, e) =>
                    ApplySearchFilter();

            _searchPanel.Controls.Add(
                _txtSearch);

            _searchPanel.Resize +=
                (s, e) =>
                {
                    _txtSearch.Width =
                        Math.Max(
                            50,
                            _searchPanel.Width - 40);
                };
        }

        private void BuildAddPanel()
        {
            _addPanel =
                new Panel();

            _addPanel.Dock =
                DockStyle.Top;

            _addPanel.Height =
                98;

            _addPanel.BackColor =
                SysColor.FromArgb(37, 37, 38);

            _addPanel.Padding =
                new Padding(6, 4, 6, 4);

            var lblName =
                new Label();

            lblName.Text =
                "Name:";

            lblName.ForeColor =
                SysColor.White;

            lblName.Font =
                new SysFont("Segoe UI", 8f);

            lblName.Location =
                new SysPoint(6, 6);

            lblName.Size =
                new SysSize(40, 18);

            _addPanel.Controls.Add(
                lblName);

            _txtName =
                new SysTextBox();

            _txtName.Location =
                new SysPoint(48, 4);

            _txtName.Size =
                new SysSize(90, 22);

            _txtName.BackColor =
                SysColor.FromArgb(30, 30, 30);

            _txtName.ForeColor =
                SysColor.White;

            _txtName.BorderStyle =
                BorderStyle.FixedSingle;

            _txtName.Font =
                new SysFont("Segoe UI", 8.5f);

            _addPanel.Controls.Add(
                _txtName);

            var lblValue =
                new Label();

            lblValue.Text =
                "Value:";

            lblValue.ForeColor =
                SysColor.White;

            lblValue.Font =
                new SysFont("Segoe UI", 8f);

            lblValue.Location =
                new SysPoint(144, 6);

            lblValue.Size =
                new SysSize(40, 18);

            _addPanel.Controls.Add(
                lblValue);

            _txtValue =
                new SysTextBox();

            _txtValue.Location =
                new SysPoint(186, 4);

            _txtValue.Size =
                new SysSize(70, 22);

            _txtValue.BackColor =
                SysColor.FromArgb(30, 30, 30);

            _txtValue.ForeColor =
                SysColor.White;

            _txtValue.BorderStyle =
                BorderStyle.FixedSingle;

            _txtValue.Font =
                new SysFont("Segoe UI", 8.5f);

            _addPanel.Controls.Add(
                _txtValue);

            var lblUnits =
                new Label();

            lblUnits.Text =
                "Units:";

            lblUnits.ForeColor =
                SysColor.White;

            lblUnits.Font =
                new SysFont("Segoe UI", 8f);

            lblUnits.Location =
                new SysPoint(6, 34);

            lblUnits.Size =
                new SysSize(40, 18);

            _addPanel.Controls.Add(
                lblUnits);

            _cboUnits =
                new ComboBox();

            _cboUnits.Location =
                new SysPoint(48, 32);

            _cboUnits.Size =
                new SysSize(90, 22);

            _cboUnits.DropDownStyle =
                ComboBoxStyle.DropDownList;

            _cboUnits.BackColor =
                SysColor.FromArgb(30, 30, 30);

            _cboUnits.ForeColor =
                SysColor.White;

            _cboUnits.FlatStyle =
                FlatStyle.Flat;

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

            _cboUnits.SelectedIndex =
                0;

            _addPanel.Controls.Add(
                _cboUnits);

            _btnAdd =
                new Button();

            _btnAdd.Text =
                "+ Add";

            _btnAdd.Location =
                new SysPoint(144, 30);

            _btnAdd.Size =
                new SysSize(55, 24);

            _btnAdd.BackColor =
                SysColor.FromArgb(0, 122, 204);

            _btnAdd.ForeColor =
                SysColor.White;

            _btnAdd.FlatStyle =
                FlatStyle.Flat;

            _btnAdd.Font =
                new SysFont("Segoe UI", 8f);

            _btnAdd.Click +=
                BtnAdd_Click;

            _addPanel.Controls.Add(
                _btnAdd);

            _btnDelete =
                new Button();

            _btnDelete.Text =
                "Delete";

            _btnDelete.Location =
                new SysPoint(204, 30);

            _btnDelete.Size =
                new SysSize(55, 24);

            _btnDelete.BackColor =
                SysColor.FromArgb(180, 40, 40);

            _btnDelete.ForeColor =
                SysColor.White;

            _btnDelete.FlatStyle =
                FlatStyle.Flat;

            _btnDelete.Font =
                new SysFont("Segoe UI", 8f);

            _btnDelete.Click +=
                BtnDelete_Click;

            _addPanel.Controls.Add(
                _btnDelete);

            _btnBulkAdd =
                new Button();

            _btnBulkAdd.Text =
                "Bulk Add Parameters";

            _btnBulkAdd.Location =
                new SysPoint(144, 58);

            _btnBulkAdd.Size =
                new SysSize(115, 24);

            _btnBulkAdd.BackColor =
                SysColor.FromArgb(0, 153, 102);

            _btnBulkAdd.ForeColor =
                SysColor.White;

            _btnBulkAdd.FlatStyle =
                FlatStyle.Flat;

            _btnBulkAdd.Font =
                new SysFont("Segoe UI", 8f);

            _btnBulkAdd.Click +=
                BtnBulkAdd_Click;

            _addPanel.Controls.Add(
                _btnBulkAdd);
        }

        private void BuildRefreshButton()
        {
            _btnRefresh =
                new Button();

            _btnRefresh.Text =
                "Refresh";

            _btnRefresh.Dock =
                DockStyle.Top;

            _btnRefresh.Height =
                26;

            _btnRefresh.BackColor =
                SysColor.FromArgb(0, 122, 204);

            _btnRefresh.ForeColor =
                SysColor.White;

            _btnRefresh.FlatStyle =
                FlatStyle.Flat;

            _btnRefresh.Font =
                new SysFont("Segoe UI", 8.5f);

            _btnRefresh.Click +=
                (s, e) =>
                    LoadParameters();
        }

        private void BuildParameterGrid()
        {
            _grid =
                new DataGridView();

            _grid.Dock =
                DockStyle.Fill;

            _grid.AllowUserToAddRows =
                false;

            _grid.AllowUserToDeleteRows =
                false;

            _grid.AllowUserToResizeRows =
                false;

            _grid.RowHeadersVisible =
                false;

            _grid.SelectionMode =
                DataGridViewSelectionMode
                    .FullRowSelect;

            _grid.MultiSelect =
                false;

            _grid.BorderStyle =
                BorderStyle.None;

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
                .ForeColor =
                SysColor.White;

            _grid.ColumnHeadersDefaultCellStyle
                .Font =
                new SysFont(
                    "Segoe UI",
                    8.5f,
                    System.Drawing.FontStyle.Bold);

            _grid.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode
                    .AutoSize;

            _grid.EnableHeadersVisualStyles =
                false;

            _grid.EditMode =
                DataGridViewEditMode
                    .EditOnKeystrokeOrF2;

            AddGridColumns();

            _grid.CellEndEdit +=
                Grid_CellEndEdit;

            _grid.CurrentCellDirtyStateChanged +=
                Grid_CurrentCellDirtyStateChanged;

            _grid.CellValueChanged +=
                Grid_CellValueChanged;
        }

        private void AddGridColumns()
        {
            _grid.Columns.Add(
                "ParamName",
                "Parameter Name");

            _grid.Columns["ParamName"]
                .ReadOnly =
                false;

            _grid.Columns.Add(
                "Units",
                "Units");

            _grid.Columns["Units"]
                .ReadOnly =
                true;

            _grid.Columns.Add(
                "Equation",
                "Equation");

            _grid.Columns["Equation"]
                .ReadOnly =
                false;

            _grid.Columns.Add(
                "NominalValue",
                "Nominal");

            _grid.Columns["NominalValue"]
                .ReadOnly =
                true;

            _grid.Columns.Add(
                "ModelValue",
                "Model Value");

            _grid.Columns["ModelValue"]
                .ReadOnly =
                true;

            var keyColumn =
                new DataGridViewCheckBoxColumn();

            keyColumn.Name =
                "Key";

            keyColumn.HeaderText =
                "Key";

            keyColumn.ReadOnly =
                true;

            _grid.Columns.Add(
                keyColumn);

            var exportColumn =
                new DataGridViewCheckBoxColumn();

            exportColumn.Name =
                "Export";

            exportColumn.HeaderText =
                "Export";

            exportColumn.ReadOnly =
                false;

            exportColumn.TrueValue =
                true;

            exportColumn.FalseValue =
                false;

            _grid.Columns.Add(
                exportColumn);

            _grid.Columns.Add(
                "Comment",
                "Comment");

            _grid.Columns["Comment"]
                .ReadOnly =
                false;

            _grid.Columns["ParamName"]
                .FillWeight =
                20;

            _grid.Columns["Units"]
                .FillWeight =
                8;

            _grid.Columns["Equation"]
                .FillWeight =
                18;

            _grid.Columns["NominalValue"]
                .FillWeight =
                12;

            _grid.Columns["ModelValue"]
                .FillWeight =
                12;

            _grid.Columns["Key"]
                .FillWeight =
                6;

            _grid.Columns["Export"]
                .FillWeight =
                6;

            _grid.Columns["Comment"]
                .FillWeight =
                18;
        }

        private void BtnBulkAdd_Click(
    object sender,
    EventArgs e)
        {
            try
            {
                IntPtr inventorHandle =
                    new IntPtr(
                        Convert.ToInt64(
                            _app.MainFrameHWND));

                InventorWindowWrapper owner =
                    new InventorWindowWrapper(
                        inventorHandle);

                using (BulkParameterDialog dialog =
                    new BulkParameterDialog(_app))
                {
                    DialogResult result =
                        dialog.ShowDialog(owner);

                    if (result == DialogResult.OK
                        && dialog.ParametersAdded)
                    {
                        LoadParameters();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to open Bulk Add Parameters."
                    + System.Environment.NewLine
                    + System.Environment.NewLine
                    + ex.Message,
                    "Bulk Add Parameters",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnAdd_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                string name =
                    _txtName.Text.Trim();

                string value =
                    _txtValue.Text.Trim();

                string units =
                    _cboUnits.SelectedItem
                        ?.ToString()
                    ?? "in";

                if (string.IsNullOrWhiteSpace(
                    name))
                {
                    MessageBox.Show(
                        "Please enter a parameter name.",
                        "Add Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (string.IsNullOrWhiteSpace(
                    value))
                {
                    MessageBox.Show(
                        "Please enter a value or expression.",
                        "Add Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                Document doc =
                    GetActiveDocument();

                if (doc == null)
                {
                    MessageBox.Show(
                        "No active document.",
                        "Add Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                Parameters parameters =
                    GetParameters(doc);

                if (parameters == null)
                {
                    return;
                }

                string expression =
                    value;

                if (!value.Contains(" ")
                    && units != "ul"
                    && units != "deg"
                    && units != "rad")
                {
                    expression =
                        value + " " + units;
                }

                parameters.UserParameters
                    .AddByExpression(
                        name,
                        expression,
                        units);

                _txtName.Clear();
                _txtValue.Clear();
                _txtName.Focus();

                LoadParameters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to add parameter:"
                    + System.Environment.NewLine
                    + ex.Message,
                    "Add Parameter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                if (_grid.SelectedRows.Count == 0)
                {
                    MessageBox.Show(
                        "Please select a parameter to delete.",
                        "Delete Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                DataGridViewRow row =
                    _grid.SelectedRows[0];

                if (row.Tag == null
                    || row.Tag.ToString() == "header")
                {
                    MessageBox.Show(
                        "Cannot delete a group header.",
                        "Delete Parameter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                string parameterName =
                    Convert.ToString(
                        row.Cells["ParamName"].Value);

                if (string.IsNullOrWhiteSpace(
                    parameterName))
                {
                    return;
                }

                DialogResult confirmation =
                    MessageBox.Show(
                        "Delete parameter '"
                        + parameterName
                        + "'?",
                        "Confirm Delete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                if (confirmation !=
                    DialogResult.Yes)
                {
                    return;
                }

                Document doc =
                    GetActiveDocument();

                Parameters parameters =
                    GetParameters(doc);

                if (parameters == null)
                {
                    return;
                }

                foreach (
                    Parameter parameter
                    in parameters)
                {
                    if (parameter.Name ==
                            parameterName
                        && parameter
                            is UserParameter)
                    {
                        parameter.Delete();

                        LoadParameters();

                        return;
                    }
                }

                MessageBox.Show(
                    "Only User Parameters can be deleted.",
                    "Delete Parameter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to delete:"
                    + System.Environment.NewLine
                    + ex.Message,
                    "Delete Parameter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void Grid_CurrentCellDirtyStateChanged(
            object sender,
            EventArgs e)
        {
            if (_grid.IsCurrentCellDirty
                && _grid.CurrentCell
                    is DataGridViewCheckBoxCell)
            {
                _grid.CommitEdit(
                    DataGridViewDataErrorContexts
                        .Commit);
            }
        }

        private void Grid_CellValueChanged(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (_loadingParameters
                || e.RowIndex < 0
                || e.ColumnIndex < 0)
            {
                return;
            }

            if (_grid.Columns[e.ColumnIndex]
                    .Name != "Export")
            {
                return;
            }

            try
            {
                DataGridViewRow row =
                    _grid.Rows[e.RowIndex];

                if (row.Tag == null
                    || row.Tag.ToString() ==
                        "header")
                {
                    return;
                }

                string parameterName =
                    row.Tag.ToString()
                        .Replace(
                            "param:",
                            "");

                bool exportValue =
                    Convert.ToBoolean(
                        row.Cells["Export"]
                            .Value
                        ?? false);

                Document doc =
                    GetActiveDocument();

                Parameters parameters =
                    GetParameters(doc);

                Parameter parameter =
                    FindParameter(
                        parameters,
                        parameterName);

                if (parameter == null)
                {
                    return;
                }

                parameter.ExposedAsProperty =
                    exportValue;

                doc.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to update Export:"
                    + System.Environment.NewLine
                    + ex.Message,
                    "Parameter Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                BeginInvoke(
                    new Action(
                        LoadParameters));
            }
        }

        private void Grid_CellEndEdit(
            object sender,
            DataGridViewCellEventArgs e)
        {
            try
            {
                if (_loadingParameters
                    || e.RowIndex < 0
                    || e.ColumnIndex < 0)
                {
                    return;
                }

                DataGridViewRow row =
                    _grid.Rows[e.RowIndex];

                if (row.Tag == null
                    || row.Tag.ToString() ==
                        "header")
                {
                    return;
                }

                string originalName =
                    row.Tag.ToString()
                        .Replace(
                            "param:",
                            "");

                string columnName =
                    _grid.Columns[e.ColumnIndex]
                        .Name;

                Document doc =
                    GetActiveDocument();

                Parameters parameters =
                    GetParameters(doc);

                Parameter parameter =
                    FindParameter(
                        parameters,
                        originalName);

                if (parameter == null)
                {
                    return;
                }

                if (columnName == "ParamName")
                {
                    string newName =
                        Convert.ToString(
                            row.Cells["ParamName"]
                                .Value)
                        ?.Trim();

                    if (string.IsNullOrWhiteSpace(
                            newName)
                        || newName ==
                            originalName)
                    {
                        return;
                    }

                    try
                    {
                        parameter.Name =
                            newName;

                        row.Tag =
                            "param:" + newName;

                        BeginInvoke(
                            new Action(
                                LoadParameters));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Failed to rename parameter:"
                            + System.Environment.NewLine
                            + ex.Message,
                            "Rename Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        BeginInvoke(
                            new Action(
                                LoadParameters));
                    }

                    return;
                }

                if (columnName == "Equation")
                {
                    string newExpression =
                        Convert.ToString(
                            row.Cells["Equation"]
                                .Value);

                    if (string.IsNullOrWhiteSpace(
                            newExpression))
                    {
                        return;
                    }

                    try
                    {
                        parameter.Expression =
                            newExpression;

                        doc.Update();

                        BeginInvoke(
                            new Action(
                                LoadParameters));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Failed to set expression:"
                            + System.Environment.NewLine
                            + ex.Message,
                            "Parameter Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        BeginInvoke(
                            new Action(
                                LoadParameters));
                    }

                    return;
                }

                if (columnName == "Comment")
                {
                    string newComment =
                        Convert.ToString(
                            row.Cells["Comment"]
                                .Value)
                        ?? "";

                    try
                    {
                        parameter.Comment =
                            newComment;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private Document GetActiveDocument()
        {
            Document doc =
                null;

            try
            {
                doc =
                    _app.ActiveEditDocument;
            }
            catch
            {
            }

            if (doc == null)
            {
                try
                {
                    doc =
                        _app.ActiveDocument;
                }
                catch
                {
                }
            }

            return doc;
        }

        private Parameters GetParameters(
            Document doc)
        {
            if (doc == null)
            {
                return null;
            }

            if (doc.DocumentType ==
                DocumentTypeEnum
                    .kPartDocumentObject)
            {
                return ((PartDocument)doc)
                    .ComponentDefinition
                    .Parameters;
            }

            if (doc.DocumentType ==
                DocumentTypeEnum
                    .kAssemblyDocumentObject)
            {
                return ((AssemblyDocument)doc)
                    .ComponentDefinition
                    .Parameters;
            }

            return null;
        }

        private Parameter FindParameter(
            Parameters parameters,
            string parameterName)
        {
            if (parameters == null
                || string.IsNullOrWhiteSpace(
                    parameterName))
            {
                return null;
            }

            foreach (
                Parameter parameter
                in parameters)
            {
                if (string.Equals(
                    parameter.Name,
                    parameterName,
                    StringComparison.Ordinal))
                {
                    return parameter;
                }
            }

            return null;
        }

        private void ApplySearchFilter()
        {
            string filter =
                _txtSearch.Text
                    .Trim()
                    .ToLowerInvariant();

            foreach (
                DataGridViewRow row
                in _grid.Rows)
            {
                if (row.Tag != null
                    && row.Tag.ToString() ==
                        "header")
                {
                    row.Visible =
                        true;

                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                    filter))
                {
                    row.Visible =
                        true;

                    continue;
                }

                string name =
                    Convert.ToString(
                        row.Cells["ParamName"]
                            .Value)
                    ?.ToLowerInvariant()
                    ?? "";

                string equation =
                    Convert.ToString(
                        row.Cells["Equation"]
                            .Value)
                    ?.ToLowerInvariant()
                    ?? "";

                string comment =
                    Convert.ToString(
                        row.Cells["Comment"]
                            .Value)
                    ?.ToLowerInvariant()
                    ?? "";

                row.Visible =
                    name.Contains(filter)
                    || equation.Contains(filter)
                    || comment.Contains(filter);
            }
        }

        public void LoadParameters()
        {
            _loadingParameters =
                true;

            try
            {
                _grid.Rows.Clear();

                Document doc =
                    GetActiveDocument();

                Parameters parameters =
                    GetParameters(doc);

                if (parameters == null)
                {
                    return;
                }

                AddGroupHeader(
                    "Model Parameters");

                foreach (
                    Parameter parameter
                    in parameters)
                {
                    try
                    {
                        if (parameter
                            is UserParameter)
                        {
                            continue;
                        }

                        AddParameterRow(
                            parameter,
                            false);
                    }
                    catch
                    {
                    }
                }

                bool hasUserParameters =
                    false;

                foreach (
                    Parameter parameter
                    in parameters)
                {
                    if (!(parameter
                        is UserParameter))
                    {
                        continue;
                    }

                    if (!hasUserParameters)
                    {
                        AddGroupHeader(
                            "User Parameters");

                        hasUserParameters =
                            true;
                    }

                    try
                    {
                        AddParameterRow(
                            parameter,
                            true);
                    }
                    catch
                    {
                    }
                }

                if (!string.IsNullOrWhiteSpace(
                    _txtSearch.Text))
                {
                    ApplySearchFilter();
                }
            }
            catch
            {
            }
            finally
            {
                _loadingParameters =
                    false;
            }
        }

        private void AddGroupHeader(
            string title)
        {
            int index =
                _grid.Rows.Add();

            DataGridViewRow row =
                _grid.Rows[index];

            row.Cells["ParamName"].Value =
                title;

            row.DefaultCellStyle.BackColor =
                SysColor.FromArgb(0, 122, 204);

            row.DefaultCellStyle.ForeColor =
                SysColor.White;

            row.DefaultCellStyle.Font =
                new SysFont(
                    "Segoe UI",
                    9f,
                    System.Drawing.FontStyle.Bold);

            row.Height =
                24;

            row.ReadOnly =
                true;

            row.Tag =
                "header";
        }

        private void AddParameterRow(
            Parameter parameter,
            bool isUser)
        {
            string name =
                "";

            string units =
                "";

            string equation =
                "";

            string nominal =
                "";

            string modelValue =
                "";

            bool isKey =
                false;

            bool isExport =
                false;

            string comment =
                "";

            try
            {
                name =
                    parameter.Name;
            }
            catch
            {
            }

            try
            {
                units =
                    parameter.get_Units();
            }
            catch
            {
            }

            try
            {
                equation =
                    parameter.Expression;
            }
            catch
            {
            }

            try
            {
                double value =
                    Convert.ToDouble(
                        parameter.Value);

                if (units == "in"
                    || units == "in.")
                {
                    nominal =
                        Math.Round(
                            value * 0.393701,
                            4)
                        .ToString();
                }
                else
                {
                    nominal =
                        parameter.Value
                            .ToString();
                }
            }
            catch
            {
                try
                {
                    nominal =
                        parameter.Value
                            .ToString();
                }
                catch
                {
                }
            }

            try
            {
                double value =
                    Convert.ToDouble(
                        parameter.ModelValue);

                if (units == "in"
                    || units == "in.")
                {
                    modelValue =
                        Math.Round(
                            value * 0.393701,
                            4)
                        .ToString();
                }
                else
                {
                    modelValue =
                        parameter.ModelValue
                            .ToString();
                }
            }
            catch
            {
                try
                {
                    modelValue =
                        parameter.ModelValue
                            .ToString();
                }
                catch
                {
                }
            }

            try
            {
                isKey =
                    parameter.IsKey;
            }
            catch
            {
            }

            try
            {
                isExport =
                    parameter.ExposedAsProperty;
            }
            catch
            {
            }

            try
            {
                comment =
                    parameter.Comment;
            }
            catch
            {
            }

            int index =
                _grid.Rows.Add();

            DataGridViewRow row =
                _grid.Rows[index];

            row.Cells["ParamName"].Value =
                name;

            row.Cells["Units"].Value =
                units;

            row.Cells["Equation"].Value =
                equation;

            row.Cells["NominalValue"].Value =
                nominal;

            row.Cells["ModelValue"].Value =
                modelValue;

            row.Cells["Key"].Value =
                isKey;

            row.Cells["Export"].Value =
                isExport;

            row.Cells["Comment"].Value =
                comment;

            row.Tag =
                "param:" + name;

            if (isUser)
            {
                row.DefaultCellStyle.ForeColor =
                    SysColor.FromArgb(
                        86,
                        156,
                        214);
            }
        }
    }
}