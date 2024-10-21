using EquipmentsApp;
using System;
using System.IO;
using System.Windows.Forms;
using Verthash;
using VerthashManager.Controls;
using VerthashManager.Properties;

namespace VerthashManager
{
    public partial class MainForm : Form
    {        
        private VerthashEngine verthashEngine;
        private VerthashWebService webService;        

        public VerthashInfo VerthashInfo { get; private set; }

        public MainForm()
        {
            InitializeComponent();

            VerthashInfo = new VerthashInfo();

            //Starting webservice
            int port = Settings.Default.DefaultPort;            
            webService = new VerthashWebService(this, port);
            webService.Start();
        }

        public void Service_Start()
        {            
            verthashEngine = new VerthashEngine(txtMinerPath.Text, txtConfigFile.Text);
            verthashEngine.InitDevices();            

            VerthashInfo.DevicesInfo.Clear();

            lvCuda.Items.Clear();
            for (int i = 0; i < verthashEngine.CudaDevices.Count; i++)
            {
                lvCuda.Items.Add(i.ToString());
                lvCuda.Items[0].SubItems.Add(verthashEngine.CudaDevices[i]);
                lvCuda.Items[0].SubItems.Add("0c");
                lvCuda.Items[0].SubItems.Add("0 H/s");
                VerthashInfo.DevicesInfo.Add(new DeviceInfo(i, DeviceTypeList.Cuda, verthashEngine.CudaDevices[i]));
            }

            lvCL.Items.Clear();
            for (int i = 0; i < verthashEngine.OpenCLDevices.Count; i++)
            {
                lvCL.Items.Add(i.ToString());
                lvCL.Items[0].SubItems.Add(verthashEngine.OpenCLDevices[i]);
                lvCL.Items[0].SubItems.Add("0c");
                lvCL.Items[0].SubItems.Add("0 H/s");
                VerthashInfo.DevicesInfo.Add(new DeviceInfo(i, DeviceTypeList.OpenCL, verthashEngine.OpenCLDevices[i]));
            }

            lvLog.Items.Clear();

            verthashEngine.OnReceivedInfo += VerthashEngine_OnReceivedInfo;
            verthashEngine.OnReceivedMessage += VerthashEngine_OnReceivedMessage;            
            verthashEngine.OnStatusChanged += VerthashEngine_OnStatusChanged;

            verthashEngine.Start();

            lvLog.Items.Add("Info");
            lvLog.Items[lvLog.Items.Count - 1].SubItems.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            lvLog.Items[lvLog.Items.Count - 1].SubItems.Add("Service started");

            SaveSettings();
        }

        public void Service_Stop()
        {
            verthashEngine.Stop();            

            for (int i = 0; i < verthashEngine.CudaDevices.Count; i++)
            {
                lvCuda.Items[i].SubItems[2].Text = "0c";
                lvCuda.Items[i].SubItems[3].Text = "0 H/s";
                
                VerthashInfo.SetDeviceTemperature(i, DeviceTypeList.Cuda, "0c");
                VerthashInfo.SetDeviceHashrate(i, DeviceTypeList.Cuda, "0 H/s");
            }

            for (int i = 0; i < verthashEngine.OpenCLDevices.Count; i++)
            {
                lvCL.Items[i].SubItems[2].Text = "0c";
                lvCL.Items[i].SubItems[3].Text = "0 H/s";

                VerthashInfo.SetDeviceTemperature(i, DeviceTypeList.OpenCL, "0c");
                VerthashInfo.SetDeviceHashrate(i, DeviceTypeList.OpenCL, "0 H/s");

            }

            tslAccepted.Text = "Accepted : ";
            tslTotalHashRate.Text = "Total hash rate : ";
            tslDifficulty.Text = "Difficulty : ";
            tslBlock.Text = "Block : ";

            VerthashInfo.Accepted = "";
            VerthashInfo.TotalHashRate = "0 H/s";
            VerthashInfo.Difficulty = "";
            VerthashInfo.CurrentBlock = "";

            lvLog.Items.Add("Info");
            lvLog.Items[lvLog.Items.Count - 1].SubItems.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            lvLog.Items[lvLog.Items.Count - 1].SubItems.Add("Service stopped");
            lvLog.Items[lvLog.Items.Count - 1].EnsureVisible();
        }

