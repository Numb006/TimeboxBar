using System.Drawing;
using System.Windows.Forms;

namespace TimeboxBar.UI
{
    public class CustomTimeDialog : Form
    {
        public int Minutes { get; private set; }

        private readonly NumericUpDown _spinner;

        public CustomTimeDialog()
        {
            Text            = "Timebox-Dauer";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            StartPosition   = FormStartPosition.CenterScreen;
            ClientSize      = new Size(240, 88);

            var lbl = new Label
            {
                Text     = "Minuten:",
                Location = new Point(16, 16),
                AutoSize = true,
                Font     = new System.Drawing.Font("Segoe UI", 10f)
            };

            _spinner = new NumericUpDown
            {
                Minimum  = 1,
                Maximum  = 240,
                Value    = 25,
                Location = new Point(90, 12),
                Width    = 70,
                Font     = new System.Drawing.Font("Segoe UI", 10f)
            };

            var btnOk = new Button
            {
                Text         = "Starten",
                DialogResult = DialogResult.OK,
                Location     = new Point(60, 50),
                Width        = 80
            };
            btnOk.Click += (s, e) => Minutes = (int)_spinner.Value;

            var btnCancel = new Button
            {
                Text         = "Abbrechen",
                DialogResult = DialogResult.Cancel,
                Location     = new Point(148, 50),
                Width        = 80
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[] { lbl, _spinner, btnOk, btnCancel });
        }
    }
}
