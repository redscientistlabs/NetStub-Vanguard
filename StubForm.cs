namespace RTCV_PS4ConnectionTest
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

    public partial class StubForm : Form
    {
        private Point originalLbTargetLocation;

        private Size originalLbTargetSize;

        public string localIP;
        public int localPort;
        public StubForm()
        {
            InitializeComponent();

            SyncObjectSingleton.SyncObject = this;
            
            if (Params.IsParamSet("PS4_IP"))
            {
                tbClientAddr.Text = Params.ReadParam("PS4_IP");
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
        }

        private void StubForm_Load(object sender, EventArgs e)
        {
            Colors.SetRTCColor(Color.FromArgb(149, 120, 161), this);

            Focus();

        }

        private void BtnRefreshDomains_Click(object sender, EventArgs e)
        {
            if (VanguardCore.vanguardConnected)
                ProcessWatch.UpdateDomains();
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
            //Connector.InitializeConnector();
            btnStartClient.Visible = true;
            VanguardImplementation.ps4 = new librpc.PS4RPC(tbClientAddr.Text);
            VanguardImplementation.ps4.Connect();
            VanguardImplementation.pl = VanguardImplementation.ps4.GetProcessList();
            foreach (var proc in VanguardImplementation.pl.processes)
            {
                if (proc.name == "eboot.bin")
                {
                    cbProcessList.Items.Add(proc.name);
                }
            }
            Params.SetParam("PS4_IP", tbClientAddr.Text);

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //Hook.Start();
            VanguardCore.Start();
            btnRefreshDomains.Visible = true;
        }

        private void btnStartClient_Click(object sender, EventArgs e)
        {
            //Hook.Start();
            VanguardCore.Start();
            btnRefreshDomains.Visible = true;
        }

        private void SendPayload(string IP, string path, bool isElf)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(new IPEndPoint(IPAddress.Parse(IP), isElf ? 9023 : 9020));
            socket.SendFile(path);
            socket.Close();
        }

        private void btnPayload_Click(object sender, EventArgs e)
        {
            btnConnect.Visible = true;
            SendPayload(tbClientAddr.Text, System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payload.bin"), false);
            Thread.Sleep(1000);
            SendPayload(tbClientAddr.Text, System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "kpayload.elf"), true);
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
            VanguardImplementation.ProcessName = (string)cbProcessList.Items[cbProcessList.SelectedIndex];
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
