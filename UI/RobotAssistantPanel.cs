using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using TextBox = System.Windows.Forms.TextBox;

namespace Inventor2023AIAssistant
{
    public interface IAssistantWorkflow
    {
        event Action<string> TranscriptAppended;
        event Action TranscriptCleared;
        event Action StatusChanged;
        event Action SavedPromptsChanged;
        event Action BusyChanged;

        string TranscriptText { get; }
        string ConnectionStatus { get; }
        bool AiAvailable { get; }
        bool IsBusy { get; }
        IList<string> GetSavedPrompts();
        IList<string> GetLibraryPrompts();
        TextBox CreateRobotPromptInput(Action focusAction);
        void SetRobotViewActive(bool active);
        Task SendPromptFromRobotAsync(TextBox promptInput);
        void StartNewChatOrDiagnose();
        void RunSavedPromptAt(int index);
        void DeleteSavedPromptAt(int index);
        void RunLibraryPromptAt(int index);
        VaultButtonStrip CreateVaultButtonStrip();
        void FocusPrompt();
    }
    public class RobotAssistantPanel : UserControl
    {
        internal sealed class RobotPalette
        {
            public Color Background;
            public Color FaceTop;
            public Color FaceBottom;
            public Color FaceHighlight;
            public Color Outline;
            public Color Shadow;
            public Color EyeSurface;
            public Color Input;
            public Color Text;
            public Color SecondaryText;
            public Color Orange;
            public Color Blue;
            public Color Green;
            public Color Red;
            public Color Unknown;
        }

        private static readonly RobotPalette DarkPalette = new RobotPalette
        {
            Background = Color.FromArgb(38, 39, 43),
            FaceTop = Color.FromArgb(116, 119, 127),
            FaceBottom = Color.FromArgb(72, 74, 82),
            FaceHighlight = Color.FromArgb(160, 163, 171),
            Outline = Color.FromArgb(37, 34, 46),
            Shadow = Color.FromArgb(87, 79, 102),
            EyeSurface = Color.FromArgb(226, 228, 232),
            Input = Color.FromArgb(30, 30, 33),
            Text = Color.FromArgb(245, 245, 247),
            SecondaryText = Color.FromArgb(67, 68, 74),
            Orange = Color.FromArgb(245, 133, 45),
            Blue = Color.FromArgb(0, 122, 204),
            Green = Color.FromArgb(54, 190, 91),
            Red = Color.FromArgb(220, 67, 67),
            Unknown = Color.FromArgb(145, 148, 153)
        };

        private static readonly RobotPalette LightPalette = new RobotPalette
        {
            Background = Color.FromArgb(241, 242, 244),
            FaceTop = Color.FromArgb(250, 250, 251),
            FaceBottom = Color.FromArgb(202, 205, 211),
            FaceHighlight = Color.White,
            Outline = Color.FromArgb(53, 51, 61),
            Shadow = Color.FromArgb(126, 117, 141),
            EyeSurface = Color.FromArgb(255, 255, 255),
            Input = Color.White,
            Text = Color.FromArgb(35, 36, 40),
            SecondaryText = Color.FromArgb(72, 72, 78),
            Orange = Color.FromArgb(225, 111, 32),
            Blue = Color.FromArgb(0, 122, 204),
            Green = Color.FromArgb(30, 148, 66),
            Red = Color.FromArgb(194, 45, 45),
            Unknown = Color.FromArgb(135, 139, 145)
        };

        private readonly IAssistantWorkflow _workflow;
        private readonly RobotPalette _palette;
        private readonly TableLayoutPanel _root;
        private readonly RobotFace _face;
        private readonly Panel _hostBackground;
        private readonly FlowLayoutPanel _navigation;
        private readonly Button _chatButton;
        private readonly Button _savedButton;
        private readonly Button _libraryButton;
        private readonly Button _diagnoseButton;
        private readonly Button _newChatButton;
        private readonly ToolTip _toolTip;
        private readonly Panel _contentHost;
        private readonly TableLayoutPanel _forehead;
        private readonly TableLayoutPanel _faceContent;
        private readonly TableLayoutPanel _eyesLayout;
        private readonly RobotEye _conversationEye;
        private readonly RobotEye _statusEye;
        private readonly RobotMouth _mouth;
        private readonly Label _promptLabel;
        private readonly TableLayoutPanel _mouthContent;
        private readonly Panel _mouthInputFrame;
        private readonly Panel _mouthTeeth;
        private readonly TextBox _promptInput;
        private readonly Button _sendButton;
        private readonly Button _eyeConversationButton;
        private readonly Button _eyeStatusButton;
        private readonly Button _collapseEyeButton;
        private readonly RobotConsole _console;
        private Button _floatingCloseButton;
        private readonly ListBox _savedList;
        private readonly ListBox _libraryList;
        private readonly StringBuilder _statusBuffer = new StringBuilder();
        private readonly List<Font> _ownedFonts = new List<Font>();
        private Control _savedView;
        private Control _libraryView;
        private VaultButtonStrip _vaultStrip;
        private bool _eyeExpanded;
        private bool _regionExpanded;
        private Size _regionSize;
        private bool _layoutInProgress;
        private int _activeResizeGripWidth;
        private bool _conversationEyeActive = true;
        private bool _uiInitialized;
        private bool _disposed;
        private bool _floatingPreviewMode;

        internal event EventHandler EyeExpansionChanged;

        internal bool IsEyeExpanded
        {
            get { return _eyeExpanded; }
        }

        internal bool IsFloatingPreviewMode
        {
            get { return _floatingPreviewMode; }
        }

        internal void LayoutAfterPreviewResize()
        {
            if (_uiInitialized && !_disposed && !IsDisposed && !Disposing)
                LayoutRoot();
        }

        public RobotAssistantPanel(
            AssistantPanel assistantPanel,
            Inventor.Application application)
            : this((IAssistantWorkflow)assistantPanel, application)
        {
        }

        internal RobotAssistantPanel(
            IAssistantWorkflow workflow,
            Inventor.Application application)
        {
            _workflow = workflow;
            _palette = ResolvePalette(application);
            AutoScroll = true;
            BackColor = _palette.Background;
            ForeColor = _palette.Text;
            Font = CreateFont("Segoe UI", 9f);
            _toolTip = new ToolTip();

            _hostBackground = new Panel();
            _hostBackground.BackColor = _palette.Background;
            _hostBackground.Dock = DockStyle.Fill;
            Controls.Add(_hostBackground);

            _root = new TableLayoutPanel();
            _root.Location = Point.Empty;
            _root.Dock = DockStyle.None;
            _root.ColumnCount = 1;
            _root.RowCount = 2;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 302f));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
            _root.BackColor = _palette.Background;

            _face = new RobotFace(_palette);
            _face.Margin = Padding.Empty;
            _face.Dock = DockStyle.Fill;

            _navigation = new FlowLayoutPanel();
            _navigation.Dock = DockStyle.Fill;
            _navigation.FlowDirection = FlowDirection.LeftToRight;
            _navigation.WrapContents = false;
            _navigation.AutoScroll = false;
            _navigation.Margin = Padding.Empty;
            _navigation.Padding = new Padding(0, 1, 0, 0);
            _navigation.BackColor = Color.Transparent;

            _chatButton = CreateButton("Chat", 44);
            _savedButton = CreateButton("Saved", 48);
            _libraryButton = CreateButton("Library", 52);
            _diagnoseButton = CreateButton("Diagnose", 65);
            _diagnoseButton.BackColor = _palette.Orange;
            _diagnoseButton.Click += (s, e) =>
                _workflow.StartNewChatOrDiagnose();
            _diagnoseButton.AccessibleName = "Diagnose AI connection";
            _toolTip.SetToolTip(_diagnoseButton,
                "Run the existing assistant connection diagnosis.");
            _newChatButton = CreateButton("New Chat", 70);
            _newChatButton.BackColor = _palette.Blue;
            _newChatButton.Click += (s, e) =>
                _workflow.StartNewChatOrDiagnose();
            _newChatButton.AccessibleName = "Start a new chat";
            _toolTip.SetToolTip(_newChatButton,
                "Start a new conversation.");

            _chatButton.Click += (s, e) => ShowChatView();
            _savedButton.Click += (s, e) => ShowSavedView();
            _libraryButton.Click += (s, e) => ShowLibraryView();
            _chatButton.AccessibleName = "Chat";
            _savedButton.AccessibleName = "Saved prompts";
            _libraryButton.AccessibleName = "Prompt library";
            _toolTip.SetToolTip(_chatButton, "Show the conversation eyes and prompt.");
            _toolTip.SetToolTip(_savedButton, "Browse and run saved prompts.");
            _toolTip.SetToolTip(_libraryButton, "Browse and run library prompts.");
            _chatButton.TabIndex = 0;
            _savedButton.TabIndex = 1;
            _libraryButton.TabIndex = 2;
            _diagnoseButton.TabIndex = 3;
            _newChatButton.TabIndex = 4;
            _eyeConversationButton = CreateButton("Response", 67);
            _eyeStatusButton = CreateButton("Status", 56);
            _collapseEyeButton = CreateButton("Collapse", 78);
            _collapseEyeButton.AccessibleName = "Collapse expanded eye";
            _collapseEyeButton.Click += (s, e) => CollapseExpandedEye();
            _toolTip.SetToolTip(_collapseEyeButton,
                "Return to the compact two-eye robot face.");
            _collapseEyeButton.Visible = false;
            _collapseEyeButton.TabIndex = 6;
            _eyeConversationButton.Visible = false;
            _eyeStatusButton.Visible = false;
            _eyeConversationButton.Click += (s, e) =>
                SelectEyeByNavigation(true);
            _eyeStatusButton.Click += (s, e) =>
                SelectEyeByNavigation(false);
            _toolTip.SetToolTip(_eyeConversationButton,
                "Show Conversation / AI Response.");
            _toolTip.SetToolTip(_eyeStatusButton,
                "Show Status / Last Action.");
            _eyeConversationButton.Visible = false;
            _eyeStatusButton.Visible = false;
            _navigation.Controls.AddRange(new Control[]
            {
                _chatButton, _savedButton, _libraryButton, _diagnoseButton
            });

