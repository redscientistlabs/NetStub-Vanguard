namespace NetStub
{
    using System;
    using System.Drawing;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Windows.Forms;
    using RTCV.Common;
    using RTCV.NetCore;
    using RTCV.UI;
    using RTCV.Vanguard;

    public enum StubMode
    {
        PS4,
        MacOSX_PPC,
        Linux_AMD64,
        WindowsXP,
    }
    public partial class StubForm : Form
    {
       
        public static volatile System.Timers.Timer AutoCorruptTimer;

        public string localIP;
        public int localPort;
        public StubForm()
        {
            InitializeComponent();

            SyncObjectSingleton.SyncObject = this;

            cbMode.Items.Clear();

            foreach (var mode in Enum.GetNames(typeof(StubMode)))
            {
                cbMode.Items.Add(mode);
            }
            
            if (Params.IsParamSet("LAST_IP"))
            {
                tbClientAddr.Text = Params.ReadParam("LAST_IP");
            }
            if (Params.IsParamSet("NETSTUB_MODE"))
            {
                string stubModeString = Params.ReadParam("NETSTUB_MODE");
                cbMode.SelectedIndex = (int)(StubMode)System.Enum.Parse(typeof(StubMode), stubModeString);
                VanguardImplementation.stubMode = (StubMode)System.Enum.Parse(typeof(StubMode), stubModeString);
            }
            if (Params.IsParamSet("PROCESS_NAME"))
            {
                VanguardImplementation.ProcessName = Params.ReadParam("PROCESS_NAME");
                tbProcName.Text = VanguardImplementation.ProcessName;
            }
            if (!Params.IsParamSet("DISCLAIMERREAD"))
            {
                var disclaimer = $@"Welcome.

Disclaimer:
This program comes with absolutely ZERO warranty.
You may use it at your own risk.
Be EXTREMELY careful with what you choose to corrupt.
Be aware there is always the chance of damage.

This program inserts random data in hooked processes. There is no way to accurately predict what can happen out of this.
The developers of this software will not be held responsible for any damage caused
as the result of use of this software.

By clicking 'Yes' you agree that you have read this warning in full and are aware of any potential consequences of use of the program. If you do not agree, click 'No' to exit this software.";
                if (MessageBox.Show(disclaimer, "Net Stub", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    Environment.Exit(0);

                Params.SetParam("DISCLAIMERREAD");
            }

            Colors.SetRTCColor(Color.LightSteelBlue, this);
        }

        private void StubForm_Load(object sender, EventArgs e)
        {

            Focus();

        }

        private void BtnRefreshDomains_Click(object sender, EventArgs e)
        {
            if (VanguardCore.vanguardConnected)
            {
                switch (VanguardImplementation.stubMode)
                {
                    case StubMode.PS4:
                        StubEndpoints.PS4.ProcessWatch.UpdateDomains();
                        break;
                    case StubMode.MacOSX_PPC:
                        StubEndpoints.MacOSX_PPC.ProcessWatch.UpdateDomains();
                        break;
                    case StubMode.Linux_AMD64:
                        StubEndpoints.X86_64_Linux.ProcessWatch.UpdateDomains();
                        break;
                    case StubMode.WindowsXP:
                        StubEndpoints.WindowsXP.ProcessWatch.UpdateDomains();
                        break;
                    default:
                        break;
                }
            }
        }

        private void connect_Click(object sender, EventArgs e)
        {
        }

        private void tbClientAddr_TextChanged(object sender, EventArgs e)
        {

        }

        private void tbClientPort_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (VanguardImplementation.stubMode == StubMode.PS4)
            { 
                //Connector.InitializeConnector();
                btnStartClient.Visible = true;
                VanguardImplementation.ps4 = new libdebug.PS4DBG(tbClientAddr.Text);
                VanguardImplementation.ps4.Connect();
                VanguardImplementation.ps4.Notify(222, $"Now connected to NetStub");
                VanguardImplementation.pl = VanguardImplementation.ps4.GetProcessList();
                StubEndpoints.PS4.ProcessWatch.ExceptionHandlerApplied = false;
                foreach (var proc in VanguardImplementation.pl.processes)
                {
                    if (proc.name == VanguardImplementation.ProcessName)
                    {
                        lbPID.Text = $"(PID: {proc.pid})";

                        VanguardImplementation.ps4.Notify(222, lbPID.Text);
                        break;
                    }
                }
            }
            else if (VanguardImplementation.stubMode == StubMode.MacOSX_PPC)
            {
                btnStartClient.Visible = true;
                VanguardImplementation.mac = new StubEndpoints.MacOSX_PPC.RPC.PowerMacRPC(tbClientAddr.Text);
                VanguardImplementation.mac.Connect();
            }
            else if (VanguardImplementation.stubMode == StubMode.Linux_AMD64)
            {
                btnStartClient.Visible = true;
                VanguardImplementation.linux = new StubEndpoints.X86_64_Linux.LinuxRPC(tbClientAddr.Text);
                VanguardImplementation.linux.Connect();
            }
            else if (VanguardImplementation.stubMode == StubMode.WindowsXP)
            {
                btnStartClient.Visible = true;
                VanguardImplementation.winxp = new StubEndpoints.WindowsXP.RPC(tbClientAddr.Text);
                VanguardImplementation.winxp.Connect();
            }
            Params.SetParam("LAST_IP", tbClientAddr.Text);

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //Hook.Start();
            VanguardCore.Start();
            if (VanguardImplementation.stubMode == StubMode.PS4)
            {
                VanguardImplementation.ps4.Notify(222, $"Now connected to RTCV");
            }
            btnRefreshDomains.Visible = true;
        }

        private void btnStartClient_Click(object sender, EventArgs e)
        {
            //Hook.Start();
            VanguardCore.Start();
            RTCV.Common.Logging.StartLogging(VanguardCore.logPath);
            btnRefreshDomains.Visible = true;
            if (VanguardImplementation.stubMode == StubMode.PS4)
            {
                if (VanguardImplementation.ProcessName == "")
                {
                    VanguardImplementation.ProcessName = "eboot.bin";
                }
                StubEndpoints.PS4.ProcessWatch.Start();
                VanguardImplementation.ps4.Notify(222, $"Now connected to RTCV");
                return;
            }
            if (VanguardImplementation.ProcessName == "")
            {
                return;
            }
            if (VanguardImplementation.stubMode == StubMode.MacOSX_PPC)
            {
                StubEndpoints.MacOSX_PPC.ProcessWatch.Start();
            }
            else if (VanguardImplementation.stubMode == StubMode.Linux_AMD64)
            {
                StubEndpoints.X86_64_Linux.ProcessWatch.Start();
            }
            else if (VanguardImplementation.stubMode == StubMode.WindowsXP)
            {
                StubEndpoints.WindowsXP.ProcessWatch.Start();
            }
        }

        private void SendPayload(string IP, string path, bool isElf)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(new IPEndPoint(IPAddress.Parse(IP), 9090));
            socket.SendFile(path);
            socket.Close();
        }

        private void btnPayload_Click(object sender, EventArgs e)
        {
            btnConnect.Visible = true;
            SendPayload(tbClientAddr.Text, System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payload.bin"), false);
        }

        private void label7_Click(object sender, EventArgs e)
        {
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        
        private void StubForm_Load_1(object sender, EventArgs e)
        {

        }

        private void cbProcessList_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void tbProcName_TextChanged(object sender, EventArgs e)
        {
            VanguardImplementation.ProcessName = tbProcName.Text;
            Params.SetParam("PROCESS_NAME", VanguardImplementation.ProcessName);
        }

        private void cbMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnPayload.Visible = false;
            if (cbMode.SelectedIndex == (int)StubMode.PS4)
            {
                btnPayload.Visible = true;
            }
            VanguardImplementation.stubMode = (StubMode)Enum.Parse(typeof(StubMode), (string)cbMode.SelectedItem);
            Params.SetParam("NETSTUB_MODE", VanguardImplementation.stubMode.ToString());
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            switch (VanguardImplementation.stubMode)
            {
                default: break;
                case StubMode.PS4:
                    {
                        StubEndpoints.PS4.ProcessWatch.OverrideExceptionHandlers = cbOverrideHandlers.Checked;
                        break;
                    }
            }
        }

        private void btnFindCaves_Click(object sender, EventArgs e)
        {
            switch (VanguardImplementation.stubMode)
            {
                default: break;
                case StubMode.Linux_AMD64:
                    {
                        StubEndpoints.X86_64_Linux.ProcessWatch.CaveList = StubEndpoints.X86_64_Linux.ProcessWatch.FindCodeCaves(16);
                        break;
                    }
                case StubMode.WindowsXP:
                    {
                        StubEndpoints.WindowsXP.ProcessWatch.CaveList = StubEndpoints.WindowsXP.ProcessWatch.FindCodeCaves(16);
                        break;
                    }
            }
        }



        //private void tbAutoAttach_TextChanged(object sender, EventArgs e)
        //{

        //}

        //private void label3_Click(object sender, EventArgs e)
        //{

        //}

        //private void btnTargetSettings_Click(object sender, EventArgs e)
        //{

        //}

        //private void label4_Click(object sender, EventArgs e)
        //{

        //}
    }
}
