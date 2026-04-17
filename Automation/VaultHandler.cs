using System;
using System.Windows.Forms;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using Autodesk.DataManagement.Client.Framework.Vault;
using Autodesk.DataManagement.Client.Framework
    .Vault.Currency.Connections;
using InventorApp = Inventor.Application;
using VaultFile = Autodesk.Connectivity
    .WebServices.File;
using SysEnv = System.Environment;

namespace Inventor2023AIAssistant
{
    public class VaultHandler
    {
        private WebServiceManager _wsm;
        private Connection _connection;

        public VaultHandler()
        {
            // Subscribe to future connection events
            Library.ConnectionManager
                .ConnectionEstablished +=
                OnConnectionEstablished;
            Library.ConnectionManager
                .ConnectionReused +=
                OnConnectionReused;

            // Try to get existing connection
            // by triggering a reuse
            try
            {
                Library.ConnectionManager
                    .GetExistingConnection(
                        "192.168.1.35",
                        "Vault",
                        "bconner",
                        "",
                        AuthenticationFlags
                        .AutodeskAuthentication);
            }
            catch { }
        }

        private void OnConnectionEstablished(
            object sender,
            ConnectionEventArgs e)
        {
            _connection = e.Connection;
            _wsm = _connection
                .WebServiceManager;
        }

        private void OnConnectionReused(
            object sender,
            ConnectionEventArgs e)
        {
            _connection = e.Connection;
            _wsm = _connection
                .WebServiceManager;
        }

        private bool EnsureConnected()
        {
            if (_wsm != null) return true;
            try
            {
                var loginResult = Library.ConnectionManager
                    .LogInWithUserLicense(
                        "192.168.1.35",
                        "Vault",
                        AutodeskAccount.Login(IntPtr.Zero),
                        AuthenticationFlags
                            .AutodeskAuthentication,
                        null);

                if (loginResult == null ||
                    !loginResult.Success)
                {
                    MessageBox.Show(
                        "Vault login failed.",
                "Vault Error");
            return false;
        }
        

                _wsm = loginResult.Connection
                    .WebServiceManager;
                    return true;
                }
    catch (Exception ex)
            {
                MessageBox.Show(
                    $"Vault error:\n{ex.Message}",
                    "Vault Error");
                return false;
            }
        }

        public bool CheckOut(string filePath)
        {
            if (!EnsureConnected()) return false;
            try
            {
                VaultFile file = GetFile(filePath);
                if (file == null)
                {
                    MessageBox.Show(
                        "File not found in Vault.");
                    return false;
                }
                ByteArray ticket;
                _wsm.DocumentService.CheckoutFile(
                    file.Id,
                    CheckoutFileOptions.Master,
                    SysEnv.MachineName,
                    filePath,
                    "Checked out via AI Assistant",
                    out ticket);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Checkout failed:\n" +
                    $"{ex.Message}");
                return false;
            }
        }

        public bool CheckIn(string filePath,
                            string comment = "")
        {
            if (!EnsureConnected()) return false;
            try
            {
                VaultFile file = GetFile(filePath);
                if (file == null)
                {
                    MessageBox.Show(
                        "File not found in Vault.");
                    return false;
                }
                _wsm.DocumentService
                    .CheckinUploadedFile(
                        file.MasterId,
                        comment,
                        false,
                        DateTime.Now,
                        null, null, false,
                        null,
                        FileClassification.None,
                        false, null);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Check in failed:\n" +
                    $"{ex.Message}");
                return false;
            }
        }

        public bool UndoCheckOut(string filePath)
        {
            if (!EnsureConnected()) return false;
            try
            {
                VaultFile file = GetFile(filePath);
                if (file == null)
                {
                    MessageBox.Show(
                        "File not found in Vault.");
                    return false;
                }
                ByteArray ticket;
                _wsm.DocumentService
                    .UndoCheckoutFile(
                        file.MasterId,
                        out ticket);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Undo failed:\n{ex.Message}");
                return false;
            }
        }

        public string GetStatus(string filePath)
        {
            if (!EnsureConnected())
                return "Vault not connected.";
            try
            {
                VaultFile file = GetFile(filePath);
                if (file == null)
                    return "File not found in Vault.";
                return file.CheckedOut
                    ? "🔓 Checked Out"
                    : "🟢 Available";
            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }

        private VaultFile GetFile(string path)
        {
            try
            {
                // Convert local path to Vault path
                string vaultPath = path.Replace(
                    @"C:\Users\bconner\OneDrive - " +
                    @"HVAC Manufacturing\Documents\Vault\",
                    "$/");
                vaultPath = vaultPath.Replace(
                    @"\", "/");

                VaultFile[] files =
                    _wsm.DocumentService
                    .FindLatestFilesByPaths(
                        new[] { vaultPath });
                return files != null &&
                       files.Length > 0
                    ? files[0] : null;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            Library.ConnectionManager
                .ConnectionEstablished -=
                OnConnectionEstablished;
            Library.ConnectionManager
                .ConnectionReused -=
                OnConnectionReused;
        }
    }
}