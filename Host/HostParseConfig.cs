using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace Host
{
    public class HostParseConfig
    {
        public string host_name { get; set;  }
        public ushort output_port { get; set; }
        public IPAddress IP { get; set; }
        public IPAddress cloud_IP { get; set; }
        public ushort cloud_port { get; set; }
        public float power_level { get; set; }
        public List<OtherHostsInfo> other_hosts { get; set; }
        public bool IsReceiving { get; set; }

        public HostParseConfig()
        {
            other_hosts = new List<OtherHostsInfo>();
            IsReceiving = true;
        }
        public static HostParseConfig LoadFromConfigFile(string filename)
        {
            HostParseConfig hostconfig=new HostParseConfig();
            XDocument doc = XDocument.Load(filename);

            hostconfig.host_name = doc.Element("HostandCPCC_Config").Element("HostConfig").Element("HostName").Value;
            hostconfig.cloud_IP = IPAddress.Parse(doc.Element("HostandCPCC_Config").Element("HostConfig").Element("CloudIP").Value);
            hostconfig.cloud_port = ushort.Parse(doc.Element("HostandCPCC_Config").Element("HostConfig").Element("CloudPort").Value);
            hostconfig.IP = IPAddress.Parse(doc.Element("HostandCPCC_Config").Element("HostConfig").Element("IP").Value);
            hostconfig.output_port = ushort.Parse(doc.Element("HostandCPCC_Config").Element("HostConfig").Element("OutputPort").Value);
            hostconfig.power_level = float.Parse(doc.Element("HostandCPCC_Config").Element("HostConfig").Element("PowerLevel").Value);

            return hostconfig;

        }
    }
}
