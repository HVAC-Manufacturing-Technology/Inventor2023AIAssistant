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

                // No minimum size restriction
                _dockableWindow
                    .SetMinimumSize(50, 50);

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

        // Width threshold for narrow mode
        private const int NarrowWidth = 200;

        public SheetMetalDefaultsPanel(
            Inventor.Application app)
        {
            _app = app;
            Dock = DockStyle.Fill;
            AutoScroll = true;
            BackColor = System.Drawing.Color
                .FromArgb(45, 45, 48);
            ForeColor = System.Drawing.Color.White;
            BuildControls();
            RefreshValues();
            Resize += (s, e) => UpdateLayout();
        }

        private void BuildControls()
        {
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
                Checked = true,
                AutoSize = false
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

            _lblMaterial = CreateLabel(
                "Material:");
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

            Controls.AddRange(new Control[]
            {
                _lblRule, _cboRule, _btnEditRule,
                _chkUseThickness,
                _lblThickness, _txtThickness,
                _lblMaterial, _cboMaterial,
                _lblUnfoldRule, _cboUnfoldRule,
                _btnEditUnfold,
                _btnOK, _btnCancel, _btnApply
            });

            UpdateLayout();
        }

        private void UpdateLayout()
        {
            int w = ClientSize.Width;
            int pad = 6;
            int rowH = 24;
            int btnH = 26;
            int editW = 26;
            bool narrow = w < NarrowWidth;
            int y = pad;

            if (narrow)
            {
                // ── Narrow mode ───────────────────
                // Stack everything vertically
                // with full width controls

                // Rule label + combo full width
                SetBounds(_lblRule,
                    pad, y,
                    w - pad * 2, 18);
                y += 20;

                SetBounds(_cboRule,
                    pad, y,
                    w - pad * 2 - editW - 2,
                    rowH);
                SetBounds(_btnEditRule,
                    w - pad - editW, y,
                    editW, rowH);
                y += rowH + pad;

                // Checkbox
                SetBounds(_chkUseThickness,
                    pad, y,
                    w - pad * 2, rowH);
                y += rowH + pad;

                // Thickness label + field
                SetBounds(_lblThickness,
                    pad, y,
                    w - pad * 2, 18);
                y += 20;
                SetBounds(_txtThickness,
                    pad, y,
                    w - pad * 2, rowH);
                y += rowH + pad;

                // Material label + combo
                SetBounds(_lblMaterial,
                    pad, y,
                    w - pad * 2, 18);
                y += 20;
                SetBounds(_cboMaterial,
                    pad, y,
                    w - pad * 2, rowH);
                y += rowH + pad;

                // Unfold label + combo
                SetBounds(_lblUnfoldRule,
                    pad, y,
                    w - pad * 2, 18);
                y += 20;
                SetBounds(_cboUnfoldRule,
                    pad, y,
                    w - pad * 2 - editW - 2,
                    rowH);
                SetBounds(_btnEditUnfold,
                    w - pad - editW, y,
                    editW, rowH);
                y += rowH + pad * 2;

                // Buttons stacked
                int bw = w - pad * 2;
                SetBounds(_btnOK,
                    pad, y, bw, btnH);
                y += btnH + 2;
                SetBounds(_btnCancel,
                    pad, y, bw, btnH);
                y += btnH + 2;
                SetBounds(_btnApply,
                    pad, y, bw, btnH);
                y += btnH + pad;
            }
            else
            {
                // ── Wide mode ─────────────────────
                // Label on left value on right

                int lblW = 90;
                int valW = w - lblW - pad * 3
                    - editW - 2;

                // Rule
                SetBounds(_lblRule,
                    pad, y,
                    w - pad * 2, 18);
                y += 20;
                SetBounds(_cboRule,
                    pad, y,
                    w - pad * 2 - editW - 2,
                    rowH);
                SetBounds(_btnEditRule,
                    w - pad - editW, y,
                    editW, rowH);
                y += rowH + pad;

                // Checkbox
                SetBounds(_chkUseThickness,
                    pad, y,
                    w - pad * 2, rowH);
                y += rowH + pad;

                // Thickness inline
                SetBounds(_lblThickness,
                    pad, y + 3, lblW, 18);
                SetBounds(_txtThickness,
                    pad + lblW + pad, y,
                    valW + editW + 2, rowH);
                y += rowH + pad;

                // Material
                SetBounds(_lblMaterial,
                    pad, y,
                    w - pad * 2, 18);
                y += 20;
                SetBounds(_cboMaterial,
                    pad, y,
                    w - pad * 2, rowH);
                y += rowH + pad;

                // Unfold Rule
                SetBounds(_lblUnfoldRule,
                    pad, y,
                    w - pad * 2, 18);
                y += 20;
                SetBounds(_cboUnfoldRule,
                    pad, y,
                    w - pad * 2 - editW - 2,
                    rowH);
                SetBounds(_btnEditUnfold,
                    w - pad - editW, y,
                    editW, rowH);
                y += rowH + pad * 2;

                // Buttons side by side
                int bw = (w - pad * 4) / 3;
                SetBounds(_btnOK,
                    pad, y, bw, btnH);
                SetBounds(_btnCancel,
                    pad * 2 + bw, y, bw, btnH);
                SetBounds(_btnApply,
                    pad * 3 + bw * 2, y,
                    bw, btnH);
                y += btnH + pad;
            }
        }

        private void SetBounds(
            Control c,
            int x, int y, int w, int h)
        {
            c.SetBounds(x, y, w, h);
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
                AutoSize = false
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

                // Load styles
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

                UpdateThicknessDisplay();

                // Material
                _cboMaterial.Items.Clear();
                try
                {
                    _cboMaterial.Items.Add(
                        "By Rule ( " +
                        part.ComponentDefinition
                        .Material.Name + " )");
                }
                catch
                {
                    _cboMaterial.Items.Add(
                        "By Sheet Metal Rule");
                }
                _cboMaterial.SelectedIndex = 0;

                // Unfold rule
                _cboUnfoldRule.Items.Clear();
                try
                {
                    _cboUnfoldRule.Items.Add(
                        "By Rule ( " +
                        _activeStyle?.UnfoldMethod
                        .ToString() + " )");
                }
                catch
                {
                    _cboUnfoldRule.Items.Add(
                        "By Sheet Metal Rule");
                }
                _cboUnfoldRule.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug
                    .WriteLine(
                    "RefreshValues: " +
                    ex.Message);
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

                if (_activeStyle != null)
                    smDef.SetBodySheetMetalStyle(
                        null, _activeStyle);

                smDef.UseSheetMetalStyleThickness =
                    _chkUseThickness.Checked;

                if (!_chkUseThickness.Checked &&
                    !string.IsNullOrEmpty(
                        _txtThickness.Text))
                    _activeStyle.Thickness =
                        _txtThickness.Text;

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