        private void SaveSettings()
        {
            Settings.Default.MinerPath = txtMinerPath.Text;
            Settings.Default.ConfigPath = txtConfigFile.Text;
            Settings.Default.Save();
        }

        private void VerthashEngine_OnStatusChanged(object sender, StatusChangedEventArgs e)
        {
            this.InvokeOnUiThreadIfRequired(() =>
            {
                tslStatus.Text = "Status : " + (e.Status ? "Started" : "Stopped");
                VerthashInfo.EngineStatus = e.Status ? "Started" : "Stopped";                
                startToolStripMenuItem.Enabled = !e.Status;
                tsbStart.Enabled = !e.Status;
                startNotifyIconMenu.Enabled = !e.Status;
                
                stopToolStripMenuItem.Enabled = e.Status;
                tsbStop.Enabled = e.Status;
                stopNotifyIconMenu.Enabled = e.Status;

                txtMinerPath.Enabled = !e.Status;
                txtConfigFile.Enabled = !e.Status;
                btnBrowseMiner.Enabled = !e.Status;
                btnBrowseConfig.Enabled = !e.Status;
                generateToolStripMenuItem.Enabled = !e.Status;
                editToolStripMenuItem.Enabled = !e.Status;
                generateVerhashdatToolStripMenuItem.Enabled = !e.Status;
            });
        }

        private void VerthashEngine_OnReceivedMessage(object sender, ReceivedMessageEvent e)
        {
            this.InvokeOnUiThreadIfRequired(() =>
            {
                lvLog.Items.Add(e.Type.ToString());
                lvLog.Items[lvLog.Items.Count - 1].SubItems.Add(e.Date);
                lvLog.Items[lvLog.Items.Count - 1].SubItems.Add(e.Message);
                
                lvLog.Items[lvLog.Items.Count - 1].EnsureVisible();
            });
        }

        private void VerthashEngine_OnReceivedInfo(object sender, ReceivedInfoEventArgs e)
        {
            this.InvokeOnUiThreadIfRequired(() =>
            {
                if (logInfoToolStripMenuItem.Checked)
                {
                    lvLog.Items.Add("Info");
                    lvLog.Items[lvLog.Items.Count - 1].SubItems.Add(e.Date);
                    lvLog.Items[lvLog.Items.Count - 1].SubItems.Add(e.Message);
                    
                    lvLog.Items[lvLog.Items.Count - 1].EnsureVisible();
                }

                if (e.IsFromDevice)
                {
                    if (e.DeviceType == DeviceTypeList.Cuda)
                    {
                        if (e.Temperature != null)
                        {
                            lvCuda.Items[e.DeviceIndex].SubItems[2].Text = e.Temperature.ToLower();
                            VerthashInfo.SetDeviceTemperature(e.DeviceIndex, DeviceTypeList.Cuda, e.Temperature.ToLower());
                        }
                        lvCuda.Items[e.DeviceIndex].SubItems[3].Text = e.DeviceHashRate;
                        VerthashInfo.SetDeviceHashrate(e.DeviceIndex, DeviceTypeList.Cuda, e.DeviceHashRate);
                    }
                    else
                    {
                        if (e.Temperature != null)
                        {
                            lvCL.Items[e.DeviceIndex].SubItems[2].Text = e.Temperature.ToLower();
                            VerthashInfo.SetDeviceTemperature(e.DeviceIndex, DeviceTypeList.OpenCL, e.Temperature.ToLower());
                        }
                        lvCL.Items[e.DeviceIndex].SubItems[3].Text = e.DeviceHashRate;
                        VerthashInfo.SetDeviceHashrate(e.DeviceIndex, DeviceTypeList.OpenCL, e.DeviceHashRate);
                    }
                }
                else
                {
                    if (e.Accepted != null && e.Accepted.Trim() != string.Empty)
                    {
                        tslAccepted.Text = "Accepted : " + e.Accepted;
                        VerthashInfo.Accepted = e.Accepted;
                    }
                    if (e.TotalHashRate != null && e.TotalHashRate.Trim() != string.Empty)
                    {
                        tslTotalHashRate.Text = "Total hash rate : " + e.TotalHashRate;
                        VerthashInfo.TotalHashRate = e.TotalHashRate;
                    }
                    if (e.Difficulty != null && e.Difficulty.Trim() != string.Empty)
                    {
                        tslDifficulty.Text = "Difficulty : " + e.Difficulty;
                        VerthashInfo.Difficulty = e.Difficulty;  
                    }
                    if (e.BlockNumber != null && e.BlockNumber.Trim() != string.Empty)
                    {
                        tslBlock.Text = "Block : " + e.BlockNumber;
                        VerthashInfo.CurrentBlock = e.BlockNumber;
                    }
                }
            });
        }

