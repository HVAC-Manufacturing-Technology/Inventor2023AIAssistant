using System;
using System.Windows.Forms;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;

namespace Inventor2023AIAssistant
{
    public class VaultHandler
    {
        private WebServiceManager _wsm;

        private readonly string _server = "192.168.1.35";
        private readonly string _vault = "Vault";

        private readonly string _username;
        private readonly string _password;

        public VaultHandler(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public bool Connect()
        {
            try
            {
                var serverId = new ServerIdentities
                {
                    DataServer = _server,
                    FileServer = _server
                };

                var creds = new UserPasswordCredentials(
                    serverId,
                    _vault,
                    _username,
                    _password,
                    false);

                _wsm = new WebServiceManager(creds);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Vault connection failed:\n{ex.Message}");
                return false;
            }
        }

        private bool EnsureConnected()
        {
            return _wsm != null || Connect();
        }

        public bool CheckOut(string filePath)
        {
            if (!EnsureConnected()) return false;

            try
            {
                var file = GetFileByPath(filePath);
                if (file == null)
                {
                    MessageBox.Show("File not found in Vault.");
                    return false;
                }

                ByteArray ticket;

                _wsm.DocumentService.CheckoutFile(
                    file.Id,
                    CheckoutFileOptions.Master,
                    Environment.MachineName,
                    filePath,
                    "Checked out via AI Assistant",
                    out ticket);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Checkout failed:\n{ex.Message}");
                return false;
            }
        }

        public bool CheckIn(string filePath, string comment)
        {
            if (!EnsureConnected()) return false;

            try
            {
                var file = GetFileByPath(filePath);
                if (file == null)
                {
                    MessageBox.Show("File not found in Vault.");
                    return false;
                }

                // In a simple scenario we don't upload new content,
                // so we pass null for uploadTicket.
                _wsm.DocumentService.CheckinUploadedFile(
                    file.MasterId,
                    comment,
                    false,                 // keepCheckedOut
                    DateTime.Now,
                    null,                  // associations
                    null,                  // BOM
                    false,                 // copyBom
                    null,                  // newFileName
                    FileClassification.None,
                    false,                 // hidden
                    null                   // uploadTicket
                );

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Check-in failed:\n{ex.Message}");
                return false;
            }
        }

        public bool UndoCheckOut(string filePath)
        {
            if (!EnsureConnected()) return false;

            try
            {
                var file = GetFileByPath(filePath);
                if (file == null)
                {
                    MessageBox.Show("File not found in Vault.");
                    return false;
                }

                ByteArray ticket;
                _wsm.DocumentService.UndoCheckoutFile(
                    file.MasterId,
                    out ticket);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Undo checkout failed:\n{ex.Message}");
                return false;
            }
        }

        public string GetStatus(string filePath)
        {
            if (!EnsureConnected())
                return "Not connected to Vault.";

            var file = GetFileByPath(filePath);
            if (file == null)
                return "File not found in Vault.";

            return file.CheckedOut
                ? "🔓 Checked Out"
                : "🟢 Available";
        }

        private File GetFileByPath(string filePath)
        {
            try
            {
                var files = _wsm.DocumentService
                    .FindLatestFilesByPaths(new[] { filePath });

                return files != null && files.Length > 0
                    ? files[0]
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
