using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Numerics;
using System.Linq;
using System.Xml;

namespace CableCloud
{
    class ConfigCloud
    {
        public static int PORT;
        public static string ADDRESS;
        private static string PATH;
        public static string COLUMN_NAMES;
        public static List<string> ROWS = new List<string>();
        private static Dictionary<string, string> NODES = new Dictionary<string, string>();


        //Reading and parsing config file
        public static void ReadConfig(string path)
        {
            PATH = path;
            XmlDocument doc = new XmlDocument();
            doc.Load(PATH);
            
            foreach(XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.Name.Equals("port"))
                {
                    PORT = int.Parse(node.InnerText);
                }
                else if (node.Name.Equals("address"))
                {
                    ADDRESS = node.InnerText;
                }
                else if (node.Name.Equals("FIB"))
                {
                    foreach(XmlNode n in node.ChildNodes)
                    {
                        if (n.Name.Equals("columns"))
                            COLUMN_NAMES = n.InnerText;
                        if (n.Name.StartsWith("row"))
                            ROWS.Add(n.InnerText);

                    }
                }
                else if (node.Name.Equals("convert"))
                {
                    foreach(XmlNode n in node.ChildNodes)
                    {
                        string[] splitted = n.InnerText.Split("=");
                        NODES.TryAdd(splitted[1], splitted[0]);
                    }
                }
            }
            Console.Title = "Fiber Cloud";
        }

        //Parsing IP address to node name
        public static string GetNodeName(IPAddress address)
        {
            return NODES[address.ToString()];
        }
    }
}
