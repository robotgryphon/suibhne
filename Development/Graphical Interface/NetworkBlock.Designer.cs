namespace Graphical_Interface {
    partial class NetworkBlock {
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.NetworkNameLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // NetworkNameLabel
            // 
            this.NetworkNameLabel.AutoSize = true;
            this.NetworkNameLabel.Font = new System.Drawing.Font("Segoe UI", 16F);
            this.NetworkNameLabel.Location = new System.Drawing.Point(15, 13);
            this.NetworkNameLabel.Name = "NetworkNameLabel";
            this.NetworkNameLabel.Size = new System.Drawing.Size(160, 30);
            this.NetworkNameLabel.TabIndex = 0;
            this.NetworkNameLabel.Text = "Network Name";
            // 
            // NetworkBlock
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.NetworkNameLabel);
            this.Name = "NetworkBlock";
            this.Size = new System.Drawing.Size(400, 80);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.Label NetworkNameLabel;

    }
}
