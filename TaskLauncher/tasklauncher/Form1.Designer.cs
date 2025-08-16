using System.Windows.Forms;

namespace TaskLauncher
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi; // important for DPI
            this.ClientSize = new System.Drawing.Size(880, 560);
            this.Name = "Form1";
            this.Text = "Task Launcher";
            this.ResumeLayout(false);
        }
        #endregion
    }
}
