using System.Drawing;
using System.Windows.Forms;
using TimeboxBar.Core;

namespace TimeboxBar.UI
{
    public class QuickStartPopup : Form
    {
        public int SelectedMinutes { get; private set; }

        public QuickStartPopup(int quick1, int quick2, bool closeOnDeactivate = false)
        {
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            TopMost         = true;
            Text            = "TimeboxBar";
            StartPosition   = FormStartPosition.CenterScreen;
            ClientSize      = new Size(200, 110);

            var font = new Font("Segoe UI", 10f);

            var btn1 = new Button
            {
                Text     = Strings.MenuQuickStart(quick1),
                Location = new Point(16, 12),
                Size     = new Size(168, 28),
                Font     = font
            };
            btn1.Click += (s, e) => { SelectedMinutes = quick1; DialogResult = DialogResult.OK; };

            var btn2 = new Button
            {
                Text     = Strings.MenuQuickStart(quick2),
                Location = new Point(16, 46),
                Size     = new Size(168, 28),
                Font     = font
            };
            btn2.Click += (s, e) => { SelectedMinutes = quick2; DialogResult = DialogResult.OK; };

            var btnCustom = new Button
            {
                Text     = Strings.MenuCustomTime,
                Location = new Point(16, 80),
                Size     = new Size(168, 22),
                Font     = new Font("Segoe UI", 8.5f)
            };
            btnCustom.Click += (s, e) => { SelectedMinutes = -1; DialogResult = DialogResult.OK; };

            AcceptButton = btn1;
            CancelButton = new Button { DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[] { btn1, btn2, btnCustom });

            if (closeOnDeactivate)
                Deactivate += (s, e) => { if (DialogResult == DialogResult.None) Close(); };
        }
    }
}
