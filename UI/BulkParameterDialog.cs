using Inventor;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using SysColor = System.Drawing.Color;
using SysEnvironment = System.Environment;
using SysFont = System.Drawing.Font;
using SysSize = System.Drawing.Size;

namespace Inventor2023AIAssistant
{
    public class BulkParameterDialog : Form
    {
        private sealed class InventorInputTextBox :
            System.Windows.Forms.TextBox
        {
            private const int WM_GETDLGCODE =
                0x0087;

            private const int DLGC_WANTARROWS =
                0x0001;

            private const int DLGC_WANTTAB =
                0x0002;

            private const int DLGC_WANTALLKEYS =
                0x0004;

            private const int DLGC_HASSETSEL =
                0x0008;

            private const int DLGC_WANTCHARS =
                0x0080;

            protected override void WndProc(
                ref Message message)
            {
                if (message.Msg == WM_GETDLGCODE)
                {
                    message.Result =
                        new IntPtr(
                            DLGC_WANTARROWS
                            | DLGC_WANTTAB
                            | DLGC_WANTALLKEYS
                            | DLGC_HASSETSEL
                            | DLGC_WANTCHARS);

                    return;
                }

                base.WndProc(
                    ref message);
            }

            protected override bool IsInputKey(
                Keys keyData)
            {
                return true;
            }
        }

        private readonly Inventor.Application _app;

        private InventorInputTextBox _txtInput;
        private Button _btnAddAll;
        private Button _btnCancel;
        private Label _lblInstructions;

        public bool ParametersAdded
        {
            get;
            private set;
        }

        private class BulkParameterRow
        {
            public int LineNumber
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }

            public string Expression
            {
                get;
                set;
            }

            public string Units
            {
                get;
                set;
            }

            public bool Export
            {
                get;
                set;
            }
        }

        public BulkParameterDialog(
            Inventor.Application app)
        {
            _app = app;

            BuildUI();
        }

        private void BuildUI()
        {
            Text =
                "Bulk Add Parameters";

            StartPosition =
                FormStartPosition.CenterParent;

            Size =
                new SysSize(720, 520);

            MinimumSize =
                new SysSize(600, 420);

            BackColor =
                SysColor.FromArgb(45, 45, 48);

            ForeColor =
                SysColor.White;

            Font =
                new SysFont("Segoe UI", 9f);

            KeyPreview =
                false;

            BuildInstructions();
            BuildButtons();
            BuildInputBox();

            Shown +=
                BulkParameterDialog_Shown;

            Activated +=
                BulkParameterDialog_Activated;

            CancelButton =
                _btnCancel;
        }

        private void BuildInstructions()
        {
            _lblInstructions =
                new Label();

            _lblInstructions.Dock =
                DockStyle.Top;

            _lblInstructions.Height =
                95;

            _lblInstructions.Padding =
                new Padding(10, 10, 10, 6);

            _lblInstructions.ForeColor =
                SysColor.White;

            _lblInstructions.Text =
                "Paste tab-separated or comma-separated parameters below."
                + SysEnvironment.NewLine
                + SysEnvironment.NewLine
                + "Format: Name, Expression, Units, Export"
                + SysEnvironment.NewLine
                + "Example: PanelWidth,24 in,in,true";

            Controls.Add(
                _lblInstructions);
        }

