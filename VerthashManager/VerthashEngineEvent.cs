using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Verthash
{
    public delegate void OnDataReceivedEvent(object sender, VhDataReceivedEventArgs e);
    public delegate void OnReceivedInfoEvent(object sender, ReceivedInfoEventArgs e);
    public delegate void OnReceivedMessageEvent(object sender, ReceivedMessageEvent e);    
    public delegate void OnStatusChangedEvent(object sender, StatusChangedEventArgs e);

    public class VhDataReceivedEventArgs : EventArgs
    {
        public string Data { get; set; }

        public VhDataReceivedEventArgs()
        {

        }

        public VhDataReceivedEventArgs(string data)
        {
            Data = data;
        }
    }

    public enum DeviceTypeList
    {
        Cuda,
        OpenCL
    }
    public class ReceivedInfoEventArgs : EventArgs
    {
        public bool IsFromDevice { get; private set; }
        public string Date { get; private set; }
        public DeviceTypeList DeviceType { get; private set; }              
        public int DeviceIndex { get; private set; }           
        public int? ErrorNbr { get; private set; }               
        public string Temperature { get; private set; }              
        public string DeviceHashRate { get; private set; }
        public string Accepted { get; private set; }
        public int? AcceptedHash { get; private set; }           
        public int? TotalHash { get; private set; }            
        public string TotalHashRate { get; private set; }      
        public string Difficulty { get; private set; }
        public string BlockNumber { get; private set; }
        public string Message { get { return _info.Substring(5).Trim(); } }

        private string _info;

        public ReceivedInfoEventArgs()
        {

        }

        public ReceivedInfoEventArgs(string info)
        {
            Date = info.Substring(1, info.IndexOf(']') - 1);
            _info = info.Substring(info.IndexOf(']') + 1).Trim();

            IsFromDevice = _info.Contains("cl_") || _info.Contains("cu_");
            if (IsFromDevice)
            {
                DeviceType = _info.Contains("cl_") ? DeviceTypeList.OpenCL : DeviceTypeList.Cuda;
                DeviceIndex = parseInt(_info.Substring(_info.IndexOf("(") + 1, _info.IndexOf(")") - _info.IndexOf("(") - 1));
                ErrorNbr = parseInt(getValue("err"));
                Temperature = getValue("temp");
                DeviceHashRate = getValue("hashrate");
            }
            else
            {
                DeviceIndex = -1;
                Accepted = getValue("accepted");
                if (Accepted != null && Accepted != string.Empty)
                {
                    AcceptedHash = parseInt(Accepted.Substring(0, Accepted.IndexOf("/")));
                    TotalHash = parseInt(Accepted.Substring(Accepted.IndexOf("/"), Accepted.IndexOf(" ")));
                }
                TotalHashRate = getValue("total hashrate");
                Difficulty = getValue("Stratum difficulty set to");
                BlockNumber = getValue("Verthash block: ");
            }
        }

        private string getValue(string name)
        {
            string _r = null;
            int sindx, eindx;
            sindx = _info.IndexOf(name);

            if (sindx >= 0)
            {
                sindx = sindx + name.Length + 1;
                eindx = _info.IndexOf(",", sindx);
                if (eindx < 0) eindx = _info.Length;
                _r = _info.Substring(sindx, eindx - sindx);
            }

            if (_r != null) _r = _r.Trim();
            return _r;
        }

        private int parseInt(string str)
        {
            if (str == "" || str == null) return Int32.MinValue;
            else
            {
                if (int.TryParse(str, out int val)) return val;
                else return 0;                
            }
        }
    }
    public enum MessageType
    {
        Information,
        Warning,
        Error,
        Debug,
        Native
    }
    public class ReceivedMessageEvent : EventArgs
    {
        public MessageType Type { get; private set; }
        public string Date { get; private set; }
        public string Message { get; private set; }


        private string _info;
        public ReceivedMessageEvent() 
        {
            
        }

        public ReceivedMessageEvent(MessageType type, string message)
        {
            Type = type;
            Message = message;            
        }

        public ReceivedMessageEvent(string info)
        {
            if (info.Contains("]"))
            {
                Date = info.Substring(1, info.IndexOf(']') - 1);
                _info = info.Substring(info.IndexOf(']') + 1).Trim();
            }
            else _info = info;
            
            string[] _w = _info.Split(' ');
            switch (_w[0])
            {
                case "INFO":
                    this.Type = MessageType.Information;
                    this.Message = _info.Substring(_info.IndexOf(" "));
                    break;
                case "WARN":
                    this.Type = MessageType.Warning;
                    this.Message = _info.Substring(_info.IndexOf(" "));
                    break;
                case "ERROR":
                    this.Type = MessageType.Error;
                    this.Message = _info.Substring(_info.IndexOf(" "));
                    break;
                case "DEBUG":
                    this.Type = MessageType.Debug;
                    this.Message = _info.Substring(_info.IndexOf(" "));
                    break;
                default:
                    this.Type = MessageType.Native;
                    this.Message = _info;
                    break;
            }
            
            this.Message = this.Message.Trim();
        }
    }
    public class StatusChangedEventArgs : EventArgs
    {
        public bool Status { get; set; }

        public StatusChangedEventArgs()
        {

        }

        public StatusChangedEventArgs(bool status)
        {
            Status = status;
        }
    }
}
