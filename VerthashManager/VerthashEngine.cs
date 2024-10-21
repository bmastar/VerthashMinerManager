using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Verthash
{
    public class VerthashEngine
    {
        private Process vhEngineProcess = null;        
        private string enginePath = "";
        private string configPath = "";
        private bool started = false;
        private bool miningMode = false;

        private string _dev;
        private List<string> cu_devices = new List<string>();
        private List<string> cl_devices = new List<string>();


        public VerthashEngine(string EnginePath)
        {
            enginePath = EnginePath;            
        }

        public VerthashEngine(string EnginePath, string ConfigPath)
        {
            enginePath = EnginePath;
            configPath = ConfigPath;            
        }

        public void InitDevices()
        {
            Start("--device-list");
            WaitForExit();
        }

        public void GenerateConfiguration(string Path)
        {
            Start("-g " + Path);
            WaitForExit();
        }

        public void GenerateVerthashfile()
        {
            string _file = Path.Combine(new FileInfo(enginePath).DirectoryName, "verthash.dat");
            if (File.Exists(_file)) File.Delete(_file);

            Start("--gen-verthash-data \"" + _file + "\"");
        }

        public string EnginePath { 
            get { return enginePath; } 
            set { enginePath = value; }
        }

        public string ConfigPath
        {
            get { return configPath; }
            set { configPath = value; }
        }

        public List<string> CudaDevices
        {
            get { return cu_devices; }
        }

        public List<string> OpenCLDevices
        {
            get { return cl_devices; }
        }

        public bool Started
        {
            get
            {
                return started;
            }
            private set
            {
                _dev = "";
                started = value;
                OnStatusChanged?.Invoke(this, new StatusChangedEventArgs(value));
            }
        }

        public void Start(string Args="")
        {
            string args = "";
            if (Args == "") args = " -c \"" + configPath + "\"";
            else args = Args;

            if (args.Trim() == "-c \"" + configPath + "\"") miningMode = true;
            else miningMode = false;

            FileInfo fileInfo = new FileInfo(enginePath);            

            if (!Started)
            {
                ProcessStartInfo startEngineInfo = new ProcessStartInfo()
                {
                    FileName = enginePath,
                    Arguments = args,
                    WorkingDirectory = fileInfo.Directory.FullName,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,                    
                    RedirectStandardOutput = true                                       
                };

                vhEngineProcess = new Process();
                vhEngineProcess.StartInfo = startEngineInfo;
                vhEngineProcess.EnableRaisingEvents = true;

                vhEngineProcess.OutputDataReceived += VhEngineProcess_OutputDataReceived;
                vhEngineProcess.ErrorDataReceived += VhEngineProcess_ErrorDataReceived;
                vhEngineProcess.Exited += VhEngineProcess_Exited;

                vhEngineProcess.Start();
                vhEngineProcess.PriorityClass = ProcessPriorityClass.Normal;


                vhEngineProcess.BeginOutputReadLine();
                vhEngineProcess.BeginErrorReadLine();               

                if (miningMode) Started = true;
            }
        }

        private void VhEngineProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            //OnReceivedMessage?.Invoke(this, new ReceivedMessageEvent(MessageType.Error, e.Data));
            VhEngineProcess_OutputDataReceived(sender, e);
        }

        public void WaitForExit()
        {
            vhEngineProcess?.WaitForExit();
        }

        private void VhEngineProcess_Exited(object sender, EventArgs e)
        {
            OnDataReceived?.Invoke(this, new VhDataReceivedEventArgs("Process exited !"));
            if (miningMode) Started = false;            
        }

        public void Stop()
        {
            if (Started && vhEngineProcess != null)
            {                                                
                vhEngineProcess.Kill();                

                Thread.Sleep(1000);                
                vhEngineProcess = null;
                if (miningMode) Started = false;
            }
        }
        private void VhEngineProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null && e.Data != String.Empty)
            {
                if (e.Data == "OpenCL devices:") _dev = "OpenCL";
                else if (e.Data == "CUDA devices:") _dev = "Cuda";
                else if (e.Data.StartsWith("\tIndex"))
                {
                    if (_dev == "OpenCL") cl_devices.Add(e.Data.Substring(e.Data.IndexOf("Name: ") + 6));
                    if (_dev == "Cuda") cu_devices.Add(e.Data.Substring(e.Data.IndexOf("Name: ") + 6));
                }
                else if (e.Data.Contains("] INFO "))
                {
                    ReceivedInfoEventArgs _e = new ReceivedInfoEventArgs(e.Data);
                    OnReceivedInfo?.Invoke(this, _e);
                }
                else OnReceivedMessage?.Invoke(this, new ReceivedMessageEvent(e.Data));                
                
                OnDataReceived?.Invoke(this, new VhDataReceivedEventArgs(e.Data));                
            }
        }
        
        public event OnDataReceivedEvent OnDataReceived;        
        public event OnReceivedInfoEvent OnReceivedInfo;
        public event OnReceivedMessageEvent OnReceivedMessage;        
        public event OnStatusChangedEvent OnStatusChanged;
    }       
}
