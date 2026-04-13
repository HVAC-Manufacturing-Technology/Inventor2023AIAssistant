using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Inventor;

namespace Inventor2023AIAssistant
{
    [Guid("C3B2E8D3-7E5F-4A88-9E9E-1F4A8A202301")]
    [ComVisible(true)]
    [ProgId("Inventor2023AIAssistant.StandardAddInServer")]
    public class StandardAddInServer : ApplicationAddInServer
    {
        private Inventor.Application _inventorApplication;
        private ButtonDefinition _buttonDef;
        private AssistantDockableWindow _dockableWindow;
        private KeyboardHook _keyboardHook;

        private readonly string _clientId =
            "{C3B2E8D3-7E5F-4A88-9E9E-1F4A8A202301}";

        // ─── Activate ─────────────────────────────────────────────────

        public void Activate(
            ApplicationAddInSite addInSiteObject,
            bool firstTime)
        {
            _inventorApplication = addInSiteObject.Application;

            ControlDefinitions controlDefs =
                _inventorApplication.CommandManager
                                    .ControlDefinitions;

            try
            {
                _buttonDef = controlDefs[
                    "Inventor2023AIAssistant_Button"]
                    as ButtonDefinition;
            }
            catch
            {
                Image img16 =
                    Properties.Resources.ResourceManager
                              .GetObject("AI16") as Image;
                Image img32 =
                    Properties.Resources.ResourceManager
                              .GetObject("AI32") as Image;

                stdole.IPictureDisp smallIcon = null;
                stdole.IPictureDisp largeIcon = null;

                if (img16 != null)
                    smallIcon =
                        PictureConverter.ImageToPictureDisp(img16);

                if (img32 != null)
                    largeIcon =
                        PictureConverter.ImageToPictureDisp(img32);

                _buttonDef = controlDefs.AddButtonDefinition(
                    "AI Assistant",
                    "Inventor2023AIAssistant_Button",
                    CommandTypesEnum.kNonShapeEditCmdType,
                    _clientId,
                    "Open Inventor 2023 AI Assistant",
                    "Open Inventor 2023 AI Assistant",
                    smallIcon,
                    largeIcon
                );
            }

            _buttonDef.OnExecute += ButtonDef_OnExecute;

            CreateUserInterface();
            InstallKeyboardHook();

            // Auto-open the assistant panel
            // every time Inventor starts
            AutoOpenAssistant();
        }

        // ─── Auto open ────────────────────────────────────────────────

        private void AutoOpenAssistant()
        {
            try
            {
                if (_dockableWindow == null)
                {
                    _dockableWindow =
                        new AssistantDockableWindow(
                            _inventorApplication,
                            _clientId);

                    _dockableWindow.SetKeyboardHook(
                        _keyboardHook);

                    _dockableWindow.Create();
                }
                else
                {
                    if (!_dockableWindow.IsVisible())
                        _dockableWindow.Toggle();
                }
            }
            catch { }
        }

        // ─── UI ───────────────────────────────────────────────────────

        private void CreateUserInterface()
        {
            UserInterfaceManager uiMgr =
                _inventorApplication.UserInterfaceManager;

            Ribbon partRibbon = uiMgr.Ribbons["Part"];

            RibbonTab assistantTab = null;
            RibbonPanel assistantPanel = null;

            try
            {
                assistantTab = partRibbon.RibbonTabs[
                    "id_Inventor2023AIAssistant_Tab"];
            }
            catch
            {
                assistantTab = partRibbon.RibbonTabs.Add(
                    "Assistant",
                    "id_Inventor2023AIAssistant_Tab",
                    _clientId,
                    "",
                    false
                );
            }

            try
            {
                assistantPanel = assistantTab.RibbonPanels[
                    "id_Inventor2023AIAssistant_Panel"];
            }
            catch
            {
                assistantPanel = assistantTab.RibbonPanels.Add(
                    "Assistant",
                    "id_Inventor2023AIAssistant_Panel",
                    _clientId,
                    "",
                    false
                );
            }

            bool buttonExists = false;

            foreach (CommandControl control in
                assistantPanel.CommandControls)
            {
                if (control.InternalName ==
                    "Inventor2023AIAssistant_Button")
                {
                    buttonExists = true;
                    break;
                }
            }

            if (!buttonExists)
            {
                assistantPanel.CommandControls.AddButton(
                    _buttonDef,
                    true,
                    true,
                    "",
                    false
                );
            }
        }

        // ─── Button ───────────────────────────────────────────────────

        private void ButtonDef_OnExecute(NameValueMap Context)
        {
            if (_dockableWindow == null)
            {
                _dockableWindow =
                    new AssistantDockableWindow(
                        _inventorApplication,
                        _clientId);

                _dockableWindow.SetKeyboardHook(_keyboardHook);

                _dockableWindow.Create();
            }
            else
            {
                // Button toggles the panel open and closed
                _dockableWindow.Toggle();
            }
        }

        // ─── Keyboard hook ────────────────────────────────────────────

        private void InstallKeyboardHook()
        {
            try
            {
                _keyboardHook = new KeyboardHook();

                _keyboardHook.OnSpacePressed += () =>
                {
                    if (_dockableWindow != null &&
                        _dockableWindow.IsVisible())
                    {
                        _dockableWindow
                            .InsertSpaceFromInventor();
                    }
                };

                _keyboardHook.OnEnterPressed += () =>
                {
                    if (_dockableWindow != null &&
                        _dockableWindow.IsVisible())
                    {
                        _dockableWindow
                            .SendEnterFromInventor();
                    }
                };

                _keyboardHook.OnShiftEnterPressed += () =>
                {
                    if (_dockableWindow != null &&
                        _dockableWindow.IsVisible())
                    {
                        _dockableWindow
                            .InsertNewlineFromInventor();
                    }
                };

                _keyboardHook.Install();
            }
            catch { }
        }

        private void UninstallKeyboardHook()
        {
            try
            {
                if (_keyboardHook != null)
                {
                    _keyboardHook.Uninstall();
                    _keyboardHook.Dispose();
                    _keyboardHook = null;
                }
            }
            catch { }
        }

        // ─── Deactivate ───────────────────────────────────────────────

        public void Deactivate()
        {
            try
            {
                UninstallKeyboardHook();

                if (_buttonDef != null)
                    _buttonDef.OnExecute -= ButtonDef_OnExecute;

                _dockableWindow?.Destroy();
                _dockableWindow = null;
            }
            catch { }

            _buttonDef = null;
            _inventorApplication = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ExecuteCommand(int CommandID) { }

        public object Automation
        {
            get { return null; }
        }
    }
}