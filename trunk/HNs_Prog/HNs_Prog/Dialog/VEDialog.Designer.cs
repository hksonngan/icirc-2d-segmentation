﻿namespace HNs_Prog.Dialog
{
    partial class VEDialog
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
            this.ButtonRun = new System.Windows.Forms.Button();
            this.RadioButtonFrangi = new System.Windows.Forms.RadioButton();
            this.RadioButtonKrissian1 = new System.Windows.Forms.RadioButton();
            this.RadioButtonManniesing = new System.Windows.Forms.RadioButton();
            this.RadioButtonKrissian2 = new System.Windows.Forms.RadioButton();
            this.RadioButtonTruc = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // ButtonRun
            // 
            this.ButtonRun.Location = new System.Drawing.Point(12, 227);
            this.ButtonRun.Name = "ButtonRun";
            this.ButtonRun.Size = new System.Drawing.Size(530, 23);
            this.ButtonRun.TabIndex = 0;
            this.ButtonRun.Text = "Run";
            this.ButtonRun.UseVisualStyleBackColor = true;
            this.ButtonRun.Click += new System.EventHandler(this.ButtonRunClick);
            // 
            // RadioButtonFrangi
            // 
            this.RadioButtonFrangi.AutoSize = true;
            this.RadioButtonFrangi.Checked = true;
            this.RadioButtonFrangi.Location = new System.Drawing.Point(12, 13);
            this.RadioButtonFrangi.Name = "RadioButtonFrangi";
            this.RadioButtonFrangi.Size = new System.Drawing.Size(411, 16);
            this.RadioButtonFrangi.TabIndex = 1;
            this.RadioButtonFrangi.TabStop = true;
            this.RadioButtonFrangi.Text = "Frangi et al., Multiscale vessel enhancement filtering (MICCAI, 1998)";
            this.RadioButtonFrangi.UseVisualStyleBackColor = true;
            this.RadioButtonFrangi.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonKrissian1
            // 
            this.RadioButtonKrissian1.AutoSize = true;
            this.RadioButtonKrissian1.Location = new System.Drawing.Point(12, 36);
            this.RadioButtonKrissian1.Name = "RadioButtonKrissian1";
            this.RadioButtonKrissian1.Size = new System.Drawing.Size(428, 16);
            this.RadioButtonKrissian1.TabIndex = 2;
            this.RadioButtonKrissian1.TabStop = true;
            this.RadioButtonKrissian1.Text = "Krissian et al., Model based detection of tubular structures (CVIU, 2000)";
            this.RadioButtonKrissian1.UseVisualStyleBackColor = true;
            this.RadioButtonKrissian1.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonKrissian2
            // 
            this.RadioButtonKrissian2.AutoSize = true;
            this.RadioButtonKrissian2.Location = new System.Drawing.Point(12, 59);
            this.RadioButtonKrissian2.Name = "RadioButtonKrissian2";
            this.RadioButtonKrissian2.Size = new System.Drawing.Size(358, 16);
            this.RadioButtonKrissian2.TabIndex = 3;
            this.RadioButtonKrissian2.TabStop = true;
            this.RadioButtonKrissian2.Text = "Krissian, Flux-based anisotropic diffusion (IEEE TMI, 2002)";
            this.RadioButtonKrissian2.UseVisualStyleBackColor = true;
            this.RadioButtonKrissian2.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonManniesing
            // 
            this.RadioButtonManniesing.AutoSize = true;
            this.RadioButtonManniesing.Location = new System.Drawing.Point(12, 81);
            this.RadioButtonManniesing.Name = "RadioButtonManniesing";
            this.RadioButtonManniesing.Size = new System.Drawing.Size(350, 16);
            this.RadioButtonManniesing.TabIndex = 4;
            this.RadioButtonManniesing.TabStop = true;
            this.RadioButtonManniesing.Text = "Manniesing et al., Vessel enhancing diffusion (MIA, 2006)";
            this.RadioButtonManniesing.UseVisualStyleBackColor = true;
            this.RadioButtonManniesing.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // RadioButtonTruc
            // 
            this.RadioButtonTruc.AutoSize = true;
            this.RadioButtonTruc.Location = new System.Drawing.Point(12, 104);
            this.RadioButtonTruc.Name = "RadioButtonTruc";
            this.RadioButtonTruc.Size = new System.Drawing.Size(280, 16);
            this.RadioButtonTruc.TabIndex = 5;
            this.RadioButtonTruc.TabStop = true;
            this.RadioButtonTruc.Text = "Truc et al., Directional filter bank (CVIU, 2009)";
            this.RadioButtonTruc.UseVisualStyleBackColor = true;
            this.RadioButtonTruc.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
            // 
            // VEDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(554, 262);
            this.Controls.Add(this.RadioButtonTruc);
            this.Controls.Add(this.RadioButtonKrissian2);
            this.Controls.Add(this.RadioButtonManniesing);
            this.Controls.Add(this.RadioButtonKrissian1);
            this.Controls.Add(this.RadioButtonFrangi);
            this.Controls.Add(this.ButtonRun);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "VEDialog";
            this.Text = "Vessel Enhenacement Methods";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonRun;
        private System.Windows.Forms.RadioButton RadioButtonFrangi;
        private System.Windows.Forms.RadioButton RadioButtonKrissian1;
        private System.Windows.Forms.RadioButton RadioButtonManniesing;
        private System.Windows.Forms.RadioButton RadioButtonKrissian2;
        private System.Windows.Forms.RadioButton RadioButtonTruc;
    }
}