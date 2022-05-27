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

        public int wSize = 128;

        private void radioButton1_CheckedChanged(object sender, EventArgs e) { if (radioButton1.Checked) wSize = 64; }
        private void radioButton2_CheckedChanged(object sender, EventArgs e) { if (radioButton2.Checked) wSize = 128; }
        private void radioButton3_CheckedChanged(object sender, EventArgs e) { if (radioButton3.Checked) wSize = 256; }

    }
}
