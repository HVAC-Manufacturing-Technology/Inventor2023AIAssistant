using Inventor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using SysColor = System.Drawing.Color;
using SysEnvironment = System.Environment;
using SysFont = System.Drawing.Font;
using SysSize = System.Drawing.Size;

namespace Inventor2023AIAssistant
{
    public class BulkParameterDialog : Form
    {
        private sealed class DialogTheme
        {
            public SysColor FormBackground;
            public SysColor InstructionBackground;
            public SysColor InputBackground;
            public SysColor ActionBackground;
            public SysColor PrimaryText;
            public SysColor SecondaryText;
            public SysColor Border;
            public SysColor Accent;
            public SysColor AddAllBackground;
            public SysColor CancelBackground;
            public bool IsDark;
        }

        private static readonly DialogTheme DarkTheme =
            new DialogTheme
            {
                FormBackground = SysColor.FromArgb(45, 45, 48),
                InstructionBackground = SysColor.FromArgb(45, 45, 48),
                InputBackground = SysColor.FromArgb(30, 30, 30),
                ActionBackground = SysColor.FromArgb(37, 37, 38),
                PrimaryText = SysColor.FromArgb(240, 240, 240),
                SecondaryText = SysColor.FromArgb(200, 200, 200),
                Border = SysColor.FromArgb(80, 80, 80),
                Accent = SysColor.FromArgb(0, 122, 204),
                AddAllBackground = SysColor.FromArgb(0, 153, 102),
                CancelBackground = SysColor.FromArgb(70, 70, 72),
                IsDark = true
            };

        private static readonly DialogTheme LightTheme =
            new DialogTheme
            {
                FormBackground = SysColor.FromArgb(240, 240, 240),
                InstructionBackground = SysColor.FromArgb(240, 240, 240),
                InputBackground = SysColor.White,
                ActionBackground = SysColor.FromArgb(225, 225, 225),
                PrimaryText = SysColor.FromArgb(32, 32, 32),
                SecondaryText = SysColor.FromArgb(80, 80, 80),
                Border = SysColor.FromArgb(160, 160, 160),
                Accent = SysColor.FromArgb(0, 122, 204),
                AddAllBackground = SysColor.FromArgb(0, 153, 102),
                CancelBackground = SysColor.FromArgb(220, 220, 220),
                IsDark = false
            };

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE =
            20;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int value,
            int valueSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(
            IntPtr hwnd,
            string appName,
            string idList);

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
        private readonly DialogTheme _theme;

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

            public bool Key
            {
                get;
                set;
            }

            public string Comment
            {
                get;
                set;
            }
        }

        public BulkParameterDialog(
            Inventor.Application app)
        {
            _app = app;
            _theme = ResolveTheme(app);

            BuildUI();
        }

