namespace FFXIVKoreanPatch
{
    partial class FfxivKoreanPatch
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FfxivKoreanPatch));
            this.initialChecker = new System.ComponentModel.BackgroundWorker();
            this.statusLabel = new System.Windows.Forms.Label();
            this.fullButton = new System.Windows.Forms.Button();
            this.fontButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // initialChecker
            // 
            this.initialChecker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.initialChecker_DoWork);
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusLabel.Location = new System.Drawing.Point(0, 316);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(10, 20, 10, 10);
            this.statusLabel.Size = new System.Drawing.Size(86, 45);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "statusLabel";
            // 
            // fullButton
            // 
            this.fullButton.AutoSize = true;
            this.fullButton.BackColor = System.Drawing.Color.Transparent;
            this.fullButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.fullButton.Enabled = false;
            this.fullButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.fullButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.fullButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.fullButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.fullButton.Location = new System.Drawing.Point(0, 175);
            this.fullButton.Margin = new System.Windows.Forms.Padding(10);
            this.fullButton.Name = "fullButton";
            this.fullButton.Padding = new System.Windows.Forms.Padding(10);
            this.fullButton.Size = new System.Drawing.Size(384, 47);
            this.fullButton.TabIndex = 0;
            this.fullButton.TabStop = false;
            this.fullButton.Text = "전체 한글 패치";
            this.fullButton.UseVisualStyleBackColor = false;
            this.fullButton.Click += new System.EventHandler(this.fullButton_Click);
            // 
            // fontButton
            // 
            this.fontButton.AutoSize = true;
            this.fontButton.BackColor = System.Drawing.Color.Transparent;
            this.fontButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.fontButton.Enabled = false;
            this.fontButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.fontButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.fontButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.fontButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.fontButton.Location = new System.Drawing.Point(0, 222);
            this.fontButton.Margin = new System.Windows.Forms.Padding(10);
            this.fontButton.Name = "fontButton";
            this.fontButton.Padding = new System.Windows.Forms.Padding(10);
            this.fontButton.Size = new System.Drawing.Size(384, 47);
            this.fontButton.TabIndex = 0;
            this.fontButton.TabStop = false;
            this.fontButton.Text = "채팅만 패치";
            this.fontButton.UseVisualStyleBackColor = false;
            this.fontButton.Click += new System.EventHandler(this.fontButton_Click);
            // 
            // removeButton
            // 
            this.removeButton.AutoSize = true;
            this.removeButton.BackColor = System.Drawing.Color.Transparent;
            this.removeButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.removeButton.Enabled = false;
            this.removeButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.removeButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.removeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.removeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.removeButton.Location = new System.Drawing.Point(0, 269);
            this.removeButton.Margin = new System.Windows.Forms.Padding(10);
            this.removeButton.Name = "removeButton";
            this.removeButton.Padding = new System.Windows.Forms.Padding(10);
            this.removeButton.Size = new System.Drawing.Size(384, 47);
            this.removeButton.TabIndex = 0;
            this.removeButton.TabStop = false;
            this.removeButton.Text = "한글 패치 삭제";
            this.removeButton.UseVisualStyleBackColor = false;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // FfxivKoreanPatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(384, 361);
            this.Controls.Add(this.fullButton);
            this.Controls.Add(this.fontButton);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.statusLabel);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FfxivKoreanPatch";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FFXIV 한글 패치";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.ComponentModel.BackgroundWorker initialChecker;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button fullButton;
        private System.Windows.Forms.Button fontButton;
        private System.Windows.Forms.Button removeButton;
    }
}