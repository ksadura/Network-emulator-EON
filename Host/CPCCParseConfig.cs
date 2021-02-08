using System;
using System.Net;
using System.Xml.Linq;
namespace Host
{
    public class CPCCParseConfig
    {
        public IPAddress NCC_IP { get; set; }
        public ushort NCC_port { get; set; }

        public CPCCParseConfig(string filename)
        {
            XDocument doc = XDocument.Load(filename);
            NCC_IP = IPAddress.Parse(doc.Element("HostandCPCC_Config").Element("CPCCConfig").Element("NCCIP").Value);
            NCC_port = ushort.Parse(doc.Element("HostandCPCC_Config").Element("CPCCConfig").Element("NCCPort").Value);
        }
    }
}
