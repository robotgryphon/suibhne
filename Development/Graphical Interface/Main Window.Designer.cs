namespace Graphical_Interface {
    partial class MainWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.button1 = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panelLabel = new System.Windows.Forms.Label();
            this.closeBtn = new System.Windows.Forms.Button();
            this.titleBar = new System.Windows.Forms.Panel();
            this.mainArea = new System.Windows.Forms.Panel();
            this.networksPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.panel1.SuspendLayout();
            this.titleBar.SuspendLayout();
            this.mainArea.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Highlight;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Location = new System.Drawing.Point(25, 0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(50, 5);
            this.button1.TabIndex = 0;
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button1);
            this.panel1.Location = new System.Drawing.Point(0, 35);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1000, 5);
            this.panel1.TabIndex = 1;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Sakkal Majalla", 26F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 44);
            this.label1.TabIndex = 2;
            this.label1.Text = "SUIBHNE";
            // 
            // panelLabel
            // 
            this.panelLabel.AutoSize = true;
            this.panelLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.panelLabel.ForeColor = System.Drawing.Color.DarkGray;
            this.panelLabel.Location = new System.Drawing.Point(131, 14);
            this.panelLabel.Name = "panelLabel";
            this.panelLabel.Size = new System.Drawing.Size(162, 19);
            this.panelLabel.TabIndex = 3;
            this.panelLabel.Text = "SERVER MANAGEMENT";
            this.panelLabel.Click += new System.EventHandler(this.panelLabel_Click);
            // 
            // closeBtn
            // 
            this.closeBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.closeBtn.BackColor = System.Drawing.Color.DarkRed;
            this.closeBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeBtn.Location = new System.Drawing.Point(970, 5);
            this.closeBtn.Margin = new System.Windows.Forms.Padding(0);
            this.closeBtn.Name = "closeBtn";
            this.closeBtn.Size = new System.Drawing.Size(25, 25);
            this.closeBtn.TabIndex = 4;
            this.closeBtn.UseVisualStyleBackColor = false;
            this.closeBtn.Click += new System.EventHandler(this.closeBtn_Click);
            // 
            // titleBar
            // 
            this.titleBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.titleBar.BackColor = System.Drawing.Color.Transparent;
            this.titleBar.Controls.Add(this.label1);
            this.titleBar.Controls.Add(this.closeBtn);
            this.titleBar.Controls.Add(this.panelLabel);
            this.titleBar.Location = new System.Drawing.Point(0, 0);
            this.titleBar.Margin = new System.Windows.Forms.Padding(0);
            this.titleBar.Name = "titleBar";
            this.titleBar.Size = new System.Drawing.Size(1000, 35);
            this.titleBar.TabIndex = 5;
            this.titleBar.Paint += new System.Windows.Forms.PaintEventHandler(this.titleBar_Paint);
            // 
            // mainArea
            // 
            this.mainArea.BackColor = System.Drawing.Color.Transparent;
            this.mainArea.Controls.Add(this.networksPanel);
            this.mainArea.Location = new System.Drawing.Point(0, 43);
            this.mainArea.Name = "mainArea";
            this.mainArea.Size = new System.Drawing.Size(1000, 555);
            this.mainArea.TabIndex = 6;
            // 
            // networksPanel
            // 
            this.networksPanel.Location = new System.Drawing.Point(13, 5);
            this.networksPanel.Name = "networksPanel";
            this.networksPanel.Size = new System.Drawing.Size(280, 530);
            this.networksPanel.TabIndex = 0;
            this.networksPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.flowLayoutPanel1_Paint);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.BackgroundImage = global::Graphical_Interface.Properties.Resources.Background;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.mainArea);
            this.Controls.Add(this.titleBar);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.Name = "MainWindow";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Suibhne IRC System";
            this.panel1.ResumeLayout(false);
            this.titleBar.ResumeLayout(false);
            this.titleBar.PerformLayout();
            this.mainArea.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label panelLabel;
        private System.Windows.Forms.Button closeBtn;
        private System.Windows.Forms.Panel titleBar;
        private System.Windows.Forms.Panel mainArea;
        private System.Windows.Forms.FlowLayoutPanel networksPanel;
    }
}

