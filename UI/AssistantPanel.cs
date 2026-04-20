using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class AssistantPanel : UserControl
    {
        // ── Fields ────────────────────────────────────
        private Inventor.Application _inventorApplication;
        private AiChatClient _aiClient;
        private List<ChatMessage> _conversationHistory;
        private bool _aiAvailable = false;
        private bool _isStreaming = false;
        private System.Windows.Forms.Timer _statusCheckTimer;
        private EngraveHandler _engraveHandler;
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
        private FileManager _fileManager;
        private ParameterExporter _paramExporter;
        private QuickMeasure _quickMeasure;
        private IPropertiesDockableWindow _iPropWindow;
        private SheetMetalDefaultsDockableWindow _smDefaultsWindow;

        private List<string> _savedPrompts;
        private List<string> _promptHistory;
        private int _promptHistoryIndex = -1;

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

        // ── Constructor ───────────────────────────────
        public AssistantPanel(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
            _aiClient = new AiChatClient();
            _conversationHistory = new List<ChatMessage>();
            _savedPrompts = new List<string>();
            _promptHistory = new List<string>();
            _chatHistory = new ChatHistoryManager();

            _writeActions =
                new WriteActionsHandler(
                    _inventorApplication);
            _actionExecutor =
                new ActionExecutor(
                    _inventorApplication);
            _iLogicGenerator =
                new ILogicGenerator(
                    _inventorApplication);
            _bomExporter =
                new BomExporter(
                    _inventorApplication);
            _vaultSearch =
                new VaultSearchHandler(
                    _inventorApplication);
            _sheetMetal =
                new SheetMetalHandler(
                    _inventorApplication);
            _drawingAuto =
                new DrawingAutomation(
                    _inventorApplication);
            _geometryAnalysis =
                new GeometryAnalysis(
                    _inventorApplication);
            _paramValidator =
                new ParameterValidator(
                    _inventorApplication);
            _healthCheck =
                new AssemblyHealthCheck(
                    _inventorApplication);
            _holePattern =
                new HolePatternCalculator(
                    _inventorApplication);
            _partNumberMgr =
                new PartNumberManager(
                    _inventorApplication);
            _sketchAnalyzer =
                new SketchAnalyzer(
                    _inventorApplication);
            _featureErrors =
                new FeatureErrorDetector(
                    _inventorApplication);
            _holeThread =
                new HoleThreadReader(
                    _inventorApplication);
            _selectionEngine =
                new SelectionEngine(
                    _inventorApplication);
            _interferenceDetector =
                new InterferenceDetector(
                    _inventorApplication);
            _constraintAnalyzer =
                new ConstraintAnalyzer(
                    _inventorApplication);
            _visibilityHandler =
                new ComponentVisibilityHandler(
                    _inventorApplication);
            _iPropertiesEditor =
                new IPropertiesEditor(
                    _inventorApplication);

            _iPropWindow =
                new IPropertiesDockableWindow(
                    _inventorApplication,
                    _iPropertiesEditor,
                    "{C3B2E8D3-7E5F-4A88-9E9E-1F4A8A202301}");
            _iPropWindow.Create();

            _smDefaultsWindow =
    new SheetMetalDefaultsDockableWindow(
        _inventorApplication,
        "{D4E52C91-7F3A-4B22-9E8F-2A3B56CD7E81}");
            _smDefaultsWindow.Create();

            _structureAnalyzer =
                new AssemblyStructureAnalyzer(
                    _inventorApplication);
            _viewCommands =
                new ViewCommandHandler(
                    _inventorApplication);
            _fileManager =
                new FileManager(
                    _inventorApplication);
            _paramExporter =
                new ParameterExporter(
                    _inventorApplication);
            _quickMeasure =
                new QuickMeasure(
                    _inventorApplication);
            _engraveHandler =
                new EngraveHandler(
        _inventorApplication);

            BuildUI();
            InitializeConversation();
            StartStatusChecker();
        }

        // ── UI Build ──────────────────────────────────
        private void BuildUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor =
                System.Drawing.Color.FromArgb(45, 45, 48);

            _lblStatus = new Label();
            _lblStatus.Location =
                new System.Drawing.Point(8, 5);
            _lblStatus.Size =
                new System.Drawing.Size(240, 18);
            _lblStatus.Font =
                new System.Drawing.Font(
                    "Segoe UI", 8.25f,
                    System.Drawing.FontStyle.Bold);
            _lblStatus.ForeColor =
                System.Drawing.Color.Gray;
            _lblStatus.Text = "Checking AI...";
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
            _tabControl.Font =
                new System.Drawing.Font("Segoe UI", 9f);

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
            _txtOutput =
                new System.Windows.Forms.TextBox();
            _txtOutput.Multiline = true;
            _txtOutput.ReadOnly = true;
            _txtOutput.ScrollBars = ScrollBars.Vertical;
            _txtOutput.BackColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            _txtOutput.ForeColor =
                System.Drawing.Color.White;
            _txtOutput.BorderStyle = BorderStyle.None;
            _txtOutput.Font =
                new System.Drawing.Font("Consolas", 9f);

            _txtPrompt = new InventorTextBox();
            _txtPrompt.Multiline = true;
            _txtPrompt.AcceptsReturn = false;
            _txtPrompt.AcceptsTab = false;
            _txtPrompt.ScrollBars = ScrollBars.Vertical;
            _txtPrompt.BackColor =
                System.Drawing.Color.FromArgb(37, 37, 38);
            _txtPrompt.ForeColor =
                System.Drawing.Color.White;
            _txtPrompt.Font =
                new System.Drawing.Font("Segoe UI", 9.5f);
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

            // Vault Button Strip
            var vaultStrip = new VaultButtonStrip();
            vaultStrip.CheckOutClicked += (s, e) =>
            {
                if (_inventorApplication?.ActiveDocument
                    == null)
                {
                    AppendOutput("⚠️ No active document.\n");
                    return;
                }
                string fp = _inventorApplication
                    .ActiveDocument.FullFileName;
                var v = new VaultHandler();
                AppendOutput(v.CheckOut(fp)
                    ? $"✅ Checked out: " +
                      $"{System.IO.Path.GetFileName(fp)}\n"
                    : "❌ Check out failed.\n");
            };
            vaultStrip.CheckInClicked += (s, e) =>
            {
                if (_inventorApplication?.ActiveDocument
                    == null)
                {
                    AppendOutput("⚠️ No active document.\n");
                    return;
                }
                string fp = _inventorApplication
                    .ActiveDocument.FullFileName;
                string comment =
                    Microsoft.VisualBasic.Interaction
                    .InputBox(
                        "Enter check in comment:",
                        "Vault Check In", "");
                var v = new VaultHandler();
                AppendOutput(v.CheckIn(fp, comment)
                    ? $"✅ Checked in: " +
                      $"{System.IO.Path.GetFileName(fp)}\n"
                    : "❌ Check in failed.\n");
            };
            vaultStrip.StatusClicked += (s, e) =>
            {
                if (_inventorApplication?.ActiveDocument
                    == null)
                {
                    AppendOutput("⚠️ No active document.\n");
                    return;
                }
                string fp = _inventorApplication
                    .ActiveDocument.FullFileName;
                var v = new VaultHandler();
                AppendOutput(v.GetStatus(fp) + "\n");
            };
            vaultStrip.UndoClicked += (s, e) =>
            {
                if (_inventorApplication?.ActiveDocument
                    == null)
                {
                    AppendOutput("⚠️ No active document.\n");
                    return;
                }
                string fp = _inventorApplication
                    .ActiveDocument.FullFileName;
                var v = new VaultHandler();
                AppendOutput(v.UndoCheckOut(fp)
                    ? $"↩️ Undo successful: " +
                      $"{System.IO.Path.GetFileName(fp)}\n"
                    : "❌ Undo failed.\n");
            };
            _tabChat.Controls.Add(vaultStrip);
            _tabChat.Resize +=
                (s, e) => LayoutChatTab();
        }

        private void BuildSavedTab()
        {
            _lstSavedPrompts = new ListBox();
            _lstSavedPrompts.BackColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            _lstSavedPrompts.ForeColor =
                System.Drawing.Color.White;
            _lstSavedPrompts.Font =
                new System.Drawing.Font("Segoe UI", 9f);
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
                int idx =
                    _lstSavedPrompts.SelectedIndex;
                _savedPrompts.RemoveAt(idx);
                _lstSavedPrompts.Items.RemoveAt(idx);
            };

            _tabSaved.Controls.Add(_lstSavedPrompts);
            _tabSaved.Controls.Add(_btnRunPrompt);
            _tabSaved.Controls.Add(_btnDeletePrompt);
            _tabSaved.Resize +=
                (s, e) => LayoutSavedTab();
        }

        private void BuildLibraryTab()
        {
            _lstLibrary = new ListBox();
            _lstLibrary.BackColor =
                System.Drawing.Color.FromArgb(30, 30, 30);
            _lstLibrary.ForeColor =
                System.Drawing.Color.White;
            _lstLibrary.Font =
                new System.Drawing.Font("Segoe UI", 9f);
            _lstLibrary.BorderStyle = BorderStyle.None;
            _lstLibrary.DoubleClick +=
                (s, e) => RunLibraryPrompt();

            string[] items = new string[]
            {
                "dock iproperties",
                "dock iprops",
                "open iproperties panel",
                "close iproperties panel",
                "toggle iproperties panel",
                "save", "save all", "list open files",
                "file info", "new part", "new assembly",
                "open file location", "save copy as",
                "close",
                "export parameters to csv",
                "export parameters to excel",
                "parameter report", "parameter summary",
                "list user parameters",
                "list model parameters",
                "measure selected", "measure edge",
                "measure face", "bounding box",
                "surface area", "measure hole",
                "measure radius", "measure angle",
                "minimum distance", "measure all edges",
                "zoom to fit", "home view",
                "isometric view", "front view",
                "top view", "right view",
                "shaded view", "shaded with edges",
                "wireframe", "zoom in", "zoom out",
                "previous view", "orbit view",
                "set part number to SAV-24R",
                "set description to Fan Coil Unit",
                "set revision to B",
                "set designer to Blake Conner",
                "set material to Galvanized Steel",
                "clear part number",
                "clear description",
                "assembly structure",
                "component hierarchy",
                "count components",
                "list sub assemblies",
                "show iproperties",
                "what file is open",
                "list parameters", "show material",
                "what is selected", "list features",
                "list components", "inspect",
                "set Width to 24", "set Height to 12",
                "set Thickness to 0.040",
                "set Length to 48",
                "show all", "hide all",
                "suppress all", "unsuppress all",
                "list hidden components",
                "list suppressed components",
                "hide coil assembly",
                "show coil assembly",
                "run interference check",
                "assembly health check",
                "find missing part numbers",
                "find missing materials",
                "new drawing", "add all views",
                "add front view", "export drawing pdf",
                "export drawing dwg",
                "create sheet metal part",
                "gauge table", "create flat pattern",
                "set thickness 0.040",
                "show bill of materials",
                "export bom to excel",
                "export bom to csv",
                "search vault", "open vault explorer",
                "show checked out files",
                "generate ilogic rule for thickness",
                "generate ilogic rule for material",
                "generate ilogic rule for part number",
                "show chat history sessions",
                "open chat history folder",
                "what gauge is standard for a fan coil casing",
                "what is the difference between FPB and SAV",
                "assembly constraint analysis",
                "sketch status", "hole report",
                "tap drill table",
                "next part number FPB",
                "next part number SAV",
                "geometry report",
                "validate parameters", "help",
                "engrave part name",
                "engrave part number on face"
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

        // ── Layout ────────────────────────────────────
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
            int tabHeight =
                Math.Max(h - tabTop - pad, 100);
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
            int ph = 70;   // prompt height
            int sh = 28;   // send button height
            int vh = 35;   // vault strip height

            // Total bottom area needed
            int bottomArea = ph + sh + vh + pad * 4;
            int outH = Math.Max(
                h - bottomArea, 40);

            // Chat output fills top area
            _txtOutput.Location =
                new System.Drawing.Point(pad, pad);
            _txtOutput.Size =
                new System.Drawing.Size(
                    w - pad * 2, outH);

            // Prompt box below output
            int pTop = pad + outH + pad;
            _txtPrompt.Location =
                new System.Drawing.Point(pad, pTop);
            _txtPrompt.Size =
                new System.Drawing.Size(
                    w - pad * 2, ph);

            // Send button below prompt
            int sTop = pTop + ph + pad;
            _btnSend.Location =
                new System.Drawing.Point(
                    w - 70 - pad, sTop);
            _btnSend.Size =
                new System.Drawing.Size(70, sh);

            // Vault strip sits at very bottom
            // via DockStyle.Bottom - no manual
            // positioning needed
        }

        private void LayoutSavedTab()
        {
            int w = _tabSaved.ClientSize.Width;
            int h = _tabSaved.ClientSize.Height;
            int pad = 6;
            int btnH = 28;

            _lstSavedPrompts.Location =
                new System.Drawing.Point(pad, pad);
            _lstSavedPrompts.Size =
                new System.Drawing.Size(
                    w - pad * 2,
                    h - btnH - pad * 3);

            int btnTop = h - btnH - pad;
            _btnRunPrompt.Location =
                new System.Drawing.Point(pad, btnTop);
            _btnRunPrompt.Size =
                new System.Drawing.Size(80, btnH);
            _btnDeletePrompt.Location =
                new System.Drawing.Point(
                    pad + 85, btnTop);
            _btnDeletePrompt.Size =
                new System.Drawing.Size(80, btnH);
        }

        private void LayoutLibraryTab()
        {
            int w = _tabLibrary.ClientSize.Width;
            int h = _tabLibrary.ClientSize.Height;
            int pad = 6;
            int btnH = 28;

            _lstLibrary.Location =
                new System.Drawing.Point(pad, pad);
            _lstLibrary.Size =
                new System.Drawing.Size(
                    w - pad * 2,
                    h - btnH - pad * 3);
            _btnRunLibrary.Location =
                new System.Drawing.Point(
                    pad, h - btnH - pad);
            _btnRunLibrary.Size =
                new System.Drawing.Size(80, btnH);
        }

        // ── Key handling ──────────────────────────────
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
                string t =
                    _txtPrompt.Text ?? string.Empty;
                if (len > 0) t = t.Remove(pos, len);
                t = t.Insert(
                    pos, System.Environment.NewLine);
                _txtPrompt.Text = t;
                _txtPrompt.SelectionStart =
                    pos +
                    System.Environment.NewLine.Length;
                _txtPrompt.SelectionLength = 0;
                return;
            }

            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                System.Diagnostics.Debug.WriteLine(
                    "Enter — calling SendPromptAsync");
                _ = SendPromptAsync();
                return;
            }
        }

        // ── InventorTextBox ───────────────────────────
        // ── InventorTextBox ───────────────────────────
        private class InventorTextBox :
            System.Windows.Forms.TextBox
        {
            private const int WH_KEYBOARD_LL = 13;
            private const int WM_KEYDOWN = 0x0100;
            private const int VK_RETURN = 0x0D;
            private const int VK_SHIFT = 0x10;

            private delegate IntPtr
                LowLevelKeyboardProc(
                    int nCode,
                    IntPtr wParam,
                    IntPtr lParam);

            [System.Runtime.InteropServices
                .DllImport("user32.dll")]
            private static extern IntPtr
                SetWindowsHookEx(
                    int idHook,
                    LowLevelKeyboardProc lpfn,
                    IntPtr hMod,
                    uint dwThreadId);

            [System.Runtime.InteropServices
                .DllImport("user32.dll")]
            private static extern bool
                UnhookWindowsHookEx(IntPtr hhk);

            [System.Runtime.InteropServices
                .DllImport("user32.dll")]
            private static extern IntPtr
                CallNextHookEx(
                    IntPtr hhk,
                    int nCode,
                    IntPtr wParam,
                    IntPtr lParam);

            [System.Runtime.InteropServices
                .DllImport("kernel32.dll")]
            private static extern IntPtr
                GetModuleHandle(string lpModuleName);

            [System.Runtime.InteropServices
                .DllImport("user32.dll")]
            private static extern short
                GetKeyState(int nVirtKey);

            private IntPtr _hookId = IntPtr.Zero;
            private LowLevelKeyboardProc _proc;

            public InventorTextBox()
            {
                _proc = HookCallback;
                _hookId = SetHook(_proc);
            }

            private IntPtr SetHook(
                LowLevelKeyboardProc proc)
            {
                using (var curProcess =
                    System.Diagnostics.Process
                        .GetCurrentProcess())
                using (var curModule =
                    curProcess.MainModule)
                {
                    return SetWindowsHookEx(
                        WH_KEYBOARD_LL,
                        proc,
                        GetModuleHandle(
                            curModule.ModuleName),
                        0);
                }
            }

            private IntPtr HookCallback(
                int nCode,
                IntPtr wParam,
                IntPtr lParam)
            {
                if (nCode >= 0 &&
                    wParam == (IntPtr)WM_KEYDOWN &&
                    !this.IsDisposed &&
                    this.Focused)
                {
                    int vkCode = System.Runtime
                        .InteropServices.Marshal
                        .ReadInt32(lParam);

                    if (vkCode == VK_RETURN)
                    {
                        System.Diagnostics.Debug
                            .WriteLine(
                                "Hook caught Return!");
                        bool shift =
                            (GetKeyState(VK_SHIFT) &
                             0x8000) != 0;
                        this.BeginInvoke(
                            new Action(() =>
                                OnKeyDown(
                                    new KeyEventArgs(
                                        shift
                                        ? Keys.Return |
                                          Keys.Shift
                                        : Keys.Return))));
                        return (IntPtr)1;
                    }
                }
                return CallNextHookEx(
                    _hookId, nCode, wParam, lParam);
            }

            protected override void Dispose(
                bool disposing)
            {
                if (_hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }
                base.Dispose(disposing);
            }
        }

        // ── Public methods ────────────────────────────
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
                    new Action(
                        InsertNewlineFromInventor));
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
        // ── Prompt helpers ────────────────────────────
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
            if (_lstSavedPrompts.SelectedIndex < 0)
                return;
            string s =
                _lstSavedPrompts.SelectedItem
                    ?.ToString();
            if (string.IsNullOrWhiteSpace(s)) return;
            _tabControl.SelectedTab = _tabChat;
            _txtPrompt.Text = s;
            _ = SendPromptAsync();
        }

        // ── New chat / diagnostic ─────────────────────
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
                System.Environment
                    .GetEnvironmentVariable(
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
                var testMessages =
                    new List<ChatMessage>
                    {
                        new ChatMessage
                        {
                            role    = "user",
                            content =
                                "Reply with the " +
                                "single word: connected"
                        }
                    };
                testResponse =
                    await _aiClient
                        .GetResponseAsync(testMessages);
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
                testResponse =
                    "Exception: " + ex.Message;
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

        // ── Conversation init ─────────────────────────
        private void InitializeConversation()
        {
            _conversationHistory.Clear();
            _conversationHistory.Add(new ChatMessage
            {
                role = "system",
                content =
                    "You are an AI assistant inside " +
                    "Autodesk Inventor 2023 at an HVAC " +
                    "manufacturing company in Athens, TX. " +
                    "Expert in FPB, SAV, ERU, DDU, " +
                    "sheet metal, iLogic, Vault, " +
                    "OSHPD, SMACNA, ASHRAE. " +
                    "Gauges: 22ga=0.034in, 20ga=0.040in, " +
                    "18ga=0.052in, 16ga=0.060in. " +
                    "Always use imperial units. " +
                    "Be concise and practical."
            });
        }

        // ── Status checker ────────────────────────────
        private void StartStatusChecker()
        {
            SetStatus(false);
            _statusCheckTimer =
                new System.Windows.Forms.Timer();
            _statusCheckTimer.Interval = 300000;
            _statusCheckTimer.Tick +=
                async (s, e) =>
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
                _lblStatus.Text =
                    "\u25cf AI Connected";
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

        // ── Parameter modifier ────────────────────────
        private string TryHandleParameterModification(
            string prompt)
        {
            string n =
                prompt.Trim().ToLowerInvariant();
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
                if (parts[i].ToLower() == "to")
                { toIndex = i; break; }

            if (toIndex < 0 ||
                toIndex >= parts.Length - 1)
                return null;

            string paramName =
                string.Join(" ", parts, 1, toIndex - 1);
            string valueStr = parts[toIndex + 1];

            if (string.IsNullOrWhiteSpace(paramName) ||
                string.IsNullOrWhiteSpace(valueStr))
                return null;

            if (!double.TryParse(
                valueStr,
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
                    parameters =
                        ((PartDocument)doc)
                            .ComponentDefinition
                            .Parameters;
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                    parameters =
                        ((AssemblyDocument)doc)
                            .ComponentDefinition
                            .Parameters;
                else
                    return
                        "Parameter editing requires " +
                        "a Part or Assembly document.";

                Parameter found = null;
                foreach (Parameter p in parameters)
                    if (string.Equals(
                        p.Name, paramName,
                        StringComparison.OrdinalIgnoreCase))
                    { found = p; break; }

                if (found == null)
                    return "Parameter '" + paramName +
                           "' not found. Type " +
                           "'list parameters' to see " +
                           "available parameters.";

                double oldVal =
                    (double)found.Value * 0.393701;
                found.Expression = valueStr + " in";

                return "\u2705 Parameter '" +
                       found.Name + "' updated: " +
                       Math.Round(oldVal, 4) +
                       " in \u2192 " + valueStr + " in";
            }
            catch (Exception ex)
            {
                return "Failed to set parameter: " +
                       ex.Message;
            }
        }

        // ── Send prompt ───────────────────────────────
        private async Task SendPromptAsync()
        {
            if (_isStreaming) return;
            if (_txtPrompt == null ||
                _txtPrompt.IsDisposed) return;

            string prompt = _txtPrompt.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            { FocusPrompt(); return; }

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

                if (n == "sheet metal defaults" ||
    n == "open sheet metal defaults" ||
    n == "dock sheet metal defaults" ||
    n == "sm defaults")
                {
                    try
                    {
                        _smDefaultsWindow.Show();
                        Output(
                            "✅ Sheet Metal Defaults " +
                            "panel opened.");
                    }
                    catch (Exception ex)
                    {
                        Output(
                            "Failed to open Sheet Metal " +
                            "Defaults: " + ex.Message);
                    }
                    return;
                }

                if (n == "close sheet metal defaults")
                {
                    _smDefaultsWindow.Hide();
                    Output(
                        "Sheet Metal Defaults " +
                        "panel closed.");
                    return;
                }

                if (n == "toggle sheet metal defaults")
                {
                    _smDefaultsWindow.Toggle();
                    Output(
                        "Sheet Metal Defaults " +
                        "panel toggled.");
                    return;
                }
                if (n == "dock iproperties" ||
                    n == "open iproperties panel" ||
                    n == "iproperties panel")
                {
                    try
                    {
                        _iPropWindow.Show();
                        Output(
                            "iProperties panel opened.");
                    }
                    catch (Exception ex)
                    {
                        Output(
                            "Failed to open iProperties: " +
                            ex.Message);
                    }
                    return;
                }

                if (n == "close iproperties panel")
                {
                    _iPropWindow.Hide();
                    Output("iProperties panel closed.");
                    return;
                }

                if (n == "toggle iproperties panel")
                {
                    _iPropWindow.Toggle();
                    Output("iProperties panel toggled.");
                    return;
                }

                response =
                    _iPropertiesEditor
                        .TryHandleIPropertiesEdit(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    TryHandleParameterModification(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _visibilityHandler
                        .TryHandleVisibility(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _structureAnalyzer
                        .TryHandleStructure(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _viewCommands
                        .TryHandleViewCommand(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _fileManager
                        .TryHandleFileCommand(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _paramExporter
                        .TryHandleParameterExport(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _quickMeasure
                        .TryHandleQuickMeasure(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }
                response =
    _engraveHandler
        .TryHandleEngrave(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }
                response =
                    _selectionEngine
                        .TryHandleSelection(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _interferenceDetector
                        .TryHandleInterference(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _constraintAnalyzer
                        .TryHandleConstraints(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _sketchAnalyzer
                        .TryHandleSketchAnalysis(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _featureErrors
                        .TryHandleFeatureErrors(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _holeThread
                        .TryHandleHoleThread(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _holePattern
                        .TryHandleHolePattern(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _partNumberMgr
                        .TryHandlePartNumber(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _geometryAnalysis
                        .TryHandleGeometry(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _paramValidator
                        .TryHandleValidation(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _healthCheck
                        .TryHandleHealthCheck(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _drawingAuto
                        .TryHandleDrawing(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _sheetMetal
                        .TryHandleSheetMetal(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _bomExporter.TryHandleBom(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _vaultSearch
                        .TryHandleVaultSearch(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _iLogicGenerator
                        .TryHandleILogic(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _writeActions
                        .TryHandleWriteAction(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

                response =
                    _actionExecutor
                        .TryExecuteAction(prompt);
                if (response != null)
                { Output(response); _chatHistory.LogAssistantMessage(response); return; }

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
                                    _txtOutput
                                        .AppendText(
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
                        _chatHistory
                            .LogAssistantMessage(
                                complete);
                    },
                    error =>
                    {
                        AppendOutput(
                            System.Environment.NewLine +
                            "Error: " + error +
                            System.Environment.NewLine +
                            System.Environment.NewLine);
                    });
            }
            finally { _isStreaming = false; }
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

        // ── Context / fallback ────────────────────────
        private string BuildContextualUserPrompt(
            string prompt)
        {
            string n =
                prompt.Trim().ToLowerInvariant();
            string data = "";

            if (Contains(n, "parameter", "dimension"))
                data = GetParametersData();
            else if (Contains(n, "material"))
                data = GetMaterialData();
            else if (Contains(n, "iproperty",
                "iproperties", "part number",
                "description"))
                data = GetIPropertiesData();
            else if (Contains(n, "selected",
                "selection"))
                data = GetSelectionData();
            else if (Contains(n, "feature", "features"))
                data = GetFeaturesContext();
            else if (Contains(n, "component",
                "components"))
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
            string n =
                prompt.Trim().ToLowerInvariant();
            if (Contains(n, "what file", "active file",
                "file open", "what document"))
                return GetActiveDocumentInfo();
            if (Contains(n, "list parameter",
                "parameters"))
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
                "\u2500\u2500 Parameters \u2500\u2500" +
                System.Environment.NewLine +
                "set <Name> to <Value>" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 iProperties \u2500\u2500" +
                System.Environment.NewLine +
                "set part number/description/revision/" +
                "designer/material to <v>" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 File \u2500\u2500" +
                System.Environment.NewLine +
                "save, save all, list open files, " +
                "new part, new assembly, close" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 View \u2500\u2500" +
                System.Environment.NewLine +
                "zoom to fit, home view, front view, " +
                "top view, shaded with edges, wireframe" +
                System.Environment.NewLine +
                System.Environment.NewLine +
                "\u2500\u2500 Tips \u2500\u2500" +
                System.Environment.NewLine +
                "Up arrow = previous prompt" +
                System.Environment.NewLine +
                "Library tab = 100+ pre-built prompts";
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
                    doc.DisplayName + " (" +
                    GetDocumentTypeName(doc) + ")" +
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
                    parameters =
                        ((PartDocument)doc)
                            .ComponentDefinition
                            .Parameters;
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                    parameters =
                        ((AssemblyDocument)doc)
                            .ComponentDefinition
                            .Parameters;
                else
                    return
                        "Parameters only available for " +
                        "Part and Assembly documents.";

                if (parameters == null ||
                    parameters.Count == 0)
                    return "No parameters found.";

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine(
                    "Parameters in " +
                    doc.DisplayName + ":");
                sb.AppendLine(new string('-', 40));
                foreach (Parameter param in parameters)
                {
                    try
                    {
                        sb.AppendLine(
                            param.Name.PadRight(25) +
                            param.Expression
                                .PadRight(20) +
                            param.get_Units());
                    }
                    catch { }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read parameters: " +
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
                    return
                        "Material: " +
                        part.ComponentDefinition
                            .Material.Name +
                        System.Environment.NewLine +
                        "Mass: " +
                        Math.Round(
                            part.ComponentDefinition
                                .MassProperties.Mass *
                            2.20462, 4) + " lbs" +
                        System.Environment.NewLine +
                        "Volume: " +
                        Math.Round(
                            part.ComponentDefinition
                                .MassProperties.Volume *
                            61.0237, 4) + " in\u00b3";
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                {
                    AssemblyDocument asm =
                        (AssemblyDocument)doc;
                    return "Assembly mass: " +
                        Math.Round(
                            asm.ComponentDefinition
                                .MassProperties.Mass *
                            2.20462, 4) + " lbs";
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
                sb.AppendLine(
                    "iProperties for " +
                    doc.DisplayName + ":");
                sb.AppendLine(new string('-', 40));
                foreach (PropertySet ps in
                    doc.PropertySets)
                    foreach (Property prop in ps)
                    {
                        try
                        {
                            string val =
                                Convert.ToString(
                                    prop.Value);
                            if (!string
                                .IsNullOrWhiteSpace(val))
                                sb.AppendLine(
                                    prop.Name
                                        .PadRight(30) +
                                    val);
                        }
                        catch { }
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
                    _inventorApplication
                        .ActiveDocument.SelectSet;
                if (sel == null || sel.Count == 0)
                    return
                        "Nothing is currently selected.";
                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine(
                    "Selected items (" +
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
                            desc = "Feature: " + pf.Name;
                        else if (item is Face f)
                            desc = "Face (" +
                                   f.SurfaceType + ")";
                        else if (item is Edge)
                            desc = "Edge";
                        else if (item is
                            ComponentOccurrence co)
                            desc = "Component: " +
                                   co.Name;
                        else if (item is Parameter p)
                            desc = "Parameter: " +
                                   p.Name + " = " +
                                   p.Expression;
                        sb.AppendLine(
                            i++ + ". " + desc);
                    }
                    catch
                    {
                        sb.AppendLine(
                            i++ + ". (unreadable)");
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
                sb.AppendLine(
                    "[Features in " +
                    doc.DisplayName + "]");
                foreach (PartFeature f in
                    part.ComponentDefinition.Features)
                {
                    try
                    {
                        sb.AppendLine(
                            "- " + f.Name +
                            (f.Suppressed
                                ? " (suppressed)"
                                : ""));
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
                sb.AppendLine(
                    "[Components in " +
                    doc.DisplayName + "]");
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        sb.AppendLine(
                            "- " + occ.Name +
                            (occ.Visible
                                ? ""
                                : " (hidden)"));
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
                "Inspection checklist:" +
                System.Environment.NewLine +
                "1. Parameters \u2014 design intent" +
                System.Environment.NewLine +
                "2. Material \u2014 manufacturing spec" +
                System.Environment.NewLine +
                "3. iProperties \u2014 Part Number/Desc" +
                System.Environment.NewLine +
                "4. Feature health \u2014 Model browser" +
                System.Environment.NewLine +
                "5. Sketches \u2014 constraints" +
                System.Environment.NewLine +
                "6. Sheet metal \u2014 flat pattern" +
                System.Environment.NewLine +
                "7. Origin \u2014 assembly use" +
                System.Environment.NewLine +
                "8. Drawing \u2014 param-driven dims";
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

    }   // ← closes AssistantPanel class
}       // ← closes namespace