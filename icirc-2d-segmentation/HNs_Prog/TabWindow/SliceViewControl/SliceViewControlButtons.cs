using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using iCiRC;
using HNs_Prog.Dialog;

namespace HNs_Prog
{
    public partial class SliceViewControl : UserControl
    {
        byte[] OutputImageBuffer;

        private void ButtonVesselResponseClick(object sender, EventArgs e)
        {
            VEDialog VesselEnhancementDialog = new VEDialog();
            VesselEnhancementDialog.ShowDialog();

            byte[] CurrentXraySlice = new byte[VolumeData.XNum * VolumeData.YNum];
            for (int i = 0; i < VolumeData.XNum * VolumeData.YNum; i++)
                CurrentXraySlice[i] = Convert.ToByte(VolumeData.VolumeDensity[CurrentSliceIndex * VolumeData.XNum * VolumeData.YNum + i]);
            double[] ResultMap = new double[VolumeData.XNum * VolumeData.YNum];
            ResultMap.Initialize();
            ResponseMap map = new ResponseMap();

            if (VesselEnhancementDialog.MethodIndex == VEDialog.VEMethod.Frangi)
            {
            }
            else if (VesselEnhancementDialog.MethodIndex == VEDialog.VEMethod.KrissianModel)
            {
                const int ScaleNum = 6;
                //double[] ScaleArray = { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 };
                //double[] ScaleArray = { 0.7, 1.4, 2.1, 2.8, 3.5, 4.2 };
                double[] ScaleArray = { 1.0, 1.28, 1.65, 2.12, 2.72, 3.5 };

                ResultMap = map.RunKrissianMethod2D(VolumeData.XNum, VolumeData.YNum, CurrentXraySlice, ScaleNum, ScaleArray);
            }

            VolumeData.VolumeMask = new byte[VolumeData.XNum * VolumeData.YNum];
            VolumeData.VolumeMask.Initialize();
            for (int i = 0; i < VolumeData.XNum * VolumeData.YNum; i++)
            {
                if (ResultMap[i] > 0.25)
                    VolumeData.VolumeMask[i] = 0xff;
            }

            for (int i = 0; i < VolumeData.XNum * VolumeData.YNum; i++)
                CurrentXraySlice[i] = Convert.ToByte(ResultMap[i] * 255.0);
            UpdateTextureOutput(VolumeData.VolumeMask);
            this.PanelOutputImage.Invalidate();
        }

        private void ButtonDICOMSaveClick(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowser.ShowNewFolderButton = true;
            DialogResult result = folderBrowser.ShowDialog();
            if (result == DialogResult.OK)
            {
                ProgressWindow winProgress = new ProgressWindow("DICOM Files Saving...", 0, VolumeData.DicomFileList.Count);
                winProgress.Show();

                for (int i = 0; i < VolumeData.DicomFileList.Count; i++)
                {
                    int FileNamePosition = VolumeData.DicomFileList[i].LastIndexOf("\\") + 1;
                    String FileName = VolumeData.DicomFileList[i].Substring(FileNamePosition);
                    File.Copy(VolumeData.DicomFileList[i], folderBrowser.SelectedPath + "\\" + FileName);
                }

                for (int i = 0; i < VolumeData.DicomFileList.Count; i++)
                {
                    byte[] BufByte = new byte[VolumeData.XNum * VolumeData.YNum * 2];
                    for (int j = 0; j < VolumeData.XNum * VolumeData.YNum; j++)
                    {
                        ushort CurrentDensity = VolumeData.VolumeDensity[(VolumeData.ZNum - 1 - i) * VolumeData.XNum * VolumeData.YNum + j];
                        BufByte[j * 2] = Convert.ToByte(CurrentDensity % 256);
                        BufByte[j * 2 + 1] = Convert.ToByte(CurrentDensity >> 8);
                    }
                    int FileNamePosition = VolumeData.DicomFileList[i].LastIndexOf("\\") + 1;
                    String FileName = VolumeData.DicomFileList[i].Substring(FileNamePosition);
                    String NewFileName = folderBrowser.SelectedPath + "\\" + FileName;

                    DicomDecoder decoder = new DicomDecoder();
                    if (decoder.ScanDicomFile(NewFileName))
                    {
                        BinaryWriter binWriter = new BinaryWriter(File.Open(NewFileName, FileMode.Open));
                        binWriter.Seek(decoder.location, SeekOrigin.Begin);
                        binWriter.Write(BufByte);
                        binWriter.Close();
                    }
                    winProgress.Increment(1);
                }
                winProgress.Close();
            }
        }

        private void ButtonRawFileSaveClick(object sender, EventArgs e)
        {
            SaveFileDialog SaveFileDialogOutput = new SaveFileDialog();
            SaveFileDialogOutput.DefaultExt = "raw";
            SaveFileDialogOutput.Filter = "Raw files (*.raw)|*.raw|Binary files (*.bin)|*.bin|All files (*.*)|*.*";
            if (SaveFileDialogOutput.ShowDialog() == DialogResult.OK)
                VolumeData.SaveMaskVolume(SaveFileDialogOutput.FileName);
        }

        private void ButtonRawFileOpenClick(object sender, EventArgs e)
        {
            OpenFileDialog FileDlg = new OpenFileDialog();
            FileDlg.Filter = "Raw files (*.raw)|*.raw|Binary files (*.bin)|*.bin|All files (*.*)|*.*";

            if (FileDlg.ShowDialog() == DialogResult.OK)
            {
                VolumeData.OpenMaskVolume(FileDlg.FileName);
                this.CheckBoxMasking.Enabled = true;
                this.CheckBoxMasking.Checked = true;
            }
        }
    }
}