            _forehead = new TableLayoutPanel();
            _forehead.Dock = DockStyle.Fill;
            _forehead.ColumnCount = 3;
            _forehead.RowCount = 2;
            _forehead.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _forehead.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82f));
            _forehead.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34f));
            _forehead.RowStyles.Add(new RowStyle(SizeType.Absolute, 27f));
            _forehead.RowStyles.Add(new RowStyle(SizeType.Absolute, 27f));
            _forehead.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            _forehead.BackColor = Color.Transparent;
            _forehead.Controls.Add(_navigation, 0, 0);
            _forehead.SetRowSpan(_navigation, 2);
            _collapseEyeButton.Dock = DockStyle.None;
            _collapseEyeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _collapseEyeButton.Size = new Size(70, 25);
            _collapseEyeButton.Margin = new Padding(0, 0, 2, 0);
            _collapseEyeButton.Visible = false;
            _forehead.Controls.Add(_collapseEyeButton, 1, 1);
            _forehead.Controls.Add(_eyeConversationButton, 1, 0);
            _forehead.Controls.Add(_eyeStatusButton, 2, 1);

            _conversationEye = new RobotEye(
                "Chat", _palette, false, true);
            _statusEye = new RobotEye(
                "Status", _palette, true, true);
            _conversationEye.Activated += Eye_Activated;
            _statusEye.Activated += Eye_Activated;
            _conversationEye.SetSummary("Ready for a prompt.");
            _statusEye.SetSummary("Waiting for activity.");

            _eyesLayout = new TableLayoutPanel();
            _eyesLayout.Dock = DockStyle.Fill;
            _eyesLayout.ColumnCount = 2;
            _eyesLayout.RowCount = 1;
            _eyesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _eyesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _eyesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _eyesLayout.Margin = new Padding(2, 3, 2, 2);
            _eyesLayout.BackColor = Color.Transparent;
            _eyesLayout.Controls.Add(_conversationEye, 0, 0);
            _eyesLayout.Controls.Add(_statusEye, 1, 0);

            _contentHost = new Panel();
            _contentHost.Dock = DockStyle.Fill;
            _contentHost.BackColor = Color.Transparent;
            _contentHost.Controls.Add(_eyesLayout);

            _promptInput = _workflow.CreateRobotPromptInput(FocusPrompt);
            _promptInput.Dock = DockStyle.None;
            _promptInput.Multiline = true;
            _promptInput.AcceptsReturn = false;
            _promptInput.AcceptsTab = false;
            _promptInput.ScrollBars = ScrollBars.Vertical;
            _promptInput.WordWrap = true;
            _promptInput.Font = new Font("Segoe UI", 9.5f);
            _promptInput.BackColor = _palette.Input;
            _promptInput.ForeColor = _palette.Text;
            _promptInput.BorderStyle = BorderStyle.None;
            _promptInput.AccessibleName = "Assistant prompt";
            _sendButton = new RobotSendButton(_palette);
            _sendButton.Width = 68;
            _sendButton.Height = 30;
            _sendButton.BackColor = _palette.Orange;
            _sendButton.ForeColor = Color.White;
            _sendButton.AccessibleName = "Send prompt";
            _sendButton.Click += async (s, e) =>
                await _workflow.SendPromptFromRobotAsync(_promptInput);
            _toolTip.SetToolTip(_sendButton, "Send this prompt to the assistant.");

            _mouth = new RobotMouth(_palette);
            _mouth.Dock = DockStyle.Fill;
            _mouth.Padding = Padding.Empty;
            _mouthContent = new TableLayoutPanel();
            TableLayoutPanel mouthContent = _mouthContent;
            mouthContent.Dock = DockStyle.Fill;
            mouthContent.ColumnCount = 2;
            mouthContent.RowCount = 3;
            mouthContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mouthContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58f));
            mouthContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            mouthContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            // Preserve a dedicated slim tooth row for mouth teeth rendering.
            mouthContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));
            mouthContent.Padding = new Padding(14, 5, 14, 5);
            mouthContent.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            mouthContent.BackColor = Color.Transparent;
            _promptLabel = new Label();
            _promptLabel.Text = "PROMPT";
            _promptLabel.Dock = DockStyle.Fill;
            _promptLabel.Font = CreateFont("Segoe UI", 8f, FontStyle.Bold);
            _promptLabel.ForeColor = _palette.Text;
            _promptLabel.TextAlign = ContentAlignment.MiddleLeft;
            _promptLabel.AccessibleName = "Robot mouth prompt input";
            mouthContent.Controls.Add(_promptLabel, 0, 0);
            mouthContent.SetColumnSpan(_promptLabel, 2);

            _mouthInputFrame = new Panel();
            _mouthInputFrame.Dock = DockStyle.Fill;
            _mouthInputFrame.Padding = new Padding(8, 5, 20, 5);
            _mouthInputFrame.BackColor = _palette.Input;
            _mouthInputFrame.Paint += (s, e) =>
            {
                Rectangle border = _mouthInputFrame.ClientRectangle;
                border.Width -= 1;
                border.Height -= 1;
                if (border.Width > 0 && border.Height > 0)
                {
                    using (Pen silver = new Pen(Color.FromArgb(190, 198, 208)))
                        e.Graphics.DrawRectangle(silver, border);
                }
            };
            // Keep the prompt label inset to align visually with the input frame.
            _promptLabel.Padding = new Padding(5, 0, 0, 0);
            _promptInput.Dock = DockStyle.Fill;
            _mouthInputFrame.Controls.Add(_promptInput);
            mouthContent.Controls.Add(_mouthInputFrame, 0, 1);
            mouthContent.Controls.Add(_sendButton, 1, 1);
            _mouthInputFrame.Margin = new Padding(0, 0, 5, 0);
            _sendButton.Dock = DockStyle.None;
            _sendButton.Size = new Size(52, 25);
            _sendButton.Anchor = AnchorStyles.None;
            _sendButton.Margin = new Padding(3, 0, 0, 0);
            _mouthContent = mouthContent;
            _mouthTeeth = new Panel();
            _mouthTeeth.Dock = DockStyle.Fill;
            _mouthTeeth.BackColor = Color.Transparent;
            _mouthTeeth.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int count = 7;
                int gap = 3;
                int toothWidth = Math.Max(3,
                    (Math.Max(1, _mouthTeeth.ClientSize.Width) -
                        gap * (count - 1)) / count);
                int total = toothWidth * count + gap * (count - 1);
                int x = (_mouthTeeth.ClientSize.Width - total) / 2;
                int y = Math.Max(0, (_mouthTeeth.ClientSize.Height - 5) / 2);
                using (SolidBrush orange = new SolidBrush(_palette.Orange))
                {
                    for (int i = 0; i < count; i++)
                    {
                        Rectangle toothBounds = new Rectangle(
                            x + i * (toothWidth + gap), y, toothWidth, 5);
                        using (GraphicsPath tooth = RobotEye.Rounded(
                            toothBounds, 2))
                            e.Graphics.FillPath(orange, tooth);
                    }
                }
            };
            mouthContent.Controls.Add(_mouthTeeth, 0, 2);
            _mouth.Controls.Add(mouthContent);
            _mouthContent.Resize += (s, e) => LayoutMouthChildren();

            _faceContent = new TableLayoutPanel();
            _faceContent.Dock = DockStyle.Fill;
            _faceContent.ColumnCount = 1;
            _faceContent.RowCount = 3;
            _faceContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _faceContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            _faceContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 126f));
            _faceContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 122f));
            _faceContent.BackColor = Color.Transparent;
            _faceContent.Controls.Add(_forehead, 0, 0);
            _faceContent.SetColumnSpan(_forehead, 1);
            _faceContent.Controls.Add(_contentHost, 0, 1);
            _faceContent.SetColumnSpan(_contentHost, 1);
            _faceContent.Controls.Add(_mouth, 0, 2);
            _mouth.Dock = DockStyle.Fill;
            _face.SetContent(_faceContent);
            _faceContent.Resize += FaceContent_Resize;
            _mouth.Resize += Mouth_Resize;

            _savedList = CreateListBox();
            _libraryList = CreateListBox();
            BuildSavedView();
            BuildLibraryView();
            _vaultStrip = _workflow.CreateVaultButtonStrip();
            _vaultStrip.Margin = Padding.Empty;
            _vaultStrip.BackColor = Color.Transparent;
            AddVaultToolTips(_vaultStrip);
            _console = new RobotConsole(_palette, _vaultStrip);
            _console.Dock = DockStyle.Fill;
            _console.Margin = Padding.Empty;

            _root.Controls.Add(_face, 0, 0);
            _root.Controls.Add(_console, 0, 1);
            _hostBackground.Controls.Add(_root);
            _hostBackground.SendToBack();
            _root.BringToFront();

            _workflow.TranscriptAppended += Workflow_TranscriptAppended;
            _workflow.TranscriptCleared += Workflow_TranscriptCleared;
            _workflow.StatusChanged += Workflow_StatusChanged;
            _workflow.SavedPromptsChanged += Workflow_SavedPromptsChanged;
            _workflow.BusyChanged += Workflow_BusyChanged;

            _conversationEye.SetDetails(_workflow.TranscriptText);
            _statusBuffer.Append(_workflow.TranscriptText);
            UpdateStatus();
            UpdateBusyState();
            UpdateSummaries();

            _floatingCloseButton = new Button();
            _floatingCloseButton.Text = "X";
            _floatingCloseButton.AccessibleName = "Close Robot Preview";
            _floatingCloseButton.AccessibleDescription =
                "Closes only the floating Robot Preview.";
            _toolTip.SetToolTip(_floatingCloseButton, "Close Robot Preview.");
            _floatingCloseButton.Size = new Size(28, 24);
            _floatingCloseButton.Anchor = AnchorStyles.None;
            _floatingCloseButton.BackColor = Color.FromArgb(159, 49, 49);
            _floatingCloseButton.ForeColor = Color.White;
            _floatingCloseButton.FlatStyle = FlatStyle.Flat;
            _floatingCloseButton.TabStop = true;
            _floatingCloseButton.TabIndex = 5;
            _floatingCloseButton.Click += (s, e) =>
            {
                Form preview = FindForm();
                if (preview != null && !preview.IsDisposed)
                    preview.Close();
            };
            _forehead.Controls.Add(_floatingCloseButton, 2, 0);
            _floatingCloseButton.Dock = DockStyle.None;
            _floatingCloseButton.Size = new Size(28, 24);
            _floatingCloseButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _floatingCloseButton.Margin = Padding.Empty;
            _floatingCloseButton.FlatAppearance.BorderColor = _palette.Shadow;
            _floatingCloseButton.FlatAppearance.BorderSize = 1;
            _floatingCloseButton.Font = CreateFont("Segoe UI", 9f, FontStyle.Bold);
            _floatingCloseButton.BringToFront();
            _face.SendToBack();
            _forehead.BringToFront();
            _contentHost.BringToFront();
            _mouth.BringToFront();
            _console.BringToFront();
            _floatingCloseButton.BringToFront();
            _collapseEyeButton.Dock = DockStyle.None;
            _collapseEyeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _toolTip.SetToolTip(_floatingCloseButton,
                "Close the floating Robot Preview only.");

            _uiInitialized = true;
            _promptInput.TextChanged += PromptInput_TextChanged;
            MinimumSize = new Size(280, 300);
            LayoutRoot();
            ArrangeEyes();
            _forehead.Resize += Forehead_Resize;
            _eyesLayout.Resize += EyeContainer_Resize;
            Resize += RobotAssistantPanel_Resize;
        }

        private void Forehead_Resize(object sender, EventArgs e)
        {
            if (!_uiInitialized || IsDisposed || Disposing || _layoutInProgress)
                return;
            LayoutRoot();
        }

        private void RobotAssistantPanel_Resize(object sender, EventArgs e)
        {
            if (!_uiInitialized || IsDisposed || Disposing)
                return;
            LayoutRoot();
        }

        private void EyeContainer_Resize(object sender, EventArgs e)
        {
            if (!_uiInitialized || IsDisposed || Disposing || _layoutInProgress)
                return;
            ArrangeEyes();
        }

        private void FaceContent_Resize(object sender, EventArgs e)
        {
            if (!_uiInitialized || IsDisposed || Disposing || _layoutInProgress)
                return;
            LayoutRoot();
        }

        private void Mouth_Resize(object sender, EventArgs e)
        {
            if (!_uiInitialized || IsDisposed || Disposing || _layoutInProgress)
                return;
            LayoutRoot();
        }

        private void PromptInput_TextChanged(object sender, EventArgs e)
        {
            if (!_uiInitialized || IsDisposed || Disposing || _layoutInProgress)
                return;
            LayoutRoot();
        }

        private void ApplyFaceFeatureBounds()
        {
            if (!_uiInitialized || _layoutInProgress
                || _faceContent == null || _faceContent.IsDisposed
                || _forehead == null || _contentHost == null
                || _mouth == null || _mouth.IsDisposed)
                return;
            ApplyFaceFeatureBoundsCore();
        }

        private void ApplyFaceFeatureBoundsCore()
        {
            if (_faceContent == null || _faceContent.IsDisposed
                || _forehead == null || _forehead.IsDisposed
                || _contentHost == null || _contentHost.IsDisposed
                || _mouth == null || _mouth.IsDisposed
                || _promptInput == null || _promptInput.IsDisposed
                || _faceContent.RowStyles.Count < 3)
                return;
            _faceContent.SuspendLayout();
            try
            {
                int width = Math.Max(1, _faceContent.ClientSize.Width);
                int height = Math.Max(1, _faceContent.ClientSize.Height);
                int foreheadHeight = 54;
                int mouthHeight = _eyeExpanded ? 104 : 108;
                int eyesHeight = _eyeExpanded ? 190
                    : (width < 350 ? 82 : 95);
                int used = foreheadHeight + eyesHeight + mouthHeight;
                if (used > height)
                {
                    eyesHeight = Math.Max(58, height - foreheadHeight - mouthHeight);
                    if (foreheadHeight + eyesHeight + mouthHeight > height)
                        mouthHeight = Math.Max(48, height - foreheadHeight - eyesHeight);
                }
                _faceContent.RowStyles[0].SizeType = SizeType.Absolute;
                _faceContent.RowStyles[0].Height = foreheadHeight;
                _faceContent.RowStyles[1].SizeType = SizeType.Absolute;
                _faceContent.RowStyles[1].Height = eyesHeight;
                _faceContent.RowStyles[2].SizeType = SizeType.Absolute;
                _faceContent.RowStyles[2].Height = mouthHeight;
            }
            finally
            {
                _faceContent.ResumeLayout(true);
            }
        }

        private RobotPalette ResolvePalette(Inventor.Application application)
        {
            try
            {
                string name = application.ThemeManager.ActiveTheme.Name;
                if (name.IndexOf("light", StringComparison.OrdinalIgnoreCase) >= 0)
                    return LightPalette;
                if (name.IndexOf("dark", StringComparison.OrdinalIgnoreCase) >= 0)
                    return DarkPalette;
            }
            catch
            {
            }
            return DarkPalette;
        }

        private Button CreateButton(string text, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = width;
            button.Height = 27;
            button.Margin = new Padding(2, 1, 4, 1);
            button.Padding = new Padding(4, 1, 4, 1);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = _palette.Shadow;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = _palette.FaceBottom;
            button.ForeColor = _palette.Text;
            button.Font = CreateFont("Segoe UI", 8.5f, FontStyle.Bold);
            button.TabStop = true;
            return button;
        }

        private Font CreateFont(
            string family,
            float size,
            FontStyle style = FontStyle.Regular)
        {
            Font font = new Font(family, size, style);
            _ownedFonts.Add(font);
            return font;
        }

        private void BuildSavedView()
        {
            TableLayoutPanel view = CreateListView();
            Label title = CreateListTitle("SAVED PROMPTS");
            FlowLayoutPanel actions = CreateActionRow();
            Button run = CreateButton("Run", 62);
            Button delete = CreateButton("Delete", 68);
            run.BackColor = _palette.Blue;
            run.Click += (s, e) =>
            {
                if (_savedList.SelectedIndex >= 0)
                    _workflow.RunSavedPromptAt(_savedList.SelectedIndex);
            };
            delete.Click += (s, e) =>
            {
                if (_savedList.SelectedIndex >= 0)
                    _workflow.DeleteSavedPromptAt(_savedList.SelectedIndex);
            };
            _savedList.DoubleClick += (s, e) =>
            {
                if (_savedList.SelectedIndex >= 0)
                    _workflow.RunSavedPromptAt(_savedList.SelectedIndex);
            };
            actions.Controls.Add(run);
            actions.Controls.Add(delete);
            view.Controls.Add(title, 0, 0);
            view.Controls.Add(_savedList, 0, 1);
            view.Controls.Add(actions, 0, 2);
            _savedView = view;
        }

        private void BuildLibraryView()
        {
            TableLayoutPanel view = CreateListView();
            Label title = CreateListTitle("PROMPT LIBRARY");
            FlowLayoutPanel actions = CreateActionRow();
            Button run = CreateButton("Run", 62);
            run.BackColor = _palette.Blue;
            run.Click += (s, e) =>
            {
                if (_libraryList.SelectedIndex >= 0)
                    _workflow.RunLibraryPromptAt(_libraryList.SelectedIndex);
            };
            _libraryList.DoubleClick += (s, e) =>
            {
                if (_libraryList.SelectedIndex >= 0)
                    _workflow.RunLibraryPromptAt(_libraryList.SelectedIndex);
            };
            foreach (string item in _workflow.GetLibraryPrompts())
                _libraryList.Items.Add(item);
            actions.Controls.Add(run);
            view.Controls.Add(title, 0, 0);
            view.Controls.Add(_libraryList, 0, 1);
            view.Controls.Add(actions, 0, 2);
            _libraryView = view;
        }

        private TableLayoutPanel CreateListView()
        {
            TableLayoutPanel view = new TableLayoutPanel();
            view.Dock = DockStyle.Fill;
            view.ColumnCount = 1;
            view.RowCount = 3;
            view.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            view.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            view.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            view.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            view.Padding = new Padding(7, 4, 7, 4);
            view.BackColor = Color.Transparent;
            return view;
        }

        private Label CreateListTitle(string text)
        {
            Label title = new Label();
            title.Text = text;
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            title.ForeColor = _palette.Text;
            title.TextAlign = ContentAlignment.MiddleLeft;
            return title;
        }

        private FlowLayoutPanel CreateActionRow()
        {
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.FlowDirection = FlowDirection.LeftToRight;
            row.WrapContents = true;
            row.BackColor = Color.Transparent;
            return row;
        }

        private ListBox CreateListBox()
        {
            ListBox list = new ListBox();
            list.Dock = DockStyle.Fill;
            list.BackColor = _palette.Input;
            list.ForeColor = _palette.Text;
            list.Font = new Font("Segoe UI", 9f);
            list.BorderStyle = BorderStyle.FixedSingle;
            list.IntegralHeight = false;
            return list;
        }

        private void AddVaultToolTips(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (child is Button)
                {
                    string originalLabel = child.Text;
                    if (originalLabel.IndexOf("Check Out",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                        child.Text = "Check Out";
                    else if (originalLabel.IndexOf("Check In",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                        child.Text = "Check In";
                    else if (originalLabel.IndexOf("Status",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                        child.Text = "Status";
                    else
                        child.Text = "Undo";
                    child.Font = CreateFont("Segoe UI", 8.5f, FontStyle.Bold);
                    string label = child.Text;
                    child.TabStop = true;
                    if (label.IndexOf("Check Out", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        child.AccessibleName = "Check Out active document";
                        _toolTip.SetToolTip(child, "Check Out the active document.");
                    }
                    else if (label.IndexOf("Check In", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        child.AccessibleName = "Check In active document";
                        _toolTip.SetToolTip(child, "Check In the active document.");
                    }
                    else if (label.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        child.AccessibleName = "Show Vault status";
                        _toolTip.SetToolTip(child, "Show Vault status for the active document.");
                    }
                    else
                    {
                        child.AccessibleName = "Undo Check Out";
                        _toolTip.SetToolTip(child, "Undo Check Out for the active document.");
                    }
                }
                AddVaultToolTips(child);
            }
        }

        internal void SetActive(bool active)
        {
            _workflow.SetRobotViewActive(active);
            if (active)
                FocusPrompt();
        }

        internal void SetFloatingPreviewMode(bool enabled)
        {
            _floatingPreviewMode = enabled;
            LayoutRoot();
        }

        internal void SetFloatingResizeGrip(bool enabled)
        {
            if (_console != null && !_console.IsDisposed)
                _console.SetResizeGripSpace(enabled);
        }

        internal void SetFloatingWindowDragHandler(Action beginDrag)
        {
            if (_forehead == null || _forehead.IsDisposed
                || beginDrag == null)
                return;
            MouseEventHandler drag = (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    beginDrag();
            };
            _forehead.MouseDown += drag;
            _toolTip.SetToolTip(_forehead,
                "Drag an unobstructed forehead area to move the floating preview.");
        }

        private void LayoutRoot()
        {
            if (!_uiInitialized
                || _layoutInProgress
                || _disposed
                || IsDisposed
                || Disposing
                || _root == null
                || _root.IsDisposed
                || _hostBackground == null
                || _hostBackground.IsDisposed
                || _face == null
                || _face.IsDisposed
                || _faceContent == null
                || _faceContent.IsDisposed
                || _forehead == null
                || _forehead.IsDisposed
                || _contentHost == null
                || _contentHost.IsDisposed
                || _eyesLayout == null
                || _eyesLayout.IsDisposed
                || _conversationEye == null
                || _conversationEye.IsDisposed
                || _statusEye == null
                || _statusEye.IsDisposed
                || _mouth == null
                || _mouth.IsDisposed
                || _promptInput == null
                || _promptInput.IsDisposed
                || _sendButton == null
                || _sendButton.IsDisposed
                || _navigation == null
                || _navigation.IsDisposed
                || _chatButton == null
                || _chatButton.IsDisposed
                || _savedButton == null
                || _savedButton.IsDisposed
                || _libraryButton == null
                || _libraryButton.IsDisposed
                || _newChatButton == null
                || _newChatButton.IsDisposed
                || _diagnoseButton == null
                || _diagnoseButton.IsDisposed
                || _eyeConversationButton == null
                || _eyeConversationButton.IsDisposed
                || _eyeStatusButton == null
                || _eyeStatusButton.IsDisposed
                || _collapseEyeButton == null
                || _collapseEyeButton.IsDisposed
                || _mouthContent == null
                || _mouthContent.IsDisposed
                || _mouthInputFrame == null
                || _mouthInputFrame.IsDisposed
                || _promptLabel == null
                || _promptLabel.IsDisposed
                || _savedList == null
                || _savedList.IsDisposed
                || _libraryList == null
                || _libraryList.IsDisposed
                || _vaultStrip == null
                || _vaultStrip.IsDisposed
                || _console == null
                || _console.IsDisposed
                || _floatingCloseButton == null
                || _floatingCloseButton.IsDisposed
                || !IsHandleCreated
                || ClientSize.Width <= 0
                || ClientSize.Height <= 0)
            {
                return;
            }

            if (_layoutInProgress)
                return;
            _layoutInProgress = true;
            _root.SuspendLayout();
            _face.SuspendLayout();
            _faceContent.SuspendLayout();
            _forehead.SuspendLayout();
            _contentHost.SuspendLayout();
            _eyesLayout.SuspendLayout();
            _mouth.SuspendLayout();
            _console.SuspendLayout();
            try
            {
            ApplyFaceFeatureBoundsCore();

            int width = Math.Max(280, ClientSize.Width);
            int lines = 1;
            if (_promptInput != null && !_promptInput.IsDisposed && _promptInput.Lines != null)
                lines = _promptInput.Lines.Length;
            int faceContentHeight = 54 + (_eyeExpanded ? 190
                : (width < 350 ? 82 : 98))
                + (int)(_eyeExpanded ? 104 : 108);
            int compactHeight = Math.Max(300,
                Math.Min(390, (int)(faceContentHeight * 1.24f)));
            int height = _floatingPreviewMode
                ? Math.Max(300, ClientSize.Height)
                : (_eyeExpanded ? compactHeight + 90 : compactHeight);
            _root.SetBounds(0, 0, width, height);
            _root.Location = Point.Empty;
            _root.Dock = DockStyle.None;
            _root.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            AutoScrollMinSize = new Size(width, height);
            bool narrowFace = width < 350
                || (_floatingPreviewMode && ClientSize.Height < 330);
            _activeResizeGripWidth = _floatingPreviewMode ? 18 : 0;
            _faceContent.SuspendLayout();
            _faceContent.RowStyles[0].Height = 54f;
            _faceContent.RowStyles[0].SizeType = SizeType.Absolute;
            _faceContent.RowStyles[1].Height = _eyeExpanded ? 190f
                : (narrowFace ? 82f : 98f);
            _faceContent.RowStyles[1].SizeType = SizeType.Absolute;
            _faceContent.RowStyles[2].Height = _eyeExpanded ? 104f : 108f;
            _faceContent.RowStyles[2].SizeType = SizeType.Absolute;
            _forehead.Height = 54;
            _navigation.Height = 27;
            _faceContent.ResumeLayout(false);
            _root.RowStyles[0].SizeType = SizeType.Absolute;
            _root.RowStyles[0].Height = height - 58;
            // No-op: preserve 58-pixel console row height
            _root.RowStyles[1].SizeType = SizeType.Absolute;
            _root.RowStyles[1].Height = 58;
            _root.PerformLayout();
            _face.PerformLayout();
            _faceContent.PerformLayout();
            int foreheadHeight = Math.Min(54, _faceContent.ClientSize.Height);
            int mouthHeight = Math.Min(
                _eyeExpanded ? 104 : 108,
                Math.Max(1, _faceContent.ClientSize.Height - foreheadHeight - 58));
            int desiredEyesHeight = _eyeExpanded ? 190 : (narrowFace ? 82 : 98);
            int reservedForEyes = Math.Min(desiredEyesHeight,
                Math.Max(1, _faceContent.ClientSize.Height - foreheadHeight - mouthHeight));
            _contentHost.Dock = DockStyle.Fill;
            _forehead.Dock = DockStyle.Fill;
            _faceContent.RowStyles[0].Height = foreheadHeight;
            _faceContent.RowStyles[1].Height = reservedForEyes;
            _faceContent.RowStyles[2].Height = mouthHeight;
            _faceContent.PerformLayout();
            _mouth.Dock = DockStyle.Fill;
            _mouth.Margin = new Padding(18, 0, 18, 0);
            _forehead.PerformLayout();
            _contentHost.PerformLayout();
            _mouth.PerformLayout();
            _console.PerformLayout();
            LayoutMouthChildren();
            LayoutForeheadControls();
            ArrangeEyes();
            _console.SetGripReservation(_activeResizeGripWidth);
            _console.LayoutVaultStrip(_vaultStrip);
            _face.SendToBack();
            _forehead.BringToFront();
            _contentHost.BringToFront();
            _mouth.BringToFront();
            _console.BringToFront();
            _floatingCloseButton.BringToFront();
            _face.Invalidate();
            _console.Invalidate();
            }
            finally
            {
                _console.ResumeLayout(true);
                _mouth.ResumeLayout(true);
                _eyesLayout.ResumeLayout(true);
                _contentHost.ResumeLayout(true);
                _forehead.ResumeLayout(true);
                _faceContent.ResumeLayout(true);
                _face.ResumeLayout(true);
                _root.ResumeLayout(true);
                _layoutInProgress = false;
            }

            if (_floatingPreviewMode)
            {
                RobotPreviewForm preview = FindForm() as RobotPreviewForm;
                if (preview != null)
                    preview.InvalidateSilhouette();
            }
        }

        private void ArrangeEyes()
        {
            if (!_uiInitialized
                || _disposed || IsDisposed || Disposing
                || _eyesLayout == null || _eyesLayout.IsDisposed
                || _conversationEye == null || _conversationEye.IsDisposed
                || _statusEye == null || _statusEye.IsDisposed
                || _contentHost == null || _contentHost.IsDisposed
                || _eyeConversationButton == null || _eyeConversationButton.IsDisposed
                || _eyeStatusButton == null || _eyeStatusButton.IsDisposed
                || _collapseEyeButton == null || _collapseEyeButton.IsDisposed
                || _floatingCloseButton == null || _floatingCloseButton.IsDisposed
                || _conversationEye.TitleControl == null
                || _conversationEye.TitleControl.IsDisposed
                || _statusEye.TitleControl == null
                || _statusEye.TitleControl.IsDisposed
                || _conversationEye.SummaryControl == null
                || _conversationEye.SummaryControl.IsDisposed
                || _statusEye.SummaryControl == null
                || _statusEye.SummaryControl.IsDisposed
                || (_eyesLayout.ClientSize.Width <= 0
                    && ClientSize.Width > 0)
                || (_eyesLayout.ClientSize.Height <= 0
                    && ClientSize.Height > 0))
                return;

            bool eyesVisible = _contentHost.Controls.Contains(_eyesLayout);
            if (_eyeExpanded && eyesVisible)
            {
                RobotEye active = _conversationEyeActive
                    ? _conversationEye : _statusEye;
                if (active.DetailsControl == null || active.DetailsControl.IsDisposed)
                    return;
            }

            _eyesLayout.SuspendLayout();
            _eyesLayout.Controls.Clear();
            _eyesLayout.ColumnStyles.Clear();
            _eyesLayout.RowStyles.Clear();
            _conversationEye.Dock = DockStyle.None;
            _statusEye.Dock = DockStyle.None;
            _conversationEye.Anchor = AnchorStyles.None;
            _statusEye.Anchor = AnchorStyles.None;
            _conversationEye.Margin = new Padding(3);
            _statusEye.Margin = new Padding(3);

            bool medium = _contentHost.ClientSize.Width >= 310
                && _contentHost.ClientSize.Width < 410;
            bool narrow = _contentHost.ClientSize.Width < 310;
            bool chatVisible = ReferenceEquals(
                _contentHost.Controls.Count == 0
                    ? null : _contentHost.Controls[0],
                _eyesLayout);
            _diagnoseButton.Visible = _floatingPreviewMode && chatVisible;
            _navigation.WrapContents = false;
            _navigation.Controls.Clear();
            _navigation.Controls.Add(_chatButton);
            _navigation.Controls.Add(_savedButton);
            _navigation.Controls.Add(_libraryButton);
            _navigation.Controls.Add(_diagnoseButton);
            if (_floatingPreviewMode)
                _forehead.Controls.Add(_newChatButton, 1, 0);
            else
                _forehead.Controls.Remove(_newChatButton);
            _collapseEyeButton.Visible = _eyeExpanded && chatVisible;
            _eyeConversationButton.Visible = narrow && chatVisible
                && !_eyeExpanded && !_floatingPreviewMode;
            _eyeStatusButton.Visible = narrow && chatVisible
                && !_eyeExpanded && !_floatingPreviewMode;
            _newChatButton.Visible = _floatingPreviewMode && chatVisible;
            _floatingCloseButton.Visible = _floatingPreviewMode;
            if (_eyeExpanded)
            {
                _eyesLayout.ColumnCount = narrow ? 1 : 2;
                _eyesLayout.RowCount = 1;
                _eyesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,
                    narrow ? 100f : (medium ? 64f : 72f)));
                if (!narrow)
                    _eyesLayout.ColumnStyles.Add(new ColumnStyle(
                        SizeType.Percent, medium ? 36f : 28f));
                _eyesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                RobotEye active = _conversationEyeActive
                    ? _conversationEye : _statusEye;
                RobotEye other = _conversationEyeActive
                    ? _statusEye : _conversationEye;
                active.Visible = true;
                active.Expanded = true;
                other.Expanded = false;
                active.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
                    | AnchorStyles.Left | AnchorStyles.Right;
                _eyesLayout.Controls.Add(active, 0, 0);
                if (!narrow)
                {
                    other.Anchor = AnchorStyles.None;
                    int compactEye = Math.Max(56, Math.Min(
                        _eyesLayout.ClientSize.Height - 8,
                        _eyesLayout.ClientSize.Width / 4 - 8));
                    other.Size = new Size(compactEye, compactEye);
                    _eyesLayout.Controls.Add(other, 1, 0);
                }
            }
            else if (narrow)
            {
                _eyesLayout.ColumnCount = 1;
                _eyesLayout.RowCount = _eyeExpanded ? 1 : 2;
                _eyesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                if (_eyeExpanded)
                {
                    _eyesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                    RobotEye activeEye = _conversationEyeActive
                        ? _conversationEye : _statusEye;
                    activeEye.Expanded = true;
                    _eyesLayout.Controls.Add(activeEye, 0, 0);
                }
                else
                {
                    _eyesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
                    _eyesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
                    _conversationEye.Expanded = false;
                    _statusEye.Expanded = false;
                    int compactEye = Math.Max(58, Math.Min(
                        _eyesLayout.ClientSize.Height - 8,
                        _eyesLayout.ClientSize.Width - 12));
                    _conversationEye.Anchor = AnchorStyles.None;
                    _statusEye.Anchor = AnchorStyles.None;
                    _conversationEye.Size = new Size(compactEye, compactEye);
                    _statusEye.Size = new Size(compactEye, compactEye);
                    _eyesLayout.Controls.Add(_conversationEye, 0, 0);
                    _eyesLayout.Controls.Add(_statusEye, 0, 1);
                }
            }
            else
            {
                _eyesLayout.ColumnCount = 2;
                _eyesLayout.RowCount = 1;
                float conversationShare = medium ? 54f : 50f;
                _eyesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, conversationShare));
                _eyesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f - conversationShare));
                _eyesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                _conversationEye.Expanded = false;
                _statusEye.Expanded = false;
                int compactEye = Math.Max(58, Math.Min(
                    _eyesLayout.ClientSize.Height - 8,
                    _eyesLayout.ClientSize.Width / 2 - 12));
                _conversationEye.Anchor = AnchorStyles.None;
                _statusEye.Anchor = AnchorStyles.None;
                _conversationEye.Size = new Size(compactEye, compactEye);
                _statusEye.Size = new Size(compactEye, compactEye);
                _eyesLayout.Controls.Add(_conversationEye, 0, 0);
                _eyesLayout.Controls.Add(_statusEye, 1, 0);
            }

            _conversationEye.SetSelected(_conversationEyeActive);
            _statusEye.SetSelected(!_conversationEyeActive);
            _eyesLayout.ResumeLayout(false);
            _eyesLayout.PerformLayout();
            LayoutNavigation();
        }

        private void SelectEyeByNavigation(bool conversation)
        {
            SelectEye(conversation ? _conversationEye : _statusEye);
        }

        private void SelectEye(RobotEye eye)
        {
            bool conversation = ReferenceEquals(eye, _conversationEye);
            if (_eyeExpanded && conversation == _conversationEyeActive)
            {
                _eyeExpanded = false;
                _conversationEye.Expanded = false;
                _statusEye.Expanded = false;
            }
            else
            {
                _conversationEyeActive = conversation;
                _eyeExpanded = true;
                _conversationEye.Expanded = conversation;
                _statusEye.Expanded = !conversation;
            }
            ArrangeEyes();
            LayoutAfterEyeStateChange();
            EyeExpansionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void LayoutAfterEyeStateChange()
        {
            if (!_uiInitialized
                || _disposed || IsDisposed || Disposing)
                return;
            LayoutRoot();
        }

        internal void CollapseExpandedEye()
        {
            if (!_eyeExpanded || _disposed || IsDisposed || Disposing)
                return;
            _eyeExpanded = false;
            _conversationEye.Expanded = false;
            _statusEye.Expanded = false;
            ArrangeEyes();
            LayoutAfterEyeStateChange();
            EyeExpansionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void LayoutNavigation()
        {
            if (_navigation == null)
                return;
            int gap = 4;
            int actionWidth;
            int reservedWidth = 116;
            _navigation.SetBounds(0, 0, Math.Max(1,
                _forehead.ClientSize.Width - reservedWidth - 34), 54);
            int visibleButtons = 4;
            int available = Math.Max(0, _navigation.ClientSize.Width - 12);
            actionWidth = Math.Max(42, (available - gap * visibleButtons) / visibleButtons);
            _navigation.WrapContents = false;
            _navigation.AutoScroll = false;
            _chatButton.Width = actionWidth;
            _savedButton.Width = actionWidth;
            _libraryButton.Width = actionWidth;
            _diagnoseButton.Width = actionWidth;
            _newChatButton.Width = 74;
            _chatButton.Height = 27;
            _savedButton.Height = 27;
            _libraryButton.Height = 27;
            _diagnoseButton.Height = 27;
            _newChatButton.Height = 27;
            _chatButton.Margin = new Padding(2, 0, gap, 0);
            _savedButton.Margin = new Padding(2, 0, gap, 0);
            _libraryButton.Margin = new Padding(2, 0, gap, 0);
            _diagnoseButton.Margin = new Padding(2, 0, gap, 0);
            _newChatButton.Margin = new Padding(2, 0, gap, 0);
            _eyeConversationButton.Width = 74;
            _eyeConversationButton.Height = 24;
            _eyeStatusButton.Width = 74;
            _eyeStatusButton.Height = 24;
            _collapseEyeButton.SetBounds(Math.Max(0,
                _forehead.ClientSize.Width - 78), 29, 74, 24);
            _eyeConversationButton.SetBounds(Math.Max(0,
                _forehead.ClientSize.Width - 78), 2, 74, 24);
            _eyeStatusButton.SetBounds(Math.Max(0,
                _forehead.ClientSize.Width - 78), 29, 74, 24);
            _floatingCloseButton.SetBounds(Math.Max(0,
                _forehead.ClientSize.Width - 31), 2, 28, 23);
            _newChatButton.SetBounds(Math.Max(0,
                _forehead.ClientSize.Width - 78), 2, 74, 24);
            _diagnoseButton.Visible = _floatingPreviewMode
                && ReferenceEquals(_contentHost.Controls.Count == 0
                    ? null : _contentHost.Controls[0], _eyesLayout);
            _diagnoseButton.Dock = DockStyle.None;
            _newChatButton.Visible = _floatingPreviewMode
                && ReferenceEquals(_contentHost.Controls.Count == 0
                    ? null : _contentHost.Controls[0], _eyesLayout);
            _eyeConversationButton.SetBounds(
                Math.Max(0, _forehead.ClientSize.Width - 78), 2, 74, 24);
            _eyeStatusButton.SetBounds(
                Math.Max(0, _forehead.ClientSize.Width - 31), 29, 28, 24);
            _floatingCloseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _newChatButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _eyeConversationButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _eyeStatusButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            if (_floatingPreviewMode)
            {
                _newChatButton.SetBounds(Math.Max(0,
                    _forehead.ClientSize.Width - 78), 2, 74, 24);
                _floatingCloseButton.SetBounds(Math.Max(0,
                    _forehead.ClientSize.Width - 31), 2, 28, 23);
            }
            _collapseEyeButton.Visible = _floatingPreviewMode
                && _eyeExpanded
                && ReferenceEquals(_contentHost.Controls.Count == 0
                    ? null : _contentHost.Controls[0], _eyesLayout);
        }

        private void LayoutMouthChildren()
        {
            if (_mouthContent == null || _mouthContent.IsDisposed
                || _mouthInputFrame == null || _mouthInputFrame.IsDisposed
                || _mouth == null || _mouth.IsDisposed
                || _sendButton == null || _sendButton.IsDisposed)
                return;
            _mouthContent.SuspendLayout();
            try
            {
                _mouthContent.Dock = DockStyle.Fill;
                _mouthContent.PerformLayout();
                _mouthContent.PerformLayout();
            }
            finally
            {
                _mouthContent.ResumeLayout(true);
            }
        }

        private void LayoutForeheadControls()
        {
            if (!_uiInitialized || _forehead == null || _forehead.IsDisposed
                || _floatingCloseButton == null || _floatingCloseButton.IsDisposed
                || !_forehead.IsHandleCreated || !_face.IsHandleCreated)
                return;
            _floatingCloseButton.Visible = _floatingPreviewMode;
            LayoutNavigation();
        }

        private void Eye_Activated(object sender, EventArgs e)
        {
            RobotEye eye = sender as RobotEye;
            if (eye == null)
                return;
            bool conversation = ReferenceEquals(eye, _conversationEye);
            if (_eyeExpanded && conversation == _conversationEyeActive)
            {
                _eyeExpanded = false;
                _conversationEye.Expanded = false;
                _statusEye.Expanded = false;
            }
            else
            {
                _conversationEyeActive = conversation;
                _eyeExpanded = true;
                _conversationEye.Expanded = conversation;
                _statusEye.Expanded = !conversation;
            }
            ArrangeEyes();
            LayoutAfterEyeStateChange();
            EyeExpansionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ShowChatView()
        {
            _contentHost.Controls.Clear();
            _contentHost.Controls.Add(_eyesLayout);
            _eyeExpanded = false;
            _conversationEye.Expanded = false;
            _statusEye.Expanded = false;
            SetNavigationState(_chatButton);
            ArrangeEyes();
            LayoutRoot();
            FocusPrompt();
        }

        private void ShowSavedView()
        {
            RefreshSavedPrompts();
            ShowContent(_savedView, _savedButton);
        }

        private void ShowLibraryView()
        {
            ShowContent(_libraryView, _libraryButton);
        }

        private void ShowContent(Control view, Button selected)
        {
            _contentHost.Controls.Clear();
            _contentHost.Controls.Add(view);
            view.Dock = DockStyle.Fill;
            SetNavigationState(selected);
        }

        private void SetNavigationState(Button selected)
        {
            foreach (Control control in _navigation.Controls)
            {
                Button button = control as Button;
                if (button == null || button == _newChatButton)
                    continue;
                bool isSelected = button == selected;
                button.BackColor = isSelected ? _palette.Blue : _palette.FaceBottom;
                button.AccessibleDescription = isSelected ? "Selected" : null;
            }
        }

        private void Workflow_TranscriptAppended(string text)
        {
            if (_disposed || IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Workflow_TranscriptAppended), text);
                return;
            }
            _conversationEye.AppendDetails(text);
            _statusBuffer.Append(text);
            if (_statusBuffer.Length > 2400)
                _statusBuffer.Remove(0, _statusBuffer.Length - 2400);
            UpdateSummaries();
        }

        private void Workflow_TranscriptCleared()
        {
            if (_disposed || IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(Workflow_TranscriptCleared));
                return;
            }
            _conversationEye.SetDetails(string.Empty);
            _statusBuffer.Length = 0;
            UpdateSummaries();
            UpdateSummaries();
        }

        private void Workflow_StatusChanged()
        {
            if (_disposed || IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(Workflow_StatusChanged));
                return;
            }
            UpdateStatus();
            UpdateSummaries();
        }

        private void Workflow_SavedPromptsChanged()
        {
            if (_disposed || IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(Workflow_SavedPromptsChanged));
                return;
            }
            RefreshSavedPrompts();
        }

        private void Workflow_BusyChanged()
        {
            if (_disposed || IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(Workflow_BusyChanged));
                return;
            }
            UpdateBusyState();
            UpdateSummaries();
        }

        private void UpdateBusyState()
        {
            _sendButton.Enabled = !_workflow.IsBusy;
            _sendButton.Text = _workflow.IsBusy ? "Working" : "Send";
            _newChatButton.Enabled = !_workflow.IsBusy;
            _diagnoseButton.Enabled = !_workflow.IsBusy;
        }

        private void UpdateStatus()
        {
            string status = _workflow.ConnectionStatus ?? "Unknown";
            Color lamp = _palette.Unknown;
            if (status.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("disconnected", StringComparison.OrdinalIgnoreCase) >= 0)
                lamp = _palette.Red;
            else if (_workflow.IsBusy)
                lamp = _palette.Orange;
            else if (_workflow.AiAvailable)
                lamp = _palette.Green;
            else if (status.IndexOf("local", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("checking", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("connecting", StringComparison.OrdinalIgnoreCase) >= 0)
                lamp = _palette.Orange;
            else if (status.IndexOf("unknown", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("unavailable", StringComparison.OrdinalIgnoreCase) >= 0)
                lamp = _palette.Unknown;
            else
                lamp = _palette.Red;
            _face.LampColor = lamp;
            _console?.SetLampColor(lamp);
            _toolTip.SetToolTip(_face, "AI connection: " + status);
            _face.AccessibleName = "Robot Assistant. AI connection: " + status;
            _face.AccessibleDescription =
                "Connection status is indicated by the antenna lamp color.";
        }

        private void UpdateSummaries()
        {
            _conversationEye.SetSummary(CompactSummary(
                _workflow.TranscriptText, "Ready for a prompt."));
            string statusText = _workflow.ConnectionStatus ?? "Unknown";
            _statusEye.SetSummary(statusText);
            _statusEye.SetDetails(
                "AI connection: " + statusText + Environment.NewLine
                + Environment.NewLine
                + "Recent status and action output:" + Environment.NewLine
                + _statusBuffer.ToString());
        }

        private string CompactSummary(string text, string emptyText)
        {
            if (string.IsNullOrWhiteSpace(text))
                return emptyText;
            string trimmed = text.Trim();
            int start = Math.Max(0, trimmed.Length - 170);
            string summary = trimmed.Substring(start).Trim();
            if (summary.Length > 96)
                summary = summary.Substring(summary.Length - 96);
            return summary;
        }

        private void RefreshSavedPrompts()
        {
            int selectedIndex = _savedList.SelectedIndex;
            _savedList.BeginUpdate();
            _savedList.Items.Clear();
            foreach (string prompt in _workflow.GetSavedPrompts())
                _savedList.Items.Add(prompt);
            if (_savedList.Items.Count > 0)
                _savedList.SelectedIndex = Math.Min(
                    Math.Max(selectedIndex, 0), _savedList.Items.Count - 1);
            _savedList.EndUpdate();
        }

        private void FocusPrompt()
        {
            if (!ReferenceEquals(_contentHost.Controls.Count == 0
                ? null : _contentHost.Controls[0], _eyesLayout))
                ShowChatView();
            if (_promptInput.CanFocus)
            {
                _promptInput.Focus();
                _promptInput.SelectionStart = _promptInput.TextLength;
                _promptInput.SelectionLength = 0;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (!_uiInitialized)
                return;

            LayoutRoot();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (_uiInitialized)
                LayoutRoot();
        }

        internal void UpdateFloatingWindowRegion(Form previewForm)
        {
            if (!_uiInitialized
                || _disposed
                || IsDisposed
                || previewForm == null
                || previewForm.IsDisposed
                || previewForm.Disposing
                || previewForm.WindowState == FormWindowState.Minimized)
                return;

            Size size = previewForm.ClientSize;
            if (!previewForm.IsHandleCreated
                || size.Width <= 0 || size.Height <= 0)
                return;
            if (size.Width < 280 || size.Height < 250)
            {
                ReplaceFloatingRegion(previewForm, null);
                return;
            }

            try
            {
                SuspendLayout();
                try
                {
                    LayoutRoot();
                    ResumeLayout(true);
                }
                catch
                {
                    ResumeLayout(true);
                    throw;
                }
                if (_face.IsHandleCreated)
                    _face.PerformLayout();
                _faceContent.PerformLayout();
                _mouth.PerformLayout();
                _console.PerformLayout();
                using (GraphicsPath silhouette = _face.CreateSilhouettePath())
                {
                    if (silhouette.PointCount == 0)
                    {
                        ReplaceFloatingRegion(previewForm, null);
                        return;
                    }
                    using (GraphicsPath regionPath = (GraphicsPath)silhouette.Clone())
                    {
                        // Slightly expand the path to provide an antialiasing margin
                        // and include a near-full-width console jaw in the region.
                        using (Matrix safety = new Matrix())
                        {
                            safety.Scale(1.01f, 1.01f);
                            regionPath.Transform(safety);
                        }
                        Rectangle consoleBounds = new Rectangle(
                            3,
                            Math.Max(0, size.Height - 58),
                            Math.Max(1, size.Width - 6),
                            58);
                        // No-op: keep shaped silhouette dimensions unchanged (58px console)
                        using (GraphicsPath consolePath = RobotEye.Rounded(
                            consoleBounds, 15))
                            regionPath.AddPath(consolePath, false);
                        Region nextRegion = new Region(regionPath);
                        ReplaceFloatingRegion(previewForm, nextRegion);
                    }
                    _regionExpanded = _eyeExpanded;
                    _regionSize = size;
                }
            }
            catch (Exception ex)
            {
                ResumeLayout(true);
                System.Diagnostics.Debug.WriteLine(
                    "Robot preview shape fallback: " + ex.Message);
                ReplaceFloatingRegion(previewForm, null);
                _regionExpanded = _eyeExpanded;
                _regionSize = size;
            }
        }

        private void ReplaceFloatingRegion(Form previewForm, Region nextRegion)
        {
            Region previous = previewForm.Region;
            previewForm.Region = nextRegion;
            if (previous != null && !ReferenceEquals(previous, nextRegion))
                previous.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                EyeExpansionChanged = null;
                _workflow.SetRobotViewActive(false);
                _workflow.TranscriptAppended -= Workflow_TranscriptAppended;
                _workflow.TranscriptCleared -= Workflow_TranscriptCleared;
                _workflow.StatusChanged -= Workflow_StatusChanged;
                _workflow.SavedPromptsChanged -= Workflow_SavedPromptsChanged;
                _workflow.BusyChanged -= Workflow_BusyChanged;
                _toolTip.Dispose();
                foreach (Font font in _ownedFonts)
                    font.Dispose();
                _ownedFonts.Clear();
            }
            base.Dispose(disposing);
        }

        private sealed class RobotFace : Panel
        {
            private readonly RobotPalette _palette;
            private Control _content;
            private Color _lampColor;
            private Rectangle _headBounds;
            private Rectangle _contentBounds;
            private Rectangle _silhouetteBounds;
            private Rectangle _earBounds;
            private Rectangle _antennaBounds;
            private Rectangle _mouthBounds;
            private Rectangle _lowerJawBounds;
            private Rectangle _contentSafeBounds;
            private Color _mouthColor;

            public Color MouthColor
            {
                get { return _mouthColor; }
                set
                {
                    if (_mouthColor == value)
                        return;
                    _mouthColor = value;
                    Invalidate();
                }
            }

            public Color LampColor
            {
                get { return _lampColor; }
                set
                {
                    if (_lampColor == value)
                        return;
                    _lampColor = value;
                    Invalidate(new Rectangle(
                        Width / 2 - 18, 0, 36, 52));
                }
            }

            public RobotFace(RobotPalette palette)
            {
                _palette = palette;
                _lampColor = palette.Unknown;
                AccessibleName = "Robot Assistant connection status";
                AccessibleDescription =
                    "The antenna lamp color indicates the full connection state.";
                _mouthColor = palette.Orange;
                BackColor = palette.Background;
                SetStyle(ControlStyles.UserPaint
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw, true);
            }

            public void SetContent(Control content)
            {
                _content = content;
                Controls.Add(content);
                LayoutContent();
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                LayoutContent();
                Invalidate();
            }

            private void LayoutContent()
            {
                if (_content == null)
                    return;
                int outerSafety = 10;
                float scale = Math.Max(.72f, Math.Min(
                    Math.Min(ClientSize.Width / 500f, ClientSize.Height / 300f),
                    1.2f));
                int antennaHeight = Math.Max(34, (int)(50 * Math.Min(
                    1f, ClientSize.Height / 360f)));
                int headInset = Math.Max(42, (int)(ClientSize.Width * .08f));
                int lowerMargin = outerSafety + 4;
                _silhouetteBounds = new Rectangle(
                    outerSafety, outerSafety,
                    Math.Max(1, ClientSize.Width - outerSafety * 2),
                    Math.Max(1, ClientSize.Height - outerSafety * 2));
                _headBounds = new Rectangle(
                    (ClientSize.Width - Math.Max(1,
                        ClientSize.Width - outerSafety * 2 - headInset * 2)) / 2,
                    antennaHeight,
                    Math.Max(1, ClientSize.Width - outerSafety * 2 - headInset * 2),
                    Math.Max(1, ClientSize.Height - antennaHeight - lowerMargin));
                int earWidth = Math.Max(40, (int)(56 * scale));
                int earHeight = Math.Max(48, (int)(70 * scale));
                int earLeft = (_headBounds.Left - earWidth) / 2;
                _earBounds = new Rectangle(
                    earLeft,
                    _headBounds.Top + (int)(_headBounds.Height * .47f)
                        - earHeight / 2,
                    earWidth, earHeight);
                int inset = Math.Max(22, (int)(24 * scale));
                _contentSafeBounds = Rectangle.FromLTRB(
                    _headBounds.Left + inset,
                    _headBounds.Top + Math.Max(12, (int)(14 * scale)),
                    _headBounds.Right - inset,
                    _headBounds.Bottom - Math.Max(10, (int)(12 * scale)));
                float lampDiameter = Math.Max(27f, 42f
                    * Math.Max(.72f, Math.Min(1.2f, ClientSize.Width / 500f)));
                float lampTop = Math.Max(3f, _headBounds.Top - lampDiameter - 10f * scale);
                _antennaBounds = new Rectangle(
                    ClientSize.Width / 2 - (int)lampDiameter / 2 - 8,
                    Math.Max(0, (int)Math.Floor(lampTop - 3f)),
                    (int)lampDiameter + 16,
                    Math.Max(1, _headBounds.Top - (int)lampTop));
                int mouthWidth = Math.Min((int)(_headBounds.Width * .62f),
                    Math.Max(210, (int)(300 * scale)));
                int mouthHeight = Math.Max(30, (int)(82 * scale));
                _mouthBounds = new Rectangle(
                    _contentSafeBounds.Left
                        + (_contentSafeBounds.Width - mouthWidth) / 2,
                    _contentSafeBounds.Bottom - mouthHeight - (int)(4 * scale),
                    mouthWidth, mouthHeight);
                _lowerJawBounds = new Rectangle(
                    _headBounds.Left + 12,
                    _headBounds.Bottom - 8,
                    _headBounds.Width - 24,
                    Math.Max(42, 50));
                _contentBounds = _contentSafeBounds;
                _content.Bounds = _contentBounds;
                _content.Margin = Padding.Empty;
            }

            public GraphicsPath CreateSilhouettePath()
            {
                GraphicsPath path = new GraphicsPath();
                if (_headBounds.Width <= 0 || _headBounds.Height <= 0)
                    return path;

                Rectangle head = Rectangle.Inflate(_headBounds, 5, 5);
                using (GraphicsPath headPath = RobotEye.Rounded(head, 27))
                    path.AddPath(headPath, false);

                float scale = Math.Max(.7f, Math.Min(
                    1.4f, ClientSize.Width / 500f));
                float earWidth = _earBounds.Width / 2f;
                float earHeight = _earBounds.Height;
                float earY = _earBounds.Top + earHeight / 2f;
                RectangleF leftEar = RectangleF.Inflate(new RectangleF(
                    _earBounds.Left, earY - earHeight / 2f,
                    _earBounds.Width, earHeight), 2f, 2f);
                RectangleF rightEar = RectangleF.Inflate(new RectangleF(
                    ClientSize.Width - leftEar.Right,
                    earY - earHeight / 2f, _earBounds.Width, earHeight), 2f, 2f);
                path.AddEllipse(leftEar);
                path.AddEllipse(rightEar);

                float lampDiameter = Math.Max(27f, 42f * scale);
                float lampY = _antennaBounds.Top + 7f;
                path.AddRectangle(new RectangleF(
                    ClientSize.Width / 2f - 3f * scale, 0f,
                    6f * scale, lampY + lampDiameter * .55f));
                path.AddEllipse(ClientSize.Width / 2f - lampDiameter / 2f - 2f,
                    lampY - 2f, lampDiameter + 4f, lampDiameter + 4f);
                path.FillMode = FillMode.Winding;
                return path;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (_headBounds.Width <= 0 || _headBounds.Height <= 0)
                    return;
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                DrawEars(g);
                DrawHead(g);
                DrawAntenna(g);
                DrawMouthHousing(g);
            }

            private void DrawEars(Graphics g)
            {
                float scale = Math.Max(.7f, Math.Min(1.4f, ClientSize.Width / 500f));
                float earWidth = _earBounds.Width;
                float earHeight = _earBounds.Height;
                float earY = _earBounds.Top + earHeight / 2f;
                RectangleF leftEar = new RectangleF(_earBounds.Left,
                    earY - earHeight / 2f, earWidth, earHeight);
                RectangleF rightEar = new RectangleF(ClientSize.Width
                    - (leftEar.Left + leftEar.Width), earY - earHeight / 2f,
                    earWidth, earHeight);
                using (SolidBrush ear = new SolidBrush(_palette.FaceBottom))
                using (Pen outline = new Pen(_palette.Outline, 3f * scale))
                using (Pen highlight = new Pen(_palette.FaceHighlight, 1.5f * scale))
                using (SolidBrush center = new SolidBrush(_palette.Shadow))
                {
                    g.FillEllipse(ear, leftEar);
                    g.DrawEllipse(outline, leftEar);
                    g.FillEllipse(ear, rightEar);
                    g.DrawEllipse(outline, rightEar);
                    float inset = earWidth * .28f;
                    g.FillEllipse(center, leftEar.Left + inset,
                        leftEar.Top + inset, earWidth - inset * 2,
                        earHeight - inset * 2);
                    g.FillEllipse(center, rightEar.Left + inset,
                        rightEar.Top + inset, earWidth - inset * 2,
                        earHeight - inset * 2);
                    g.DrawArc(highlight, leftEar.Left + 2, leftEar.Top + 4,
                        earWidth - 4, earHeight - 8, 90, 180);
                    g.DrawArc(highlight, rightEar.Left + 2, rightEar.Top + 4,
                        earWidth - 4, earHeight - 8, 270, 180);
                }
            }

            private void DrawAntenna(Graphics g)
            {
                float cx = ClientSize.Width / 2f;
                float baseY = _headBounds.Top + 1;
                float scale = Math.Max(.7f, Math.Min(1.4f, ClientSize.Width / 500f));
                float lampDiameter = Math.Max(27f, 42f * scale);
                float lampRadius = lampDiameter / 2f;
                float lampY = _antennaBounds.Top + 7f;
            using (Pen shadow = new Pen(_palette.Shadow, 6f * scale))
            using (Pen stem = new Pen(_palette.Orange, 4f * scale))
                using (SolidBrush lamp = new SolidBrush(_lampColor))
                using (Pen lampOutline = new Pen(_palette.Outline, 2.5f * scale))
                using (SolidBrush shine = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
                {
                    g.DrawLine(shadow, cx + 1, baseY, cx + 1,
                        lampY + lampDiameter * .58f);
                    g.DrawLine(stem, cx, baseY, cx,
                        lampY + lampDiameter * .58f);
                    RectangleF bulb = new RectangleF(
                        cx - lampRadius, lampY, lampDiameter, lampDiameter);
                    g.FillEllipse(lamp, bulb);
                    g.DrawEllipse(lampOutline, bulb);
                    g.FillEllipse(shine,
                        cx - lampRadius * .45f,
                        lampY + lampRadius * .23f,
                        lampRadius * .55f,
                        lampRadius * .55f);
                }
            }

            private void DrawMouthHousing(Graphics g)
            {
                if (_contentBounds.Width <= 0 || _contentBounds.Height <= 0)
                    return;
                float scale = Math.Max(.75f, Math.Min(1.35f, ClientSize.Width / 500f));
                Rectangle mouth = _mouthBounds;
                using (GraphicsPath shape = Rounded(mouth, mouth.Height / 2))
                using (SolidBrush silver = new SolidBrush(_palette.EyeSurface))
                using (Pen shadow = new Pen(_palette.Shadow, 4f * scale))
                using (Pen outline = new Pen(_palette.Outline, 3f * scale))
                using (Pen highlight = new Pen(_palette.FaceHighlight, 1.5f * scale))
                using (SolidBrush tooth = new SolidBrush(_mouthColor))
                using (Pen toothOutline = new Pen(_palette.Outline, 1.5f * scale))
                {
                    g.FillPath(silver, shape);
                    g.DrawPath(shadow, shape);
                    g.DrawPath(outline, shape);
                    Rectangle inner = Rectangle.Inflate(mouth, -4, -4);
                    using (GraphicsPath innerShape = Rounded(
                        inner, Math.Max(3, mouth.Height / 3)))
                        g.DrawPath(highlight, innerShape);
                    int count = Math.Max(5, Math.Min(9, mouth.Width / 21));
                    float toothWidth = Math.Max(6f,
                        (mouth.Width * .62f) / count);
                    float gap = toothWidth * .42f;
                    float total = count * toothWidth + (count - 1) * gap;
                    float startX = mouth.Left + (mouth.Width - total) / 2f;
                    float toothTop = mouth.Top + mouth.Height * .28f;
                    float toothHeight = mouth.Height * .46f;
                    for (int i = 0; i < count; i++)
                    {
                        RectangleF toothBounds = new RectangleF(
                            startX + i * (toothWidth + gap), toothTop,
                            toothWidth, toothHeight);
                        g.FillRectangle(tooth, toothBounds);
                        g.DrawRectangle(toothOutline, toothBounds.X,
                            toothBounds.Y, toothBounds.Width, toothBounds.Height);
                    }
                }
            }

            private void DrawHead(Graphics g)
            {
                Rectangle shadowBounds = _headBounds;
                shadowBounds.Offset(0, 4);
                using (GraphicsPath shadowPath = Rounded(shadowBounds, 22))
                using (SolidBrush shadowBrush = new SolidBrush(_palette.Shadow))
                using (GraphicsPath facePath = Rounded(_headBounds, 22))
                using (GraphicsPath innerFacePath = Rounded(
                    Rectangle.Inflate(_headBounds, -5, -5), 18))
                using (LinearGradientBrush face = new LinearGradientBrush(
                    _headBounds, _palette.FaceTop, _palette.FaceBottom,
                    LinearGradientMode.Vertical))
                using (LinearGradientBrush upperHighlight = new LinearGradientBrush(
                    new Rectangle(_headBounds.Left, _headBounds.Top,
                        _headBounds.Width, Math.Max(1, _headBounds.Height / 3)),
                    Color.FromArgb(72, _palette.FaceHighlight),
                    Color.FromArgb(0, _palette.FaceHighlight),
                    LinearGradientMode.Vertical))
                using (Pen outline = new Pen(_palette.Outline, 3f))
                using (Pen highlight = new Pen(
                    Color.FromArgb(125, _palette.FaceHighlight), 1.5f))
                {
                    g.FillPath(shadowBrush, shadowPath);
                    g.FillPath(face, facePath);
                    g.FillRectangle(upperHighlight, new Rectangle(
                        _headBounds.Left, _headBounds.Top,
                        _headBounds.Width, Math.Max(1, _headBounds.Height / 3)));
                    g.DrawPath(outline, facePath);
                    g.DrawPath(highlight, innerFacePath);
                    DrawScrew(g, _headBounds.Left + 13, _headBounds.Top + 13);
                    DrawScrew(g, _headBounds.Right - 13, _headBounds.Top + 13);
                    DrawScrew(g, _headBounds.Left + 13, _headBounds.Bottom - 13);
                    DrawScrew(g, _headBounds.Right - 13, _headBounds.Bottom - 13);
                }
            }

            private void DrawScrew(Graphics g, int x, int y)
            {
                using (SolidBrush fill = new SolidBrush(_palette.Shadow))
                using (Pen outline = new Pen(_palette.Outline, 1f))
                using (Pen slot = new Pen(_palette.FaceHighlight, 1f))
                {
                    g.FillEllipse(fill, x - 3, y - 3, 6, 6);
                    g.DrawEllipse(outline, x - 3, y - 3, 6, 6);
                    g.DrawLine(slot, x - 1, y, x + 1, y);
                }
            }

            internal static GraphicsPath Rounded(Rectangle bounds, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                int diameter = Math.Max(2, radius * 2);
                path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
                path.AddArc(bounds.Right - diameter, bounds.Top,
                    diameter, diameter, 270, 90);
                path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter,
                    diameter, diameter, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - diameter,
                    diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

        internal sealed class RobotEye : Panel
        {
            private readonly RobotPalette _palette;
            private readonly bool _technical;
            private readonly bool _circularInCompact;
            private readonly ToolTip _toolTip;
            private readonly Label _title;
            private readonly Label _summary;
            private readonly TextBox _details;
            private bool _selected;
            private bool _hover;
            private bool _expanded;

            internal Control TitleControl { get { return _title; } }
            internal Control SummaryControl { get { return _summary; } }
            internal Control DetailsControl { get { return _details; } }

            public event EventHandler Activated;
            public bool Expanded
            {
                get { return _expanded; }
                set
                {
                    if (_expanded == value)
                        return;
                    _expanded = value;
                    UpdateContentState();
                }
            }

            public RobotEye(
                string title,
                RobotPalette palette,
                bool technical,
                bool circularInCompact)
            {
                _palette = palette;
                _technical = technical;
                _circularInCompact = circularInCompact;
                _toolTip = new ToolTip();
                BackColor = Color.Transparent;
                Margin = new Padding(4);
                Padding = new Padding(10, 9, 10, 9);
                TabStop = true;
                AccessibleRole = AccessibleRole.PushButton;
                AccessibleName = title;
                SetStyle(ControlStyles.UserPaint
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw, true);

                _title = new Label();
                _title.Text = technical ? "Status" : "Conversation";
                _title.Dock = DockStyle.None;
                _title.Height = 20;
                _title.TextAlign = ContentAlignment.MiddleCenter;
                _title.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                _title.ForeColor = _palette.Text;
                _title.BackColor = Color.Transparent;
                _title.Click += (s, e) => Activated?.Invoke(this, EventArgs.Empty);
                _title.MouseEnter += (s, e) => SetHoverState(true);
                _title.MouseLeave += (s, e) => SetHoverState(false);

                _summary = new Label();
                _summary.Dock = DockStyle.Fill;
                _summary.TextAlign = ContentAlignment.MiddleCenter;
                _summary.AutoEllipsis = true;
                _summary.Font = new Font(
                    technical ? "Consolas" : "Segoe UI", 8.5f);
                _summary.ForeColor = _palette.Text;
                _summary.BackColor = Color.Transparent;
                _summary.Padding = new Padding(3, 0, 3, 0);
                _summary.Visible = false;
                _summary.Click += (s, e) => Activated?.Invoke(this, EventArgs.Empty);

                _details = new TextBox();
                _details.Dock = DockStyle.Fill;
                _details.Multiline = true;
                _details.ReadOnly = true;
                _details.WordWrap = true;
                _details.ScrollBars = ScrollBars.Vertical;
                _details.HideSelection = false;
                _details.Padding = new Padding(8);
                _details.BorderStyle = BorderStyle.None;
                _details.BackColor = _palette.EyeSurface;
                _details.ForeColor = Color.FromArgb(35, 36, 40);
                _details.Font = new Font(
                    technical ? "Consolas" : "Segoe UI", 9.5f);
                _details.Visible = false;
                _details.AccessibleName = title + " details";
                _details.AccessibleDescription =
                    "Scrollable, selectable text. Use the standard copy keyboard shortcut.";

                Controls.Add(_summary);
                Controls.Add(_details);
                Controls.Add(_title);
                Click += (s, e) => Activated?.Invoke(this, EventArgs.Empty);
                MouseEnter += (s, e) => SetHoverState(true);
                MouseLeave += (s, e) => SetHoverState(false);
                _summary.MouseEnter += (s, e) => SetHoverState(true);
                _summary.MouseLeave += (s, e) => SetHoverState(false);
                KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                    {
                        e.Handled = true;
                        Activated?.Invoke(this, EventArgs.Empty);
                    }
                };
                _toolTip.SetToolTip(this, title + ". Select to expand or collapse.");
                AccessibleDescription = title
                    + ". Select to expand or collapse. Compact text is summarized; expanded text can be copied.";
                AccessibleDescription = title
                    + ". Compact summary. Select to expand readable details.";
                _toolTip.SetToolTip(_title, title + ". Select to expand or collapse.");
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                Focus();
            }

            public void SetSummary(string text)
            {
                string oneLine = string.IsNullOrWhiteSpace(text)
                    ? string.Empty
                    : text.Replace('\r', ' ').Replace('\n', ' ').Trim();
                _summary.Text = oneLine;
            }

            public void SetDetails(string text)
            {
                _details.Text = text ?? string.Empty;
                _details.SelectionStart = _details.TextLength;
                _details.ScrollToCaret();
            }

            public void AppendDetails(string text)
            {
                _details.AppendText(text);
                _details.SelectionStart = _details.TextLength;
                _details.ScrollToCaret();
            }

            public void SetSelected(bool selected)
            {
                if (_selected == selected)
                    return;
                _selected = selected;
                Invalidate();
            }

            public void SetHoverState(bool hover)
            {
                if (_hover == hover)
                    return;
                _hover = hover;
                Invalidate();
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                UpdateContentState();
            }

            private void UpdateContentState()
            {
                _summary.Visible = !Expanded;
                _details.Visible = Expanded;
                _title.Text = _technical ? "Status" : "Chat";
                if (Expanded)
                    _details.BringToFront();
                else
                    _summary.Visible = false;
                _title.BringToFront();
                if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                    return;
                int headerHeight = 20;
                _title.Dock = DockStyle.None;
                _title.AutoEllipsis = true;
                _title.Bounds = new Rectangle(8, 3,
                    Math.Max(1, ClientSize.Width - 16), headerHeight);
                _summary.Dock = DockStyle.None;
                Rectangle outer = Rectangle.Inflate(ClientRectangle, -3, -3);
                int ringSize = Math.Max(1, Math.Min(
                    outer.Width - 12, outer.Height - 48));
                int ringTop = outer.Top + 24;
                int summaryTop = ringTop + ringSize + 3;
                _summary.Bounds = new Rectangle(6, summaryTop,
                    Math.Max(1, ClientSize.Width - 12),
                    Math.Max(1, ClientSize.Height - summaryTop - 2));
                _details.Dock = DockStyle.None;
                _details.Bounds = new Rectangle(8, headerHeight + 4,
                    Math.Max(1, ClientSize.Width - 16),
                    Math.Max(1, ClientSize.Height - headerHeight - 12));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle outer = Rectangle.Inflate(ClientRectangle, -3, -3);
                if (outer.Width <= 4 || outer.Height <= 4)
                    return;
                int eyeSize = Math.Max(1, Math.Min(
                    outer.Width - 12, outer.Height - 48));
                Rectangle ringBounds = new Rectangle(
                    outer.Left + (outer.Width - eyeSize) / 2,
                    outer.Top + 24, eyeSize, eyeSize);
                using (GraphicsPath path = Expanded
                    ? Rounded(ringBounds, 14)
                    : Ellipse(ringBounds))
                using (SolidBrush surface = new SolidBrush(
                    _hover ? ControlPaint.Light(_palette.FaceTop, .1f) : _palette.FaceTop))
                using (Pen shadow = new Pen(_palette.Shadow, 4f))
                using (Pen darkRing = new Pen(_palette.Outline, 3f))
                using (Pen orangeRing = new Pen(_palette.Orange, _selected ? 5f : 3.5f))
                using (Pen highlight = new Pen(_palette.FaceHighlight, 1f))
                {
                    g.FillPath(surface, path);
                    g.DrawPath(shadow, path);
                    g.DrawPath(darkRing, path);
                    g.DrawPath(orangeRing, path);
                    Rectangle inner = Rectangle.Inflate(ringBounds, -5, -5);
                    using (GraphicsPath innerPath = Expanded
                        ? Rounded(inner, Math.Min(14, inner.Height / 8))
                        : Ellipse(inner))
                        g.DrawPath(highlight, innerPath);
                    DrawIris(g, ringBounds);
                }
            }

            private void DrawIris(Graphics g, Rectangle outer)
            {
                int diameter = Expanded ? 22 : Math.Min(38,
                    Math.Max(28, Math.Min(outer.Width - 12, outer.Height - 12)));
                int x = outer.Left + (outer.Width - diameter) / 2;
                int y = outer.Top + (outer.Height - diameter) / 2;
                Rectangle iris = new Rectangle(x, y, diameter, diameter);
                using (SolidBrush orange = new SolidBrush(_palette.Orange))
                using (Pen ring = new Pen(_palette.Outline, 2f))
                using (SolidBrush white = new SolidBrush(Color.White))
                using (SolidBrush pupil = new SolidBrush(_palette.Outline))
                using (SolidBrush glint = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                {
                    g.FillEllipse(orange, iris);
                    g.DrawEllipse(ring, iris);
                    Rectangle eye = Rectangle.Inflate(iris, -5, -5);
                    g.FillEllipse(white, eye);
                    int pupilSize = Math.Max(7, diameter / 3);
                    g.FillEllipse(pupil, iris.Left + (diameter - pupilSize) / 2,
                        iris.Top + (diameter - pupilSize) / 2, pupilSize, pupilSize);
                    g.FillEllipse(glint, iris.Left + diameter / 3,
                        iris.Top + diameter / 4, Math.Max(3, diameter / 7),
                        Math.Max(3, diameter / 7));
                }
            }

            internal static GraphicsPath Rounded(Rectangle bounds, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                int d = Math.Max(2, radius * 2);
                path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
                path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
                path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }

            private static GraphicsPath Ellipse(Rectangle bounds)
            {
                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(bounds);
                return path;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _toolTip.Dispose();
                    _title.Font.Dispose();
                    _summary.Font.Dispose();
                    _details.Font.Dispose();
                }
                base.Dispose(disposing);
            }
        }

    }

    internal sealed class RobotMouth : Panel
    {
        private readonly RobotAssistantPanel.RobotPalette _palette;

        public RobotMouth(RobotAssistantPanel.RobotPalette palette)
        {
            _palette = palette;
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle mouth = Rectangle.Inflate(ClientRectangle, -3, -3);
            if (mouth.Width <= 0 || mouth.Height <= 0)
                return;
            using (GraphicsPath path = new GraphicsPath())
            using (LinearGradientBrush fill = new LinearGradientBrush(
                mouth, _palette.FaceHighlight, _palette.FaceBottom,
                LinearGradientMode.Vertical))
            using (Pen shadow = new Pen(_palette.Shadow, 3f))
            using (Pen outline = new Pen(_palette.Outline, 2f))
            {
                int diameter = Math.Min(22, mouth.Height);
                path.AddArc(mouth.Left, mouth.Top, diameter, diameter, 180, 90);
                path.AddArc(mouth.Right - diameter, mouth.Top,
                    diameter, diameter, 270, 90);
                path.AddArc(mouth.Right - diameter, mouth.Bottom - diameter,
                    diameter, diameter, 0, 90);
                path.AddArc(mouth.Left, mouth.Bottom - diameter,
                    diameter, diameter, 90, 90);
                path.CloseFigure();
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(shadow, path);
                Rectangle inner = Rectangle.Inflate(mouth, -3, -3);
                using (GraphicsPath innerPath = new GraphicsPath())
                using (Pen innerOutline = new Pen(_palette.Orange, 1f))
                {
                    int innerDiameter = Math.Min(16, inner.Height);
                    innerPath.AddArc(inner.Left, inner.Top,
                        innerDiameter, innerDiameter, 180, 90);
                    innerPath.AddArc(inner.Right - innerDiameter, inner.Top,
                        innerDiameter, innerDiameter, 270, 90);
                    innerPath.AddArc(inner.Right - innerDiameter,
                        inner.Bottom - innerDiameter, innerDiameter,
                        innerDiameter, 0, 90);
                    innerPath.AddArc(inner.Left,
                        inner.Bottom - innerDiameter, innerDiameter,
                        innerDiameter, 90, 90);
                    innerPath.CloseFigure();
                    e.Graphics.DrawPath(innerOutline, innerPath);
                }
                using (Pen edge = new Pen(_palette.FaceHighlight, 1f))
                    e.Graphics.DrawPath(edge, path);
                DrawTeeth(e.Graphics, mouth);
            }
        }

        private void DrawTeeth(Graphics graphics, Rectangle mouth)
        {
            int toothCount = Math.Max(5, Math.Min(11, mouth.Width / 22));
            int gap = 3;
            int toothWidth = Math.Max(6,
                (mouth.Width - (toothCount - 1) * gap - 16) / toothCount);
            int totalWidth = toothCount * toothWidth + (toothCount - 1) * gap;
            int x = mouth.Left + (mouth.Width - totalWidth) / 2;
            int y = mouth.Bottom - 14;
            using (SolidBrush tooth = new SolidBrush(Color.FromArgb(245, 246, 248)))
            using (SolidBrush alternate = new SolidBrush(_palette.FaceHighlight))
            using (Pen outline = new Pen(_palette.Outline, 1f))
            {
                for (int i = 0; i < toothCount; i++)
                {
                    Rectangle toothBounds = new Rectangle(
                        x + i * (toothWidth + gap), y, toothWidth, 6);
                    graphics.FillRectangle(i % 2 == 0 ? tooth : alternate,
                        toothBounds);
                    graphics.DrawRectangle(outline, toothBounds);
                }
            }
        }
    }

    internal sealed class RobotSendButton : Button
    {
        private readonly RobotAssistantPanel.RobotPalette _palette;
        private bool _hover;
        private bool _pressed;
        private readonly Font _paintFont;

        public RobotSendButton(RobotAssistantPanel.RobotPalette palette)
        {
            _palette = palette;
            Text = "Send";
            ForeColor = Color.White;
            BackColor = palette.Orange;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _paintFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            AccessibleRole = AccessibleRole.PushButton;
            TabStop = true;
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            Color baseColor = Enabled ? _palette.Orange : _palette.Shadow;
            if (_hover && Enabled)
                baseColor = ControlPaint.Light(baseColor, .12f);
            if (_pressed && Enabled)
                baseColor = ControlPaint.Dark(baseColor, .1f);

            using (GraphicsPath path = RobotAssistantPanel.RobotEye.Rounded(
                bounds, Math.Min(7, bounds.Height / 3)))
            using (SolidBrush fill = new SolidBrush(baseColor))
            using (Pen outline = new Pen(_palette.Outline, 1.5f))
            using (Pen toothOutline = new Pen(_palette.Outline, 1f))
            using (SolidBrush textBrush = new SolidBrush(ForeColor))
            using (StringFormat format = new StringFormat())
            {
                graphics.FillPath(fill, path);
                graphics.DrawPath(outline, path);
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                Rectangle textBounds = Rectangle.Inflate(bounds, -2, -1);
                graphics.DrawString("Send", _paintFont, textBrush,
                    textBounds, format);
                if (Focused && ShowFocusCues)
                    ControlPaint.DrawFocusRectangle(graphics,
                        Rectangle.Inflate(bounds, -4, -3),
                        ForeColor, baseColor);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _paintFont.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class RobotConsole : Panel
    {
        private readonly RobotAssistantPanel.RobotPalette _palette;
        private readonly VaultButtonStrip _vaultStrip;
        private bool _reserveGripSpace;

        public RobotConsole(
            RobotAssistantPanel.RobotPalette palette,
            VaultButtonStrip vaultStrip)
        {
            _palette = palette;
            _vaultStrip = vaultStrip;
            BackColor = Color.Transparent;
            MinimumSize = new Size(160, 40);
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            Controls.Add(_vaultStrip);
            Resize += (s, e) => LayoutVaultStrip(_vaultStrip);
            LayoutVaultStrip(_vaultStrip);
        }

        public void SetLampColor(Color color)
        {
            Invalidate();
        }

        public void LayoutVaultStrip(VaultButtonStrip vaultStrip)
        {
            if (vaultStrip == null || vaultStrip.IsDisposed)
                return;
            int consoleWidth = ClientSize.Width;
            int left = 9;
            int padding = 0;
            int gripWidth = _reserveGripSpace ? 34 : 0;
            int gaps = 3 * 6;
            int count = 0;
            foreach (Control child in vaultStrip.Controls)
                if (child is Button)
                    count++;
            int width = Math.Max(1, consoleWidth - left - 12 - gripWidth - gaps);
            int height = Math.Max(1, ClientSize.Height - 4);
            vaultStrip.Dock = DockStyle.None;
            vaultStrip.Bounds = new Rectangle(
                left + padding, 0, width + gaps, height);
            int index = 0;
            int buttonWidth = count == 0 ? 1 : Math.Max(1, width / count);
            foreach (Control button in vaultStrip.Controls)
            {
                if (button is Button)
                {
                    button.Dock = DockStyle.None;
                    button.Height = Math.Max(25, height - 2);
                    button.Width = buttonWidth + (index == count - 1
                        ? width - buttonWidth * count : 0);
                    button.Left = index * (buttonWidth + 6);
                    button.Top = 1;
                    index++;
                }
            }
            vaultStrip.PerformLayout();
        }

        public void SetResizeGripSpace(bool enabled)
        {
            _reserveGripSpace = enabled;
            LayoutVaultStrip(_vaultStrip);
        }

        public void SetGripReservation(int width)
        {
            _reserveGripSpace = width > 0;
            LayoutVaultStrip(_vaultStrip);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int insetX = Math.Max(5, (int)(ClientSize.Width * .03f));
            Rectangle bounds = Rectangle.FromLTRB(
                insetX, 3, ClientSize.Width - insetX,
                ClientSize.Height - 3);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;
            DrawConsoleFrame(e.Graphics, bounds);
        }

        private void DrawConsoleFrame(Graphics graphics, Rectangle bounds)
        {
            using (GraphicsPath path = RobotAssistantPanel.RobotEye.Rounded(
                bounds, Math.Min(15, bounds.Height / 2)))
            using (LinearGradientBrush fill = new LinearGradientBrush(
                bounds, _palette.FaceHighlight, _palette.FaceBottom,
                LinearGradientMode.Vertical))
            using (Pen shadow = new Pen(_palette.Shadow, 3f))
            using (Pen outline = new Pen(_palette.Outline, 2f))
            {
                graphics.FillPath(fill, path);
                graphics.DrawPath(shadow, path);
                graphics.DrawPath(outline, path);
                using (SolidBrush lamp = new SolidBrush(_palette.Orange))
                using (SolidBrush darkLamp = new SolidBrush(_palette.Outline))
                using (Pen slot = new Pen(_palette.FaceHighlight, 1f))
                using (Pen lampOutline = new Pen(_palette.Outline, 1f))
                {
                    float centerX = bounds.Left + bounds.Width / 2f;
                    float centerY = bounds.Top + bounds.Height / 2f;
                    graphics.FillEllipse(lamp, centerX - 5, centerY - 5, 10, 10);
                    graphics.DrawEllipse(lampOutline,
                        centerX - 5, centerY - 5, 10, 10);
                    graphics.FillEllipse(darkLamp,
                        centerX - 2, centerY - 2, 4, 4);
                    graphics.DrawLine(slot,
                        bounds.Left + bounds.Width * .27f, centerY,
                        bounds.Left + bounds.Width * .39f, centerY);
                    graphics.DrawLine(slot,
                        bounds.Left + bounds.Width * .61f, centerY,
                        bounds.Left + bounds.Width * .73f, centerY);
                }
            }
        }
    }

    internal sealed class RobotSilhouette
    {
        public static GraphicsPath CreatePath(Size size)
        {
            GraphicsPath path = new GraphicsPath();
            if (size.Width < 280 || size.Height < 220)
                return path;

            float scale = Math.Max(.72f, Math.Min(
                Math.Min(size.Width / 520f, size.Height / 360f), 1.2f));
            float centerX = size.Width / 2f;
            float safety = 10f;
            float lampDiameter = Math.Max(25f, 42f * scale);
            float headTop = Math.Max(safety + lampDiameter + 10f * scale,
                58f * scale);
            float jawHeight = Math.Max(46f, 54f * scale);
            float headBottom = size.Height - safety - jawHeight;
            float halfWidth = size.Width * .40f;
            float corner = Math.Min(30f * scale, halfWidth * .2f);
            PointF[] headPoints =
            {
                new PointF(centerX - halfWidth + corner, headTop),
                new PointF(centerX + halfWidth - corner, headTop),
                new PointF(centerX + halfWidth, headTop + corner),
                new PointF(centerX + halfWidth, headBottom - 23f * scale),
                new PointF(centerX + halfWidth - 23f * scale, headBottom),
                new PointF(centerX - halfWidth + 23f * scale, headBottom),
                new PointF(centerX - halfWidth, headBottom - 23f * scale),
                new PointF(centerX - halfWidth, headTop + corner)
            };
            using (GraphicsPath head = new GraphicsPath())
            {
                head.AddPolygon(headPoints);
                using (Matrix transform = new Matrix())
                {
                    transform.Translate(0f, 4f * scale);
                    head.Transform(transform);
                }
                path.AddPath(head, false);
            }
            path.AddPolygon(headPoints);

            float earWidth = Math.Max(18f, 28f * scale);
            float earHeight = Math.Max(38f, 58f * scale);
            float earCenterY = headTop + (headBottom - headTop) * .47f;
            using (GraphicsPath leftEar = RobotAssistantPanel.RobotEye.Rounded(
                Rectangle.Round(new RectangleF(
                    centerX - halfWidth - earWidth + 10f,
                    earCenterY - earHeight / 2f,
                    earWidth,
                    earHeight)), (int)(earWidth / 2f)))
            using (GraphicsPath rightEar = RobotAssistantPanel.RobotEye.Rounded(
                Rectangle.Round(new RectangleF(
                    centerX + halfWidth - 10f,
                    earCenterY - earHeight / 2f,
                    earWidth,
                    earHeight)), (int)(earWidth / 2f)))
            {
                path.AddPath(leftEar, false);
                path.AddPath(rightEar, false);
            }

            float lampTop = Math.Max(3f, headTop - lampDiameter - 10f * scale);
            path.AddRectangle(new RectangleF(
                centerX - 5f * scale,
                lampTop + lampDiameter * .52f,
                10f * scale,
                headTop - (lampTop + lampDiameter * .52f) + 3f * scale));
            path.AddEllipse(new RectangleF(
                centerX - lampDiameter / 2f,
                lampTop,
                lampDiameter,
                lampDiameter));
            RectangleF jaw = new RectangleF(
                centerX - size.Width * .32f,
                headBottom - 2f,
                size.Width * .64f,
                Math.Max(42f, jawHeight - 4f));
            using (GraphicsPath jawPath = RobotAssistantPanel.RobotEye.Rounded(
                Rectangle.Round(jaw), 15))
                path.AddPath(jawPath, false);
            path.FillMode = FillMode.Winding;
            return path;
        }
    }

    internal sealed class RobotPreviewForm : Form
    {
        private const int HtCaption = 2;

        private readonly RobotAssistantPanel _robotPanel;
        private readonly Button _resizeButton;
        private readonly ToolTip _toolTip;
        private bool _updatingRegion;
        private bool _resizing;
        private Point _resizeOrigin;
        private Size _resizeStartSize;

        public RobotPreviewForm(
            AssistantPanel assistantPanel,
            Inventor.Application application)
        {
            Text = "Robot Assistant Preview";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 360);
            MinimumSize = new Size(430, 300);
            KeyPreview = true;
            BackColor = Color.FromArgb(38, 39, 43);
            ShowInTaskbar = false;

            _toolTip = new ToolTip();
            _robotPanel = new RobotAssistantPanel(
                assistantPanel, application);
            _robotPanel.Dock = DockStyle.Fill;
            _robotPanel.SetFloatingPreviewMode(true);
            _robotPanel.SetFloatingResizeGrip(true);
            Controls.Add(_robotPanel);
            _robotPanel.TabIndex = 0;

            _resizeButton = new Button();
            _resizeButton.Text = "↘";
            _resizeButton.AccessibleName = "Resize Robot Preview";
            _resizeButton.AccessibleDescription =
                "Drag to resize the floating robot window.";
            _resizeButton.Size = new Size(18, 18);
            _resizeButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            _resizeButton.BackColor = Color.FromArgb(75, 71, 88);
            _resizeButton.ForeColor = Color.White;
            _resizeButton.FlatStyle = FlatStyle.Flat;
            _resizeButton.Cursor = Cursors.SizeNWSE;
            _resizeButton.Location = GetResizeGripLocation();
            _resizeButton.MouseDown += ResizeButton_MouseDown;
            _resizeButton.MouseMove += ResizeButton_MouseMove;
            _resizeButton.MouseUp += ResizeButton_MouseUp;
            Controls.Add(_resizeButton);
            _toolTip.SetToolTip(_resizeButton,
                "Drag to resize the Robot Preview.");
            _robotPanel.SetFloatingWindowDragHandler(BeginWindowDrag);
            _robotPanel.EyeExpansionChanged += RobotPanel_EyeExpansionChanged;
            _resizeButton.TabIndex = 2;
            _resizeButton.BringToFront();

            Resize += (s, e) => _robotPanel.LayoutAfterPreviewResize();
            Resize += (s, e) => UpdateSilhouetteRegion();
            Shown += (s, e) => UpdateSilhouetteRegion();
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    if (_robotPanel.IsEyeExpanded)
                        _robotPanel.CollapseExpandedEye();
                    else
                        Close();
                }
            };
            FormClosed += (s, e) =>
            {
                if (Region != null)
                {
                    Region oldRegion = Region;
                    Region = null;
                    oldRegion.Dispose();
                }
            };
        }

        private void RobotPanel_EyeExpansionChanged(object sender, EventArgs e)
        {
            if (IsDisposed || Disposing || WindowState == FormWindowState.Minimized)
                return;
            Size compact = new Size(520, 360);
            Size expanded = new Size(520, 390);
            Size target = _robotPanel.IsEyeExpanded ? expanded : compact;
            if (Size != target)
                Size = target;
            _robotPanel.LayoutAfterPreviewResize();
            UpdateSilhouetteRegion();
        }

        private Point GetResizeGripLocation()
        {
            int jawBottom = ClientSize.Height - 2;
            return new Point(
                ClientSize.Width - _resizeButton.Width - 3,
                jawBottom - _resizeButton.Height - 2);
        }

        public void BeginDragFromForehead()
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, (IntPtr)HtCaption, IntPtr.Zero);
        }

        private void BeginWindowDrag()
        {
            BeginDragFromForehead();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _robotPanel.SetActive(true);
            UpdateSilhouetteRegion();
        }

        internal void InvalidateSilhouette()
        {
            if (!_updatingRegion && !IsDisposed && !Disposing && IsHandleCreated
                && WindowState != FormWindowState.Minimized)
                BeginInvoke(new Action(UpdateSilhouetteRegion));
        }

        internal void UpdateSilhouetteRegion()
        {
            if (_updatingRegion || IsDisposed || Disposing
                || WindowState == FormWindowState.Minimized
                || ClientSize.Width < 280 || ClientSize.Height < 250)
                return;
            _updatingRegion = true;
            try
            {
                _robotPanel.UpdateFloatingWindowRegion(this);
                _resizeButton.Location = GetResizeGripLocation();
                _resizeButton.BringToFront();
            }
            finally
            {
                _updatingRegion = false;
            }
        }

        private void ResizeButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            _resizing = true;
            _resizeOrigin = Cursor.Position;
            _resizeStartSize = Size;
            _resizeButton.Capture = true;
        }

        private void ResizeButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_resizing)
                return;
            Point delta = new Point(
                Cursor.Position.X - _resizeOrigin.X,
                Cursor.Position.Y - _resizeOrigin.Y);
            int width = Math.Max(MinimumSize.Width,
                _resizeStartSize.Width + delta.X);
            int height = Math.Max(MinimumSize.Height,
                _resizeStartSize.Height + delta.Y);
            float aspect = 520f / (_robotPanel.IsEyeExpanded ? 390f : 360f);
            if (Math.Abs(delta.X) >= Math.Abs(delta.Y))
                height = Math.Max(MinimumSize.Height,
                    (int)(width / aspect));
            else
                width = Math.Max(MinimumSize.Width,
                    (int)(height * aspect));
            Size = new Size(width, height);
        }

        private void ResizeButton_MouseUp(object sender, MouseEventArgs e)
        {
            _resizing = false;
            _resizeButton.Capture = false;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int message,
            IntPtr wParam,
            IntPtr lParam);

        protected override void Dispose(bool disposing)
        {
            if (disposing && _toolTip != null)
                _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
