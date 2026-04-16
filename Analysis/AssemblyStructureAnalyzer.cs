using System;
using System.Collections.Generic;
using Inventor;

namespace Inventor2023AIAssistant
{
    /// <summary>
    /// Builds an indented assembly hierarchy tree
    /// and provides assembly statistics from chat.
    ///
    /// Commands:
    ///   assembly structure
    ///   component hierarchy
    ///   assembly tree
    ///   show structure
    ///   count components
    ///   how many components
    ///   list sub assemblies
    ///   assembly summary
    /// </summary>
    public class AssemblyStructureAnalyzer
    {
        private readonly Inventor.Application _app;
        private const int MaxDepth = 4;

        public AssemblyStructureAnalyzer(
            Inventor.Application app)
        {
            _app = app;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleStructure(string prompt)
        {
            if (prompt == null) return null;
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("assembly structure") ||
                n.Contains("component hierarchy") ||
                n.Contains("assembly tree") ||
                n.Contains("show structure") ||
                n.Contains("assembly layout"))
                return BuildStructureTree();

            if (n.Contains("count components") ||
                n.Contains("how many components") ||
                n.Contains("component count"))
                return CountComponents();

            if (n.Contains("list sub") ||
                n.Contains("find sub") ||
                n.Contains("subassembl"))
                return ListSubAssemblies();

            if (n == "assembly summary" ||
                n == "summarize assembly")
                return AssemblySummary();

            return null;
        }

        // ─── Build indented tree ──────────────────────────────────────

        private string BuildStructureTree()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open. " +
                           "Open an assembly first.";

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine(
                    "\u2500\u2500 Assembly Structure \u2500\u2500");
                sb.AppendLine(asm.DisplayName);

                var occs = asm.ComponentDefinition
                              .Occurrences;
                int total = occs.Count;

                for (int i = 1; i <= total; i++)
                {
                    try
                    {
                        ComponentOccurrence occ =
                            occs[i];
                        bool isLast = (i == total);
                        string prefix = isLast
                            ? "\u2514\u2500 "
                            : "\u251c\u2500 ";
                        string childPrefix = isLast
                            ? "   "
                            : "\u2502  ";

                        string flags = GetFlags(occ);
                        sb.AppendLine(
                            prefix + occ.Name + flags);

                        // Recurse into sub-assemblies
                        if (occ.Definition is
                            AssemblyComponentDefinition
                            asmDef)
                        {
                            AppendChildren(
                                sb, asmDef.Occurrences,
                                childPrefix, 1);
                        }
                    }
                    catch { }
                }

