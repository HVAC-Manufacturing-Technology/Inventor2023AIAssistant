using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class AssistantPanel : UserControl
    {
        private Inventor.Application _inventorApplication;
        private AiChatClient _aiClient;
        private List<ChatMessage> _conversationHistory;
        private bool _aiAvailable = false;
        private System.Windows.Forms.Timer _statusCheckTimer;
        private WriteActionsHandler _writeActions;
        private ActionExecutor _actionExecutor;
        private ILogicGenerator _iLogicGenerator;
        private BomExporter _bomExporter;
        private VaultSearchHandler _vaultSearch;
        private SheetMetalHandler _sheetMetal;
        private ChatHistoryManager _chatHistory;
        private DrawingAutomation _drawingAuto;
        private GeometryAnalysis _geometryAnalysis;
        private ParameterValidator _paramValidator;
        private AssemblyHealthCheck _healthCheck;
        private HolePatternCalculator _holePattern;
        private PartNumberManager _partNumberMgr;
        private SketchAnalyzer _sketchAnalyzer;
        private FeatureErrorDetector _featureErrors;
        private HoleThreadReader _holeThread;
        private SelectionEngine _selectionEngine;
        private InterferenceDetector _interferenceDetector;
        private ConstraintAnalyzer _constraintAnalyzer;
        private ComponentVisibilityHandler _visibilityHandler;
        private IPropertiesEditor _iPropertiesEditor;
        private AssemblyStructureAnalyzer _structureAnalyzer;
        private ViewCommandHandler _viewCommands;
        private List<string> _savedPrompts;
        private List<string> _promptHistory;
        private int _promptHistoryIndex = -1;
        private bool _isStreaming = false;

        private Label _lblStatus;
        private Button _btnNewChat;
        private TabControl _tabControl;
        private TabPage _tabChat;
        private TabPage _tabSaved;
        private TabPage _tabLibrary;
        private System.Windows.Forms.TextBox _txtOutput;
        private System.Windows.Forms.TextBox _txtPrompt;
        private Button _btnSend;
        private ListBox _lstSavedPrompts;
        private Button _btnDeletePrompt;
        private Button _btnRunPrompt;
        private ListBox _lstLibrary;
        private Button _btnRunLibrary;

        public AssistantPanel(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
            _aiClient = new AiChatClient();
            _writeActions = new WriteActionsHandler(
                _inventorApplication);
            _actionExecutor = new ActionExecutor(
                _inventorApplication);
            _iLogicGenerator = new ILogicGenerator(
                _inventorApplication);
            _bomExporter = new BomExporter(
                _inventorApplication);
            _vaultSearch = new VaultSearchHandler(
                _inventorApplication);
            _sheetMetal = new SheetMetalHandler(
                _inventorApplication);
            _chatHistory = new ChatHistoryManager();
            _drawingAuto = new DrawingAutomation(
                _inventorApplication);
            _geometryAnalysis = new GeometryAnalysis(
                _inventorApplication);
            _paramValidator = new ParameterValidator(
                _inventorApplication);
            _healthCheck = new AssemblyHealthCheck(
                _inventorApplication);
            _holePattern = new HolePatternCalculator(
                _inventorApplication);
            _partNumberMgr = new PartNumberManager(
                _inventorApplication);
            _sketchAnalyzer = new SketchAnalyzer(
                _inventorApplication);
            _featureErrors = new FeatureErrorDetector(
                _inventorApplication);
            _holeThread = new HoleThreadReader(
                _inventorApplication);
            _selectionEngine = new SelectionEngine(
                _inventorApplication);
            _interferenceDetector =
                new InterferenceDetector(
                    _inventorApplication);
            _constraintAnalyzer = new ConstraintAnalyzer(
                _inventorApplication);
            _visibilityHandler =
                new ComponentVisibilityHandler(
                    _inventorApplication);
            _iPropertiesEditor =
                new IPropertiesEditor(
                    _inventorApplication);
            _structureAnalyzer =
                new AssemblyStructureAnalyzer(
                    _inventorApplication);
            _viewCommands =
                new ViewCommandHandler(
                    _inventorApplication);
            _conversationHistory = new List<ChatMessage>();
            _savedPrompts = new List<string>();
            _promptHistory = new List<string>();

            BuildUI();
            InitializeConversation();
            StartStatusChecker();
        }

        private void BuildUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = System.Drawing.Color.FromArgb(
                45, 45, 48);

            _lblStatus = new Label();
            _lblStatus.Location =
                new System.Drawing.Point(8, 5);
            _lblStatus.Size =
                new System.Drawing.Size(240, 18);
            _lblStatus.Font = new Font(
                "Segoe UI", 8.25f, FontStyle.Bold);
            _lblStatus.ForeColor =
                System.Drawing.Color.Gray;
            _lblStatus.Text = "Checking AI connection...";
            _lblStatus.BackColor =
                System.Drawing.Color.Transparent;

            _btnNewChat = new Button();
            _btnNewChat.Text = "New Chat";
            _btnNewChat.Size =
                new System.Drawing.Size(85, 22);
            _btnNewChat.TabStop = false;
            _btnNewChat.BackColor =
                System.Drawing.Color.FromArgb(63, 63, 70);
            _btnNewChat.ForeColor =
                System.Drawing.Color.White;
            _btnNewChat.FlatStyle = FlatStyle.Flat;
            _btnNewChat.Click += BtnNewChat_Click;

            _tabControl = new TabControl();
            _tabControl.Font = new Font("Segoe UI", 9f);

            _tabChat = new TabPage("Chat");
            _tabChat.BackColor =
                System.Drawing.Color.FromArgb(45, 45, 48);

            _tabSaved = new TabPage("Saved");
            _tabSaved.BackColor =
                System.Drawing.Color.FromArgb(45, 45, 48);

            _tabLibrary = new TabPage("Library");
            _tabLibrary.BackColor =
                System.Drawing.Color.FromArgb(45, 45, 48);

            BuildChatTab();
            BuildSavedTab();
            BuildLibraryTab();

            _tabControl.TabPages.Add(_tabChat);
            _tabControl.TabPages.Add(_tabSaved);
            _tabControl.TabPages.Add(_tabLibrary);

            this.Controls.Add(_lblStatus);
            this.Controls.Add(_btnNewChat);
            this.Controls.Add(_tabControl);

            this.Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private void BuildChatTab()
        {
            _txtOutput = new System.Windows.Forms.TextBox();
            _txtOutput.Multiline = true;
            _txtOutput.ReadOnly = true;
            _txtOutput.ScrollBars = ScrollBars.Vertical;
            _txtOutput.BackColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            _txtOutput.ForeColor =
                System.Drawing.Color.White;
            _txtOutput.BorderStyle = BorderStyle.None;
            _txtOutput.Font = new Font("Consolas", 9f);

            _txtPrompt = new System.Windows.Forms.TextBox();
            _txtPrompt.Multiline = true;
            _txtPrompt.AcceptsReturn = true;
            _txtPrompt.AcceptsTab = false;
            _txtPrompt.ScrollBars = ScrollBars.Vertical;
            _txtPrompt.BackColor =
                System.Drawing.Color.FromArgb(37, 37, 38);
            _txtPrompt.ForeColor =
                System.Drawing.Color.White;
            _txtPrompt.Font = new Font("Segoe UI", 9.5f);
            _txtPrompt.KeyDown += TxtPrompt_KeyDown;

            _btnSend = new Button();
            _btnSend.Text = "Send";
            _btnSend.TabStop = false;
            _btnSend.BackColor =
                System.Drawing.Color.FromArgb(0, 122, 204);
            _btnSend.ForeColor =
                System.Drawing.Color.White;
            _btnSend.FlatStyle = FlatStyle.Flat;
            _btnSend.Click +=
                (s, e) => _ = SendPromptAsync();

            _tabChat.Controls.Add(_txtOutput);
            _tabChat.Controls.Add(_txtPrompt);
            _tabChat.Controls.Add(_btnSend);
            _tabChat.Resize += (s, e) => LayoutChatTab();
        }

        private void BuildSavedTab()
        {
            _lstSavedPrompts = new ListBox();
            _lstSavedPrompts.BackColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            _lstSavedPrompts.ForeColor =
                System.Drawing.Color.White;
            _lstSavedPrompts.Font =
                new Font("Segoe UI", 9f);
            _lstSavedPrompts.BorderStyle =
                BorderStyle.None;
            _lstSavedPrompts.DoubleClick +=
                (s, e) => RunSavedPrompt();

            _btnRunPrompt = new Button();
            _btnRunPrompt.Text = "\u25b6 Run";
            _btnRunPrompt.TabStop = false;
            _btnRunPrompt.BackColor =
                System.Drawing.Color.FromArgb(0, 122, 204);
            _btnRunPrompt.ForeColor =
                System.Drawing.Color.White;
            _btnRunPrompt.FlatStyle = FlatStyle.Flat;
            _btnRunPrompt.Click +=
                (s, e) => RunSavedPrompt();

            _btnDeletePrompt = new Button();
            _btnDeletePrompt.Text = "\u2715 Delete";
            _btnDeletePrompt.TabStop = false;
            _btnDeletePrompt.BackColor =
                System.Drawing.Color.FromArgb(63, 63, 70);
            _btnDeletePrompt.ForeColor =
                System.Drawing.Color.White;
            _btnDeletePrompt.FlatStyle = FlatStyle.Flat;
            _btnDeletePrompt.Click += (s, e) =>
            {
                if (_lstSavedPrompts.SelectedIndex < 0)
                    return;
                int idx = _lstSavedPrompts.SelectedIndex;
                _savedPrompts.RemoveAt(idx);
                _lstSavedPrompts.Items.RemoveAt(idx);
            };

            _tabSaved.Controls.Add(_lstSavedPrompts);
            _tabSaved.Controls.Add(_btnRunPrompt);
            _tabSaved.Controls.Add(_btnDeletePrompt);
            _tabSaved.Resize += (s, e) => LayoutSavedTab();
        }

        private void BuildLibraryTab()
        {
            _lstLibrary = new ListBox();
            _lstLibrary.BackColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            _lstLibrary.ForeColor =
                System.Drawing.Color.White;
            _lstLibrary.Font = new Font("Segoe UI", 9f);
            _lstLibrary.BorderStyle = BorderStyle.None;
            _lstLibrary.DoubleClick +=
                (s, e) => RunLibraryPrompt();

            string[] items = new string[]
            {
                // ── Read ──────────────────────────────────
                "what file is open",
                "list parameters",
                "show material",
                "show iproperties",
                "what is selected",
                "list features",
                "list components",
                "update mass",
                "check interference",
                "inspect",

                // ── iProperties panel ─────────────────────
                "open iproperties panel",
                "show iproperties panel",
                "dock iproperties panel",
                "display iproperties",

                // ── iProperties Editor ────────────────────
                "set part number to SAV-24R",
                "set description to Fan Coil Unit",
                "set revision to B",
                "set designer to Blake Conner",
                "set material to Galvanized Steel",
                "set company to HVAC Manufacturing",
                "set project to Job-1234",
                "clear part number",
                "clear description",

                // ── Assembly Structure ────────────────────
                "assembly structure",
                "component hierarchy",
                "assembly tree",
                "count components",
                "how many components",
                "list sub assemblies",
                "assembly summary",

                // ── View Commands ─────────────────────────
                "zoom to fit",
                "home view",
                "isometric view",
                "top view",
                "front view",
                "right view",
                "shaded view",
                "shaded with edges",
                "wireframe",
                "zoom in",
                "zoom out",
                "previous view",
                "orbit view",

                // ── Parameters ────────────────────────────
                "set Width to 24",
                "set Height to 12",
                "set Thickness to 0.040",
                "set Length to 48",
                "set BendRadius to 0.125",

                // ── Sketches ──────────────────────────────
                "create sketch on front plane",
                "create sketch on top plane",
                "create sketch on side plane",
                "finish sketch",

                // ── Geometry ──────────────────────────────
                "draw circle at origin radius 1",
                "draw rectangle 4 2",
                "draw line from 0 0 to 4 0",

                // ── Features ──────────────────────────────
                "extrude 0.125",
                "extrude 0.25",
                "extrude 0.5",
                "extrude 1",
                "fillet radius 0.125",
                "chamfer distance 0.125",

                // ── Chained actions ───────────────────────
                "create a 24x12 plate 0.034 thick",
                "create a 24x12 plate 0.040 thick",
                "create a 48x24 plate 0.052 thick",
                "create a cylinder radius 1 height 4",

                // ── Selection ─────────────────────────────
                "select all holes smaller than 0.25",
                "select all holes smaller than 0.375",
                "select all faces parallel to xy plane",
                "select all faces parallel to xz plane",
                "select all faces parallel to yz plane",
                "select all edges longer than 2",
                "select all fillets",
                "select all sheet metal bends",
                "select suppressed features",
                "show selection",
                "deselect all",

                // ── Visibility ────────────────────────────
                "show all",
                "hide all",
                "show all components",
                "suppress all",
                "unsuppress all",
                "list hidden components",
                "list suppressed components",
                "hide coil assembly",
                "show coil assembly",
                "suppress fasteners",
                "unsuppress fasteners",
                "toggle coil assembly",

                // ── Interference ──────────────────────────
                "run interference check",
                "check clearance",
                "minimum clearance",
                "assembly mass",

                // ── Constraints ───────────────────────────
                "assembly constraint analysis",
                "check assembly constraints",
                "find unconstrained components",
                "find over constrained components",
                "list constraints",
                "find grounded components",
                "degrees of freedom",

                // ── Sketch analysis ───────────────────────
                "sketch status",
                "is this sketch fully constrained",
                "find unconstrained geometry",
                "sketch summary",
                "list sketch dimensions",
                "list all sketches",
                "active sketch",
                "how many lines in this sketch",
                "how many circles in this sketch",

                // ── Feature errors ────────────────────────
                "show feature errors",
                "feature health report",
                "find suppressed features",
                "show feature warnings",
                "list features",

                // ── Hole and thread reader ─────────────────
                "hole report",
                "all threaded holes",
                "what size thread is this hole",
                "tap drill 1/4-20",
                "tap drill 3/8-16",
                "tap drill 1/2-13",
                "tap drill table",
                "selected hole info",

                // ── Hole pattern ──────────────────────────
                "calculate hole pattern 4 holes on 8 inch bolt circle",
                "calculate hole pattern 6 holes on 6 inch bolt circle",
                "rivet spacing for 24 inch panel",
                "rivet spacing for 48 inch panel",
                "flange bolt pattern for 12x12 duct",
                "flange bolt pattern for 24x24 duct",
                "flange bolt pattern for 48x24 duct",
                "6 holes across 20 inches",
                "8 holes across 36 inches",
                "corner bolt pattern 24x12",
                "smacna pattern 0.040",
                "smacna pattern 0.034",

                // ── Part numbers ──────────────────────────
                "next part number FPB",
                "next part number SAV",
                "next part number PANEL",
                "assign next part number FPB",
                "assign next part number SAV",
                "last used part number",
                "part number registry",
                "open part number folder",

                // ── Geometry analysis ─────────────────────
                "geometry report",
                "surface area",
                "volume",
                "bounding box",
                "count holes",
                "wall thickness",
                "largest face",
                "center of mass",
                "mass",

                // ── Validation ────────────────────────────
                "validate parameters",
                "check part number is set",
                "check description is set",
                "check material is set",

                // ── Health check ──────────────────────────
                "assembly health check",
                "find suppressed components",
                "find hidden components",
                "find missing part numbers",
                "find missing materials",
                "duplicate part numbers",
                "list all open documents",

                // ── Drawing ───────────────────────────────
                "new drawing",
                "add all views",
                "add front view",
                "add top view",
                "add right view",
                "add isometric view",
                "add dimensions",
                "update title block",
                "add border",
                "add sheet",
                "drawing info",
                "set scale 1:2",
                "export drawing pdf",
                "export drawing dwg",
                "delete all views",

                // ── Sheet metal ───────────────────────────
                "create sheet metal part",
                "show sheet metal settings",
                "set thickness 0.034",
                "set thickness 0.040",
                "set thickness 0.052",
                "set gauge 22",
                "set gauge 20",
                "set gauge 18",
                "set bend radius 0.125",
                "set k-factor 0.44",
                "create flat pattern",
                "gauge table",

                // ── BOM ───────────────────────────────────
                "show bill of materials",
                "export bom to excel",
                "export bom to csv",
                "list unique parts",
                "count fasteners",

                // ── Vault ─────────────────────────────────
                "search vault",
                "show checked out files",
                "show recent vault files",
                "open vault explorer",

                // ── Write ─────────────────────────────────
                "set description HVAC Panel",
                "rename part number FPB-001",
                "create model state ALT-1",
                "export dxf",
                "export pdf",
                "save",

                // ── iLogic ────────────────────────────────
                "generate ilogic rule for thickness",
                "generate ilogic rule for material",
                "generate ilogic rule for part number",
                "generate ilogic rule to suppress small holes",
                "generate ilogic rule for mass check",
                "generate ilogic rule to export dxf",
                "generate ilogic rule to validate parameters",

                // ── Chat history ──────────────────────────
                "show chat history sessions",
                "open chat history folder",

                // ── HVAC AI ───────────────────────────────
                "what gauge is standard for a fan coil casing",
                "what is the standard duct flange bolt pattern",
                "what are OSHPD seismic requirements for HVAC",
                "what is the difference between FPB and SAV",
                "what sheet metal thickness for 48 inch wide unit",
                "help",
            };

            foreach (string p in items)
                _lstLibrary.Items.Add(p);

            _btnRunLibrary = new Button();
            _btnRunLibrary.Text = "\u25b6 Run";
            _btnRunLibrary.TabStop = false;
            _btnRunLibrary.BackColor =
                System.Drawing.Color.FromArgb(0, 122, 204);
            _btnRunLibrary.ForeColor =
                System.Drawing.Color.White;
            _btnRunLibrary.FlatStyle = FlatStyle.Flat;
            _btnRunLibrary.Click +=
                (s, e) => RunLibraryPrompt();

            _tabLibrary.Controls.Add(_lstLibrary);
            _tabLibrary.Controls.Add(_btnRunLibrary);
            _tabLibrary.Resize +=
                (s, e) => LayoutLibraryTab();
        }

        private void LayoutControls()
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            int pad = 8;

            _lblStatus.Location =
                new System.Drawing.Point(pad, 5);
            _lblStatus.Width = w - 100 - pad;
            _btnNewChat.Location =
                new System.Drawing.Point(w - 93, 3);

            int tabTop = 30;
            int tabHeight = h - tabTop - pad;
            if (tabHeight < 100) tabHeight = 100;

            _tabControl.Location =
                new System.Drawing.Point(0, tabTop);
            _tabControl.Size =
                new System.Drawing.Size(w, tabHeight);

            LayoutChatTab();
            LayoutSavedTab();
            LayoutLibraryTab();
        }

        private void LayoutChatTab()
        {
            int w = _tabChat.ClientSize.Width;
            int h = _tabChat.ClientSize.Height;
            int pad = 6;
            int promptHeight = 70;
            int sendHeight = 28;
            int outputHeight = h - promptHeight -
                               sendHeight - (pad * 3);
            if (outputHeight < 40) outputHeight = 40;

            _txtOutput.Location =
                new System.Drawing.Point(pad, pad);
            _txtOutput.Size =
                new System.Drawing.Size(
                    w - (pad * 2), outputHeight);

            int promptTop = pad + outputHeight + pad;
            _txtPrompt.Location =
                new System.Drawing.Point(pad, promptTop);
            _txtPrompt.Size =
                new System.Drawing.Size(
                    w - (pad * 2), promptHeight);

            int sendTop = promptTop + promptHeight + pad;
            _btnSend.Location =
                new System.Drawing.Point(
                    w - 70 - pad, sendTop);
            _btnSend.Size =
                new System.Drawing.Size(70, sendHeight);
        }

        private void LayoutSavedTab()
        {
            int w = _tabSaved.ClientSize.Width;
            int h = _tabSaved.ClientSize.Height;
            int pad = 6;
            int btnHeight = 28;

            _lstSavedPrompts.Location =
                new System.Drawing.Point(pad, pad);
            _lstSavedPrompts.Size =
                new System.Drawing.Size(
                    w - (pad * 2),
                    h - btnHeight - (pad * 3));

            int btnTop = h - btnHeight - pad;
            _btnRunPrompt.Location =
                new System.Drawing.Point(pad, btnTop);
            _btnRunPrompt.Size =
                new System.Drawing.Size(80, btnHeight);
            _btnDeletePrompt.Location =
                new System.Drawing.Point(pad + 85, btnTop);
            _btnDeletePrompt.Size =
                new System.Drawing.Size(80, btnHeight);
        }

        private void LayoutLibraryTab()
        {
            int w = _tabLibrary.ClientSize.Width;
            int h = _tabLibrary.ClientSize.Height;
            int pad = 6;
            int btnHeight = 28;

            _lstLibrary.Location =
                new System.Drawing.Point(pad, pad);
            _lstLibrary.Size =
                new System.Drawing.Size(
                    w - (pad * 2),
                    h - btnHeight - (pad * 3));

            int btnTop = h - btnHeight - pad;
            _btnRunLibrary.Location =
                new System.Drawing.Point(pad, btnTop);
            _btnRunLibrary.Size =
                new System.Drawing.Size(80, btnHeight);
        }

        private void TxtPrompt_KeyDown(
            object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (_promptHistory.Count == 0) return;
                if (_promptHistoryIndex == -1)
                    _promptHistoryIndex =
                        _promptHistory.Count - 1;
                else if (_promptHistoryIndex > 0)
                    _promptHistoryIndex--;
                _txtPrompt.Text =
                    _promptHistory[_promptHistoryIndex];
                _txtPrompt.SelectionStart =
                    _txtPrompt.TextLength;
                return;
            }

            if (e.KeyCode == Keys.Down)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (_promptHistory.Count == 0) return;
                if (_promptHistoryIndex == -1 ||
                    _promptHistoryIndex >=
                    _promptHistory.Count - 1)
                {
                    _promptHistoryIndex = -1;
                    _txtPrompt.Clear();
                    return;
                }
                _promptHistoryIndex++;
                _txtPrompt.Text =
                    _promptHistory[_promptHistoryIndex];
                _txtPrompt.SelectionStart =
                    _txtPrompt.TextLength;
                return;
            }

            if (e.KeyCode == Keys.Enter && e.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                int pos = _txtPrompt.SelectionStart;
                int len = _txtPrompt.SelectionLength;
                string t = _txtPrompt.Text ?? string.Empty;
                if (len > 0) t = t.Remove(pos, len);
                t = t.Insert(pos,
                    System.Environment.NewLine);
                _txtPrompt.Text = t;
                _txtPrompt.SelectionStart =
                    pos + System.Environment.NewLine.Length;
                _txtPrompt.SelectionLength = 0;
                return;
            }

            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                this.BeginInvoke(
                    new Action(() => _ = SendPromptAsync()));
            }
        }

        private void RunLibraryPrompt()
        {
            if (_lstLibrary.SelectedIndex < 0) return;
            string s =
                _lstLibrary.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(s)) return;
            _tabControl.SelectedTab = _tabChat;
            _txtPrompt.Text = s;
            _ = SendPromptAsync();
        }

        private void SavePrompt(string prompt)
        {
            string t = prompt.Trim();
            if (string.IsNullOrWhiteSpace(t)) return;
            _promptHistory.Add(t);
            _promptHistoryIndex = -1;
            if (_savedPrompts.Contains(t)) return;
            _savedPrompts.Add(t);
            _lstSavedPrompts.Items.Add(t);
        }

        private void RunSavedPrompt()
        {
            if (_lstSavedPrompts.SelectedIndex < 0) return;
            string s =
                _lstSavedPrompts.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(s)) return;
            _tabControl.SelectedTab = _tabChat;
            _txtPrompt.Text = s;
            _ = SendPromptAsync();
        }

        private async void BtnNewChat_Click(
            object sender, EventArgs e)
        {
            if (!_aiAvailable)
            {
                await RunConnectionDiagnosticAsync();
                return;
            }
            NewChat();
        }

        private void NewChat()
        {
            _chatHistory.LogNewChat();
            InitializeConversation();
            _txtOutput.Clear();
            _txtPrompt.Clear();
            FocusPrompt();
            _txtOutput.AppendText(
                "\u2500\u2500 New Chat Started \u2500\u2500" +
                System.Environment.NewLine +
                System.Environment.NewLine);
        }

        private async Task RunConnectionDiagnosticAsync()
        {
            AppendOutput(
                "\u2500\u2500 Connection Diagnostic \u2500\u2500" +
                System.Environment.NewLine);

            string key =
                System.Environment.GetEnvironmentVariable(
                    "GROQ_API_KEY");

            if (string.IsNullOrWhiteSpace(key))
            {
                AppendOutput(
                    "\u274c GROQ_API_KEY is NOT set." +
                    System.Environment.NewLine +
                    "   Fix: set it in sysdm.cpl > " +
                    "Advanced > Environment Variables, " +
                    "then restart Inventor." +
                    System.Environment.NewLine +
                    System.Environment.NewLine);
                return;
            }

            AppendOutput(
                "\u2705 GROQ_API_KEY found: " +
                key.Substring(0, 8) + "..." +
                System.Environment.NewLine);

            bool configured = _aiClient.IsConfigured();

            AppendOutput(
                (configured ? "\u2705" : "\u274c") +
                " IsConfigured() = " + configured +
                System.Environment.NewLine);

            if (!configured)
            {
                AppendOutput(
                    "   Fix: rebuild after verifying " +
                    "AiChatClient.cs." +
                    System.Environment.NewLine +
                    System.Environment.NewLine);
                return;
            }

            AppendOutput(
                "\u23f3 Sending test request to Groq..." +
                System.Environment.NewLine);

            bool connected = false;
            string testResponse = "";

            try
            {
                var testMessages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        role = "user",
                        content =
                            "Reply with the single " +
                            "word: connected"
                    }
                };

                testResponse =
                    await _aiClient.GetResponseAsync(
                        testMessages);

                connected =
                    !string.IsNullOrWhiteSpace(
                        testResponse) &&
                    !testResponse.StartsWith(
                        "Groq request failed") &&
                    !testResponse.StartsWith(
                        "Groq is not configured");
            }
            catch (Exception ex)
            {
                testResponse = "Exception: " + ex.Message;
            }

            if (connected)
            {
                AppendOutput(
                    "\u2705 Groq responded: " +
                    testResponse +
                    System.Environment.NewLine +
                    "\u2705 Connection is working." +
                    System.Environment.NewLine +
                    System.Environment.NewLine);
                _aiAvailable = true;
                SetStatus(true);
            }
            else
            {
                AppendOutput(
                    "\u274c Groq request failed." +
                    System.Environment.NewLine +
                    "   Response: " + testResponse +
                    System.Environment.NewLine +
                    System.Environment.NewLine);
            }
        }

        private void InitializeConversation()
        {
            _conversationHistory.Clear();
            _conversationHistory.Add(new ChatMessage
            {
                role = "system",
                content =
                    "You are an AI assistant embedded inside " +
                    "Autodesk Inventor 2023, running at an HVAC " +
                    "manufacturing company in Athens, Texas. " +
                    "You are an expert in HVAC terminal units " +
                    "(FPB, SAV, ERU, DDU), sheet metal design, " +
                    "Autodesk Inventor, iLogic, Vault, " +
                    "OSHPD seismic compliance, SMACNA, ASHRAE. " +
                    "Standard gauges: 22ga=0.034in, " +
                    "20ga=0.040in, 18ga=0.052in, 16ga=0.060in. " +
                    "Always use imperial units. " +
                    "Be concise and practical."
            });
        }

        private void StartStatusChecker()
        {
            SetStatus(false);
            _statusCheckTimer =
                new System.Windows.Forms.Timer();
            _statusCheckTimer.Interval = 5000;
            _statusCheckTimer.Tick += async (s, e) =>
                await CheckAiStatusAsync();
            _statusCheckTimer.Start();
            _ = CheckAiStatusAsync();
        }

        private async Task CheckAiStatusAsync()
        {
            bool available =
                await _aiClient.CheckConnectionAsync();
            if (available != _aiAvailable)
            {
                _aiAvailable = available;
                SetStatus(available);
            }
        }

        private void SetStatus(bool aiAvailable)
        {
            if (_lblStatus.InvokeRequired)
            {
                _lblStatus.Invoke(
                    new Action<bool>(SetStatus),
                    aiAvailable);
                return;
            }
            if (aiAvailable)
            {
                _lblStatus.Text = "\u25cf AI Connected";
                _lblStatus.ForeColor =
                    System.Drawing.Color.LimeGreen;
                _btnNewChat.Text = "New Chat";
            }
            else
            {
                _lblStatus.Text =
                    "\u25cf Running in Local Mode";
                _lblStatus.ForeColor =
                    System.Drawing.Color.OrangeRed;
                _btnNewChat.Text = "Diagnose";
            }
        }

        public void FocusPrompt()
        {
            if (_txtPrompt == null ||
                _txtPrompt.IsDisposed) return;
            if (_txtPrompt.InvokeRequired)
            {
                _txtPrompt.Invoke(
                    new Action(FocusPrompt));
                return;
            }
            _tabControl.SelectedTab = _tabChat;
            _txtPrompt.Focus();
            _txtPrompt.SelectionStart =
                _txtPrompt.TextLength;
            _txtPrompt.SelectionLength = 0;
        }

        public void InsertSpaceFromInventor()
        {
            if (_txtPrompt == null ||
                _txtPrompt.IsDisposed) return;
            if (_tabControl.SelectedTab != _tabChat)
                return;
            if (_txtPrompt.InvokeRequired)
            {
                _txtPrompt.Invoke(
                    new Action(InsertSpaceFromInventor));
                return;
            }
            int start = _txtPrompt.SelectionStart;
            int length = _txtPrompt.SelectionLength;
            string text =
                _txtPrompt.Text ?? string.Empty;
            if (length > 0)
                text = text.Remove(start, length);
            text = text.Insert(start, " ");
            _txtPrompt.Text = text;
            _txtPrompt.SelectionStart = start + 1;
            _txtPrompt.SelectionLength = 0;
        }

        public void SendEnterFromInventor()
        {
            if (_txtPrompt == null ||
                _txtPrompt.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.Invoke(
                    new Action(SendEnterFromInventor));
                return;
            }
            if (_tabControl.SelectedTab != _tabChat)
                return;
            _ = SendPromptAsync();
        }

        public void InsertNewlineFromInventor()
        {
            if (_txtPrompt == null ||
                _txtPrompt.IsDisposed) return;
            if (_tabControl.SelectedTab != _tabChat)
                return;
            if (_txtPrompt.InvokeRequired)
            {
                _txtPrompt.Invoke(
                    new Action(InsertNewlineFromInventor));
                return;
            }
            int start = _txtPrompt.SelectionStart;
            int length = _txtPrompt.SelectionLength;
            string text =
                _txtPrompt.Text ?? string.Empty;
            if (length > 0)
                text = text.Remove(start, length);
            text = text.Insert(
                start, System.Environment.NewLine);
            _txtPrompt.Text = text;
            _txtPrompt.SelectionStart =
                start +
                System.Environment.NewLine.Length;
            _txtPrompt.SelectionLength = 0;
        }

        public void LogKeyPress(int keyASCII) { }

        private string TryHandleParameterModification(
            string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            bool isSet =
                n.StartsWith("set ") ||
                n.StartsWith("change ") ||
                n.StartsWith("update ");

            if (!isSet) return null;

            string[] parts = prompt.Trim().Split(
                new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4) return null;

            int toIndex = -1;
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].ToLower() == "to")
                {
                    toIndex = i;
                    break;
                }
            }

            if (toIndex < 0 ||
                toIndex >= parts.Length - 1)
                return null;

            string paramName = string.Join(
                " ", parts, 1, toIndex - 1);
            string valueStr = parts[toIndex + 1];

            if (string.IsNullOrWhiteSpace(paramName) ||
                string.IsNullOrWhiteSpace(valueStr))
                return null;

            if (!double.TryParse(valueStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo
                    .InvariantCulture,
                out double newValue))
                return null;

            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "No active document is open.";

                Parameters parameters = null;

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                    parameters = ((PartDocument)doc)
                        .ComponentDefinition.Parameters;
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                    parameters = ((AssemblyDocument)doc)
                        .ComponentDefinition.Parameters;
                else
                    return
                        "Parameter editing requires a " +
                        "Part or Assembly document.";

                Parameter found = null;
                foreach (Parameter p in parameters)
                {
                    if (string.Equals(
                        p.Name, paramName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        found = p;
                        break;
                    }
                }

                if (found == null)
                    return "Parameter '" + paramName +
                           "' not found. " +
                           "Type 'list parameters' " +
                           "to see available parameters.";

                double oldVal =
                    (double)found.Value * 0.393701;

                found.Expression = valueStr + " in";

                return "\u2705 Parameter '" +
                       found.Name +
                       "' updated: " +
                       Math.Round(oldVal, 4) +
                       " in \u2192 " +
                       valueStr + " in";
            }
            catch (Exception ex)
            {
                return "Failed to set parameter: " +
                       ex.Message;
            }
        }

        private async Task SendPromptAsync()
        {
            if (_isStreaming) return;
            if (_txtPrompt == null ||
                _txtPrompt.IsDisposed) return;

            string prompt = _txtPrompt.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                FocusPrompt();
                return;
            }

            SavePrompt(prompt);
            _chatHistory.LogUserMessage(prompt);

            AppendOutput(
                "You: " + prompt +
                System.Environment.NewLine);

            _txtPrompt.Clear();
            FocusPrompt();
            _btnSend.Enabled = false;

            try
            {
                string response = null;
                string n =
                    prompt.Trim().ToLowerInvariant();

                if (n == "open chat history folder" ||
                    n == "open history folder")
                {
                    response =
                        _chatHistory.OpenHistoryFolder();
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                if (n == "show chat history sessions" ||
                    n == "list chat history" ||
                    n == "chat history")
                {
                    response =
                        _chatHistory.ListRecentSessions();
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                if (n == "open vault explorer")
                {
                    response =
                        _vaultSearch
                            .LaunchVaultExplorer();
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Parameter modifier
                response =
                    TryHandleParameterModification(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Visibility handler
                response =
                    _visibilityHandler
                        .TryHandleVisibility(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ iProperties editor
                response =
                    _iPropertiesEditor
                        .TryHandleIPropertiesEdit(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Assembly structure
                response =
                    _structureAnalyzer
                        .TryHandleStructure(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ View commands
                response =
                    _viewCommands
                        .TryHandleViewCommand(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Selection engine
                response =
                    _selectionEngine
                        .TryHandleSelection(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Interference detector
                response =
                    _interferenceDetector
                        .TryHandleInterference(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Constraint analyzer
                response =
                    _constraintAnalyzer
                        .TryHandleConstraints(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Sketch analyzer
                response =
                    _sketchAnalyzer
                        .TryHandleSketchAnalysis(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Feature error detector
                response =
                    _featureErrors
                        .TryHandleFeatureErrors(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Hole thread reader
                response =
                    _holeThread.TryHandleHoleThread(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Hole pattern calculator
                response =
                    _holePattern.TryHandleHolePattern(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Part number manager
                response =
                    _partNumberMgr.TryHandlePartNumber(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Geometry analysis
                response =
                    _geometryAnalysis.TryHandleGeometry(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Parameter validation
                response =
                    _paramValidator.TryHandleValidation(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Assembly health check
                response =
                    _healthCheck.TryHandleHealthCheck(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Drawing automation
                response =
                    _drawingAuto.TryHandleDrawing(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Sheet metal
                response =
                    _sheetMetal.TryHandleSheetMetal(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ BOM
                response =
                    _bomExporter.TryHandleBom(prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Vault search
                response =
                    _vaultSearch.TryHandleVaultSearch(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ iLogic
                response =
                    _iLogicGenerator.TryHandleILogic(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Write actions
                response =
                    _writeActions.TryHandleWriteAction(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ Action executor
                response =
                    _actionExecutor.TryExecuteAction(
                        prompt);
                if (response != null)
                {
                    Output(response);
                    _chatHistory.LogAssistantMessage(
                        response);
                    return;
                }

                // ─ AI if connected
                if (_aiAvailable)
                {
                    string ctx =
                        BuildContextualUserPrompt(prompt);
                    _conversationHistory.Add(
                        new ChatMessage
                        {
                            role = "user",
                            content = ctx
                        });
                    await StreamResponseAsync();
                    return;
                }

                // ─ Local fallback
                response =
                    GetLocalFallbackResponse(prompt);
                Output(response);
                _chatHistory.LogAssistantMessage(
                    response);
            }
            catch (Exception ex)
            {
                AppendOutput(
                    "Error: " + ex.Message +
                    System.Environment.NewLine +
                    System.Environment.NewLine);
            }
            finally
            {
                _btnSend.Enabled = true;
            }
        }

        private void Output(string response)
        {
            AppendOutput(
                "Assistant: " + response +
                System.Environment.NewLine +
                System.Environment.NewLine);
        }

        private async Task StreamResponseAsync()
        {
            _isStreaming = true;
            AppendOutput("Assistant: ");

            try
            {
                await _aiClient
                    .GetStreamingResponseAsync(
                    _conversationHistory,
                    token =>
                    {
                        if (_txtOutput == null ||
                            _txtOutput.IsDisposed)
                            return;
                        if (_txtOutput.InvokeRequired)
                            _txtOutput.Invoke(
                                new Action(() =>
                                    _txtOutput.AppendText(
                                        token)));
                        else
                            _txtOutput.AppendText(token);
                    },
                    complete =>
                    {
                        Action nl = () =>
                            _txtOutput.AppendText(
                                System.Environment
                                    .NewLine +
                                System.Environment
                                    .NewLine);
                        if (_txtOutput.InvokeRequired)
                            _txtOutput.Invoke(nl);
                        else
                            nl();

                        _conversationHistory.Add(
                            new ChatMessage
                            {
                                role = "assistant",
                                content = complete
                            });
                        _chatHistory.LogAssistantMessage(
                            complete);
                    },
                    error =>
                    {
                        AppendOutput(
                            System.Environment.NewLine +
                            "Error: " + error +
                            System.Environment.NewLine +
                            System.Environment.NewLine);
                    }
                );
            }
            finally
            {
                _isStreaming = false;
            }
        }

        private void AppendOutput(string text)
        {
            if (_txtOutput == null ||
                _txtOutput.IsDisposed) return;
            if (_txtOutput.InvokeRequired)
            {
                _txtOutput.Invoke(
                    new Action<string>(AppendOutput),
                    text);
                return;
            }
            _txtOutput.AppendText(text);
        }

        private string BuildContextualUserPrompt(
            string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();
            string data = "";

            if (Contains(n, "parameter", "dimension"))
                data = GetParametersData();
            else if (Contains(n, "material"))
                data = GetMaterialData();
            else if (Contains(n, "iproperty",
                "iproperties", "part number",
                "description"))
                data = GetIPropertiesData();
            else if (Contains(n, "selected", "selection"))
                data = GetSelectionData();
            else if (Contains(n, "feature", "features"))
                data = GetFeaturesContext();
            else if (Contains(n,
                "component", "components"))
                data = GetComponentsContext();
            else
                data = GetActiveDocumentContext();

            return prompt +
                   System.Environment.NewLine +
                   System.Environment.NewLine +
                   data;
        }

        private string GetLocalFallbackResponse(
            string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (Contains(n, "what file", "active file",
                "file open", "what document"))
                return GetActiveDocumentInfo();
            if (Contains(n,
                "list parameter", "parameters"))
                return GetParametersData();
            if (Contains(n, "material"))
                return GetMaterialData();
            if (Contains(n, "iproperty", "iproperties",
                "part number", "description"))
                return GetIPropertiesData();
            if (Contains(n, "selected", "selection"))
                return GetSelectionData();
            if (Contains(n, "inspect", "check",
                "review", "summarize"))
                return GetModelInspectionSummary();
            if (Contains(n, "help"))
                return GetHelpText();

            return
                "Running in Local Mode. " +
                "Type 'help' for all commands.";
        }

        private string GetHelpText()
        {
            return
                "\u2500\u2500 Read \u2500\u2500" +
                System.Environment.NewLine +
                "what file is open, list parameters, " +
                "show material, show iproperties, " +
                "open iproperties panel, " +
                "what is selected, list features, " +
                "list components, update mass, inspect" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 Parameters \u2500\u2500" +
                System.Environment.NewLine +
                "set <Name> to <Value>  " +
                "(e.g. 'set Width to 24')" +
                System.Environment.NewLine +
                "change <Name> to <Value>" +
                System.Environment.NewLine +
                "update <Name> to <Value>" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 iProperties Editor \u2500\u2500" +
                System.Environment.NewLine +
                "set part number to <value>, " +
                "set description to <value>, " +
                "set revision to <value>, " +
                "set designer to <value>, " +
                "set material to <value>, " +
                "show iproperties, " +
                "clear part number, clear description" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 Assembly Structure \u2500\u2500" +
                System.Environment.NewLine +
                "assembly structure, component hierarchy, " +
                "assembly tree, count components, " +
                "list sub assemblies, assembly summary" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 View Commands \u2500\u2500" +
                System.Environment.NewLine +
                "zoom to fit, home view, isometric view, " +
                "top/front/right view, shaded view, " +
                "shaded with edges, wireframe, " +
                "zoom in, zoom out, orbit view" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 Visibility \u2500\u2500" +
                System.Environment.NewLine +
                "hide <name>, show <name>, " +
                "hide all, show all, " +
                "suppress <name>, unsuppress <name>, " +
                "suppress all, unsuppress all, " +
                "toggle <name>, " +
                "list hidden components, " +
                "list suppressed components" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 iProperties Panel \u2500\u2500" +
                System.Environment.NewLine +
                "open iproperties panel, " +
                "show iproperties panel, " +
                "dock iproperties panel" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 Drawing \u2500\u2500" +
                System.Environment.NewLine +
                "new drawing, add all views, " +
                "add front/top/right/isometric view, " +
                "add dimensions, update title block, " +
                "add border, add sheet, drawing info, " +
                "set scale 1:2, export drawing pdf/dwg, " +
                "delete all views" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 Actions \u2500\u2500" +
                System.Environment.NewLine +
                "create sketch on [front/top/side] plane, " +
                "draw circle/rectangle/line, finish sketch, " +
                "extrude, fillet, chamfer, " +
                "create a [W]x[H] plate [T] thick, " +
                "create a cylinder radius [R] height [H]" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 Sheet Metal \u2500\u2500" +
                System.Environment.NewLine +
                "create sheet metal part, " +
                "set thickness/gauge/bend radius/k-factor, " +
                "create flat pattern, gauge table" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 BOM \u2500\u2500" +
                System.Environment.NewLine +
                "show bill of materials, " +
                "export bom to excel/csv, " +
                "list unique parts, count fasteners" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 Vault \u2500\u2500" +
                System.Environment.NewLine +
                "search vault, show checked out files, " +
                "show recent vault files, " +
                "open vault explorer" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 iLogic \u2500\u2500" +
                System.Environment.NewLine +
                "generate ilogic rule for [topic]" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 History \u2500\u2500" +
                System.Environment.NewLine +
                "show chat history sessions, " +
                "open chat history folder" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 Tips \u2500\u2500" +
                System.Environment.NewLine +
                "Up arrow = previous prompt" +
                System.Environment.NewLine +
                "Library tab = 130+ pre-built prompts";
        }

        private string GetActiveDocumentContext()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return "[No active document.]";
                return
                    "[Inventor Context]" +
                    System.Environment.NewLine +
                    "Name: " + doc.DisplayName +
                    System.Environment.NewLine +
                    "Type: " +
                    GetDocumentTypeName(doc) +
                    System.Environment.NewLine +
                    "Path: " + doc.FullFileName;
            }
            catch (Exception ex)
            {
                return "[Context error: " +
                       ex.Message + "]";
            }
        }

        private string GetActiveDocumentInfo()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return
                        "No active document is open.";
                return
                    "Active document: " +
                    doc.DisplayName +
                    " (" + GetDocumentTypeName(doc) +
                    ")" +
                    System.Environment.NewLine +
                    "Path: " + doc.FullFileName;
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        private string GetParametersData()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return
                        "No active document is open.";

                Parameters parameters = null;

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                    parameters = ((PartDocument)doc)
                        .ComponentDefinition.Parameters;
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                    parameters = ((AssemblyDocument)doc)
                        .ComponentDefinition.Parameters;
                else
                    return
                        "Parameters only available " +
                        "for Part and Assembly documents.";

                if (parameters == null ||
                    parameters.Count == 0)
                    return "No parameters found.";

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine("Parameters in " +
                    doc.DisplayName + ":");
                sb.AppendLine(new string('-', 40));

                foreach (Parameter param in parameters)
                {
                    try
                    {
                        sb.AppendLine(
                            param.Name.PadRight(25) +
                            param.Expression.PadRight(20) +
                            param.get_Units());
                    }
                    catch { }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return
                    "Failed to read parameters: " +
                    ex.Message;
            }
        }

        private string GetMaterialData()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return
                        "No active document is open.";

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    PartDocument part =
                        (PartDocument)doc;
                    string mat =
                        part.ComponentDefinition
                            .Material.Name;
                    double mass =
                        part.ComponentDefinition
                            .MassProperties.Mass;
                    double vol =
                        part.ComponentDefinition
                            .MassProperties.Volume;
                    return
                        "Material: " + mat +
                        System.Environment.NewLine +
                        "Mass: " +
                        Math.Round(mass * 2.20462, 4) +
                        " lbs" +
                        System.Environment.NewLine +
                        "Volume: " +
                        Math.Round(vol * 61.0237, 4) +
                        " in\u00b3";
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                {
                    AssemblyDocument asm =
                        (AssemblyDocument)doc;
                    double mass =
                        asm.ComponentDefinition
                           .MassProperties.Mass;
                    return "Assembly mass: " +
                           Math.Round(
                               mass * 2.20462, 4) +
                           " lbs";
                }
                return "Material data not available.";
            }
            catch (Exception ex)
            {
                return "Failed to read material: " +
                       ex.Message;
            }
        }

        private string GetIPropertiesData()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null)
                    return
                        "No active document is open.";

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine("iProperties for " +
                    doc.DisplayName + ":");
                sb.AppendLine(new string('-', 40));

                foreach (PropertySet ps in
                    doc.PropertySets)
                {
                    foreach (Property prop in ps)
                    {
                        try
                        {
                            string val =
                                Convert.ToString(
                                    prop.Value);
                            if (!string.IsNullOrWhiteSpace(
                                    val))
                                sb.AppendLine(
                                    prop.Name.PadRight(30) +
                                    val);
                        }
                        catch { }
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return
                    "Failed to read iProperties: " +
                    ex.Message;
            }
        }

        private string GetSelectionData()
        {
            try
            {
                SelectSet sel =
                    _inventorApplication.ActiveDocument
                                        .SelectSet;
                if (sel == null || sel.Count == 0)
                    return
                        "Nothing is currently selected.";

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine("Selected items (" +
                    sel.Count + "):");
                sb.AppendLine(new string('-', 40));

                int i = 1;
                foreach (object item in sel)
                {
                    try
                    {
                        string desc =
                            item.GetType().Name;
                        if (item is PartFeature pf)
                            desc =
                                "Feature: " + pf.Name;
                        else if (item is Face f)
                            desc = "Face (" +
                                   f.SurfaceType + ")";
                        else if (item is Edge)
                            desc = "Edge";
                        else if (item is
                            ComponentOccurrence co)
                            desc =
                                "Component: " + co.Name;
                        else if (item is Parameter p)
                            desc =
                                "Parameter: " + p.Name +
                                " = " + p.Expression;
                        sb.AppendLine(i + ". " + desc);
                        i++;
                    }
                    catch
                    {
                        sb.AppendLine(
                            i + ". (unreadable)");
                        i++;
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read selection: " +
                       ex.Message;
            }
        }

        private string GetFeaturesContext()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum.kPartDocumentObject)
                    return "[Features not available.]";

                PartDocument part = (PartDocument)doc;
                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine("[Features in " +
                    doc.DisplayName + "]");

                foreach (PartFeature f in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        string s = f.Suppressed ?
                            " (suppressed)" : "";
                        sb.AppendLine(
                            "- " + f.Name + s);
                    }
                    catch { }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "[Features error: " +
                       ex.Message + "]";
            }
        }

        private string GetComponentsContext()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;
                if (doc == null ||
                    doc.DocumentType !=
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                    return
                        "[Components not available.]";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;
                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine("[Components in " +
                    doc.DisplayName + "]");

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        string v = occ.Visible ?
                            "" : " (hidden)";
                        sb.AppendLine(
                            "- " + occ.Name + v);
                    }
                    catch { }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "[Components error: " +
                       ex.Message + "]";
            }
        }

        private string GetModelInspectionSummary()
        {
            return
                "Recommended inspection checklist:" +
                System.Environment.NewLine +
                "1. Parameters \u2014 " +
                "design intent and errors" +
                System.Environment.NewLine +
                "2. Material \u2014 " +
                "matches manufacturing requirements" +
                System.Environment.NewLine +
                "3. iProperties \u2014 " +
                "Part Number and Description" +
                System.Environment.NewLine +
                "4. Feature health \u2014 " +
                "warnings in Model browser" +
                System.Environment.NewLine +
                "5. Sketches \u2014 " +
                "under-constrained references" +
                System.Environment.NewLine +
                "6. Sheet metal \u2014 " +
                "flat pattern and bends" +
                System.Environment.NewLine +
                "7. Origin \u2014 " +
                "sensible for assembly use" +
                System.Environment.NewLine +
                "8. Drawing readiness \u2014 " +
                "parameter-driven dimensions";
        }

        private string GetDocumentTypeName(
            Document doc)
        {
            switch (doc.DocumentType)
            {
                case DocumentTypeEnum
                    .kPartDocumentObject:
                    return "Part";
                case DocumentTypeEnum
                    .kAssemblyDocumentObject:
                    return "Assembly";
                case DocumentTypeEnum
                    .kDrawingDocumentObject:
                    return "Drawing";
                case DocumentTypeEnum
                    .kPresentationDocumentObject:
                    return "Presentation";
                default:
                    return "Unknown";
            }
        }

        private bool Contains(
            string input, params string[] keywords)
        {
            foreach (string kw in keywords)
                if (input.Contains(kw)) return true;
            return false;
        }
    }
}
