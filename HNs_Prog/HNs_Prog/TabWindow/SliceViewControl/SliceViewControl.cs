using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using MathNet.Numerics.LinearAlgebra;


namespace HNs_Prog
{
    public partial class SliceViewControl : UserControl
    {
        Volume VolumeData;
        Device GlobalDevice;
        Texture TextureSliceImage, TextureHistogram, TextureLUT, TextureOutput;
        SwapChain SwapChainSliceImage, SwapChainHistogram, SwapChainLUT, SwapChainOutput;
        int MinDensity, MaxDensity, BinSize, BinNum;
        int CurrentSliceIndex;
        int[] DensityCurrentSlice;

        Color[] ColorLUT;
        LUTMode CurrentLUTMode;
        Histogram HistogramVolume, HistogramCurrentSlice;
        HistogramMode CurrentHistogramMode;
        WindowingFunction WindowFunction;

        // For drawing
        Point[] WindowControlPoints;
        Point[] ThresholdControlPoints;
        bool PanelMPRHistogramMouseClicked;
        int ClickedControlPointIndex;
        Point MouseClickPosition;

        public SliceViewControl()
        {
            InitializeComponent();
        }

        public SliceViewControl(ref Volume refVolumeData)
        {
            InitializeComponent();

            VolumeData = refVolumeData;

            // Set LookUpTable(LUT) using gray color map
            CurrentLUTMode = LUTMode.Gray;
            ColorLUT = new Color[257];          // ColorLUT[256] for thresholding color label
            UpdateLookUpTable(Color.Blue);      // ColorLUT[256] = Color.Blue;

            // Find min & max density values
            MinDensity = Convert.ToInt32(ushort.MaxValue);
            MaxDensity = Convert.ToInt32(ushort.MinValue);
            int TotalNum = VolumeData.XNum * VolumeData.YNum * VolumeData.ZNum;
            for (int i = 0; i < TotalNum; i++)
            {
                MinDensity = Math.Min(MinDensity, Convert.ToInt32(VolumeData.VolumeDensity[i]));
                MaxDensity = Math.Max(MaxDensity, Convert.ToInt32(VolumeData.VolumeDensity[i]));
            }
            // Set the number of bins of histogram and the step size of each bin 
            BinNum = this.PanelSliceHistogram.Width - 2;
            BinSize = (MaxDensity + 1 - MinDensity) / BinNum;
            if (BinSize < 1)
                BinSize = 1;

            // Buffer the density of the current slice
            CurrentSliceIndex = VolumeData.ZNum / 2;
            int NonZeroSliceIndex = CurrentSliceIndex + 1;
            this.LabelCurrentSlice.Text = NonZeroSliceIndex.ToString() + " / " + VolumeData.ZNum.ToString();
            DensityCurrentSlice = new int[VolumeData.YNum * VolumeData.XNum];
            UpdateSliceDensity();

            // Build the basic windowing
            WindowFunction = new WindowingFunction(MinDensity, MinDensity + BinSize * BinNum - 1);

            WindowControlPoints = new Point[4];
            ThresholdControlPoints = new Point[4];
            WindowControlPoints[0] = new Point(1, this.PanelSliceHistogram.Height - 2);
            int BinIndex = (WindowFunction.ControlPointDensity[1] - MinDensity) / BinSize;
            WindowControlPoints[1] = new Point(1 + BinIndex * (this.PanelSliceHistogram.Width - 2) / BinNum, this.PanelSliceHistogram.Height - 2);
            BinIndex = (WindowFunction.ControlPointDensity[2] - MinDensity) / BinSize;
            WindowControlPoints[2] = new Point(1 + BinIndex * (this.PanelSliceHistogram.Width - 2) / BinNum, 1);
            WindowControlPoints[3] = new Point(this.PanelSliceHistogram.Width - 2, 1);

            int TempLeft = WindowControlPoints[1].X;
            int TempRight = WindowControlPoints[2].X;
            int TempDeltaX = (TempRight - TempLeft) / 3;
            ThresholdControlPoints[0] = new Point(TempLeft + TempDeltaX, this.PanelSliceHistogram.Height - 2);
            ThresholdControlPoints[1] = new Point(TempLeft + TempDeltaX, 1);
            ThresholdControlPoints[2] = new Point(TempRight - TempDeltaX, 1);
            ThresholdControlPoints[3] = new Point(TempRight - TempDeltaX, this.PanelSliceHistogram.Height - 2);
            int MinThreshDensity = (TempLeft + TempDeltaX - 1) * BinSize + BinSize / 2;
            int MaxThreshDensity = (TempRight - TempDeltaX - 1) * BinSize + BinSize / 2;
            WindowFunction.SetThresholingRange(MinThreshDensity, MaxThreshDensity);

            // Build the histograms 
            CurrentHistogramMode = HistogramMode.Volume;
            HistogramVolume = new Histogram(MinDensity, MaxDensity, BinNum);
            HistogramVolume.SetVolume(VolumeData, HistogramMode.Volume, 0);
            HistogramCurrentSlice = new Histogram(MinDensity, MaxDensity, BinNum);
            HistogramCurrentSlice.SetVolume(VolumeData, HistogramMode.Axial, CurrentSliceIndex);

            InitializeDevice();

            PanelMPRHistogramMouseClicked = false;
            ClickedControlPointIndex = -1;
            this.TrackBarSliceImage.Maximum = VolumeData.ZNum;
            this.TrackBarSliceImage.TickFrequency = VolumeData.ZNum / 16;
            this.TrackBarSliceImage.Value = CurrentSliceIndex;

            UpdateTextureLUT();
            UpdateTextureHistogram();
            UpdateTextureSliceImage();
            this.PanelSliceImage.Invalidate();
            this.PanelSliceLUT.Invalidate();
            this.PanelSliceHistogram.Invalidate();

            byte[] OutputBuffer = new byte[512*512];
            OutputBuffer.Initialize();
            UpdateTextureOutput(OutputBuffer);
            this.PanelOutputImage.Invalidate();
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

            presentParams.DeviceWindow = this.PanelSliceImage;
            presentParams.BackBufferWidth = this.PanelSliceImage.Width;
            presentParams.BackBufferHeight = this.PanelSliceImage.Height;
            SwapChainSliceImage = new SwapChain(GlobalDevice, presentParams);
            presentParams.DeviceWindow = this.PanelSliceHistogram;
            presentParams.BackBufferWidth = this.PanelSliceHistogram.Width;
            presentParams.BackBufferHeight = this.PanelSliceHistogram.Height;
            SwapChainHistogram = new SwapChain(GlobalDevice, presentParams);
            presentParams.DeviceWindow = this.PanelSliceLUT;
            presentParams.BackBufferWidth = this.PanelSliceLUT.Width;
            presentParams.BackBufferHeight = this.PanelSliceLUT.Height;
            SwapChainLUT = new SwapChain(GlobalDevice, presentParams);
            presentParams.DeviceWindow = this.PanelOutputImage;
            presentParams.BackBufferWidth = this.PanelOutputImage.Width;
            presentParams.BackBufferHeight = this.PanelOutputImage.Height;
            SwapChainOutput = new SwapChain(GlobalDevice, presentParams);

            TextureSliceImage = new Texture(GlobalDevice, VolumeData.XNum, VolumeData.YNum, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            TextureHistogram = new Texture(GlobalDevice, this.PanelSliceHistogram.Width - 2, this.PanelSliceHistogram.Height - 2, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            TextureLUT = new Texture(GlobalDevice, this.PanelSliceLUT.Width - 2, this.PanelSliceLUT.Height - 2, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
            TextureOutput = new Texture(GlobalDevice, VolumeData.XNum, VolumeData.YNum, 1, Usage.Dynamic, Format.X8R8G8B8, Pool.Default);
        }

        private void UpdateSliceDensity()
        {
            int idxTempY, idxTempZ;
            idxTempZ = CurrentSliceIndex * VolumeData.XNum * VolumeData.YNum;
            for (int y = 0; y < VolumeData.YNum; y++)
            {
                idxTempY = y * VolumeData.XNum;
                for (int x = 0; x < VolumeData.XNum; x++)
                    DensityCurrentSlice[idxTempY + x] = Convert.ToInt32(VolumeData.VolumeDensity[idxTempZ + idxTempY + x]);
            }
        }

        private void UpdateLookUpTable(Color ThresholdingLabel)
        {
            switch (CurrentLUTMode)
            {
                case LUTMode.Gray:
                    for (int i = 0; i < 256; i++)
                        ColorLUT[i] = Color.FromArgb(i, i, i);
                    break;
            }
            ColorLUT[256] = ThresholdingLabel;
        }

        private void UpdateTextureSliceImage()
        {
            int CurrentIntensity;
            uint[] PixelArray = (uint[])TextureSliceImage.LockRectangle(typeof(uint), 0, LockFlags.Discard, VolumeData.YNum * VolumeData.XNum);
            for (int i = 0; i < VolumeData.YNum * VolumeData.XNum; i++)
            {
                CurrentIntensity = WindowFunction.ConvertDensityToIntensity(DensityCurrentSlice[i]);
                PixelArray[i] = (uint)ColorLUT[CurrentIntensity].ToArgb();
            }
            if (CheckBoxMasking.Checked)
            {
                int CurrentSliceOffset = CurrentSliceIndex * VolumeData.YNum * VolumeData.XNum;
                for (int i = 0; i < VolumeData.YNum * VolumeData.XNum; i++)
                {
                    if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x01)
                        PixelArray[i] = (uint)Color.Blue.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x02)
                        PixelArray[i] = (uint)Color.Green.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x03)
                        PixelArray[i] = (uint)Color.Yellow.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x04)
                        PixelArray[i] = (uint)Color.Red.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x05)
                            PixelArray[i] = (uint)Color.Red.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0xff)
                        PixelArray[i] = (uint)Color.Yellow.ToArgb();
                }
            }
            TextureSliceImage.UnlockRectangle(0);
        }

        private void UpdateTextureLUT()
        {
            int TextureWidth = this.PanelSliceLUT.Width - 2;
            int TextureHeight = this.PanelSliceLUT.Height - 2;
            uint[] PixelArray = (uint[])TextureLUT.LockRectangle(typeof(uint), 0, LockFlags.Discard, TextureWidth * TextureHeight);
            for (int i = 0; i < TextureHeight; i++)
            {
                uint CurrentColor = (uint)ColorLUT[TextureHeight - 1 - i].ToArgb();
                for (int j = 0; j < TextureWidth; j++)
                    PixelArray[i * TextureWidth + j] = CurrentColor;
            }
            TextureLUT.UnlockRectangle(0);
        }

        private void UpdateTextureHistogram()
        {
            int TextureWidth = this.PanelSliceHistogram.Width - 2;
            int TextureHeight = this.PanelSliceHistogram.Height - 2;

            Histogram CurrentHistogram;
            if(CurrentHistogramMode==HistogramMode.Volume)
                CurrentHistogram = HistogramVolume;
            else
                CurrentHistogram = HistogramCurrentSlice;
            double MaxCountBinLog, CurrentBinLog;
            int CurrentBinCount, CurrentBinHeight;
            int CurrentBinDensity, CurrentBinIntensity;
            MaxCountBinLog = Math.Log(Convert.ToDouble(CurrentHistogram.HistogramData[CurrentHistogram.MaxCountBinIndex]), 2.0);

            uint[] PixelArray = (uint[])TextureHistogram.LockRectangle(typeof(uint), 0, LockFlags.Discard, TextureWidth * TextureHeight);
            for (int x = 0; x < TextureWidth; x++)
            {
                CurrentBinCount = CurrentHistogram.HistogramData[x];
                if (CurrentBinCount == 0)
                    CurrentBinHeight = 0;
                else if (CurrentBinCount == 1)
                    CurrentBinHeight = Convert.ToInt32(Convert.ToDouble(TextureHeight) / MaxCountBinLog / 2.0 + 0.5);
                else
                {
                    CurrentBinLog = Math.Log(Convert.ToDouble(CurrentBinCount), 2.0);
                    CurrentBinHeight = Convert.ToInt32(CurrentBinLog * Convert.ToDouble(TextureHeight) / MaxCountBinLog + 0.5);
                }

                CurrentBinDensity = MinDensity + BinSize * x + BinSize / 2;
                CurrentBinIntensity = WindowFunction.ConvertDensityToIntensity(CurrentBinDensity);
                uint CurrentBinColor = (uint)ColorLUT[CurrentBinIntensity].ToArgb();

                for (int y = 0; y < CurrentBinHeight; y++)
                    PixelArray[(TextureHeight - 1 - y) * TextureWidth + x] = CurrentBinColor;
                for (int y = CurrentBinHeight; y < TextureHeight; y++)
                    PixelArray[(TextureHeight - 1 - y) * TextureWidth + x] = (uint)Color.DeepSkyBlue.ToArgb();
            }
            TextureHistogram.UnlockRectangle(0);
        }

        private void UpdateTextureOutput(byte[] Intensity)
        {
            int TextureWidth = this.PanelOutputImage.Width;
            int TextureHeight = this.PanelOutputImage.Height;
            uint[] PixelArray = (uint[])TextureOutput.LockRectangle(typeof(uint), 0, LockFlags.Discard, TextureWidth * TextureHeight);
            for (int i = 0; i < TextureHeight * TextureWidth; i++)
                PixelArray[i] = (uint)(Color.FromArgb(Intensity[i], Intensity[i], Intensity[i])).ToArgb();
            if (CheckBoxMasking.Checked)
            {
                int CurrentSliceOffset = CurrentSliceIndex * VolumeData.YNum * VolumeData.XNum;
                for (int i = 0; i < VolumeData.YNum * VolumeData.XNum; i++)
                {
                    if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x01)
                        PixelArray[i] = (uint)Color.Green.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x02)
                        PixelArray[i] = (uint)Color.Blue.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x03)
                        PixelArray[i] = (uint)Color.Pink.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x04)
                        PixelArray[i] = (uint)Color.Red.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0x05)
                        PixelArray[i] = (uint)Color.Yellow.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0xff)
                        PixelArray[i] = (uint)Color.Yellow.ToArgb();
                }
            }
            TextureOutput.UnlockRectangle(0);
        }

        private void UpdateTextureOutput()
        {
            int CurrentIntensity;
            int CurrentSliceOffset = CurrentSliceIndex * VolumeData.YNum * VolumeData.XNum;
            int TextureWidth = this.PanelOutputImage.Width;
            int TextureHeight = this.PanelOutputImage.Height;
            uint[] PixelArray = (uint[])TextureOutput.LockRectangle(typeof(uint), 0, LockFlags.Discard, TextureWidth * TextureHeight);
            for (int i = 0; i < TextureHeight * TextureWidth; i++)
            {
                if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 0)
                {
                    CurrentIntensity = WindowFunction.ConvertDensityToIntensity(DensityCurrentSlice[i]);
                    PixelArray[i] = (uint)ColorLUT[CurrentIntensity].ToArgb();
                }
                else
                {
                    if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 1)
                        PixelArray[i] = (uint)Color.Red.ToArgb();
                    else if (VolumeData.VolumeMask[CurrentSliceOffset + i] == 2)
                        PixelArray[i] = (uint)Color.Blue.ToArgb();
                }
            }
            TextureOutput.UnlockRectangle(0);
        }

        private void PanelSliceImagePaint(object sender, PaintEventArgs e)
        {
            Surface tempSurface = SwapChainSliceImage.GetBackBuffer(0, BackBufferType.Mono);

            GlobalDevice.SetRenderTarget(0, tempSurface);
            GlobalDevice.SetTexture(0, TextureSliceImage);

            GlobalDevice.BeginScene();
            using (Sprite s = new Sprite(GlobalDevice))
            {
                s.Begin(SpriteFlags.None);
                s.Draw2D(TextureSliceImage, new Rectangle(0, 0, VolumeData.XNum, VolumeData.YNum), 
                    new Rectangle(0, 0, this.PanelSliceImage.Width, this.PanelSliceImage.Height), new Point(0, 0), Color.White);
                s.End();
            }
            GlobalDevice.EndScene();
            SwapChainSliceImage.Present();
        }

        private void PanelOutputImagePaint(object sender, PaintEventArgs e)
        {
            Surface tempSurface = SwapChainOutput.GetBackBuffer(0, BackBufferType.Mono);

            GlobalDevice.SetRenderTarget(0, tempSurface);
            GlobalDevice.SetTexture(0, TextureOutput);

            GlobalDevice.BeginScene();
            using (Sprite s = new Sprite(GlobalDevice))
            {
                s.Begin(SpriteFlags.None);
                s.Draw2D(TextureOutput, new Rectangle(0, 0, VolumeData.XNum, VolumeData.YNum),
                    new Rectangle(0, 0, this.PanelOutputImage.Width, this.PanelOutputImage.Height), new Point(0, 0), Color.White);
                s.End();
            }
            GlobalDevice.EndScene();
            SwapChainOutput.Present();
        }

        private void PanelSliceLUTPaint(object sender, PaintEventArgs e)
        {
            Surface tempSurface = SwapChainLUT.GetBackBuffer(0, BackBufferType.Mono);
            GlobalDevice.SetRenderTarget(0, tempSurface);

            GlobalDevice.BeginScene();
            GlobalDevice.Clear(ClearFlags.Target, Color.Black, 0f, 0);
            using (Sprite s = new Sprite(GlobalDevice))
            {
                s.Begin(SpriteFlags.None);
                s.Draw2D(TextureLUT, new Rectangle(0, 0, this.PanelSliceLUT.Width - 2, this.PanelSliceLUT.Height - 2),
                    new Rectangle(0, 0, this.PanelSliceLUT.Width - 2, this.PanelSliceLUT.Height - 2), new Point(1, 1), Color.White);
                s.End();
            }
            GlobalDevice.EndScene();
            SwapChainLUT.Present();
        }

        private void PanelSliceHistogramPaint(object sender, PaintEventArgs e)
        {
            Surface tempSurface = SwapChainHistogram.GetBackBuffer(0, BackBufferType.Mono);
            GlobalDevice.SetRenderTarget(0, tempSurface);

            GlobalDevice.BeginScene();
            GlobalDevice.Clear(ClearFlags.Target, Color.Black, 0f, 0);
            using (Sprite s = new Sprite(GlobalDevice))
            {
                s.Begin(SpriteFlags.None);
                s.Draw2D(TextureHistogram, new Rectangle(0, 0, this.PanelSliceHistogram.Width - 2, this.PanelSliceHistogram.Height - 2),
                    new Rectangle(0, 0, this.PanelSliceHistogram.Width - 2, this.PanelSliceHistogram.Height - 2), new Point(1, 1), Color.White);
                s.End();
            }
            GlobalDevice.EndScene();
            SwapChainHistogram.Present();

            Graphics g = e.Graphics;
            g.DrawLines(new Pen(Color.Yellow, 1), WindowControlPoints);
            for (int i = 0; i < 4; i++)
                g.FillRectangle(new SolidBrush(Color.Red), WindowControlPoints[i].X - 1, WindowControlPoints[i].Y - 1, 3, 3);
            if (WindowFunction.ThresholdOn)
            {
                g.DrawLines(new Pen(Color.Green, 1), ThresholdControlPoints);
                for (int i = 0; i < 4; i++)
                    g.FillRectangle(new SolidBrush(Color.Blue), ThresholdControlPoints[i].X - 1, ThresholdControlPoints[i].Y - 1, 3, 3);
            }
        }

        private void PanelSliceHistogramMouseDown(object sender, MouseEventArgs e)
        {
            if (e.X < 0 || e.X > this.PanelSliceHistogram.Width - 1 || e.Y < 0 || e.Y > this.PanelSliceHistogram.Height - 1)
                return;

            if (WindowFunction.ThresholdOn)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Math.Abs(e.X - ThresholdControlPoints[i].X) < 2 && Math.Abs(e.Y - ThresholdControlPoints[i].Y) < 2)
                    {
                        ClickedControlPointIndex = i;
                        PanelMPRHistogramMouseClicked = true;
                    }
                }
            }
            else
            {
                if (Math.Abs(e.X - WindowControlPoints[1].X) < 2 && Math.Abs(e.Y - WindowControlPoints[1].Y) < 2)
                {
                    ClickedControlPointIndex = 1;
                    PanelMPRHistogramMouseClicked = true;
                }
                if (Math.Abs(e.X - WindowControlPoints[2].X) < 2 && Math.Abs(e.Y - WindowControlPoints[2].Y) < 2)
                {
                    ClickedControlPointIndex = 2;
                    PanelMPRHistogramMouseClicked = true;
                }
            }
        }

        private void PanelSliceHistogramMouseMove(object sender, MouseEventArgs e)
        {
            if (!PanelMPRHistogramMouseClicked)
                return;

            if (e.X < 0 || e.X > this.PanelSliceHistogram.Width - 1 || e.Y < 0 || e.Y > this.PanelSliceHistogram.Height - 1)
                return;

            if (WindowFunction.ThresholdOn)
            {
                ThresholdControlPoints[ClickedControlPointIndex].X = e.X;
                if (ClickedControlPointIndex == 0 || ClickedControlPointIndex == 1)
                {
                    ThresholdControlPoints[0].X = e.X;
                    ThresholdControlPoints[1].X = e.X;
                    WindowFunction.ThresholdMin = MinDensity + (e.X - 1) * BinSize + BinSize / 2;
                }
                else if (ClickedControlPointIndex == 2 || ClickedControlPointIndex == 3)
                {
                    ThresholdControlPoints[2].X = e.X;
                    ThresholdControlPoints[3].X = e.X;
                    WindowFunction.ThresholdMax = MinDensity + (e.X - 1) * BinSize + BinSize / 2;
                }
            }
            else
            {
                WindowControlPoints[ClickedControlPointIndex].X = e.X;
                WindowFunction.ControlPointDensity[ClickedControlPointIndex] = MinDensity + (e.X - 1) * BinSize + BinSize / 2;
            }

            UpdateTextureHistogram();
            UpdateTextureSliceImage();
            this.PanelSliceImage.Invalidate();
            this.PanelSliceHistogram.Invalidate();
        }

        private void PanelSliceHistogramMouseUp(object sender, MouseEventArgs e)
        {
            ClickedControlPointIndex = -1;
            PanelMPRHistogramMouseClicked = false;
        }

        private void TrackBarSliceImageScroll(object sender, EventArgs e)
        {
            if (CurrentSliceIndex == this.TrackBarSliceImage.Value || this.TrackBarSliceImage.Value < 0 || this.TrackBarSliceImage.Value >= VolumeData.ZNum)
                return;

            CurrentSliceIndex = this.TrackBarSliceImage.Value;
            int NonZeroSliceIndex = CurrentSliceIndex + 1;
            this.LabelCurrentSlice.Text = NonZeroSliceIndex.ToString() + " / " + VolumeData.ZNum.ToString();
            UpdateSliceDensity();

            if (CurrentHistogramMode == HistogramMode.Axial)
            {
                HistogramCurrentSlice.SetVolume(VolumeData, HistogramMode.Axial, CurrentSliceIndex);
                UpdateTextureHistogram();
                this.PanelSliceHistogram.Invalidate();
            }
            UpdateTextureSliceImage();
            this.PanelSliceImage.Invalidate();
        }

        private void TrackBarSliceImageValueChanged(object sender, EventArgs e)
        {
            if (CurrentSliceIndex == this.TrackBarSliceImage.Value || this.TrackBarSliceImage.Value < 0 || this.TrackBarSliceImage.Value >= VolumeData.ZNum)
                return;

            CurrentSliceIndex = this.TrackBarSliceImage.Value;
            int NonZeroSliceIndex = CurrentSliceIndex + 1;
            this.LabelCurrentSlice.Text = NonZeroSliceIndex.ToString() + " / " + VolumeData.ZNum.ToString();
            UpdateSliceDensity();

            if (CurrentHistogramMode == HistogramMode.Axial)
            {
                HistogramCurrentSlice.SetVolume(VolumeData, HistogramMode.Axial, CurrentSliceIndex);
                UpdateTextureHistogram();
                this.PanelSliceHistogram.Invalidate();
            }
            UpdateTextureSliceImage();
            this.PanelSliceImage.Invalidate();
        }

        private void RadioButtonHistogramCheckedChanged(object sender, EventArgs e)
        {
            if (RadioButtonHistogramVolume.Checked)
                CurrentHistogramMode = HistogramMode.Volume;
            else if (RadioButtonHistogramSlice.Checked)
                CurrentHistogramMode = HistogramMode.Axial;

            UpdateTextureHistogram();
            this.PanelSliceHistogram.Invalidate();
        }

        private void CheckBoxThresholdingCheckedChanged(object sender, EventArgs e)
        {
            WindowFunction.ThresholdOn = CheckBoxThresholding.Checked;
            UpdateTextureHistogram();
            UpdateTextureSliceImage();
            this.PanelSliceHistogram.Invalidate();
            this.PanelSliceImage.Invalidate();
        }

        /*
        private void ButtonHistogramCalculateClick(object sender, EventArgs e)
        {
            OutputCalculator = new HistogramCalculator();
            OutputCalculator.Calculate(ref VolumeData, CurrentSliceIndex);

            // RadioButtons
            RadioButton[] RadioButtonOutput = new RadioButton[5];
            for (int i = 0; i < 5; i++)
            {
                RadioButtonOutput[i] = new RadioButton();
                RadioButtonOutput[i].AutoSize = true;
                RadioButtonOutput[i].Checked = false;
                RadioButtonOutput[i].Location = new System.Drawing.Point(200, (i + 1) * 20);
                RadioButtonOutput[i].Name = "RadioButtonOutput" + i.ToString();
                RadioButtonOutput[i].Size = new System.Drawing.Size(400, 17);
                RadioButtonOutput[i].TabIndex = i;
                RadioButtonOutput[i].TabStop = true;
                RadioButtonOutput[i].UseVisualStyleBackColor = true;
                RadioButtonOutput[i].CheckedChanged += new System.EventHandler(this.RadioButtonOuputCheckedChanged);
            }
            RadioButtonOutput[0].Checked = true;
            RadioButtonOutput[0].Text = "(I, |G|)-plot";
            RadioButtonOutput[1].Text = "(I, Sigma*|G|)-plot";
            RadioButtonOutput[2].Text = "(I, Theta*Sigma*|G|)-plot";
            RadioButtonOutput[3].Text = "(L, H)-plot";
            RadioButtonOutput[4].Text = "(Alpha, Sigma)-plot";

            this.GroupBoxSliceOutput.Text = "Histogram";
            this.LabelOutputString.Text = "";
            while (this.GroupBoxSliceOutput.Controls.Count > 3)
                this.GroupBoxSliceOutput.Controls.RemoveAt(this.GroupBoxSliceOutput.Controls.Count - 1);
            for (int i = 0; i < 5; i++)
                this.GroupBoxSliceOutput.Controls.Add(RadioButtonOutput[i]);

            this.LabelOutputString.Text = OutputCalculator.OutputString;
            OutputCalculator.SetOutputImage(ref VolumeData, CurrentSliceIndex, 0);
            UpdateTextureOutput(OutputCalculator.OutputImage);
            this.PanelOutputImage.Invalidate();
        }
         * */

        private void PanelSliceImageMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                MouseClickPosition = new Point(e.X, e.Y);
                ContextMenuStripOutput.Show(this.PanelSliceImage, e.Location);
            }
        }

        private void ToolStripMenuItemSRGClick(object sender, EventArgs e)
        {
        }

        private void CheckBoxMaskingCheckedChanged(object sender, EventArgs e)
        {
            UpdateTextureSliceImage();
            this.PanelSliceImage.Invalidate();
        }

        private void ButtonSaveOutputImageClick(object sender, EventArgs e)
        {
            SaveFileDialog SaveFileDialogOutput = new SaveFileDialog();
            SaveFileDialogOutput.DefaultExt = "bmp";
            SaveFileDialogOutput.Filter = "BITMAP files (*.bmp)|*.bmp|JPEG files (*.jpg)|*.jpg|All files (*.*)|*.*";
            if (SaveFileDialogOutput.ShowDialog() == DialogResult.OK)
            {
                string Ext = SaveFileDialogOutput.FileName.Substring(SaveFileDialogOutput.FileName.Length - 3, 3);
                if (Ext.Equals("bmp"))
                    TextureLoader.Save(SaveFileDialogOutput.FileName, ImageFileFormat.Bmp, TextureOutput);
                else if (Ext.Equals("jpg"))
                    TextureLoader.Save(SaveFileDialogOutput.FileName, ImageFileFormat.Jpg, TextureOutput);
            }
        }

        private void ButtonSaveOutputStringClick(object sender, EventArgs e)
        {
            SaveFileDialog SaveFileDialogOutput = new SaveFileDialog();
            SaveFileDialogOutput.DefaultExt = "txt";
            SaveFileDialogOutput.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (SaveFileDialogOutput.ShowDialog() == DialogResult.OK)
            {
                TextWriter tw = new StreamWriter(SaveFileDialogOutput.FileName);
                tw.Flush();
                tw.WriteLine(this.LabelOutputString.Text);
                tw.Close();
            }
        }


    }
}
