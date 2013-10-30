namespace HNs_Prog
{
    partial class MPRViewControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GroupBoxMPRHistogram = new System.Windows.Forms.GroupBox();
            this.RadioButtonHistogramSagittal = new System.Windows.Forms.RadioButton();
            this.RadioButtonHistogramCoronal = new System.Windows.Forms.RadioButton();
            this.RadioButtonHistogramAxial = new System.Windows.Forms.RadioButton();
            this.RadioButtonHistogramVolume = new System.Windows.Forms.RadioButton();
            this.PanelMPRLUT = new System.Windows.Forms.Panel();
            this.PanelMPRHistogram = new System.Windows.Forms.Panel();
            this.PanelMPRSagittal = new System.Windows.Forms.Panel();
            this.PanelMPRCoronal = new System.Windows.Forms.Panel();
            this.PanelMPRAxial = new System.Windows.Forms.Panel();
            this.GroupBoxMPRWindowing = new System.Windows.Forms.GroupBox();
            this.RadioButtonWindowThresholding = new System.Windows.Forms.RadioButton();
            this.RadioButtonWindowBasic = new System.Windows.Forms.RadioButton();
            this.LabelLUT4 = new System.Windows.Forms.Label();
            this.LabelLUT1 = new System.Windows.Forms.Label();
            this.LabelLUT3 = new System.Windows.Forms.Label();
            this.LabelLUT2 = new System.Windows.Forms.Label();
            this.GroupBoxMPRHistogram.SuspendLayout();
            this.GroupBoxMPRWindowing.SuspendLayout();
            this.SuspendLayout();
            // 
            // GroupBoxMPRHistogram
            // 
            this.GroupBoxMPRHistogram.Controls.Add(this.RadioButtonHistogramSagittal);
            this.GroupBoxMPRHistogram.Controls.Add(this.RadioButtonHistogramCoronal);
            this.GroupBoxMPRHistogram.Controls.Add(this.RadioButtonHistogramAxial);
            this.GroupBoxMPRHistogram.Controls.Add(this.RadioButtonHistogramVolume);
            this.GroupBoxMPRHistogram.Location = new System.Drawing.Point(1000, 18);
            this.GroupBoxMPRHistogram.Name = "GroupBoxMPRHistogram";
            this.GroupBoxMPRHistogram.Size = new System.Drawing.Size(100, 106);
            this.GroupBoxMPRHistogram.TabIndex = 4;
            this.GroupBoxMPRHistogram.TabStop = false;
            this.GroupBoxMPRHistogram.Text = "Histogram";
            // 
            // RadioButtonHistogramSagittal
            // 
            this.RadioButtonHistogramSagittal.AutoSize = true;
            this.RadioButtonHistogramSagittal.Location = new System.Drawing.Point(10, 80);
            this.RadioButtonHistogramSagittal.Name = "RadioButtonHistogramSagittal";
            this.RadioButtonHistogramSagittal.Size = new System.Drawing.Size(60, 17);
            this.RadioButtonHistogramSagittal.TabIndex = 3;
            this.RadioButtonHistogramSagittal.TabStop = true;
            this.RadioButtonHistogramSagittal.Text = "Sagittal";
            this.RadioButtonHistogramSagittal.UseVisualStyleBackColor = true;
            this.RadioButtonHistogramSagittal.CheckedChanged += new System.EventHandler(this.RadioButtonHistogramCheckedChanged);
            // 
            // RadioButtonHistogramCoronal
            // 
            this.RadioButtonHistogramCoronal.AutoSize = true;
            this.RadioButtonHistogramCoronal.Location = new System.Drawing.Point(10, 60);
            this.RadioButtonHistogramCoronal.Name = "RadioButtonHistogramCoronal";
            this.RadioButtonHistogramCoronal.Size = new System.Drawing.Size(61, 17);
            this.RadioButtonHistogramCoronal.TabIndex = 2;
            this.RadioButtonHistogramCoronal.TabStop = true;
            this.RadioButtonHistogramCoronal.Text = "Coronal";
            this.RadioButtonHistogramCoronal.UseVisualStyleBackColor = true;
            this.RadioButtonHistogramCoronal.CheckedChanged += new System.EventHandler(this.RadioButtonHistogramCheckedChanged);
            // 
            // RadioButtonHistogramAxial
            // 
            this.RadioButtonHistogramAxial.AutoSize = true;
            this.RadioButtonHistogramAxial.Location = new System.Drawing.Point(10, 40);
            this.RadioButtonHistogramAxial.Name = "RadioButtonHistogramAxial";
            this.RadioButtonHistogramAxial.Size = new System.Drawing.Size(47, 17);
            this.RadioButtonHistogramAxial.TabIndex = 1;
            this.RadioButtonHistogramAxial.Text = "Axial";
            this.RadioButtonHistogramAxial.UseVisualStyleBackColor = true;
            this.RadioButtonHistogramAxial.CheckedChanged += new System.EventHandler(this.RadioButtonHistogramCheckedChanged);
            // 
            // RadioButtonHistogramVolume
            // 
            this.RadioButtonHistogramVolume.AutoSize = true;
            this.RadioButtonHistogramVolume.Checked = true;
            this.RadioButtonHistogramVolume.Location = new System.Drawing.Point(10, 20);
            this.RadioButtonHistogramVolume.Name = "RadioButtonHistogramVolume";
            this.RadioButtonHistogramVolume.Size = new System.Drawing.Size(60, 17);
            this.RadioButtonHistogramVolume.TabIndex = 0;
            this.RadioButtonHistogramVolume.TabStop = true;
            this.RadioButtonHistogramVolume.Text = "Volume";
            this.RadioButtonHistogramVolume.UseVisualStyleBackColor = true;
            this.RadioButtonHistogramVolume.CheckedChanged += new System.EventHandler(this.RadioButtonHistogramCheckedChanged);
            // 
            // PanelMPRLUT
            // 
            this.PanelMPRLUT.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.PanelMPRLUT.Location = new System.Drawing.Point(522, 30);
            this.PanelMPRLUT.Name = "PanelMPRLUT";
            this.PanelMPRLUT.Size = new System.Drawing.Size(26, 382);
            this.PanelMPRLUT.TabIndex = 5;
            this.PanelMPRLUT.Paint += new System.Windows.Forms.PaintEventHandler(this.panelLUTPaint);
            // 
            // PanelMPRHistogram
            // 
            this.PanelMPRHistogram.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.PanelMPRHistogram.Location = new System.Drawing.Point(556, 30);
            this.PanelMPRHistogram.Name = "PanelMPRHistogram";
            this.PanelMPRHistogram.Size = new System.Drawing.Size(382, 382);
            this.PanelMPRHistogram.TabIndex = 3;
            this.PanelMPRHistogram.Paint += new System.Windows.Forms.PaintEventHandler(this.panelHistogramPaint);
            this.PanelMPRHistogram.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panelHistogramMouseMove);
            this.PanelMPRHistogram.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelHistogramMouseDown);
            this.PanelMPRHistogram.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panelHistogramMoseUp);
            // 
            // PanelMPRSagittal
            // 
            this.PanelMPRSagittal.AutoScroll = true;
            this.PanelMPRSagittal.BackColor = System.Drawing.Color.Black;
            this.PanelMPRSagittal.Location = new System.Drawing.Point(483, 483);
            this.PanelMPRSagittal.Name = "PanelMPRSagittal";
            this.PanelMPRSagittal.Size = new System.Drawing.Size(478, 478);
            this.PanelMPRSagittal.TabIndex = 2;
            this.PanelMPRSagittal.Paint += new System.Windows.Forms.PaintEventHandler(this.panelSagittalImagePaint);
            // 
            // PanelMPRCoronal
            // 
            this.PanelMPRCoronal.AutoScroll = true;
            this.PanelMPRCoronal.BackColor = System.Drawing.Color.Black;
            this.PanelMPRCoronal.Location = new System.Drawing.Point(2, 483);
            this.PanelMPRCoronal.Name = "PanelMPRCoronal";
            this.PanelMPRCoronal.Size = new System.Drawing.Size(478, 478);
            this.PanelMPRCoronal.TabIndex = 1;
            this.PanelMPRCoronal.Paint += new System.Windows.Forms.PaintEventHandler(this.panelCoronalImagePaint);
            // 
            // PanelMPRAxial
            // 
            this.PanelMPRAxial.AutoScroll = true;
            this.PanelMPRAxial.BackColor = System.Drawing.Color.Black;
            this.PanelMPRAxial.ForeColor = System.Drawing.SystemColors.Control;
            this.PanelMPRAxial.Location = new System.Drawing.Point(2, 2);
            this.PanelMPRAxial.Name = "PanelMPRAxial";
            this.PanelMPRAxial.Size = new System.Drawing.Size(478, 478);
            this.PanelMPRAxial.TabIndex = 0;
            this.PanelMPRAxial.Paint += new System.Windows.Forms.PaintEventHandler(this.panelAxialImagePaint);
            // 
            // GroupBoxMPRWindowing
            // 
            this.GroupBoxMPRWindowing.Controls.Add(this.RadioButtonWindowThresholding);
            this.GroupBoxMPRWindowing.Controls.Add(this.RadioButtonWindowBasic);
            this.GroupBoxMPRWindowing.Location = new System.Drawing.Point(1120, 18);
            this.GroupBoxMPRWindowing.Name = "GroupBoxMPRWindowing";
            this.GroupBoxMPRWindowing.Size = new System.Drawing.Size(120, 106);
            this.GroupBoxMPRWindowing.TabIndex = 6;
            this.GroupBoxMPRWindowing.TabStop = false;
            this.GroupBoxMPRWindowing.Text = "Window Function";
            // 
            // RadioButtonWindowThresholding
            // 
            this.RadioButtonWindowThresholding.AutoSize = true;
            this.RadioButtonWindowThresholding.Location = new System.Drawing.Point(10, 40);
            this.RadioButtonWindowThresholding.Name = "RadioButtonWindowThresholding";
            this.RadioButtonWindowThresholding.Size = new System.Drawing.Size(86, 17);
            this.RadioButtonWindowThresholding.TabIndex = 1;
            this.RadioButtonWindowThresholding.Text = "Thresholding";
            this.RadioButtonWindowThresholding.UseVisualStyleBackColor = true;
            this.RadioButtonWindowThresholding.CheckedChanged += new System.EventHandler(this.RadioButtonWindowingCheckedChanged);
            // 
            // RadioButtonWindowBasic
            // 
            this.RadioButtonWindowBasic.AutoSize = true;
            this.RadioButtonWindowBasic.Checked = true;
            this.RadioButtonWindowBasic.Location = new System.Drawing.Point(10, 20);
            this.RadioButtonWindowBasic.Name = "RadioButtonWindowBasic";
            this.RadioButtonWindowBasic.Size = new System.Drawing.Size(51, 17);
            this.RadioButtonWindowBasic.TabIndex = 0;
            this.RadioButtonWindowBasic.TabStop = true;
            this.RadioButtonWindowBasic.Text = "Basic";
            this.RadioButtonWindowBasic.UseVisualStyleBackColor = true;
            this.RadioButtonWindowBasic.CheckedChanged += new System.EventHandler(this.RadioButtonWindowingCheckedChanged);
            // 
            // LabelLUT4
            // 
            this.LabelLUT4.AutoSize = true;
            this.LabelLUT4.Location = new System.Drawing.Point(490, 24);
            this.LabelLUT4.Name = "LabelLUT4";
            this.LabelLUT4.Size = new System.Drawing.Size(31, 13);
            this.LabelLUT4.TabIndex = 7;
            this.LabelLUT4.Text = "255 -";
            // 
            // LabelLUT1
            // 
            this.LabelLUT1.AutoSize = true;
            this.LabelLUT1.Location = new System.Drawing.Point(490, 403);
            this.LabelLUT1.Name = "LabelLUT1";
            this.LabelLUT1.Size = new System.Drawing.Size(31, 13);
            this.LabelLUT1.TabIndex = 8;
            this.LabelLUT1.Text = "0 -----";
            // 
            // LabelLUT3
            // 
            this.LabelLUT3.AutoSize = true;
            this.LabelLUT3.Location = new System.Drawing.Point(490, 150);
            this.LabelLUT3.Name = "LabelLUT3";
            this.LabelLUT3.Size = new System.Drawing.Size(31, 13);
            this.LabelLUT3.TabIndex = 9;
            this.LabelLUT3.Text = "170 -";
            // 
            // LabelLUT2
            // 
            this.LabelLUT2.AutoSize = true;
            this.LabelLUT2.Location = new System.Drawing.Point(490, 277);
            this.LabelLUT2.Name = "LabelLUT2";
            this.LabelLUT2.Size = new System.Drawing.Size(31, 13);
            this.LabelLUT2.TabIndex = 10;
            this.LabelLUT2.Text = "85 ---";
            // 
            // MPRViewControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.LabelLUT2);
            this.Controls.Add(this.LabelLUT3);
            this.Controls.Add(this.LabelLUT1);
            this.Controls.Add(this.LabelLUT4);
            this.Controls.Add(this.GroupBoxMPRWindowing);
            this.Controls.Add(this.PanelMPRLUT);
            this.Controls.Add(this.GroupBoxMPRHistogram);
            this.Controls.Add(this.PanelMPRHistogram);
            this.Controls.Add(this.PanelMPRSagittal);
            this.Controls.Add(this.PanelMPRCoronal);
            this.Controls.Add(this.PanelMPRAxial);
            this.Name = "MPRViewControl";
            this.Size = new System.Drawing.Size(1262, 964);
            this.GroupBoxMPRHistogram.ResumeLayout(false);
            this.GroupBoxMPRHistogram.PerformLayout();
            this.GroupBoxMPRWindowing.ResumeLayout(false);
            this.GroupBoxMPRWindowing.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox GroupBoxMPRHistogram;
        private System.Windows.Forms.RadioButton RadioButtonHistogramAxial;
        private System.Windows.Forms.RadioButton RadioButtonHistogramVolume;
        private System.Windows.Forms.RadioButton RadioButtonHistogramCoronal;
        private System.Windows.Forms.RadioButton RadioButtonHistogramSagittal;
        private System.Windows.Forms.GroupBox GroupBoxMPRWindowing;
        private System.Windows.Forms.RadioButton RadioButtonWindowThresholding;
        private System.Windows.Forms.RadioButton RadioButtonWindowBasic;
        private System.Windows.Forms.Panel PanelMPRLUT;
        private System.Windows.Forms.Panel PanelMPRAxial;
        private System.Windows.Forms.Panel PanelMPRCoronal;
        private System.Windows.Forms.Panel PanelMPRSagittal;
        private System.Windows.Forms.Panel PanelMPRHistogram;
        private bool PanelMPRHistogramMouseClicked;
        private int ClickedControlPointIndex;
        private System.Windows.Forms.Label LabelLUT4;
        private System.Windows.Forms.Label LabelLUT1;
        private System.Windows.Forms.Label LabelLUT3;
        private System.Windows.Forms.Label LabelLUT2;
    }
}
