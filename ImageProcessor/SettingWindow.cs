using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ImageProcessor
{
    public partial class SettingWindow : Form
    {
        public SettingWindow()
        {
            InitializeComponent();
        }

        //Offset value from the trackbar
        public int Offset = 0;

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Offset = trackBar1.Value;
        }
    }
}
