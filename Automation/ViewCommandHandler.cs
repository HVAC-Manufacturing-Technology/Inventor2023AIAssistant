using System;
using Inventor;

namespace Inventor2023AIAssistant
{
    /// <summary>
    /// Handles Inventor viewport and display
    /// commands from plain English chat.
    ///
    /// Commands:
    ///   zoom to fit / fit all
    ///   home view / reset view
    ///   isometric view / iso view
    ///   top / front / right / back / bottom / left view
    ///   rotate view left / right
    ///   zoom in / zoom out
    ///   orbit view / free orbit
    ///   look at face / look at selected
    ///   previous view / next view
    ///   wireframe / shaded / shaded with edges
    ///   hidden line view
    /// </summary>
    public class ViewCommandHandler
    {
        private readonly Inventor.Application _app;

        public ViewCommandHandler(
            Inventor.Application app)
        {
            _app = app;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleViewCommand(string prompt)
        {
            if (prompt == null) return null;
            string n = prompt.Trim().ToLowerInvariant();

            // ── Zoom to fit ───────────────────────────────────────────
            if (n == "zoom to fit" ||
                n == "fit all" ||
                n == "zoom all" ||
                n == "fit view" ||
                n == "zoom fit")
                return RunCmd("AppZoomAllCmd",
                    "Zoomed to fit all.");

            // ── Home view ─────────────────────────────────────────────
            if (n == "home view" ||
                n == "reset view" ||
                n == "default view" ||
                n == "go home")
                return RunCmd("AppHomeViewCmd",
                    "Home view restored.");

            // ── Isometric ─────────────────────────────────────────────
            if (n == "isometric view" ||
                n == "iso view" ||
                n == "isometric" ||
                n == "iso")
                return RunCmd("AppIsometricViewCmd",
                    "Isometric view applied.");

            // ── Top view ──────────────────────────────────────────────
            if (n == "top view" ||
                n == "view from top" ||
                n == "look from top")
                return RunCmd("AppTopViewCmd",
                    "Top view applied.");

            // ── Front view ────────────────────────────────────────────
            if (n == "front view" ||
                n == "view from front" ||
                n == "look from front")
                return RunCmd("AppFrontViewCmd",
                    "Front view applied.");

            // ── Right view ────────────────────────────────────────────
            if (n == "right view" ||
                n == "view from right" ||
                n == "look from right")
                return RunCmd("AppRightViewCmd",
                    "Right view applied.");

            // ── Back view ─────────────────────────────────────────────
            if (n == "back view" ||
                n == "view from back")
                return RunCmd("AppBackViewCmd",
                    "Back view applied.");

            // ── Bottom view ───────────────────────────────────────────
            if (n == "bottom view" ||
                n == "view from bottom")
                return RunCmd("AppBottomViewCmd",
                    "Bottom view applied.");

            // ── Left view ─────────────────────────────────────────────
            if (n == "left view" ||
                n == "view from left")
                return RunCmd("AppLeftViewCmd",
                    "Left view applied.");

            // ── Rotate left ───────────────────────────────────────────
            if (n == "rotate view left" ||
                n == "rotate left")
                return RunCmd("AppRotateViewLeftCmd",
                    "View rotated left.");

            // ── Rotate right ──────────────────────────────────────────
            if (n == "rotate view right" ||
                n == "rotate right")
                return RunCmd("AppRotateViewRightCmd",
                    "View rotated right.");

            // ── Zoom in ───────────────────────────────────────────────
            if (n == "zoom in")
                return RunCmd("AppZoomInCmd",
                    "Zoomed in.");

            // ── Zoom out ──────────────────────────────────────────────
            if (n == "zoom out")
                return RunCmd("AppZoomOutCmd",
                    "Zoomed out.");

            // ── Pan ───────────────────────────────────────────────────
            if (n == "pan view" || n == "pan")
                return RunCmd("AppPanCmd",
                    "Pan mode activated.");

            // ── Free orbit ────────────────────────────────────────────
            if (n == "orbit view" ||
                n == "free orbit" ||
                n == "orbit" ||
                n == "rotate model")
                return RunCmd("AppFreeOrbitCmd",
                    "Free orbit activated.");

            // ── Look at ───────────────────────────────────────────────
            if (n == "look at face" ||
                n == "look at selected" ||
                n == "look at")
                return RunCmd("AppLookAtCmd",
                    "Look-at applied to selection.");

            // ── Previous view ─────────────────────────────────────────
            if (n == "previous view" ||
                n == "prev view" ||
                n == "back view history")
                return RunCmd("AppPreviousViewCmd",
                    "Previous view restored.");

            // ── Next view ─────────────────────────────────────────────
            if (n == "next view")
                return RunCmd("AppNextViewCmd",
                    "Next view restored.");

            // ── Wireframe ─────────────────────────────────────────────
            if (n == "wireframe" ||
                n == "wireframe view" ||
                n == "wire frame")
                return RunCmd(
                    "AppWireframeDisplayCmd",
                    "Wireframe display applied.");

            // ── Shaded ────────────────────────────────────────────────
            if (n == "shaded" ||
                n == "shaded view" ||
                n == "shade")
                return RunCmd(
                    "AppShadedDisplayCmd",
                    "Shaded display applied.");

            // ── Shaded with edges ─────────────────────────────────────
            if (n == "shaded with edges" ||
                n == "shaded edges" ||
                n == "shaded with edge" ||
                n == "shade with edges")
                return RunCmd(
                    "AppShadedWithEdgesDisplayCmd",
                    "Shaded with edges display applied.");

            // ── Hidden line ───────────────────────────────────────────
            if (n == "hidden line" ||
                n == "hidden line view" ||
                n == "hidden lines")
                return RunCmd(
                    "AppHiddenLineDisplayCmd",
                    "Hidden line display applied.");

            return null;
        }

        // ─── Execute Inventor command ─────────────────────────────────

        private string RunCmd(
            string cmdName, string successMsg)
        {
            try
            {
                _app.CommandManager
                    .ControlDefinitions[cmdName]
                    .Execute();
                return "\u2705 " + successMsg;
            }
            catch (Exception ex)
            {
                return "Failed to execute view command: " +
                       ex.Message +
                       System.Environment.NewLine +
                       "Command: " + cmdName;
            }
        }
    }
}
