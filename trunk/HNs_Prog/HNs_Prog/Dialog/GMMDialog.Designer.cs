namespace HNs_Prog.Dialog
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
            this.RadioButtonGMMIntensity = new System.Windows.Forms.RadioButton();
            this.RadioButtonGMMIVesselness = new System.Windows.Forms.RadioButton();
            this.RadioButtonGMMPerPixelIntensity = new System.Windows.Forms.RadioButton();
            this.RadioButtonGMMIFK = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
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
            // RadioButtonGMMIntensity
            // 
            this.RadioButtonGMMIntensity.AutoSize = true;
            this.RadioButtonGMMIntensity.Checked = true;
            this.RadioButtonGMMIntensity.Location = new System.Drawing.Point(13, 13);
            this.RadioButtonGMMIntensity.Name = "RadioButtonGMMIntensity";
            this.RadioButtonGMMIntensity.Size = new System.Drawing.Size(174, 16);
            this.RadioButtonGMMIntensity.TabIndex = 1;
            this.RadioButtonGMMIntensity.TabStop = true;
            this.RadioButtonGMMIntensity.Text = "Intensity-only GMM Model";
            this.RadioButtonGMMIntensity.UseVisualStyleBackColor = true;
            this.RadioButtonGMMIntensity.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonGMMIVesselness
            // 
            this.RadioButtonGMMIVesselness.AutoSize = true;
            this.RadioButtonGMMIVesselness.Location = new System.Drawing.Point(13, 36);
            this.RadioButtonGMMIVesselness.Name = "RadioButtonGMMIVesselness";
            this.RadioButtonGMMIVesselness.Size = new System.Drawing.Size(210, 16);
            this.RadioButtonGMMIVesselness.TabIndex = 2;
            this.RadioButtonGMMIVesselness.TabStop = true;
            this.RadioButtonGMMIVesselness.Text = "Intensity-Vesslness GMM Model";
            this.RadioButtonGMMIVesselness.UseVisualStyleBackColor = true;
            this.RadioButtonGMMIVesselness.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonGMMIFK
            // 
            this.RadioButtonGMMIFK.AutoSize = true;
            this.RadioButtonGMMIFK.Location = new System.Drawing.Point(13, 59);
            this.RadioButtonGMMIFK.Name = "RadioButtonGMMIFK";
            this.RadioButtonGMMIFK.Size = new System.Drawing.Size(237, 16);
            this.RadioButtonGMMIFK.TabIndex = 3;
            this.RadioButtonGMMIFK.TabStop = true;
            this.RadioButtonGMMIFK.Text = "Intensity-Frangi-Krissian GMM Model";
            this.RadioButtonGMMIFK.UseVisualStyleBackColor = true;
            this.RadioButtonGMMIFK.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonGMMSCOriginal
            // 
            this.RadioButtonGMMSCOriginal.AutoSize = true;
            this.RadioButtonGMMSCOriginal.Location = new System.Drawing.Point(13, 81);
            this.RadioButtonGMMSCOriginal.Name = "RadioButtonGMMSCOriginal";
            this.RadioButtonGMMSCOriginal.Size = new System.Drawing.Size(308, 16);
            this.RadioButtonGMMSCOriginal.TabIndex = 4;
            this.RadioButtonGMMSCOriginal.Text = "Original Spatial-Color GMM Model (Ting Yu et al.)";
            this.RadioButtonGMMSCOriginal.UseVisualStyleBackColor = true;
            this.RadioButtonGMMSCOriginal.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonGMMSIFrangi
            // 
            this.RadioButtonGMMSIFrangi.AutoSize = true;
            this.RadioButtonGMMSIFrangi.Location = new System.Drawing.Point(13, 103);
            this.RadioButtonGMMSIFrangi.Name = "RadioButtonGMMSIFrangi";
            this.RadioButtonGMMSIFrangi.Size = new System.Drawing.Size(253, 16);
            this.RadioButtonGMMSIFrangi.TabIndex = 5;
            this.RadioButtonGMMSIFrangi.Text = "Our Spatial-Intensity-Frangi GMM Model";
            this.RadioButtonGMMSIFrangi.UseVisualStyleBackColor = true;
            this.RadioButtonGMMSIFrangi.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonGMMPerPixelIntensity
            // 
            this.RadioButtonGMMPerPixelIntensity.AutoSize = true;
            this.RadioButtonGMMPerPixelIntensity.Location = new System.Drawing.Point(13, 157);
            this.RadioButtonGMMPerPixelIntensity.Name = "RadioButtonGMMPerPixelIntensity";
            this.RadioButtonGMMPerPixelIntensity.Size = new System.Drawing.Size(277, 16);
            this.RadioButtonGMMPerPixelIntensity.TabIndex = 6;
            this.RadioButtonGMMPerPixelIntensity.TabStop = true;
            this.RadioButtonGMMPerPixelIntensity.Text = "Per-Pixel Intensity GMM Model (Chris et al.)";
            this.RadioButtonGMMPerPixelIntensity.UseVisualStyleBackColor = true;
            this.RadioButtonGMMPerPixelIntensity.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // GMMDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(394, 262);
            this.Controls.Add(this.RadioButtonGMMIFK);
            this.Controls.Add(this.RadioButtonGMMPerPixelIntensity);
            this.Controls.Add(this.RadioButtonGMMIVesselness);
            this.Controls.Add(this.RadioButtonGMMIntensity);
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
        private System.Windows.Forms.RadioButton RadioButtonGMMIntensity;
        private System.Windows.Forms.RadioButton RadioButtonGMMIVesselness;
        private System.Windows.Forms.RadioButton RadioButtonGMMPerPixelIntensity;
        private System.Windows.Forms.RadioButton RadioButtonGMMIFK;
    }
}