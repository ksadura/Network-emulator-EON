using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System;
using System.Xml.Linq;

namespace NetworkNodes
{
    public class NetworkNodeConfig
    {
        public IPAddress CCAddress { get; set; }

        public ushort CCPort { get; set; }

        public static string NodeName { get; set; }

        public IPAddress CloudAddress { get; set; }

        public ushort CloudPort { get; set; }

        public static IPAddress NodeAddress;

        public static NetworkNodeConfig ParseConfig(string FileName)
        {
            var config = new NetworkNodeConfig();
	        XDocument doc = new XDocument();
            doc = XDocument.Load(FileName);

            config.CCAddress = IPAddress.Parse(doc.Element("NodeConfig").Element("CCAddress").Value);
            config.CCPort = ushort.Parse(doc.Element("NodeConfig").Element("CCPort").Value);
            NodeName = doc.Element("NodeConfig").Element("NodeName").Value;
            NodeAddress = IPAddress.Parse(doc.Element("NodeConfig").Element("NodeAddress").Value);
            config.CloudAddress = IPAddress.Parse(doc.Element("NodeConfig").Element("CloudAddress").Value);
            config.CloudPort = ushort.Parse(doc.Element("NodeConfig").Element("CloudPort").Value);
	        Console.Title = NodeName;

            return config;
        }
    }
}
