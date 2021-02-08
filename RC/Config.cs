using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Dijkstra
{
    class Config
    {
        public static List<Link> linksArray;
        public static List<Node> nodesArray;
        public static string Address;
        public static int Port;
        public static Dictionary<string, string> linkWithPorts;
        public static Dictionary<string, string> ipToName;
        public static int LRMPort;
        public static string Name;

        static Config()
        {
            linksArray = new List<Link>();
            nodesArray = new List<Node>();
            linkWithPorts = new Dictionary<string, string>();
            ipToName = new Dictionary<string, string>();
        }

        public static void ReadConfig(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            foreach(string line in lines)
            {
                switch (line.Split(" ")[0])
                {
                    case "Name:":
                        Name = line.Split(" ")[1];
                        break;
                    case "Node:":
                        nodesArray.Add(new Node(line.Split(" ")[1]));
                        break;
                    case "Link:":
                        string id = line.Split(" ")[1];
                        int weight = int.Parse(line.Split(" ")[2]);
                        int capacity = int.Parse(line.Split(" ")[3]);
                        linksArray.Add(new Link(capacity, weight, id));
                        linkWithPorts.Add(id, line.Split(" ")[4]);
                        linkWithPorts.Add(ReverseID(id), ReversePorts(line.Split(" ")[4]));
                        break;
                    case "IPAddress:":
                        Address = line.Split(" ")[1];
                        break;
                    case "Port:":
                        Port = int.Parse(line.Split(" ")[1]);
                        break;
                    case "Convert:":
                        ipToName.Add(line.Split(" ")[1], line.Split(" ")[2]);
                        break;
                    case "PortLRM:":
                        LRMPort = Convert.ToInt32(line.Split(" ")[1]);
                        break;
                    default:
                        break;

                }
            }
            Console.Title = Name;
            
        }

        private static string ReverseID(string id)
        {
            string[] nodes = id.Split("-");
            return nodes[1] + "-" + nodes[0];
        }

        private static string ReversePorts(string _ports)
        {
            string[] ports = _ports.Split("-");
            return  ports[1] + "-" + ports[0];
        }
    }
}
