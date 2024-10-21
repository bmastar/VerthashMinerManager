using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using VerthashManager;


namespace Verthash
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        [WebGet(UriTemplate = "minerinfo", ResponseFormat = WebMessageFormat.Json)]
        VerthashInfo MinerInfo();

        [OperationContract]
        [WebGet(UriTemplate = "startminer", ResponseFormat = WebMessageFormat.Json)]
        string StartMiner();

        [OperationContract]
        [WebGet(UriTemplate = "stopminer", ResponseFormat = WebMessageFormat.Json)]
        string StopMiner();


    }

    public class WebComponents : IService
    {
        private MainForm mainForm = null;
        public WebComponents(MainForm mForm)
        {
            mainForm = mForm;
        }
       
        public VerthashInfo MinerInfo()
        {            
            return mainForm.VerthashInfo;
        }

        public string StartMiner()
        {
            mainForm.Service_Start();
            return "Commande start sent successfully";
        }

        public string StopMiner()
        {
            mainForm.Service_Stop();
            return "Commande stop sent successfully";
        }
    }

    public class DeviceInfo
    {
        public int DeviceIndex { get; set; }
        public string DeviceType { get; set; }
        public string DeviceName { get; set; }        
        public string Temperature { get; set; }
        public string HashRate { get; set; }

        public DeviceInfo()
        {

        }

        public DeviceInfo(int deviceIndex, DeviceTypeList deviceType, string deviceName)
        {
            DeviceIndex = deviceIndex;
            DeviceType = deviceType.ToString();    
            DeviceName = deviceName;
            Temperature = "0c";
            HashRate = "0 H/s";
        }
    }

    public class VerthashInfo
    {
        public List<DeviceInfo> DevicesInfo { get; set; }
        public string EngineStatus { get; set; }
        public string Accepted {  get; set; }
        public string TotalHashRate { get; set; }
        public string Difficulty { get; set; }
        public string CurrentBlock {  get; set; }
        public VerthashInfo()
        {
            DevicesInfo = new List<DeviceInfo>();
            EngineStatus = "Stopped";
            Accepted = "";
            TotalHashRate = "0 H/s";
            Difficulty = "";
            CurrentBlock = "";
        }

        public void SetDeviceTemperature(int deviceIndex, DeviceTypeList deviceType, string value)
        {
            DeviceInfo deviceInfo = DevicesInfo.Find(x => x.DeviceType == deviceType.ToString() && x.DeviceIndex == deviceIndex);
            deviceInfo.Temperature = value;
        }

        public void SetDeviceHashrate(int deviceIndex, DeviceTypeList deviceType, string value)
        {
            DeviceInfo deviceInfo = DevicesInfo.Find(x => x.DeviceType == deviceType.ToString() && x.DeviceIndex == deviceIndex);
            deviceInfo.HashRate = value;
        }

    }

    public class VerthashWebService
    {
        public int Port { get; private set; }

        private WebServiceHost svcHost;
        private WebComponents componenet;
        public VerthashWebService(MainForm parentForm, int port = 8000)
        {
            Port = port;
            componenet = new WebComponents(parentForm);
            InitWebServiceHost();
        }        

        private void InitWebServiceHost()
        {
            Uri baseAddress = new Uri("http://localhost:" + Port + "/");            
            svcHost = new WebServiceHost(componenet, baseAddress);
            var behaviour = svcHost.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            behaviour.InstanceContextMode = InstanceContextMode.Single;
        }

        public void Start()
        {
            try
            {
                svcHost.Open();
            }
            catch (AddressAccessDeniedException)
            {
                string userName = Environment.UserDomainName + "\\" + Environment.UserName;
                GrantUrlAcl(userName);

                try
                {
                    InitWebServiceHost();
                    svcHost.Open();
                }
                catch (AddressAccessDeniedException)
                {
                    MessageBox.Show("Error reserving namespaces for URLs\r\nTry run the command below:" + Environment.NewLine + "netsh http add urlacl url=http://+:" + Port + "/ user=" + userName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Stop()
        {
            if (svcHost.State == CommunicationState.Opened) svcHost.Close();
        }

        private void GrantUrlAcl(string userName)
        {                        
            //commande argements :
            string args = "http add urlacl url=http://+:" + Port + "/ user=" + userName;

            //Start netsh
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = "netsh",
                Arguments = args,                
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
            };

            try
            {
                Process netshProcess = new Process();
                netshProcess.StartInfo = startInfo;

                netshProcess.Start();
                netshProcess.WaitForExit();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}