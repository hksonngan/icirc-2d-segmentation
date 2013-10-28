using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace HNs_Prog
{
    public enum HistogramMode { Volume = 0, Axial, Coronal, Sagittal };
    public enum LUTMode { Gray, Rainbow, Hot, Cool };

    public partial class MPRViewControl : UserControl
    {
        Size TextureHistogramSize, TextureLUTSize;

        private Device GlobalDevice;
        private Texture TextureAxial, TextureCoronal, TextureSagittal;
        private Texture TextureHistogram, TextureLUT;
        private SwapChain SwapChainAxial, SwapChainCoronal, SwapChainSagittal;
        private SwapChain SwapChainHistogram, SwapChainLUT;

        Volume VolumeData;
        int MinDensity, MaxDensity, BinSize, BinNum; 
        int CurrentXIndex, CurrentYIndex, CurrentZIndex;
        private int[] DensityAxail, DensityCoronal, DensitySagittal;

        List<Color> ColorLUT;
        LUTMode CurrentLUTMode;

        Histogram[] HistogramList;
        HistogramMode CurrentHistogramMode;

        WindowingFunction WindowFunction;

        // For drawing
        Point[] ControlPoints;

        public MPRViewControl(ref Volume refVolumeData)
        {
            InitializeComponent();
            TextureHistogramSize = new Size(512, 512);
            TextureLUTSize = new Size(16, 256);

            VolumeData = refVolumeData;

            CurrentXIndex = VolumeData.XNum / 2;
            CurrentYIndex = VolumeData.YNum / 2;
            CurrentZIndex = VolumeData.ZNum / 2;
            MinDensity = Convert.ToInt32(ushort.MaxValue);
            MaxDensity = Convert.ToInt32(ushort.MinValue);
            int TotalNum = VolumeData.XNum * VolumeData.YNum * VolumeData.ZNum;
            for (int i = 0; i < TotalNum; i++)
            {
                MinDensity = Math.Min(MinDensity, Convert.ToInt32(VolumeData.VolumeDensity[i]));
                MaxDensity = Math.Max(MaxDensity, Convert.ToInt32(VolumeData.VolumeDensity[i]));
            }
            BinNum = this.PanelMPRHistogram.Width - 2;//TextureHistogramSize.Width;
            BinSize = (MaxDensity - MinDensity) / BinNum + 1;

            HistogramList = new Histogram[4];
            for (int i = 0; i < 4; i++)
                HistogramList[i] = new Histogram(MinDensity, MaxDensity, BinNum);
            HistogramList[Convert.ToInt32(HistogramMode.Volume)].SetVolume(VolumeData, HistogramMode.Volume, 0);
            HistogramList[Convert.ToInt32(HistogramMode.Axial)].SetVolume(VolumeData, HistogramMode.Axial, CurrentZIndex);
            HistogramList[Convert.ToInt32(HistogramMode.Coronal)].SetVolume(VolumeData, HistogramMode.Coronal, CurrentYIndex);
            HistogramList[Convert.ToInt32(HistogramMode.Sagittal)].SetVolume(VolumeData, HistogramMode.Sagittal, CurrentXIndex);
            CurrentHistogramMode = HistogramMode.Volume;


            CurrentLUTMode = LUTMode.Gray;
            ColorLUT = new List<Color>();
            UpdateLookUpTable();

            WindowFunction = new WindowingFunction(MinDensity, MinDensity + BinSize * BinNum - 1);

            ControlPoints = new Point[4];
            ControlPoints[0] = new Point(1, this.PanelMPRHistogram.Height - 2);
            int BinIndex = (WindowFunction.ControlPointDensity[1] - MinDensity) / BinSize;
            ControlPoints[1] = new Point(1 + BinIndex * (this.PanelMPRHistogram.Width - 2) / BinNum, this.PanelMPRHistogram.Height - 2);
            BinIndex = (WindowFunction.ControlPointDensity[2] - MinDensity) / BinSize;
            ControlPoints[2] = new Point(1 + BinIndex * (this.PanelMPRHistogram.Width - 2) / BinNum, 1);
            ControlPoints[3] = new Point(this.PanelMPRHistogram.Width - 2, 1);

            InitializeDevice();

            UpdateDensityAxial();
            UpdateDensityCoronal();
            UpdateDensitySagittal();

            UpdateAxialImage();
            UpdateCoronalImage();
            UpdateSagittalImage();
            UpdateHistogramImage();
            UpdateLUTImage();
            LabelLUTPaint();

            PanelMPRHistogram.Invalidate();
            PanelMPRAxial.Invalidate();
            PanelMPRCoronal.Invalidate();
            PanelMPRSagittal.Invalidate();
            PanelMPRLUT.Invalidate();

            PanelMPRHistogramMouseClicked = false;
            ClickedControlPointIndex = -1;
        }

        public void InitializeDevice()
        {           
            PresentParameters presentParams = new PresentParameters();
            presentParams.Windowed = true;
            presentParams.SwapEffect = SwapEffect.Copy;
            presentParams.BackBufferCount = 1;
            presentParams.BackBufferFormat = Format.X8R8G8B8;
            presentParams.EnableAutoDepthStencil = false;
            presentParams.PresentationInterval = PresentInterval.Immediate;
            presentParams.DeviceWindow = this;
            GlobalDevice = new Device(0, DeviceType.Hardware, this, CreateFlags.HardwareVertexProcessing, presentParams);

            presentParams.DeviceWindow = this.PanelMPRAxial;
            presentParams.BackBufferWidth = this.PanelMPRAxial.Width;
            presentParams.BackBufferHeight = this.PanelMPRAxial.Height;
            SwapChainAxial = new SwapChain(GlobalDevice, presentParams);
            presentParams.DeviceWindow = this.PanelMPRCoronal;
            SwapChainCoronal = new SwapChain(GlobalDevice, presentParams);
            presentParams.DeviceWindow = this.PanelMPRSagittal;
            SwapChainSagittal = new SwapChain(GlobalDevice, presentParams);
            presentParams.DeviceWindow = this.PanelMPRHistogram;
            presentParams.BackBufferWidth = this.PanelMPRHistogram.Width;
            presentParams.BackBufferHeight = this.PanelMPRHistogram.Height;
            SwapChainHistogram = new SwapChain(GlobalDevice, presentParams);
            presentParams.DeviceWindow = this.PanelMPRLUT;
            presentParams.BackBufferWidth = this.PanelMPRLUT.Width;
            presentParams.BackBufferHeight = this.PanelMPRLUT.Height;
            SwapChainLUT = new SwapChain(GlobalDevice, presentParams);

            TextureAxial = new Texture(GlobalDevice, VolumeData.XNum, VolumeData.YNum, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            TextureCoronal = new Texture(GlobalDevice, VolumeData.XNum, VolumeData.ZNum, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            TextureSagittal = new Texture(GlobalDevice, VolumeData.YNum, VolumeData.ZNum, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            TextureHistogram = new Texture(GlobalDevice, TextureHistogramSize.Width, TextureHistogramSize.Height, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            TextureLUT = new Texture(GlobalDevice, TextureLUTSize.Width, TextureLUTSize.Height, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
        }

        private void UpdateDensityAxial()
        {
            DensityAxail = new int[VolumeData.YNum * VolumeData.XNum];
            int idxTempY, idxTempZ;
            idxTempZ = CurrentZIndex * VolumeData.XNum * VolumeData.YNum;
            for (int y = 0; y < VolumeData.YNum; y++)
            {
                idxTempY = y * VolumeData.XNum;
                for (int x = 0; x < VolumeData.XNum; x++)
                    DensityAxail[idxTempY + x] = Convert.ToInt32(VolumeData.VolumeDensity[idxTempZ + idxTempY + x]);
            }
        }

        private void UpdateDensityCoronal()
        {
            DensityCoronal = new int[VolumeData.ZNum * VolumeData.XNum];
            int idxTempY, idxTempZ;
            idxTempY = CurrentYIndex * VolumeData.XNum;
            for (int z = 0; z < VolumeData.ZNum; z++)
            {
                idxTempZ = z * VolumeData.XNum * VolumeData.YNum;
                for (int x = 0; x < VolumeData.XNum; x++)
                    DensityCoronal[z * VolumeData.XNum + x]  = Convert.ToInt32(VolumeData.VolumeDensity[idxTempZ + idxTempY + x]);
            }
        }

        private void UpdateDensitySagittal()
        {
            DensitySagittal = new int[VolumeData.ZNum * VolumeData.YNum];
            int idxTempY, idxTempZ;
            idxTempY = CurrentYIndex * VolumeData.XNum;
            for (int z = 0; z < VolumeData.ZNum; z++)
            {
                idxTempZ = z * VolumeData.XNum * VolumeData.YNum;
                for (int y = 0; y < VolumeData.YNum; y++)
                    DensitySagittal[z * VolumeData.YNum + y] = Convert.ToInt32(VolumeData.VolumeDensity[idxTempZ + y * VolumeData.XNum + CurrentXIndex]);
            }
        }

        private void UpdateAxialImage()
        {
            int CurrentIntensity;
            uint[] PixelArray = (uint[])TextureAxial.LockRectangle(typeof(uint), 0, LockFlags.Discard, VolumeData.YNum * VolumeData.XNum);
            for (int i = 0; i < VolumeData.YNum * VolumeData.XNum; i++)
            {
                CurrentIntensity = WindowFunction.ConvertDensityToIntensity(DensityAxail[i]);
                PixelArray[i] = (uint)ColorLUT[CurrentIntensity].ToArgb();
            }
            TextureAxial.UnlockRectangle(0);
        }

        private void UpdateCoronalImage()
        {
            int CurrentIntensity;
            uint[] PixelArray = (uint[])TextureCoronal.LockRectangle(typeof(uint), 0, LockFlags.Discard, VolumeData.ZNum * VolumeData.XNum);
            for (int i = 0; i < VolumeData.ZNum * VolumeData.XNum; i++)
            {
                CurrentIntensity = WindowFunction.ConvertDensityToIntensity(DensityCoronal[i]);
                PixelArray[i] = (uint)ColorLUT[CurrentIntensity].ToArgb();
            }
            TextureCoronal.UnlockRectangle(0);
        }

        private void UpdateSagittalImage()
        {
            int CurrentIntensity;
            uint[] PixelArray = (uint[])TextureSagittal.LockRectangle(typeof(uint), 0, LockFlags.Discard, VolumeData.ZNum * VolumeData.YNum);
            for (int i = 0; i < VolumeData.ZNum * VolumeData.YNum; i++)
            {
                CurrentIntensity = WindowFunction.ConvertDensityToIntensity(DensitySagittal[i]);
                PixelArray[i] = (uint)ColorLUT[CurrentIntensity].ToArgb();
            }
            TextureSagittal.UnlockRectangle(0);
        }

        private void UpdateHistogramImage()
        {
            double MaxCountBinLog, CurrentBinLog;
            Histogram CurrentHistogram = HistogramList[(int)CurrentHistogramMode];
            int CurrentBinCount, CurrentBinHeight;
            int CurrentBinDensity, CurrentBinIntensity;
            MaxCountBinLog = Math.Log(Convert.ToDouble(CurrentHistogram.HistogramData[CurrentHistogram.MaxCountBinIndex]), 2.0);

            uint[] PixelArray = (uint[])TextureHistogram.LockRectangle(typeof(uint), 0, LockFlags.Discard, TextureHistogramSize.Width * TextureHistogramSize.Height);
            for (int x = 0; x < this.PanelMPRHistogram.Width - 2; x++)
            {
                CurrentBinCount = CurrentHistogram.HistogramData[x];
                if (CurrentBinCount == 0)
                    CurrentBinHeight = 0;
                else if (CurrentBinCount == 1)
                    CurrentBinHeight = Convert.ToInt32(Convert.ToDouble(this.PanelMPRHistogram.Height - 2) / MaxCountBinLog  / 2.0 + 0.5);
                else
                {
                    CurrentBinLog = Math.Log(Convert.ToDouble(CurrentBinCount), 2.0);
                    CurrentBinHeight = Convert.ToInt32(CurrentBinLog * Convert.ToDouble(this.PanelMPRHistogram.Height - 2) / MaxCountBinLog + 0.5);
                }

                CurrentBinDensity = MinDensity + BinSize * x + BinSize/2;
                CurrentBinIntensity = WindowFunction.ConvertDensityToIntensity(CurrentBinDensity);

                for (int y = 0; y < CurrentBinHeight; y++)
                    PixelArray[(this.PanelMPRHistogram.Height - 2 - 1 - y) * TextureHistogramSize.Width + x] = (uint)ColorLUT[CurrentBinIntensity].ToArgb();
                for (int y = CurrentBinHeight; y < this.PanelMPRHistogram.Height - 2; y++)
                    PixelArray[(this.PanelMPRHistogram.Height - 2 - 1 - y) * TextureHistogramSize.Width + x] = (uint)Color.DeepSkyBlue.ToArgb();
            }
            TextureHistogram.UnlockRectangle(0);
        }

        private void UpdateLUTImage()
        {
            uint[] PixelArray = (uint[])TextureLUT.LockRectangle(typeof(uint), 0, LockFlags.Discard, TextureLUTSize.Width * TextureLUTSize.Height);
            for (int i = 0; i < TextureLUTSize.Height; i++)
            {
                uint CurrentColor = (uint)ColorLUT[TextureLUTSize.Height - 1 - i].ToArgb();
                for (int j = 0; j < TextureLUTSize.Width; j++)
                    PixelArray[i * TextureLUTSize.Width + j] = CurrentColor;
            }
            TextureLUT.UnlockRectangle(0);
        }

        private void UpdateLookUpTable()
        {
            ColorLUT.Clear();
            switch (CurrentLUTMode)
            {
            case LUTMode.Gray:
                for (int i = 0; i < 256; i++)
                    ColorLUT.Add(Color.FromArgb(i, i, i));
                ColorLUT.Add(Color.Red);
                break;
            }
        }

        private void panelAxialImagePaint(object sender, PaintEventArgs e)
        {
            Surface tempSurface = SwapChainAxial.GetBackBuffer(0, BackBufferType.Mono);
            
            GlobalDevice.SetRenderTarget(0, tempSurface);
            GlobalDevice.SetTexture(0, TextureAxial);

            GlobalDevice.BeginScene();
            using (Sprite s = new Sprite(GlobalDevice))
            {
                s.Begin(SpriteFlags.None);
                s.Draw2D(TextureAxial, new Rectangle(0, 0, VolumeData.XNum, VolumeData.YNum), new Rectangle(0, 0, 478, 478), new Point(0, 0), Color.White);
                s.End();
            }
            GlobalDevice.EndScene();
            SwapChainAxial.Present();
        }

        private void panelCoronalImagePaint(object sender, PaintEventArgs e)
        {
            Surface tempSurface = SwapChainCoronal.GetBackBuffer(0, BackBufferType.Mono);
            GlobalDevice.SetRenderTarget(0, tempSurface);

            GlobalDevice.BeginScene();
            GlobalDevice.Clear(ClearFlags.Target, Color.Blue, 0f, 0);
            using (Sprite s = new Sprite(GlobalDevice))
            {
                s.Begin(SpriteFlags.None);
                s.Draw2D(TextureCoronal, new Rectangle(0, 0, VolumeData.XNum, VolumeData.ZNum), new Rectangle(0, 0, 478, 478), new Point(0, 0), Color.White);
                s.End();
            }
            GlobalDevice.EndScene();
            SwapChainCoronal.Present();
        }

        private void panelSagittalImagePaint(object sender, PaintEventArgs e)
        {
            Surface tempSurface = SwapChainSagittal.GetBackBuffer(0, BackBufferType.Mono);
            GlobalDevice.SetRenderTarget(0, tempSurface);

            GlobalDevice.BeginScene();
            GlobalDevice.Clear(ClearFlags.Target, Color.Blue, 0f, 0);
            using (Sprite s = new Sprite(GlobalDevice))
            {
                s.Begin(SpriteFlags.None);
                s.Draw2D(TextureSagittal, new Rectangle(0, 0, VolumeData.YNum, VolumeData.ZNum), new Rectangle(0, 0, 478, 478), new Point(0, 0), Color.White);
                s.End();
            }
            GlobalDevice.EndScene();
            SwapChainSagittal.Present();
        }

        private void panelHistogramPaint(object sender, PaintEventArgs e)
        {
            Surface tempSurface = SwapChainHistogram.GetBackBuffer(0, BackBufferType.Mono);
            GlobalDevice.SetRenderTarget(0, tempSurface);

            GlobalDevice.BeginScene();
            GlobalDevice.Clear(ClearFlags.Target, Color.Blue, 0f, 0);
            using (Sprite s = new Sprite(GlobalDevice))
            {
                s.Begin(SpriteFlags.None);
                s.Draw2D(TextureHistogram, new Rectangle(0, 0, this.PanelMPRHistogram.Width - 2, this.PanelMPRHistogram.Height - 2),
                    new Rectangle(0, 0, this.PanelMPRHistogram.Width - 2, this.PanelMPRHistogram.Height - 2), new Point(1, 1), Color.White);
                s.End();
            }
            GlobalDevice.EndScene();
            SwapChainHistogram.Present();

            Graphics g = e.Graphics;
            g.DrawLines(new Pen(Color.Yellow, 1), ControlPoints);
            for (int i = 0; i < 4; i++)
                g.FillRectangle(new SolidBrush(Color.Red), ControlPoints[i].X - 1, ControlPoints[i].Y - 1, 3, 3);
        }

        private void panelLUTPaint(object sender, PaintEventArgs e)
        {
            Surface tempSurface = SwapChainLUT.GetBackBuffer(0, BackBufferType.Mono);
            GlobalDevice.SetRenderTarget(0, tempSurface);

            GlobalDevice.BeginScene();
            GlobalDevice.Clear(ClearFlags.Target, Color.Blue, 0f, 0);
            using (Sprite s = new Sprite(GlobalDevice))
            {
                s.Begin(SpriteFlags.None);
                s.Draw2D(TextureLUT, new Rectangle(0, 0, TextureLUTSize.Width, TextureLUTSize.Height), 
                    new Rectangle(0, 0, this.PanelMPRLUT.Width - 2, this.PanelMPRLUT.Height - 2), new Point(1, 1), Color.White);
                s.End();
            }
            GlobalDevice.EndScene();
            SwapChainLUT.Present();
        }

        private void RadioButtonHistogramCheckedChanged(object sender, EventArgs e)
        {
            if (RadioButtonHistogramVolume.Checked)
                CurrentHistogramMode = HistogramMode.Volume;
            else if (RadioButtonHistogramAxial.Checked)
                CurrentHistogramMode = HistogramMode.Axial;
            else if (RadioButtonHistogramCoronal.Checked)
                CurrentHistogramMode = HistogramMode.Coronal;
            else if (RadioButtonHistogramSagittal.Checked)
                CurrentHistogramMode = HistogramMode.Sagittal;

            UpdateHistogramImage();
            PanelMPRHistogram.Invalidate();
        }

        private void RadioButtonWindowingCheckedChanged(object sender, EventArgs e)
        {
            if (RadioButtonWindowBasic.Checked)
            {
                ControlPoints = new Point[4];
                ControlPoints[0] = new Point(1, this.PanelMPRHistogram.Height - 2);
                int BinIndex = (WindowFunction.ControlPointDensity[1] - MinDensity) / BinSize;
                ControlPoints[1] = new Point(1 + BinIndex * (this.PanelMPRHistogram.Width - 2) / BinNum, this.PanelMPRHistogram.Height - 2);
                BinIndex = (WindowFunction.ControlPointDensity[2] - MinDensity) / BinSize;
                ControlPoints[2] = new Point(1 + BinIndex * (this.PanelMPRHistogram.Width - 2) / BinNum, 1);
                ControlPoints[3] = new Point(this.PanelMPRHistogram.Width - 2, 1);
            }
            else if (RadioButtonWindowThresholding.Checked)
            {
                ControlPoints = new Point[6];
                ControlPoints[0] = new Point(1, this.PanelMPRHistogram.Height - 2);
                int BinIndex = (WindowFunction.ControlPointDensity[1] - MinDensity) / BinSize;
                ControlPoints[1] = new Point(1 + BinIndex * (this.PanelMPRHistogram.Width - 2) / BinNum,
                                                this.PanelMPRHistogram.Height - 2 - WindowFunction.ControlPointIntensity[1] * (this.PanelMPRHistogram.Height - 2) / 256);
                ControlPoints[2] = new Point(1 + BinIndex * (this.PanelMPRHistogram.Width - 2) / BinNum, 1);
                BinIndex = (WindowFunction.ControlPointDensity[2] - MinDensity) / BinSize;
                ControlPoints[3] = new Point(1 + BinIndex * (this.PanelMPRHistogram.Width - 2) / BinNum, 1);
                ControlPoints[4] = new Point(1 + BinIndex * (this.PanelMPRHistogram.Width - 2) / BinNum, 
                                                this.PanelMPRHistogram.Height - 2 - WindowFunction.ControlPointIntensity[2] * (this.PanelMPRHistogram.Height - 2) / 256);
                BinIndex = (WindowFunction.ControlPointDensity[3] - MinDensity) / BinSize;
                ControlPoints[5] = new Point(this.PanelMPRHistogram.Width - 2, 1);
            }

            WindowFunction = new WindowingFunction(MinDensity, MinDensity + BinSize * BinNum - 1);
            UpdateHistogramImage();
            PanelMPRHistogram.Invalidate();

            UpdateAxialImage();
            UpdateCoronalImage();
            UpdateSagittalImage();
            PanelMPRAxial.Invalidate();
            PanelMPRCoronal.Invalidate();
            PanelMPRSagittal.Invalidate();
        }

        private void panelHistogramMouseDown(object sender, MouseEventArgs e)
        {
            if (e.X < 0 || e.X > this.PanelMPRHistogram.Width - 1 || e.Y < 0 || e.Y > this.PanelMPRHistogram.Height - 1)
                return;

            if (Math.Abs(e.X - ControlPoints[1].X) < 4 && Math.Abs(e.Y - ControlPoints[1].Y) < 4)
            {
                ClickedControlPointIndex = 1;
                PanelMPRHistogramMouseClicked = true;
            }
            if (Math.Abs(e.X - ControlPoints[2].X) < 4 && Math.Abs(e.Y - ControlPoints[2].Y) < 4)
            {
                ClickedControlPointIndex = 2;
                PanelMPRHistogramMouseClicked = true;
            }
        }

        private void panelHistogramMoseUp(object sender, MouseEventArgs e)
        {
            //if (e.X < 0 || e.X > 255 || e.Y < 0 || e.Y > 255)
            //    return;

            ClickedControlPointIndex = -1;
            PanelMPRHistogramMouseClicked = false;
        }

        private void panelHistogramMouseMove(object sender, MouseEventArgs e)
        {
            if (e.X < 0 || e.X > this.PanelMPRHistogram.Width - 1 || e.Y < 0 || e.Y > this.PanelMPRHistogram.Height - 1)
                return;

            if (PanelMPRHistogramMouseClicked)
            {
                ControlPoints[ClickedControlPointIndex].X = e.X;
                WindowFunction.ControlPointDensity[ClickedControlPointIndex] = MinDensity + e.X * BinSize;

                UpdateHistogramImage();
                PanelMPRHistogram.Invalidate();
                UpdateAxialImage();
                UpdateCoronalImage();
                UpdateSagittalImage();
                PanelMPRAxial.Invalidate();
                PanelMPRCoronal.Invalidate();
                PanelMPRSagittal.Invalidate();
            }
        }

        private void LabelLUTPaint()
        {
            LabelLUT2.Hide();
            LabelLUT3.Hide();
        }
    }
}
