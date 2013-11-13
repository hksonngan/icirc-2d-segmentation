﻿namespace HNs_Prog.Dialog
{
    partial class GMMDialog
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
            this.RadioButtonGMMSCOriginal = new System.Windows.Forms.RadioButton();
            this.RadioButtonGMMSIFrangi = new System.Windows.Forms.RadioButton();
            this.ButtonRun = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // RadioButtonGMMSCOriginal
            // 
            this.RadioButtonGMMSCOriginal.AutoSize = true;
            this.RadioButtonGMMSCOriginal.Checked = true;
            this.RadioButtonGMMSCOriginal.Location = new System.Drawing.Point(13, 13);
            this.RadioButtonGMMSCOriginal.Name = "RadioButtonGMMSCOriginal";
            this.RadioButtonGMMSCOriginal.Size = new System.Drawing.Size(308, 16);
            this.RadioButtonGMMSCOriginal.TabIndex = 1;
            this.RadioButtonGMMSCOriginal.TabStop = true;
            this.RadioButtonGMMSCOriginal.Text = "Original Spatial-Color GMM Model (Ting Yu et al.)";
            this.RadioButtonGMMSCOriginal.UseVisualStyleBackColor = true;
            this.RadioButtonGMMSCOriginal.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonGMMSIFrangi
            // 
            this.RadioButtonGMMSIFrangi.AutoSize = true;
            this.RadioButtonGMMSIFrangi.Location = new System.Drawing.Point(13, 36);
            this.RadioButtonGMMSIFrangi.Name = "RadioButtonGMMSIFrangi";
            this.RadioButtonGMMSIFrangi.Size = new System.Drawing.Size(253, 16);
            this.RadioButtonGMMSIFrangi.TabIndex = 2;
            this.RadioButtonGMMSIFrangi.TabStop = true;
            this.RadioButtonGMMSIFrangi.Text = "Our Spatial-Intensity-Frangi GMM Model";
            this.RadioButtonGMMSIFrangi.UseVisualStyleBackColor = true;
            this.RadioButtonGMMSIFrangi.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // ButtonRun
            // 
            this.ButtonRun.Location = new System.Drawing.Point(13, 227);
            this.ButtonRun.Name = "ButtonRun";
            this.ButtonRun.Size = new System.Drawing.Size(369, 23);
            this.ButtonRun.TabIndex = 0;
            this.ButtonRun.Text = "Run";
            this.ButtonRun.UseVisualStyleBackColor = true;
            this.ButtonRun.Click += new System.EventHandler(this.ButtonRunClick);
            // 
            // GMMDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(394, 262);
            this.Controls.Add(this.ButtonRun);
            this.Controls.Add(this.RadioButtonGMMSIFrangi);
            this.Controls.Add(this.RadioButtonGMMSCOriginal);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "GMMDialog";
            this.Text = "GMM Models";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonRun;
        private System.Windows.Forms.RadioButton RadioButtonGMMSCOriginal;
        private System.Windows.Forms.RadioButton RadioButtonGMMSIFrangi;
    }
}