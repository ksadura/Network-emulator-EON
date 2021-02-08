using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;

namespace NCC
{
    public class NCCConfig
    {
        public string IP { get; set; }
        public string Port { get; set; }
        public IPAddress CC_IP { get; set; }
        public ushort CC_Port { get; set; }
        public Dictionary<string,IPAddress> Directory { get; set; }
        public NCCConfig(string filename)
        {
            Directory = new Dictionary<string,IPAddress>();
            getInfo(filename);
        }
        public void getInfo(string filename)
        {
            XDocument doc = XDocument.Load(filename);

            IP = doc.Element("NCC_Config").Element("NCCIP").Value;
            Port = doc.Element("NCC_Config").Element("NCCPort").Value;
            CC_IP = IPAddress.Parse(doc.Element("NCC_Config").Element("CCIP").Value);
            CC_Port = ushort.Parse(doc.Element("NCC_Config").Element("CCPort").Value);
            int Iterator = int.Parse(doc.Element("NCC_Config").Element("Directory").Element("HostCount").Value);
            for(int i = 0; i < Iterator; i++)
            {
                int change = i + 1;
                string temp = "H" + change.ToString();
                Directory.TryAdd(temp, IPAddress.Parse(doc.Element("NCC_Config").Element("Directory").Element(temp).Value));
            }
        }
    }
}
