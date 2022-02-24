namespace ImageProcessor
{
    partial class SettingWindow
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
            this.label1 = new System.Windows.Forms.Label();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.AoI_label = new System.Windows.Forms.Label();
            this.Format_label = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(74, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(311, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Image processor setting window example !!";
            // 
            // trackBar1
            // 
            this.trackBar1.Location = new System.Drawing.Point(25, 70);
            this.trackBar1.Maximum = 255;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(436, 69);
            this.trackBar1.TabIndex = 1;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // AoI_label
            // 
            this.AoI_label.AutoSize = true;
            this.AoI_label.Location = new System.Drawing.Point(50, 132);
            this.AoI_label.Name = "AoI_label";
            this.AoI_label.Size = new System.Drawing.Size(34, 20);
            this.AoI_label.TabIndex = 2;
            this.AoI_label.Text = "AoI";
            // 
            // Format_label
            // 
            this.Format_label.AutoSize = true;
            this.Format_label.Location = new System.Drawing.Point(50, 162);
            this.Format_label.Name = "Format_label";
            this.Format_label.Size = new System.Drawing.Size(60, 20);
            this.Format_label.TabIndex = 3;
            this.Format_label.Text = "Format";
            // 
            // SettingWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(488, 201);
            this.ControlBox = false;
            this.Controls.Add(this.Format_label);
            this.Controls.Add(this.AoI_label);
            this.Controls.Add(this.trackBar1);
            this.Controls.Add(this.label1);
            this.Name = "SettingWindow";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TrackBar trackBar1;
        public System.Windows.Forms.Label AoI_label;
        public System.Windows.Forms.Label Format_label;
    }
}