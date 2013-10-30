using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HNs_Prog
{
    public class Volume
    {
        public byte[] VolumeMask;
        public ushort[] VolumeDensity;
        public int XNum, YNum, ZNum;
        public double XSize, YSize, ZSize;
        public byte LabelNum;
        public List<String> DicomFileList;

        public Volume()
        {
            VolumeMask = null;
            VolumeDensity = null;
            XNum = 0;
            YNum = 0;
            ZNum = 0;
            XSize = 0.0;
            YSize = 0.0;
            ZSize = 0.0;
            LabelNum = 0x00;

            DicomFileList = new List<string>();
        }

        public bool OpenMaskVolume(String FileName)
        {
            String Ext = FileName.Substring(FileName.Length - 3, 3).ToLower();
            if (Ext.Equals("bin"))
            {
                FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                BinaryReader reader = new BinaryReader(fs);
                byte[] OutputBuffer = new byte[XNum * YNum * ZNum / 8];
                OutputBuffer = reader.ReadBytes(XNum * YNum * ZNum / 8);
                reader.Close();
                fs.Close();

                byte[] FlagOneBit = new byte[8];
                for (int i = 0; i < 8; i++)
                    FlagOneBit[i] = (byte)Math.Pow(2, (double)(7 - i));

                int[] NeighborOffset = new int[6];
                NeighborOffset[0] = -1;
                NeighborOffset[1] = 1;
                NeighborOffset[2] = -XNum;
                NeighborOffset[3] = XNum;
                NeighborOffset[4] = -(XNum * YNum);
                NeighborOffset[5] = XNum * YNum;

                int x, y, z, NeighborIndex;
                byte CurrentEightBit, NeighborEightBit;
                VolumeMask = new byte[XNum * YNum * ZNum];
                VolumeMask.Initialize();
                for (int i = 0; i < XNum * YNum * ZNum; i++)
                {
                    CurrentEightBit = OutputBuffer[i / 8];
                    if ((CurrentEightBit & FlagOneBit[i % 8]) > 0)
                    {
                        z = i / (XNum * YNum);
                        y = (i % (XNum * YNum)) / XNum;
                        x = i % XNum;

                        if (x < 1 || x > XNum - 2 || y < 1 || y > YNum - 2 || z < 1 || z > ZNum - 2)
                            continue;

                        //bool IsEdge = false;
                        for (int j = 0; j < 6; j++)
                        {
                            NeighborIndex = i + NeighborOffset[j];
                            NeighborEightBit = OutputBuffer[NeighborIndex / 8];
                            if ((NeighborEightBit & FlagOneBit[NeighborIndex % 8]) == 0)
                            {
                                VolumeMask[i] = 1;
                                VolumeMask[NeighborIndex] = 1;
                            }
                            //IsEdge = true;
                        }
                        /*
                        if (IsEdge)
                        {
                            for (int j = 0; j < 6; j++)
                                VolumeData.VolumeMask[i + NeighborOffset[j]] = 1;
                        }
                         * */
                    }
                }
            }
            else if (Ext.Equals("raw"))
            {
                FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                BinaryReader reader = new BinaryReader(fs);
                
                VolumeMask = null;
                VolumeMask = new byte[XNum * YNum * ZNum];
                VolumeMask.Initialize();

                int FileSize = Convert.ToInt32(fs.Length);
                byte[] Buffer = new byte[FileSize];
                Buffer = reader.ReadBytes(FileSize);
                for (int i = 0; i < FileSize; i++)
                    VolumeMask[i] = Buffer[i];
                Buffer = null;

                reader.Close();
                fs.Close();
            }
            else
                return false;

            return true;
        }

        public bool SaveMaskVolume(String FileName)
        {
            String Ext = FileName.Substring(FileName.Length - 3, 3).ToLower();
            if (Ext.Equals("bin"))
            {

            }
            else if (Ext.Equals("raw"))
            {
                FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write);
                BinaryWriter writer = new BinaryWriter(fs);
                writer.Write(VolumeMask);
                writer.Close();
                fs.Close();
            }
            else if (Ext.Equals("den"))
            {
                FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write);
                BinaryWriter writer = new BinaryWriter(fs);
                for (int i = 0; i < XNum * YNum * ZNum; i++)
                    writer.Write(VolumeDensity[i]);
                writer.Close();
                fs.Close();
            }
            else
                return false;

            return true;
        }
    }
}
