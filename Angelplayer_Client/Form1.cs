﻿using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WebSocketSharp;
using static Angelplayer_Client.Encrypt;

namespace Angelplayer_Client
{
    public partial class Form1 : Form
    {
        //declare NotifyIcon to make application show in right-down toolbox
        private System.Windows.Forms.NotifyIcon notifyIcon1;

        private const string FILE_NAME = "saves.dat";
        private const string FILE_KEY = "tachibana_kanade_maji_tenshi";

        /* Get Installed Applications from windows machine key
         * @return String
         */
        public string GetInstalledApps()
        {
            string output = "";
            string uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(uninstallKey))
            {
                foreach (string skName in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        try
                        {
                            output += sk.GetValue("DisplayName").ToString() + ",";
                        }
                        catch (Exception ex)
                        { }
                    }
                }
            }
            return output.TrimEnd(',');
        }

        /* Get IPv4 Address from System.Net
         * @return String
         */
        public static string GetIP4Address()
        {
            string IP4Address = String.Empty;
            foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (IPA.AddressFamily.ToString() == "InterNetwork")
                {
                    IP4Address += IPA.ToString() + ",";
                }
            }
            return IP4Address.TrimEnd(',');
        }

        /* Get IPv4 Address from System.Net
         * @return String
         */
        public static string GetMacAddress() {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            string MACAddress = String.Empty;
            foreach (var nic in nics)
            {
                //Catch Ethernet MAC only
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    MACAddress += nic.GetPhysicalAddress().ToString() + ",";
                }    
            }
            return MACAddress.TrimEnd(','); ;
        }


        /* Get computer/Device Name
         * @return String
         */
        public static string GetDeviceName()
        {
            return Environment.MachineName;
        }

        /* Get User Name
         * @return String
         */
        public static string GetUserName()
        {
            return Environment.UserName;
        }

        /* Get OS Info.
         * @return String
         */
        public static string GetOSVersion()
        {
            string r = "";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                ManagementObjectCollection information = searcher.Get();
                if (information != null)
                {
                    foreach (ManagementObject obj in information)
                    {
                        r = obj["Caption"].ToString() + " - " + obj["OSArchitecture"].ToString();
                    }
                }
                r = r.Replace("NT 5.1.2600", "XP");
                r = r.Replace("NT 5.2.3790", "Server 2003");
            }
            return r;
        }

        /* turn string to base64 encoding
         * @return String
         */
        private string StringToBase64(string srcText)
        {
            Byte[] bytesEncode = System.Text.Encoding.UTF8.GetBytes(srcText);
            string dstText = Convert.ToBase64String(bytesEncode);
            return dstText;
        }

        public void ConnectToSocket(){
            try
            {
                WS.client.Close();
                WS.client = new WebSocket("ws://" + txt_host.Text + ":" + txt_port.Text);
                WS.client.Connect();
                WS.client.OnMessage += (sender1, e1) =>
                    MessageBox.Show("server says: " + e1.Data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void InitUI()
        {
            //先前有存檔的情況
            if (File.Exists(FILE_NAME))
            {
                string saves = "";
                using (FileStream fs = new FileStream(FILE_NAME, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader r = new BinaryReader(fs))
                    {
                        saves = r.ReadString();
                    }
                }
                saves = StringEncrypt.aesDecryptBase64(saves, FILE_KEY);

                //get saved data and put to textbox
                string[] data = saves.Split(',');
                txt_host.Text = data[0];
                txt_port.Text = data[1];
                txt_cid.Text = data[2];
                txt_passwd.Text = data[3];

                btn_unlock.Enabled = true;
                btn_save.Enabled = false;
                //disable all of textbox
                txt_cid.Enabled = false;
                txt_host.Enabled = false;
                txt_port.Enabled = false;
            }
            else {
                btn_unlock.Enabled = false;
            }
            //initialize all lbels to print device info.
            lbl_ip.Text = "Local IP: " + GetIP4Address();
            lbl_device_name.Text = "Device Name: " + GetDeviceName();
            lbl_mac.Text = "MAC Address: " + GetMacAddress();
            lbl_user_name.Text = "User Name: " + GetUserName();
            lbl_os_version.Text = "OS version: " + GetOSVersion();
        }

        public Form1()
        {
            InitializeComponent();
            //build NotifyIcon
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.notifyIcon1.Icon = new Icon("SSS.ico");
            this.notifyIcon1.Text = "Angelplayer Control System";
            this.notifyIcon1.MouseDoubleClick += new MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitUI();
            timer_send.Start();
            timer_reconnect.Start();
        }
        private static void ShowWindowsMessage(bool flag)
        {
            //MessageBox.Show(flag.ToString());
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            ConnectToSocket();
            if (File.Exists(FILE_NAME))
            {
                File.Delete(FILE_NAME);
            }

            string pw = Interaction.InputBox("input admin password", "input admin password", "", -1, -1);

            using (FileStream fs = new FileStream(FILE_NAME, FileMode.CreateNew))
            {
                using (BinaryWriter w = new BinaryWriter(fs))
                {
                    string saves = txt_host.Text + ",";
                    saves += txt_port.Text + ",";
                    saves += txt_cid.Text + ",";
                    saves += pw;
                    saves = StringEncrypt.aesEncryptBase64(saves, FILE_KEY);
                    w.Write(saves);
                }
            }

            txt_passwd.Text = pw;
            txt_cid.Enabled = false;
            txt_host.Enabled = false;
            txt_port.Enabled = false;
            btn_save.Enabled = false;
            btn_unlock.Enabled = true;
        }

        private void timer_send_Tick(object sender, EventArgs e)
        {
            var ws = WS.client;
            Action<bool> completed = ShowWindowsMessage;
            String comm = txt_cid.Text + ":";
            comm += GetIP4Address() + ":";
            comm += GetMacAddress() + ":";
            comm += GetDeviceName() + ":";
            comm += GetOSVersion() + ":";
            comm += GetUserName() + ":";
            comm += GetInstalledApps();
            comm = StringToBase64(comm);
            int max_length = 1000;

            ws.SendAsync(StringToBase64("SYN"), completed);
            Thread.Sleep(50);
            while (comm.Length > 0) {
                if (comm.Length < max_length)
                {
                    ws.SendAsync(comm, completed);
                    break;
                }
                else {
                    int len = comm.Length;
                    ws.SendAsync(comm.Substring(0, max_length).ToString(), completed);
                    comm = comm.Substring(max_length, len - max_length);
                }
                Thread.Sleep(50);
            }
            Thread.Sleep(50);
            ws.SendAsync(StringToBase64("ACK"), completed);

        }

        private void timer_reconnect_Tick(object sender, EventArgs e)
        {
            WS.client.Connect();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            WS.client.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            MessageBox.Show("This Program can not be closed!");
            e.Cancel = true;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //when click minimized button,then hide form and show notifyicon
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.notifyIcon1.Visible = true;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //make Form show again
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.notifyIcon1.Visible = false;
        }

        private void btn_unlock_Click(object sender, EventArgs e)
        {
            string pw = Interaction.InputBox("input admin password", "input admin password");
            if (pw != txt_passwd.Text)
            {
                MessageBox.Show("incorrect password!");
                return;
            }
            btn_save.Enabled = true;
            txt_cid.Enabled = true;
            txt_host.Enabled = true;
            txt_port.Enabled = true;
            btn_unlock.Enabled = false;
        }
    }
}