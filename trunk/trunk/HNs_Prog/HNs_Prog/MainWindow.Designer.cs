namespace HNs_Prog
{
    partial class FormMainWindow
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
            this.tabPageLibrary = new System.Windows.Forms.TabPage();
            this.ButtonSliceView = new System.Windows.Forms.Button();
            this.ButtonVRView = new System.Windows.Forms.Button();
            this.ButtonMPRView = new System.Windows.Forms.Button();
            this.ListControlVolumeLibrary = new System.Windows.Forms.ListView();
            this.columnHeaderStudyID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderSeriesNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderStudyDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderStudyDesc = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderSeriesDesc = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderPatientName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderPatientBirthDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderPatientSex = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ButtonImport = new System.Windows.Forms.Button();
            this.tabControlMainView = new System.Windows.Forms.TabControl();
            this.tabPageLibrary.SuspendLayout();
            this.tabControlMainView.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPageLibrary
            // 
            this.tabPageLibrary.Controls.Add(this.ButtonSliceView);
            this.tabPageLibrary.Controls.Add(this.ButtonVRView);
            this.tabPageLibrary.Controls.Add(this.ButtonMPRView);
            this.tabPageLibrary.Controls.Add(this.ListControlVolumeLibrary);
            this.tabPageLibrary.Controls.Add(this.ButtonImport);
            this.tabPageLibrary.Location = new System.Drawing.Point(4, 22);
            this.tabPageLibrary.Name = "tabPageLibrary";
            this.tabPageLibrary.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageLibrary.Size = new System.Drawing.Size(1581, 964);
            this.tabPageLibrary.TabIndex = 0;
            this.tabPageLibrary.Text = "DICOM Library";
            this.tabPageLibrary.UseVisualStyleBackColor = true;
            // 
            // ButtonSliceView
            // 
            this.ButtonSliceView.Location = new System.Drawing.Point(101, 6);
            this.ButtonSliceView.Name = "ButtonSliceView";
            this.ButtonSliceView.Size = new System.Drawing.Size(87, 23);
            this.ButtonSliceView.TabIndex = 4;
            this.ButtonSliceView.Text = "2D Slice";
            this.ButtonSliceView.UseVisualStyleBackColor = true;
            this.ButtonSliceView.Click += new System.EventHandler(this.ButtonSliceViewClick);
            // 
            // ButtonVRView
            // 
            this.ButtonVRView.Location = new System.Drawing.Point(197, 6);
            this.ButtonVRView.Name = "ButtonVRView";
            this.ButtonVRView.Size = new System.Drawing.Size(87, 23);
            this.ButtonVRView.TabIndex = 3;
            this.ButtonVRView.Text = "VR";
            this.ButtonVRView.UseVisualStyleBackColor = true;
            this.ButtonVRView.Click += new System.EventHandler(this.ButtonVRViewClick);
            // 
            // ButtonMPRView
            // 
            this.ButtonMPRView.Location = new System.Drawing.Point(293, 6);
            this.ButtonMPRView.Name = "ButtonMPRView";
            this.ButtonMPRView.Size = new System.Drawing.Size(87, 23);
            this.ButtonMPRView.TabIndex = 2;
            this.ButtonMPRView.Text = "MPR";
            this.ButtonMPRView.UseVisualStyleBackColor = true;
            this.ButtonMPRView.Click += new System.EventHandler(this.ButtonMPRViewClick);
            // 
            // ListControlVolumeLibrary
            // 
            this.ListControlVolumeLibrary.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderStudyID,
            this.columnHeaderSeriesNumber,
            this.columnHeaderStudyDate,
            this.columnHeaderStudyDesc,
            this.columnHeaderSeriesDesc,
            this.columnHeaderPatientName,
            this.columnHeaderPatientBirthDate,
            this.columnHeaderPatientSex});
            this.ListControlVolumeLibrary.FullRowSelect = true;
            this.ListControlVolumeLibrary.GridLines = true;
            this.ListControlVolumeLibrary.Location = new System.Drawing.Point(6, 35);
            this.ListControlVolumeLibrary.MultiSelect = false;
            this.ListControlVolumeLibrary.Name = "ListControlVolumeLibrary";
            this.ListControlVolumeLibrary.Size = new System.Drawing.Size(1569, 860);
            this.ListControlVolumeLibrary.TabIndex = 1;
            this.ListControlVolumeLibrary.UseCompatibleStateImageBehavior = false;
            this.ListControlVolumeLibrary.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderStudyID
            // 
            this.columnHeaderStudyID.Text = "Study ID";
            this.columnHeaderStudyID.Width = 80;
            // 
            // columnHeaderSeriesNumber
            // 
            this.columnHeaderSeriesNumber.Text = "Series Number";
            this.columnHeaderSeriesNumber.Width = 100;
            // 
            // columnHeaderStudyDate
            // 
            this.columnHeaderStudyDate.Text = "Study Date";
            this.columnHeaderStudyDate.Width = 100;
            // 
            // columnHeaderStudyDesc
            // 
            this.columnHeaderStudyDesc.Text = "Study Description";
            this.columnHeaderStudyDesc.Width = 150;
            // 
            // columnHeaderSeriesDesc
            // 
            this.columnHeaderSeriesDesc.Text = "Series Description";
            this.columnHeaderSeriesDesc.Width = 120;
            // 
            // columnHeaderPatientName
            // 
            this.columnHeaderPatientName.Text = "Patient\'s Name";
            this.columnHeaderPatientName.Width = 150;
            // 
            // columnHeaderPatientBirthDate
            // 
            this.columnHeaderPatientBirthDate.Text = "Patient\'s Birth Date";
            this.columnHeaderPatientBirthDate.Width = 120;
            // 
            // columnHeaderPatientSex
            // 
            this.columnHeaderPatientSex.Text = "Patient\'s Sex";
            this.columnHeaderPatientSex.Width = 80;
            // 
            // ButtonImport
            // 
            this.ButtonImport.Location = new System.Drawing.Point(6, 6);
            this.ButtonImport.Name = "ButtonImport";
            this.ButtonImport.Size = new System.Drawing.Size(87, 23);
            this.ButtonImport.TabIndex = 0;
            this.ButtonImport.Text = "Import";
            this.ButtonImport.UseVisualStyleBackColor = true;
            this.ButtonImport.Click += new System.EventHandler(this.ButtonImportClick);
            // 
            // tabControlMainView
            // 
            this.tabControlMainView.Controls.Add(this.tabPageLibrary);
            this.tabControlMainView.Location = new System.Drawing.Point(3, 3);
            this.tabControlMainView.Name = "tabControlMainView";
            this.tabControlMainView.SelectedIndex = 0;
            this.tabControlMainView.Size = new System.Drawing.Size(1589, 990);
            this.tabControlMainView.TabIndex = 0;
            // 
            // FormMainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1594, 996);
            this.Controls.Add(this.tabControlMainView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormMainWindow";
            this.Text = "HN\'s Prog";
            this.tabPageLibrary.ResumeLayout(false);
            this.tabControlMainView.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabPage tabPageLibrary;
        private System.Windows.Forms.TabControl tabControlMainView;
        private System.Windows.Forms.Button ButtonImport;
        private System.Windows.Forms.ListView ListControlVolumeLibrary;
        private System.Windows.Forms.ColumnHeader columnHeaderStudyID;
        private System.Windows.Forms.ColumnHeader columnHeaderStudyDate;
        private System.Windows.Forms.ColumnHeader columnHeaderStudyDesc;
        private System.Windows.Forms.ColumnHeader columnHeaderPatientName;
        private System.Windows.Forms.ColumnHeader columnHeaderPatientSex;
        private System.Windows.Forms.ColumnHeader columnHeaderPatientBirthDate;
        private System.Windows.Forms.ColumnHeader columnHeaderSeriesNumber;
        private System.Windows.Forms.Button ButtonMPRView;
        private System.Windows.Forms.Button ButtonVRView;
        private System.Windows.Forms.Button ButtonSliceView;

        //private MPRImagePanelControl imagePanelControlAxial;
        //private MPRImagePanelControl imagePanelControlCoronal;
        private MPRViewControl MPRViewWindow;
        private VRViewControl VRViewWindow;
        private SliceViewControl SliceViewWindow;
        private System.Windows.Forms.ColumnHeader columnHeaderSeriesDesc;
    }
}

