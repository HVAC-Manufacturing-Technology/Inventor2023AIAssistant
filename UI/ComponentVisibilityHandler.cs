using System;
using System.Collections.Generic;
using Inventor;

namespace Inventor2023AIAssistant
{
    /// <summary>
    /// Handles show/hide and suppress/unsuppress
    /// of assembly components via plain English chat.
    ///
    /// Supported commands:
    ///   hide <name>
    ///   show <name>
    ///   show all
    ///   hide all
    ///   suppress <name>
    ///   unsuppress <name>
    ///   unsuppress all
    ///   suppress all
    ///   list hidden components
    ///   list suppressed components
    /// </summary>
    public class ComponentVisibilityHandler
    {
        private readonly Inventor.Application _app;

        public ComponentVisibilityHandler(
            Inventor.Application app)
        {
            _app = app;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleVisibility(string prompt)
        {
            if (prompt == null) return null;
            string n = prompt.Trim().ToLowerInvariant();

            // ── Show all ──────────────────────────────────────────────
            if (n == "show all" ||
                n == "show all components" ||
                n == "unhide all" ||
                n == "make all visible")
                return ShowAll();

            // ── Hide all ──────────────────────────────────────────────
            if (n == "hide all" ||
                n == "hide all components")
                return HideAll();

            // ── Unsuppress all ────────────────────────────────────────
            if (n == "unsuppress all" ||
                n == "unsuppress all components" ||
                n == "activate all")
                return UnsuppressAll();

            // ── Suppress all ──────────────────────────────────────────
            if (n == "suppress all" ||
                n == "suppress all components")
                return SuppressAll();

            // ── List hidden ───────────────────────────────────────────
            if (n == "list hidden" ||
                n == "list hidden components" ||
                n == "show hidden components" ||
                n == "find hidden")
                return ListHidden();

            // ── List suppressed ───────────────────────────────────────
            if (n == "list suppressed" ||
                n == "list suppressed components" ||
                n == "find suppressed components" ||
                n == "show suppressed components")
                return ListSuppressed();

            // ── Hide <name> ───────────────────────────────────────────
            if (n.StartsWith("hide "))
            {
                string target = prompt.Trim()
                    .Substring(5).Trim();
                if (!string.IsNullOrWhiteSpace(target))
                    return HideComponent(target);
            }

            // ── Show <name> ───────────────────────────────────────────
            if (n.StartsWith("show "))
            {
                string target = prompt.Trim()
                    .Substring(5).Trim();
                if (!string.IsNullOrWhiteSpace(target))
                    return ShowComponent(target);
            }

            // ── Suppress <name> ───────────────────────────────────────
            if (n.StartsWith("suppress "))
            {
                string target = prompt.Trim()
                    .Substring(9).Trim();
                if (!string.IsNullOrWhiteSpace(target))
                    return SuppressComponent(target);
            }

            // ── Unsuppress <name> ─────────────────────────────────────
            if (n.StartsWith("unsuppress ") ||
                n.StartsWith("activate "))
            {
                int skip = n.StartsWith("unsuppress ")
                    ? 11 : 9;
                string target = prompt.Trim()
                    .Substring(skip).Trim();
                if (!string.IsNullOrWhiteSpace(target))
                    return UnsuppressComponent(target);
            }

            // ── Toggle visibility <name> ──────────────────────────────
            if (n.StartsWith("toggle ") ||
                n.StartsWith("toggle visibility "))
            {
                int skip = n.StartsWith(
                    "toggle visibility ") ? 18 : 7;
                string target = prompt.Trim()
                    .Substring(skip).Trim();
                if (!string.IsNullOrWhiteSpace(target))
                    return ToggleVisibility(target);
            }

            return null; // Not handled here
        }

        // ─── Show single component ────────────────────────────────────

        private string ShowComponent(string name)
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open. " +
                           "Show/hide requires an assembly.";

                var matches = FindComponents(
                    asm, name);

                if (matches.Count == 0)
                    return "No component matching '" +
                           name + "' was found. " +
                           "Type 'list components' " +
                           "to see all component names.";

                int count = 0;
                foreach (var occ in matches)
                {
                    occ.Visible = true;
                    count++;
                }

                return "✅ Showed " + count +
                       " component(s) matching '" +
                       name + "'.";
            }
            catch (Exception ex)
            {
                return "Failed to show component: " +
                       ex.Message;
            }
        }

        // ─── Hide single component ────────────────────────────────────

        private string HideComponent(string name)
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open. " +
                           "Show/hide requires an assembly.";

                var matches = FindComponents(asm, name);

                if (matches.Count == 0)
                    return "No component matching '" +
                           name + "' was found. " +
                           "Type 'list components' " +
                           "to see all component names.";

                int count = 0;
                foreach (var occ in matches)
                {
                    occ.Visible = false;
                    count++;
                }

