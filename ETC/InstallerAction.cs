using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;

namespace Inventor2023AIAssistant
{
    [RunInstaller(true)]
    public class InstallerAction : Installer
    {
        public override void Install(
            IDictionary stateSaver)
        {
            base.Install(stateSaver);
            try
            {
                string key =
                    this.Context.Parameters["GROQKEY"];
                if (!string.IsNullOrWhiteSpace(key))
                {
                    Environment.SetEnvironmentVariable(
                        "GROQ_API_KEY",
                        key,
                        EnvironmentVariableTarget.Machine);
                }
            }
            catch (Exception ex)
            {
                throw new InstallException(
                    "Failed to set GROQ_API_KEY: " +
                    ex.Message);
            }
        }

        public override void Uninstall(
            IDictionary savedState)
        {
            base.Uninstall(savedState);
            try
            {
                Environment.SetEnvironmentVariable(
                    "GROQ_API_KEY",
                    null,
                    EnvironmentVariableTarget.Machine);
            }
            catch { }
        }
    }
}