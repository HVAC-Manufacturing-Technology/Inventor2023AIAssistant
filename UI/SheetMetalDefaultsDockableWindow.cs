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

            _lblRule = CreateLabel(
                "Sheet Metal Rule:");
            _cboRule = new ComboBox
            {
                BackColor = System.Drawing.Color
                    .FromArgb(37, 37, 38),
                ForeColor =
                    System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle =
                    ComboBoxStyle.DropDownList
            };
            _cboRule.SelectedIndexChanged +=
                CboRule_Changed;

            _btnEditRule = CreateIconButton("✏️");
            _btnEditRule.Click +=
                (s, e) => EditActiveRule();

            _chkUseThickness = new CheckBox
            {
                Text = "Use Thickness from Rule",
                ForeColor =
                    System.Drawing.Color.White,
                AutoSize = true,
                Checked = true
            };
            _chkUseThickness.CheckedChanged +=
                (s, e) =>
                {
                    _txtThickness.ReadOnly =
                        _chkUseThickness.Checked;
                    UpdateThicknessDisplay();
                };

            _lblThickness = CreateLabel(
                "Thickness:");
            _txtThickness = new WinTextBox
            {
                BackColor = System.Drawing.Color
                    .FromArgb(37, 37, 38),
                ForeColor =
                    System.Drawing.Color.White,
                BorderStyle =
                    BorderStyle.FixedSingle,
                Font = new System.Drawing.Font(
                    "Segoe UI", 9f),
                ReadOnly = true
            };

            _lblMaterial = CreateLabel("Material:");
            _cboMaterial = new ComboBox
            {
                BackColor = System.Drawing.Color
                    .FromArgb(37, 37, 38),
                ForeColor =
                    System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle =
                    ComboBoxStyle.DropDownList
            };

            _lblUnfoldRule = CreateLabel(
                "Unfold Rule:");
            _cboUnfoldRule = new ComboBox
            {
                BackColor = System.Drawing.Color
                    .FromArgb(37, 37, 38),
                ForeColor =
                    System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle =
                    ComboBoxStyle.DropDownList
            };

            _btnEditUnfold =
                CreateIconButton("✏️");
            _btnEditUnfold.Click +=
                (s, e) => EditUnfoldRule();

            _btnOK = CreateButton("OK",
                System.Drawing.Color
                .FromArgb(0, 122, 204));
            _btnOK.Click += (s, e) =>
                ApplyValues();

            _btnCancel = CreateButton("Cancel",
                System.Drawing.Color
                .FromArgb(63, 63, 70));
            _btnCancel.Click += (s, e) =>
                RefreshValues();

            _btnApply = CreateButton("Apply",
                System.Drawing.Color
                .FromArgb(0, 122, 204));
            _btnApply.Click += (s, e) =>
                ApplyValues();

            // ── Manual Layout ─────────────────────
            int y = 10;
            int pad = 10;
            int rowH = 26;

            AddLbl(_lblRule, 10, y);
            y += 20;
            AddCtrl(_cboRule, 10, y, 195, rowH);
            AddCtrl(_btnEditRule, 210, y, 30, rowH);
            y += rowH + pad;

            AddCtrl(_chkUseThickness,
                10, y, 230, 22);
            y += 28;

            AddLbl(_lblThickness, 10, y);
            AddCtrl(_txtThickness,
                150, y - 2, 90, rowH);
            y += rowH + pad;

            AddLbl(_lblMaterial, 10, y);
            y += 20;
            AddCtrl(_cboMaterial,
                10, y, 220, rowH);
            y += rowH + pad;

            AddLbl(_lblUnfoldRule, 10, y);
            y += 20;
            AddCtrl(_cboUnfoldRule,
                10, y, 195, rowH);
            AddCtrl(_btnEditUnfold,
                210, y, 30, rowH);
            y += rowH + pad + 10;

            AddCtrl(_btnOK, 10, y, 70, 28);
            AddCtrl(_btnCancel, 88, y, 70, 28);
            AddCtrl(_btnApply, 166, y, 70, 28);
        }

        private void AddLbl(Label l, int x, int y)
        {
            l.Location =
                new System.Drawing.Point(x, y);
            Controls.Add(l);
        }

        private void AddCtrl(Control c,
            int x, int y, int w, int h)
        {
            c.Location =
                new System.Drawing.Point(x, y);
            c.Size =
                new System.Drawing.Size(w, h);
            Controls.Add(c);
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