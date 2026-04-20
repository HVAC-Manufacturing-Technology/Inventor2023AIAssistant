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
                    .SetMinimumSize(260, 400);

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

        private Label _lblTitle;
        private Label _lblThickness;
        private Label _lblBendRadius;
        private Label _lblBendRelief;
        private Label _lblCornerRelief;
        private Label _lblMaterial;
        private Label _lblUnfoldRule;

        private WinTextBox _txtThickness;
        private WinTextBox _txtBendRadius;
        private WinTextBox _txtBendRelief;
        private WinTextBox _txtCornerRelief;
        private WinTextBox _txtMaterial;
        private WinTextBox _txtUnfoldRule;

        private Button _btnRefresh;
        private Button _btnApply;

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
            Padding = new Padding(8);

            _lblTitle = new Label
            {
                Text = "Sheet Metal Defaults",
                Font = new System.Drawing.Font(
                    "Segoe UI", 10f,
                    System.Drawing.FontStyle.Bold),
                ForeColor =
                    System.Drawing.Color.White,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign =
                    System.Drawing.ContentAlignment
                    .MiddleLeft
            };

            _lblThickness =
                CreateLabel("Thickness:");
            _txtThickness = CreateTextBox();

            _lblBendRadius =
                CreateLabel("Bend Radius:");
            _txtBendRadius = CreateTextBox();

            _lblBendRelief =
                CreateLabel("Bend Relief:");
            _txtBendRelief = CreateTextBox();
            _txtBendRelief.ReadOnly = true;

            _lblCornerRelief =
                CreateLabel("Corner Relief:");
            _txtCornerRelief = CreateTextBox();
            _txtCornerRelief.ReadOnly = true;

            _lblMaterial =
                CreateLabel("Material:");
            _txtMaterial = CreateTextBox();
            _txtMaterial.ReadOnly = true;

            _lblUnfoldRule =
                CreateLabel("Unfold Rule:");
            _txtUnfoldRule = CreateTextBox();
            _txtUnfoldRule.ReadOnly = true;

            _btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Height = 28,
                Dock = DockStyle.Bottom,
                BackColor = System.Drawing.Color
                    .FromArgb(63, 63, 70),
                ForeColor =
                    System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            _btnRefresh.Click += (s, e) =>
                RefreshValues();

            _btnApply = new Button
            {
                Text = "✅ Apply",
                Height = 28,
                Dock = DockStyle.Bottom,
                BackColor = System.Drawing.Color
                    .FromArgb(0, 122, 204),
                ForeColor =
                    System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            _btnApply.Click += (s, e) =>
                ApplyValues();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                BackColor = System.Drawing.Color
                    .FromArgb(45, 45, 48),
                Padding = new Padding(4)
            };

            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent, 40f));
            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent, 60f));

            layout.Controls.Add(
                _lblThickness, 0, 0);
            layout.Controls.Add(
                _txtThickness, 1, 0);
            layout.Controls.Add(
                _lblBendRadius, 0, 1);
            layout.Controls.Add(
                _txtBendRadius, 1, 1);
            layout.Controls.Add(
                _lblBendRelief, 0, 2);
            layout.Controls.Add(
                _txtBendRelief, 1, 2);
            layout.Controls.Add(
                _lblCornerRelief, 0, 3);
            layout.Controls.Add(
                _txtCornerRelief, 1, 3);
            layout.Controls.Add(
                _lblMaterial, 0, 4);
            layout.Controls.Add(
                _txtMaterial, 1, 4);
            layout.Controls.Add(
                _lblUnfoldRule, 0, 5);
            layout.Controls.Add(
                _txtUnfoldRule, 1, 5);

            Controls.Add(layout);
            Controls.Add(_btnApply);
            Controls.Add(_btnRefresh);
            Controls.Add(_lblTitle);
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
                Dock = DockStyle.Fill,
                TextAlign =
                    System.Drawing.ContentAlignment
                    .MiddleLeft
            };
        }

        private WinTextBox CreateTextBox()
        {
            return new WinTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color
                    .FromArgb(37, 37, 38),
                ForeColor =
                    System.Drawing.Color.White,
                BorderStyle =
                    BorderStyle.FixedSingle,
                Font = new System.Drawing.Font(
                    "Segoe UI", 9f)
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
                {
                    ClearValues();
                    return;
                }

                PartDocument part =
                    (PartDocument)doc;

                if (!(part.ComponentDefinition
                    is SheetMetalComponentDefinition))
                {
                    ClearValues();
                    return;
                }

                SheetMetalComponentDefinition smDef =
                    (SheetMetalComponentDefinition)
                    part.ComponentDefinition;

                // Thickness
                try
                {
                    double t =
                        smDef.Thickness.Value
                        * 0.393701;
                    _txtThickness.Text =
                        Math.Round(t, 4) + " in";
                }
                catch { _txtThickness.Text = "N/A"; }

                // Bend Radius
                try
                {
                    double br =
                        smDef.BendRadius.Value
                        * 0.393701;
                    _txtBendRadius.Text =
                        Math.Round(br, 4) + " in";
                }
                catch
                {
                    _txtBendRadius.Text = "N/A";
                }

                // Bend Relief
                try
                {
                    double brel =
                        smDef.BendRadius.Value
                        * 0.393701;
                    _txtBendRelief.Text =
                        Math.Round(brel, 4) + " in";
                }
                catch
                {
                    _txtBendRelief.Text = "N/A";
                }

                // Corner Relief Size
                try
                {
                    double cr =
                        smDef.CornerReliefSize.Value
                        * 0.393701;
                    _txtCornerRelief.Text =
                        Math.Round(cr, 4) + " in";
                }
                catch
                {
                    _txtCornerRelief.Text = "N/A";
                }

                // Material
                try
                {
                    _txtMaterial.Text =
                        part.ComponentDefinition
                        .Material.Name;
                }
                catch { _txtMaterial.Text = "N/A"; }

                // Unfold Rule
                try
                {
                    _txtUnfoldRule.Text =
                        smDef.UnfoldMethod
                        .ToString();
                }
                catch
                {
                    _txtUnfoldRule.Text = "N/A";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "RefreshValues: " + ex.Message);
            }
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
                        "Active document is not " +
                        "a Sheet Metal part.");
                    return;
                }

                SheetMetalComponentDefinition smDef =
                    (SheetMetalComponentDefinition)
                    part.ComponentDefinition;

                // Apply thickness
                try
                {
                    string tStr =
                        _txtThickness.Text
                        .Replace(" in", "").Trim();
                    if (double.TryParse(
                        tStr, out double t))
                        smDef.Thickness.Value =
                            t / 0.393701;
                }
                catch { }

                // Apply bend radius
                try
                {
                    string brStr =
                        _txtBendRadius.Text
                        .Replace(" in", "").Trim();
                    if (double.TryParse(
                        brStr, out double br))
                        smDef.BendRadius.Value =
                            br / 0.393701;
                }
                catch { }

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

        private void ClearValues()
        {
            _txtThickness.Text = "";
            _txtBendRadius.Text = "";
            _txtBendRelief.Text = "";
            _txtCornerRelief.Text = "";
            _txtMaterial.Text = "";
            _txtUnfoldRule.Text = "";
        }
    }
}