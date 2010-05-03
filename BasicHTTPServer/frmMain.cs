using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BasicHTTPServer
{
    public partial class frmMain : Form
    {
        protected HTTPServer MyServer;
        protected int PortToUse = 11095;

        public frmMain()
        {
            InitializeComponent();
            MyServer = new HTTPServer(PortToUse);
            while (!HTTPServer.CheckPortAvailability(MyServer.portNum))
                MyServer.portNum++;
            MyServer.Start();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MyServer.IsAlive)
                MyServer.Stop();
        }
    }
}
