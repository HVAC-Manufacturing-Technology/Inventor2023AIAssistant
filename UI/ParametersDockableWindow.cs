using Inventor;
using System;

namespace Inventor2023AIAssistant
{
    public class ParametersDockableWindow
    {
        private readonly Inventor.Application _app;
        private readonly string _clientId;
        private DockableWindow _dockableWindow;
        private ParametersPanel _panel;
        private bool _created;

        public ParametersDockableWindow(
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

                // Check if already exists
                try
                {
                    foreach (DockableWindow dw in
                        uiMgr.DockableWindows)
                    {
                        if (dw.InternalName ==
                            "ParametersPanel")
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
                        "ParametersPanel",
                        "Parameters");

                _dockableWindow.SetMinimumSize(
                    50, 50);

                _panel =
                    new ParametersPanel(_app);

                IntPtr handle = _panel.Handle;

                _dockableWindow.AddChild(handle);
                _dockableWindow.Visible = false;
                _created = true;
            }
            catch (Exception ex)
            {
                _created = false;
                System.Diagnostics.Debug.WriteLine(
                    "ParametersDockableWindow: " +
                    ex.Message);
            }
        }

        public void Show()
        {
            Create();
            if (_panel != null)
                _panel.LoadParameters();
            if (_dockableWindow != null)
                _dockableWindow.Visible = true;
        }

        public void Hide()
        {
            if (_dockableWindow != null)
                _dockableWindow.Visible = false;
        }

        public void Toggle()
        {
            Create();
            if (_dockableWindow == null) return;

            if (_dockableWindow.Visible)
                Hide();
            else
                Show();
        }
    }
}