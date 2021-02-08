using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Net;
using Commons.Tools;

namespace CC
{
    public class Config
    {
        public ConnectionData RCConnectionData { get; set; }
        public ConnectionData LRMConnectionData { get; set; }
        public IPAddress IP { get; set; }
        public ushort Port { get; set; }
        public Dictionary<Tuple<IPAddress, IPAddress>,string> Modulations { get; set; }
        public bool isSubCC { get; set; }
        public ConnectionData CCConnectionData { get; set; }
        public static string Name { get; set; }
        public Dictionary<ushort,IPAddress> PortNodes { get; set; }

    public static Config ParseConfig(string FileName)
        {
            var config = new Config();
            XDocument doc = new XDocument();
            doc = XDocument.Load(FileName);

            ConnectionData RCconnectionData = new ConnectionData();
            RCconnectionData.ipAddress = IPAddress.Parse(doc.Element("CCConfig").Element("RCAddress").Value);
            RCconnectionData.port = ushort.Parse(doc.Element("CCConfig").Element("RCPort").Value);
            config.RCConnectionData = RCconnectionData;
            ConnectionData LRMconnectionData = new ConnectionData();
            LRMconnectionData.ipAddress = IPAddress.Parse(doc.Element("CCConfig").Element("LRMAddress").Value);
            LRMconnectionData.port = ushort.Parse(doc.Element("CCConfig").Element("LRMPort").Value);
            config.LRMConnectionData = LRMconnectionData;
            config.IP = IPAddress.Parse(doc.Element("CCConfig").Element("IP").Value);
            config.Port = ushort.Parse(doc.Element("CCConfig").Element("Port").Value);
            Dictionary<Tuple<IPAddress, IPAddress>, string> modulations = new Dictionary<Tuple<IPAddress, IPAddress>, string>();
            foreach (var element in doc.Element("CCConfig").Element("Modulations").Elements("Modulation"))
            {
                string modulation = element.Element("Name").Value;
                IPAddress host_from = IPAddress.Parse(element.Element("HostFrom").Value);
                IPAddress host_to = IPAddress.Parse(element.Element("HostTo").Value);
                Tuple<IPAddress, IPAddress> hosts = new Tuple<IPAddress, IPAddress>(host_from, host_to);
                modulations.Add(hosts, modulation);
            }
            config.Modulations = modulations;
            config.isSubCC = bool.Parse(doc.Element("CCConfig").Element("subCC").Value);
            if (config.isSubCC)
            {
                ConnectionData CCconnectionData = new ConnectionData();
                CCconnectionData.ipAddress = IPAddress.Parse(doc.Element("CCConfig").Element("CCAddress").Value);
                CCconnectionData.port = ushort.Parse(doc.Element("CCConfig").Element("CCPort").Value);
                config.CCConnectionData = CCconnectionData;
                Dictionary<ushort, IPAddress> portNodes = new Dictionary<ushort, IPAddress>();
                foreach (var element in doc.Element("CCConfig").Element("Nodes").Elements("Node"))
                {
                    ushort port = ushort.Parse(element.Element("Port").Value);
                    IPAddress IP = IPAddress.Parse(element.Element("IP").Value);
                    portNodes.Add(port, IP);
                }
                config.PortNodes = portNodes;
            }
            Config.Name = doc.Element("CCConfig").Element("Name").Value;
            Console.Title = Config.Name;

            return config;
        }
    }
}
