using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Inventor;

namespace Inventor2023AIAssistant
{
    public partial class AssistantForm : Form
    {
        private Inventor.Application _inventorApplication;
        private AiChatClient _aiClient;
        private List<ChatMessage> _conversationHistory;
        private bool _aiAvailable = false;
        private System.Windows.Forms.Timer _statusCheckTimer;
        private WriteActionsHandler _writeActions;

        public AssistantForm(Inventor.Application inventorApplication)
        {
            InitializeComponent();

            _inventorApplication = inventorApplication;
            _aiClient = new AiChatClient();
            _writeActions = new WriteActionsHandler(_inventorApplication);
            _conversationHistory = new List<ChatMessage>();

            this.Shown += AssistantForm_Shown;
            this.FormClosed += AssistantForm_FormClosed;

            txtPrompt.Multiline = true;
            txtPrompt.AcceptsReturn = true;
            txtPrompt.AcceptsTab = false;
            txtPrompt.ShortcutsEnabled = true;
            txtPrompt.KeyDown += TxtPrompt_KeyDown;

            btnSend.TabStop = false;
            btnNewChat.TabStop = false;

            InitializeConversation();
            StartStatusChecker();
        }

        // ─── Conversation ─────────────────────────────────────────────

        private void InitializeConversation()
        {
            _conversationHistory.Clear();

            _conversationHistory.Add(new ChatMessage
            {
                role = "system",
                content =
                    "You are an AI assistant embedded inside " +
                    "Autodesk Inventor 2023. " +
                    "You help with engineering, CAD, design, " +
                    "HVAC, sheet metal, and general questions. " +
                    "When the user asks about their active file, " +
                    "model, parameters, material, or iProperties, " +
                    "use the Inventor context provided in the message. " +
                    "For general questions not related to Inventor, " +
                    "answer them normally and helpfully. " +
                    "Be concise and practical."
            });
        }

        // ─── Status checker ───────────────────────────────────────────

        private void StartStatusChecker()
        {
            SetStatus(false);

            _statusCheckTimer = new System.Windows.Forms.Timer();
            _statusCheckTimer.Interval = 5000;
            _statusCheckTimer.Tick += async (s, e) =>
                await CheckAiStatusAsync();
            _statusCheckTimer.Start();

            _ = CheckAiStatusAsync();
        }

        private async Task CheckAiStatusAsync()
        {
            bool available = await _aiClient.CheckConnectionAsync();

            if (available != _aiAvailable)
            {
                _aiAvailable = available;
                SetStatus(available);
            }
        }

        private void SetStatus(bool aiAvailable)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(
                    new Action<bool>(SetStatus), aiAvailable);
                return;
            }

