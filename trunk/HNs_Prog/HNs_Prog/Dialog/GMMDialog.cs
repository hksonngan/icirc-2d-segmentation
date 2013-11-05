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
    public partial class GMMDialog : Form
    {
        public enum GMMModel { SCOriginal, SIFrangi };
        public GMMModel ModelIndex;

        public GMMDialog()
        {
            InitializeComponent();
            ModelIndex = GMMModel.SCOriginal;
        }

        private void ButtonRunClick(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            if (RadioButtonGMMSCOriginal.Checked)
                ModelIndex = GMMModel.SCOriginal;
            else if (RadioButtonGMMSIFrangi.Checked)
                ModelIndex = GMMModel.SIFrangi;
        }
    }
}
