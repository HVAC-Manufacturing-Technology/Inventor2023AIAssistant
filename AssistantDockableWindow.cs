using System;
using System.Windows.Forms;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class AssistantDockableWindow
    {
        private DockableWindow _dockableWindow;
        private AssistantPanel _assistantPanel;
        private Inventor.Application _inventorApplication;
        private readonly string _clientId;
        private KeyboardHook _keyboardHook;

        public AssistantDockableWindow(
            Inventor.Application inventorApplication,
            string clientId)
        {
            _inventorApplication = inventorApplication;
            _clientId = clientId;
        }

        public void SetKeyboardHook(KeyboardHook hook)
        {
            _keyboardHook = hook;
        }

        // ─── Create ───────────────────────────────────────────────────

        public void Create()
        {
            try
            {
                UserInterfaceManager uiMgr =
                    _inventorApplication.UserInterfaceManager;

                DockableWindows dockableWindows =
                    uiMgr.DockableWindows;

                try
                {
                    _dockableWindow = dockableWindows[
                        "Inventor2023AIAssistant.DockableWindow"];

                    if (_dockableWindow != null)
                    {
                        _dockableWindow.Visible = true;
                        _assistantPanel?.FocusPrompt();
                        return;
                    }
                }
                catch { }

                _dockableWindow = dockableWindows.Add(
                    _clientId,
                    "Inventor2023AIAssistant.DockableWindow",
                    "AI Assistant"
                );

                _dockableWindow.DockingState =
                    DockingStateEnum.kDockRight;

                _dockableWindow.Width = 420;
                _dockableWindow.ShowVisibilityCheckBox = false;
                _dockableWindow.ShowTitleBar = true;

                _assistantPanel = new AssistantPanel(
                    _inventorApplication);

                // Register panel handle with hook so it
                // only fires when the panel is focused
                if (_keyboardHook != null)
                {
                    _keyboardHook.SetPanelHandle(
                        _assistantPanel.Handle);
                }

                _dockableWindow.AddChild(
                    _assistantPanel.Handle);

                _dockableWindow.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to create dockable window: " +
                    ex.Message,
                    "Inventor 2023 AI Assistant");
            }
        }

        // ─── Toggle ───────────────────────────────────────────────────

        public void Toggle()
        {
            if (_dockableWindow == null)
            {
                Create();
                return;
            }

            _dockableWindow.Visible =
                !_dockableWindow.Visible;

            if (_dockableWindow.Visible)
                _assistantPanel?.FocusPrompt();
        }

        public bool IsVisible()
        {
            return _dockableWindow != null &&
                   _dockableWindow.Visible;
        }

        // ─── Keyboard passthrough ─────────────────────────────────────

        public void InsertSpaceFromInventor()
        {
            if (_dockableWindow != null &&
                _dockableWindow.Visible)
                _assistantPanel?.InsertSpaceFromInventor();
        }

        public void SendEnterFromInventor()
        {
            if (_dockableWindow != null &&
                _dockableWindow.Visible)
                _assistantPanel?.SendEnterFromInventor();
        }

        public void InsertNewlineFromInventor()
        {
            if (_dockableWindow != null &&
                _dockableWindow.Visible)
                _assistantPanel?.InsertNewlineFromInventor();
        }

        public void LogKeyPress(int keyASCII)
        {
            if (_dockableWindow != null &&
                _dockableWindow.Visible)
                _assistantPanel?.LogKeyPress(keyASCII);
        }

        // ─── Destroy ──────────────────────────────────────────────────

        public void Destroy()
        {
            try
            {
                if (_assistantPanel != null &&
                    !_assistantPanel.IsDisposed)
                {
                    _assistantPanel.Dispose();
                    _assistantPanel = null;
                }

                if (_dockableWindow != null)
                {
                    _dockableWindow.Visible = false;
                    _dockableWindow = null;
                }
            }
            catch { }
        }
    }
}