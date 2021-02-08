using System;
using System.Collections.Generic;
using System.Xml;

namespace LRM
{
    class Config
    {
        public static int PORT;
        public static string ADDRESS;
        private static string PATH;
        public static List<string> LRMROWS = new List<string>();
        private static string NAME;
        public static string CCName;
        public static string RCName;
        public static string Nodes;

        //Reading and parsing config file
        public static void ReadConfig(string path)
        {
            PATH = path;
            XmlDocument doc = new XmlDocument();
            doc.Load(PATH);

            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.Name.Equals("port"))
                {
                    PORT = int.Parse(node.InnerText);
                }
                else if (node.Name.Equals("address"))
                {
                    ADDRESS = node.InnerText;
                }
                else if (node.Name.Equals("name"))
                {
                    NAME = node.InnerText;
                }
                else if (node.Name.Equals("CC"))
                {
                    CCName = node.InnerText;
                }
                else if (node.Name.Equals("RC"))
                {
                    RCName = node.InnerText;
                }
                else if (node.Name.Equals("LRM"))
                {
                    foreach (XmlNode n in node.ChildNodes)
                    {
                        LRMROWS.Add(n.InnerText);
                    }
                }
                else if (node.Name.Equals("nodes"))
                {
                    Nodes = node.InnerText;
                }

            }
            Console.Title = NAME;
        }
    }
}