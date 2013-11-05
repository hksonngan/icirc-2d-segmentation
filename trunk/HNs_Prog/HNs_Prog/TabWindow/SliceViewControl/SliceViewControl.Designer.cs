namespace HNs_Prog
{
    partial class SliceViewControl
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
            this.components = new System.ComponentModel.Container();
            this.PanelSliceImage = new System.Windows.Forms.Panel();
            this.PanelSliceLUT = new System.Windows.Forms.Panel();
            this.PanelSliceHistogram = new System.Windows.Forms.Panel();
            this.TrackBarSliceImage = new System.Windows.Forms.TrackBar();
            this.GroupBoxSliceHistogram = new System.Windows.Forms.GroupBox();
            this.RadioButtonHistogramSlice = new System.Windows.Forms.RadioButton();
            this.RadioButtonHistogramVolume = new System.Windows.Forms.RadioButton();
            this.CheckBoxThresholding = new System.Windows.Forms.CheckBox();
            this.PanelOutputImage = new System.Windows.Forms.Panel();
            this.GroupBoxSliceOutput = new System.Windows.Forms.GroupBox();
            this.ButtonSaveOutputString = new System.Windows.Forms.Button();
            this.LabelOutputString = new System.Windows.Forms.Label();
            this.ButtonSaveOutputImage = new System.Windows.Forms.Button();
            this.LabelCurrentSlice = new System.Windows.Forms.Label();
            this.SaveFileDialogOutput = new System.Windows.Forms.SaveFileDialog();
            this.ContextMenuStripOutput = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ToolStripMenuItemSRG = new System.Windows.Forms.ToolStripMenuItem();
            this.CheckBoxMasking = new System.Windows.Forms.CheckBox();
            this.ButtonRawFileOpen = new System.Windows.Forms.Button();
            this.ButtonRawFileSave = new System.Windows.Forms.Button();
            this.ButtonDICOMSave = new System.Windows.Forms.Button();
            this.ButtonVesselResponse = new System.Windows.Forms.Button();
            this.ButtonImageSequenceSave = new System.Windows.Forms.Button();
            this.ButtonGMMTracking = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.TrackBarSliceImage)).BeginInit();
            this.GroupBoxSliceHistogram.SuspendLayout();
            this.GroupBoxSliceOutput.SuspendLayout();
            this.ContextMenuStripOutput.SuspendLayout();
            this.SuspendLayout();
            // 
            // PanelSliceImage
            // 
            this.PanelSliceImage.BackColor = System.Drawing.Color.Black;
            this.PanelSliceImage.Location = new System.Drawing.Point(77, 10);
            this.PanelSliceImage.Name = "PanelSliceImage";
            this.PanelSliceImage.Size = new System.Drawing.Size(512, 512);
            this.PanelSliceImage.TabIndex = 0;
            this.PanelSliceImage.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelSliceImagePaint);
            this.PanelSliceImage.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PanelSliceImageMouseClick);
            // 
            // PanelSliceLUT
            // 
            this.PanelSliceLUT.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.PanelSliceLUT.Location = new System.Drawing.Point(29, 532);
            this.PanelSliceLUT.Name = "PanelSliceLUT";
            this.PanelSliceLUT.Size = new System.Drawing.Size(34, 258);
            this.PanelSliceLUT.TabIndex = 1;
            this.PanelSliceLUT.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelSliceLUTPaint);
            // 
            // PanelSliceHistogram
            // 
            this.PanelSliceHistogram.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.PanelSliceHistogram.Location = new System.Drawing.Point(76, 532);
            this.PanelSliceHistogram.Name = "PanelSliceHistogram";
            this.PanelSliceHistogram.Size = new System.Drawing.Size(514, 258);
            this.PanelSliceHistogram.TabIndex = 2;
            this.PanelSliceHistogram.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelSliceHistogramPaint);
            this.PanelSliceHistogram.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PanelSliceHistogramMouseDown);
            this.PanelSliceHistogram.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PanelSliceHistogramMouseMove);
            this.PanelSliceHistogram.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PanelSliceHistogramMouseUp);
            // 
            // TrackBarSliceImage
            // 
            this.TrackBarSliceImage.Location = new System.Drawing.Point(22, 10);
            this.TrackBarSliceImage.Name = "TrackBarSliceImage";
            this.TrackBarSliceImage.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.TrackBarSliceImage.Size = new System.Drawing.Size(45, 512);
            this.TrackBarSliceImage.TabIndex = 3;
            this.TrackBarSliceImage.Value = 1;
            this.TrackBarSliceImage.Scroll += new System.EventHandler(this.TrackBarSliceImageScroll);
            this.TrackBarSliceImage.ValueChanged += new System.EventHandler(this.TrackBarSliceImageValueChanged);
            // 
            // GroupBoxSliceHistogram
            // 
            this.GroupBoxSliceHistogram.Controls.Add(this.RadioButtonHistogramSlice);
            this.GroupBoxSliceHistogram.Controls.Add(this.RadioButtonHistogramVolume);
            this.GroupBoxSliceHistogram.Location = new System.Drawing.Point(605, 722);
            this.GroupBoxSliceHistogram.Name = "GroupBoxSliceHistogram";
            this.GroupBoxSliceHistogram.Size = new System.Drawing.Size(117, 65);
            this.GroupBoxSliceHistogram.TabIndex = 4;
            this.GroupBoxSliceHistogram.TabStop = false;
            this.GroupBoxSliceHistogram.Text = "Histogram";
            // 
            // RadioButtonHistogramSlice
            // 
            this.RadioButtonHistogramSlice.AutoSize = true;
            this.RadioButtonHistogramSlice.Location = new System.Drawing.Point(12, 37);
            this.RadioButtonHistogramSlice.Name = "RadioButtonHistogramSlice";
            this.RadioButtonHistogramSlice.Size = new System.Drawing.Size(51, 16);
            this.RadioButtonHistogramSlice.TabIndex = 1;
            this.RadioButtonHistogramSlice.TabStop = true;
            this.RadioButtonHistogramSlice.Text = "Slice";
            this.RadioButtonHistogramSlice.UseVisualStyleBackColor = true;
            // 
            // RadioButtonHistogramVolume
            // 
            this.RadioButtonHistogramVolume.AutoSize = true;
            this.RadioButtonHistogramVolume.Checked = true;
            this.RadioButtonHistogramVolume.Location = new System.Drawing.Point(12, 18);
            this.RadioButtonHistogramVolume.Name = "RadioButtonHistogramVolume";
            this.RadioButtonHistogramVolume.Size = new System.Drawing.Size(66, 16);
            this.RadioButtonHistogramVolume.TabIndex = 0;
            this.RadioButtonHistogramVolume.TabStop = true;
            this.RadioButtonHistogramVolume.Text = "Volume";
            this.RadioButtonHistogramVolume.UseVisualStyleBackColor = true;
            this.RadioButtonHistogramVolume.CheckedChanged += new System.EventHandler(this.RadioButtonHistogramCheckedChanged);
            // 
            // CheckBoxThresholding
            // 
            this.CheckBoxThresholding.AutoSize = true;
            this.CheckBoxThresholding.Location = new System.Drawing.Point(605, 685);
            this.CheckBoxThresholding.Name = "CheckBoxThresholding";
            this.CheckBoxThresholding.Size = new System.Drawing.Size(98, 16);
            this.CheckBoxThresholding.TabIndex = 6;
            this.CheckBoxThresholding.Text = "Thresholding";
            this.CheckBoxThresholding.UseVisualStyleBackColor = true;
            this.CheckBoxThresholding.CheckedChanged += new System.EventHandler(this.CheckBoxThresholdingCheckedChanged);
            // 
            // PanelOutputImage
            // 
            this.PanelOutputImage.BackColor = System.Drawing.Color.Black;
            this.PanelOutputImage.Location = new System.Drawing.Point(796, 10);
            this.PanelOutputImage.Name = "PanelOutputImage";
            this.PanelOutputImage.Size = new System.Drawing.Size(512, 512);
            this.PanelOutputImage.TabIndex = 7;
            this.PanelOutputImage.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelOutputImagePaint);
            // 
            // GroupBoxSliceOutput
            // 
            this.GroupBoxSliceOutput.Controls.Add(this.ButtonSaveOutputString);
            this.GroupBoxSliceOutput.Controls.Add(this.LabelOutputString);
            this.GroupBoxSliceOutput.Controls.Add(this.ButtonSaveOutputImage);
            this.GroupBoxSliceOutput.Location = new System.Drawing.Point(796, 536);
            this.GroupBoxSliceOutput.Name = "GroupBoxSliceOutput";
            this.GroupBoxSliceOutput.Size = new System.Drawing.Size(512, 237);
            this.GroupBoxSliceOutput.TabIndex = 9;
            this.GroupBoxSliceOutput.TabStop = false;
            this.GroupBoxSliceOutput.Text = "Output";
            // 
            // ButtonSaveOutputString
            // 
            this.ButtonSaveOutputString.Location = new System.Drawing.Point(12, 204);
            this.ButtonSaveOutputString.Name = "ButtonSaveOutputString";
            this.ButtonSaveOutputString.Size = new System.Drawing.Size(494, 23);
            this.ButtonSaveOutputString.TabIndex = 1;
            this.ButtonSaveOutputString.Text = "Save as text file";
            this.ButtonSaveOutputString.UseVisualStyleBackColor = true;
            this.ButtonSaveOutputString.Click += new System.EventHandler(this.ButtonSaveOutputStringClick);
            // 
            // LabelOutputString
            // 
            this.LabelOutputString.AutoSize = true;
            this.LabelOutputString.Location = new System.Drawing.Point(12, 18);
            this.LabelOutputString.Name = "LabelOutputString";
            this.LabelOutputString.Size = new System.Drawing.Size(0, 12);
            this.LabelOutputString.TabIndex = 0;
            // 
            // ButtonSaveOutputImage
            // 
            this.ButtonSaveOutputImage.Location = new System.Drawing.Point(12, 175);
            this.ButtonSaveOutputImage.Name = "ButtonSaveOutputImage";
            this.ButtonSaveOutputImage.Size = new System.Drawing.Size(494, 23);
            this.ButtonSaveOutputImage.TabIndex = 11;
            this.ButtonSaveOutputImage.Text = "Save as image file";
            this.ButtonSaveOutputImage.UseVisualStyleBackColor = true;
            this.ButtonSaveOutputImage.Click += new System.EventHandler(this.ButtonSaveOutputImageClick);
            // 
            // LabelCurrentSlice
            // 
            this.LabelCurrentSlice.AutoSize = true;
            this.LabelCurrentSlice.Location = new System.Drawing.Point(601, 14);
            this.LabelCurrentSlice.Name = "LabelCurrentSlice";
            this.LabelCurrentSlice.Size = new System.Drawing.Size(36, 12);
            this.LabelCurrentSlice.TabIndex = 10;
            this.LabelCurrentSlice.Text = "Label";
            // 
            // ContextMenuStripOutput
            // 
            this.ContextMenuStripOutput.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItemSRG});
            this.ContextMenuStripOutput.Name = "ContextMenuStripOutput";
            this.ContextMenuStripOutput.Size = new System.Drawing.Size(204, 136);
            // 
            // ToolStripMenuItemSRG
            // 
            this.ToolStripMenuItemSRG.Name = "ToolStripMenuItemSRG";
            this.ToolStripMenuItemSRG.Size = new System.Drawing.Size(203, 22);
            this.ToolStripMenuItemSRG.Text = "Seeded Region Grwoing";
            this.ToolStripMenuItemSRG.Click += new System.EventHandler(this.ToolStripMenuItemSRGClick);
            // 
            // CheckBoxMasking
            // 
            this.CheckBoxMasking.AutoSize = true;
            this.CheckBoxMasking.Enabled = false;
            this.CheckBoxMasking.Location = new System.Drawing.Point(605, 532);
            this.CheckBoxMasking.Name = "CheckBoxMasking";
            this.CheckBoxMasking.Size = new System.Drawing.Size(100, 16);
            this.CheckBoxMasking.TabIndex = 13;
            this.CheckBoxMasking.Text = "Mask volume";
            this.CheckBoxMasking.UseVisualStyleBackColor = true;
            this.CheckBoxMasking.CheckedChanged += new System.EventHandler(this.CheckBoxMaskingCheckedChanged);
            // 
            // ButtonRawFileOpen
            // 
            this.ButtonRawFileOpen.Location = new System.Drawing.Point(603, 403);
            this.ButtonRawFileOpen.Name = "ButtonRawFileOpen";
            this.ButtonRawFileOpen.Size = new System.Drawing.Size(180, 23);
            this.ButtonRawFileOpen.TabIndex = 14;
            this.ButtonRawFileOpen.Text = "Raw File Open";
            this.ButtonRawFileOpen.UseVisualStyleBackColor = true;
            this.ButtonRawFileOpen.Click += new System.EventHandler(this.ButtonRawFileOpenClick);
            // 
            // ButtonRawFileSave
            // 
            this.ButtonRawFileSave.Location = new System.Drawing.Point(603, 432);
            this.ButtonRawFileSave.Name = "ButtonRawFileSave";
            this.ButtonRawFileSave.Size = new System.Drawing.Size(180, 23);
            this.ButtonRawFileSave.TabIndex = 17;
            this.ButtonRawFileSave.Text = "Raw File Save";
            this.ButtonRawFileSave.UseVisualStyleBackColor = true;
            this.ButtonRawFileSave.Click += new System.EventHandler(this.ButtonRawFileSaveClick);
            // 
            // ButtonDICOMSave
            // 
            this.ButtonDICOMSave.Location = new System.Drawing.Point(603, 499);
            this.ButtonDICOMSave.Name = "ButtonDICOMSave";
            this.ButtonDICOMSave.Size = new System.Drawing.Size(180, 23);
            this.ButtonDICOMSave.TabIndex = 17;
            this.ButtonDICOMSave.Text = "DICOM Save";
            this.ButtonDICOMSave.UseVisualStyleBackColor = true;
            this.ButtonDICOMSave.Click += new System.EventHandler(this.ButtonDICOMSaveClick);
            // 
            // ButtonVesselResponse
            // 
            this.ButtonVesselResponse.Location = new System.Drawing.Point(605, 40);
            this.ButtonVesselResponse.Name = "ButtonVesselResponse";
            this.ButtonVesselResponse.Size = new System.Drawing.Size(178, 23);
            this.ButtonVesselResponse.TabIndex = 23;
            this.ButtonVesselResponse.Text = "Vessel Response";
            this.ButtonVesselResponse.UseVisualStyleBackColor = true;
            this.ButtonVesselResponse.Click += new System.EventHandler(this.ButtonVesselResponseClick);
            // 
            // ButtonGMMTracking
            // 
            this.ButtonGMMTracking.Location = new System.Drawing.Point(605, 70);
            this.ButtonGMMTracking.Name = "ButtonGMMTracking";
            this.ButtonGMMTracking.Size = new System.Drawing.Size(178, 23);
            this.ButtonGMMTracking.TabIndex = 25;
            this.ButtonGMMTracking.Text = "GMM-based Tracking";
            this.ButtonGMMTracking.UseVisualStyleBackColor = true;
            this.ButtonGMMTracking.Click += new System.EventHandler(this.ButtonGMMTrackingClick);
            // 
            // ButtonImageSequenceSave
            // 
            this.ButtonImageSequenceSave.Location = new System.Drawing.Point(603, 470);
            this.ButtonImageSequenceSave.Name = "ButtonImageSequenceSave";
            this.ButtonImageSequenceSave.Size = new System.Drawing.Size(180, 23);
            this.ButtonImageSequenceSave.TabIndex = 24;
            this.ButtonImageSequenceSave.Text = "Image Sequence Save";
            this.ButtonImageSequenceSave.UseVisualStyleBackColor = true;
            this.ButtonImageSequenceSave.Click += new System.EventHandler(this.ButtonImageSequenceSaveClick);
            // 
            // SliceViewControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.ButtonGMMTracking);
            this.Controls.Add(this.ButtonImageSequenceSave);
            this.Controls.Add(this.ButtonRawFileSave);
            this.Controls.Add(this.ButtonVesselResponse);
            this.Controls.Add(this.ButtonRawFileOpen);
            this.Controls.Add(this.CheckBoxMasking);
            this.Controls.Add(this.ButtonDICOMSave);
            this.Controls.Add(this.LabelCurrentSlice);
            this.Controls.Add(this.GroupBoxSliceOutput);
            this.Controls.Add(this.PanelOutputImage);
            this.Controls.Add(this.CheckBoxThresholding);
            this.Controls.Add(this.GroupBoxSliceHistogram);
            this.Controls.Add(this.TrackBarSliceImage);
            this.Controls.Add(this.PanelSliceHistogram);
            this.Controls.Add(this.PanelSliceLUT);
            this.Controls.Add(this.PanelSliceImage);
            this.Name = "SliceViewControl";
            this.Size = new System.Drawing.Size(1581, 964);
            ((System.ComponentModel.ISupportInitialize)(this.TrackBarSliceImage)).EndInit();
            this.GroupBoxSliceHistogram.ResumeLayout(false);
            this.GroupBoxSliceHistogram.PerformLayout();
            this.GroupBoxSliceOutput.ResumeLayout(false);
            this.GroupBoxSliceOutput.PerformLayout();
            this.ContextMenuStripOutput.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel PanelSliceImage;
        private System.Windows.Forms.Panel PanelSliceLUT;
        private System.Windows.Forms.Panel PanelSliceHistogram;
        private System.Windows.Forms.TrackBar TrackBarSliceImage;
        private System.Windows.Forms.GroupBox GroupBoxSliceHistogram;
        private System.Windows.Forms.RadioButton RadioButtonHistogramVolume;
        private System.Windows.Forms.RadioButton RadioButtonHistogramSlice;
        private System.Windows.Forms.CheckBox CheckBoxThresholding;
        private System.Windows.Forms.Panel PanelOutputImage;
        private System.Windows.Forms.GroupBox GroupBoxSliceOutput;
        private System.Windows.Forms.Label LabelCurrentSlice;
        private System.Windows.Forms.Button ButtonSaveOutputImage;
        private System.Windows.Forms.SaveFileDialog SaveFileDialogOutput;
        private System.Windows.Forms.ContextMenuStrip ContextMenuStripOutput;
        private System.Windows.Forms.CheckBox CheckBoxMasking;
        private System.Windows.Forms.Button ButtonRawFileOpen;
        private System.Windows.Forms.Label LabelOutputString;
        private System.Windows.Forms.Button ButtonSaveOutputString;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemSRG;
        private System.Windows.Forms.Button ButtonRawFileSave;
        private System.Windows.Forms.Button ButtonDICOMSave;
        private System.Windows.Forms.Button ButtonVesselResponse;
        private System.Windows.Forms.Button ButtonImageSequenceSave;
        private System.Windows.Forms.Button ButtonGMMTracking;
    }
}