        private void BuildButtons()
        {
            Panel buttonPanel =
                new Panel();

            buttonPanel.Dock =
                DockStyle.Bottom;

            buttonPanel.Height =
                50;

            buttonPanel.Padding =
                new Padding(10, 8, 10, 8);

            buttonPanel.BackColor =
                SysColor.FromArgb(37, 37, 38);

            Controls.Add(
                buttonPanel);

            _btnCancel =
                new Button();

            _btnCancel.Text =
                "Cancel";

            _btnCancel.Width =
                90;

            _btnCancel.Height =
                30;

            _btnCancel.Dock =
                DockStyle.Right;

            _btnCancel.DialogResult =
                DialogResult.Cancel;

            _btnCancel.BackColor =
                SysColor.FromArgb(80, 80, 80);

            _btnCancel.ForeColor =
                SysColor.White;

            _btnCancel.FlatStyle =
                FlatStyle.Flat;

            _btnCancel.TabIndex =
                2;

            buttonPanel.Controls.Add(
                _btnCancel);

            _btnAddAll =
                new Button();

            _btnAddAll.Text =
                "Add All";

            _btnAddAll.Width =
                110;

            _btnAddAll.Height =
                30;

            _btnAddAll.Dock =
                DockStyle.Right;

            _btnAddAll.BackColor =
                SysColor.FromArgb(0, 153, 102);

            _btnAddAll.ForeColor =
                SysColor.White;

            _btnAddAll.FlatStyle =
                FlatStyle.Flat;

            _btnAddAll.TabIndex =
                1;

            _btnAddAll.Click +=
                BtnAddAll_Click;

            buttonPanel.Controls.Add(
                _btnAddAll);
        }

        private void BuildInputBox()
        {
            _txtInput =
                new InventorInputTextBox();

            _txtInput.Dock =
                DockStyle.Fill;

            _txtInput.Multiline =
                true;

            _txtInput.AcceptsReturn =
                true;

            _txtInput.AcceptsTab =
                true;

            _txtInput.ScrollBars =
                ScrollBars.Both;

            _txtInput.WordWrap =
                false;

            _txtInput.ShortcutsEnabled =
                true;

            _txtInput.ReadOnly =
                false;

            _txtInput.Enabled =
                true;

            _txtInput.TabStop =
                true;

            _txtInput.TabIndex =
                0;

            _txtInput.BorderStyle =
                BorderStyle.FixedSingle;

            _txtInput.BackColor =
                SysColor.FromArgb(30, 30, 30);

            _txtInput.ForeColor =
                SysColor.White;

            _txtInput.Font =
                new SysFont("Consolas", 10f);

            _txtInput.Text =
                string.Empty;

            _txtInput.Enter +=
                TxtInput_Enter;

            _txtInput.MouseDown +=
                TxtInput_MouseDown;

            Controls.Add(
                _txtInput);

            _txtInput.BringToFront();
        }

        private void BulkParameterDialog_Shown(
            object sender,
            EventArgs e)
        {
            BeginInvoke(
                new Action(
                    FocusInputBox));
        }

        private void BulkParameterDialog_Activated(
            object sender,
            EventArgs e)
        {
            BeginInvoke(
                new Action(
                    FocusInputBox));
        }

        private void TxtInput_Enter(
            object sender,
            EventArgs e)
        {
            _txtInput.SelectionStart =
                _txtInput.TextLength;

            _txtInput.SelectionLength =
                0;
        }

        private void TxtInput_MouseDown(
            object sender,
            MouseEventArgs e)
        {
            ActiveControl =
                _txtInput;

            _txtInput.Select();

            _txtInput.Focus();
        }

        private void FocusInputBox()
        {
            if (_txtInput == null
                || _txtInput.IsDisposed
                || !_txtInput.CanSelect)
            {
                return;
            }

            ActiveControl =
                _txtInput;

            _txtInput.Select();

            _txtInput.Focus();

            _txtInput.SelectionStart =
                _txtInput.TextLength;

            _txtInput.SelectionLength =
                0;

            _txtInput.ScrollToCaret();
        }

        private void BtnAddAll_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                Inventor.Document doc =
                    GetActiveDocument();

