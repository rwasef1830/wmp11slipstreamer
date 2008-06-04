using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WMP11Slipstreamer
{
    public partial class SourceError : Form
    {
        Timer timer;

        public SourceError(string message)
        {
            InitializeComponent();
            this.label1.Text = message;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            this.Close();
        }

        void SourceError_Load(object sender, EventArgs e)
        {
            System.Media.SystemSounds.Hand.Play();
            timer = new Timer();
            timer.Interval = 10000;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}