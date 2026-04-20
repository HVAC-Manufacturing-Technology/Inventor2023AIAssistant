using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class IPropertiesPanel : UserControl
    {
        private Inventor.Application _app;
        private IPropertiesEditor _editor;
        private Label _lblDoc;
        private TabControl _tabControl;
        private System.Windows.Forms.Timer _refreshTimer;
        private string _lastDocName = "";

        // ── PropertySet GUIDs ─────────────────────────────────────────
        private const string DesignGuid =
            "{32853F0F-3444-11D1-9E93-0060B03C1CA6}";
        private const string SummaryGuid =
            "{F29F85E0-4FF9-1068-AB91-08002B27B3D9}";
        private const string StatusGuid =
            "{9EAC4F1E-BBA4-11D0-8C73-00C04FD5ABBE}";
        private const string UserGuid =
            "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}";

        // ── Per-tab grids ─────────────────────────────────────────────
        private DataGridView _gridGeneral;
        private DataGridView _gridSummary;
        private DataGridView _gridProject;
        private DataGridView _gridStatus;
        private DataGridView _gridCustom;
        private DataGridView _gridPhysical;

        // ── Tab field definitions ─────────────────────────────────────
        private static readonly (string Name, string Guid)[] _generalFields =
        {
            ("Part Number",     DesignGuid),
            ("Description",     DesignGuid),
            ("Revision Number", DesignGuid),
            ("Designer",        DesignGuid),
            ("Engineer",        DesignGuid),
            ("Authority",       DesignGuid),
            ("Cost",            DesignGuid),
            ("Stock Number",    DesignGuid),
            ("Vendor",          DesignGuid),
            ("Web Link",        DesignGuid),
        };

        private static readonly (string Name, string Guid)[] _summaryFields =
        {
            ("Title",    SummaryGuid),
            ("Subject",  SummaryGuid),
            ("Author",   SummaryGuid),
            ("Manager",  SummaryGuid),
            ("Company",  SummaryGuid),
            ("Category", SummaryGuid),
            ("Keywords", SummaryGuid),
            ("Comments", SummaryGuid),
        };

        private static readonly (string Name, string Guid)[] _projectFields =
        {
            ("Part Number",     DesignGuid),
            ("Project",         DesignGuid),
            ("Designer",        DesignGuid),
            ("Engineer",        DesignGuid),
            ("Authority",       DesignGuid),
            ("Cost",            DesignGuid),
            ("Stock Number",    DesignGuid),
            ("Vendor",          DesignGuid),
            ("Web Link",        DesignGuid),
            ("Creation Time",   DesignGuid),
        };

        private static readonly (string Name, string Guid)[] _statusFields =
        {
            ("Design State",        StatusGuid),
            ("Checked By",          StatusGuid),
            ("Check Date",          StatusGuid),
            ("Eng Approved By",     StatusGuid),
            ("Eng Approved Date",   StatusGuid),
            ("Mfg Approved By",     StatusGuid),
            ("Mfg Approved Date",   StatusGuid),
        };

        // ── Constructor ───────────────────────────────────────────────
        public IPropertiesPanel(
            Inventor.Application app,
            IPropertiesEditor editor)
        {
            _app = app;
            _editor = editor;
            BuildUI();
            LoadProperties();

            _refreshTimer =
                new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 2000;
            _refreshTimer.Tick +=
                (s, e) => RefreshIfChanged();
            _refreshTimer.Start();
        }

        // ── UI Build ──────────────────────────────────────────────────
        private void BuildUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor =
                System.Drawing.Color.FromArgb(45, 45, 48);

            // ── Document label ────────────────────────────────────────
            _lblDoc = new Label();
            _lblDoc.Dock = DockStyle.Top;
            _lblDoc.Height = 24;
            _lblDoc.ForeColor =
                System.Drawing.Color.White;
            _lblDoc.BackColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            _lblDoc.Font =
                new System.Drawing.Font(
                    "Segoe UI", 8.5f,
                    System.Drawing.FontStyle.Bold);
            _lblDoc.TextAlign = ContentAlignment.MiddleLeft;
            _lblDoc.Padding = new Padding(6, 0, 0, 0);
            _lblDoc.Text = "No document open";

            // ── Button panel ──────────────────────────────────────────
            var btnPanel = new Panel();
            btnPanel.Dock = DockStyle.Bottom;
            btnPanel.Height = 36;
            btnPanel.BackColor =
                System.Drawing.Color.FromArgb(45, 45, 48);

            var btnSave = new Button();
            btnSave.Text = "\uD83D\uDCBE Save";
            btnSave.Size =
                new System.Drawing.Size(90, 26);
            btnSave.Location =
                new System.Drawing.Point(6, 5);
            btnSave.BackColor =
                System.Drawing.Color.FromArgb(0, 122, 204);
            btnSave.ForeColor =
                System.Drawing.Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Click += BtnSave_Click;

            var btnRefresh = new Button();
            btnRefresh.Text = "\u21BB Refresh";
            btnRefresh.Size =
                new System.Drawing.Size(90, 26);
            btnRefresh.Location =
                new System.Drawing.Point(102, 5);
            btnRefresh.BackColor =
                System.Drawing.Color.FromArgb(63, 63, 70);
            btnRefresh.ForeColor =
                System.Drawing.Color.White;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Click +=
                (s, e) => LoadProperties();

            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnRefresh);

            // ── Tab control ───────────────────────────────────────────
            _tabControl = new TabControl();
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.Font =
                new System.Drawing.Font("Segoe UI", 8.5f);

            _gridGeneral = BuildGrid();
            _gridSummary = BuildGrid();
            _gridProject = BuildGrid();
            _gridStatus = BuildGrid();
            _gridCustom = BuildGrid();
            _gridPhysical = BuildGrid();
            _gridPhysical.ReadOnly = true;

            _tabControl.TabPages.Add(
                BuildTab("General", _gridGeneral));
            _tabControl.TabPages.Add(
                BuildTab("Summary", _gridSummary));
            _tabControl.TabPages.Add(
                BuildTab("Project", _gridProject));
            _tabControl.TabPages.Add(
                BuildTab("Status", _gridStatus));
            _tabControl.TabPages.Add(
                BuildTab("Custom", _gridCustom));
            _tabControl.TabPages.Add(
                BuildTab("Physical", _gridPhysical));

            this.Controls.Add(_tabControl);
            this.Controls.Add(btnPanel);
            this.Controls.Add(_lblDoc);
        }

        // ── Grid factory ──────────────────────────────────────────────
        private DataGridView BuildGrid()
        {
            var grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            grid.GridColor =
                System.Drawing.Color.FromArgb(63, 63, 70);
            grid.BorderStyle = BorderStyle.None;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            grid.Font =
                new System.Drawing.Font("Segoe UI", 9f);

            var colProp =
                new DataGridViewTextBoxColumn();
            colProp.HeaderText = "Property";
            colProp.Name = "Property";
            colProp.AutoSizeMode =
                DataGridViewAutoSizeColumnMode
                    .AllCells;
            colProp.ReadOnly = true;
            colProp.ReadOnly = true;
            colProp.DefaultCellStyle.BackColor =
                System.Drawing.Color.FromArgb(37, 37, 38);
            colProp.DefaultCellStyle.ForeColor =
                System.Drawing.Color.FromArgb(
                    180, 180, 180);

            var colVal =
                new DataGridViewTextBoxColumn();
            colVal.HeaderText = "Value";
            colVal.Name = "Value";
            colVal.AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;
            colVal.DefaultCellStyle.BackColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            colVal.DefaultCellStyle.ForeColor =
                System.Drawing.Color.White;

            grid.ColumnHeadersDefaultCellStyle
                .BackColor =
                System.Drawing.Color.FromArgb(45, 45, 48);
            grid.ColumnHeadersDefaultCellStyle
                .ForeColor =
                System.Drawing.Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font =
                new System.Drawing.Font(
                    "Segoe UI", 9f,
                    System.Drawing.FontStyle.Bold);
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeight = 26;

            grid.Columns.Add(colProp);
            grid.Columns.Add(colVal);

            grid.RowsDefaultCellStyle.BackColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            grid.RowsDefaultCellStyle.ForeColor =
                System.Drawing.Color.White;
            grid.AlternatingRowsDefaultCellStyle
                .BackColor =
                System.Drawing.Color.FromArgb(37, 37, 38);
            grid.AlternatingRowsDefaultCellStyle
                .ForeColor =
                System.Drawing.Color.White;
            grid.DefaultCellStyle.SelectionBackColor =
                System.Drawing.Color.FromArgb(0, 122, 204);
            grid.DefaultCellStyle.SelectionForeColor =
                System.Drawing.Color.White;

            return grid;
        }

        private TabPage BuildTab(
            string title, DataGridView grid)
        {
            var page = new TabPage(title);
            page.BackColor =
                System.Drawing.Color.FromArgb(45, 45, 48);
            page.Padding = new Padding(0);
            page.Controls.Add(grid);
            return page;
        }

        // ── Refresh ───────────────────────────────────────────────────
        private void RefreshIfChanged()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                string name = doc?.DisplayName ?? "";
                if (name != _lastDocName)
                    LoadProperties();
            }
            catch { }
        }

        // ── Load all tabs ─────────────────────────────────────────────
        public void LoadProperties()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(LoadProperties));
                return;
            }

            try
            {
                ClearAllGrids();

                Document doc = _app.ActiveDocument;
                if (doc == null)
                {
                    _lblDoc.Text = "No document open";
                    _lastDocName = "";
                    return;
                }

                _lastDocName = doc.DisplayName;
                _lblDoc.Text = doc.DisplayName;

                // ── General ───────────────────────────────────────────
                LoadFieldsIntoGrid(
                    _gridGeneral, doc, _generalFields);

                // ── Summary ───────────────────────────────────────────
                LoadFieldsIntoGrid(
                    _gridSummary, doc, _summaryFields);

                // ── Project ───────────────────────────────────────────
                LoadFieldsIntoGrid(
                    _gridProject, doc, _projectFields);

                // ── Status ────────────────────────────────────────────
                LoadFieldsIntoGrid(
                    _gridStatus, doc, _statusFields);

                // ── Custom (user-defined properties) ──────────────────
                try
                {
                    PropertySet customPs =
                        doc.PropertySets[UserGuid];
                    foreach (Property p in customPs)
                    {
                        try
                        {
                            string val =
                                Convert.ToString(
                                    p.Value);
                            _gridCustom.Rows.Add(
                                p.Name, val);
                        }
                        catch { }
                    }
                    if (_gridCustom.Rows.Count == 0)
                        _gridCustom.Rows.Add(
                            "(none)", "");
                }
                catch
                {
                    _gridCustom.Rows.Add(
                        "(none)", "");
                }

                // ── Physical ──────────────────────────────────────────
                LoadPhysical(doc);
            }
            catch (Exception ex)
            {
                _lblDoc.Text = "Error: " + ex.Message;
            }
        }

        private void LoadFieldsIntoGrid(
            DataGridView grid,
            Document doc,
            (string Name, string Guid)[] fields)
        {
            foreach (var field in fields)
            {
                string val = "";
                try
                {
                    val = Convert.ToString(
                        doc.PropertySets[field.Guid]
                           [field.Name].Value);
                }
                catch { }
                grid.Rows.Add(field.Name, val);
            }
        }

        private void LoadPhysical(Document doc)
        {
            try
            {
                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    PartDocument part =
                        (PartDocument)doc;
                    MassProperties mp =
                        part.ComponentDefinition
                            .MassProperties;

                    string mat =
                        part.ComponentDefinition
                            .Material.Name;

                    _gridPhysical.Rows.Add(
                        "Material", mat);
                    _gridPhysical.Rows.Add(
                        "Mass",
                        Math.Round(
                            mp.Mass * 2.20462, 4)
                        + " lbs");
                    _gridPhysical.Rows.Add(
                        "Volume",
                        Math.Round(
                            mp.Volume * 61.0237, 4)
                        + " in\u00b3");
                    // ✅ Replace with — calculate from mass/volume:
                    double densityLbsIn3 = 0;
                    try
                    {
                        densityLbsIn3 =
                            (mp.Mass * 2.20462) /
                            (mp.Volume * 61.0237);
                    }
                    catch { }
                    _gridPhysical.Rows.Add(
                        "Density",
                        Math.Round(densityLbsIn3, 6)
                        + " lbs/in\u00b3");
                    _gridPhysical.Rows.Add(
                        "Area",
                        Math.Round(
                            mp.Area * 1550.0031, 4)
                        + " in\u00b2");
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                {
                    AssemblyDocument asm =
                        (AssemblyDocument)doc;
                    MassProperties mp =
                        asm.ComponentDefinition
                           .MassProperties;

                    _gridPhysical.Rows.Add(
                        "Mass",
                        Math.Round(
                            mp.Mass * 2.20462, 4)
                        + " lbs");
                    _gridPhysical.Rows.Add(
                        "Volume",
                        Math.Round(
                            mp.Volume * 61.0237, 4)
                        + " in\u00b3");
                    _gridPhysical.Rows.Add(
                        "Area",
                        Math.Round(
                            mp.Area * 1550.0031, 4)
                        + " in\u00b2");
                }
                else
                {
                    _gridPhysical.Rows.Add(
                        "(not available)", "");
                }
            }
            catch (Exception ex)
            {
                _gridPhysical.Rows.Add(
                    "Error", ex.Message);
            }
        }

        private void ClearAllGrids()
        {
            _gridGeneral.Rows.Clear();
            _gridSummary.Rows.Clear();
            _gridProject.Rows.Clear();
            _gridStatus.Rows.Clear();
            _gridCustom.Rows.Clear();
            _gridPhysical.Rows.Clear();
        }

        // ── Save ──────────────────────────────────────────────────────
        private void BtnSave_Click(
            object sender, EventArgs e)
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null)
                {
                    MessageBox.Show(
                        "No active document.",
                        "iProperties",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                int saved = 0;

                // Save General + Summary + Project
                // + Status via editor
                saved += SaveGrid(_gridGeneral, doc);
                saved += SaveGrid(_gridSummary, doc);
                saved += SaveGrid(_gridProject, doc);
                saved += SaveGrid(_gridStatus, doc);

                // Save Custom properties directly
                try
                {
                    PropertySet customPs =
                        doc.PropertySets[UserGuid];
                    foreach (DataGridViewRow row in
                        _gridCustom.Rows)
                    {
                        try
                        {
                            string name =
                                row.Cells["Property"]
                                   .Value?.ToString();
                            string val =
                                row.Cells["Value"]
                                   .Value?.ToString()
                                ?? "";
                            if (string
                                .IsNullOrWhiteSpace(name)
                                || name == "(none)")
                                continue;
                            customPs[name].Value = val;
                            saved++;
                        }
                        catch { }
                    }
                }
                catch { }

                doc.Save();
                _lblDoc.Text =
                    "\u2705 Saved " + saved +
                    " properties \u2014 " +
                    doc.DisplayName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Save failed: " + ex.Message,
                    "iProperties",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private int SaveGrid(
            DataGridView grid, Document doc)
        {
            int saved = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                try
                {
                    string propName =
                        row.Cells["Property"]
                           .Value?.ToString();
                    string propVal =
                        row.Cells["Value"]
                           .Value?.ToString() ?? "";

                    if (string.IsNullOrWhiteSpace(
                            propName)) continue;

                    string result =
                        _editor.SetPropertyByName(
                            propName, propVal);

                    if (result != null &&
                        result.StartsWith("\u2705"))
                        saved++;
                }
                catch { }
            }
            return saved;
        }

        // ── Cleanup ───────────────────────────────────────────────────
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}