                if (doc == null)
                {
                    MessageBox.Show(
                        "Open a part or assembly before adding parameters.",
                        "Bulk Add Parameters",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                List<BulkParameterRow> rows =
                    ParseRows(
                        _txtInput.Text);

                if (rows.Count == 0)
                {
                    MessageBox.Show(
                        "No valid parameter rows were found.",
                        "Bulk Add Parameters",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    FocusInputBox();

                    return;
                }

                UserParameters userParameters =
                    GetUserParameters(
                        doc);

                if (userParameters == null)
                {
                    MessageBox.Show(
                        "User parameters are unavailable for the active document.",
                        "Bulk Add Parameters",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                List<string> existingNames =
                    GetExistingNames(
                        userParameters);

                List<string> pastedNames =
                    new List<string>();

                List<BulkParameterRow> uniqueRows =
                    new List<BulkParameterRow>();

                int duplicateCount =
                    0;

                foreach (
                    BulkParameterRow row in rows)
                {
                    string normalizedName =
                        NormalizeName(
                            row.Name);

                    if (pastedNames.Contains(
                        normalizedName))
                    {
                        duplicateCount++;

                        continue;
                    }

                    pastedNames.Add(
                        normalizedName);

                    uniqueRows.Add(
                        row);
                }

                rows =
                    uniqueRows;

                Transaction transaction =
                    null;

                int addedCount =
                    0;

                int skippedCount =
                    0;

                try
                {
                    transaction =
                        _app.TransactionManager
                            .StartTransaction(
                                (Inventor._Document)doc,
                                "Bulk Add Parameters");

                    foreach (
                        BulkParameterRow row in rows)
                    {
                        string normalizedName =
                            NormalizeName(
                                row.Name);

                        if (existingNames.Contains(
                            normalizedName))
                        {
                            skippedCount++;

                            continue;
                        }

                        UserParameter newParameter =
                            userParameters
                                .AddByExpression(
                                    row.Name,
                                    row.Expression,
                                    row.Units);

                        newParameter.ExposedAsProperty =
                            row.Export;

                        existingNames.Add(
                            normalizedName);

                        addedCount++;
                    }

                    doc.Update();

                    transaction.End();

                    transaction =
                        null;
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        try
                        {
                            transaction.Abort();
                        }
                        catch
                        {
                        }
                    }

                    throw new Exception(
                        "No parameters were added because the batch encountered an error."
                        + SysEnvironment.NewLine
                        + SysEnvironment.NewLine
                        + ex.Message,
                        ex);
                }

                ParametersAdded =
                    addedCount > 0;

                MessageBox.Show(
                    "Bulk parameter operation completed."
                    + SysEnvironment.NewLine
                    + SysEnvironment.NewLine
                    + "Added: "
                    + addedCount
                    + SysEnvironment.NewLine
                    + "Skipped existing: "
                    + skippedCount
                    + SysEnvironment.NewLine
                    + "Ignored duplicate rows: "
                    + duplicateCount,
                    "Bulk Add Parameters",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult =
                    DialogResult.OK;

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Bulk Add Parameters",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                FocusInputBox();
            }
        }

        private Inventor.Document GetActiveDocument()
        {
            Inventor.Document doc =
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

        private UserParameters GetUserParameters(
            Inventor.Document doc)
        {
            if (doc is PartDocument)
            {
                PartDocument partDoc =
                    (PartDocument)doc;

                return partDoc
                    .ComponentDefinition
                    .Parameters
                    .UserParameters;
            }

            if (doc is AssemblyDocument)
            {
                AssemblyDocument assemblyDoc =
                    (AssemblyDocument)doc;

                return assemblyDoc
                    .ComponentDefinition
                    .Parameters
                    .UserParameters;
            }

            return null;
        }

        private List<string> GetExistingNames(
            UserParameters userParameters)
        {
            List<string> names =
                new List<string>();

            foreach (
                UserParameter parameter
                in userParameters)
            {
                names.Add(
                    NormalizeName(
                        parameter.Name));
            }

            return names;
        }

        private string NormalizeName(
            string name)
        {
            return name
                .Trim()
                .ToLowerInvariant();
        }

        private List<BulkParameterRow> ParseRows(
            string input)
        {
            List<BulkParameterRow> rows =
                new List<BulkParameterRow>();

            string normalizedInput =
                input.Replace(
                    "\r\n",
                    "\n")
                .Replace(
                    "\r",
                    "\n");

            normalizedInput =
                RepairJoinedRows(
                    normalizedInput);

            string[] lines =
                normalizedInput.Split('\n');

            for (
                int index = 0;
                index < lines.Length;
                index++)
            {
                string line =
                    lines[index].Trim();

                if (string.IsNullOrWhiteSpace(
                    line))
                {
                    continue;
                }

                char delimiter =
                    line.Contains("\t")
                        ? '\t'
                        : ',';

                string[] values =
                    line.Split(delimiter);

                for (
                    int valueIndex = 0;
                    valueIndex < values.Length;
                    valueIndex++)
                {
                    values[valueIndex] =
                        values[valueIndex].Trim();
                }

                bool isHeaderRow =
                    values.Length >= 3
                    && values[0].Equals(
                        "Name",
                        StringComparison.OrdinalIgnoreCase)
                    && values[1].Equals(
                        "Expression",
                        StringComparison.OrdinalIgnoreCase)
                    && values[2].Equals(
                        "Units",
                        StringComparison.OrdinalIgnoreCase);

                if (isHeaderRow)
                {
                    continue;
                }

                if (values.Length < 3)
                {
                    throw new Exception(
                        "Line "
                        + (index + 1)
                        + " does not contain Name, Expression, and Units.");
                }

                string name =
                    values[0];

                string expression =
                    values[1];

                string units =
                    values[2];

                string exportText =
                    values.Length >= 4
                        ? values[3]
                        : "false";

                if (string.IsNullOrWhiteSpace(
                    name))
                {
                    throw new Exception(
                        "Line "
                        + (index + 1)
                        + " has no parameter name.");
                }

                if (string.IsNullOrWhiteSpace(
                    expression))
                {
                    throw new Exception(
                        "Line "
                        + (index + 1)
                        + " has no expression.");
                }

                if (string.IsNullOrWhiteSpace(
                    units))
                {
                    throw new Exception(
                        "Line "
                        + (index + 1)
                        + " has no units.");
                }

                rows.Add(
                    new BulkParameterRow
                    {
                        LineNumber =
                            index + 1,

                        Name =
                            name,

                        Expression =
                            expression,

                        Units =
                            units,

                        Export =
                            ParseBoolean(
                                exportText)
                    });
            }

            return rows;
        }

        private string RepairJoinedRows(
            string input)
        {
            return input
                .Replace(
                    "falseName,",
                    "false"
                    + SysEnvironment.NewLine
                    + "Name,")
                .Replace(
                    "trueName,",
                    "true"
                    + SysEnvironment.NewLine
                    + "Name,")
                .Replace(
                    "falsName,",
                    "false"
                    + SysEnvironment.NewLine
                    + "Name,")
                .Replace(
                    "falseName\t",
                    "false"
                    + SysEnvironment.NewLine
                    + "Name\t")
                .Replace(
                    "trueName\t",
                    "true"
                    + SysEnvironment.NewLine
                    + "Name\t")
                .Replace(
                    "falsName\t",
                    "false"
                    + SysEnvironment.NewLine
                    + "Name\t");
        }

        private bool ParseBoolean(
            string value)
        {
            string normalized =
                value
                    .Trim()
                    .ToLowerInvariant();

            if (normalized == "yes"
                || normalized == "y"
                || normalized == "1"
                || normalized == "x"
                || normalized.StartsWith(
                    "true"))
            {
                return true;
            }

            if (normalized == "no"
                || normalized == "n"
                || normalized == "0"
                || normalized == ""
                || normalized.StartsWith(
                    "fals"))
            {
                return false;
            }

            throw new Exception(
                "Export value '"
                + value
                + "' is not recognized. "
                + "Use true, false, yes, "
                + "no, 1, or 0.");
        }
    }
}