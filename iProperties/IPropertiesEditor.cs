using System;
using Inventor;

namespace Inventor2023AIAssistant
{
    public class IPropertiesEditor
    {
        private readonly Inventor.Application _app;

        private const string DesignGuid =
            "{32853F0F-3444-11D1-9E93-0060B03C1CA6}";

        private const string SummaryGuid =
            "{F29F85E0-4FF9-1068-AB91-08002B27B3D9}";

        public IPropertiesEditor(Inventor.Application app)
        {
            _app = app;
        }

        public string TryHandleIPropertiesEdit(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return null;

            string n = prompt.Trim().ToLowerInvariant();

            if (n == "show iproperties")
                return ShowIProperties();

            if (n == "clear part number")
                return SetDesignProperty("Part Number", "");

            if (n == "clear description")
                return SetDesignProperty("Description", "");

            if (!n.StartsWith("set ") || !n.Contains(" to "))
                return null;

            int toIdx = prompt
                .ToLowerInvariant()
                .IndexOf(" to ", 4);

            if (toIdx < 0)
                return null;

            string prop =
                n.Substring(4, toIdx - 4).Trim();

            string value =
                prompt.Substring(toIdx + 4).Trim();

            switch (prop)
            {
                case "part number":
                    return SetDesignProperty(
                        "Part Number", value);

                case "description":
                    return SetDesignProperty(
                        "Description", value);

                case "revision":
                case "revision number":
                    return SetDesignProperty(
                        "Revision Number", value);

                case "designer":
                    return SetDesignProperty(
                        "Designer", value);

                case "company":
                    return SetSummaryProperty(
                        "Company", value);

                case "project":
                    return SetDesignProperty(
                        "Project", value);

                case "stock number":
                    return SetDesignProperty(
                        "Stock Number", value);

                case "keywords":
                    return SetSummaryProperty(
                        "Keywords", value);

                case "material":
                    return SetMaterial(value);

                default:
                    return null;
            }
        }

        // ── Public setters (called by IPropertiesPanel) ───────────────

        public string SetPropertyByName(
            string name, string value)
        {
            string n = name.Trim().ToLowerInvariant();

            switch (n)
            {
                case "part number":
                    return SetDesignProperty(
                        "Part Number", value);

                case "description":
                    return SetDesignProperty(
                        "Description", value);

                case "revision":
                case "revision number":
                    return SetDesignProperty(
                        "Revision Number", value);

                case "designer":
                    return SetDesignProperty(
                        "Designer", value);

                case "company":
                    return SetSummaryProperty(
                        "Company", value);

                case "project":
                    return SetDesignProperty(
                        "Project", value);

                case "stock number":
                    return SetDesignProperty(
                        "Stock Number", value);

                case "keywords":
                    return SetSummaryProperty(
                        "Keywords", value);

                case "material":
                    return SetMaterial(value);

                default:
                    return null;
            }
        }

        // ── Private helpers ───────────────────────────────────────────

        private string SetDesignProperty(
            string name, string value)
        {
            try
            {
                Document doc = _app.ActiveDocument;
                Property prop =
                    doc.PropertySets[DesignGuid][name];

                string oldVal =
                    Convert.ToString(prop.Value);

                prop.Value = value;

                return "\u2705 " + name +
                       " updated:\nFrom: " + oldVal +
                       "\nTo:   " + value;
            }
            catch (Exception ex)
            {
                return "Failed to set " +
                       name + ": " + ex.Message;
            }
        }

        private string SetSummaryProperty(
            string name, string value)
        {
            try
            {
                Document doc = _app.ActiveDocument;
                Property prop =
                    doc.PropertySets[SummaryGuid][name];

                prop.Value = value;

                return "\u2705 " + name + " updated.";
            }
            catch (Exception ex)
            {
                return "Failed to set " +
                       name + ": " + ex.Message;
            }
        }

        private string SetMaterial(string materialName)
        {
            try
            {
                PartDocument part =
                    _app.ActiveDocument as PartDocument;

                if (part == null)
                    return
                        "Material can only be set " +
                        "on a Part.";

                Material found = null;

                foreach (Material m in
                    part.ComponentDefinition
                        .Material.Parent.Materials)
                {
                    if (string.Equals(
                        m.Name, materialName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        found = m;
                        break;
                    }
                }

                if (found == null)
                    return "Material not found: " +
                           materialName;

                string oldMat =
                    part.ComponentDefinition.Material.Name;

                part.ComponentDefinition.Material = found;

                return "\u2705 Material updated:\nFrom: " +
                       oldMat + "\nTo:   " + found.Name;
            }
            catch (Exception ex)
            {
                return "Failed to set material: " +
                       ex.Message;
            }
        }

        private string ShowIProperties()
        {
            try
            {
                Document doc = _app.ActiveDocument;
                var sb =
                    new System.Text.StringBuilder();

                sb.AppendLine("iProperties:");
                sb.AppendLine(new string('-', 30));

                foreach (PropertySet ps in
                    doc.PropertySets)
                {
                    foreach (Property p in ps)
                    {
                        try
                        {
                            string v =
                                Convert.ToString(
                                    p.Value);
                            if (!string
                                .IsNullOrWhiteSpace(v))
                                sb.AppendLine(
                                    p.Name + ": " + v);
                        }
                        catch { }
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Failed to read iProperties: " +
                       ex.Message;
            }
        }
    }
}