using System;
using System.Text;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class ConstraintAnalyzer
    {
        private Inventor.Application _inventorApplication;

        public ConstraintAnalyzer(
            Inventor.Application inventorApplication)
        {
            _inventorApplication = inventorApplication;
        }

        // ─── Entry point ──────────────────────────────────────────────

        public string TryHandleConstraints(string prompt)
        {
            string n = prompt.Trim().ToLowerInvariant();

            if (n.Contains("assembly constraint") ||
                n.Contains("check constraint") ||
                n.Contains("constraint analysis") ||
                n.Contains("constraint report") ||
                n.Contains("assembly constrained"))
                return RunConstraintAnalysis();

            if (n.Contains("fully constrained component") ||
                n.Contains("over constrained component") ||
                n.Contains("under constrained component") ||
                n.Contains("unconstrained component") ||
                n.Contains("free component"))
                return FindConstraintIssues(prompt);

            if (n.Contains("what constraint") ||
                n.Contains("list constraint") ||
                n.Contains("show constraint"))
                return ListConstraints(prompt);

            if (n.Contains("grounded component") ||
                n.Contains("find grounded"))
                return FindGroundedComponents();

            if (n.Contains("degrees of freedom") ||
                n.Contains("dof") ||
                n.Contains("how many dof"))
                return CheckDegreesOfFreedom();

            return null;
        }

        // ─── Full constraint analysis ─────────────────────────────────

        private string RunConstraintAnalysis()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Constraint analysis requires " +
                           "an Assembly document.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Assembly Constraint Analysis ──");
                sb.AppendLine(
                    "Assembly: " + doc.DisplayName);
                sb.AppendLine(new string('═', 45));

                int total = 0;
                int grounded = 0;
                int fullyConstrained = 0;
                int underConstrained = 0;
                int overConstrained = 0;
                int suppressed = 0;

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        total++;

                        if (occ.Suppressed)
                        {
                            suppressed++;
                            continue;
                        }

                        // Check grounded
                        if (occ.Grounded)
                        {
                            grounded++;
                            sb.AppendLine(
                                "⚓ " + occ.Name +
                                " — Grounded");
                            continue;
                        }

                        // Get constraint count
                        int constraints =
                            occ.Constraints.Count;

                        // Get DOF
                        int dof = GetComponentDOF(occ);

                        string status;
                        string icon;

                        if (dof == 0)
                        {
                            status = "Fully Constrained";
                            icon = "✅";
                            fullyConstrained++;
                        }
                        else if (dof < 0)
                        {
                            status = "Over Constrained";
                            icon = "❌";
                            overConstrained++;
                        }
                        else
                        {
                            status = "Under Constrained " +
                                     "(" + dof + " DOF)";
                            icon = "⚠️";
                            underConstrained++;
                        }

                        sb.AppendLine(
                            icon + " " + occ.Name +
                            " — " + status +
                            " (" + constraints +
                            " constraints)");
                    }
                    catch { }
                }

                sb.AppendLine(new string('═', 45));
                sb.AppendLine("Summary:");
                sb.AppendLine(
                    "  Total components: " + total);
                sb.AppendLine(
                    "  ⚓ Grounded:           " + grounded);
                sb.AppendLine(
                    "  ✅ Fully constrained:  " +
                    fullyConstrained);
                sb.AppendLine(
                    "  ⚠️ Under constrained:  " +
                    underConstrained);
                sb.AppendLine(
                    "  ❌ Over constrained:   " +
                    overConstrained);
                sb.AppendLine(
                    "  ○ Suppressed:         " +
                    suppressed);

                sb.AppendLine();

                if (overConstrained > 0)
                    sb.AppendLine(
                        "❌ Fix over-constrained " +
                        "components first — they may " +
                        "cause model errors.");
                else if (underConstrained > 0)
                    sb.AppendLine(
                        "⚠️ " + underConstrained +
                        " component(s) need more " +
                        "constraints.");
                else
                    sb.AppendLine(
                        "✅ Assembly constraints " +
                        "look good.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to analyze constraints: " +
                       ex.Message;
            }
        }

        // ─── Find constraint issues ───────────────────────────────────

        private string FindConstraintIssues(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Requires an Assembly document.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;

                string n = prompt.ToLowerInvariant();
                bool findOver =
                    n.Contains("over");
                bool findUnder =
                    n.Contains("under") ||
                    n.Contains("free") ||
                    n.Contains("unconstrained");
                bool findFully =
                    n.Contains("fully");

                // Default to under if nothing specified
                if (!findOver && !findFully)
                    findUnder = true;

                var sb = new StringBuilder();

                if (findUnder)
                    sb.AppendLine(
                        "── Under Constrained " +
                        "Components ──");
                else if (findOver)
                    sb.AppendLine(
                        "── Over Constrained " +
                        "Components ──");
                else
                    sb.AppendLine(
                        "── Fully Constrained " +
                        "Components ──");

                int count = 0;

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Suppressed ||
                            occ.Grounded)
                            continue;

                        int dof = GetComponentDOF(occ);

                        bool match = false;

                        if (findUnder && dof > 0)
                            match = true;
                        else if (findOver && dof < 0)
                            match = true;
                        else if (findFully && dof == 0)
                            match = true;

                        if (match)
                        {
                            string icon =
                                dof > 0 ? "⚠️"
                                : dof < 0 ? "❌"
                                : "✅";
                            sb.AppendLine(
                                icon + " " + occ.Name +
                                " (" + dof + " DOF, " +
                                occ.Constraints.Count +
                                " constraints)");
                            count++;
                        }
                    }
                    catch { }
                }

                if (count == 0)
                    sb.AppendLine(
                        "✅ No matching components found.");
                else
                    sb.AppendLine(
                        System.Environment.NewLine +
                        "Total: " + count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to find issues: " + ex.Message;
            }
        }

        // ─── List constraints ─────────────────────────────────────────

        private string ListConstraints(string prompt)
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Requires an Assembly document.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine("── Assembly Constraints ──");
                sb.AppendLine(
                    "Assembly: " + doc.DisplayName);
                sb.AppendLine(new string('-', 45));

                int totalConstraints = 0;

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Suppressed) continue;

                        int cCount = occ.Constraints.Count;

                        if (cCount == 0) continue;

                        sb.AppendLine(
                            occ.Name + " (" +
                            cCount + " constraints):");

                        foreach (
                            AssemblyConstraint con in
                            occ.Constraints)
                        {
                            try
                            {
                                sb.AppendLine(
                                    "  - " + con.Name +
                                    " (" + con.Type + ")");
                                totalConstraints++;
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                sb.AppendLine(new string('-', 45));
                sb.AppendLine(
                    "Total constraints: " +
                    totalConstraints);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to list constraints: " +
                       ex.Message;
            }
        }

        // ─── Find grounded components ─────────────────────────────────

        private string FindGroundedComponents()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "Requires an Assembly document.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Grounded Components ──");

                int count = 0;

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Grounded)
                        {
                            sb.AppendLine(
                                "⚓ " + occ.Name);
                            count++;
                        }
                    }
                    catch { }
                }

                if (count == 0)
                    sb.AppendLine(
                        "No grounded components found." +
                        System.Environment.NewLine +
                        "Tip: The first component in " +
                        "an assembly is usually grounded.");
                else
                    sb.AppendLine(
                        System.Environment.NewLine +
                        "Total grounded: " + count);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to find grounded: " +
                       ex.Message;
            }
        }

        // ─── Check degrees of freedom ─────────────────────────────────

        private string CheckDegreesOfFreedom()
        {
            try
            {
                Document doc =
                    _inventorApplication.ActiveDocument;

                if (doc == null)
                    return "No active document is open.";

                if (doc.DocumentType !=
                    DocumentTypeEnum.kAssemblyDocumentObject)
                    return "DOF check requires " +
                           "an Assembly document.";

                AssemblyDocument asm =
                    (AssemblyDocument)doc;

                var sb = new StringBuilder();
                sb.AppendLine(
                    "── Degrees of Freedom ──");
                sb.AppendLine(
                    "Assembly: " + doc.DisplayName);
                sb.AppendLine(new string('-', 45));
                sb.AppendLine(
                    "Component".PadRight(28) +
                    "DOF".PadRight(6) +
                    "Status");
                sb.AppendLine(new string('-', 45));

                foreach (ComponentOccurrence occ in
                    asm.ComponentDefinition.Occurrences)
                {
                    try
                    {
                        if (occ.Suppressed) continue;

                        if (occ.Grounded)
                        {
                            sb.AppendLine(
                                occ.Name.PadRight(28) +
                                "0".PadRight(6) +
                                "⚓ Grounded");
                            continue;
                        }

                        int dof = GetComponentDOF(occ);

                        string status =
                            dof == 0
                                ? "✅ Fully constrained"
                                : dof < 0
                                    ? "❌ Over constrained"
                                    : "⚠️ " + dof +
                                      " DOF free";

                        sb.AppendLine(
                            occ.Name.PadRight(28) +
                            dof.ToString().PadRight(6) +
                            status);
                    }
                    catch { }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to check DOF: " + ex.Message;
            }
        }

        // ─── DOF helper ───────────────────────────────────────────────

        private int GetComponentDOF(
            ComponentOccurrence occ)
        {
            try
            {
                // A rigid body in 3D has 6 DOF
                // Each mate/flush removes 1 DOF
                // Each angle removes 1 DOF
                // Each insert removes 2 DOF
                // Each tangent removes 1 DOF

                int dof = 6;
                int constraintCount =
                    occ.Constraints.Count;

                foreach (AssemblyConstraint con in
                    occ.Constraints)
                {
                    try
                    {
                        string type =
                            con.Type.ToString().ToLower();

                        if (type.Contains("mate") ||
                            type.Contains("flush"))
                            dof -= 1;
                        else if (type.Contains("angle"))
                            dof -= 1;
                        else if (type.Contains("insert"))
                            dof -= 2;
                        else if (type.Contains("tangent"))
                            dof -= 1;
                        else if (type.Contains("symmetry"))
                            dof -= 1;
                        else
                            dof -= 1;
                    }
                    catch { }
                }

                return dof;
            }
            catch
            {
                return 6;
            }
        }
    }
}