            if (aiAvailable)
            {
                lblStatus.Text = "● AI Connected";
                lblStatus.ForeColor = System.Drawing.Color.Green;
                btnNewChat.Text = "New Chat";
            }
            else
            {
                lblStatus.Text = "● Running in Local Mode";
                lblStatus.ForeColor = System.Drawing.Color.OrangeRed;
                btnNewChat.Text = "Diagnose";
            }
        }

        // ─── Form events ──────────────────────────────────────────────

        private void AssistantForm_Shown(object sender, EventArgs e)
        {
            FocusPrompt();
        }

        private void AssistantForm_FormClosed(
            object sender, FormClosedEventArgs e)
        {
            if (_statusCheckTimer != null)
            {
                _statusCheckTimer.Stop();
                _statusCheckTimer.Dispose();
                _statusCheckTimer = null;
            }
        }

        // ─── Focus ────────────────────────────────────────────────────

        public void FocusPrompt()
        {
            if (txtPrompt == null || txtPrompt.IsDisposed)
                return;

            txtPrompt.Focus();
            txtPrompt.SelectionStart = txtPrompt.TextLength;
            txtPrompt.SelectionLength = 0;
        }

        public void InsertSpaceFromInventor()
        {
            if (txtPrompt == null || txtPrompt.IsDisposed)
                return;

            if (!txtPrompt.Focused)
                txtPrompt.Focus();

            int start = txtPrompt.SelectionStart;
            int length = txtPrompt.SelectionLength;
            string text = txtPrompt.Text ?? string.Empty;

            if (length > 0)
                text = text.Remove(start, length);

            text = text.Insert(start, " ");
            txtPrompt.Text = text;
            txtPrompt.SelectionStart = start + 1;
            txtPrompt.SelectionLength = 0;
        }

        // ─── Keyboard ────────────────────────────────────────────────

        private void TxtPrompt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                _ = SendPromptAsync();
            }
        }

        // ─── Buttons ─────────────────────────────────────────────────

        private void btnSend_Click(object sender, EventArgs e)
        {
            _ = SendPromptAsync();
        }

        private async void btnNewChat_Click(object sender, EventArgs e)
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
            InitializeConversation();
            txtOutput.Clear();
            txtPrompt.Clear();
            FocusPrompt();

            txtOutput.AppendText(
                "── New Chat Started ──" +
                System.Environment.NewLine +
                System.Environment.NewLine);
        }

        // ─── Diagnostic ───────────────────────────────────────────────

        private async Task RunConnectionDiagnosticAsync()
        {
            txtOutput.AppendText(
                "── Connection Diagnostic ──" +
                System.Environment.NewLine);

            string key = System.Environment.GetEnvironmentVariable(
                "GROQ_API_KEY");

            if (string.IsNullOrWhiteSpace(key))
            {
                txtOutput.AppendText(
                    "❌ GROQ_API_KEY is NOT set." +
                    System.Environment.NewLine +
                    "   Fix: set it in sysdm.cpl > Advanced > " +
                    "Environment Variables, then restart Inventor." +
                    System.Environment.NewLine +
                    System.Environment.NewLine);
                return;
            }

            txtOutput.AppendText(
                "✅ GROQ_API_KEY found: " +
                key.Substring(0, 8) + "..." +
                System.Environment.NewLine);

            bool configured = _aiClient.IsConfigured();

            txtOutput.AppendText(
                (configured ? "✅" : "❌") +
                " IsConfigured() = " + configured +
                System.Environment.NewLine);

            if (!configured)
            {
                txtOutput.AppendText(
                    "   Fix: rebuild after verifying AiChatClient.cs." +
                    System.Environment.NewLine +
                    System.Environment.NewLine);
                return;
            }

            txtOutput.AppendText(
                "⏳ Sending test request to Groq..." +
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
                        content = "Reply with the single word: connected"
                    }
                };

                testResponse = await _aiClient.GetResponseAsync(
                    testMessages);

                connected =
                    !string.IsNullOrWhiteSpace(testResponse) &&
                    !testResponse.StartsWith("Groq request failed") &&
                    !testResponse.StartsWith("Failed to parse") &&
                    !testResponse.StartsWith("Groq is not configured");
            }
            catch (Exception ex)
            {
                testResponse = "Exception: " + ex.Message;
                connected = false;
            }

            if (connected)
            {
                txtOutput.AppendText(
                    "✅ Groq responded: " + testResponse +
                    System.Environment.NewLine +
                    "✅ Connection is working." +
                    System.Environment.NewLine +
                    System.Environment.NewLine);

                _aiAvailable = true;
                SetStatus(true);
            }
            else
            {
                txtOutput.AppendText(
                    "❌ Groq request failed." +
                    System.Environment.NewLine +
                    "   Response: " + testResponse +
                    System.Environment.NewLine +
                    System.Environment.NewLine +
                    "   Possible causes:" +
                    System.Environment.NewLine +
                    "   - Invalid or expired API key" +
                    System.Environment.NewLine +
                    "   - Network/firewall blocking api.groq.com" +
                    System.Environment.NewLine +
                    "   - Rate limit exceeded" +
                    System.Environment.NewLine +
                    System.Environment.NewLine);
            }
        }

        // ─── Send ─────────────────────────────────────────────────────

        private async Task SendPromptAsync()
        {
            string prompt = txtPrompt.Text;

            if (string.IsNullOrWhiteSpace(prompt))
            {
                FocusPrompt();
                return;
            }

            txtOutput.AppendText(
                "You: " + prompt +
                System.Environment.NewLine);

            txtPrompt.Clear();
            FocusPrompt();

            btnSend.Enabled = false;

            string response;

            try
            {
                string writeResult =
                    _writeActions.TryHandleWriteAction(prompt);

                if (writeResult != null)
                {
                    response = writeResult;
                }
                else if (_aiAvailable)
                {
                    string contextualPrompt =
                        BuildContextualUserPrompt(prompt);

                    _conversationHistory.Add(new ChatMessage
                    {
                        role = "user",
                        content = contextualPrompt
                    });

                    response = await _aiClient.GetResponseAsync(
                        _conversationHistory);

                    _conversationHistory.Add(new ChatMessage
                    {
                        role = "assistant",
                        content = response
                    });
                }
                else
                {
                    response = GetLocalFallbackResponse(prompt);
                }
            }
            catch (Exception ex)
            {
                response = "Error: " + ex.Message;
            }
            finally
            {
                btnSend.Enabled = true;
            }

            txtOutput.AppendText(
                "Assistant: " + response +
                System.Environment.NewLine +
                System.Environment.NewLine);
        }

        // ─── Context builder ──────────────────────────────────────────

        private string BuildContextualUserPrompt(string prompt)
        {
            string normalized = prompt.Trim().ToLowerInvariant();
            string inventorData = "";

            if (Contains(normalized, "parameter", "dimension",
                "variable"))
                inventorData = GetParametersData();
            else if (Contains(normalized, "material"))
                inventorData = GetMaterialData();
            else if (Contains(normalized, "iproperty", "iproperties",
                "part number", "description", "metadata"))
                inventorData = GetIPropertiesData();
            else if (Contains(normalized, "selected", "selection"))
                inventorData = GetSelectionData();
            else
                inventorData = GetActiveDocumentContext();

            return prompt +
                   System.Environment.NewLine +
                   System.Environment.NewLine +
                   inventorData;
        }

        // ─── Local fallback ───────────────────────────────────────────

        private string GetLocalFallbackResponse(string prompt)
        {
            string normalized = prompt.Trim().ToLowerInvariant();

            if (Contains(normalized, "what file", "active file",
                "file open", "what document", "active document"))
                return GetActiveDocumentInfo();

            if (Contains(normalized, "list parameter",
                "show parameter", "parameters"))
                return GetParametersData();

            if (Contains(normalized, "material"))
                return GetMaterialData();

            if (Contains(normalized, "iproperty", "iproperties",
                "part number", "description"))
                return GetIPropertiesData();

            if (Contains(normalized, "selected", "selection"))
                return GetSelectionData();

            if (Contains(normalized, "inspect", "check",
                "review", "summarize"))
                return GetModelInspectionSummary();

            if (Contains(normalized, "help"))
                return
                    "Available commands:" +
                    System.Environment.NewLine +
                    "- 'what file is open'" +
                    System.Environment.NewLine +
                    "- 'list parameters'" +
                    System.Environment.NewLine +
                    "- 'show material'" +
                    System.Environment.NewLine +
                    "- 'show iproperties'" +
                    System.Environment.NewLine +
                    "- 'what is selected'" +
                    System.Environment.NewLine +
                    "- 'inspect' or 'summarize'" +
                    System.Environment.NewLine +
                    "- 'set [param] to [value]'" +
                    System.Environment.NewLine +
                    "- 'suppress [feature]'" +
                    System.Environment.NewLine +
                    "- 'unsuppress [feature]'" +
                    System.Environment.NewLine +
                    "- 'create model state [name]'" +
                    System.Environment.NewLine +
                    System.Environment.NewLine +
                    "Click Diagnose to test the AI connection.";

            return
                "Running in Local Mode. " +
                "Try: 'what file is open', 'list parameters', " +
                "'show material', 'show iproperties', " +
                "'what is selected', 'inspect', or 'help'." +
                System.Environment.NewLine +
                "Click Diagnose to test your Groq connection.";
        }

        // ─── Inventor data readers ────────────────────────────────────

        private string GetActiveDocumentContext()
        {
            try
            {
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "[No active document.]";

                return "[Inventor Context]" +
                       System.Environment.NewLine +
                       "Name: " + doc.DisplayName +
                       System.Environment.NewLine +
                       "Type: " + GetDocumentTypeName(doc) +
                       System.Environment.NewLine +
                       "Path: " + doc.FullFileName;
            }
            catch (Exception ex)
            {
                return "[Context error: " + ex.Message + "]";
            }
        }

        private string GetActiveDocumentInfo()
        {
            try
            {
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                return "Active document: " + doc.DisplayName +
                       " (" + GetDocumentTypeName(doc) + ")" +
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
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                Parameters parameters = null;

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                    parameters = ((PartDocument)doc)
                        .ComponentDefinition.Parameters;
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    parameters = ((AssemblyDocument)doc)
                        .ComponentDefinition.Parameters;
                else
                    return "Parameters are only available for " +
                           "Part and Assembly documents.";

                if (parameters == null || parameters.Count == 0)
                    return "No parameters found.";

                var sb = new System.Text.StringBuilder();
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
                return "Failed to read parameters: " + ex.Message;
            }
        }

        private string GetMaterialData()
        {
            try
            {
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType ==
                    DocumentTypeEnum.kPartDocumentObject)
                {
                    PartDocument part = (PartDocument)doc;
                    string mat =
                        part.ComponentDefinition.Material.Name;
                    double mass =
                        part.ComponentDefinition.MassProperties.Mass;
                    double vol =
                        part.ComponentDefinition.MassProperties.Volume;

                    return "Material: " + mat +
                           System.Environment.NewLine +
                           "Mass: " + Math.Round(mass, 4) + " kg" +
                           System.Environment.NewLine +
                           "Volume: " +
                           Math.Round(vol * 1e6, 4) + " cm³";
                }
                else if (doc.DocumentType ==
                    DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    AssemblyDocument asm = (AssemblyDocument)doc;
                    double mass =
                        asm.ComponentDefinition.MassProperties.Mass;

                    return "Assembly mass: " +
                           Math.Round(mass, 4) + " kg";
                }

                return "Material data not available for this " +
                       "document type.";
            }
            catch (Exception ex)
            {
                return "Failed to read material: " + ex.Message;
            }
        }

        private string GetIPropertiesData()
        {
            try
            {
                Document doc = _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("iProperties for " +
                    doc.DisplayName + ":");
                sb.AppendLine(new string('-', 40));

                foreach (PropertySet ps in doc.PropertySets)
                {
                    foreach (Property prop in ps)
                    {
                        try
                        {
                            string val = Convert.ToString(prop.Value);
                            if (!string.IsNullOrWhiteSpace(val))
                                sb.AppendLine(
                                    prop.Name.PadRight(30) + val);
                        }
                        catch { }
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read iProperties: " + ex.Message;
            }
        }

        private string GetSelectionData()
        {
            try
            {
                SelectSet sel =
                    _inventorApplication.ActiveDocument.SelectSet;

                if (sel == null || sel.Count == 0)
                    return "Nothing is currently selected. " +
                           "Select a face, edge, feature, or " +
                           "component and try again.";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Selected items (" +
                    sel.Count + "):");
                sb.AppendLine(new string('-', 40));

                int i = 1;

                foreach (object item in sel)
                {
                    try
                    {
                        string desc = item.GetType().Name;

                        if (item is PartFeature pf)
                            desc = "Feature: " + pf.Name;
                        else if (item is Face f)
                            desc = "Face (" +
                                   f.SurfaceType.ToString() + ")";
                        else if (item is Edge)
                            desc = "Edge";
                        else if (item is ComponentOccurrence co)
                            desc = "Component: " + co.Name;
                        else if (item is Parameter p)
                            desc = "Parameter: " + p.Name +
                                   " = " + p.Expression;

                        sb.AppendLine(i + ". " + desc);
                        i++;
                    }
                    catch
                    {
                        sb.AppendLine(i + ". (unreadable)");
                        i++;
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read selection: " + ex.Message;
            }
        }

        private string GetModelInspectionSummary()
        {
            return
                "Recommended inspection checklist:" +
                System.Environment.NewLine +
                "1. Parameters — design intent and errors" +
                System.Environment.NewLine +
                "2. Material — matches manufacturing requirements" +
                System.Environment.NewLine +
                "3. iProperties — Part Number and Description" +
                System.Environment.NewLine +
                "4. Feature health — warnings in Model browser" +
                System.Environment.NewLine +
                "5. Sketches — under-constrained references" +
                System.Environment.NewLine +
                "6. Manufacturing geometry — holes, bends, clearances" +
                System.Environment.NewLine +
                "7. Origin — sensible for assembly use" +
                System.Environment.NewLine +
                "8. Drawing readiness — parameter-driven dimensions";
        }

        // ─── Helpers ──────────────────────────────────────────────────

        private string GetDocumentTypeName(Document doc)
        {
            switch (doc.DocumentType)
            {
                case DocumentTypeEnum.kPartDocumentObject:
                    return "Part";
                case DocumentTypeEnum.kAssemblyDocumentObject:
                    return "Assembly";
                case DocumentTypeEnum.kDrawingDocumentObject:
                    return "Drawing";
                case DocumentTypeEnum.kPresentationDocumentObject:
                    return "Presentation";
                default:
                    return "Unknown";
            }
        }

        private bool Contains(string input, params string[] keywords)
        {
            foreach (string kw in keywords)
                if (input.Contains(kw)) return true;
            return false;
        }
    }
}