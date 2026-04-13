namespace Inventor2023AIAssistant
{
    partial class AssistantForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TextBox txtPrompt;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnNewChat;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.txtPrompt = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnNewChat = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblStatus
            //
            this.lblStatus.Location = new System.Drawing.Point(20, 5);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(500, 18);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.Text = "Checking AI connection...";
            this.lblStatus.Font = new System.Drawing.Font(
                "Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            //
            // btnNewChat
            //
            this.btnNewChat.Location = new System.Drawing.Point(560, 2);
            this.btnNewChat.Name = "btnNewChat";
            this.btnNewChat.Size = new System.Drawing.Size(100, 22);
            this.btnNewChat.TabIndex = 4;
            this.btnNewChat.TabStop = false;
            this.btnNewChat.Text = "New Chat";
            this.btnNewChat.UseVisualStyleBackColor = true;
            this.btnNewChat.Click += new System.EventHandler(
                this.btnNewChat_Click);
            //
            // txtPrompt
            //
            this.txtPrompt.Location = new System.Drawing.Point(20, 30);
            this.txtPrompt.Multiline = true;
            this.txtPrompt.Name = "txtPrompt";
            this.txtPrompt.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPrompt.Size = new System.Drawing.Size(640, 80);
            this.txtPrompt.TabIndex = 0;
            //
            // btnSend
            //
            this.btnSend.Location = new System.Drawing.Point(20, 120);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(100, 30);
            this.btnSend.TabIndex = 1;
            this.btnSend.TabStop = false;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(
                this.btnSend_Click);
            //
            // txtOutput
            //
            this.txtOutput.Location = new System.Drawing.Point(20, 160);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtOutput.Size = new System.Drawing.Size(640, 270);
            this.txtOutput.TabIndex = 2;
            //
            // AssistantForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 461);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnNewChat);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtPrompt);
            this.Name = "AssistantForm";
            this.Text = "Inventor 2023 AI Assistant";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}