                return "✅ Hid " + count +
                       " component(s) matching '" +
                       name + "'.";
            }
            catch (Exception ex)
            {
                return "Failed to hide component: " +
                       ex.Message;
            }
        }

        // ─── Toggle visibility ────────────────────────────────────────

        private string ToggleVisibility(string name)
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                var matches = FindComponents(asm, name);

                if (matches.Count == 0)
                    return "No component matching '" +
                           name + "' was found.";

                int shown = 0;
                int hidden = 0;
                foreach (var occ in matches)
                {
                    occ.Visible = !occ.Visible;
                    if (occ.Visible) shown++;
                    else hidden++;
                }

                return "✅ Toggled " + matches.Count +
                       " component(s) matching '" +
                       name + "': " +
                       shown + " shown, " +
                       hidden + " hidden.";
            }
            catch (Exception ex)
            {
                return "Failed to toggle: " + ex.Message;
            }
        }

        // ─── Show all ─────────────────────────────────────────────────

        private string ShowAll()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                int count = 0;
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (!occ.Visible)
                        {
                            occ.Visible = true;
                            count++;
                        }
                    }
                    catch { }
                }

                return count == 0
                    ? "All components are already visible."
                    : "✅ Showed " + count +
                      " hidden component(s).";
            }
            catch (Exception ex)
            {
                return "Failed to show all: " + ex.Message;
            }
        }

        // ─── Hide all ─────────────────────────────────────────────────

        private string HideAll()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                int count = 0;
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Visible)
                        {
                            occ.Visible = false;
                            count++;
                        }
                    }
                    catch { }
                }

                return count == 0
                    ? "All components are already hidden."
                    : "✅ Hid " + count + " component(s).";
            }
            catch (Exception ex)
            {
                return "Failed to hide all: " + ex.Message;
            }
        }

        // ─── Suppress single component ────────────────────────────────

        private string SuppressComponent(string name)
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open. " +
                           "Suppress requires an assembly.";

                var matches = FindComponents(asm, name);

                if (matches.Count == 0)
                    return "No component matching '" +
                           name + "' was found. " +
                           "Type 'list components' " +
                           "to see all component names.";

                int count = 0;
                foreach (var occ in matches)
                {
                    occ.Suppress();
                    count++;
                }

                return "✅ Suppressed " + count +
                       " component(s) matching '" +
                       name + "'.";
            }
            catch (Exception ex)
            {
                return "Failed to suppress: " + ex.Message;
            }
        }

        // ─── Unsuppress single component ──────────────────────────────

        private string UnsuppressComponent(string name)
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                var matches = FindComponents(asm, name);

                if (matches.Count == 0)
                    return "No component matching '" +
                           name + "' was found. " +
                           "Type 'list components' " +
                           "to see all component names.";

                int count = 0;
                foreach (var occ in matches)
                {
                    occ.Unsuppress();
                    count++;
                }

                return "✅ Unsuppressed " + count +
                       " component(s) matching '" +
                       name + "'.";
            }
            catch (Exception ex)
            {
                return "Failed to unsuppress: " +
                       ex.Message;
            }
        }

        // ─── Suppress all ─────────────────────────────────────────────

        private string SuppressAll()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                int count = 0;
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (!occ.Suppressed)
                        {
                            occ.Suppress();
                            count++;
                        }
                    }
                    catch { }
                }

                return count == 0
                    ? "All components are already suppressed."
                    : "✅ Suppressed " + count +
                      " component(s).";
            }
            catch (Exception ex)
            {
                return "Failed to suppress all: " +
                       ex.Message;
            }
        }

        // ─── Unsuppress all ───────────────────────────────────────────

        private string UnsuppressAll()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                int count = 0;
                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Suppressed)
                        {
                            occ.Unsuppress();
                            count++;
                        }
                    }
                    catch { }
                }

                return count == 0
                    ? "All components are already active."
                    : "✅ Unsuppressed " + count +
                      " component(s).";
            }
            catch (Exception ex)
            {
                return "Failed to unsuppress all: " +
                       ex.Message;
            }
        }

        // ─── List hidden ──────────────────────────────────────────────

        private string ListHidden()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                var sb = new System.Text.StringBuilder();
                int count = 0;

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (!occ.Visible)
                        {
                            sb.AppendLine(
                                "  - " + occ.Name);
                            count++;
                        }
                    }
                    catch { }
                }

                if (count == 0)
                    return "No hidden components found.";

                return "Hidden components (" +
                       count + "):" +
                       System.Environment.NewLine +
                       sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list hidden: " +
                       ex.Message;
            }
        }

        // ─── List suppressed ──────────────────────────────────────────

        private string ListSuppressed()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                var sb = new System.Text.StringBuilder();
                int count = 0;

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Suppressed)
                        {
                            sb.AppendLine(
                                "  - " + occ.Name);
                            count++;
                        }
                    }
                    catch { }
                }

                if (count == 0)
                    return "No suppressed components found.";

                return "Suppressed components (" +
                       count + "):" +
                       System.Environment.NewLine +
                       sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list suppressed: " +
                       ex.Message;
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────

        // Finds all components whose name contains
        // the search string (case-insensitive)
        private List<ComponentOccurrence> FindComponents(
            AssemblyDocument asm, string name)
        {
            var results =
                new List<ComponentOccurrence>();
            string search = name.ToLowerInvariant();

            foreach (ComponentOccurrence occ in
                asm.ComponentDefinition.Occurrences)
            {
                try
                {
                    if (occ.Name.ToLowerInvariant()
                            .Contains(search))
                        results.Add(occ);
                }
                catch { }
            }
            return results;
        }

        private AssemblyDocument GetAssembly()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null) return null;
                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return null;
                return (AssemblyDocument)doc;
            }
            catch { return null; }
        }
    }
}