        private void btnBrowseMiner_Click(object sender, EventArgs e)
        {
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Title = "Select Verthash miner executable";
            openFileDialog1.CheckFileExists = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK) txtMinerPath.Text = openFileDialog1.FileName;
        }

        private void btnBrowseConfig_Click(object sender, EventArgs e)
        {
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.Title = "Select config file";
            openFileDialog1.CheckFileExists = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK) txtConfigFile.Text = openFileDialog1.FileName;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (verthashEngine != null) verthashEngine.Stop();
            if (webService != null) webService.Stop();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Service_Start();
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Service_Stop();
        }

        private void generateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (txtConfigFile.Text == string.Empty)
            {
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.Title = "Select config file";
                openFileDialog1.CheckFileExists = false;
                if (openFileDialog1.ShowDialog() == DialogResult.OK) txtConfigFile.Text = openFileDialog1.FileName;
            }

            if (txtMinerPath.Text == string.Empty)
            {
                openFileDialog1.FilterIndex = 1;
                openFileDialog1.Title = "Select Verthash miner executable";
                openFileDialog1.CheckFileExists = true;
                if (openFileDialog1.ShowDialog() == DialogResult.OK) txtMinerPath.Text = openFileDialog1.FileName;
            }

            if (txtMinerPath.Text != string.Empty && txtConfigFile.Text != string.Empty)
            {
                string confFile = txtConfigFile.Text.Contains("\\") ? txtConfigFile.Text : "miner\\" + txtConfigFile.Text;

                FileInfo fileInfo = new FileInfo(confFile);
                bool _gen = true;

                if (fileInfo.Exists)
                {
                    if (MessageBox.Show("File already exists, overwrite ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        fileInfo.Delete();
                    }
                    else _gen = false;
                }

                if (_gen)
                {
                    VerthashEngine tmpEngine = new VerthashEngine(txtMinerPath.Text);
                    tmpEngine.GenerateConfiguration(fileInfo.FullName);

                    string confData = File.ReadAllText(fileInfo.FullName);
                    confData = confData.Replace("Url = \"stratum+tcp://example.com:port\"", Settings.Default.DefaultUrl).Replace("Username = \"user\"", Settings.Default.DefaultUser);
                    File.WriteAllText(fileInfo.FullName, confData);

                    MessageBox.Show("Configuration file generated successfully");
                }

            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string confFile = txtConfigFile.Text.Contains("\\") ? txtConfigFile.Text : "miner\\" + txtConfigFile.Text;
            FileInfo fileInfo = new FileInfo(confFile);
            XmlEditorForm xmlEditorForm = new XmlEditorForm(fileInfo.FullName);
            xmlEditorForm.ShowDialog();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lvLog.Items.Clear();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        private void tsmCopyMessage_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(lvLog.SelectedItems[0].SubItems[2].Text);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string _log = string.Empty;
                for (int i = 0;i < lvLog.Items.Count;i++)
                {
                    _log = _log + lvLog.Items[i].SubItems[0].Text + "\t" + lvLog.Items[i].SubItems[1].Text + "\t" + lvLog.Items[i].SubItems[2].Text + Environment.NewLine;
                }

                File.WriteAllText(saveFileDialog1.FileName, _log);

                MessageBox.Show("Log saved successfully");
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
            }
            else ShowInTaskbar = true;            
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }

        private void generateVerhashdatToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            VerthashEngine tmpEngine = new VerthashEngine(txtMinerPath.Text);            
            tmpEngine.GenerateVerthashfile();
            MessageBox.Show("File generation started. This may take a while...");
        }
    }
}