        private DialogTheme ResolveTheme(
            Inventor.Application app)
        {
            try
            {
                string activeThemeName =
                    app.ThemeManager.ActiveTheme.Name;

                if (activeThemeName.IndexOf(
                    "light",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return LightTheme;
                }

                if (activeThemeName.IndexOf(
                    "dark",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return DarkTheme;
                }
            }
            catch
            {
            }

            return DarkTheme;
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

            FormBorderStyle =
                FormBorderStyle.Sizable;

            BackColor =
                _theme.FormBackground;

            ForeColor =
                _theme.PrimaryText;

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

            Panel instructionPanel =
                new Panel();

            instructionPanel.Dock =
                DockStyle.Top;

            instructionPanel.Height =
                145;

            instructionPanel.BackColor =
                _theme.InstructionBackground;

            _lblInstructions.Dock =
                DockStyle.Fill;

            _lblInstructions.Padding =
                new Padding(12, 10, 12, 6);

            _lblInstructions.ForeColor =
                _theme.SecondaryText;

            _lblInstructions.BackColor =
                _theme.InstructionBackground;

            _lblInstructions.Font =
                new SysFont("Segoe UI", 9f);

            _lblInstructions.Text =
                "Paste comma-separated or tab-separated parameters below."
                + SysEnvironment.NewLine
                + SysEnvironment.NewLine
                + "Format:"
                + SysEnvironment.NewLine
                + "Name, Unit/Type, Equation, Key, Export, Comment"
                + SysEnvironment.NewLine
                + SysEnvironment.NewLine
                + "Example:"
                + SysEnvironment.NewLine
                + "PanelWidth, in, 24 in, true, true, Overall panel width";

            Panel separator =
                new Panel();

            separator.Dock =
                DockStyle.Bottom;

            separator.Height =
                2;

            separator.BackColor =
                _theme.Accent;

            instructionPanel.Controls.Add(
                _lblInstructions);

            instructionPanel.Controls.Add(
                separator);

            Controls.Add(
                instructionPanel);
        }

        private void BuildButtons()
        {
            Panel buttonPanel =
                new Panel();

            buttonPanel.Dock =
                DockStyle.Bottom;

            buttonPanel.Height =
                58;

            buttonPanel.BackColor =
                _theme.ActionBackground;

            Controls.Add(
                buttonPanel);

            _btnAddAll =
                CreateButton("Add All", 100, BtnAddAll_Click);

            _btnCancel =
                CreateButton("Cancel", 85, null);

            _btnCancel.DialogResult =
                DialogResult.Cancel;

            _btnAddAll.BackColor =
                _theme.AddAllBackground;

            _btnCancel.BackColor =
                _theme.CancelBackground;

            _btnAddAll.ForeColor =
                _theme.PrimaryText;

            _btnCancel.ForeColor =
                _theme.PrimaryText;

            _btnAddAll.FlatAppearance.BorderColor =
                _theme.Border;

            _btnCancel.FlatAppearance.BorderColor =
                _theme.Border;

            _btnAddAll.Height =
                30;

            _btnCancel.Height =
                30;

            _btnAddAll.Margin =
                new Padding(8, 0, 0, 0);

            _btnCancel.Margin =
                new Padding(8, 0, 0, 0);

            FlowLayoutPanel buttonFlow =
                new FlowLayoutPanel();

            buttonFlow.Dock =
                DockStyle.Fill;

            buttonFlow.FlowDirection =
                FlowDirection.RightToLeft;

            buttonFlow.WrapContents =
                false;

            buttonFlow.Padding =
                new Padding(12, 13, 12, 0);

            buttonFlow.BackColor =
                _theme.ActionBackground;

            Panel topBorder =
                new Panel();

            topBorder.Dock =
                DockStyle.Top;

            topBorder.Height =
                1;

            topBorder.BackColor =
                _theme.Border;

            buttonFlow.Controls.Add(_btnCancel);
            buttonFlow.Controls.Add(_btnAddAll);
            buttonPanel.Controls.Add(buttonFlow);
            buttonPanel.Controls.Add(topBorder);
        }

        private Button CreateButton(
            string text,
            int width,
            EventHandler click)
        {
            Button button =
                new Button();

            button.Text = text;
            button.Width = width;
            button.Height = 30;
            button.BackColor = _theme.CancelBackground;
            button.ForeColor = _theme.PrimaryText;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new SysFont("Segoe UI", 8.5f);
            button.FlatAppearance.BorderColor = _theme.Border;

            if (click != null)
            {
                button.Click += click;
            }

            return button;
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

            _txtInput.Font =
                new SysFont("Consolas", 10f);

            _txtInput.BackColor =
                _theme.InputBackground;

            _txtInput.ForeColor =
                _theme.PrimaryText;

            _txtInput.BorderStyle =
                BorderStyle.None;

            _txtInput.Text =
                string.Empty;

            _txtInput.Enter +=
                TxtInput_Enter;

            _txtInput.MouseDown +=
                TxtInput_MouseDown;

            Panel inputBorder =
                new Panel();

            inputBorder.Dock =
                DockStyle.Fill;

            inputBorder.Padding =
                new Padding(1);

            inputBorder.BackColor =
                _theme.Border;

            inputBorder.Controls.Add(
                _txtInput);

            Controls.Add(
                inputBorder);

            inputBorder.BringToFront();
        }

        private void BulkParameterDialog_Shown(
            object sender,
            EventArgs e)
        {
            ApplyDarkTitleBar();
            ApplyDarkInputTheme();

            BeginInvoke(
                new Action(
                    FocusInputBox));
        }

        private void ApplyDarkTitleBar()
        {
            if (!_theme.IsDark || !IsHandleCreated)
            {
                return;
            }

            try
            {
                int enabled = 1;
                int result = DwmSetWindowAttribute(
                    Handle,
                    DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref enabled,
                    sizeof(int));

                if (result != 0)
                {
                    DwmSetWindowAttribute(
                        Handle,
                        19,
                        ref enabled,
                        sizeof(int));
                }
            }
            catch
            {
            }
        }

        private void ApplyDarkInputTheme()
        {
            if (!_theme.IsDark
                || _txtInput == null
                || !_txtInput.IsHandleCreated)
            {
                return;
            }

            try
            {
                int result = SetWindowTheme(
                    _txtInput.Handle,
                    "DarkMode_Explorer",
                    null);

                if (result < 0)
                {
                    return;
                }
            }
            catch
            {
            }
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
                    ParseRows(_txtInput.Text);

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

                        newParameter.IsKey =
                            row.Key;

                        newParameter.Comment =
                            row.Comment;

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

            string[] lines =
                normalizedInput.Split('\n');

            int format =
                0;

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
                    SplitDelimitedLine(line, delimiter);

                if (IsLegacyHeader(values))
                {
                    format = 4;
                    continue;
                }

                if (IsNewFormatHeader(values))
                {
                    format = 6;
                    continue;
                }

                if (format == 0)
                {
                    if (values.Length == 4)
                    {
                        format = 4;
                    }
                    else if (values.Length == 6)
                    {
                        format = 6;
                    }
                    else
                    {
                        throw new Exception(
                            "Line " + (index + 1)
                            + " must contain either four legacy columns or six parameter columns.");
                    }
                }

                if (values.Length != format)
                {
                    throw new Exception(
                        "Line " + (index + 1)
                        + " does not match the selected " + format + "-column format.");
                }

                string name = values[0];
                string units = format == 4 ? values[2] : values[1];
                string expression = format == 4 ? values[1] : values[2];
                string keyText = format == 4 ? "false" : values[3];
                string exportText = format == 4 ? values[3] : values[4];
                string comment = format == 4 ? string.Empty : values[5];

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
                        + " has no equation.");
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

                        Key =
                            ParseBoolean(keyText),

                        Export =
                            ParseBoolean(
                                exportText),

                        Comment =
                            comment
                    });
            }

            return rows;
        }

        private string[] SplitDelimitedLine(
            string line,
            char delimiter)
        {
            List<string> values =
                new List<string>();

            StringBuilder value =
                new StringBuilder();

            bool inQuotes =
                false;

            for (int index = 0; index < line.Length; index++)
            {
                char current = line[index];

                if (current == '"')
                {
                    if (inQuotes
                        && index + 1 < line.Length
                        && line[index + 1] == '"')
                    {
                        value.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (current == delimiter && !inQuotes)
                {
                    values.Add(value.ToString().Trim());
                    value.Length = 0;
                }
                else
                {
                    value.Append(current);
                }
            }

            if (inQuotes)
            {
                throw new Exception(
                    "A quoted value is missing its closing quotation mark.");
            }

            values.Add(value.ToString().Trim());
            return values.ToArray();
        }

        private bool IsLegacyHeader(
            string[] values)
        {
            return values.Length == 4
                && values[0].Equals("Name", StringComparison.OrdinalIgnoreCase)
                && values[1].Equals("Expression", StringComparison.OrdinalIgnoreCase)
                && values[2].Equals("Units", StringComparison.OrdinalIgnoreCase)
                && values[3].Equals("Export", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsNewFormatHeader(
            string[] values)
        {
            if (values.Length != 6)
            {
                return false;
            }

            string[] expected =
            {
                "name",
                "unit",
                "equation",
                "key",
                "export",
                "comment"
            };

            for (int index = 0; index < values.Length; index++)
            {
                if (NormalizeHeader(values[index]) != expected[index])
                {
                    return false;
                }
            }

            return true;
        }

        private string NormalizeHeader(
            string value)
        {
            string normalized =
                value.Trim().Replace(" ", string.Empty).ToLowerInvariant();

            if (normalized == "name"
                || normalized == "parametername")
            {
                return "name";
            }

            if (normalized == "unit/type"
                || normalized == "units"
                || normalized == "unit")
            {
                return "unit";
            }

            if (normalized == "equation"
                || normalized == "expression")
            {
                return "equation";
            }

            if (normalized == "key")
            {
                return "key";
            }

            if (normalized == "export"
                || normalized == "exportparameter")
            {
                return "export";
            }

            if (normalized == "comment")
            {
                return "comment";
            }

            return normalized;
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
                "Boolean value '"
                + value
                + "' is not recognized. "
                + "Use true, false, yes, "
                + "no, 1, or 0.");
        }
    }
}