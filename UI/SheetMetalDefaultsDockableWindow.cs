using WinTextBox = System.Windows.Forms.TextBox;
using System;
using System.Windows.Forms;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class SheetMetalDefaultsDockableWindow
    {
        private Inventor.Application _app;
        private DockableWindow _dockableWindow;
        private SheetMetalDefaultsPanel _panel;
        private readonly string _clientId;
        private bool _created = false;

        public SheetMetalDefaultsDockableWindow(
            Inventor.Application app,
            string clientId)
        {
            _app = app;
            _clientId = clientId;
        }

        public void Create()
        {
            if (_created) return;
            try
            {
                UserInterfaceManager uiMgr =
                    _app.UserInterfaceManager;

                try
                {
                    foreach (DockableWindow dw in
                        uiMgr.DockableWindows)
                    {
                        if (dw.InternalName ==
                            "SheetMetalDefaultsPanel")
                        {
                            _dockableWindow = dw;
                            _created = true;
                            return;
                        }
                    }
                }
                catch { }

                _dockableWindow =
                    uiMgr.DockableWindows.Add(
                        _clientId,
                        "SheetMetalDefaultsPanel",
                        "Sheet Metal Defaults");

                _dockableWindow
                    .SetMinimumSize(280, 420);

                _panel =
                    new SheetMetalDefaultsPanel(
                        _app);

                IntPtr handle = _panel.Handle;
                _dockableWindow.AddChild(handle);
                _dockableWindow.Visible = false;
                _created = true;
            }
            catch (Exception ex)
            {
                _created = false;
                throw new Exception(
                    "Failed to create Sheet Metal " +
                    "Defaults panel: " + ex.Message);
            }
        }

        public void Show()
        {
            if (!_created) Create();
            if (_dockableWindow != null)
            {
                _dockableWindow.Visible = true;
                _panel?.RefreshValues();
            }
        }

        public void Hide()
        {
            if (_dockableWindow != null)
                _dockableWindow.Visible = false;
        }

        public void Toggle()
        {
            if (_dockableWindow == null) return;
            if (_dockableWindow.Visible)
                Hide();
            else
                Show();
        }

        public bool IsVisible =>
            _dockableWindow?.Visible ?? false;
    }

    public class SheetMetalDefaultsPanel
        : System.Windows.Forms.UserControl
    {
        private readonly Inventor.Application _app;

        private Label _lblRule;
        private ComboBox _cboRule;
        private Button _btnEditRule;
        private CheckBox _chkUseThickness;
        private Label _lblThickness;
        private WinTextBox _txtThickness;
        private Label _lblMaterial;
        private ComboBox _cboMaterial;
        private Label _lblUnfoldRule;
        private ComboBox _cboUnfoldRule;
        private Button _btnEditUnfold;
        private Button _btnOK;
        private Button _btnCancel;
        private Button _btnApply;

        private SheetMetalStyle _activeStyle;

        public SheetMetalDefaultsPanel(
            Inventor.Application app)
        {
            _app = app;
            BuildUI();
            RefreshValues();
        }

        private void BuildUI()
        {
            BackColor = System.Drawing.Color
                .FromArgb(45, 45, 48);
            ForeColor = System.Drawing.Color.White;
            Padding = new Padding(10);
            AutoScroll = true;

            // ── Main layout table ─────────────────
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                AutoSize = true,
                BackColor = System.Drawing.Color
                    .FromArgb(45, 45, 48),
                Padding = new Padding(6)
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent, 40f));
            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent, 52f));
            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute, 34f));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 26f));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 32f));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 30f));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 32f));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 26f));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 32f));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 26f));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 32f));
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 38f));

            // ── Row 0 — Rule Label ────────────────
            _lblRule = CreateLabel("Sheet Metal Rule:");
            layout.Controls.Add(_lblRule, 0, 0);
            layout.SetColumnSpan(_lblRule, 3);

            // ── Row 1 — Rule Dropdown + Edit ──────
            _cboRule = new ComboBox
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color
                    .FromArgb(37, 37, 38),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle =
                    ComboBoxStyle.DropDownList
            };
            _cboRule.SelectedIndexChanged +=
                CboRule_Changed;

            _btnEditRule = CreateIconButton("✏️");
            _btnEditRule.Dock = DockStyle.Fill;
            _btnEditRule.Click +=
                (s, e) => EditActiveRule();

            layout.Controls.Add(_cboRule, 0, 1);
            layout.SetColumnSpan(_cboRule, 2);
            layout.Controls.Add(_btnEditRule, 2, 1);

            // ── Row 2 — Use Thickness Checkbox ────
            _chkUseThickness = new CheckBox
            {
                Text = "Use Thickness from Rule",
                ForeColor = System.Drawing.Color.White,
                Dock = DockStyle.Fill,
                Checked = true
            };
            _chkUseThickness.CheckedChanged +=
                (s, e) =>
                {
                    _txtThickness.ReadOnly =
                        _chkUseThickness.Checked;
                    UpdateThicknessDisplay();
                };
            layout.Controls.Add(
                _chkUseThickness, 0, 2);
            layout.SetColumnSpan(_chkUseThickness, 3);

            // ── Row 3 — Thickness ─────────────────
            _lblThickness = CreateLabel("Thickness:");
            _txtThickness = new WinTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color
                    .FromArgb(37, 37, 38),
                ForeColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new System.Drawing.Font(
                    "Segoe UI", 9f),
                ReadOnly = true
            };

            layout.Controls.Add(_lblThickness, 0, 3);
            layout.Controls.Add(_txtThickness, 1, 3);
            layout.SetColumnSpan(_txtThickness, 2);

            // ── Row 4 — Material Label ────────────
            _lblMaterial = CreateLabel("Material:");
            layout.Controls.Add(_lblMaterial, 0, 4);
            layout.SetColumnSpan(_lblMaterial, 3);

            // ── Row 5 — Material Dropdown ─────────
            _cboMaterial = new ComboBox
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color
                    .FromArgb(37, 37, 38),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle =
                    ComboBoxStyle.DropDownList
            };
            layout.Controls.Add(_cboMaterial, 0, 5);
            layout.SetColumnSpan(_cboMaterial, 3);

            // ── Row 6 — Unfold Rule Label ─────────
            _lblUnfoldRule = CreateLabel("Unfold Rule:");
            layout.Controls.Add(_lblUnfoldRule, 0, 6);
            layout.SetColumnSpan(_lblUnfoldRule, 3);

            // ── Row 7 — Unfold Dropdown + Edit ────
            _cboUnfoldRule = new ComboBox
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color
                    .FromArgb(37, 37, 38),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle =
                    ComboBoxStyle.DropDownList
            };

            _btnEditUnfold = CreateIconButton("✏️");
            _btnEditUnfold.Dock = DockStyle.Fill;
            _btnEditUnfold.Click +=
                (s, e) => EditUnfoldRule();

            layout.Controls.Add(_cboUnfoldRule, 0, 7);
            layout.SetColumnSpan(_cboUnfoldRule, 2);
            layout.Controls.Add(_btnEditUnfold, 2, 7);

            // ── Row 8 — Buttons ───────────────────
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection =
                    FlowDirection.LeftToRight,
                BackColor = System.Drawing.Color
                    .FromArgb(45, 45, 48),
                AutoSize = true
            };

            _btnOK = CreateButton("OK",
                System.Drawing.Color
                .FromArgb(0, 122, 204));
            _btnOK.Click += (s, e) => ApplyValues();

            _btnCancel = CreateButton("Cancel",
                System.Drawing.Color
                .FromArgb(63, 63, 70));
            _btnCancel.Click += (s, e) =>
                RefreshValues();

            _btnApply = CreateButton("Apply",
                System.Drawing.Color
                .FromArgb(0, 122, 204));
            _btnApply.Click += (s, e) => ApplyValues();

            btnPanel.Controls.Add(_btnOK);
            btnPanel.Controls.Add(_btnCancel);
            btnPanel.Controls.Add(_btnApply);

            layout.Controls.Add(btnPanel, 0, 8);
            layout.SetColumnSpan(btnPanel, 3);

            Controls.Add(layout);
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = System.Drawing.Color
                    .FromArgb(180, 180, 180),
                Font = new System.Drawing.Font(
                    "Segoe UI", 9f),
                AutoSize = true
            };
        }

        private Button CreateIconButton(
            string icon)
        {
            return new Button
            {
                Text = icon,
                BackColor = System.Drawing.Color
                    .FromArgb(63, 63, 70),
                ForeColor =
                    System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
        }

        private Button CreateButton(
            string text,
            System.Drawing.Color color)
        {
            return new Button
            {
                Text = text,
                BackColor = color,
                ForeColor =
                    System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
        }

        public void RefreshValues()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum
                    .kPartDocumentObject)
                    return;

                PartDocument part =
                    (PartDocument)doc;

                if (!(part.ComponentDefinition
                    is SheetMetalComponentDefinition))
                    return;

                SheetMetalComponentDefinition smDef =
                    (SheetMetalComponentDefinition)
                    part.ComponentDefinition;

                // Load styles into dropdown
                _cboRule.Items.Clear();
                _activeStyle =
                    smDef.ActiveSheetMetalStyle;

                foreach (SheetMetalStyle style in
                    smDef.SheetMetalStyles)
                {
                    _cboRule.Items.Add(style.Name);
                    if (style.Name ==
                        _activeStyle?.Name)
                        _cboRule.SelectedItem =
                            style.Name;
                }

                // Use thickness checkbox
                _chkUseThickness.Checked =
                    smDef.UseSheetMetalStyleThickness;
                _txtThickness.ReadOnly =
                    _chkUseThickness.Checked;

                // Show thickness string directly
                UpdateThicknessDisplay();

                // Material
                _cboMaterial.Items.Clear();
                _cboMaterial.Items.Add(
                    "By Sheet Metal Rule ( " +
                    (part.ComponentDefinition
                    .Material?.Name ?? "Steel") +
                    " )");
                _cboMaterial.SelectedIndex = 0;

                // Unfold Rule
                _cboUnfoldRule.Items.Clear();
                _cboUnfoldRule.Items.Add(
                    "By Sheet Metal Rule ( " +
                    (_activeStyle?.UnfoldMethod
                    .ToString() ??
                    "Default_kFactor") + " )");
                _cboUnfoldRule.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug
                    .WriteLine(
                    "RefreshValues: " + ex.Message);
            }
        }

        private void UpdateThicknessDisplay()
        {
            try
            {
                if (_activeStyle != null)
                    _txtThickness.Text =
                        _activeStyle.Thickness;
            }
            catch { }
        }

        private void CboRule_Changed(
            object sender, EventArgs e)
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null) return;

                PartDocument part =
                    (PartDocument)doc;

                if (!(part.ComponentDefinition
                    is SheetMetalComponentDefinition))
                    return;

                SheetMetalComponentDefinition smDef =
                    (SheetMetalComponentDefinition)
                    part.ComponentDefinition;

                string selected =
                    _cboRule.SelectedItem
                    ?.ToString();

                foreach (SheetMetalStyle style in
                    smDef.SheetMetalStyles)
                {
                    if (style.Name == selected)
                    {
                        _activeStyle = style;
                        UpdateThicknessDisplay();
                        break;
                    }
                }
            }
            catch { }
        }

        private void EditActiveRule()
        {
            MessageBox.Show(
                "To edit Sheet Metal Rules:\n" +
                "Manage → Styles Editor → " +
                "Sheet Metal",
                "Edit Rule");
        }

        private void EditUnfoldRule()
        {
            MessageBox.Show(
                "To edit Unfold Rules:\n" +
                "Manage → Styles Editor → " +
                "Unfold Rule",
                "Edit Unfold Rule");
        }

        private void ApplyValues()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum
                    .kPartDocumentObject)
                {
                    MessageBox.Show(
                        "Please open a Sheet " +
                        "Metal part first.");
                    return;
                }

                PartDocument part =
                    (PartDocument)doc;

                if (!(part.ComponentDefinition
                    is SheetMetalComponentDefinition))
                {
                    MessageBox.Show(
                        "Not a Sheet Metal part.");
                    return;
                }

                SheetMetalComponentDefinition smDef =
                    (SheetMetalComponentDefinition)
                    part.ComponentDefinition;

                // Apply selected style
                if (_activeStyle != null)
                {
                    smDef.SetBodySheetMetalStyle(
                        null, _activeStyle);
                }

                // Apply use thickness setting
                smDef.UseSheetMetalStyleThickness =
                    _chkUseThickness.Checked;

                // Apply manual thickness if needed
                if (!_chkUseThickness.Checked &&
                    !string.IsNullOrEmpty(
                        _txtThickness.Text))
                {
                    _activeStyle.Thickness =
                        _txtThickness.Text;
                }

                MessageBox.Show(
                    "✅ Sheet Metal defaults " +
                    "applied!",
                    "Applied");

                RefreshValues();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "❌ Apply failed:\n" +
                    ex.Message);
            }
        }
    }
}