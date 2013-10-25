namespace HNs_Prog
{
    partial class VRViewControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelVR = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // PanelVR
            // 
            this.PanelVR.BackColor = System.Drawing.Color.Black;
            this.PanelVR.Location = new System.Drawing.Point(5, 5);
            this.PanelVR.Name = "PanelVR";
            this.PanelVR.Size = new System.Drawing.Size(954, 954);
            this.PanelVR.TabIndex = 0;
            this.PanelVR.Paint += new System.Windows.Forms.PaintEventHandler(this.PanelVRPaint);
            // 
            // VRViewControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PanelVR);
            this.Name = "VRViewControl";
            this.Size = new System.Drawing.Size(1262, 964);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel PanelVR;
    }
}