                sb.AppendLine();
                sb.AppendLine("Total top-level: " +
                    total + " component(s)");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to build structure: " +
                       ex.Message;
            }
        }

        private void AppendChildren(
            System.Text.StringBuilder sb,
            ComponentOccurrences occs,
            string prefix,
            int depth)
        {
            if (depth >= MaxDepth) return;
            int total = occs.Count;

            for (int i = 1; i <= total; i++)
            {
                try
                {
                    ComponentOccurrence occ = occs[i];
                    bool isLast = (i == total);
                    string connector = isLast
                        ? "\u2514\u2500 "
                        : "\u251c\u2500 ";
                    string childPrefix = prefix +
                        (isLast ? "   " : "\u2502  ");

                    string flags = GetFlags(occ);
                    sb.AppendLine(prefix + connector +
                        occ.Name + flags);

                    if (occ.Definition is
                        AssemblyComponentDefinition
                        asmDef)
                    {
                        AppendChildren(
                            sb, asmDef.Occurrences,
                            childPrefix, depth + 1);
                    }
                }
                catch { }
            }
        }

        private string GetFlags(ComponentOccurrence occ)
        {
            var flags = new List<string>();
            try
            {
                if (!occ.Visible) flags.Add("hidden");
                if (occ.Suppressed)
                    flags.Add("suppressed");
                if (occ.Definition is
                    AssemblyComponentDefinition)
                    flags.Add("sub-asm");
            }
            catch { }

            return flags.Count > 0
                ? " [" + string.Join(", ", flags) + "]"
                : "";
        }

        // ─── Count components ─────────────────────────────────────────

        private string CountComponents()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                int total = 0;
                int hidden = 0;
                int suppressed = 0;
                int subAsm = 0;
                var uniqueNames =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        total++;
                        if (!occ.Visible) hidden++;
                        if (occ.Suppressed) suppressed++;
                        if (occ.Definition is
                            AssemblyComponentDefinition)
                            subAsm++;

                        // Get base name without :N suffix
                        string name = occ.Name;
                        int colon = name.LastIndexOf(':');
                        if (colon > 0)
                            name = name.Substring(0,
                                colon).Trim();
                        uniqueNames.Add(name);
                    }
                    catch { }
                }

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine(
                    "\u2500\u2500 Component Count \u2500\u2500");
                sb.AppendLine("Total occurrences:  " +
                    total);
                sb.AppendLine("Unique parts:       " +
                    uniqueNames.Count);
                sb.AppendLine("Sub-assemblies:     " +
                    subAsm);
                sb.AppendLine("Hidden:             " +
                    hidden);
                sb.AppendLine("Suppressed:         " +
                    suppressed);
                sb.AppendLine("Visible & active:   " +
                    (total - hidden - suppressed));

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to count components: " +
                       ex.Message;
            }
        }

        // ─── List sub-assemblies ──────────────────────────────────────

        private string ListSubAssemblies()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine(
                    "\u2500\u2500 Sub-Assemblies \u2500\u2500");

                int count = 0;

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (!(occ.Definition is
                            AssemblyComponentDefinition
                            asmDef)) continue;

                        count++;
                        int childCount =
                            asmDef.Occurrences.Count;
                        string flags = GetFlags(occ);

                        sb.AppendLine(
                            "\u2022 " + occ.Name +
                            flags +
                            System.Environment.NewLine +
                            "  \u2514 " + childCount +
                            " component(s) inside");
                    }
                    catch { }
                }

                if (count == 0)
                {
                    sb.AppendLine(
                        "No sub-assemblies found " +
                        "at the top level.");
                    sb.AppendLine(
                        "This assembly contains " +
                        "only part files.");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine(
                        "Total sub-assemblies: " + count);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list sub-assemblies: " +
                       ex.Message;
            }
        }

        // ─── Assembly summary ─────────────────────────────────────────

        private string AssemblySummary()
        {
            try
            {
                AssemblyDocument asm = GetAssembly();
                if (asm == null)
                    return "No assembly is open.";

                var sb =
                    new System.Text.StringBuilder();
                sb.AppendLine(
                    "\u2500\u2500 Assembly Summary \u2500\u2500");
                sb.AppendLine("File: " +
                    asm.DisplayName);
                sb.AppendLine();

                int total = 0;
                int hidden = 0;
                int suppressed = 0;
                int subAsm = 0;

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        total++;
                        string flags = GetFlags(occ);
                        sb.AppendLine(
                            "\u2022 " + occ.Name + flags);

                        if (!occ.Visible) hidden++;
                        if (occ.Suppressed) suppressed++;
                        if (occ.Definition is
                            AssemblyComponentDefinition)
                            subAsm++;
                    }
                    catch { }
                }

                sb.AppendLine();
                sb.AppendLine(new string('\u2500', 30));
                sb.AppendLine("Total:       " + total);
                sb.AppendLine("Sub-asm:     " + subAsm);
                sb.AppendLine("Hidden:      " + hidden);
                sb.AppendLine("Suppressed:  " + suppressed);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to summarize assembly: " +
                       ex.Message;
            }
        }

        // ─── Helper ───────────────────────────────────────────────────

        private AssemblyDocument GetAssembly()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                if (doc == null) return null;
                if (doc.DocumentType !=
                    DocumentTypeEnum
                        .kAssemblyDocumentObject)
                    return null;
                return (AssemblyDocument)doc;
            }
            catch { return null; }
        }
    }
}
