using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace HNs_Prog
{
    public partial class VRViewControl : UserControl
    {
        private Device dx_device;

        public VRViewControl()
        {
            InitializeComponent();

            InitializeDevice();
        }

        public void InitializeDevice()
        {
            PresentParameters presentParams = new PresentParameters();
            presentParams.Windowed = true;
            presentParams.SwapEffect = SwapEffect.Discard;

            dx_device = new Device(0, DeviceType.Hardware, this.PanelVR, CreateFlags.SoftwareVertexProcessing, presentParams);
        }

        private void PanelVRPaint(object sender, PaintEventArgs e)
        {
            dx_device.Clear(ClearFlags.Target, Color.DarkSlateBlue, 1.0f, 0);
            dx_device.Present();
        }
    }
}
