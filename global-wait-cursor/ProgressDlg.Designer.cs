namespace global_wait_cursor
{
    partial class ProgressDlg
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            progressBar = new ProgressBar();
            bnCancel = new Button();
            bnStart = new Button();
            SuspendLayout();
            // 
            // progressBar
            // 
            progressBar.Dock = DockStyle.Bottom;
            progressBar.Location = new Point(0, 210);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(478, 34);
            progressBar.TabIndex = 0;
            // 
            // bnCancel
            // 
            bnCancel.Location = new Point(310, 157);
            bnCancel.Name = "bnCancel";
            bnCancel.Size = new Size(142, 40);
            bnCancel.TabIndex = 1;
            bnCancel.Text = "Cancel";
            bnCancel.UseVisualStyleBackColor = true;
            // 
            // bnStart
            // 
            bnStart.Location = new Point(310, 111);
            bnStart.Name = "bnStart";
            bnStart.Size = new Size(142, 40);
            bnStart.TabIndex = 1;
            bnStart.Text = "Start";
            bnStart.UseVisualStyleBackColor = true;
            // 
            // ProgressDlg
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(478, 244);
            Controls.Add(bnStart);
            Controls.Add(bnCancel);
            Controls.Add(progressBar);
            Name = "ProgressDlg";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Progress Dialog";
            ResumeLayout(false);
        }

        #endregion

        private ProgressBar progressBar;
        private Button bnCancel;
        private Button bnStart;
    }
}