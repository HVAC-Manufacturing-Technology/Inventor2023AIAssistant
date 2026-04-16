using System.Windows.Forms;

namespace Inventor2023AIAssistant
{
    public class PromptTextBox : TextBox
    {
        protected override bool IsInputKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Space)
            {
                return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                int start = this.SelectionStart;
                int length = this.SelectionLength;
                string current = this.Text ?? string.Empty;

                if (length > 0)
                {
                    current = current.Remove(start, length);
                }

                current = current.Insert(start, " ");
                this.Text = current;
                this.SelectionStart = start + 1;
                this.SelectionLength = 0;

                e.SuppressKeyPress = true;
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }
    }
}