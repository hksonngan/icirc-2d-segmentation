using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HNs_Prog
{
    public partial class ProgressWindow : Form
    {
        string taskName;

        public ProgressWindow()
        {
            InitializeComponent();
        }

        public ProgressWindow(string Task, int MinValue, int MaxValue)
        {
            InitializeComponent();
            progressBar.Minimum = MinValue;
            progressBar.Maximum = MaxValue;
            progressBar.Value = MinValue;
            taskName = Task;
        }

        public void Increment(int IncValue)
        {
            progressBar.Increment(IncValue);
            labelProgrss.Text = taskName + ": " + progressBar.Value.ToString() + "/" + progressBar.Maximum.ToString();
            labelProgrss.Update();
        }
    }
}
