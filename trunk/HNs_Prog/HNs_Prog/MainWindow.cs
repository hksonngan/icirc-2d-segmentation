using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace HNs_Prog
{
    public partial class FormMainWindow : Form
    {
        List<DicomVolume> DicomLibrary;

        public FormMainWindow()
        {
            InitializeComponent();
            DicomLibrary = new List<DicomVolume>();
        }

        private void ButtonImportClick(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowser.ShowNewFolderButton = false;
            DialogResult result = folderBrowser.ShowDialog();
            if (result == DialogResult.OK)
            {
                string[] aryFiles = Directory.GetFiles(folderBrowser.SelectedPath);

                ProgressWindow winProgress = new ProgressWindow("DICOM File Loading", 0, aryFiles.Length);
                winProgress.Show();
                for (int i = 0; i < aryFiles.Length; i++)
                {
                    DicomDecoder decoder = new DicomDecoder();
                    if (decoder.ScanDicomFile(aryFiles[i]))
                    {
                        string[] ItemInfomation = new string[9];
                        for (int j = 0; j < decoder.dicomInfo.Count; j++)
                        {
                            string InfomationTag = decoder.dicomInfo[j].Substring(0, 8);
                            if (InfomationTag.Equals("00200010"))           // Study ID
                                ItemInfomation[0] = decoder.dicomInfo[j].Substring(20, decoder.dicomInfo[j].Length - 20);
                            else if (InfomationTag.Equals("00200011"))      // Series Number
                                ItemInfomation[1] = decoder.dicomInfo[j].Substring(25, decoder.dicomInfo[j].Length - 25);
                            else if (InfomationTag.Equals("00280008"))      // Number of Frames
                                ItemInfomation[2] = decoder.dicomInfo[j].Substring(28, decoder.dicomInfo[j].Length - 28);
                            else if (InfomationTag.Equals("00080020"))      // Study Date
                                ItemInfomation[3] = decoder.dicomInfo[j].Substring(22, decoder.dicomInfo[j].Length - 22);
                            else if (InfomationTag.Equals("00081030"))      // Study Description
                                ItemInfomation[4] = decoder.dicomInfo[j].Substring(29, decoder.dicomInfo[j].Length - 29);
                            else if (InfomationTag.Equals("0008103E"))      // Series Description
                                ItemInfomation[5] = decoder.dicomInfo[j].Substring(30, decoder.dicomInfo[j].Length - 30);
                            else if (InfomationTag.Equals("00100010"))      // Patient's Name
                                ItemInfomation[6] = decoder.dicomInfo[j].Substring(26, decoder.dicomInfo[j].Length - 26);
                            else if (InfomationTag.Equals("00100030"))      // Patient's Birth Date
                                ItemInfomation[7] = decoder.dicomInfo[j].Substring(32, decoder.dicomInfo[j].Length - 32);
                            else if (InfomationTag.Equals("00100040"))      // Patient's Sex 
                                ItemInfomation[8] = decoder.dicomInfo[j].Substring(25, decoder.dicomInfo[j].Length - 25);
                        }

                        if (DicomLibrary.Count > 0)
                        {
                            bool IsContains = false;
                            for (int j = 0; j < DicomLibrary.Count && !IsContains; j++)
                            {
                                if (DicomLibrary[j].StudyID == ItemInfomation[0] && DicomLibrary[j].SeriesNumber == ItemInfomation[1])
                                {
                                    DicomLibrary[j].AddNextSlice(aryFiles[i]);
                                    IsContains = true;
                                }
                            }
                            if (!IsContains)
                            {
                                DicomVolume NewVolume = new DicomVolume(aryFiles[i], decoder, ItemInfomation);
                                DicomLibrary.Add(NewVolume);
                            }
                        }
                        else
                        {
                            DicomVolume NewVolume = new DicomVolume(aryFiles[i], decoder, ItemInfomation);
                            DicomLibrary.Add(NewVolume);
                        }
                    }
                    winProgress.Increment(1);
                }
                winProgress.Close();
                UpdateListControlVolumeLibrary();
            }
        }

        private void ButtonMPRViewClick(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection Indexes = this.ListControlVolumeLibrary.SelectedIndices;
            if (Indexes.Count == 1)
            {
                // New Tab Create.
                TabPage TabPageMPRView = new TabPage();
                TabPageMPRView.Location = new System.Drawing.Point(4, 22);
                TabPageMPRView.Name = "TabPageMPRView";
                TabPageMPRView.Padding = new System.Windows.Forms.Padding(3);
                TabPageMPRView.Size = new System.Drawing.Size(1262, 964);
                TabPageMPRView.TabIndex = 1;
                TabPageMPRView.Text = "MPR View";
                TabPageMPRView.UseVisualStyleBackColor = true;
                this.tabControlMainView.TabPages.Add(TabPageMPRView);
                this.tabControlMainView.SelectTab("TabPageMPRView");

                Volume SelectedVolume = DicomLibrary[Indexes[0]].ImportVolume();
                this.MPRViewWindow = new MPRViewControl(ref SelectedVolume);
                this.MPRViewWindow.BackColor = System.Drawing.SystemColors.Control;
                this.MPRViewWindow.Location = new System.Drawing.Point(5, 5);
                this.MPRViewWindow.Name = "MPRViewUserControl";
                this.MPRViewWindow.Size = new System.Drawing.Size(1262, 964);
                this.MPRViewWindow.TabIndex = 1;
                TabPageMPRView.Controls.Add(this.MPRViewWindow);

                MessageBox.Show("MPRView");
            }
        }

        private void ButtonSliceViewClick(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection Indexes = this.ListControlVolumeLibrary.SelectedIndices;
            if (Indexes.Count == 1)
            {
                Volume SelectedVolume = DicomLibrary[Indexes[0]].ImportVolume();

                // New Tab Create.
                TabPage TabPageSliceView = new TabPage();
                TabPageSliceView.Location = new System.Drawing.Point(4, 22);
                TabPageSliceView.Name = "TabPageSliceView";
                TabPageSliceView.Padding = new System.Windows.Forms.Padding(3);
                TabPageSliceView.Size = new System.Drawing.Size(1581, 964);
                TabPageSliceView.TabIndex = 1;
                TabPageSliceView.Text = "2D Slice View - " + DicomLibrary[Indexes[0]].StudyID + " / " + DicomLibrary[Indexes[0]].SeriesNumber;
                TabPageSliceView.UseVisualStyleBackColor = true;
                this.tabControlMainView.TabPages.Add(TabPageSliceView);
                this.tabControlMainView.SelectTab("TabPageSliceView");

                this.SliceViewWindow = new SliceViewControl(ref SelectedVolume);
                this.SliceViewWindow.BackColor = System.Drawing.SystemColors.Control;
                this.SliceViewWindow.Location = new System.Drawing.Point(5, 5);
                this.SliceViewWindow.Name = "SliceViewControl";
                this.SliceViewWindow.Size = new System.Drawing.Size(1581, 964);
                this.SliceViewWindow.TabIndex = 1;
                TabPageSliceView.Controls.Add(this.SliceViewWindow);
            }
        }

        private void UpdateListControlVolumeLibrary()
        {
            this.ListControlVolumeLibrary.Items.Clear();
            string[] ItemInfomation = new string[8];
            for (int i = 0; i < DicomLibrary.Count; i++)
            {
                ItemInfomation[0] = DicomLibrary[i].StudyID;
                ItemInfomation[1] = DicomLibrary[i].SeriesNumber;
                ItemInfomation[2] = DicomLibrary[i].StudyDate;
                ItemInfomation[3] = DicomLibrary[i].StudyDesc;
                ItemInfomation[4] = DicomLibrary[i].SeriesDesc;
                ItemInfomation[5] = DicomLibrary[i].PatientName;
                ItemInfomation[6] = DicomLibrary[i].PatientBirthDate;
                ItemInfomation[7] = DicomLibrary[i].PatientSex;
                ListViewItem Item = new ListViewItem(ItemInfomation);
                this.ListControlVolumeLibrary.Items.Add(Item);
            }
        }

        private void ButtonVRViewClick(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection Indexes = this.ListControlVolumeLibrary.SelectedIndices;
            if (Indexes.Count == 1)
            {
                // New Tab Create.
                TabPage TabPageVRView = new TabPage();
                TabPageVRView.Location = new System.Drawing.Point(4, 22);
                TabPageVRView.Name = "TabPageVRView";
                TabPageVRView.Padding = new System.Windows.Forms.Padding(3);
                TabPageVRView.Size = new System.Drawing.Size(1262, 964);
                TabPageVRView.TabIndex = 2;
                TabPageVRView.Text = "VR View";
                TabPageVRView.UseVisualStyleBackColor = true;
                this.tabControlMainView.TabPages.Add(TabPageVRView);
                this.tabControlMainView.SelectTab("TabPageVRView");

                Volume SelectedVolume = DicomLibrary[Indexes[0]].ImportVolume();
                this.VRViewWindow = new VRViewControl();
                this.VRViewWindow.BackColor = System.Drawing.SystemColors.Control;
                this.VRViewWindow.Location = new System.Drawing.Point(5, 5);
                this.VRViewWindow.Name = "VRViewUserControl";
                this.VRViewWindow.Size = new System.Drawing.Size(1262, 964);
                this.VRViewWindow.TabIndex = 2;
                TabPageVRView.Controls.Add(this.VRViewWindow);

                MessageBox.Show("VRView");
            }
        }


    }
}
