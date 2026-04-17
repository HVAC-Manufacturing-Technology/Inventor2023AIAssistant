using System;
using System.Drawing;
using System.Windows.Forms;

namespace Inventor2023AIAssistant
{
    public class VaultButtonStrip : Panel
    {
        private Button _btnCheckOut;
        private Button _btnCheckIn;
        private Button _btnStatus;
        private Button _btnUndo;

        // Text labels for wide mode
        private const string TextCheckOut =
            "🔓 Check Out";
        private const string TextCheckIn =
            "🔒 Check In";
        private const string TextStatus =
            "📋 Status";
        private const string TextUndo =
            "↩️ Undo";

        // Icon only for narrow mode
        private const string IconCheckOut = "🔓";
        private const string IconCheckIn = "🔒";
        private const string IconStatus = "📋";
        private const string IconUndo = "↩️";

        // Width threshold to switch to icons
        private const int IconModeWidth = 300;

        public event EventHandler CheckOutClicked;
        public event EventHandler CheckInClicked;
        public event EventHandler StatusClicked;
        public event EventHandler UndoClicked;

        public VaultButtonStrip()
        {
            Height = 35;
            Dock = DockStyle.Bottom;
            BackColor = Color.FromArgb(37, 37, 38);

            BuildButtons();
            Resize += (s, e) => UpdateLayout();
        }

        private void BuildButtons()
        {
            _btnCheckOut = CreateButton(
                TextCheckOut,
                Color.FromArgb(0, 122, 204));
            _btnCheckOut.Click += (s, e) =>
                CheckOutClicked?.Invoke(this,
                    EventArgs.Empty);

            _btnCheckIn = CreateButton(
                TextCheckIn,
                Color.FromArgb(16, 124, 16));
            _btnCheckIn.Click += (s, e) =>
                CheckInClicked?.Invoke(this,
                    EventArgs.Empty);

            _btnStatus = CreateButton(
                TextStatus,
                Color.FromArgb(63, 63, 70));
            _btnStatus.Click += (s, e) =>
                StatusClicked?.Invoke(this,
                    EventArgs.Empty);

            _btnUndo = CreateButton(
                TextUndo,
                Color.FromArgb(204, 51, 51));
            _btnUndo.Click += (s, e) =>
                UndoClicked?.Invoke(this,
                    EventArgs.Empty);

            // Add in reverse for DockStyle.Left
            Controls.Add(_btnUndo);
            Controls.Add(_btnStatus);
            Controls.Add(_btnCheckIn);
            Controls.Add(_btnCheckOut);

            UpdateLayout();
        }

        private Button CreateButton(
            string text, Color backColor)
        {
            return new Button
            {
                Text = text,
                Dock = DockStyle.Left,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                Font = new Font(
                    "Segoe UI", 9f)
            };
        }

        private void UpdateLayout()
        {
            bool iconMode = Width < IconModeWidth;
            int btnWidth = Width / 4;

            if (iconMode)
            {
                _btnCheckOut.Text = IconCheckOut;
                _btnCheckIn.Text = IconCheckIn;
                _btnStatus.Text = IconStatus;
                _btnUndo.Text = IconUndo;
                btnWidth = Width / 4;
            }
            else
            {
                _btnCheckOut.Text = TextCheckOut;
                _btnCheckIn.Text = TextCheckIn;
                _btnStatus.Text = TextStatus;
                _btnUndo.Text = TextUndo;
            }

            _btnCheckOut.Width = btnWidth;
            _btnCheckIn.Width = btnWidth;
            _btnStatus.Width = btnWidth;
            _btnUndo.Width = btnWidth;
        }
    }
}