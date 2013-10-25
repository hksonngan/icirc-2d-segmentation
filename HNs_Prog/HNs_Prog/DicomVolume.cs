using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HNs_Prog
{
    class DicomVolume
    {
        public DicomVolume(String FileName, DicomDecoder decoder, string[] strPara)
        {
            XNum = decoder.width;
            YNum = decoder.height;
            ZNum = decoder.nImages;
            XSize = decoder.pixelWidth;
            YSize = decoder.pixelHeight;
            ZSize = decoder.pixelDepth;

            StudyID = strPara[0];
            SeriesNumber = strPara[1];
            StudyDate = strPara[2];
            StudyDesc = strPara[3];
            SeriesDesc = strPara[4];
            PatientName = strPara[5];
            PatientBirthDate = strPara[6];
            PatientSex = strPara[7];

            FileList = new List<String>();
            FileList.Add(FileName);
        }

        public void AddNextSlice(String FileName)
        {
            FileList.Add(FileName);
        }

        public Volume ImportVolume()
        {
            Volume NewVolume = new Volume();
            NewVolume.XNum = XNum;
            NewVolume.YNum = YNum;
            NewVolume.ZNum = ZNum;
            NewVolume.XSize = XSize;
            NewVolume.YSize = YSize;
            NewVolume.ZSize = ZSize;
            NewVolume.VolumeDensity = new ushort[XNum * YNum * ZNum];

            ProgressWindow winProgress = new ProgressWindow("Volume Importing", 0, ZNum);
            winProgress.Show();
            for (int i = 0; i < FileList.Count; i++)
            {
                DicomDecoder decoder = new DicomDecoder();
                decoder.ImportDicomFile(FileList[FileList.Count - 1 - i]);
                if (decoder.bitsAllocated == 8)
                {
                    List<byte> ListBuffer = new List<byte>();
                    ushort[] ArrayBuffer = new ushort[XNum * YNum * ZNum];
                    decoder.GetPixels8(ref ListBuffer);
                    for (int j = 0; j < XNum * YNum * ZNum; j++)
                        ArrayBuffer[j] = (ushort)ListBuffer[j];
                    ArrayBuffer.CopyTo(NewVolume.VolumeDensity, i * XNum * YNum * ZNum);
                }
                else if (decoder.bitsAllocated == 16)
                {
                    List<ushort> ListBuffer = new List<ushort>();
                    ushort[] ArrayBuffer = new ushort[XNum * YNum];
                    decoder.GetPixels16(ref ListBuffer);
                    ArrayBuffer = ListBuffer.ToArray();
                    ArrayBuffer.CopyTo(NewVolume.VolumeDensity, i * XNum * YNum);
                }
                NewVolume.DicomFileList.Add(FileList[i]);
                winProgress.Increment(1);
            }
            winProgress.Close();
            return NewVolume;
        }

        public int XNum, YNum, ZNum;
        public double XSize, YSize, ZSize;

        public string StudyID, SeriesNumber, StudyDate, StudyDesc, SeriesDesc;
        public string PatientName, PatientBirthDate, PatientSex;
        public List<String> FileList;
    }
}
