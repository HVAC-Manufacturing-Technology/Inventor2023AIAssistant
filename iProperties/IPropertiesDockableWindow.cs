using System;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class IPropertiesDockableWindow
    {
        private Inventor.Application _app;
        private IPropertiesEditor _editor;
        private DockableWindow _dockableWindow;
        private IPropertiesPanel _panel;
        private readonly string _clientId;
        private bool _created = false;

        public IPropertiesDockableWindow(
            Inventor.Application app,
            IPropertiesEditor editor,
            string clientId)
        {
            _app = app;
            _editor = editor;
            _clientId = clientId;
        }

        public void Create()
        {
            if (_created) return;

            try
            {
                UserInterfaceManager uiMgr =
                    _app.UserInterfaceManager;

                // Check if window already exists
                try
                {
                    foreach (DockableWindow dw in
                        uiMgr.DockableWindows)
                    {
                        if (dw.InternalName ==
                            "IPropertiesPanel")
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
                        "IPropertiesPanel",
                        "iProperties");

                _dockableWindow.SetMinimumSize(50, 50);

                // Create panel and force handle creation
                _panel = new IPropertiesPanel(
                    _app, _editor);

                // Force the window handle to be created
                // before passing to Inventor
                IntPtr handle = _panel.Handle;

                _dockableWindow.AddChild(handle);
                _dockableWindow.Visible = false;
                _created = true;
            }
            catch (Exception ex)
            {
                _created = false;
                System.Diagnostics.Debug.WriteLine(
                    "IPropertiesDockableWindow.Create: " +
                    ex.Message);
                throw new Exception(
                    "Failed to create iProperties panel: " +
                    ex.Message);
            }
        }

        public void Show()
        {
            try
            {
                if (!_created || _dockableWindow == null)
                    Create();

                _panel?.LoadProperties();
                _dockableWindow.Visible = true;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Failed to open iProperties: " +
                    ex.Message);
            }
        }

        public void Hide()
        {
            try
            {
                if (_dockableWindow != null)
                    _dockableWindow.Visible = false;
            }
            catch { }
        }

        public void Toggle()
        {
            try
            {
                if (!_created || _dockableWindow == null)
                { Show(); return; }
                if (_dockableWindow.Visible) Hide();
                else Show();
            }
            catch { }
        }

        public bool IsVisible =>
            _dockableWindow?.Visible ?? false;
    }
}