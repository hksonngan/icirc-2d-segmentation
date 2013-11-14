using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HNs_Prog.Dialog
{
    public partial class VEDialog : Form
    {
        public enum VEMethod { Frangi, KrissianModel, KrissianFlux, ManniesingVED, TrucDFB };
        public VEMethod MethodIndex;
        public bool CheckedHomohorphicFiltering;
        public bool CheckedMedianFiltering;
        public bool CheckedAllFrames;

        public VEDialog()
        {
            InitializeComponent();
            MethodIndex = VEMethod.Frangi;
            CheckedHomohorphicFiltering = true;
            CheckedMedianFiltering = true;
            CheckedAllFrames = false;
        }

        private void ButtonRunClick(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            if (RadioButtonFrangi.Checked)
                MethodIndex = VEMethod.Frangi;
            else if (RadioButtonKrissian1.Checked)
                MethodIndex = VEMethod.KrissianModel;
            else if (RadioButtonKrissian2.Checked)
                MethodIndex = VEMethod.KrissianFlux;
            else if (RadioButtonManniesing.Checked)
                MethodIndex = VEMethod.ManniesingVED;
            else if (RadioButtonTruc.Checked)
                MethodIndex = VEMethod.TrucDFB;
        }

        private void CheckBoxCheckedChanged(object sender, EventArgs e)
        {
            CheckedHomohorphicFiltering = CheckBoxHomomorphicFiltering.Checked;
            CheckedMedianFiltering = CheckBoxMedianFiltering.Checked;
            CheckedAllFrames = CheckBoxAllFrame.Checked;
        }
    }
}
